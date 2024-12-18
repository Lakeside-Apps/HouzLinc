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
using Insteon.Mock;
using System.Diagnostics;

namespace Insteon.Commands;

/// <summary>
///  Basic Insteon Device Command
/// </summary>
public abstract class DeviceCommand : HubCommand
{
    internal DeviceCommand(Gateway gateway, bool isMacroCommand = false) : base (gateway, isMacroCommand)
    {
        IMCommandType = IMCommandTypes.IM;
        IMCommandCode = IMCommandCode_SendInsteonMessage;
        ToDeviceID = InsteonID.Null;
    }

    internal DeviceCommand(Gateway gateway, InsteonID toDeviceId, bool isMacroCommand = false) : 
        this(gateway, isMacroCommand)
    {
        ToDeviceID = toDeviceId;
    }

    /// <summary>
    ///  If the command is directed at a physical device, callers need to set this
    /// </summary>
    public InsteonID ToDeviceID { get; set; }

    /// <summary>
    /// Device standard command codes
    /// Sent in Command1 byte
    /// </summary>
    protected const byte CommandCode_AssignToAllLinkGroup = 0x01;     // implemented
    protected const byte CommandCode_DeleteFromAllLinkGroup = 0x02;   // implemented
    protected const byte CommandCode_ProductDataRequest = 0x03;       // implemented
    protected const byte CommandCode2_DeviceStringRequest = 0x02;     // implemented
    protected const byte CommandCode_EnterLinkingMode = 0x09;         // implemented
    protected const byte CommandCode_EnterUnlinkingMode = 0x0A;       // implemented
    protected const byte CommandCode_GetInsteonEngineVersion = 0x0D;  // implemented
    protected const byte CommandCode_Ping = 0x0F;                     // implemented
    protected const byte CommandCode_IDRequest = 0x10;
    internal  const byte CommandCode_LightON = 0x11;                  // implemented
    protected const byte CommandCode_FastLightON = 0x12;              // implemented
    internal  const byte CommandCode_LightOFF = 0x13;                 // implemented
    protected const byte CommandCode_FastLightOFF = 0x14;             // implemented
    protected const byte CommandCode_IncrementalBright = 0x15;        // implemented
    protected const byte CommandCode_IncrementalDim = 0x16;           // implemented
    protected const byte CommandCode_StartManualChange = 0x17;
    protected const byte CommandCode_StopManualChange = 0x18;
    protected const byte CommandCode_LightStatusRequest = 0x19;       // implemented
    protected const byte CommandCode_ReadOperatingFlags = 0x1F;       // implemented
    protected const byte CommandCode_SetOperatingFlags = 0x20;        // implemented
    protected const byte CommandCode_InstantOnOff = 0x21;
    protected const byte CommandCode_RROn = 0x2E;
    protected const byte CommandCode_RROff = 0x2F;
    protected const byte CommandCode_Beep = 0x30;

    /// <summary>
    /// Device extended command codes 
    /// Sent in Command1 byte
    /// </summary>
    protected const byte CommandCode_GetForGroup = 0x2E;              // 0x01 in Data2      // implemented
    protected const byte CommandData2_GetForGroup = 0x01;
    protected const byte CommandCode_SetForGroup = 0x2E;
    protected const byte CommandData2_SetFollowMaskForGroup = 0x02;
    protected const byte CommandData2_SetFollowOffMaskForGroup = 0x03;
    protected const byte CommandData2_SetX10AdrressInfo = 0x04;       
    protected const byte CommandData2_SetRampRateForGroup = 0x05;     
    protected const byte CommandData2_SetOnLevelForGroup = 0x06;      
    protected const byte CommandData2_SetLEDBrightness = 0x07;         
    protected const byte CommandData2_SetNonToggleMask = 0x08;         
    protected const byte CommandData2_SetLEDBitMask = 0x09;            
    protected const byte CommandData2_SetX10AllBitMask = 0x0A; 
    protected const byte CommandData2_SetOnOffBitMask = 0x0B;
    protected const byte CommandData2_SetTriggerGroupBitMask = 0x0C;   

    protected const byte CommandCode_GetDatabase = 0x2F;              // 0x00 in Data2      // implemented
    protected const byte CommandCode_SetDatabase = 0x2F;              // 0x02 in Data2
    protected const byte CommandCode_TriggerGroup = 0x30;                                   // implemented

    /// <summary>
    ///  Bytes sent as part of a device command
    ///  Derived classes should set these before calling RunAsync
    ///  Data is only for extended commands
    /// </summary>
    internal byte Command1 { get; set; }
    internal byte Command2 { get; set; }
    internal byte[]? Data { get; set; }

    /// <summary>
    ///  Whether to expected an extended response message
    ///  Derived classes should set to true if they expect one
    /// </summary>
    protected bool ExpectExtendedResponseMessage = false;

    /// <summary>
    ///  Helpers to get/set Data in a 1-based manner, as in the Insteon doc
    ///  Set before calling RunAsync
    /// </summary>
    internal void ClearData()
    {
        if (Data == null)
        {
            Data = new byte[InsteonExtendedMessage.DataLength];
        }

        for (int i = 0; i < Data.Length; i++)
        {
            Data[i] = 0;
        }
    }

    internal void SetDataByte(int n, byte b)
    {
        if (Data == null)
        {
            Data = new byte[InsteonExtendedMessage.DataLength];
        }

        Data[n - 1] = b;
    }

    internal void SetDataInsteonID(int n, InsteonID id)
    {
        if (Data == null)
        {
            Data = new byte[InsteonExtendedMessage.DataLength];
        }

        id.ToByteArray(Data, n - 1);
    }

    internal byte DataByte(int n)
    {
        Debug.Assert(Data != null);
        return Data[n - 1];
    }

    /// <summary>
    ///  Run the command 
    /// </summary>
    /// <returns>true for success, false if IM returned NAK</returns>
    private protected override async Task<bool> RunAsync()
    {
        Debug.Assert(MockPhysicalDevice == null, "DeviceCommand.RunAsync() should not be called while unit-testing");

        // Prepare command string and expected response length
        byte messageFlags = (byte)((byte)MessageType.Direct + (byte)MessageMaxHops.Direct + ((Data != null) ? (byte)MessageLength.Extended : (byte)MessageLength.Standard));
        IMCommandParams = ToDeviceID.ToCommandString() + messageFlags.ToString("X2") + Command1.ToString("X2") + Command2.ToString("X2");
        IMResponseLength = 6;

        if (Data != null)
        {
            Debug.Assert(Data.Length == InsteonExtendedMessage.DataLength, "DeviceCommand: extended Data not initialized properly!");

            // Compute the checksum in byte 14
            int checksum = Command1 + Command2;
            for (int i = 1; i < 14; i++)
            {
                checksum += DataByte(i);
            }

            checksum = (0xFF - (checksum & 0xFF) + 1) & 0xFF;
            SetDataByte(14, (byte)checksum);

            // Add the user data bytes to the command
            foreach (byte b in Data)
            {
                IMCommandParams += b.ToString("X2");
            }

            IMResponseLength += InsteonExtendedMessage.DataLength;
        }

        // Send command and wait for response
        return await base.RunAsync();
    }

    /// <summary>
    /// Process a single response message.
    /// ResponseStream should be on the response header 
    /// </summary>
    /// <returns>true if advanced in the stream</returns>
    protected override async Task<bool> ProcessResponseMessageAsync()
    {
        bool advanced = false;

        // Acquire response header
        HexString header = await ResponseStream.Read(ResponseHeaderLength, true);

        // Standard response message
        if (header.Byte(2) == IMCommandCode_StandardMessageReceived)
        {
            HexString response = await ResponseStream.Read(ResponseHeaderLength + InsteonStandardMessage.Length);
            InsteonStandardMessage responseMessage = new InsteonStandardMessage(response.SubHexString(ResponseHeaderLength + 1, InsteonStandardMessage.Length));

            if (responseMessage.IsDirectACK && responseMessage.ToDeviceId == HubInsteonID)
            {
                if (responseMessage.FromDeviceId == ToDeviceID)
                {
                    Logger.Log.Debug("Received standard direct (SD) ACK message from device " + responseMessage.FromDeviceId.ToString());
                    if (OnStandardResponseReceived(responseMessage))
                    {
                        ResponseStream.Advance();
                        advanced = true;
                    }
                }
            }
            else if (responseMessage.IsDirectNAK && responseMessage.ToDeviceId == HubInsteonID)
            {
                if (responseMessage.FromDeviceId == ToDeviceID && responseMessage.Command1 == Command1)
                {
                    Logger.Log.Debug("Received standard direct (SD) NAK message from device " + responseMessage.FromDeviceId.ToString());
                    if (OnDirectNAKReceived(responseMessage))
                    {
                        ResponseStream.Advance();
                        advanced = true;
                    }
                }
            }
            else if (responseMessage.IsAllLinkBroadcast)
            {
                Logger.Log.Debug("Received standard All-Link broadcast (SA) message ACK from device " + responseMessage.FromDeviceId.ToString());
                ResponseStream.Advance();
                advanced = true;
            }
            else if (responseMessage.IsBroadcast)
            {
                if (ToDeviceID == null || responseMessage.FromDeviceId == ToDeviceID)
                {
                    Logger.Log.Debug("Received standard broadcast (SB) message ACK from device " + responseMessage.FromDeviceId.ToString());
                    if (OnStandardBroadcastResponseReceived(responseMessage))
                    {
                        ResponseStream.Advance();
                        advanced = true;
                    }
                }
            }
            else if (responseMessage.IsCleanup)
            {
                Logger.Log.Debug("Received standard All-Link cleanup (SC) message from device " + responseMessage.FromDeviceId.ToString());
                ResponseStream.Advance();
                advanced = true;
            }
            else if (responseMessage.IsCleanupACK)
            {
                Logger.Log.Debug("Received standard All-Link cleanup (SC) message ACK from device " + responseMessage.FromDeviceId.ToString());
                ResponseStream.Advance();
                advanced = true;
            }
            else if (responseMessage.IsCleanupNAK)
            {
                Logger.Log.Debug("Received standard All-Link cleanup (SC) message NAK from device " + responseMessage.FromDeviceId.ToString());
                ResponseStream.Advance();
                advanced = true;
            }
        }

        // No device found standard message
        if (header.Byte(2) == IMCommandCode_NoDeviceStandardMessageReceived)
        {
            HexString response = await ResponseStream.Read(ResponseHeaderLength + InsteonStandardMessage.Length);
            InsteonStandardMessage responseMessage = new InsteonStandardMessage(response.SubHexString(ResponseHeaderLength + 1, InsteonStandardMessage.Length));

            if (responseMessage.IsDirectACK && responseMessage.ToDeviceId == HubInsteonID)
            {
                if (responseMessage.FromDeviceId == ToDeviceID)
                {
                    Logger.Log.Debug($"Received 'no device found' standard direct (SD) ACK message from IM ({ responseMessage.FromDeviceId.ToString()})");
                    if (OnNoDeviceStandardResponseReceived(responseMessage))
                    {
                        ResponseStream.Advance();
                        advanced = true;
                    }
                }
            }
        }

        // Extended response message
        else if (header.Byte(2) == IMCommandCode_ExtendedMessageReceived)
        {
            HexString response = await ResponseStream.Read(ResponseHeaderLength + InsteonExtendedMessage.Length);
            InsteonExtendedMessage responseMessage = new InsteonExtendedMessage(response.SubHexString(ResponseHeaderLength + 1, InsteonExtendedMessage.Length));

            if (responseMessage.FromDeviceId == ToDeviceID)
            {
                if (responseMessage.ToDeviceId == HubInsteonID)
                {
                    if (OnExtendedResponseReceived(responseMessage))
                    {
                        Logger.Log.Debug("Received valid extended message response from device " + responseMessage.FromDeviceId.ToString());
                        ResponseStream.Advance();
                        advanced = true;
                    }
                }
            }
        }
        else
        {
            advanced = await base.ProcessResponseMessageAsync();
        }

        return advanced;
    }

    /// <summary>
    ///  Called when echo/ACK is received from IM
    ///  See base class for details
    /// </summary>
    /// <param name="message">message received</param>
    /// <returns>true if response message was acceptable</returns>
    private protected override bool OnIMResponseReceived(HexString message)
    {
        if (IMCommandCode == IMCommandCode_SendInsteonMessage)
        {
            // If the command was Send Standard or Extended Direct Insteon Message,
            // IM should have echo-ed the command
            if (message.ToString() == IMCommandParams)
            {
                IMResponse = message;
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            base.OnIMAllLinkRecordReceived(message);
        }

        return true;
    }

    /// <summary>
    ///  Called when a standard response message is received from the device the command was addressed to
    ///  See base class for details
    /// </summary>
    /// <param name="message">message received</param>
    /// <returns>true if response message was acceptable</returns>
    private protected override bool OnStandardResponseReceived(InsteonStandardMessage message)
    {
        StandardResponseMessage = message;
        if (!ExpectExtendedResponseMessage)
        {
            SetComplete();
        }
        return true;
    }

    /// <summary>
    ///  Called when we receive a notification from the IM that the device does not exist
    /// </summary>
    /// <param name="message">message received</param>
    /// <returns>true if response message was acceptable</returns>
    private protected virtual bool OnNoDeviceStandardResponseReceived(InsteonStandardMessage message)
    {
        StandardResponseMessage = message;
        SetComplete(ErrorReasons.NoDeviceResponse);
        return true;
    }

    /// <summary>
    ///  Called when a standard broadcast response message is received from the device the command was addressed to
    ///  Default implementation is to ignore the message; derived classes should provide appropriate implementation
    /// </summary>
    /// <param name="message">message received</param>
    /// <returns>true if response message was acceptable</returns>
    private protected virtual bool OnStandardBroadcastResponseReceived(InsteonStandardMessage message)
    {
        return true;
    }

    /// <summary>
    ///  Called when a standard NAK is received from the device the command was addressed to
    ///  See base class for details
    /// </summary>
    /// <param name="message">message received</param>
    /// <returns>true if response message was acceptable</returns>
    private protected virtual bool OnDirectNAKReceived(InsteonStandardMessage message)
    {
        StandardResponseMessage = message;

        // For commands expecting an extended message response (i.e., GetDeviceLinkRecordCommand),
        // some devices appear to return one or more direct NAK messages before returning the extended message response.
        // As a result, we ignore the NAK if we are expecting an extended message response
        if (ExpectExtendedResponseMessage)
        {
            Logger.Log.Debug("Extended response message expected, NAK considered as ACK");
        }

        // For set commands expecting a standard response (i.e., SetOperatingFlag)
        // some devices appear to send a NAK message with Command2 equal to the commandCode as success.
        // Treat that message as a standard ACK in that case
        else if (message.Command2 == Command2)
        {
            // In that case, consider the NAK a valid standard response from the device and complete the command
            Logger.Log.Debug("Cmd2 of NAK message is same as sent, NAK considered as ACK");
            SetComplete();
        }

        // Otherwise record the Direct NAK error code and complete the command
        else
        {
            SetComplete(ErrorReasons.NAK);
        }

        return true;
    }

    /// <summary>
    ///  Called when an extended response message from the device the command was addressed to is recevied by the IM
    ///  See base class for details
    /// </summary>
    private protected virtual bool OnExtendedResponseReceived(InsteonExtendedMessage message)
    {
        if (ExpectExtendedResponseMessage)
        {
            ExtendedResponseMessage = message;
            SetComplete();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Echo the command to the log listeners
    /// </summary>
    /// <param name="attempt">Current running attempt</param>
    /// <param name="maxAttempts">Max running attempts</param>
    private protected override void Echo(int attempt, int maxAttempts)
    {
        Logger.Log.CommandEcho(GetLogName() + " " + (ToDeviceID != null ? ToDeviceID + " " : "") + GetLogParams() +
            (attempt > 1 ? " (Attempt " + attempt + " of " + maxAttempts + ")" : "") +
            (isMacroCommand ? "Macro" : ""));
    }

    /// <summary>
    ///  Command completed, no error detected yet. 
    ///  Check that we got all we expected, and throw if something is not expected
    /// </summary>
    private protected override void Done()
    {
    }

    /// <summary>
    /// Command completed with an error
    /// </summary>
    private protected override void Error()
    {
        if (ErrorReason == ErrorReasons.Timeout)
        {
            LogOutput("Command " + GetLogName() + " " + ToDeviceID + " timed out!");
        }
        // Note: we check lower-case standardResponseMessage here as getting
        // StandardResponseMessage throws if command did not return a standard response message.
        else if (ErrorReason == ErrorReasons.NAK && standardResponseMessage != null)
        {
            string errorCode;
            switch (StandardResponseMessage.DirectNAKErrorCode)
            {
                case DirectNAKErrorCodes.PreNAK:
                    errorCode = "Pre NAK";
                    break;
                case DirectNAKErrorCodes.IncorrectChecksum:
                    errorCode = "Incorrect Checksum";
                    break;
                case DirectNAKErrorCodes.NoLoadDetected:
                    errorCode = "No Load Detected";
                    break;
                case DirectNAKErrorCodes.NotInDatabase:
                    errorCode = "Device not in IM database";
                    break;
                case DirectNAKErrorCodes.None:
                    errorCode = "";
                    break;
                default:
                    errorCode = "Unknown Error";
                    break;
            }

            LogOutput("Device " + ToDeviceID.ToString() + " responded with a NAK (reason: " + errorCode + ")");
        }
        else
        {
            base.Error();
        }
    }

    // TODO: consider creating two different base classes for standard and extended responses.

    /// <summary>
    /// Standard response message
    /// Will throw if accessed before command completed successfully
    /// </summary>
    internal InsteonStandardMessage StandardResponseMessage
    {
        get
        {
            Debug.Assert(ExpectExtendedResponseMessage == false);
            if (standardResponseMessage == null)
            {
                ErrorReason = ErrorReasons.NoDeviceStandardResponse;
                throw new Exception("INSTEON Device " + ToDeviceID.ToString() + " did not respond!");
            }
            return standardResponseMessage;
        }
        private protected set
        {
            standardResponseMessage = value;
        }
    }

    private InsteonStandardMessage? standardResponseMessage;

    /// <summary>
    /// Extended response message
    /// Will throw is accessed before command completed successfully
    /// </summary>
    internal InsteonExtendedMessage ExtendedResponseMessage
    { 
        get
        {
            Debug.Assert(ExpectExtendedResponseMessage == true);
            if (extendedResponseMessage == null)
            {
                ErrorReason = ErrorReasons.NoDeviceExtendedResponse;
                throw new Exception("INSTEON Device " + ToDeviceID.ToString() + " did not respond with an extended message response!");
            }
            return extendedResponseMessage;
        }
        private protected set 
        {
            extendedResponseMessage = value;
        }
    }
    private InsteonExtendedMessage? extendedResponseMessage;

    // Mock physical device, only set for unit-testing
    internal MockPhysicalDevice? MockPhysicalDevice { get; init; }
}
