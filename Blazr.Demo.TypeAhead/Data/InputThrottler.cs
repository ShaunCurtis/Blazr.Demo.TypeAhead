using System.Diagnostics;

namespace Blazr.Demo.TypeAhead;

public class InputThrottler
{
    private int _backOff = 0;
    private Func<Task> _taskToRun;
    private Task _runningTask = Task.CompletedTask;
    private TaskCompletionSource<bool>? _queuedTaskCompletionSource;
    private TaskCompletionSource<bool>? _runningTaskCompletionSource;

    private async Task RunQueueAsync()
    {
        ///Debug.WriteLine($"Running RunQueueAsync");

        // if we have a completed task then null it
        if (_runningTaskCompletionSource is not null && _runningTaskCompletionSource.Task.IsCompleted)
            _runningTaskCompletionSource = null;

        // if we have a running task then everything is already in motion and there's nothing to do
        if (_runningTaskCompletionSource is not null)
            return;

        ///int counter = 0;

        // run the loop while we have a queued request.
        while (_queuedTaskCompletionSource is not null)
        {
            ///Debug.WriteLine($"In the Do Loop");

            // assign the queued task reference to the running task  
            _runningTaskCompletionSource = _queuedTaskCompletionSource;
            // And release the reference
            _queuedTaskCompletionSource = null;

            // start backoff task
            var backoffTask = Task.Delay(_backOff);

            // start main task
            var mainTask = _taskToRun.Invoke();

            // await both ensures we run the backoff period or greater
            await Task.WhenAll( new Task[] { mainTask, backoffTask } );

            // Set the running task completion as complete
            _runningTaskCompletionSource?.SetResult(true);

            // and release our reference to the running task completion
            // The originator will still hold a reference and can act on it's completion
            _runningTaskCompletionSource = null;

            ///Debug.WriteLine($"Completed Do loop {counter}");
            ///counter++;

            // back to the top to check if another task has been queued
        }
        Debug.WriteLine($"Exited Do loop");

        return;
    }

    public Task<bool> QueueAsync()
    {
        var oldCompletionTask = _queuedTaskCompletionSource;

        // Create a new completion task
        var newCompletionTask = new TaskCompletionSource<bool>();

        // get the actual task before we assign it to the queue
        var task = newCompletionTask.Task;

        // replace _queuedTaskCompletionSource
        _queuedTaskCompletionSource = newCompletionTask;

        // check if we already have a queued queued task.
        // If so set it as completed, false = not run 
        if (oldCompletionTask is not null)
        {
            oldCompletionTask?.TrySetResult(false);
            ///Debug.WriteLine($"Queued Completion Task discarded");
        }


        // if we don't have a running task or the task is complete , then there's no process running the queue
        // So we need to call it and assign it to `runningTask`
        if (_runningTask is null || _runningTask.IsCompleted)
            _runningTask = this.RunQueueAsync();

        // return the reference to the task we queued
        return task;
    }

    private InputThrottler(Func<Task> toRun, int backOff)
    {
        _backOff = backOff;
        _taskToRun = toRun;
    }

    /// <summary>
    /// Static method to create a new InputThrottler
    /// </summary>
    /// <param name="toRun">method to run to update the component</param>
    /// <param name="backOff">Back off period in millisecs</param>
    /// <returns></returns>
    public static InputThrottler Create(Func<Task> toRun, int backOff)
            => new InputThrottler(toRun, backOff > 300 ? backOff : 300);
}
