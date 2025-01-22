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
using Insteon.Base;
using System.Diagnostics;

namespace Insteon.Commands;

/// <summary>
/// Base class for all Insteon Commands
/// </summary>
public abstract class Command
{
    public Command(Gateway gateway, bool isMacroCommand = false)
    {
        this.gateway = gateway;
        this.isMacroCommand = isMacroCommand;
    }

    // Gateway/hub this command uses
    private protected Gateway gateway;
    protected InsteonID HubInsteonID => gateway.DeviceId;

    // Command logging 
    private protected abstract string GetLogName();
    private protected abstract string GetLogParams();

    /// <summary>
    /// Echo the command to the log listeners
    /// </summary>
    /// <param name="attempt">Current running attempt</param>
    /// <param name="maxAttempts">Max running attempts</param>
    private protected virtual void Echo(int attempt, int maxAttempts)
    {
        Logger.Log.CommandEcho(GetLogName() + " " + GetLogParams() +
            (attempt > 1 ? " (Attempt " + attempt + " of " + maxAttempts + ")" : "") + 
            (isMacroCommand ? "Macro" : ""));
    }

    /// <summary>
    ///  Command completed execution with no error
    ///  This method can throw if the expected answer is not available
    ///  Derived classes override to check against their own expectations
    /// </summary>
    private protected virtual void Done()
    {
        Logger.Log.Debug("Command " + GetLogName() + " complete");
    }

    /// <summary>
    /// Command completed with an error
    /// Error is in ErrorReason
    /// Derived classes override to handle
    /// </summary>
    private protected virtual void Error()
    {
        if (ErrorReason != ErrorReasons.NoError)
        {
            LogOutput("Command " + GetLogName() + " returned error: " + ErrorReasonAsString);
        }
    }

    /// <summary>
    ///  Run the command asynchronously
    ///  Overriden by derived classes
    /// </summary>
    /// <returns>true if success, false if IM returned NAK</returns>
    private protected abstract Task<bool> RunAsync();

    /// <summary>
    /// Cancel command execution 
    /// </summary>
#pragma warning disable 1998
    public virtual async Task Cancel()
#pragma warning restore 1998
    {
        ErrorReason = ErrorReasons.Cancelled;
    }
    protected bool IsCancelled => ErrorReason == ErrorReasons.Cancelled;

    /// <summary>
    /// Error reason when returning false from TryRun...
    /// </summary>
    public enum ErrorReasons
    {
        NoError = 0,
        Cancelled,
        NAK,
        Timeout,
        HttpRequestError,       // Operation threw an HttpRequestException or WebException
        TransientHttpError,     // such as "unexptected end of stream on com.android.okhttp..."
        InvalidCommandType,
        NoIMResponse,
        NoDeviceResponse,
        NoDeviceStandardResponse,
        NoDeviceExtendedResponse,
        NoAllLinkRecordResponse,
        SubCommandFailed,
        Unknown,
    }
    internal ErrorReasons ErrorReason { get; private protected set; }

    /// <summary>
    /// Whether the error is recoverable by retrying the command
    /// </summary>
    /// <param name="errorReason"></param>
    /// <returns></returns>
    private static bool IsRecoverable(ErrorReasons errorReason)
    {
        return errorReason == ErrorReasons.NoDeviceResponse ||
                errorReason == ErrorReasons.NoDeviceStandardResponse ||
                errorReason == ErrorReasons.NoDeviceExtendedResponse ||
                errorReason == ErrorReasons.NoIMResponse ||
                errorReason == ErrorReasons.Timeout ||
                errorReason == ErrorReasons.TransientHttpError ||
                errorReason == ErrorReasons.NAK;    // At times the IM randomly returns NAK
    }

    /// <summary>
    /// Return a human readable form of the Error Type
    /// </summary>
    internal string ErrorReasonAsString
    {
        get
        {
            switch (ErrorReason)
            {
                case ErrorReasons.NoError:
                    return "No Error";
                case ErrorReasons.Cancelled:
                    return "Cancelled";
                case ErrorReasons.NAK:
                    return "NAK";
                case ErrorReasons.Timeout:
                    return "Timeout";
                case ErrorReasons.HttpRequestError:
                    return "Fatal Hub Response Error";
                case ErrorReasons.TransientHttpError:
                    return "Hub Response Error";
                case ErrorReasons.InvalidCommandType:
                    return "Invalid Command Type";
                case ErrorReasons.NoIMResponse:
                    return "No IM Response";
                case ErrorReasons.NoDeviceResponse:
                    return "No device response";
                case ErrorReasons.NoDeviceStandardResponse:
                    return "No standard response from the device";
                case ErrorReasons.NoDeviceExtendedResponse:
                    return "No extended response from the device";
                case ErrorReasons.SubCommandFailed:
                    return "Sub-Command Failed";
                case ErrorReasons.Unknown:
                    return "Unknown Error";
                default:
                    return "Invalid error code";
            }
        }
    }

    /// <summary>
    /// Convert an exception thrown by httpClient to an ErrorReason
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    internal ErrorReasons GetErrorReasonFromException(Exception e)
    {
        switch (e)
        {
            case HttpRequestException:
            // Thrown on Android
            case System.Net.WebException:
                if (e.Message.Contains("unexpected end of stream"))
                    return ErrorReasons.TransientHttpError;
                return ErrorReasons.HttpRequestError;

            case InsteonMessage.InvalidMessageException:
            case Insteon.Model.AllLinkRecord.InvalidAllLinkRecordException:
                Debug.Assert(false, "Commands should catch exception: " + e.Message);
                return ErrorReasons.HttpRequestError;

            default:
                if (e.Message.Contains("HttpClient.Timeout"))
                    return ErrorReasons.Timeout;
                return ErrorReasons.Unknown;
        }
    }

    /// <summary>
    /// Max number of attempts of the command by default
    /// The default is 1 (no retry), so as to return to the scheduler as soon as possible
    /// to decide whether to retry or cancel the job, potentially giving pending commands the opportunity to run.
    /// A different number can be passed to TryRunAsync.
    /// </summary>
    internal const int DefaultMaxAttempts = 3;

    // Wait time before retrying after the command returns a error that could be due to network or device instability
    private static TimeSpan waitBeforeRetryAfterError = TimeSpan.FromMilliseconds(100);

    // Semaphore to allow only one command to run at a time
    private static SemaphoreSlim commandSemaphore = new SemaphoreSlim(1, 1);

    // Currently running command (ignoring sub-commands)
    // This is the command that holds the semaphore
    public static Command? Running { get; private set; }

    // As a macro command, this command will skip acquiring the semaphore
    // but the commands it spawns in its implementation of RunAsync will acquire it.
    public bool isMacroCommand = false;

    /// <summary>
    /// Try executing the command after acquiring the semaphore ensuring only one command runs at once.
    /// - If running as a subcommand, this command is allowed to run if its parent command is running 
    ///   and holding the semaphore.
    /// - If it is a "macro" command, it is allowed to run without holding the semaphore, but should 
    ///   make no attempt to talk to the hub. Instead it should spawn other commands that will themselves 
    ///   acquire and hold the semaphore.
    /// This retries on certain errors, up to maxAttempts times, after waiting for a while.
    /// </summary>
    /// <param name="maxAttempts">max number of attempts</param>
    /// <param name="parentCommand">Run as a subcommand of that parent command</param>
    /// <returns>success, member ErrorReason contains the detail</returns>
    public async Task<bool> TryRunAsync(int maxAttempts = DefaultMaxAttempts, Command? parentCommand = null)
    {
        bool success = false;
        gateway.OnGatewayTraffic(true);

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var holdingSemaphore = false;

            // If running as a sub-command, the semaphore must be held by the parent command
            Debug.Assert(parentCommand == null || 
                (Running == parentCommand && commandSemaphore.CurrentCount == 0));

            if (!isMacroCommand && parentCommand == null)
            {
                // We allow only one command to run at a time
                await commandSemaphore.WaitAsync();
                holdingSemaphore = true;
                Running = this;
            }

            Echo(attempt, maxAttempts);
            success = await RunAsync();

            if (holdingSemaphore)
            {
                Running = null;
                commandSemaphore.Release();
            }

            if (success)
                break;

            Debug.Assert(ErrorReason != ErrorReasons.NoError, "Command.TryRunAsync: failure with no error reason");

            if (attempt == maxAttempts)
                break;

            // We retry for recoverable errors (network, IM or device instability)
            if (!IsRecoverable(ErrorReason))
                break;

            // Wait a bit and retry. Increase the wait for each attempt.
            // This seems to work with devices not responding reliably
            TimeSpan wait = waitBeforeRetryAfterError * attempt;
            Logger.Log.Debug($"Waiting {wait} before next attempt");
            await Task.Delay(wait);
        }

        gateway.OnGatewayTraffic(false);
        return success;
    }

    /// <summary>
    ///  Whether to suppress normal logging for this command
    /// </summary>
    internal bool SuppressLogging = false;

    /// <summary>
    /// Log command name
    /// </summary>
    /// <param name="msg"></param>
    public void LogCommand(string msg)
    {
        if (!SuppressLogging)
        {
            Logger.Log.CommandEcho(msg);
        }
        else
        {
            Logger.Log.Debug(msg);
        }
    }

    /// <summary>
    /// Log normal command ouput
    /// </summary>
    /// <param name="msg"></param>
    internal void LogOutput(string msg)
    {
        if (!SuppressLogging)
        {
            Logger.Log.CommandOutput(msg);
        }
        else
        {
            Logger.Log.Debug(msg);
        }
    }
}
