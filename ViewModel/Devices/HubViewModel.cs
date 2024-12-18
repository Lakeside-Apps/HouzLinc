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

namespace ViewModel.Devices;

public sealed class HubViewModel : DeviceViewModel
{
    public HubViewModel(Device device) : base(device)
    {
    }

    static public bool IsA(Device d)
    {
        return d.IsHub;
    }

    /// <summary>
    /// Return a string describing the type of channels on this device, if any
    /// </summary>
    public override string ChannelType => "Channel";

    // Return a the view model of given channel on this device
    protected override ChannelViewModel? GetChannelViewModel(int id)
    {
        return ChannelViewModels[id];
    }
}
