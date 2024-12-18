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

using Microsoft.Extensions.Logging;
using Common;

namespace ViewModel.Console;

public sealed class ConsoleLogger : ILogger
{
    public static ConsoleLogger? Instance { get; private set; }

    public ConsoleLogger()
	{
        Instance = this;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        throw new NotImplementedException();
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= this.logLevel;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel) &&
            (logLevel <= LogLevel.Information ||
            eventId.Id == (int)LogEventId.CommandEcho||
            eventId.Id == (int)LogEventId.CommandOutput||
            eventId.Id == (int)LogEventId.CommandError))
        {
            ConsoleViewModel.Instance.WriteEventToLog((Common.LogEventId)eventId.Id, formatter(state, null));
        }
    }

    public void SetLogLevel(LogLevel logLevel)
    {
        this.logLevel = logLevel;
    }

    LogLevel logLevel = LogLevel.Information;
}

public sealed class ConsoleLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new ConsoleLogger();
    }

    public void Dispose()
    {
    }
}
