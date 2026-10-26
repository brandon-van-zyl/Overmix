namespace Overmix.Core.Sound;

/// <summary>Your list of Apps, in the order their Dials appear.</summary>
public sealed class AppList
{
    // Replaced, never changed in place, so anyone looping over Items is safe even if the list changes meanwhile.
    private App[] _apps;

    public AppList(IEnumerable<App> apps) => _apps = apps.DistinctBy(app => app.Program).ToArray();

    public IReadOnlyList<App> Items => _apps;

    public event EventHandler? Changed;

    public bool Contains(ProgramName program) => Find(program) is not null;

    public App? Find(ProgramName program) => Array.Find(_apps, app => app.Program == program);

    /// <summary>Adds an App to the end of the list. Returns false if its program is already on it.</summary>
    public bool Add(App app)
    {
        if (Contains(app.Program)) return false;
        Replace([.. _apps, app]);
        return true;
    }

    public void Remove(ProgramName program)
    {
        if (Contains(program)) Replace(_apps.Where(app => app.Program != program).ToArray());
    }

    /// <summary>Replaces the App for the same program, for example to change its Shortcuts.</summary>
    public void Update(App app)
    {
        var index = Array.FindIndex(_apps, existing => existing.Program == app.Program);
        if (index < 0 || _apps[index] == app) return;
        var apps = (App[])_apps.Clone();
        apps[index] = app;
        Replace(apps);
    }

    private void Replace(App[] apps)
    {
        _apps = apps;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
