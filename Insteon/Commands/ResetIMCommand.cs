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

public sealed class ResetIMCommand : HubCommand
{
    public const string Name = "ResetIM";
    public const string Help = "";

    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return ""; }

    public ResetIMCommand(Gateway gateway) : base(gateway)
    {
        IMCommandType = IMCommandTypes.IM;
        IMCommandCode = IMCommandCode_ResetIM;
        IMResponseLength = 0;
        IMCommandParams = "";
    }

    private protected override void Done()
    {
        base.Done();
        LogOutput("Done!");
    }
}
