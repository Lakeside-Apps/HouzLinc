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

namespace UnitTests.Infrastructure;

/// <summary>
/// Captures the app log (Common.Logger) during tests, so failing tests can show what happened
/// </summary>
internal sealed class TestLog : ILoggerFactory, ILogger
{
    public static TestLog Instance { get; } = new();

    /// <summary>
    /// Log lines captured since the last Clear
    /// </summary>
    public List<string> Lines { get; } = new();

    public void Clear() => Lines.Clear();

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        lock (Lines)
        {
            Lines.Add($"{logLevel}: {formatter(state, exception)}");
        }
    }

    public bool IsEnabled(LogLevel logLevel) => true;
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public ILogger CreateLogger(string categoryName) => this;
    public void AddProvider(ILoggerProvider provider) { }
    public void Dispose() { }
}
