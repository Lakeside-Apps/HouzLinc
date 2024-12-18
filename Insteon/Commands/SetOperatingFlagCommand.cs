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
public sealed class SetOperatingFlagCommand : DeviceCommand
{
    public const string Name = "SetOperatingFlag";
    public const string Help = "<DeviceID> <OperatingFlag>";
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return "CommandCode: " + Command2.ToString("X2"); }

    internal enum CommandCodeForRemoteLinc : byte
    {
        ProgramLockOn = 0x00,
        ProgramLockOff = 0x01,
        LEDOn = 0x02,
        LEDOff = 0x03,
        BeeperOn = 0x04,
        BeeperOff = 0x05,
        StayAwakeOn = 0x06,
        StayAwakeOff = 0x07,
        ListenOnlyOn = 0x08,
        ListenOnlyOff = 0x09,
        NoImAliveOn = 0x0A,
        NoImAliveOff = 0x0B
    }

    internal enum CommandCodeForKeypadLinc : byte
    {
        ProgramLockOn = 0x00,
        ProgramLockOff = 0x01,
        LEDOnTxOn = 0x02,
        LEDOnTxOff  = 0x03,
        ResumeDimOn = 0x04,
        ResumeDimOff = 0x05,
        Keypad8 = 0x06,
        Keypad6 = 0x07,
        LEDBacklightOn = 0x08,
        LEDBackLightOff = 0x09,
        KeyBeepOn = 0x0A,
        KeyBeepOff = 0x0B
    }

    internal enum CommandCode : byte
    {
        ProgramLockOn = 0x00,
        ProgramLockOff = 0x01,
        LEDOnTxOn = 0x02,
        LEDOnTxOff = 0x03,
        ResumeDimOn = 0x04,
        ResumeDimOff = 0x05,
        LoadSenseOn = 0x06,
        LoadSenseOff = 0x07,
        LEDOn = 0x08,
        LEDOff = 0x09
    }

    public SetOperatingFlagCommand(Gateway gateway, InsteonID deviceID, byte commandCode) : base(gateway, deviceID)
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
