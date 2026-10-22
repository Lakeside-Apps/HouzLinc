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

using Common;

namespace UnitTests.Infrastructure;

/// <summary>
/// Scheduler dispatcher for unit tests: a single-threaded event loop with a simulated clock.
/// Nothing runs until the test calls RunUntilIdle or AdvanceBy, which run queued work, due timers,
/// and async continuations on the calling thread, in a deterministic order.
/// </summary>
internal sealed class TestDispatcher : ISchedulerDispatcher
{
    public TestDispatcher()
    {
        synchronizationContext = new TestSynchronizationContext(this);
    }

    /// <summary>
    /// Simulated current time. Only moves forward when the test calls AdvanceBy.
    /// </summary>
    public DateTime Now { get; private set; } = new DateTime(2026, 1, 1);

    // Everything runs on the thread pumping this dispatcher
    public bool HasThreadAccess => true;

    public void Post(Action action)
    {
        lock (queue)
        {
            queue.Enqueue(action);
        }
    }

    public IDisposable StartTimer(TimeSpan delay, Action callback)
    {
        var timer = new Timer(this, Now + delay, callback);
        timers.Add(timer);
        return timer;
    }

    /// <summary>
    /// Run queued work and timers due at the current simulated time, until nothing is left to run
    /// </summary>
    public void RunUntilIdle()
    {
        // Run async continuations on this event loop, as the UI thread does in the app
        var previousContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(synchronizationContext);
        try
        {
            while (TryRunNext()) { }
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }
    }

    /// <summary>
    /// Move the simulated clock forward, running everything that becomes due along the way, in time order
    /// </summary>
    public void AdvanceBy(TimeSpan duration)
    {
        var endTime = Now + duration;
        RunUntilIdle();
        while (NextTimerDueBy(endTime) is Timer timer)
        {
            Now = timer.DueTime;
            RunUntilIdle();
        }
        Now = endTime;
        RunUntilIdle();
    }

    // Run the next queued action, or else the next due timer. Returns false if there was nothing to run.
    private bool TryRunNext()
    {
        Action? action = null;
        lock (queue)
        {
            if (queue.Count > 0)
            {
                action = queue.Dequeue();
            }
        }

        if (action != null)
        {
            action();
            return true;
        }

        if (NextTimerDueBy(Now) is Timer timer)
        {
            timers.Remove(timer);
            timer.Callback();
            return true;
        }

        return false;
    }

    private Timer? NextTimerDueBy(DateTime time)
    {
        return timers.Where(t => t.DueTime <= time).OrderBy(t => t.DueTime).FirstOrDefault();
    }

    private sealed class Timer(TestDispatcher dispatcher, DateTime dueTime, Action callback) : IDisposable
    {
        public DateTime DueTime => dueTime;
        public Action Callback => callback;
        public void Dispose() => dispatcher.timers.Remove(this);
    }

    // Posts async continuations to the dispatcher queue
    private sealed class TestSynchronizationContext(TestDispatcher dispatcher) : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state) => dispatcher.Post(() => d(state));
        public override SynchronizationContext CreateCopy() => this;
    }

    private readonly Queue<Action> queue = new();
    private readonly List<Timer> timers = new();
    private readonly TestSynchronizationContext synchronizationContext;
}
