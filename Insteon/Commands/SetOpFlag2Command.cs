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

namespace Insteon.Commands;

/// <summary>
/// Set On Mask (a.k.a., follow mask) for button
/// </summary>
public sealed class SetOpFlag2Command : DeviceCommand
{
    public const string Name = "SetOpFlag2";
    public const string Help = "<DeviceID> <OpFlag2>";
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return "CommandCode: " + Command2.ToString("X2"); }

    enum CommandCode : byte
    {
        TenDflagOn = 0x10,
        TenDflagOff = 0x11,
        X10OffflagOn = 0x12,
        X10OffOfagOff = 0x13,
        ErrorBlinkOff = 0x14,
        ErrorBlinkOn = 0x15,
        CleanupReportOff = 0x16,
        CleanupReportOn = 0x17,
        DetachLoadOff = 0x1A,
        DetachLoadOn = 0x1B,
        StartHopsOfLastRxAck = 0x1C,
        StartHopsOf1 = 0x1D
    }

    public SetOpFlag2Command(Gateway gateway, InsteonID deviceID, byte commandCode) : base(gateway, deviceID)
    {
        Command1 = CommandCode_SetOperatingFlags;
        Command2 = commandCode;
        SetDataByte(1, 0);
        ExpectExtendedResponseMessage = false;
    }

    private protected override void Done()
    {
        base.Done();
        LogOutput("Done!");
    }
}
