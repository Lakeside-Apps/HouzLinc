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

using Insteon.Base;
using Insteon.Model;
using System.Diagnostics;

namespace Insteon.Commands;

public sealed class GetIMFirstAllLinkRecordCommand : HubCommand
{
    public const string Name = "GetIMFirstAllLinkRecord";
    public const string Help = "";

    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return ""; }

    public GetIMFirstAllLinkRecordCommand(Gateway gateway) : base(gateway)
    {
        IMCommandType = IMCommandTypes.IM;
        IMCommandCode = IMCommandCode_GetFirstAllLinkRecord;
        IMResponseLength = 0;
        IMCommandParams = "";
    }

    /// <summary>
    ///  Received response/ACK from the IM
    ///  See base class for details
    /// </summary>
    private protected override bool OnIMResponseReceived(HexString message)
    {
        // Wait for the AllLinkRecord message
        return true;
    }

    /// <summary>
    ///  Received IM All-Link record response message
    ///  See base class for details
    /// </summary>
    private protected override bool OnIMAllLinkRecordReceived(HexString message)
    {
        try
        {
            Record = new AllLinkRecord(message);
        }
        catch (AllLinkRecord.InvalidAllLinkRecordException)
        {
            return false;
        }

        SetComplete();
        return true;
    }

    private protected override void Done()
    {
        if (!SuppressLogging && ErrorReason == ErrorReasons.NoError)
        {
            Record.LogCommandOutput(0);
        }
    }

    /// <summary>
    /// Record returned by this command
    /// Will throw if accessed before command completed successfully
    /// </summary>
    internal AllLinkRecord Record
    {
        get
        {
            if (record == null)
            {
                ErrorReason = ErrorReasons.NoAllLinkRecordResponse;
                throw new Exception("No All-Link record response from the IM!");
            }
            return record;
        }
        set
        {
            record = value;
        }
    }
    private AllLinkRecord? record;
}

public sealed class GetIMNextAllLinkRecordCommand : HubCommand
{
    public const string Name = "GetIMNextAllLinkRecord";
    public const string Help = "";

    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return ""; }

    public GetIMNextAllLinkRecordCommand(Gateway gateway) : base(gateway)
    {
        IMCommandType = IMCommandTypes.IM;
        IMCommandCode = IMCommandCode_GetNextAllLinkRecord;
        IMResponseLength = 0;
        IMCommandParams = "";
    }

    /// <summary>
    ///  Received response/ACK from the IM
    ///  See base class for details
    /// </summary>
    private protected override bool OnIMResponseReceived(HexString message)
    {
        return true;
        // Wait for the AllLinkRecord message
    }

    /// <summary>
    ///  Received All-Link record response message
    ///  See base class for details
    /// </summary>
    private protected override bool OnIMAllLinkRecordReceived(HexString message)
    {
        try
        {
            Record = new AllLinkRecord(message);
        }
        catch (AllLinkRecord.InvalidAllLinkRecordException)
        {
            return false;
        }

        SetComplete();
        return true;
    }

    private protected override void Done()
    {
        if (!SuppressLogging && ErrorReason == ErrorReasons.NoError)
        {
            Record.LogCommandOutput(0);
        }
    }

    /// <summary>
    /// Record returned by this command
    /// Will throw if accessed before command completed successfully
    /// </summary>
    internal AllLinkRecord Record
    {
        get
        {
            if (record == null)
            {
                ErrorReason = ErrorReasons.NoAllLinkRecordResponse;
                throw new Exception("No All-Link record response from the IM!");
            }
            return record;
        }
        set
        {
            record = value;
        }
    }
    private AllLinkRecord? record;
}

/// <summary>
///  Get All-Link database of the IM
///  This Command is a macro command that runs GetIMFirstAllLinkRecordCommand and
///  then GetIMNextAllLinkRecordCommand over the whole database
/// </summary>
public sealed class GetIMDatabaseCommand : HubCommand
{
    public const string Name = "GetIMDB";
    public const string Help = "";
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return ""; }

    public GetIMDatabaseCommand(Gateway gateway) : base(gateway, isMacroCommand: true)
    {
        AllLinkDatabase = new AllLinkDatabase();
    }

    private protected override async Task<bool> RunAsync()
    {
        if (MockPhysicalIM != null)
        {
            // Mock implementation of this command for testing purposes
            AllLinkDatabase = MockPhysicalIM.AllLinkDatabase;
            return true;
        }

        bool done = false;

        var cmd = new GetIMFirstAllLinkRecordCommand(gateway) { SuppressLogging = true };
        if (await cmd.TryRunAsync())
        {
            Debug.Assert(cmd.Record != null);
            AddRecord(cmd.Record);
        }
        else
        {
            if (cmd.ErrorReason == ErrorReasons.NAK)
            {
                done = true;
            }
            else
            {
                throw new Exception(Name + " Command failed!");
            }
        }

        while (!done && !IsCancelled)
        {
            var cmdNext = new GetIMNextAllLinkRecordCommand(gateway) { SuppressLogging = true };
            if (await cmdNext.TryRunAsync(maxAttempts: 10))
            {
                AddRecord(cmdNext.Record);
            }
            else
            {
                if (cmdNext.ErrorReason == ErrorReasons.NAK)
                {
                    done = true;
                }
                else
                {
                    throw new Exception(Name + " Command failed!");
                }
            }
        }

        Done();
        return true;
    }

    /// <summary>
    /// Add record to the database
    /// </summary>
    /// <param name="record"></param>
    private void AddRecord(AllLinkRecord record)
    {
        AllLinkDatabase.Add(record);
        record.LogCommandOutput(AllLinkDatabase.Count - 1);
    }

    /// <summary>
    ///  Obtained records
    /// </summary>
    internal AllLinkDatabase AllLinkDatabase { get; private set; }
}
