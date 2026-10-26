using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Overmix.ViewModels;

namespace Overmix.Views;

public sealed partial class SettingsView : UserControl
{
    public SettingsView() => InitializeComponent();

    private async void OnBrowse(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (TopLevel.GetTopLevel(this)?.StorageProvider is not { } storage || DataContext is not SettingsViewModel settings) return;
            var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Choose a program to add",
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("Programs") { Patterns = ["*.exe"] }],
            });
            if (files.Count > 0 && files[0].TryGetLocalPath() is { } path) settings.AddProgramFile(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Trace.TraceWarning($"Overmix: couldn't add that program: {ex.Message}");
        }
    }
}
