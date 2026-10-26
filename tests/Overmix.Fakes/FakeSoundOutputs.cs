using Overmix.Core;
using Overmix.Core.Sound;

namespace Overmix.Fakes;

/// <summary>Pretend sound outputs. The first one plugged in becomes the current one.</summary>
public sealed class FakeSoundOutputs : ISoundOutputs
{
    private readonly List<SoundOutput> _all = [];

    public IReadOnlyList<SoundOutput> All => _all;

    public DeviceId? Current { get; private set; }

    public event EventHandler? Changed;

    public void MakeCurrent(DeviceId output)
    {
        if (Current == output || !_all.Exists(existing => existing.Id == output)) return;
        Current = output;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Plug(string id, string name)
    {
        var output = new SoundOutput(new DeviceId(id), name);
        _all.Add(output);
        Current ??= output.Id;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Unplug(string id)
    {
        var device = new DeviceId(id);
        if (_all.RemoveAll(output => output.Id == device) == 0) return;
        if (Current == device) Current = _all.FirstOrDefault()?.Id;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
