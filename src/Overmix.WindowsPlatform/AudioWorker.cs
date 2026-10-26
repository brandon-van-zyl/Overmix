using System.Diagnostics;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using Overmix.Core;
using Overmix.Core.Sound;

namespace Overmix.WindowsPlatform;

/// <summary>What Windows' audio looked like at one moment.</summary>
internal sealed record AudioSnapshot(
    IReadOnlyDictionary<ProgramName, ProgramState> Programs,
    IReadOnlyDictionary<DeviceId, bool> MicsMuted,
    IReadOnlyList<SoundOutput> Outputs,
    DeviceId? DefaultOutput,
    long CommandsDone);

internal sealed record ProgramState(string Name, Volume Volume);

/// <summary>
/// Owns every Windows audio object on one background thread. Windows' change notifications trigger a fresh
/// <see cref="AudioSnapshot"/> for the UI thread; between changes nothing runs at all.
/// </summary>
internal sealed class AudioWorker : IDisposable
{
    /// <summary>Notifications come in bursts, so wait this long for a burst to settle before looking.</summary>
    private static readonly TimeSpan Settle = TimeSpan.FromMilliseconds(150);

    /// <summary>A slow look now and then, in case Windows ever misses telling us about a change.</summary>
    private static readonly TimeSpan SafetyNet = TimeSpan.FromSeconds(30);

    private readonly SynchronizationContext _ui;
    private readonly ComThread _thread = new("Overmix audio", ApartmentState.MTA);
    private readonly Timer _settle;
    private readonly Timer _safetyNet;
    private readonly SessionEvents _sessionEvents;
    private volatile bool _disposed;
    private int _refreshScheduled;
    private long _commandsSent;
    private long _commandsDone;

    // Used only on the audio thread.
    private readonly Dictionary<string, TrackedDevice> _outputs = [];
    private readonly Dictionary<string, TrackedDevice> _mics = [];
    private readonly Dictionary<string, TrackedSession> _sessions = [];
    private readonly ProcessNames _processNames = new();
    private MMDeviceEnumerator? _enumerator;
    private MMDeviceNotificationClient? _deviceEvents;

    public AudioWorker(SynchronizationContext ui)
    {
        _ui = ui;
        _sessionEvents = new SessionEvents(RequestRefresh);
        _settle = new Timer(_ => _thread.Post(Refresh));
        _safetyNet = new Timer(_ => RequestRefresh(), null, SafetyNet, SafetyNet);
        _thread.Post(Start);
    }

    /// <summary>A fresh snapshot, raised on the UI thread.</summary>
    public event Action<AudioSnapshot>? SnapshotReady;

    public void SetProgramVolume(ProgramName program, Volume volume) => Send(() =>
    {
        foreach (var session in _sessions.Values.Where(session => session.Program == program))
            session.Control.SimpleAudioVolume.Volume = volume.ToScalar();
    });

    public void SetMicMuted(DeviceId mic, bool muted) => Send(() =>
    {
        if (_mics.TryGetValue(mic.Value, out var device)) device.Device.AudioEndpointVolume.Mute = muted;
    });

    public void SetDefaultOutput(DeviceId output) => Send(() => PolicyConfig.SetDefaultOutput(output.Value));

    public void Dispose()
    {
        _disposed = true;
        _safetyNet.Dispose();
        _settle.Dispose();
        _thread.Post(Stop);
        _thread.Dispose();
    }

    /// <summary>Runs a change on the audio thread. Called on the UI thread.</summary>
    private void Send(Action command)
    {
        var id = ++_commandsSent;
        _thread.Post(() =>
        {
            Try(command);
            _commandsDone = id;
            RequestRefresh();
        });
    }

    /// <summary>Asks for a fresh snapshot once things settle. Safe from any thread.</summary>
    private void RequestRefresh()
    {
        if (_disposed || Interlocked.Exchange(ref _refreshScheduled, 1) == 1) return;
        try
        {
            _settle.Change(Settle, Timeout.InfiniteTimeSpan);
        }
        catch (ObjectDisposedException)
        {
            // Shutting down.
        }
    }

    private void Start()
    {
        _enumerator = new MMDeviceEnumerator();
        _deviceEvents = _enumerator.CreateNotificationClient(useSynchronizationContext: false);
        _deviceEvents.DeviceAdded += (_, _) => RequestRefresh();
        _deviceEvents.DeviceRemoved += (_, _) => RequestRefresh();
        _deviceEvents.DeviceStateChanged += (_, _) => RequestRefresh();
        _deviceEvents.DefaultDeviceChanged += (_, _) => RequestRefresh();
        Refresh();
    }

    private void Refresh()
    {
        Interlocked.Exchange(ref _refreshScheduled, 0);
        if (_disposed || _enumerator is not { } enumerator) return;

        Try(() => SyncDevices(enumerator, DataFlow.Render, _outputs, WatchSessions));
        Try(() => SyncDevices(enumerator, DataFlow.Capture, _mics, WatchMute));
        Try(SyncSessions);
        var snapshot = TakeSnapshot(enumerator);
        _ui.Post(_ => Deliver(snapshot), null);
    }

    private void Deliver(AudioSnapshot snapshot)
    {
        // A snapshot taken before our latest change ran would undo it on screen. The one after it is on its way.
        if (_disposed || snapshot.CommandsDone < _commandsSent) return;
        SnapshotReady?.Invoke(snapshot);
    }

    private void WatchSessions(MMDevice output) =>
        output.AudioSessionManager.OnSessionCreated += (_, _) => RequestRefresh();

    private void WatchMute(MMDevice mic)
    {
        var volume = mic.AudioEndpointVolume;
        var muted = volume.Mute;
        volume.OnVolumeNotification += data =>
        {
            // Some apps adjust the mic level constantly, so only a change of mute counts.
            if (data.Muted == Volatile.Read(ref muted)) return;
            Volatile.Write(ref muted, data.Muted);
            RequestRefresh();
        };
    }

    private static void SyncDevices(
        MMDeviceEnumerator enumerator, DataFlow flow, Dictionary<string, TrackedDevice> tracked, Action<MMDevice> watch)
    {
        var present = new HashSet<string>();
        using (var devices = enumerator.EnumerateAudioEndPoints(flow, DeviceState.Active))
        {
            for (var i = 0; i < devices.Count; i++)
            {
                var device = devices[i];
                var keep = false;
                try
                {
                    var id = device.ID;
                    present.Add(id);
                    if (tracked.ContainsKey(id)) continue;
                    watch(device);
                    tracked[id] = new TrackedDevice(device, device.FriendlyName);
                    keep = true;
                }
                catch (Exception e) when (IsAudioError(e))
                {
                    Trace.TraceWarning($"Overmix audio: skipped a device: {e.Message}");
                }
                finally
                {
                    if (!keep) device.Dispose();
                }
            }
        }

        foreach (var gone in tracked.Keys.Where(id => !present.Contains(id)).ToList())
        {
            Try(tracked[gone].Device.Dispose);
            tracked.Remove(gone);
        }
    }

    private void SyncSessions()
    {
        var present = new HashSet<string>();
        foreach (var output in _outputs.Values)
        {
            var manager = output.Device.AudioSessionManager;
            manager.RefreshSessions();
            var sessions = manager.Sessions;
            for (var i = 0; i < sessions.Count; i++)
            {
                var control = sessions[i];
                var keep = false;
                try
                {
                    if (control.IsSystemSoundsSession || control.State == AudioSessionState.AudioSessionStateExpired) continue;
                    var id = control.GetSessionInstanceIdentifier;
                    present.Add(id);
                    if (_sessions.ContainsKey(id)) continue;
                    if (_processNames.Find((int)control.GetProcessID) is not { } name) continue;
                    control.RegisterEventClient(_sessionEvents);
                    _sessions[id] = new TrackedSession(control, new ProgramName(name), Pretty(name));
                    keep = true;
                }
                catch (Exception e) when (IsAudioError(e))
                {
                    // The session ended while we looked at it.
                }
                finally
                {
                    if (!keep) control.Dispose();
                }
            }
        }

        foreach (var gone in _sessions.Keys.Where(id => !present.Contains(id)).ToList())
        {
            Try(_sessions[gone].Control.Dispose);
            _sessions.Remove(gone);
        }
    }

    private AudioSnapshot TakeSnapshot(MMDeviceEnumerator enumerator)
    {
        var programs = new Dictionary<ProgramName, ProgramState>();
        foreach (var session in _sessions.Values)
        {
            if (!TryRead(() => session.Control.SimpleAudioVolume.Volume, out var scalar)) continue;
            var volume = Volume.FromScalar(scalar);

            // A program with several sessions, one per process or device, shows its loudest.
            if (!programs.TryGetValue(session.Program, out var known) || volume.Percent > known.Volume.Percent)
                programs[session.Program] = new ProgramState(session.Name, volume);
        }

        var micsMuted = new Dictionary<DeviceId, bool>();
        foreach (var (id, mic) in _mics)
        {
            if (TryRead(() => mic.Device.AudioEndpointVolume.Mute, out var muted)) micsMuted[new DeviceId(id)] = muted;
        }

        var outputs = _outputs
            .Select(pair => new SoundOutput(new DeviceId(pair.Key), pair.Value.Name))
            .OrderBy(output => output.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        return new AudioSnapshot(programs, micsMuted, outputs, DefaultOutput(enumerator), _commandsDone);
    }

    private static DeviceId? DefaultOutput(MMDeviceEnumerator enumerator)
    {
        try
        {
            if (!enumerator.TryGetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia, out var device)) return null;
            using (device)
            {
                return new DeviceId(device.ID);
            }
        }
        catch (Exception e) when (IsAudioError(e))
        {
            return null;
        }
    }

    private void Stop()
    {
        foreach (var session in _sessions.Values) Try(session.Control.Dispose);
        foreach (var device in _outputs.Values.Concat(_mics.Values)) Try(device.Device.Dispose);
        _sessions.Clear();
        _outputs.Clear();
        _mics.Clear();
        Try(() => _deviceEvents?.Dispose());
        Try(() => _enumerator?.Dispose());
    }

    /// <summary>"chrome" reads better as "Chrome"; names with capitals are left alone.</summary>
    private static string Pretty(string name) =>
        name.Length > 0 && name.All(c => !char.IsUpper(c)) ? char.ToUpperInvariant(name[0]) + name[1..] : name;

    private static bool IsAudioError(Exception e) =>
        e is COMException or InvalidOperationException or NotSupportedException or ObjectDisposedException
            or UnauthorizedAccessException or ArgumentException;

    private static void Try(Action action)
    {
        try
        {
            action();
        }
        catch (Exception e) when (IsAudioError(e))
        {
            Trace.TraceWarning($"Overmix audio: {e.Message}");
        }
    }

    private static bool TryRead<T>(Func<T> read, out T value)
    {
        try
        {
            value = read();
            return true;
        }
        catch (Exception e) when (IsAudioError(e))
        {
            value = default!;
            return false;
        }
    }

    private sealed record TrackedDevice(MMDevice Device, string Name);

    private sealed record TrackedSession(AudioSessionControl Control, ProgramName Program, string Name);

    /// <summary>Turns the session notifications that matter into a refresh.</summary>
    private sealed class SessionEvents(Action changed) : IAudioSessionEventsHandler
    {
        public void OnVolumeChanged(float volume, bool isMuted) => changed();

        public void OnStateChanged(AudioSessionState state)
        {
            // Playing and paused both count as using sound; only an ended session matters.
            if (state == AudioSessionState.AudioSessionStateExpired) changed();
        }

        public void OnSessionDisconnected(AudioSessionDisconnectReason disconnectReason) => changed();

        public void OnDisplayNameChanged(string displayName)
        {
        }

        public void OnIconPathChanged(string iconPath)
        {
        }

        public void OnChannelVolumeChanged(uint channelCount, nint newVolumes, uint channelIndex)
        {
        }

        public void OnGroupingParamChanged(ref Guid groupingId)
        {
        }
    }
}
