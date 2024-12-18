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

public sealed class HLChannels : List<HLChannel>
{
    internal HLChannels() { }

    internal HLChannels(Channels channels)
    {
        foreach(var channel in channels)
        {
            Add(new HLChannel(channel));
        }
    }

    internal Channels BuildModel(Device device)
    {
        var channels = new Channels();
        foreach (var hlChannel in this)
        {
            channels.Add(hlChannel.BuildModel(device));
        }
        return channels;
    }
}
