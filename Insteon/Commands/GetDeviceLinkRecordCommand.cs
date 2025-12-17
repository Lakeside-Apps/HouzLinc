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
using Insteon.Model;
using System.Diagnostics;

namespace Insteon.Commands;

/// <summary>
/// Command to get a given link record from the device database
/// </summary>
public sealed class GetDeviceLinkRecordCommand : DeviceCommand
{
    public const string Name = "GetLink";
    public const string Help = "<DeviceID> <Record Seq Number> [<Insteon Engine Version (default: 2)]";
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return "Seq: " + LinkRecordSeq.ToString(); }

    public GetDeviceLinkRecordCommand(Gateway gateway, InsteonID deviceID, int linkRecordSeq, int engineVersion) : base(gateway, deviceID, isMacroCommand: false, engineVersion: engineVersion)
    {
        Command1 = CommandCode_GetDatabase;
        Command2 = 0;
        LinkRecordSeq = linkRecordSeq;
        Address = (ushort)(0xFFF - linkRecordSeq * AllLinkRecord.RecordByteLength);
        SetDataByte(5, 1);      // retrieve one single link
        ExpectExtendedResponseMessage = true;
    }

    private protected override async Task<bool> RunAsync()
    {
        if (MockPhysicalDevice != null)
        {
            // Mock implementation of this command for testing purposes
            if (MockPhysicalDevice.AllLinkDatabase.Count <= LinkRecordSeq)
                return false;

            AllLinkRecord = MockPhysicalDevice.AllLinkDatabase[LinkRecordSeq];
            return true;
        }

        return await base.RunAsync();
    }

    // Sequence number of the link to retrieve
    private int LinkRecordSeq { get; set; }

    // Address of the link to retrieve
    private ushort Address
    {
        get => (ushort)((DataByte(3) << 8) + DataByte(4));
        set
        {
            Debug.Assert((0xFFF - value) % AllLinkRecord.RecordByteLength == 0, "Invalid start address");
            SetDataByte(3, (byte)((value >> 8) & 0x00FF));
            SetDataByte(4, (byte)(value & 0x00FF));
        }
    }

    /// <summary>
    ///  Called when an extended response message from the device the command was addressed to is recevied by the IM
    /// </summary>
    /// <param name="message">The message</param>
    /// <returns>true if the message was acceptable</returns>
    private protected override bool OnExtendedResponseReceived(InsteonExtendedMessage message)
    {
        // Set this to make Done() happy
        ExtendedResponseMessage = message;

        try
        {
            AllLinkRecord allLinkRecord = new AllLinkRecord(message, EngineVersion);
            Logger.Log.Debug("Received record");
            if (allLinkRecord.Address == Address)
            {
                AllLinkRecord = allLinkRecord;
                SetComplete();
                return true;
            }
        }
        catch (AllLinkRecord.InvalidAllLinkRecordException)
        {
            if (++invalidRecordTimes > invalidRecordTimesMax)
            {
                // If we received an invalid record too many times in a row, 
                // request clearing the stream buffer in a (desperate) attempt to pick subsequent records
                RequestClearBuffer = true;
                invalidRecordTimes = 0;
            }
        }

        return false;
    }

    private int invalidRecordTimes = 0;
    private const int invalidRecordTimesMax = 3;

    /// <summary>
    ///  Called by RunAsync when the execution of the command is complete and successful
    ///  Because of the retries for missed records, we only show the Done! message when we 
    ///  have no more missed records 
    /// </summary>
    private protected override void Done()
    {
        base.Done();
        if (!SuppressLogging && ErrorReason == ErrorReasons.NoError)
        {
            AllLinkRecord?.LogCommandOutput(LinkRecordSeq);
        }
    }

    /// <summary>
    /// Retrieved record
    /// </summary>
    internal AllLinkRecord? AllLinkRecord { get; private set; }
}
