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

using System.Diagnostics;

namespace Insteon.Model;

/// <summary>
/// Represents a list of channels for a given device
/// </summary>
public sealed class Channels : List<Channel>
{
    internal Channels() { }

    internal Channels(Device device)
    {
        Device = device;
    }

    internal void OnDeserialized()
    {
        ForEach(channel => channel.OnDeserialized());
        AddChannelObserver(Device.House.ModelObserver);
    }

    // Deep copy constructor
    internal Channels(Channels channels)
    {
        foreach (var channel in channels)
        {
            Add(new Channel(this, channel));
        }
    }

    // Copy from another list of channels of same count.
    // We consider the other list the source of thruth.
    internal void CopyFrom(Channels fromChannels)
    {
        Debug.Assert(Count == fromChannels.Count);

        for (int i = 0; i < Count; i++)
        {
            this[i].CopyFrom(fromChannels[i]);
        }
    }

    // Whether this list of channels is identical to another list of channels
    // Used primarily for testing and DEBUG checks
    internal bool IsIdenticalTo(Channels channels)
    {
        if (channels.Count != Count)
        {
            return false;
        }

        for (int i = 0; i < Count; i++)
        {
            if (!this[i].IsIdenticalTo(channels[i]))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Device that owns these channels
    /// </summary>
    public Device Device { get; set; } = null!;

    /// <summary>
    /// Observers can subscribe to change notifications on individual channels
    /// </summary>
    /// <param name="observer"></param>
    /// <returns>this, to allow chaining behind new() or other methods returning this</returns>
    public Channels AddChannelObserver(IChannelObserver observer)
    {
        foreach(var channel in this)
        {
            channel.AddObserver(observer);
        }
        return this;
    }

    /// <summary>
    /// Marks all channels as changed, e.g., when replacing or copying of a device
    /// </summary>
    /// <returns>this, to allow chaining behind new() or other methods returning this</returns>
    internal Channels MarkAllChannelsChanged()
    {
        foreach (var channel in this)
        {
            channel.PropertiesSyncStatus = SyncStatus.Changed;
        }
        return this;
    }
}
