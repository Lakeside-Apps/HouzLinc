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

using System.Diagnostics;
using Common;
using Insteon.Base;
using Insteon.Model;
using Insteon.Mock;

namespace Insteon.Commands;

/// <summary>
///  Base class for command sent to the Hub 
///  Either for the IM or a specific Insteon physical device
/// </summary>
public abstract class HubCommand : Command
{
    /// <summary>
    /// IM command codes
    /// </summary>
    protected const byte IMCommandCode_StandardMessageReceived = 0x50;
    protected const byte IMCommandCode_ExtendedMessageReceived = 0x51;
    protected const byte IMCommandCode_X10Received = 0x52;
    protected const byte IMCommandCode_AllLinkCompleted = 0x53;
    protected const byte IMCommandCode_ButtonEventReport = 0x54;
    protected const byte IMCommandCode_UserResetDetected = 0x55;
    protected const byte IMCommandCode_AllLinkFailureReport = 0x56;
    protected const byte IMCommandCode_AllLinkRecordResponse = 0x57;
    protected const byte IMCommandCode_AllLinkCleanupStatusReport = 0x58;
    protected const byte IMCommandCode_DataBaseRecordFound = 0x58;
    protected const byte IMCommandCode_NoDeviceStandardMessageReceived = 0x5C;   // Sent by the IM when the device is not found

    protected const byte IMCommandCode_GetIMInfo = 0x60;
    protected const byte IMCommandCode_SendAllLink = 0x61;
    protected const byte IMCommandCode_SendInsteonMessage = 0x62;
    protected const byte IMCommandCode_SendX10 = 0x63;
    protected const byte IMCommandCode_StartAllLinking = 0x64;
    protected const byte IMCommandCode_CancelAllLinking = 0x65;
    protected const byte IMCommandCode_SetHostDeviceCategory = 0x66;
    protected const byte IMCommandCode_ResetIM = 0x67;
    protected const byte IMCommandCode_SetInsteonACKMessageByte = 0x68;
    protected const byte IMCommandCode_GetFirstAllLinkRecord = 0x69;
    protected const byte IMCommandCode_GetNextAllLinkRecord = 0x6A;
    protected const byte IMCommandCode_SetImConfiguration = 0x6B;
    protected const byte IMCommandCode_GetAllLinkRecordForSender = 0x6C;
    protected const byte IMCommandCode_LEDOn = 0x6D;
    protected const byte IMCommandCode_LEDOff = 0x6E;
    protected const byte IMCommandCode_ManageAllLinkRecord = 0x6F;
    protected const byte IMCommandCode_SetInsteonNAKMessageByte = 0x70;
    protected const byte IMCommandCode_SetInsteonNAKMessageTwoByte = 0x71;
    protected const byte IMCommandCode_RFSleep = 0x72;
    protected const byte IMCommandCode_GetImConfiguration = 0x73;
    protected const byte IMCommandCode_CancelCleanup = 0x74;
    protected const byte IMCommandCode_Read8BytesFromDatabase = 0x75;
    protected const byte IMCommandCode_Write8BytesToDatabase = 0x76;
    protected const byte IMCommandCode_Beep = 0x77;
    protected const byte IMCommandCode_SetStatus = 0x79;
    protected const byte IMCommandCode_SetDatabaseLinkDataForNextLink = 0x79;
    protected const byte IMCommandCode_SetApplicationRetriesToNewLinks = 0x7A;
    protected const byte IMCommandCode_SetRFFrequencyOffset = 0x7B;
    protected const byte IMCommandCode_SetAcknowledgeForTempLinc = 0x7C;

    protected const byte IMCommandCode_NAK = 0x15;
    protected const byte IMCommandCode_ACK = 0x06;

    /// <summary>
    /// Constructor for commands that need a custom gateway/hub (e.g., GetIMInfo on new hub)
    /// </summary>
    /// <param name="gateway"></param>
    internal HubCommand(Gateway gateway, bool isMacroCommand = false) : base(gateway, isMacroCommand)
    {
    }

    /// <summary>
    ///  To be set by derived classes before calling RunAsync
    /// </summary>
    protected enum IMCommandTypes { Short, Hub, HubConfig, IM };
    protected IMCommandTypes IMCommandType;
    protected byte IMCommandCode;
    protected string IMCommandParams = string.Empty;
    // Expected length of the answer by the IM  (not including 0x02 <command> header, nor ACK)
    protected int IMResponseLength;

    // Default timeout after which the command will fail (and possibly retry) if expected response not received
    protected TimeSpan ResponseTimeout = TimeSpan.FromSeconds(5);

    // Minimum time interval between two commands
    private static readonly TimeSpan MinTimeBetweenCommands = TimeSpan.FromMilliseconds(50);

    // Whether to clear the stream buffer before reading from it again
    protected bool RequestClearBuffer = false;

    /// <summary>
    /// Exceptions that might be thrown during command execution
    /// </summary>
    internal class HubCommandNAKException : Exception
    {
        internal HubCommandNAKException(string msg) : base(msg) { }
    }

    /// <summary>
    /// Cancel command
    /// </summary>
#pragma warning disable 1998
    public override async Task Cancel()
#pragma warning restore 1998
    {
        SetComplete(ErrorReasons.Cancelled);
    }

    /// <summary>
    ///  Send the command and wait for response from the hub/IM
    ///  IMResponse contains the Hub response.
    ///  Derived classes which override WaitForResponse might expect additional response
    /// </summary>
    /// <returns>success, member ErrorReason contains the details</returns>
    private protected override async Task<bool> RunAsync()
    {
        Debug.Assert(MockPhysicalIM == null, "HubCommand.RunAsync() should not be called for testing");

        IsComplete = false;

        // Ensure previous command finished more than MinTimeBetweenCommands ago
        TimeSpan timeInterval = (DateTime.Now - LastCommandCompleteTime);
        if (timeInterval < MinTimeBetweenCommands)
        {
            Logger.Log.Debug("Waiting " + (MinTimeBetweenCommands - timeInterval).TotalMilliseconds + "ms before sending next command");
            await Task.Delay(MinTimeBetweenCommands - timeInterval);
        }

        // The hub clears the buffer for every command
        ResponseStream.Reset();

        // Send the request
        var success = await SendCommandAsync();

        if (success)
        {
            // Process response
            success = await ProcessResponseAsync();
        }

        LastCommandCompleteTime = DateTime.Now;
        return success;
    }

    // Time we last completed a command
    static protected DateTime LastCommandCompleteTime { get; private set; }

    // Return inner command string container in request string
    private string CommandString
    {
        get
        {
            string commandString;
            switch (IMCommandType)
            {
                case IMCommandTypes.Short:
                    {
                        // Short form command: <command><group> ("/0?CCGN=I=0")
                        // where CC is command code, GN group number (00 to FF), e.g.,
                        // /0?08=I=0 stops linking on the PLM. This can also be done with the generic PLM command resource.
                        // /0?09GN=I=0 starts linking for a group number, GN. The group number can be 00 to FF.
                        // /0?0AGN=I=0 starts unlinking for a group number, GN. The group number can be 00 to FF.
                        // Note: none of these "/0" commands are currently implemented
                        commandString = (IMCommandCode > 0) ? IMCommandCode.ToString("x2") : "" + IMCommandParams;
                        break;
                    }
                case IMCommandTypes.Hub:
                    {
                        // Commands for the Hub (e.g., Clear: "/1?XB=M=1", IMCommandParam)
                        commandString = IMCommandParams;
                        break;
                    }
                case IMCommandTypes.HubConfig:
                    {
                        // Commands for the Hub channel configuration, e.g.,
                        // "/2?S215=Hall_Lights=2=t=00:00=ff=00:00=ff=f=f=f=f=f=f=f=f=f=f=t=AABBCC01=t=f22" Creates a new schedule.
                        commandString = IMCommandParams;
                        break;
                    }
                case IMCommandTypes.IM:
                    {
                        // Commands for the IM, a.k.a., PLM. Some targets a specific device, e.g., "/3?02AABBCCxxxx=I=3"
                        commandString = (IMCommandCode > 0) ? "02" + IMCommandCode.ToString("x2") : "" + IMCommandParams;
                        break;
                    }
                default:
                    // Litteral mode for debugging, send IMCommandParams verbatim
                    commandString = IMCommandParams;
                    break;
            }

            return commandString;
        }
    }

    /// <summary>
    ///  Construct the command string and send the command to the Hub
    /// </summary>
    /// <returns>success, member ErrorReason contains the detail</returns>
    private async Task<bool> SendCommandAsync()
    {
        // Assemble the request string to send to the hub
        string requestString = string.Empty;
        requestString = GetCommandTypeToken() + "?" + CommandString;
        switch (IMCommandType)
        {
            case IMCommandTypes.Short:
            case IMCommandTypes.IM:
                requestString += "=I=" + GetCommandTypeToken();
                break;

            case IMCommandTypes.Hub:
                requestString += "=M=" + GetCommandTypeToken();
                break;

            case IMCommandTypes.HubConfig:
            default:
                break;
        }
        Logger.Log.Debug("Sending command (" + GetLogName() + "): " + requestString);

        HttpResponseMessage httpResponse;
        try
        {
            httpResponse = await HttpClient.GetAsync(requestString);
        }
        catch (Exception e)
        {
            ErrorReason = GetErrorReasonFromException(e);
            Debug.Assert(ErrorReason != ErrorReasons.NoError);
            Logger.Log.Error($"{ErrorReasonAsString} ({e.Message})");
            return false;
        }

        if (httpResponse.IsSuccessStatusCode)
        {
            return true;
        }
        else
        {
            ErrorReason = ErrorReasons.HttpRequestError;
            Logger.Log.Error($"{ErrorReasonAsString} - Http response code from {HttpClient.BaseAddress}: {httpResponse.StatusCode}");
            return false;
        }
    }

    /// <summary>
    ///  Private helper to generate the proper command type flag
    /// </summary>
    /// <returns></returns>
    private string GetCommandTypeToken()
    {
        switch (IMCommandType)
        {
            case IMCommandTypes.Short:
                return "0";
            case IMCommandTypes.Hub:
                return "1";
            case IMCommandTypes.HubConfig:
                return "2";
            case IMCommandTypes.IM:
                return "3";
        }
        ErrorReason = ErrorReasons.InvalidCommandType;
        throw new System.ArgumentException("Invalid command type");
    }

    /// <summary>
    /// Called to indicate that this command has read all expected response
    /// </summary>
    /// <param name="errorReason">optional error reason</param>
    private protected void SetComplete(ErrorReasons errorReason = ErrorReasons.NoError)
    {
        ErrorReason = errorReason;
        IsComplete = true;
    }
    private bool IsComplete;

    /// <summary>
    /// Process the next response message 
    /// </summary>
    /// <returns>false if there are no command currenly process responses</returns>
    private protected async Task<bool> ProcessResponseAsync()
    {
        // Response processing start time
        DateTime StartTime = DateTime.Now;

        // Assert we are not response complete yet (in case this is a rerun)
        Debug.Assert(IsComplete == false);

        // Process response until command is complete
        while (!IsComplete)
        {
            // Clear buffer if requested
            if (RequestClearBuffer)
            {
                await ResponseStream.Clear();
                RequestClearBuffer = false;
            }

            // Tracks that we have progressed in the response
            bool advanced = false;

            try
            {
                // Read the response header
                HexString header = await ResponseStream.Read(ResponseHeaderLength, true);

                // If received a response message, process it
                if (header.Byte(1) == 0x02)
                {
                    if (await ProcessResponseMessageAsync())
                    {
                        advanced = true;
                    }
                }

                // Extra ACK
                // In some cases we get an extra ACK
                else if (header.Byte(1) == IMCommandCode_ACK)
                {
                    Logger.Log.Debug("Received extra ACK from the IM");
                    ResponseStream.Advance(1);
                    advanced = true;
                }

                // IM NAK - we are done with all commands
                // Complete them and let the caller try again
                else if (header.Byte(1) == IMCommandCode_NAK)
                {
                    Logger.Log.Debug("Received NAK from the IM");
                    ResponseStream.Advance(1);
                    advanced = true;
                    SetComplete(ErrorReasons.NAK);
                }

                // Unexpected header: message likely overwritten by the buffer wrapping around
                else if (header.Byte(1) != 0 || header.Byte(2) != 0)
                {
                    // It is possible that the standard message has been erased by the wrapping around of the buffer
                    // Peek ahead by a standard message length to see if we have the rest of the response by any chance
                    int skipLength = ResponseHeaderLength + InsteonStandardMessage.Length;
                    HexString response = await ResponseStream.Read(skipLength + ResponseHeaderLength);
                    if (response.Byte(skipLength + 1) == 0x02 && (response.Byte(skipLength + 2) == 0x50 || response.Byte(skipLength + 2) == 0x51))
                    {
                        ResponseStream.Advance(skipLength);
                        advanced = true;
                    }
                }
            }
            // If we get an exception, we fail the command with the proper ErrorReason
            catch (Exception e)
            {
                var errorReason = GetErrorReasonFromException(e);
                SetComplete(errorReason);
                Logger.Log.Error($"{ErrorReasonAsString} ({e.Message})");
            }

            if (advanced)
            {
                // We have progressed, reset timeout
                StartTime = DateTime.Now;
            }
            else if (!IsComplete)
            {
                // If we have made no progress and are not complete yet, command may time out
                if ((DateTime.Now - StartTime) >= ResponseTimeout)
                {
                    SetComplete(ErrorReasons.Timeout);
                }
            }
        }

        // Log command complete here rather than in SetComplete 
        // to preserve logical order of the log
        Logger.Log.Debug("Command " + GetLogName() + " Set to Complete, " + ErrorReasonAsString);

        // Cancellation is considered success
        if (ErrorReason == ErrorReasons.NoError || ErrorReason == ErrorReasons.Cancelled)
        {
            Done();
            return true;
        }
        else
        {
            Error();
            return false;
        }
    }

    /// <summary>
    /// Process a single response message.
    /// Derived classes override to handle their own specific responses
    /// ResponseStream should be on the response header 
    /// </summary>
    /// <returns>true if advanced in the stream</returns>
    protected virtual async Task<bool> ProcessResponseMessageAsync()
    {
        bool advanced = false;

        // Acquire response header
        HexString header = await ResponseStream.Read(ResponseHeaderLength, true);

        // Response from the IM
        if (header.Byte(2) == IMCommandCode)
        {
            HexString response = await ResponseStream.Read(ResponseHeaderLength + IMResponseLength + 1);
            if (response.Byte(ResponseHeaderLength + IMResponseLength + 1) == IMCommandCode_ACK)
            {
                response = response.SubHexString(1 + ResponseHeaderLength, IMResponseLength);
                Logger.Log.Debug("Received valid response from the IM: " + response.ToString());

                if (OnIMResponseReceived(response))
                {
                    ResponseStream.Advance();
                    advanced = true;
                    await OnAfterIMReponseReceivedAsync();
                }
            }
            else if (response.Byte(ResponseHeaderLength + IMResponseLength + 1) == IMCommandCode_NAK)
            {
                // Received a NAK, terminate command processing
                Logger.Log.Debug("Received NAK from the IM: " + response.ToString());
                ResponseStream.Advance();
                advanced = true;
                SetComplete(ErrorReasons.NAK);
            }
        }
        else if (header.Byte(2) == IMCommandCode_StandardMessageReceived)
        {
            // Certain devices (e.g., RemoteLinc mini-remote) send a standard message in response to
            // the user pressing and holding SET to all-link devices, so handle standard messages received
            HexString response = await ResponseStream.Read(ResponseHeaderLength + InsteonStandardMessage.Length);
            InsteonStandardMessage responseMessage = new InsteonStandardMessage(response.SubHexString(ResponseHeaderLength + 1, InsteonStandardMessage.Length));

            Logger.Log.Debug("Received standard direct (SD) ACK message from device " + responseMessage.FromDeviceId.ToString() + " to device " + responseMessage.ToDeviceId.ToString());
            if (OnStandardResponseReceived(responseMessage))
            {
                ResponseStream.Advance();
                advanced = true;
            }
        }

        // X10 message
        else if (header.Byte(2) == IMCommandCode_X10Received)
        {
            HexString response = await ResponseStream.Read(ResponseHeaderLength + 2);
            Logger.Log.Debug("Received X10 message");
            ResponseStream.Advance();
            advanced = true;
        }

        // All-Linking Completed message
        else if (header.Byte(2) == IMCommandCode_AllLinkCompleted)
        {
            HexString response = await ResponseStream.Read(ResponseHeaderLength + InsteonAllLinkingCompletedMessage.Length);
            InsteonAllLinkingCompletedMessage responseMessage = new InsteonAllLinkingCompletedMessage(response.SubHexString(ResponseHeaderLength + 1, InsteonAllLinkingCompletedMessage.Length));
            Logger.Log.Debug("Received All-Linking Completed message");

            if (OnAllLinkingCompletedResponseReceived(responseMessage))
            {
                ResponseStream.Advance();
                advanced = true;
            }
        }

        // Button Event Report message
        else if (header.Byte(2) == IMCommandCode_ButtonEventReport)
        {
            HexString response = await ResponseStream.Read(ResponseHeaderLength + 1);
            Logger.Log.Debug("Received Button Event Report message from the IM");
            ResponseStream.Advance();
            advanced = true;
        }

        // User Reset Detected message
        else if (header.Byte(2) == IMCommandCode_UserResetDetected)
        {
            HexString response = await ResponseStream.Read(ResponseHeaderLength + 1);
            Logger.Log.Debug("Received IM User Reset Detected message from the IM");
            ResponseStream.Advance();
            advanced = true;
        }

        // All-Link Cleanup Failure message
        else if (header.Byte(2) == IMCommandCode_AllLinkFailureReport)
        {
            // TODO: create All-Link Cleanup Failure message type
            HexString response = await ResponseStream.Read(ResponseHeaderLength + 4);
            Logger.Log.Debug("Received All-Link Cleanup Failure Report message");
            ResponseStream.Advance();
            advanced = true;
        }

        // All-Link Record Response message
        else if (header.Byte(2) == IMCommandCode_AllLinkRecordResponse)
        {
            HexString response = await ResponseStream.Read(ResponseHeaderLength + AllLinkRecord.MessageLength);
            response = response.SubHexString(ResponseHeaderLength + 1, AllLinkRecord.MessageLength);
            Logger.Log.Debug("Received IM All-Link Response message: " + response);

            if (OnIMAllLinkRecordReceived(response))
            {
                ResponseStream.Advance();
                advanced = true;
            }
        }

        // All-Link Cleanup Status Report message
        else if (header.Byte(2) == IMCommandCode_AllLinkCleanupStatusReport)
        {
            HexString response = await ResponseStream.Read(ResponseHeaderLength + 1);
            Logger.Log.Debug("Received All-Link Cleanup Status Report message");
            ResponseStream.Advance();
            advanced = true;
        }

        return advanced;
    }

    /// <summary>
    ///  Called when the response from the IM is received
    ///  Complete the command. Derived classes should override if they don't want to complete.
    /// </summary>
    /// <param name="message">message received</param>
    /// <returns>true if response message was acceptable</returns>
    private protected virtual bool OnIMResponseReceived(HexString message)
    {
        IMResponse = message;
        SetComplete();
        return true;
    }

    /// <summary>
    /// This should normally be declared only in DeviceCommand, howevver during
    /// the processing of certain hub commands such as StartIMAllLinking, certain
    /// devices (e.g., mini-remote) send a standard message in response to the user
    /// depressing and holding the SET button
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    private protected virtual bool OnStandardResponseReceived(InsteonStandardMessage message)
    {
        return true;
    }

    /// <summary>
    /// Called after the response from the IM is received
    /// Gives derived classes an opportunity to execute async work
    /// </summary>
#pragma warning disable 1998
    protected virtual async Task OnAfterIMReponseReceivedAsync()
#pragma warning restore 1998
    {
    }

    /// <summary>
    ///  Called when an All-Linking Completed response message is received from the IM
    ///  Default implementation is to ignore the message; derived classes should provide appropriate implementation
    /// </summary>
    /// <param name="message">message received</param>
    /// <returns>true if response message was acceptable</returns>
    private protected virtual bool OnAllLinkingCompletedResponseReceived(InsteonAllLinkingCompletedMessage message)
    {
        return true;
    }

    /// <summary>
    ///  Called when an IM All-Link response message is received from the IM
    ///  Default implementation is to ignore the message; derived classes should provide appropriate implementation
    /// </summary>
    /// <param name="message">message received</param>
    /// <returns>true if response message was acceptable</returns>
    private protected virtual bool OnIMAllLinkRecordReceived(HexString message)
    {
        return true;
    }

    /// <summary>
    /// IM response
    /// Will throw if accessed before command completed successfully
    /// </summary>
    internal HexString IMResponse
    {
        get 
        {
            if (imResponse == null)
            {
                ErrorReason = ErrorReasons.NoIMResponse;
                throw new Exception("IM did not respond!");
            }
            return imResponse;
        }
        private protected set { imResponse = value; }
    }
    private HexString? imResponse;

    //  HttpClient this command uses
    private protected HttpClient HttpClient => gateway.HttpClient;

    // ResponseStream this command uses
    private protected InsteonHexStream ResponseStream => gateway.ResponseStream;

    /// <summary>
    ///  Length of the header preceding response messages (0x02 CommandCode)
    /// </summary>
    protected const int ResponseHeaderLength = 2;

    // Mock physical IM, only set for unit-testing
    internal MockPhysicalIM? MockPhysicalIM { get; init; }
}
