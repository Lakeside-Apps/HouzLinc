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
using static Insteon.Base.InsteonMessage;

namespace Insteon.Commands;

/// <summary>
///  Command to get the content of a device database. Returns all records in the device database.
///  Note: this is the raw Get All Link Database Insteon command. It is not very reliable as the 
///  response data keeps wrapping around the hub response buffer and we sometimes miss records. 
///  The GetDeviceDatabaseCommand command wraps logic to fetch the missing records individually.
/// </summary>
public sealed class GetDeviceLinkRecordsCommand : DeviceCommand
{
    public const string Name = "GetLinkRecords";
    public const string Help = "<DeviceID> [<Insteon Engine Version (default: 2)]";

    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return ""; }

    internal GetDeviceLinkRecordsCommand(Gateway gateway, InsteonID deviceID, int engineVersion) : base(gateway, deviceID, isMacroCommand: false, engineVersion: engineVersion)
    {
        Command1 = CommandCode_GetDatabase;
        Command2 = 0;
        ExpectExtendedResponseMessage = true;

        // Set start address at top of database memory (0xFFF)
        SetDataByte(3, (byte)0xF);
        SetDataByte(4, (byte)0x00FF);

        // 0 indicates to fetch all records in the command data
        SetDataByte(5, 0);

        // This command can be slow, give it a bit of slack
        ResponseTimeout = TimeSpan.FromSeconds(10);

        Records = new AllLinkDatabase();
    }

    // Helper const for start address
    private const int StartAddress = 0xFFF;

    private protected override async Task<bool> RunAsync()
    {
        if (MockPhysicalDevice != null)
        {
            // Mock implementation of this command for testing purposes
            Records = MockPhysicalDevice.AllLinkDatabase;
            return true;
        }

        return await base.RunAsync();
    }

    /// <summary>
    ///  Called when an extended response message from the device the command was addressed to is recevied by the IM
    /// </summary>
    /// <param name="message">The message</param>
    /// <returns>true if the message was acceptable</returns>
    private protected override bool OnExtendedResponseReceived(InsteonExtendedMessage message)
    {
        try
        {
            // Set this to ensure Done() does not report an error
            ExtendedResponseMessage = message;

            AllLinkRecord record = new AllLinkRecord(message, EngineVersion);
            if (AddRecord(record))
            {
                if (record.IsLast)
                {
                    SetComplete();
                }
            }
            return true;
        }
        catch (AllLinkRecord.InvalidAllLinkRecordException)
        {
            // This generally means that we have not gotten all the response data from the hub yet
            // return false and let the retry mechanism take care of it
            return false;
        }

    }

    /// <summary>
    ///  Store a new record, preceded by placeholder if some records were skipped (not acquired from the hub)
    /// </summary>
    /// <param name="record">The record to add</param>
    /// <returns>Whether a record was added</returns>
    private bool AddRecord(AllLinkRecord record)
    {
        if ((StartAddress - record.Address) % AllLinkRecord.RecordByteLength != 0)
        {
            throw new InvalidMessageException("Record has incorrect address: " + Convert.ToString(record.Address, 16));
        }

        // Compute the record sequence number
        int seq = (StartAddress - record.Address) / AllLinkRecord.RecordByteLength;

        // If any record were missed, report them
        if (seq >= Records.Count)
        {
            int missedRecords = seq - Records.Count;

            if (missedRecords == 1)
            {
                Logger.Log.CommandOutput("Record " + (seq - 1) + " missed");
            }
            else if (missedRecords > 1)
            {
                Logger.Log.CommandOutput("Records " + (seq - missedRecords) + "-" + (seq - 1) + " missed");
            }
        }
        else
        {
            if (Records[seq] != null)
            {
                // If the record was already added, that means that we are still waiting for the next record
                Logger.Log.Debug("Record already added (" + Convert.ToString(record.Address, 16) + ")");
                return false;
            }
        }

        // Add the new found record to the database
        Records.SetRecordAt(seq, record);
        LogRecord(seq, record);

        return true;
    }

    /// <summary>
    ///  Log content of a record 
    /// </summary>
    /// <param name="seq">record sequence number</param>
    /// <param name="record">record to log</param>
    private void LogRecord(int seq, AllLinkRecord record)
    {
        if (!SuppressLogging)
        {
            record.LogCommandOutput(seq);
        }
    }

    /// <summary>
    ///  List of returned records
    /// </summary>
    internal AllLinkDatabase Records { get; private set; }
}
