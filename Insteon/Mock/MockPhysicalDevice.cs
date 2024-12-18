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
using Insteon.Model;

namespace Insteon.Mock;

/// <summary>
/// Simulates a real physical device for testing purposes. 
/// Commands can take a MockPhysicalDevice as a parameter and will be simulated on a mock device.
/// </summary>
public class MockPhysicalDevice : DeviceBase
{
    public MockPhysicalDevice(InsteonID Id) : base(Id)
    {
        this.AllLinkDatabase = new AllLinkDatabase();
    }

    public MockPhysicalDevice(InsteonID Id, AllLinkDatabase allLinkDatabase) : base(Id)
    {
        this.AllLinkDatabase = allLinkDatabase;
    }

    internal AllLinkDatabase AllLinkDatabase;
}
