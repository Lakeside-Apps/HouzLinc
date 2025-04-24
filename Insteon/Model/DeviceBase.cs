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

namespace Insteon.Model;

/// <summary>
/// This class represents a basic Insteon device with InsteonID, product information and operating flags
/// That definition is shared between logical devices in the model, and physical devices representing
/// the actual physical device.
/// </summary>
public abstract class DeviceBase : ProductData
{
    internal DeviceBase(InsteonID id) : base()
    {
        Id = id;
        OperatingFlags = 0;
    }

    internal DeviceBase(InsteonID id, ProductData productData) : base(productData)
    {
        Id = id;
        OperatingFlags = 0;
    }

    /// <summary>
    /// Equality comparer and hash code to put Device in a HashSet
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object? obj)
    {
        if (obj is Device d)
            return Id == d.Id;
        else
            return false;
    }

    public override int GetHashCode()
    {
        return Id.ToInt();
    }

    public static int GetHashCodeFromId(InsteonID deviceId)
    {
        return deviceId.ToInt();
    }

    /// <summary>
    /// Common device properties
    /// </summary>
    public virtual InsteonID Id { get; set; }
    public virtual Bits OperatingFlags { get; set; }
    public virtual Bits OpFlags2 { get; set; }
    public virtual byte LEDBrightness { get; set; }
    public virtual byte OnLevel { get; set; }
    public virtual byte RampRate { get; set; }
}
