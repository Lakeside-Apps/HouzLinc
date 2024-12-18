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
/// Set Trigger Group Bit Mask for Keypadlinc buttons
/// TriggerGroupBitMask:
///   bit 0: sets button 1 TriggerGroup to on
///   ...
///   bit 7: sets button 8 TriggerGroup to on
/// </summary>
public sealed class SetTriggerGroupBitMaskCommand : DeviceCommand
{
    public const string Name = "SetTriggerGroupBitMask";
    public const string Help = "<DeviceID> <TriggerGroupBitMask(hex)>";
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return "TriggerGroupBitmask: " + TriggerGroupBitMask.ToString("X2"); }

    public SetTriggerGroupBitMaskCommand(Gateway gateway, InsteonID deviceID, byte triggerGroupBitMask) : base(gateway, deviceID)
    {
        Command1 = CommandCode_SetForGroup;
        Command2 = 0;
        SetDataByte(1, 0x01);
        SetDataByte(2, CommandData2_SetTriggerGroupBitMask);
        TriggerGroupBitMask = triggerGroupBitMask;
        ExpectExtendedResponseMessage = false;
    }

    private protected override void Done()
    {
        base.Done();
        LogOutput("Done!");
    }

    internal byte TriggerGroupBitMask
    {
        get => DataByte(3);
        private set => SetDataByte(3, value);
    }
}
