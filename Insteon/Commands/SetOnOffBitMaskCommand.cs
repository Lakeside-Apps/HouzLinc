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
/// Set On/Off Bit Mask for Keypadlinc non toggle buttons, 
/// i.e., whether the button will go to on or off state when pressed
/// OnOffBitMask:
///   bit 0: sets button 1 to on 
///   ...
///   bit 7: sets button 8 to on
/// </summary>
public sealed class SetOnOffBitMaskCommand : DeviceCommand
{
    public const string Name = "SetOnOffBitMask";
    public const string Help = "<DeviceID> <OnOffBitMask(hex)>";
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return "OnOffBitMask: " + OnOffBitMask.ToString("X2"); }

    public SetOnOffBitMaskCommand(Gateway gateway, InsteonID deviceID, byte onOffBitMask) : base(gateway, deviceID)
    {
        Command1 = CommandCode_SetForGroup;
        Command2 = 0;
        SetDataByte(1, 0x01);
        SetDataByte(2, CommandData2_SetOnOffBitMask);
        OnOffBitMask = onOffBitMask;
        ExpectExtendedResponseMessage = false;
    }

    private protected override void Done()
    {
        base.Done();
        LogOutput("Done!");
    }

    internal byte OnOffBitMask
    {
        get => DataByte(3);
        private set => SetDataByte(3, value);
    }
}
