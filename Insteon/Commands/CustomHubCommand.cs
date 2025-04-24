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

/// <summary>
/// This sends a litteral command string to the hub, for testing purposes
/// The command string is the part after the "?" in the URL
/// </summary>
public sealed class CustomHubCommand : HubCommand
{
    public const string Name = "Custom";
    public const string Help = $"<CommandType (0|1|2|3)> <CommandString>";
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return $"Type: {IMCommandType} Command: {IMCommandParams}"; }

    public CustomHubCommand(Gateway gateway, int commandType, string commandString
    ) : base(gateway)
    {
        switch(commandType)
        {
            case 0:
                IMCommandType = IMCommandTypes.Short;
                break;
            case 1:
                IMCommandType = IMCommandTypes.Hub;
                break;
            case 2:
                IMCommandType = IMCommandTypes.HubConfig;
                break;
            case 3:
            default:
                IMCommandType = IMCommandTypes.IM;
                break;
        }
        IMResponseLength = 3;
        IMCommandCode = 0;
        IMCommandParams = commandString;
        RequestClearBuffer = true;
    }

    private protected override void Done()
    {
        base.Done();
    }
}
