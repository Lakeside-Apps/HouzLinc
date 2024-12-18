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
/// Set LED Bit Mask for Keypadlinc buttons
/// LEDBitMask:
///   bit 0: sets button 1 LED to on
///   ...
///   bit 7: sets button 8 LED to on
/// </summary>
public sealed class SetLEDBitMaskCommand : DeviceCommand
{
    public const string Name = "SetLEDBitMask"; 
    public const string Help = "<DeviceUD> <LEDBitMask(hex)>";

    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return "LEDBitMask: " + LEDBitMask.ToString("X2"); }

    public SetLEDBitMaskCommand(Gateway gateway, InsteonID deviceID, byte ledBitMask) : base(gateway, deviceID)
    {
        Command1 = CommandCode_SetForGroup;
        Command2 = 0;
        SetDataByte(1, 0x01);
        SetDataByte(2, CommandData2_SetLEDBitMask);
        LEDBitMask = ledBitMask;
        ExpectExtendedResponseMessage = false;
    }

    private protected override void Done()
    {
        base.Done();
        LogOutput("Done!");
    }

    internal byte LEDBitMask
    {
        get => DataByte(3);
        set => SetDataByte(3, value);
    }
}
