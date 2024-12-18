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
using System.Diagnostics;

namespace Insteon.Commands;

/// <summary>
///  Command to request product data
///  Returns product key, device category and subcategory
/// </summary>
public sealed class GetProductDataCommand : DeviceCommand
{
    public const string Name = "GetProductData";
    public const string Help = "<DeviceID>";
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return ""; }

    public GetProductDataCommand(Gateway gateway, InsteonID deviceID) : base(gateway, deviceID)
    {
        Command1 = CommandCode_ProductDataRequest;
        Command2 = 0x00;
        ExpectExtendedResponseMessage = true;
    }

    private protected override async Task<bool> RunAsync()
    {
        if (MockPhysicalDevice != null)
        {
            // Mock implementation of this command for testing purposes only
            // Simulate a response from the device
            OnExtendedResponseReceived(new InsteonExtendedMessage(
                    InsteonMessage.BuildHexString(ToDeviceID, InsteonID.Null, (byte)MessageType.Direct | (byte)MessageLength.Extended, command1: CommandCode_ProductDataRequest, command2: 0, 
                    data: $"00{MockPhysicalDevice.ProductKey:X6}{(byte)MockPhysicalDevice.CategoryId:X2}{(byte)MockPhysicalDevice.SubCategory:X2}{(byte)MockPhysicalDevice.Revision:X2}")));
            return true;
        }
        return await base.RunAsync();
    }

    /// <summary>
    /// Called when an extended response message is received
    /// See base class for details
    /// </summary>
    private protected override bool OnExtendedResponseReceived(InsteonExtendedMessage message)
    {
        // Checks that the extended response message contains valid category and subcategory
        if ((message.DataByte(5) != 0 || message.DataByte(6) != 0) && 
             message.DataByte(5) < (int)DeviceKind.CategoryId.Count)
        {
            return base.OnExtendedResponseReceived(message);
        }

        return false;
    }

    private protected override void Done()
    {
        base.Done();
        Debug.Assert(CategoryId != DeviceKind.CategoryId.GeneralizedControllers || SubCategory != 0);
        LogOutput(ExtendedResponseMessage.FromDeviceId.ToString() + ", Product Key: " + ProductKey + ", Category: " + CategoryId + ": " + CategoryName + ", Subcategory: " + SubCategory + ", Model: " + ModelName + " (" + ModelNumber + ")" + ", FirmwareRev: " + FirmwareRev);
    }

    /// <summary>
    /// Command results
    /// Will throw if accessed before command completed successfully
    /// </summary>
    internal int ProductKey => ExtendedResponseMessage.DataByte(2) << 16 + ExtendedResponseMessage.DataByte(3) << 8 + ExtendedResponseMessage.DataByte(4);
    internal DeviceKind.CategoryId CategoryId => (DeviceKind.CategoryId)ExtendedResponseMessage.DataByte(5);
    internal string CategoryName => DeviceKind.GetCategoryName(CategoryId);
    internal int SubCategory => ExtendedResponseMessage.DataByte(6);
    internal string ModelName => DeviceKind.GetModelName(CategoryId, SubCategory); 
    internal string ModelNumber => DeviceKind.GetModelNumber(CategoryId, SubCategory);
    internal int FirmwareRev => ExtendedResponseMessage.DataByte(7);
}
