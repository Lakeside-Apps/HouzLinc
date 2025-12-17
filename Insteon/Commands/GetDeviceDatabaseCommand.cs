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

// This class is a macro command that runs GetDeviceLinkRecordCommand for each record in the device database
public sealed class GetDeviceDatabaseCommand : DeviceCommand
{
    public const string Name = "GetDB";
    public const string Help = "<DeviceID> [<Insteon Engine Version (default: 2)]";
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return ""; }

    public GetDeviceDatabaseCommand(Gateway gateway, InsteonID deviceID, int engineVersion) : 
        base(gateway, deviceID, isMacroCommand: true, engineVersion) { }

    /// <summary>
    /// Database return by this command
    /// Will throw if accessed before command completed successfully
    /// </summary>
    internal AllLinkDatabase Records
    {
        get
        {
            if (records == null)
            {
                ErrorReason = ErrorReasons.NoAllLinkRecordResponse;
                throw new Exception("No All-Link record response from the IM!");
            }
            return records;
        }
        set
        {
            records = value;
        }
    }
    private AllLinkDatabase? records;

    // The multi-record get database command is not very reliable. 
    // This flag controls whether to use it or not
    private bool useMultiRecordCommand = false;

    private protected override async Task<bool> RunAsync()
    {
        // If using the multi-record command, attempt to get records that way
        if (useMultiRecordCommand)
        {
            GetDeviceLinkRecordsCommand cmd = new GetDeviceLinkRecordsCommand(gateway, ToDeviceID, EngineVersion);

            // Since we will be patching up the returned Records list using the single-record command (see below)
            // - allow only one attempt
            // - ignore whether this command succeeded or not
            await cmd.TryRunAsync(maxAttempts: 1);
            Records = cmd.Records;
        }
        else
        {
            Records = new AllLinkDatabase();
        }

        // If we did not try to acquire records with the multi-record command,
        // or if we have not gotten all the records, acquire them now
        // Go over the database and requery any null entry or missing entry at the end
        for (int i = 0; ; i++)
        {
            Debug.Assert(i <= Records.Count);

            if (i == Records.Count || Records[i] == null)
            {
                GetDeviceLinkRecordCommand cmd = new GetDeviceLinkRecordCommand(gateway, ToDeviceID, i, EngineVersion);
                cmd.SuppressLogging = true;

                if (await cmd.TryRunAsync(maxAttempts: 10))
                {
                    if (cmd.AllLinkRecord!.Address != (ushort)(StartAddress - (i * AllLinkRecord.RecordByteLength)))
                    {
                        ErrorReason = ErrorReasons.SubCommandFailed;
                        throw new Exception("Record has incorrect address");
                    }

                    if (i < Records.Count)
                    {
                        Records[i] = cmd.AllLinkRecord;
                    }
                    else
                    {
                        Records.Add(cmd.AllLinkRecord);
                    }

                    cmd.AllLinkRecord.LogCommandOutput(i);
                    if (cmd.AllLinkRecord.IsLast)
                    {
                        break;
                    }
                }
                else
                {
                    // If TryRunAsync returned false, we should have a reason
                    Debug.Assert(cmd.ErrorReason != ErrorReasons.NoError);

                    // Propagate reason to this parent command
                    ErrorReason = cmd.ErrorReason;
                    throw new Exception($"Failed to acquire record! {ErrorReasonAsString}");
                }
            }
            else
            {
                Debug.Assert(Records[i].Address == (ushort)(StartAddress - (i * AllLinkRecord.RecordByteLength)));
            }
        }

        Done();
        return true;
    }

    /// <summary>
    ///  Called by RunAsync when the execution of the command is complete and successful
    ///  Because of the retries for missed records, we only show the Done! message when we 
    ///  have no more missed records 
    /// </summary>
    private protected override void Done()
    {
        if (!SuppressLogging)
        {
            if (Records != null && Records.Count > 0)
            {
                LogOutput("Done! - Device " + ToDeviceID.ToString() + " reported " + Records.Count.ToString() + " records");
            }
            else
            {
                LogOutput("Done! - No record received");
            }
        }
    }

    // Top address of the link database
    private const int StartAddress = 0xFFF;
}
