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
/// Set On Level for button
/// </summary>
public sealed class SetOnLevelForGroupCommand : DeviceCommand
{
    public const string Name = "SetOnLevelForGroup";
    public const string Help = "<DeviceID> <Group> <OnLevel>";
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return "Group: " + Group.ToString() + ", OnLevel: " + OnLevel.ToString(); }

    public SetOnLevelForGroupCommand(Gateway gateway, InsteonID deviceID, byte group, byte onLevel) : base(gateway, deviceID)
    {
        Command1 = CommandCode_SetForGroup;
        Command2 = 0;
        SetDataByte(2, CommandData2_SetOnLevelForGroup);
        Group = group;
        OnLevel = onLevel;
        ExpectExtendedResponseMessage = false;
    }

    private protected override void Done()
    {
        base.Done();
        LogOutput("Done!");
    }

    internal byte Group
    {
        get => DataByte(1);
        private set => SetDataByte(1, value);      }

    internal byte OnLevel
    {
        get => DataByte(3);
        private set => SetDataByte(3, value);
    }
}
