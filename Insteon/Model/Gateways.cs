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
using System;

namespace Insteon.Model;

public sealed class Gateways : List<Gateway>
{
    internal Gateways(House house)
    {
        House = house;
    }

    internal void CopyFrom(Gateways fromGateways)
    {
        Clear();
        foreach (Gateway gateway in fromGateways)
        {
            Add(gateway);
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
    /// Notify all observers that a gateway has changed
    /// </summary>
    /// <param name="gateway"></param>
    public void NotifyObservers(Gateway gateway)
    {
        observers.ForEach(o => o.GatewayChanged(gateway));
    }
}
