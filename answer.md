# Building a Blazor Autocomplete Control

You can either buy into a component library ot build your own.  This article shows you how to build your own.

Before we look at the control we need a *De-Bouncer**.  De-bouncing, for those unsure of what I mean, is controlling the number of component refreshes and calls to the data pipeline as you type.

If you type "uni", does the control lookup and refrash the list on every keystroke, or does it wait until you stop typing?  The user experience will depend on how quickly the control can fetch the data and then update the display.  If the data pipeine is slower that your typing speed, you start to stack requests. There will be perceptible delay while the data pipeline and UI catch up when you stop.

*De-bouncing* is a mechanism to minimize the effect.  The normal technique aplied is to use a timer which resets on each keypress and only executes the data pipeline request when the timer expires.  It normally about 300 millseconds.  The major issue with this technique is that it's an accumulator.  The time taken to execute the data pipeline request abd update the display is the timer + the query/refresh.

## InputThrottler

This is my de-bouncer.  It uses built in functionality in the Async library.

Here's the class outline.

```csharp
public class InputThrottler
{
    private int _backOff = 0;
    private Func<Task> _taskToRun;
    private Task _runningTask = Task.CompletedTask;
    private TaskCompletionSource<bool>? _queuedTaskCompletionSource;
    private TaskCompletionSource<bool>? _runningTaskCompletionSource;

    private InputThrottler(Func<Task> toRun, int backOff);

    private async Task RunQueueAsync();

    public Task<bool> QueueAsync();

    public static InputThrottler Create(Func<Task> toRun, int backOff)
            => new InputThrottler(toRun, backOff > 300 ? backOff : 300);
}
``` 

I've restricted instance to a static `Create` method. You can't just "new" an instance up.  The `Func` delegate is the actual method that gets called to refresh the data.  The backoff is the minimum update backoff period.

There are two private `TaskCompletionSource` global variables that track the running and queued requests.  What is a `TaskCompletionSource`? It's an object that provides manual creation and management of Tasks.  As we go through the code you'll see how you use it.

`_runningTask` references the current `RunQueueAsync` running.  It provides a mechanism to check if the queue is currently running.

```csharp
public Task<bool> QueueAsync()
{
```
We get a reference to the currently queueed CompletionTask.  We need a reference so we can complete it once we replaced it in `_queuedTaskCompletionSource`.  It may be null. 
```csharp
    var oldCompletionTask = _queuedTaskCompletionSource;
```
Create a new CompletionTask and get it's actual Task.  Belt and braces stuff to make sure we have it referenced before we assign it to the active queue. 
```csharp
    var newCompletionTask = new TaskCompletionSource<bool>();
    var task = newCompletionTask.Task;
```
Switch out the CompletionTask assigned to the active queue.

```csharp
    // replace _queuedTaskCompletionSource
    _queuedTaskCompletionSource = newCompletionTask;
```

Set the old CompletionTask to complete.  The return `bool` informs the caller whether it needs to run a UI update.
 
```csharp
    if (oldCompletionTask is not null)
        oldCompletionTask?.TrySetResult(false);
```

Checks to see if `RunQueueAsync` is currently running.  If not start the task and assign it to `_runningTask`.

```csharp
    if (_runningTask is null || _runningTask.IsCompleted)
        _runningTask = this.RunQueueAsync();
````

Return the task associated with queued CompletionTask.

```csharp
    return task;
```  

```csharp
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
```








