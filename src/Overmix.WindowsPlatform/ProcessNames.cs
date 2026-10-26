using System.Diagnostics;

namespace Overmix.WindowsPlatform;

/// <summary>
/// Maps process IDs to program names using Windows' process list, which needs no access to the processes
/// themselves. That keeps Overmix from opening handles to games protected by anti-cheat.
/// </summary>
internal sealed class ProcessNames
{
    private Dictionary<int, string> _names = [];

    /// <summary>The program name for a process, or null if it isn't running.</summary>
    public string? Find(int processId)
    {
        if (_names.TryGetValue(processId, out var name)) return name;
        _names = Snapshot();
        return _names.GetValueOrDefault(processId);
    }

    /// <summary>The IDs of every process running right now.</summary>
    public IReadOnlySet<int> Running()
    {
        _names = Snapshot();
        return _names.Keys.ToHashSet();
    }

    private static Dictionary<int, string> Snapshot()
    {
        var names = new Dictionary<int, string>();
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                names[process.Id] = process.ProcessName;
            }
        }

        return names;
    }
}
