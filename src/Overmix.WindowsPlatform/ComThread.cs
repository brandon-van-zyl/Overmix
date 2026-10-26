using System.Collections.Concurrent;
using System.Diagnostics;

namespace Overmix.WindowsPlatform;

/// <summary>A dedicated thread in a chosen COM apartment, working through a queue. It sleeps while the queue is empty.</summary>
internal sealed class ComThread : IDisposable
{
    private readonly BlockingCollection<Action> _queue = new();
    private readonly Thread _thread;

    public ComThread(string name, ApartmentState apartment)
    {
        _thread = new Thread(Run) { Name = name, IsBackground = true };
        _thread.SetApartmentState(apartment);
        _thread.Start();
    }

    public void Post(Action work)
    {
        try
        {
            _queue.Add(work);
        }
        catch (InvalidOperationException)
        {
            // Shutting down: late work is dropped.
        }
    }

    public Task<T> Run<T>(Func<T> work)
    {
        var result = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        Post(() =>
        {
            try
            {
                result.SetResult(work());
            }
            catch (Exception e)
            {
                result.SetException(e);
            }
        });
        return result.Task;
    }

    public void Dispose()
    {
        _queue.CompleteAdding();
        _thread.Join(TimeSpan.FromSeconds(2));
    }

    private void Run()
    {
        foreach (var work in _queue.GetConsumingEnumerable())
        {
            try
            {
                work();
            }
            catch (Exception e)
            {
                Trace.TraceError($"{_thread.Name}: {e}");
            }
        }
    }
}
