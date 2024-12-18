// Kept only as reference, currently unused, we use Microsoft.Extensions.Logging.Debug instead.
// Based on https://github.com/unoplatform/uno.extensions.logging/blob/master/src/Uno.Extensions.Logging.WebAssembly.Console/WebAssemblyConsoleLogger.cs

using System.Diagnostics;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Common;

public sealed class DebugLogger : ILogger
{
    static DebugLogger()
    {
        var logLevelString = GetLogLevelString(LogLevel.Information);
        _messagePadding = new string(' ', logLevelString.Length + _loglevelPadding.Length);
        _newLineWithMessagePadding = Environment.NewLine + _messagePadding;
    }

    public DebugLogger()
        : this(string.Empty)
    {
    }

    public DebugLogger(string name)
    {
        this.name = name;
    }

    private static readonly string _loglevelPadding = ": ";
    private static readonly string _messagePadding;
    private static readonly string _newLineWithMessagePadding;
    private static readonly StringBuilder _logBuilder = new(); 

    private readonly string name;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        throw new NotImplementedException();
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = $"---> {formatter(state, null)}";
        WriteMessage(logLevel, name, eventId.Id, message, exception);
    }

    private void WriteMessage(LogLevel logLevel, string logName, int eventId, string message, Exception? exception)
    {
        lock (_logBuilder)
        {
            try
            {
                CreateDefaultLogMessage(_logBuilder, logLevel, logName, eventId, message, exception);
                var formattedMessage = _logBuilder.ToString();
                Debug.WriteLine(formattedMessage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to log \"{message}\": {ex}");
            }
            finally
            {
                _logBuilder.Clear();
            }
        }
    }

    private void CreateDefaultLogMessage(StringBuilder logBuilder, LogLevel logLevel, string logName, int eventId, string message, Exception? exception)
    {
        logBuilder.Append(GetLogLevelString(logLevel));
        logBuilder.Append(_loglevelPadding);
        logBuilder.Append(logName);
        logBuilder.Append("[");
        logBuilder.Append(eventId);
        logBuilder.Append("]");

        if (!string.IsNullOrEmpty(message))
        {
            // message
            logBuilder.AppendLine();
            logBuilder.Append(_messagePadding);

            var len = logBuilder.Length;
            logBuilder.Append(message);
            logBuilder.Replace(Environment.NewLine, _newLineWithMessagePadding, len, message.Length);
        }

        // Example:
        // System.InvalidOperationException
        //    at Namespace.Class.Function() in File:line X
        if (exception != null)
        {
            // exception message
            logBuilder.AppendLine();
            logBuilder.Append(exception.ToString());
        }
    }

    private static string GetLogLevelString(LogLevel logLevel)
    {
        switch (logLevel)
        {
            case LogLevel.Trace:
                return "trce";
            case LogLevel.Debug:
                return "dbug";
            case LogLevel.Information:
                return "info";
            case LogLevel.Warning:
                return "warn";
            case LogLevel.Error:
                return "fail";
            case LogLevel.Critical:
                return "crit";
            default:
                throw new ArgumentOutOfRangeException(nameof(logLevel));
        }
    }
}

public sealed class DebugLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new DebugLogger(categoryName);
    }

    public void Dispose()
    {
    }
}
