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

public sealed class HLGateways : List<HLGateway>
{
    public HLGateways() { }

    public HLGateways(Gateways gateways)
    {
        foreach(var gateway in gateways)
        {
            Add(new HLGateway(gateway));
        }
    }

    public Gateways BuildModel(House house)
    {
        var gateways = new Gateways(house);
        foreach(var hlGateway in this)
        {
            gateways.Add(hlGateway.BuildModel());
        }
        return gateways;
    }
}
