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
using Insteon.Mock;
using Insteon.Base;
using Insteon.Model;

namespace Insteon.Commands;

public sealed class SetDeviceLinkRecordCommand : DeviceCommand
{
    public const string Name = "SetLink";
    public const string Help = "<DeviceID> <seq> <DestDeviceID> <Group> <Flags> <Data1> <Data2> <Data3>";
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return allLinkRecord.GetLogOutput(seq); }

    public SetDeviceLinkRecordCommand(Gateway gateway, InsteonID deviceID, int seq, AllLinkRecord allLinkRecord) : base(gateway, deviceID)
    {
        if (seq < 0 || seq >= 512)
        {
            throw new ArgumentException("Attempting to write ALL-Link record out of database bounds");
        }

        Command1 = CommandCode_SetDatabase;
        Command2 = 0;
        this.seq = seq;

        SetDataByte(1, 0);              // unused
        SetDataByte(2, 0x02);           // always 0x02
        Address = (ushort)(0xFFF - seq * AllLinkRecord.RecordByteLength); // record address in bytes 3 and 4
        SetDataByte(5, 0x08);           // number of bytes to write

        this.allLinkRecord = allLinkRecord;  // record data to write
        SetDataByte(6, (byte)allLinkRecord.Flags);
        SetDataByte(7, (byte)allLinkRecord.Group);
        SetDataInsteonID(8, allLinkRecord.DestID);
        SetDataByte(11, allLinkRecord.Data1);
        SetDataByte(12, allLinkRecord.Data2);
        SetDataByte(13, allLinkRecord.Data3);
    }

    private protected override void Done()
    {
        base.Done();
        LogOutput("Done!");
    }

    // Address of the link to retrieve
    private ushort Address
    {
        //get => (ushort)((DataByte(3) << 8) + DataByte(4));
        set
        {
            Debug.Assert((0xFFF - value) % AllLinkRecord.RecordByteLength == 0, "Invalid start address");
            SetDataByte(3, (byte)((value >> 8) & 0x00FF));
            SetDataByte(4, (byte)(value & 0x00FF));
        }
    }

    private protected override async Task<bool> RunAsync()
    {
        if (MockPhysicalDevice != null)
        {
            // Mock implementation of this command for testing purposes
            MockPhysicalDevice.AllLinkDatabase.SetRecordAt(seq, allLinkRecord);
            return true;
        }

        return await base.RunAsync();
    }

    private int seq;
    private AllLinkRecord allLinkRecord;
}
