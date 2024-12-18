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

namespace Insteon.Commands;

public sealed class SendAllLinkCommand : HubCommand
{
    public const string Name = "SendAllLink";
    public const string Help = "<group> " + LightOnCommand.Name + "|" + LightOffCommand.Name + 
        "|" + BrighterCommand.Name + "|" + DimmerCommand.Name + " [<level>]";

    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return ""; }

    public SendAllLinkCommand(Gateway gateway, byte group, byte cmd, byte level) : base(gateway)
    {
        this.group = group;
        this.cmd = cmd;
        this.level = level;

        IMCommandType = IMCommandTypes.IM;
        IMCommandCode = IMCommandCode_SendAllLink;
        IMResponseLength = 3;
        IMCommandParams = $"{group:X2}{cmd:X2}{level:X2}";
    }

    private protected override void Done()
    {
        base.Done();
        LogOutput($"Command {cmd:X2} param {level:X2} sent to group {group} members");
    }

    private byte group;
    private byte cmd;
    private byte level;
}
