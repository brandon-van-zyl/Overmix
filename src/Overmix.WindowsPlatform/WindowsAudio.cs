using Overmix.Core;
using Overmix.Core.Sound;
using Overmix.Core.Voice;

namespace Overmix.WindowsPlatform;

// These three live on the UI thread and keep the latest AudioWorker snapshot. Changes made through them
// show straight away; the next snapshot confirms them.

internal sealed class WindowsProgramSound : IProgramSound
{
    private readonly AudioWorker _audio;
    private Dictionary<ProgramName, ProgramState> _programs = [];

    public WindowsProgramSound(AudioWorker audio)
    {
        _audio = audio;
        audio.SnapshotReady += Apply;
    }

    public IReadOnlyCollection<SoundProgram> UsingSound =>
        _programs.Select(program => new SoundProgram(program.Key, program.Value.Name)).ToList();

    public event EventHandler<ProgramName>? Started;

    public event EventHandler<ProgramName>? Stopped;

    public event EventHandler<ProgramName>? VolumeChanged;

    public Volume? GetVolume(ProgramName program) =>
        _programs.TryGetValue(program, out var state) ? state.Volume : null;

    public void SetVolume(ProgramName program, Volume volume)
    {
        if (!_programs.TryGetValue(program, out var state) || state.Volume == volume) return;
        _programs[program] = state with { Volume = volume };
        _audio.SetProgramVolume(program, volume);
        VolumeChanged?.Invoke(this, program);
    }

    private void Apply(AudioSnapshot snapshot)
    {
        var before = _programs;
        _programs = new Dictionary<ProgramName, ProgramState>(snapshot.Programs);
        foreach (var program in before.Keys.Where(program => !_programs.ContainsKey(program)).ToList())
            Stopped?.Invoke(this, program);
        foreach (var (program, state) in _programs.ToList())
        {
            if (!before.TryGetValue(program, out var old)) Started?.Invoke(this, program);
            else if (old.Volume != state.Volume) VolumeChanged?.Invoke(this, program);
        }
    }
}

internal sealed class WindowsMicrophones : IMicrophones
{
    private readonly AudioWorker _audio;
    private Dictionary<DeviceId, bool> _muted = [];

    public WindowsMicrophones(AudioWorker audio)
    {
        _audio = audio;
        audio.SnapshotReady += Apply;
    }

    public IReadOnlyCollection<DeviceId> All => _muted.Keys.ToList();

    public event EventHandler<DeviceId>? Added;

    public event EventHandler<DeviceId>? Removed;

    public event EventHandler<DeviceId>? MuteChanged;

    public bool IsMuted(DeviceId mic) => _muted.GetValueOrDefault(mic);

    public void SetMuted(DeviceId mic, bool muted)
    {
        if (!_muted.TryGetValue(mic, out var was) || was == muted) return;
        _muted[mic] = muted;
        _audio.SetMicMuted(mic, muted);
        MuteChanged?.Invoke(this, mic);
    }

    private void Apply(AudioSnapshot snapshot)
    {
        var before = _muted;
        _muted = new Dictionary<DeviceId, bool>(snapshot.MicsMuted);
        foreach (var mic in before.Keys.Where(mic => !_muted.ContainsKey(mic)).ToList())
            Removed?.Invoke(this, mic);

        // Handlers may mute a mic as it's added (Mic Mute does), so work from a copy.
        foreach (var (mic, muted) in _muted.ToList())
        {
            if (!before.TryGetValue(mic, out var was)) Added?.Invoke(this, mic);
            else if (was != muted) MuteChanged?.Invoke(this, mic);
        }
    }
}

internal sealed class WindowsSoundOutputs : ISoundOutputs
{
    private readonly AudioWorker _audio;

    public WindowsSoundOutputs(AudioWorker audio)
    {
        _audio = audio;
        audio.SnapshotReady += Apply;
    }

    public IReadOnlyList<SoundOutput> All { get; private set; } = [];

    public DeviceId? Current { get; private set; }

    public event EventHandler? Changed;

    public void MakeCurrent(DeviceId output)
    {
        if (Current == output || !All.Any(existing => existing.Id == output)) return;
        Current = output;
        _audio.SetDefaultOutput(output);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Apply(AudioSnapshot snapshot)
    {
        if (snapshot.Outputs.SequenceEqual(All) && snapshot.DefaultOutput == Current) return;
        All = snapshot.Outputs;
        Current = snapshot.DefaultOutput;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
