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
using Insteon.Mock;
using Insteon.Model;
using System.Diagnostics;
using static Insteon.Model.AllLinkRecord;

namespace Insteon.Commands;

/// <summary>
/// Manage All-Link record in the IM database
/// </summary>
public class ManageIMAllLinkRecordCommand : HubCommand
{
    public const string Name = "ManageIMRecord";
    public static string Help = $"{CC_FindFirst}|{CC_FindNext}|{CC_ModifyFirstOrAdd}|{CC_ModifyFirstControllerOrAdd}|{CC_ModifyFirstResponderOrAdd}|{CC_DeleteFirst} <DeviceID> <Group> <Flags> <Data1> <Data2> <Data3>";
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return ControlCodeAsString() + inputRecord.GetLogOutput(-1, showNotInUseRecord: true); }

    public enum ControlCodes : byte
    {
        FindFirst = 0x00,
        FindNext = 0x01,
        ModifyFirstOrAdd = 0x20,
        ModifyFirstControllerOrAdd = 0x40,
        ModifyFirstResponderOrAdd = 0x41,
        DeleteFirstFound = 0x80,
        Invalid = 0xff,
    }

    internal const string CC_FindFirst = "FindFirst";
    internal const string CC_FindNext = "FindNext";
    internal const string CC_ModifyFirstOrAdd = "ModifyFirstOrAdd";
    internal const string CC_ModifyFirstControllerOrAdd = "ModifyFirstControllerOrAdd";
    internal const string CC_ModifyFirstResponderOrAdd = "ModifyFirstResponderOrAdd";
    internal const string CC_DeleteFirst = "deletefirst";

    internal string ControlCodeAsString()
    {
        switch(this.controlCode)
        {
            case ControlCodes.FindFirst:
                return CC_FindFirst;

            case ControlCodes.FindNext:
                return CC_FindNext;

            case ControlCodes.ModifyFirstOrAdd:
                return CC_ModifyFirstOrAdd;

            case ControlCodes.ModifyFirstControllerOrAdd:
                return CC_ModifyFirstControllerOrAdd;

            case ControlCodes.ModifyFirstResponderOrAdd:
                return CC_ModifyFirstResponderOrAdd;

            case ControlCodes.DeleteFirstFound:
                return CC_DeleteFirst;

            default:
                return "Invalid ControlCode";
        }
    }

    public static ControlCodes ControlCodeFromString(string s)
    {
        ManageIMAllLinkRecordCommand.ControlCodes controlCode;
        if (s.Equals(ManageIMAllLinkRecordCommand.CC_FindFirst, StringComparison.OrdinalIgnoreCase))
            controlCode = ManageIMAllLinkRecordCommand.ControlCodes.FindFirst;
        else if (s.Equals(ManageIMAllLinkRecordCommand.CC_FindNext, StringComparison.OrdinalIgnoreCase))
            controlCode = ManageIMAllLinkRecordCommand.ControlCodes.FindNext;
        else if (s.Equals(ManageIMAllLinkRecordCommand.CC_ModifyFirstOrAdd, StringComparison.OrdinalIgnoreCase))
            controlCode = ManageIMAllLinkRecordCommand.ControlCodes.ModifyFirstOrAdd;
        else if (s.Equals(ManageIMAllLinkRecordCommand.CC_ModifyFirstControllerOrAdd, StringComparison.OrdinalIgnoreCase))
            controlCode = ManageIMAllLinkRecordCommand.ControlCodes.ModifyFirstControllerOrAdd;
        else if (s.Equals(ManageIMAllLinkRecordCommand.CC_ModifyFirstResponderOrAdd, StringComparison.OrdinalIgnoreCase))
            controlCode = ManageIMAllLinkRecordCommand.ControlCodes.ModifyFirstResponderOrAdd;
        else if (s.Equals(ManageIMAllLinkRecordCommand.CC_DeleteFirst, StringComparison.OrdinalIgnoreCase))
            controlCode = ManageIMAllLinkRecordCommand.ControlCodes.DeleteFirstFound;
        else
            controlCode = ManageIMAllLinkRecordCommand.ControlCodes.Invalid;

        return controlCode;
    }

    public ManageIMAllLinkRecordCommand(Gateway gateway, ControlCodes controlCode, AllLinkRecord record) : base(gateway)
    {
        IMCommandType = IMCommandTypes.IM;
        IMCommandCode = IMCommandCode_ManageAllLinkRecord;
        IMResponseLength = 9;

        this.controlCode = controlCode;
        this.inputRecord = record;
    }

    private protected override async Task<bool> RunAsync()
    {
        if (MockPhysicalIM != null)
        {
            return MockRunAsync();
        }

        IMCommandParams = $"{(byte)controlCode:X2}{(byte)inputRecord.Flags:X2}{(byte)inputRecord.Group:X2}" +
            $"{inputRecord.DestID.ToCommandString()}{(byte)inputRecord.Data1:X2}{(byte)inputRecord.Data2:X2}{(byte)inputRecord.Data3:X2}";

        bool success = await base.RunAsync();

        if (!success)
        {
            LogOutput("Record Not Found");
        }

        return success;
    }

    private protected override bool OnIMResponseReceived(HexString message)
    {
        IMResponse = message;

        // For FindFirst and FindNext commands, wait for the All-Link Record message
        if (controlCode != ControlCodes.FindFirst && controlCode != ControlCodes.FindNext)
        {
            SetComplete();
        }

        return true;
    }

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
        if (!SuppressLogging)
        {
            if (controlCode == ControlCodes.FindFirst || controlCode == ControlCodes.FindNext)
            {
                Debug.Assert(Record != null);
                Record.LogCommandOutput(-1, showNotInUseRecord: true);
            }
            else
            {
                LogOutput("Done!");
            }

            base.Done();
        }
    }

    // Mock implementation of this command for testing purposes
    private bool MockRunAsync()
    {
        Debug.Assert(MockPhysicalIM != null);

        if (MockPhysicalIM is MockPhysicalIM mockPhysicalIM)
        {
            switch (controlCode)
            {
                case ControlCodes.FindFirst:
                    {
                        if (mockPhysicalIM.AllLinkDatabase.TryGetEntry(inputRecord, IdGroupComparer.Instance, out AllLinkRecord? matchingRecord))
                        {
                            Record = matchingRecord;
                            mockPhysicalIM.nextRecordIndex++;
                            return true;
                        }
                    }
                    ErrorReason = ErrorReasons.NAK;
                    break;

                case ControlCodes.FindNext:
                    {
                        if (mockPhysicalIM.AllLinkDatabase.TryGetMatchingEntries(inputRecord, IdGroupComparer.Instance, out List<AllLinkRecord>? matchingRecords))
                        {
                            if (mockPhysicalIM.nextRecordIndex < matchingRecords.Count)
                            {
                                Record = matchingRecords[mockPhysicalIM.nextRecordIndex++];
                                return true;
                            }
                        }
                    }
                    ErrorReason = ErrorReasons.NAK;
                    break;

                case ControlCodes.ModifyFirstOrAdd:
                    {
                        if (mockPhysicalIM.AllLinkDatabase.TryGetEntry(inputRecord, IdGroupComparer.Instance, out AllLinkRecord? matchingRecord))
                        {
                            mockPhysicalIM.AllLinkDatabase.ReplaceRecord(inputRecord, matchingRecord);
                            return true;
                        } 
                        else
                        {
                            mockPhysicalIM.AllLinkDatabase.Add(inputRecord);
                            return true;
                        }
                    }

                case ControlCodes.ModifyFirstControllerOrAdd:
                case ControlCodes.ModifyFirstResponderOrAdd:
                    {
                        if (mockPhysicalIM.AllLinkDatabase.TryGetMatchingEntries(inputRecord, IdGroupComparer.Instance, out List<AllLinkRecord>? matchingRecords))
                        {
                            foreach(var record in matchingRecords)
                            {
                                if (controlCode == ControlCodes.ModifyFirstControllerOrAdd ? record.IsController : record.IsResponder)
                                {
                                    mockPhysicalIM.AllLinkDatabase.ReplaceRecord(inputRecord, record);
                                    return true;
                                }
                            }
                        }
                        else
                        {
                            mockPhysicalIM.AllLinkDatabase.Add(inputRecord);
                            return true;
                        }
                    }
                    break;

                case ControlCodes.DeleteFirstFound:
                    return mockPhysicalIM.AllLinkDatabase.RemoveMatchingEntry(inputRecord, IdGroupComparer.Instance);

                default:
                    Debug.Assert(false, "ManageIMAllLinkRecordCommand - Unknown control code");
                    break;
            }
        }
        return false;
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

    // Command control code
    private ControlCodes controlCode;

    // Record provided as argument to the command
    private AllLinkRecord inputRecord;
}

/// <summary>
/// Classes for each Control Code of ManageIMAllLinkRecordCommand
/// </summary>

public sealed class FindIMFirstAllLinkRecordCommand : ManageIMAllLinkRecordCommand
{
    internal static new string Name { get => "IMFirstRecord"; }
    private protected override string GetLogName() { return Name; }

    internal FindIMFirstAllLinkRecordCommand(Gateway gateway, AllLinkRecord record) : base(gateway, ControlCodes.FindFirst, record) { }
}

public sealed class FindIMNextAllLinkRecordCommand : ManageIMAllLinkRecordCommand
{
    internal static new string Name { get => "IMNextRecord"; }
    private protected override string GetLogName() { return Name; }

    internal FindIMNextAllLinkRecordCommand(Gateway gateway, AllLinkRecord record) : base(gateway, ControlCodes.FindNext, record) { }
}

public sealed class EditIMFirstFoundOrAddAllLinkRecordCommand : ManageIMAllLinkRecordCommand
{
    internal static new string Name { get => "IMEditFirstOrAddRecord"; }
    private protected override string GetLogName() { return Name; }

    internal EditIMFirstFoundOrAddAllLinkRecordCommand(Gateway gateway, AllLinkRecord record) : base(gateway, ControlCodes.ModifyFirstOrAdd, record) { }
}

public sealed class EditIMFirstFoundOrAddControllerAllLinkRecordCommand : ManageIMAllLinkRecordCommand
{
    internal static new string Name { get => "IMEditFirstOrAddControllerRecord"; }
    private protected override string GetLogName() { return Name; }

    internal EditIMFirstFoundOrAddControllerAllLinkRecordCommand(Gateway gateway, AllLinkRecord record) : base(gateway, ControlCodes.ModifyFirstControllerOrAdd, record) { }
}

public sealed class EditIMFirstFoundOrAddResponderAllLinkRecordCommand : ManageIMAllLinkRecordCommand
{
    internal static new string Name { get => "IMEditFirstOrAddResponderRecord"; }
    private protected override string GetLogName() { return Name; }

    internal EditIMFirstFoundOrAddResponderAllLinkRecordCommand(Gateway gateway, AllLinkRecord record) : base(gateway, ControlCodes.ModifyFirstResponderOrAdd, record) { }
}

public sealed class DeleteIMFirstFoundAllLinkRecordCommand : ManageIMAllLinkRecordCommand
{
    internal static new string Name { get => "IMDeleteFirstRecord"; }
    private protected override string GetLogName() { return Name; }

    internal DeleteIMFirstFoundAllLinkRecordCommand(Gateway gateway, AllLinkRecord record) : base(gateway, ControlCodes.DeleteFirstFound, record) { }
}
