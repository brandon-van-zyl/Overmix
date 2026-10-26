using Overmix.Core.Sound;

namespace Overmix.Fakes;

/// <summary>Pretend per-program sound: programs start and stop, and their volume can change from outside Overmix.</summary>
public sealed class FakeProgramSound : IProgramSound
{
    private readonly Dictionary<ProgramName, (string Name, Volume Volume)> _programs = [];

    public IReadOnlyCollection<SoundProgram> UsingSound =>
        _programs.Select(program => new SoundProgram(program.Key, program.Value.Name)).ToList();

    public event EventHandler<ProgramName>? Started;

    public event EventHandler<ProgramName>? Stopped;

    public event EventHandler<ProgramName>? VolumeChanged;

    public Volume? GetVolume(ProgramName program) =>
        _programs.TryGetValue(program, out var playing) ? playing.Volume : null;

    public void SetVolume(ProgramName program, Volume volume)
    {
        if (!_programs.TryGetValue(program, out var playing) || playing.Volume == volume) return;
        _programs[program] = (playing.Name, volume);
        VolumeChanged?.Invoke(this, program);
    }

    /// <summary>A program starts using sound.</summary>
    public void Start(string program, int volume = 100, string? name = null)
    {
        var key = new ProgramName(program);
        if (_programs.TryAdd(key, (name ?? key.Value, new Volume(volume)))) Started?.Invoke(this, key);
    }

    /// <summary>A program stops using sound, as when it quits.</summary>
    public void Stop(string program)
    {
        var key = new ProgramName(program);
        if (_programs.Remove(key)) Stopped?.Invoke(this, key);
    }
}
