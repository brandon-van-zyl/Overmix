using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Overmix;

public sealed partial class OvermixApplication : Application
{
    private Shell? _shell;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        // Tests run the app without a desktop, and build their windows themselves.
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) _shell = Shell.Start(desktop);
        base.OnFrameworkInitializationCompleted();
    }
}
