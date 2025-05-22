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
using System.Diagnostics.CodeAnalysis;

namespace Insteon.Model;

public sealed class Gateways : List<Gateway>
{
    internal Gateways(House house)
    {
        House = house;
    }

    // Copy state from another Gateway object.
    // This is similar to Devices.CopyFrom and Scenes.CopyFrom
    internal void CopyFrom(Gateways fromGateways)
    {
        var gateways2 = new Gateways(House);
        foreach (var gateway in fromGateways)
        {
            gateways2.Add(gateway);
        }

        var gatewaysToRemove = new List<Gateway>();
        foreach (var gateway in this)
        {
            if (gateways2.TryGetEntry(gateway.GetHashCode(), out var fromGateway))
            {
                gateway.CopyFrom(fromGateway);
                gateways2.Remove(fromGateway);
            }
            else
            {
                gatewaysToRemove.Add(gateway);
            }
        }

        foreach (var gateway in gatewaysToRemove)
        {
            Remove(gateway);
        }

        foreach (var gateway in gateways2)
        {
            var newGateway = new Gateway(House);
            Add(newGateway);
            newGateway.CopyFrom(gateway);
        }
    }

    private House House { get; init; }

    internal bool IsIdenticalTo(Gateways gateways)
    {
        if (gateways.Count != Count)
        {
            return false;
        }

        for (int i = 0; i < Count; i++)
        {
            if (!this[i].IsIdenticalTo(gateways[i]))
            {
                return false;
            }
        }

        return true;
    }

    internal void OnDeserialized()
    {
        foreach (Gateway gateway in this)
        {
            gateway.OnDeserialized();
        }

        AddObserver(House.ModelObserver);
    }

    internal Gateways Ensure()
    {
        Add(new Gateway(House));
        return this;
    }

    /// <summary>
    /// Observers can subscribe to change notifications
    /// </summary>
    /// <param name="gatewaysObserver"></param>
    public Gateways AddObserver(IGatewaysObserver gatewaysObserver)
    {
        observers.Add(gatewaysObserver);
        return this;
    }
    private List<IGatewaysObserver> observers = new List<IGatewaysObserver>();

    /// <summary>
    /// Notify all observers that a g has changed
    /// </summary>
    /// <param name="gateway"></param>
    public void NotifyObservers(Gateway gateway)
    {
        observers.ForEach(o => o.GatewayChanged(gateway));
    }

    public bool TryGetEntry(int key, [NotNullWhen(true)] out Gateway? gateway)
    {
        gateway = null;
        foreach (var g in this)
        {
            if (g.GetHashCode() == key)
            {
                gateway = g;
                return true;
            }
        }
        return false;
    }
}
