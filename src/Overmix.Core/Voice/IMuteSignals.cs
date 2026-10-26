namespace Overmix.Core.Voice;

/// <summary>Plays the Mute Chime: a short sound as Mic Mute turns on or off.</summary>
public interface IMuteChime
{
    void Play(bool muted);
}

/// <summary>The Mute Border: a red border around every screen while Mic Mute is on.</summary>
public interface IMuteBorder
{
    void Show();

    void Hide();
}
