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
/// Command to cancel all-linking operation of the IM with a device
/// </summary>
public sealed class CancelIMAllLinkingCommand : HubCommand
{
    // Command logging 
    public const string Name ="CancelIMLinking";
    public const string Help = "";
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return ""; }

    public CancelIMAllLinkingCommand(Gateway gateway) : base(gateway)
    {
        IMCommandType = IMCommandTypes.IM;
        IMCommandCode = IMCommandCode_CancelAllLinking;
    }

    private protected override void Done()
    {
        if (ErrorReason == ErrorReasons.NoError)
        {
            LogOutput("Cancelled IM Linking");
        }
    }
}
