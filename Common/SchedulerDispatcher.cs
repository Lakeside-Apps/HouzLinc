/* Copyright 2022 Christian Fortini

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using Microsoft.UI.Dispatching;

namespace Common;

/// <summary>
/// The thread (event loop) and clock a Scheduler runs on.
/// In the app, this is the UI thread's DispatcherQueue and the system clock (see DispatcherQueueSchedulerDispatcher).
/// Unit tests substitute their own implementation to run jobs deterministically on a simulated clock.
/// </summary>
public interface ISchedulerDispatcher
{
    /// <summary>
    /// Current time, used to compute when jobs are ready to run
    /// </summary>
    DateTime Now { get; }

    /// <summary>
    /// Whether the caller is running on the scheduler thread
    /// </summary>
    bool HasThreadAccess { get; }

    /// <summary>
    /// Queue an action to run on the scheduler thread
    /// </summary>
    void Post(Action action);

    /// <summary>
    /// Call a callback on the scheduler thread once a delay has elapsed
    /// </summary>
    /// <returns>Dispose to stop the timer before it fires</returns>
    IDisposable StartTimer(TimeSpan delay, Action callback);
}

/// <summary>
/// Scheduler dispatcher running on the DispatcherQueue of the thread the scheduler was created on (UI thread)
/// </summary>
public sealed class DispatcherQueueSchedulerDispatcher : ISchedulerDispatcher
{
    public DateTime Now => DateTime.Now;

    public bool HasThreadAccess => Environment.CurrentManagedThreadId == threadId;

    public void Post(Action action)
    {
        DispatcherQueue.GetForCurrentThread().TryEnqueue(() => action());
    }

    public IDisposable StartTimer(TimeSpan delay, Action callback)
    {
        var timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        timer.Tick += (sender, e) => callback();
        timer.Interval = delay;
        timer.Start();
        return new TimerHandle(timer);
    }

    private sealed class TimerHandle(DispatcherQueueTimer timer) : IDisposable
    {
        public void Dispose() => timer.Stop();
    }

    // Id of the thread the scheduler was created on
    private readonly int threadId = Environment.CurrentManagedThreadId;
}
