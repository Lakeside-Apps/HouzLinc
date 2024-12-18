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
using Common;

namespace Insteon.Commands;

public sealed class AssignToAllLinkGroupCommand : DeviceCommand
{
    public const string Name = "AssignToGroup";
    public const string Help = "<DeviceID> <Group>";
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return "Group: " + Command2.ToString(); }

    public AssignToAllLinkGroupCommand(Gateway gateway, InsteonID deviceId, byte group) : base(gateway)
    {
        ToDeviceID = deviceId;
        Command1 = CommandCode_AssignToAllLinkGroup;
        Command2 = group;
    }

    private protected override void Done()
    {
        base.Done();
        LogOutput($"Device {ToDeviceID} entered linking mode!");
    }
}

public sealed class DeleteFromAllLinkGroupCommand : DeviceCommand
{
    public const string Name = "DeleteFromGroup";
    public const string Help = "<DeviceID> <Group>";
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return Command2.ToString(); }

    public DeleteFromAllLinkGroupCommand(Gateway gateway, InsteonID deviceId, byte group) : base(gateway)
    {
        ToDeviceID = deviceId;
        Command1 = CommandCode_DeleteFromAllLinkGroup;
        Command2 = group;
    }

    private protected override void Done()
    {
        base.Done();
        LogOutput($"Device {ToDeviceID} was deleted from group {Command2}!");
    }
}
