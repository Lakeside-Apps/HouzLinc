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

namespace Insteon.Model;

/// <summary>
/// This class stores properties value pairs for a Device and handles persistence.
/// These properties are exposed to the user interface by the Device class and
/// property change notifications are sent to observers by the Device class.
/// </summary>
public sealed class DeviceProperties : List<DeviceProperty>
{
    internal DeviceProperties() { }

    internal DeviceProperties(DeviceProperties properties)
    {
        foreach (var property in properties)
        {
            var newProperty = new DeviceProperty
            {
                Name = property.Name,
                Value = property.Value,
                LastUpdate = property.LastUpdate,
                PendingValue = property.PendingValue
            };
            Add(newProperty);
        }
    }

    internal void CopyFrom(DeviceProperties properties)
    {
        Clear();
        foreach (var property in properties)
        {
            var newProperty = new DeviceProperty
            {
                Name = property.Name,
                Value = property.Value,
                LastUpdate = property.LastUpdate,
                PendingValue = property.PendingValue
            };
            Add(newProperty);
        }
    }

    // This updates the value of a property in the bag, along with the lastUpdate time.
    // Return true if the property was changed.
    // If lastUpdate is default, the current time is used.
    internal bool SetValue(string name, byte value, DateTime lastUpdate = default)
    {
        var property = this.FirstOrDefault(p => p.Name == name);
        if (property != null)
        {
            if (property.Value != value || lastUpdate != default)
            {
                property.Value = value;
                property.LastUpdate = lastUpdate != default ? lastUpdate : DateTime.Now;
                return true;
            }
        }
        else if (value != 0 || lastUpdate != default)
        {
            property = new DeviceProperty
            {
                Name = name,
                Value = value,
                LastUpdate = lastUpdate != default ? lastUpdate : DateTime.Now
            };
            Add(property);
            return true;
        }

        return false;
    }

    internal byte GetValue(string propertyName)
    {
        var property = this.FirstOrDefault(p => p.Name == propertyName);
        if (property != null)
        {
            return property.Value;
        }
        return 0;
    }

    internal byte GetValue(string propertyName, out DateTime lastUpdate)
    {
        lastUpdate = default;
        var property = this.FirstOrDefault(p => p.Name == propertyName);
        if (property != null)
        {
            lastUpdate = property.LastUpdate;
            return property.Value;
        }
        return 0;
    }

    internal bool Exists(string propertyName)
    {
        return this.Any(p => p.Name == propertyName);
    }
}

public sealed class DeviceProperty
{
    internal string Name { get; init; } = string.Empty;

    internal byte Value { get; set; } = 0;

    internal DateTime LastUpdate { get; set; }

    // Only used for roundtrip to houselinc.xml
    internal byte PendingValue { get; init; }
}
