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
/// Set overall LED brightness
/// Brightness values appear to go from 1 to 127 (0x7F), even though the doc says 0x11 to 0x7F.
/// Value 0 seems to be reserved for a default brightness or something of the sort.
/// </summary>
public sealed class SetLEDBrightnessCommand : DeviceCommand
{
    public const string Name = "SetLEDBrightness";
    public const string Help = "<DeviceID> <Brightness>(1-127)";
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return "Brightness: " + Brightness.ToString(); }

    public SetLEDBrightnessCommand(Gateway gateway, InsteonID deviceID, byte brightness) : base(gateway, deviceID)
    {
        Command1 = CommandCode_SetForGroup;
        Command2 = 0;
        SetDataByte(2, CommandData2_SetLEDBrightness);
        Brightness = brightness;
        ExpectExtendedResponseMessage = false;
    }

    private protected override async Task<bool> RunAsync()
    {
        if (MockPhysicalDevice != null)
        {
            // Mock implementation of this command for testing purposes only
            MockPhysicalDevice.LEDBrightness = Brightness;
            return true;
        }
        return await base.RunAsync();
    }

    private protected override void Done()
    {
        base.Done();
        LogOutput("Done!");
    }

    internal byte Brightness
    {
        get => DataByte(3);
        private set => SetDataByte(3, value);
    }
}
