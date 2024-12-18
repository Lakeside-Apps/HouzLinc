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

public sealed class GetIMInfoCommand : HubCommand
{
    public const string Name = "GetIMInfo";
    public const string Help = "";

    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return ""; }

    public GetIMInfoCommand(Gateway gateway) : base(gateway)
    {
        IMCommandType = IMCommandTypes.IM;
        IMCommandCode = IMCommandCode_GetIMInfo;
        IMResponseLength = 6;
        IMCommandParams = "";
    }

    private protected override void Done()
    {
        base.Done();
        LogOutput(InsteonID + ", Category: " + CategoryId + ", Subcategory: " + Subcategory + ", Revision: " + FirmwareRevision);
    }


    /// <summary>
    /// Command results
    /// Will throw if accessed before command completed successfully
    /// </summary>
    internal InsteonID InsteonID => new InsteonID(IMResponse.BytesAsString(1, 3));
    internal DeviceKind.CategoryId CategoryId => (DeviceKind.CategoryId)(IMResponse.Byte(4));
    internal byte Subcategory => IMResponse.Byte(5);
    internal byte FirmwareRevision => IMResponse.Byte(6);
}
