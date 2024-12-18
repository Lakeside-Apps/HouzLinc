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

using Insteon.Model;

namespace Insteon.Serialization.Houselinc;

/// <summary>
/// Collection of Devices in sequence and searchable by InsteonID
/// </summary>
public sealed class HLDevices : List<HLDevice>
{
    public HLDevices() { }

    public HLDevices(Devices devices)
    {
        foreach(Device device in devices)
        {
            Add(new HLDevice(device));
        }
    }

    public Devices BuildModel(House house)
    {
        var devices = new Devices(house);
        foreach(var hlDevice in this)
        {
            devices.Add(hlDevice.BuildModel(house, devices));
        }
        return devices;
    }
}
