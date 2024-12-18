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
using ViewModel.Base;
using ViewModel.Settings;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;

namespace ViewModel.Console;

/// <summary>
///  Backs the console UI. This class implements the logic for a command input box and a log
/// 
///  The Log gets populated by event written to the "AppLogger" event source 
///  and is available as:
///   LogText - console log, read only, bindable oneway
/// 
///  The command box is managed by the following members:
///   CurrentCommand - text to show in the command line, read only, bindable oneway
///   ProcessCommandAsync - call to process new command and remember in the history
///   NextCommand - make CurrentCommand the next command from the command history 
///   PreviousCommand - make CurrentCommand the previous command from the command history
/// 
///  A command processor can be set to be called by ProcessCommand:
///   CommandProcessor - read write 
/// </summary>
[Bindable(true)]
public partial class ConsoleViewModel : PageViewModel
{
    private ConsoleViewModel()
    {
        _commandHistory = new List<string>();
        SettingsStore.ReadLastUsedStringList("CommandHistory", _commandHistory);
        _commandIndex = _commandHistory.Count;
    }
    public static ConsoleViewModel Instance => instance ??= new ConsoleViewModel();
    private static ConsoleViewModel? instance;

    /// <summary>
    ///  Used by ProcessCommandAsync to process commands
    ///  No command processing occurs if null
    /// </summary>
    public ICommandProcessor? CommandProcessor { get; set; }

    /// <summary>
    ///  Asynchronously process a command string
    /// </summary>
    /// <param name="command">Command to process</param>
    /// <returns>Task to await on</returns>
    public async Task ProcessCommandAsync(string command)
    {
        if (command == string.Empty)
        {
            return;
        }

        AddCommandToHistory(command);

        // Check for commands for the ConsoleViewModel first
        string[] tokens = command.Split(' ');
        if (tokens[0].Equals("Output", StringComparison.OrdinalIgnoreCase))
        {
            ProcessOutputCommand(tokens);;
        }
        else if (tokens[0].Equals("Clear", StringComparison.OrdinalIgnoreCase))
        {
            ProcessClearCommand(tokens);
        }
        // otherwise delegate to the command processor
        else if (CommandProcessor != null)
        {
            await CommandProcessor.ProcessAsync(command);
        }
    }

    /// <summary>
    ///  Command targetted at the ConsoleViewModel itself
    ///  Change output verbosity level
    /// </summary>
    /// <param name="tokens">Command, broken in tokens</param>
    public void ProcessOutputCommand(string[] tokens)
    {
        Logger.Log.CommandEcho("Ouput");
        if (tokens.Count() == 1)
        {
            Logger.Log.CommandOutput($"Output level: {OutputLevel.ToString()}");
            Logger.Log.CommandError("To set output level:");
            Logger.Log.CommandError("Ouput debug|verbose|info|warning|error|critical");
        }
        else
        {
            if (tokens[1].Equals("critical", StringComparison.OrdinalIgnoreCase))
            {
                OutputLevel = LogLevel.Critical;
            }
            else if (tokens[1].Equals("error", StringComparison.OrdinalIgnoreCase))
            {
                OutputLevel = LogLevel.Error;
            }
            else if (tokens[1].Equals("warning", StringComparison.OrdinalIgnoreCase))
            {   
                OutputLevel = LogLevel.Warning;
            }
            else if (tokens[1].Equals("info", StringComparison.OrdinalIgnoreCase))
            {
                OutputLevel = LogLevel.Information;
            }
            else if (tokens[1].Equals("informational", StringComparison.OrdinalIgnoreCase))
            {
                OutputLevel = LogLevel.Information;
            }
            else if (tokens[1].Equals("verbose", StringComparison.OrdinalIgnoreCase))
            {
                OutputLevel = LogLevel.Debug;
            }
            else if (tokens[1].Equals("debug", StringComparison.OrdinalIgnoreCase))
            {
                OutputLevel = LogLevel.Debug;
            }
            else
            {
                Logger.Log.CommandError("Ouput debug|verbose|info|warning|error|critical");
                return;
            }
            Logger.Log.CommandOutput($"Output level set to {OutputLevel}");
        }
    }

    /// <summary>
    ///  Output verbosity level 
    ///  Notifies the event listener when changed
    /// </summary>
    private LogLevel _outputLevel = LogLevel.Information;
    public LogLevel OutputLevel 
    {
        get
        {
            return _outputLevel;
        }
        set
        {
            _outputLevel = value;
            if (ConsoleLogger.Instance != null)
            {
                ConsoleLogger.Instance.SetLogLevel(value);
            }
        }
    }

    /// <summary>
    ///  Clears the log
    /// </summary>
    public void ProcessClearCommand(string[] tokens)
    {
        Logger.Log.CommandEcho("Clear");
        if (tokens.Count() == 1)
        {
            LogItems.Clear();
        }
        else
        {
            Logger.Log.CommandError("Clear");
        }
    }

    /// <summary>
    ///  Private, only called by ConsoleEventListener. Write an event to the log
    /// </summary>
    /// <param name="eventKind">Event kind (see Logger.cs)</param>
    /// <param name="message">Message to show in the log</param>
    internal void WriteEventToLog(LogEventId eventKind, string message)
    {
        switch (eventKind)
        {
            case (LogEventId.CommandEcho):
                LogItems.Add($">{message}");
                break;

            case (LogEventId.CommandOutput):
            case (LogEventId.CommandError):
                // Properly indent a multiline message
                message = message.Replace("\n", "\n   ");
                message = message.TrimEnd(['\r','\n',' ']);
                LogItems.Add($"   {message}");
                break;

            case (LogEventId.Debug):
                // Properly indent a multiline message
                message = message.Replace("\n", "\n      ");
                message = message.TrimEnd(['\r', '\n', ' ']);
                LogItems.Add($"      {message}");
                break;

            default:
                LogItems.Add(message);
                break;
        }
    }

    /// <summary>
    /// Observable collection of log items. Read only, oneway bindable to the UI
    /// </summary>
    public ObservableCollection<string> LogItems = new ObservableCollection<string>();

    /// <summary>
    ///  Current command line. Read Only, oneway bindable to the UI
    ///  Set when a command is executed
    /// </summary>
    public string CurrentCommand
    {
        get
        {
            if (_commandIndex < _commandHistory.Count)
            {
                return _commandHistory[_commandIndex];
            }
            else
            {
                return "";
            }
        }
    }

    // Add the passed command in the command history if necessary
    private void AddCommandToHistory(string command)
    {
        bool addCommand = false;

        if (_commandHistory.Count == 0)
        {
            // Add passed command if history is empty
            addCommand = true;
        }
        else if (_commandIndex == _commandHistory.Count)
        {
            // Add it to top of history if past top and different from current top
            if (!command.Equals(_commandHistory[_commandIndex - 1], StringComparison.Ordinal))
            {
                addCommand = true;
            }
        }
        else if (!command.Equals(_commandHistory[_commandIndex], StringComparison.Ordinal))
        {
            // Add it to top of history if different from current
            addCommand = true;
        }

        if (addCommand)
        {
            _commandHistory.Add(command);
            SettingsStore.WriteLastUsedStringList("CommandHistory", _commandHistory, maxItems: 50);
            _commandIndex = _commandHistory.Count;
            OnCommandChanged();
        }
    }

    /// <summary>
    /// Caret position in the current command line
    /// </summary>
    /// <returns></returns>
    public int CurrentSelectionStart
    {
        get
        {
            if (_commandIndex < _commandHistory.Count)
            {
                return _commandHistory[_commandIndex].Length;
            }
            else
            {
                return 0;
            }
        }
    }

    /// <summary>
    ///  Brings next command in CurrentCommand
    /// </summary>
    public void NextCommand()
    {
        if (_commandIndex < _commandHistory.Count)
        {
            _commandIndex++;
            OnCommandChanged();
        }
    }

    /// <summary>
    ///  Brings previous command in CurrentCommand
    /// </summary>
    public void PreviousCommand()
    {
        if (_commandIndex > 0)
        {
            _commandIndex--;
            OnCommandChanged();
        }
    }

    private void OnCommandChanged()
    {
        OnPropertyChanged("CurrentCommand");
        OnPropertyChanged("CurrentSelectionStart");
    }

    List<string> _commandHistory;
    int _commandIndex;
}
