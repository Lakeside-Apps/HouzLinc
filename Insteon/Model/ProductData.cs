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
using Insteon.Commands;

namespace Insteon.Model;

/// <summary>
/// Holds immutable product identification data.
/// </summary>
public class ProductData
{
    internal ProductData() { }

    internal ProductData(ProductData productData)
    {
        CategoryId = productData.CategoryId;
        SubCategory = productData.SubCategory;
        ProductKey = productData.ProductKey;
        Revision = productData.Revision;
    }

    public virtual DeviceKind.CategoryId CategoryId { get; set; }
    public virtual int SubCategory { get; set; }
    public virtual int ProductKey { get; set; }
    public virtual int Revision { get; set; }

    public bool IsHub => DeviceKind.GetModelType(CategoryId, SubCategory) == DeviceKind.ModelType.Hub;

    public bool IsKeypadLinc =>
        DeviceKind.GetModelType(CategoryId, SubCategory) == DeviceKind.ModelType.KeypadLincDimmer ||
        DeviceKind.GetModelType(CategoryId, SubCategory) == DeviceKind.ModelType.KeypadLincRelay;

    public bool IsRemoteLinc =>
        DeviceKind.GetModelType(CategoryId, SubCategory) == DeviceKind.ModelType.RemoteLinc;

    public bool IsMultiChannelDevice => IsHub || IsKeypadLinc || IsRemoteLinc;

    public bool IsDimmableLightingControl => CategoryId == DeviceKind.CategoryId.DimmableLightingControl;

    /// <summary>
    /// Whether this device supports product data request.
    /// Remotelinc 2 does not appear to support it.
    /// </summary>
    internal virtual bool HasProductDataRequest => true;

    /// <summary>
    /// Factory method to create a ProductData object from a device (not the IM)
    /// </summary>
    /// <param name="house"></param>
    /// <param name="deviceId"></param>
    /// <returns></returns>
    internal static async Task<ProductData?> GetDeviceProductDataAsync(House house, InsteonID deviceId)
    {
        var command = new GetProductDataCommand(house.Gateway, deviceId) { MockPhysicalDevice = house.GetMockPhysicalDevice(deviceId) };
        if (await command.TryRunAsync(maxAttempts: 1))
        {
            var productData = new ProductData()
            {
                CategoryId = command.CategoryId,
                SubCategory = command.SubCategory,
                ProductKey = command.ProductKey,
                Revision = command.FirmwareRev
            };
            return productData;
        }
        return null;
    }

    /// <summary>
    /// Factory method to create a ProductData object for the IM (hub)
    /// </summary>
    /// <param name="house"></param>
    /// <returns></returns>
    internal static async Task<ProductData?> GetIMProductDataAsync(House house)
    {
        var command = new GetIMInfoCommand(house.Gateway);
        if (await command.TryRunAsync(maxAttempts: 1))
        {
            var productData = new ProductData()
            {
                CategoryId = command.CategoryId,
                SubCategory = command.Subcategory,
                ProductKey = 0,
                Revision = command.FirmwareRevision
            };
            return productData;
        }
        return null;
    }
}


