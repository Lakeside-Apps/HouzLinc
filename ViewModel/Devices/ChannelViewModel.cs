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

using System.Runtime.CompilerServices;
using System.Diagnostics;
using Insteon.Model;
using ViewModel.Base;
using System.ComponentModel;

namespace ViewModel.Devices;

[Bindable(true)]
public sealed class ChannelViewModel : BaseViewModel, IChannelObserver
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="deviceViewModel">DeviceViewModel holding this channelViewModel</param>
    /// <param name="channelId">1-based channel id, 0 means empty/no channel</param>
    internal ChannelViewModel(DeviceViewModel deviceViewModel, int channelId)
    {
        this.deviceViewModel = deviceViewModel;

        channel = deviceViewModel.Device.GetChannel(channelId);
        channel.AddObserver(this);
    }

    private DeviceViewModel deviceViewModel;
    private Channel channel;

    // Job handle to read properties
    private object? readCurrentChannelPropertiesJob;

    /// <summary>
    /// Notifies that this channel has become the current one in the UI
    /// </summary>
    internal void ActiveStateChanged(bool isActive)
    {
        if (isActive && channel != null)
        {
            // Only attempt to read information from the device if it is connected
            deviceViewModel.Device.ScheduleRetrieveConnectionStatus(status =>
            {
                if (status == Device.ConnectionStatus.Connected)
                {
                    // Schedule a job to read the channel properties in 10 seconds
                    // That job will be cancelled if the channel or device is deactivated before
                    readCurrentChannelPropertiesJob = deviceViewModel.Device.ScheduleReadChannelProperties(
                        channel.Id,
                        (bool success) => { readCurrentChannelPropertiesJob = null; },
                        delay: new TimeSpan(0, 0, 10),
                        forceSync: false);
                }
            },
            force: true);
        }
        else if (readCurrentChannelPropertiesJob != null)
        {
            Device.CancelScheduledJob(readCurrentChannelPropertiesJob);
            readCurrentChannelPropertiesJob = null;
        }
    }

    /// <summary>
    /// Name property
    /// Read/write (two-way bindable) property, synced with the device
    /// Change notification fired in OnChannelPropertyChanged
    /// </summary>
    public string Name
    {
        get => channel.Name ?? channel.DefaultName;
        set
        {
            Debug.Assert(nameof(Name) == nameof(channel.Name));
            channel.Name = value; 
        }
    }

    /// <summary>
    /// Id Property
    /// Read only, one time bindable
    /// </summary>
    public int Id
    {
        get => channel.Id;
    }

    public string IdString
    {
        get => Id.ToString();
    }

    public string IdAndName
    {
        get => $"{IdString} - {Name}";
    }

    /// <summary>
    /// FollowMask
    /// Read/write (two-way bindable) property, synced with the device
    /// Change notification fired in OnChannelPropertyChanged
    /// </summary>
    public int FollowMask
    {
        get => channel.FollowMask;
        set
        {
            Debug.Assert(nameof(FollowMask) == nameof(channel.FollowMask));
            channel.FollowMask = value;
        }
    }

    /// <summary>
    /// FollowOffMask
    /// Read/write (two-way bindable) property, synced with the device
    /// </summary>
    public int FollowOffMask
    {
        get => channel.FollowOffMask;
        set
        {
            Debug.Assert(nameof(FollowOffMask) == nameof(channel.FollowOffMask));
            channel.FollowOffMask = value;
        }
    }

    /// <summary>
    /// ToggleMode
    /// Read/write (two-way bindable) property, synced with the device
    /// </summary>
    public ToggleMode ToggleMode
    {
        get => channel.ToggleMode;
        set
        {
            Debug.Assert(nameof(ToggleMode) == nameof(channel.ToggleMode));
            channel.ToggleMode = value;
        }
    }

    public int ToggleModeAsInt
    {
        get => (int)ToggleMode;
        set { ToggleMode = (ToggleMode)value; }
    }

    public string ToggleModeAsString
    {
        get
        {
            switch (ToggleMode)
            {
                case ToggleMode.Off:
                    return "Off";
                case ToggleMode.On:
                    return "On";
                default:
                    return "Toggle";
            }
        }
    }

    /// <summary>
    /// OnLevel
    /// Read/write (two-way bindable) property, synced with the device
    /// </summary>
    public int OnLevel
    {
        get => channel.OnLevel;
        set
        {
            Debug.Assert(nameof(OnLevel) == nameof(channel.OnLevel));
            channel.OnLevel = value; 
        }
    }

    /// <summary>
    /// RampRate
    /// Read/write (two-way bindable) property, synced with the device
    /// </summary>
    public int RampRate
    {
        get => channel != null ? channel.RampRate : 0;
        set
        {
            Debug.Assert(nameof(RampRate) == nameof(channel.RampRate));
            channel.RampRate = value;
        }
    }

    public string RampRateAsString
    {
        get => ViewModel.Base.RampRate.ToString(RampRate);
        set { RampRate = ViewModel.Base.RampRate.FromString(value); }
    }

    /// <summary>
    /// Are channel properties synced
    /// Read/write, one way bindable
    /// </summary>
    internal bool ArePropertiesSynced
    {
        get => arePropertiesSynced;
        private set
        {
            if (value != arePropertiesSynced)
            {
                arePropertiesSynced = value;
                OnPropertyChanged();
            }
        }
    }
    private bool arePropertiesSynced = true;

    // Implementation of IChannelObserver
    public void ChannelPropertyChanged(Channel channel, [CallerMemberName] string? propertyName = null)
    {
        OnPropertyChanged(propertyName);
    }

    public void ChannelSyncStatusChanged(Channel channel)
    {
        ArePropertiesSynced = channel.PropertiesSyncStatus == SyncStatus.Synced;
    }

    /// <summary>
    /// Whether this channel button controls the load, either directly or through follow behavior.
    /// </summary>
    public bool IsLoadControlling => (deviceViewModel as KeypadLincViewModel)?.IsCurrentButtonLoadControlling ?? false;
    public void OnIsLoadControllingChanged()
    {
        OnPropertyChanged(nameof(IsLoadControlling));
    }
}
