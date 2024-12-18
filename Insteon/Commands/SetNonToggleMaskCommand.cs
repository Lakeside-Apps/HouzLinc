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
/// Set Non Toggle Mask for Keypadlinc buttons
/// NonToggleMask:
///   bit 0: sets button 1 to non-toggle
///   ...
///   bit 7: sets button 8 to non-toggle
/// </summary>
public sealed class SetNonToggleMaskCommand : DeviceCommand
{
    public const string Name = "SetNonToggleMask";
    public const string Help = "<DeviceID> <NonToggleMask>";
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return "NonToggleMask: " + NonToggleMask.ToString("X2"); }

    public SetNonToggleMaskCommand(Gateway gateway, InsteonID deviceID, byte nonToggleMask) : base(gateway, deviceID)
    {
        Command1 = CommandCode_SetForGroup;
        Command2 = 0;
        SetDataByte(1, 0x01);
        SetDataByte(2, CommandData2_SetNonToggleMask);
        NonToggleMask = nonToggleMask;
        ExpectExtendedResponseMessage = false;
    }

    private protected override void Done()
    {
        base.Done();
        LogOutput("Done!");
    }

    internal byte NonToggleMask
    {
        get => DataByte(3);
        private set => SetDataByte(3, value);
    }
}
