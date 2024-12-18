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

using System.Diagnostics.Tracing;
using Microsoft.Extensions.Logging;


namespace Common;

// Classes of log events to facilitate filtering in our ILogger implementations.
public enum LogEventClass
{
    General,
    Job,
    Command,
    UserAction
}

// Event ids.
// Please keep in sync with logLevelFromLogEventId and logEventClassFromLogEventId.
public enum LogEventId
{
    Error = 1,
    Warning,
    Info,
    Debug,
    JobRunning,
    JobFailure,
    JobCompletion,
    CommandEcho,
    CommandOutput,
    CommandError,
    UserActionRequested,
    UserActionCleared
}

// Common.Logger class to handle existing logging code.
public partial class Logger
{
    // Singleton instance of Common.logger.
    public static Logger Log => log ??= new Logger("HouzLinc");
    private static Logger? log;

    // ILogger created by the ambient logger factory in the constructor below
    // The Log function of this ILogger will in turn call Log(..) on all ILoggers
    // registered via an ILoggerProvider (see App.xaml.cs).
    private ILogger logger;

    public Logger(string name)
    {
        ILoggerFactory factory = global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory;
        logger = factory.CreateLogger(name);
    }

    // LogLevel assigned to the various log events
    private static readonly LogLevel[] logLevelFromLogEventId = new LogLevel[]
    {
        LogLevel.None,
        LogLevel.Error,         // Error
        LogLevel.Warning,       // Warning
        LogLevel.Information,   // Info
        LogLevel.Debug,         // Debug
        LogLevel.Information,   // JobRunning
        LogLevel.Error,         // JobFailure
        LogLevel.Information,   // JobCompletion
        LogLevel.Information,   // CommandEcho
        LogLevel.Information,   // CommandOuput
        LogLevel.Error,         // CommandError
        LogLevel.Information,   // UserActionRequested
        LogLevel.Information,   // UserActionCleared
    };

    // Class (grouping) of log events to facilitate filtering in the ilogger implementations
    private static readonly LogEventClass[] logEventClassFromLogEventId = new LogEventClass[]
    {
        LogEventClass.General,
        LogEventClass.General,  // Error
        LogEventClass.General,  // Warning
        LogEventClass.General,  // Info
        LogEventClass.General,  // Debug
        LogEventClass.Job,      // JobRunning
        LogEventClass.Job,      // JobFailure
        LogEventClass.Job,      // JobCompletion
        LogEventClass.Command,  // CommandEcho
        LogEventClass.Command,  // CommandOutput
        LogEventClass.Command,  // CommandError
        LogEventClass.UserAction,  // UserActionRequested
        LogEventClass.UserAction   // UserActionCleared
    };

    public LogEventClass GetLogEventClass(int logEventId)
    {
        return logEventClassFromLogEventId[logEventId];
    }

    [LoggerMessage(EventId = (int)LogEventId.Error, Message = "{message}", Level = LogLevel.Error)]
    static partial void Error(ILogger logger, string message);
    public void Error(string message)
    {
        Error(logger, message);
    }

    [LoggerMessage(EventId = (int)LogEventId.Warning, Message = "{message}", Level = LogLevel.Warning)]
    static partial void Warning(ILogger logger, string message);
    public void Warning(string message)
    {
        Warning(logger, message);
    }

    [LoggerMessage(EventId = (int)LogEventId.Info, Message = "{message}", Level = LogLevel.Information)]
    static partial void Info(ILogger logger, string message);
    public void Info(string message)
    {
        Info(logger, message);
    }

    [LoggerMessage(EventId = (int)LogEventId.Debug, Message = "{message}", Level = LogLevel.Debug)]
    static partial void Debug(ILogger logger, string message);
    public void Debug(string message)
    {
        Debug(logger, message);
    }

    // Scheduler logging

    [LoggerMessage(EventId = (int)LogEventId.JobRunning, Message = "{message}", Level = LogLevel.Information)]
    static partial void Running(ILogger logger, string message);
    public void Running(string message)
    {
        Running(logger, message);
    }

    [LoggerMessage(EventId = (int)LogEventId.JobCompletion, Message = "{message}", Level = LogLevel.Information)]
    static partial void Completed(ILogger logger, string message);
    public void Completed(string message)
    {
        Completed(logger, message);
    }

    [LoggerMessage(EventId = (int)LogEventId.JobFailure, Message = "{message}", Level = LogLevel.Error)]
    static partial void Failed(ILogger logger, string message);
    public void Failed(string message)
    {
        Failed(logger, message);
    }

    // Command logging

    [LoggerMessage(EventId = (int)LogEventId.CommandEcho, Message = "{message}", Level = LogLevel.Information)]
    static partial void CommandEcho(ILogger logger, string message);
    public void CommandEcho(string message)
    {
        CommandEcho(logger, message);
    }

    [LoggerMessage(EventId = (int)LogEventId.CommandOutput, Message = "{message}", Level = LogLevel.Information)]
    static partial void CommandOutput(ILogger logger, string message);
    public void CommandOutput(string message)
    {
        CommandOutput(logger, message);
    }

    [LoggerMessage(EventId = (int)LogEventId.CommandError, Message = "{message}", Level = LogLevel.Error)]
    static partial void CommandError(ILogger logger, string message);
    public void CommandError(string message)
    {
        CommandError(logger, message);
    }

    // User action requests
    // Using the logging system to request an action by the user

    [LoggerMessage(EventId = (int)LogEventId.UserActionRequested, Message = "{message}", Level = LogLevel.Information)]
    static partial void RequestUserAction(ILogger logger, string message);
    public void RequestUserAction(string message)
    {
        RequestUserAction(logger, message);
    }

    [LoggerMessage(EventId = (int)LogEventId.UserActionCleared, Message = "{message}", Level = LogLevel.Information)]
    static partial void ClearUserAction(ILogger logger, string message);
    public void ClearUserAction(string message)
    {
        ClearUserAction(logger, message);
    }

    public bool IsEnabled(LogEventId logEventId)
    {
        return logger.IsEnabled(logLevelFromLogEventId[(int)logEventId]);
    }
}
