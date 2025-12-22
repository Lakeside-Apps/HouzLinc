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
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;

namespace ViewModel.Settings;

/// <summary>
///  This class displays job related log events on the status bar.
///  It ensures that events stay long enough on the status bar to be readable
///  while still keeping up with incoming events.
///  It can display a UserActionRequested message until a UserActionCleared event is received.
/// </summary>
public sealed class StatusTextLogger : ILogger
{
    public delegate void UpdateStatusText(string text, bool isUserActionRequest);
    public static UpdateStatusText? UpdateStatusTextCallback
    {
        get => updateStatusTextCallback;
        set
        {
            // New listener: show the current event
            if (userActionRequestEvent != null)
            {
                shownEvent = userActionRequestEvent;
            }
            updateStatusTextCallback = value;
            UpdateStatusTextCallback?.Invoke(
                text: shownEvent?.Text ?? string.Empty,
                isUserActionRequest: (shownEvent?.EventId.Id ?? 0) == (int)LogEventId.UserActionRequested);
        }
    }
    private static UpdateStatusText? updateStatusTextCallback;

    public StatusTextLogger()
    {
        messageTimer = new System.Timers.Timer(tickPeriod.TotalMilliseconds);
        messageTimer.Elapsed += (s, e) => TimerTick(s, EventArgs.Empty);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (Logger.Log.GetLogEventClass(eventId.Id) == LogEventClass.Job ||
            Logger.Log.GetLogEventClass(eventId.Id) == LogEventClass.UserAction)
        {
            if (!messageTimer.Enabled)
            {
                tickCount = 0;
                messageTimer.Start();
            }

            pendingEvents.Enqueue(new LogEvent(eventId, formatter(state, null)));
        }
    }

    private void TimerTick(object? sender, object e)
    {
        tickCount++;

        if (pendingEvents.Count > 0)
        {
            if (pendingEvents.Peek().EventId.Id == (int)LogEventId.UserActionCleared)
            {
                // Pending event is a clear user action request, in response of user clicking "Go" or "Snooze" button.
                // Don't wait on timer, clear immediately to be responsive to user.
                ShowEvent(pendingEvents.Dequeue());
                tickCount = 0;
            }
            else if (shownEvent == null || tickCount >= TickCountToShowPendingMessage)
            {
                // We have pending event but either no event showing yet or the count to show pending event has been reached.
                // Show next pending event unless we are currently showing an error event and now getting a job failure event,
                // which might be less descriptive than the error event
                if (pendingEvents.Peek().EventId.Id == (int)LogEventId.JobFailure && shownEvent != null && shownEvent.EventId.Id == (int)LogEventId.Error)
                {
                    pendingEvents.Dequeue();
                }
                else
                {
                    ShowEvent(pendingEvents.Dequeue());
                }
                tickCount = 0;
            }
        }
        else
        {
            if (tickCount == TickCountToHideMessage)
            {
                // Count to hide event reached: remove the message if it is a completion or failure event
                // Other events will stay up until next event comes in
                if (shownEvent != null &&
                    (shownEvent.EventId.Id == (int)LogEventId.JobCompletion ||
                     shownEvent.EventId.Id == (int)LogEventId.JobFailure ||
                     shownEvent.EventId.Id == (int)LogEventId.UserActionCleared))
                {
                    ShowEvent(null);
                    messageTimer.Stop();
                }
                tickCount = 0;
            }
        }
    }

    private void ShowEvent(LogEvent? eventToShow)
    {
        shownEvent = eventToShow;

        if (shownEvent != null && shownEvent.EventId.Id == (int)LogEventId.UserActionCleared)
        {
            userActionRequestEvent = null;
        }

        if (userActionRequestEvent == null)
        {
            var isUserActionRequested = (shownEvent?.EventId.Id ?? 0) == (int)LogEventId.UserActionRequested;

            if (UpdateStatusTextCallback is { } cb)
            {
                dispatcherQueue.TryEnqueue(() => cb(shownEvent?.Text ?? string.Empty, isUserActionRequested));
            }
        }

        if (shownEvent != null && shownEvent.EventId.Id == (int)LogEventId.UserActionRequested)
        {
            userActionRequestEvent = shownEvent;
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        throw new NotImplementedException();
    }

    private class LogEvent
    {
        public LogEvent(EventId eventId, string text)
        {
            EventId = eventId;
            Text = text;
        }

        public EventId EventId { get; private set; }
        public string Text { get; private set; }
    }

    // First message in this is shown, other are pending to be shown
    private static LogEvent? shownEvent;
    private static LogEvent? userActionRequestEvent = default;
    private Queue<LogEvent> pendingEvents = new Queue<LogEvent>();
    private readonly System.Timers.Timer messageTimer;
    private int tickCount;
    private readonly TimeSpan tickPeriod = TimeSpan.FromMilliseconds(100);
    private const int TickCountToHideMessage = 20; // 2s - Time to take down shown message
    private const int TickCountToShowPendingMessage = 1; // .1s - Time to show pending message
    private readonly DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();
}

public sealed class StatusTextLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new StatusTextLogger();
    }

    public void Dispose()
    {
    }
}
