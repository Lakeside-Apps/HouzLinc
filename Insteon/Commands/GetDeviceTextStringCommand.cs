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
///  Command to request the Device Text String 
///  Returns text string describing device
/// </summary>
public sealed class GetDeviceTextStringCommand : DeviceCommand
{
    public const string Name = "GetTextString";
    public const string Help = "<DeviceID>";
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return ""; }

    public GetDeviceTextStringCommand(Gateway gateway, InsteonID deviceID) : base(gateway, deviceID)
    {
        Command1 = CommandCode_ProductDataRequest;
        Command2 = CommandCode2_DeviceStringRequest;
        ExpectExtendedResponseMessage = true;
    }

    private protected override void Done()
    {
        base.Done();
        LogOutput(ExtendedResponseMessage.FromDeviceId.ToString() + ", Text String: " + TextString);
    }

    internal string TextString
    {
        get
        {
            if (ExtendedResponseMessage != null)
            {
                string s = "";
                for (int i = 1; i <= 14; i++)
                {
                    s += (char)ExtendedResponseMessage.DataByte(i);
                }
                return s;
            }
            return string.Empty;
        }
    }
}

