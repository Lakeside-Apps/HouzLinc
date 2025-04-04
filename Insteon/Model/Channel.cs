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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Common;
using Insteon.Commands;
using Insteon.Drivers;
using Insteon.Synchronization;

namespace Insteon.Model;

public sealed class Channel : ChannelBase
{
    internal Channel(Channels channels, bool fromDeserialization = false)
    {
        this.Channels = channels;
        isDeserialized = !fromDeserialization;
    }

    // Copy constructor
    // This does NOT sent property change notifications to observers
    internal Channel(Channels channels, Channel fromChannel) : this(channels)
    {
        Id = fromChannel.Id;
        name = fromChannel.Name;
        followMask = fromChannel.FollowMask;
        FollowMaskLastUpdate = fromChannel.FollowMaskLastUpdate;
        followOffMask = fromChannel.FollowOffMask;
        FollowOffMaskLastUpdate = fromChannel.FollowOffMaskLastUpdate;
        toggleMode = fromChannel.ToggleMode;
        ToggleModeLastUpdate = fromChannel.ToggleModeLastUpdate;
        ledOn = fromChannel.LEDOn;
        LEDOnLastUpdate = fromChannel.LEDOnLastUpdate;
        onLevel = fromChannel.OnLevel;
        OnLevelLastUpdate = fromChannel.OnLevelLastUpdate;
        rampRate = fromChannel.RampRate;
        RampRateLastUpdate = fromChannel.RampRateLastUpdate;
        propertiesSyncStatus = fromChannel.propertiesSyncStatus;
    }

    // Copy state from another channel
    // This generates observer notifications for all properties that changed
    internal void CopyFrom(Channel fromChannel)
    {
        Name = fromChannel.Name;
        FollowMask = fromChannel.FollowMask;
        FollowMaskLastUpdate = fromChannel.FollowMaskLastUpdate;
        FollowOffMask = fromChannel.FollowOffMask;
        FollowOffMaskLastUpdate = fromChannel.FollowOffMaskLastUpdate;
        ToggleMode = fromChannel.ToggleMode;
        ToggleModeLastUpdate = fromChannel.ToggleModeLastUpdate;
        LEDOn = fromChannel.LEDOn;
        LEDOnLastUpdate = fromChannel.LEDOnLastUpdate;
        OnLevel = fromChannel.OnLevel;
        OnLevelLastUpdate = fromChannel.OnLevelLastUpdate;
        RampRate = fromChannel.RampRate;
        RampRateLastUpdate = fromChannel.RampRateLastUpdate;
        PropertiesSyncStatus = fromChannel.propertiesSyncStatus;
    }

    // Whether this is strictly identical to another channel
    // Used for testing and DEBUG checks
    internal bool IsIdenticalTo(Channel channel)
    {
        return Id == channel.Id &&
            name == channel.Name &&
            followMask == channel.FollowMask &&
            FollowMaskLastUpdate == channel.FollowMaskLastUpdate &&
            followOffMask == channel.FollowOffMask &&
            FollowOffMaskLastUpdate == channel.FollowOffMaskLastUpdate &&
            toggleMode == channel.ToggleMode &&
            ToggleModeLastUpdate == channel.ToggleModeLastUpdate &&
            ledOn == channel.LEDOn &&
            LEDOnLastUpdate == channel.LEDOnLastUpdate &&
            onLevel == channel.OnLevel &&
            OnLevelLastUpdate == channel.OnLevelLastUpdate &&
            rampRate == channel.RampRate &&
            RampRateLastUpdate == channel.RampRateLastUpdate &&
            propertiesSyncStatus == channel.propertiesSyncStatus;
    }

    internal void OnDeserialized()
    {
        isDeserialized = true;
        AddObserver(House.ModelObserver);
    }
    private bool isDeserialized;

    // Channel list this channel is part of
    private Channels Channels { get; init; }

    // Access to the device
    internal Device Device => Channels.Device;

    /// <summary>
    /// Observers can subscribe to change notifications
    /// </summary>
    /// <param name="observer"></param>
    public Channel AddObserver(IChannelObserver observer)
    {
        observers.Add(observer);
        return this;
    }
    private List<IChannelObserver> observers = new List<IChannelObserver>();

    /// <summary>
    /// House this device channel belongs to
    /// </summary>
    public House House => Device.House;

    // Helpers to get physical device and channel
    private DeviceDriverBase DeviceDriver => Device.DeviceDriver;
    private ChannelDriver? ChannelDriver
    {
        get
        {
            if (channelDriver == null)
            {
                if (DeviceDriver is DeviceDriver pdp)
                {
                    if (pdp.Channels != null && Id > 0 && Id <= pdp.Channels.Length)
                    {
                        channelDriver = pdp.Channels[Id - 1];
                    }
                }
            }
            return channelDriver;
        }
    }
    private ChannelDriver? channelDriver = null;

    /// <summary>
    /// Id property
    /// Read/write, one time bindable
    /// </summary>
    public override int Id { get; init; }

    /// <summary>
    /// Name property
    /// Read/write, 2-way bindable, optional
    /// </summary>
    public string? Name 
    {
        get => name;
        set
        {
            if (value != name)
            {
                name = value;
                NotifyObserversOfPropertyChanged();
            }
        }
    }
    private string? name;

    /// <summary>
    /// Suggested default name if the channel does not have a name
    /// </summary>
    public string DefaultName => Name ?? DeviceDriver?.GetChannelDefaultName(Id) ?? string.Empty;

    /// <summary>
    /// FollowMask property
    /// Bit mask of buttons "following" (i.e., turning on) when this channel button is on
    /// Read/write, 2-way bindable
    /// </summary>
    public override int FollowMask
    {
        get => followMask;
        set 
        {
            if (followMask != value)
            {
                followMask = value;
                if (NotifyObserversOfPropertyChanged())
                {
                    FollowMaskLastUpdate = DateTime.Now;
                }
            }
        }
    }
    private int followMask;
    public DateTime FollowMaskLastUpdate { get; set; }
    public SyncStatus FollowMaskSyncStatus => 
        ArePropertiesRead ? ((FollowMask == ChannelDriver.FollowMask) ? SyncStatus.Synced : SyncStatus.Changed) : SyncStatus.Unknown;

    /// <summary>
    /// FollowMaskOff property
    /// Bit mask of buttons "following off" (i.e., turning off) when this channel button is on
    /// A bit in this mask is only taken into account if same bit is set in FollowMask
    /// Read/write, 2-way bindable
    /// </summary>
    public override int FollowOffMask 
    {
        get => followOffMask;
        set
        {
            if (followOffMask != value)
            {
                followOffMask = value;
                if (NotifyObserversOfPropertyChanged())
                {
                    FollowOffMaskLastUpdate = DateTime.Now;
                }
            }
        }
    }
    private int followOffMask;
    public DateTime FollowOffMaskLastUpdate { get; set; }
    public SyncStatus FollowOffMaskSyncStatus => 
        ArePropertiesRead ? ((FollowOffMask == ChannelDriver.FollowOffMask) ? SyncStatus.Synced : SyncStatus.Changed) : SyncStatus.Unknown;

    /// <summary>
    /// Toggle property
    /// Whther this channel button behaves as a toggle, always on, or always off
    /// Read/write, 2-way bindable
    /// </summary>
    public override ToggleMode ToggleMode
    {
        get => toggleMode;
        set
        {
            if (toggleMode != value)
            {
                toggleMode = value;
                if (NotifyObserversOfPropertyChanged())
                {
                    ToggleModeLastUpdate = DateTime.Now;
                }
            }
        }
    }
    private ToggleMode toggleMode;
    public DateTime ToggleModeLastUpdate { get; set; }
    public SyncStatus ToggleModeSyncStatus => 
        ArePropertiesRead ? ((ToggleMode == ChannelDriver.ToggleMode) ? SyncStatus.Synced : SyncStatus.Changed) : SyncStatus.Unknown;

    /// <summary>
    /// LEDOn property
    /// Whether this channel button is lit on or not
    /// Read/write, 2-way bindable
    /// </summary>
    public override bool LEDOn
    {
        get => ledOn; 
        set
        {
            if (ledOn != value)
            {
                ledOn = value;
                if (NotifyObserversOfPropertyChanged())
                {
                    LEDOnLastUpdate = DateTime.Now;
                }
            }
        }
    }
    private bool ledOn = true;
    public DateTime LEDOnLastUpdate { get; set; }
    public SyncStatus LEDOnSyncStatus =>
        ArePropertiesRead ? ((LEDOn == ChannelDriver.LEDOn) ? SyncStatus.Synced : SyncStatus.Changed) : SyncStatus.Unknown;

    /// <summary>
    /// OnLevel property
    /// OnLevel when turning on the load on this device
    /// Read/write, 2-way bindable
    /// </summary>
    public override int OnLevel
    {
        get => onLevel;
        set
        {
            if (onLevel != value)
            {
                onLevel = value;
                if (NotifyObserversOfPropertyChanged())
                {
                    OnLevelLastUpdate = DateTime.Now;
                }
            }
        }
    }
    private int onLevel;
    public DateTime OnLevelLastUpdate { get; set; }
    public SyncStatus OnLevelSyncStatus =>
        ArePropertiesRead ? ((OnLevel == ChannelDriver.OnLevel) ? SyncStatus.Synced : SyncStatus.Changed) : SyncStatus.Unknown;

    /// <summary>
    /// RampRate property
    /// RampRate when turning on the load on this device
    /// Read/write, 2-way bindable
    /// </summary>
    public override int RampRate
    {
        get => rampRate;
        set
        {
            if (rampRate != value)
            {
                rampRate = value;
                if (NotifyObserversOfPropertyChanged())
                {
                    RampRateLastUpdate = DateTime.Now;
                }
            }
        }
    }
    private int rampRate;
    public DateTime RampRateLastUpdate { get; set; }
    public SyncStatus RampRateSyncStatus =>
        ArePropertiesRead ? ((RampRate == ChannelDriver.RampRate) ? SyncStatus.Synced : SyncStatus.Changed) : SyncStatus.Unknown;

    // Helper to send notifications when a property value is changed
    // Returns whether nofitications were sent and sync status was updated
    private bool NotifyObserversOfPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        if (isDeserialized)
        {
            UpdatePropertiesSyncStatus();
            observers.ForEach(o => o.ChannelPropertyChanged(this, propertyName));
            return true;
        }
        return false;
    }

    /// <summary>
    /// Overall channel properties sync status
    /// Change notification is sent by UpdatePropertiesSyncStatus
    /// </summary>
    public SyncStatus PropertiesSyncStatus
    {
        get => propertiesSyncStatus;
        set
        {
            if (value != propertiesSyncStatus)
            {
                propertiesSyncStatus = value;
                observers.ForEach(o => o.ChannelSyncStatusChanged(this));
            }
        }
    }
    private SyncStatus propertiesSyncStatus;

    // Helper to update the overall sync status for the channel
    // See same method in Device for additional information
    private void UpdatePropertiesSyncStatus(bool afterSync = false)
    {
        if (!isDeserialized || deferPropertiesSyncStatusUpdate)
        {
            return;
        }

        if (!ArePropertiesRead)
        {
            if (!afterSync)
                PropertiesSyncStatus = SyncStatus.Changed;
        }
        else if (
            this.FollowMaskSyncStatus == SyncStatus.Synced &&
            this.FollowOffMaskSyncStatus == SyncStatus.Synced &&
            this.OnLevelSyncStatus == SyncStatus.Synced &&
            this.RampRateSyncStatus == SyncStatus.Synced &&
            this.ToggleModeSyncStatus == SyncStatus.Synced &&
            this.LEDOnSyncStatus == SyncStatus.Synced)
        {
            PropertiesSyncStatus = SyncStatus.Synced;
        }
        else if (PropertiesSyncStatus != SyncStatus.Changed)
        {
            PropertiesSyncStatus = afterSync ? SyncStatus.Unknown : SyncStatus.Changed;
        }
    }

    private bool deferPropertiesSyncStatusUpdate = false;

    /// <summary>
    /// Sends a LightOn command to all responders on this channel
    /// </summary>
    /// <param name="level"></param>
    /// <param name="completionCallback"></param>
    /// <returns></returns>
    public object ScheduleTurnOn(double level, Scheduler.JobCompletionCallback<bool>? completionCallback = null)
    {
        return InsteonScheduler.Instance.AddAsyncJob(
            $"Turning Hub Scene On {level * 100}% - {Id}: {Name}",
            () => DeviceDriver.TrySendAllLinkCommandToGroup(Id, DeviceCommand.CommandCode_LightON, level),
            completionCallback);
    }

    /// <summary>
    /// Sends a LightOff command to all responders on this channel
    /// </summary>
    /// <param name="completionCallback"></param>
    /// <returns></returns>
    public object ScheduleTurnOff(Scheduler.JobCompletionCallback<bool>? completionCallback = null)
    {
        return InsteonScheduler.Instance.AddAsyncJob(
            $"Turning Hub Scene Off - {Id}: {Name}",
            () => DeviceDriver.TrySendAllLinkCommandToGroup(Id, DeviceCommand.CommandCode_LightOFF, 0),
            completionCallback);
    }

    /// <summary>
    /// Asynchronously read channel properties in the physical device channel and notifies via PropertiesSyncStatusChanged event
    /// Properties are read into the channel driver, but hard propagated to this logical device only if forceSync is true
    /// </summary>
    /// <param name="forceSync">see above</param>
    /// <returns>success</returns>
    internal async Task<bool> TryReadChannelProperties(bool forceSync)
    {
        // Fail silently if the device does not have channel properties.
        if (!DeviceDriver.HasChannelProperties)
            return true;

        bool success = await DeviceDriver.TryReadChannelPropertiesAsync(Id);
        if (success)
        {
            using (new Common.DeferredExecution<bool>(UpdatePropertiesSyncStatus, param: true,
                deferExecutionCallback: (bool defer) => deferPropertiesSyncStatusUpdate = defer))
            {
                if (forceSync && ChannelDriver != null)
                {
                    this.FollowMask = ChannelDriver.FollowMask;
                    this.FollowOffMask = ChannelDriver.FollowOffMask;
                    this.OnLevel = ChannelDriver.OnLevel;
                    this.RampRate = ChannelDriver.RampRate;
                    this.ToggleMode = ChannelDriver.ToggleMode;
                    this.LEDOn = ChannelDriver.LEDOn;
                }
            }
        }

        return success;
    }

    // Have properties of this channel been read from the physical device already
    [MemberNotNullWhen(true, nameof(ChannelDriver))]
    private bool ArePropertiesRead => ChannelDriver?.ArePropertiesRead ?? false;

    /// <summary>
    /// Asynchronously synchronize local properties to the device
    /// </summary>
    /// <returns>true if success</returns>
    internal async Task<bool> TryWriteChannelProperties()
    {
        bool success = true;
        bool changed = false;

        // First try to read the channel properties to limit the number of writes
        // If that fails, we still try to write all properties
        if (!ArePropertiesRead)
        {
            await TryReadChannelProperties(forceSync: false);
        }

        // Now try to write properties if not read or not synced
        if (!ArePropertiesRead || FollowMaskSyncStatus == SyncStatus.Changed)
        {
            if (ChannelDriver != null && await ChannelDriver.TryWriteFollowMask((byte)FollowMask))
            {
                changed = true;
                FollowMaskLastUpdate = DateTime.Now;
            }
            else success = false;
        }
        else
        {
            Debug.Assert(FollowMask == ChannelDriver.FollowMask);
        }

        if (!ArePropertiesRead || FollowOffMaskSyncStatus == SyncStatus.Changed)
        {
            if (ChannelDriver != null && await ChannelDriver.TryWriteFollowOffMask((byte)FollowOffMask))
            {
                changed = true;
                FollowOffMaskLastUpdate = DateTime.Now;
            }
            else success = false;
        }
        else
        {
            Debug.Assert(FollowOffMask == ChannelDriver.FollowOffMask);
        }

        if (!ArePropertiesRead || OnLevelSyncStatus == SyncStatus.Changed)
        {
            if (ChannelDriver != null && await ChannelDriver.TryWriteOnLevel((byte)OnLevel))
            {
                changed = true;
                OnLevelLastUpdate = DateTime.Now;
            }
            else success = false;
        }
        else
        {
            Debug.Assert(OnLevel == ChannelDriver.OnLevel);
        }

        if (!ArePropertiesRead || RampRateSyncStatus == SyncStatus.Changed)
        {
            if (ChannelDriver != null && await ChannelDriver.TryWriteRampRate((byte)RampRate))
            {
                changed = true;
                RampRateLastUpdate = DateTime.Now;
            }
            else success = false;
        }
        else
        {
            Debug.Assert(RampRate == ChannelDriver.RampRate);
        }

        // Reflect writing properties in the sync status
        UpdatePropertiesSyncStatus();

        // If we successfully wrote at least one property value,notify observer
        // of change as this might have affected the SyncStatus of the channel
        if (changed)
        {
            UpdatePropertiesSyncStatus();
        }

        return success;
    }
}
