namespace Overmix.Core.Sound;

/// <summary>The two outputs the Output Switch flips between.</summary>
public sealed record OutputPair(DeviceId First, DeviceId Second);

/// <summary>Flips your sound between two chosen outputs, normally your headset and your speakers.</summary>
public sealed class OutputSwitch
{
    private readonly ISoundOutputs _outputs;

    public OutputSwitch(ISoundOutputs outputs, OutputPair? pair)
    {
        _outputs = outputs;
        Pair = pair;
        outputs.Changed += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
    }

    public OutputPair? Pair { get; private set; }

    public IReadOnlyList<SoundOutput> Available => _outputs.All;

    public SoundOutput? Current => Find(_outputs.Current);

    /// <summary>The output a flip switches to, if it's plugged in.</summary>
    public SoundOutput? Other => Pair is { } pair
        ? Find(_outputs.Current == pair.First ? pair.Second : pair.First)
        : null;

    /// <summary>The pair, the outputs or the current output changed.</summary>
    public event EventHandler? Changed;

    public void Choose(OutputPair? pair)
    {
        if (pair == Pair) return;
        Pair = pair;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Switches to the other output of the pair. Returns false if it isn't plugged in.</summary>
    public bool Flip()
    {
        if (Other is not { } other) return false;
        _outputs.MakeCurrent(other.Id);
        return true;
    }

    private SoundOutput? Find(DeviceId? id) => _outputs.All.FirstOrDefault(output => output.Id == id);
}
