using Overmix.Core.Media;
using Overmix.Core.Shortcuts;
using Overmix.Core.Sound;
using Overmix.Core.Voice;

namespace Overmix.Core;

/// <summary>
/// The PC-specific parts Overmix runs on: the Windows implementations on the gaming PC, fakes in tests.
/// Every part raises its events on the UI thread.
/// </summary>
public sealed record Platform(
    IProgramSound Sound,
    IMainScreen MainScreen,
    IMicrophones Microphones,
    ISoundOutputs Outputs,
    INowPlaying NowPlaying,
    IShortcutKeys ShortcutKeys,
    IStartWithWindows StartWithWindows,
    IGameFocus GameFocus,
    IProgramIcons Icons);
