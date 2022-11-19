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
        //Debug.WriteLine($"");
        //Debug.WriteLine($"Running RunQueueAsync");

        // if we have a completed task then null it
        if (_runningTaskCompletionSource is not null && _runningTaskCompletionSource.Task.IsCompleted)
            _runningTaskCompletionSource = null;

        // if we have a running task then nothing to do
        if (_runningTaskCompletionSource is not null)
            return;

        //int counter = 0;

        // run the loop while we have a queued request.
        while (_queuedTaskCompletionSource is not null)
        {
            //Debug.WriteLine($"In the Do Loop");

            // assign the queued task reference to the running task  
            _runningTaskCompletionSource = _queuedTaskCompletionSource;
            // And release the reference
            _queuedTaskCompletionSource = null;

            // do our backoff
            await Task.Delay(_backOff);

            // run the task
            await _taskToRun.Invoke();

            // Set the running taks as complete
            _runningTaskCompletionSource?.SetResult(true);

            // and release the reference to the running task;
            _runningTaskCompletionSource = null;

            //Debug.WriteLine($"Completed Do loop {counter}");
            //counter++;

            // back to the top to check if another task has been queued
        }
        //Debug.WriteLine($"Exited Do loop");

        return;
    }

    public Task<bool> QueueAsync()
    {
        // check if we already have a queued queued task.
        // If so set it as completed, false = not run 
        if (_queuedTaskCompletionSource is not null)
            _queuedTaskCompletionSource?.SetResult(false);

        // Create a new completion task
        var newCompletionTask = new TaskCompletionSource<bool>();

        // get the actual task before we assign it to the queue
        var task = newCompletionTask.Task;

        // add a new task to the queue
        // note that this releases the old one
        _queuedTaskCompletionSource = newCompletionTask;

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
            => new InputThrottler(toRun, backOff > 250 ? backOff : 250);
}
