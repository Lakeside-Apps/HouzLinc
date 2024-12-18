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

public sealed class GetPropertiesForGroupCommand : DeviceCommand
{
    public const string Name = "GetPropertiesForGroup";
    public const string Help = "<DeviceID> <Group>";
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return "Group: " + Group.ToString(); }

    public GetPropertiesForGroupCommand(Gateway gateway,InsteonID deviceID, byte group) : base(gateway, deviceID)
    {
        Command1 = CommandCode_GetForGroup;
        Command2 = 0;
        Group = group;
        ExpectExtendedResponseMessage = true;
    }

    private protected override async Task<bool> RunAsync()
    {
        if (MockPhysicalDevice != null)
        {
            // Mock implementation of this command for testing purposes only
            // Simulate a response from the device
            // For now this only responds with the global LED brightness. 
            // TODO: implement other properties
            OnExtendedResponseReceived(new InsteonExtendedMessage(
                    InsteonMessage.BuildHexString(ToDeviceID, InsteonID.Null, (byte)MessageType.Direct | (byte)MessageLength.Extended, command1: CommandCode_SetForGroup, command2: 0,
                    data: $"{Group:X2}01000000000000{MockPhysicalDevice.LEDBrightness:X2}0000000000")));
            return true;
        }
        return await base.RunAsync();
    }

    /// <summary>
    /// Called when an extended respons message is received
    /// See base class for details
    /// </summary>
    private protected override bool OnExtendedResponseReceived(InsteonExtendedMessage message)
    {
        // Checks that the extended response message contains valid data
        if (message.DataByte(1) == Group && message.DataByte(2) == 0x01)
        {
            return base.OnExtendedResponseReceived(message);
        }

        return false;
    }

    private protected override void Done()
    {
        base.Done();
        LogOutput(ExtendedResponseMessage.FromDeviceId.ToString() + ", Button: " + ResponseGroup.ToString());
        LogOutput(
                    "Follow Bit Mask: " + Convert.ToString(FollowMask, 2) + "\r\n" +
                    "Follow On/Off Bit Mask: " + Convert.ToString(FollowOffMask, 2) + "\r\n" +
                    "X10 House Code: " + X10HouseCode.ToString("X2") + "\r\n" +
                    "X10 Unit: " + X10Unit.ToString("X2") + "\r\n" +
                    "Ramp Rate: " + RampRate.ToString() + "\r\n" +
                    "On-Level: " + OnLevel.ToString() + "\r\n" +
                    "Global LED Brightness: " + LEDBrightness.ToString() + " (Group ignored)\r\n" +
                    "Non-Toggle Mask: " + Convert.ToString(NonToggleMask, 2) + "\r\n" +
                    "LED bit Mask: " + Convert.ToString(LEDOnMask, 2) + "\r\n" +
                    "X10 All Bit Mask: " + Convert.ToString(X10AllMask, 2) + "\r\n" +
                    "On/Off Bit Mask: " + Convert.ToString(OnOffMask, 2) + "\r\n" +
                    "Trigger Bit Mask: " + Convert.ToString(TriggerAllLinkMask, 2));
    }

    internal byte Group
    {
        get => DataByte(1);
        set => SetDataByte(1, value);
    }

    /// <summary>
    /// Command results
    /// Will throw if accessed before command completed successfully
    /// </summary>
    internal byte ResponseGroup => ExtendedResponseMessage.DataByte(1);
    internal byte FollowMask => ExtendedResponseMessage.DataByte(3);
    internal byte FollowOffMask => ExtendedResponseMessage.DataByte(4);
    internal byte RampRate => ExtendedResponseMessage.DataByte(7);
    internal byte OnLevel => ExtendedResponseMessage.DataByte(8);
    internal byte LEDBrightness => ExtendedResponseMessage.DataByte(9);
    internal byte NonToggleMask => ExtendedResponseMessage.DataByte(10);
    internal byte OnOffMask => ExtendedResponseMessage.DataByte(13);
    internal byte LEDOnMask => ExtendedResponseMessage.DataByte(11);
    internal byte X10HouseCode => ExtendedResponseMessage.DataByte(5);
    internal byte X10Unit => ExtendedResponseMessage.DataByte(6);
    internal byte X10AllMask => ExtendedResponseMessage.DataByte(12);
    internal byte TriggerAllLinkMask => ExtendedResponseMessage.DataByte(14);
}
