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
using Insteon.Drivers;
using Common;
using System.Diagnostics;
using Insteon.Synchronization;
using System.Runtime.CompilerServices;
using Insteon.Mock;
using System.Diagnostics.CodeAnalysis;
using static Insteon.Model.AllLinkRecord;
using Insteon.Commands;

namespace Insteon.Model;

public sealed class Device : DeviceBase
{
    internal Device(Devices devices, InsteonID id, bool fromDeserialization = false) : base(id)
    {
        this.devices = devices;
        isDeserialized = !fromDeserialization;
    }

    private Devices devices { get; init; }

    public House House => devices.House;

    // Copy the device properties from another device.
    // Generate observer notifications for each property that changed.
    internal void CopyFrom(Device device)
    {
        CopyPropertiesFrom(device);
        EnsureChannels();
        Channels.CopyFrom(device.Channels);
        AllLinkDatabase.CopyFrom(device.AllLinkDatabase);
    }

    // Helper to copy all properties from another device,
    // generating observer notifications for each property that changed
    private void CopyPropertiesFrom(Device device)
    {
        CategoryId = device.CategoryId;
        SubCategory = device.SubCategory;
        ProductKey = device.ProductKey;
        Revision = device.Revision;
        EngineVersion = device.EngineVersion;
        IsProductDataKnown = device.IsProductDataKnown;
        OperatingFlags = device.OperatingFlags;
        LEDBrightness = device.LEDBrightness;
        X10House = device.X10House;
        X10Unit = device.X10Unit;
        OnLevel = device.OnLevel;
        RampRate = device.RampRate;
        OpFlags2 = device.OpFlags2;
        DisplayName = device.DisplayName;
        Location = device.Location;
        Room = device.Room;
        AddedDateTime = device.AddedDateTime;
        Powerline = device.Powerline;
        Radio = device.Radio;
        YakityYak = device.YakityYak;
        HopIfModSyncFails = device.HopIfModSyncFails;
        WebDeviceID = device.WebDeviceID;
        WebGatewayControllerGroup = device.WebGatewayControllerGroup;
        Driver = device.Driver;
        Status = device.Status;
        PropertiesSyncStatus = device.PropertiesSyncStatus;
    }

    // Whether this device is indentical to another device
    // Used only for testing and check state in DEBUG
    internal bool IsIdenticalTo(Device device)
    {
        return Id == device.Id &&
            CategoryId == device.CategoryId &&
            SubCategory == device.SubCategory &&
            ProductKey == device.ProductKey &&
            Revision == device.Revision &&
            EngineVersion == device.EngineVersion &&
            IsProductDataKnown == device.IsProductDataKnown &&
            OperatingFlags == device.OperatingFlags &&
            LEDBrightness == device.LEDBrightness &&
            OnLevel == device.OnLevel &&
            RampRate == device.RampRate &&
            X10House == device.X10House &&
            X10Unit == device.X10Unit &&
            OpFlags2 == device.OpFlags2 &&
            NonToggleMask == device.NonToggleMask &&
            NonToggleOnOffMask == device.NonToggleOnOffMask &&
            DisplayName == device.DisplayName &&
            Location == device.Location &&
            Room == device.Room &&
            AddedDateTime == device.AddedDateTime &&
            Powerline == device.Powerline &&
            Radio == device.Radio &&
            YakityYak == device.YakityYak &&
            HopIfModSyncFails == device.HopIfModSyncFails &&
            WebDeviceID == device.WebDeviceID &&
            WebGatewayControllerGroup == device.WebGatewayControllerGroup &&
            Driver == device.Driver &&
            Status == device.Status &&
            PropertiesSyncStatus == device.PropertiesSyncStatus &&
            AllLinkDatabase.IsIdenticalTo(device.AllLinkDatabase) &&
            Channels.IsIdenticalTo(device.Channels);
    }

    // Called when the model has been fully deserialized
    internal void OnDeserialized()
    {
        isDeserialized = true;

        // We assume that product data of a device obtained from the deserialization is correct
        // since this information is immutable and never change after the device is added to the model.
        IsProductDataKnown = true;

        AllLinkDatabase.OnDeserialized();

        if (IsHub && DisplayName == null)
        {
            DisplayName = "Hub";
        }

        EnsureChannels();

        Channels.OnDeserialized();

        AddObserver(House.ModelObserver);
    }
    private bool isDeserialized;

    /// <summary>
    /// For observers to subscribe to change notifications
    /// </summary>
    /// <param name="deviceObserver"></param>
    public Device AddObserver(IDeviceObserver deviceObserver)
    {
        observers.Add(deviceObserver);
        return this;
    }

    /// <summary>
    /// For observers to unsubscribe to change notifications
    /// </summary>
    /// <param name="deviceObserver"></param>
    public Device RemoveObserver(IDeviceObserver deviceObserver)
    {
        observers.Remove(deviceObserver);
        return this;
    }

    private List<IDeviceObserver> observers = new();

    /// <summary>
    /// Driver of the phycial device in the Insteon network.
    /// This creates the device driver if not created yet, using the Product Data
    /// of this device to determine what driver type to create. The Product Data was
    /// either loaded from the persisted model or acquired from the physical device
    /// when adding a new device.
    /// We use IsProductDataKnown to assert that we have correct Product Data. At least 
    /// Category and SubCategory need to be correct to initialize the proper device driver.
    /// The Product Data is also copied to the device driver during creation to allow 
    /// it to function properly.
    /// </summary>
    internal DeviceDriverBase DeviceDriver
    {
        get
        {
            if (deviceDriver == null)
            {
                // See above
                Debug.Assert(IsProductDataKnown);

                if (IsHub)
                {
                    deviceDriver = new IMDriver(this);
                }
                else if (IsKeypadLinc)
                {
                    deviceDriver = new KeypadLincDriver(this);
                }
                else if (IsRemoteLinc)
                {
                    deviceDriver = new RemoteLincDriver(this);
                }
                else
                {
                    deviceDriver = new DeviceDriver(this);
                }
            }

            return deviceDriver;
        }
    }
    private DeviceDriverBase? deviceDriver;

    /// <summary>
    /// For testing purposes: set to mock physical device associated to this device
    /// to avoid network dependency during unit-testing
    /// </summary>
    public MockPhysicalDevice? MockPhysicalDevice => House.GetMockPhysicalDevice(Id);

    // -----------------------------------------------------------------------------------
    // Properties obtained from the physical device but generally not modified after that
    // ------------------------------------------------------------------------------------

    public override InsteonID Id
    {
        get => id ?? InsteonID.Null;
        set
        {
            if (value != id)
            {
                id = value;
                NotifyObserversOfPropertyChanged();
            }
        }
    }
    private InsteonID? id;

    public override DeviceKind.CategoryId CategoryId
    {
        get => categoryId;
        set
        {
            if (value != categoryId)
            {
                categoryId = value;
                NotifyObserversOfPropertyChanged();
            }
        }
    }
    private DeviceKind.CategoryId categoryId;

    public override int SubCategory
    {
        get => subCategory;
        set
        {
            if (value != subCategory)
            {
                subCategory = value;
                NotifyObserversOfPropertyChanged();
            }
        }
    }
    private int subCategory;

    public override int ProductKey
    {
        get => productKey;
        set
        {
            if (value != productKey)
            {
                productKey = value;
                NotifyObserversOfPropertyChanged();
            }
        }
    }
    private int productKey;

    public override int Revision
    {
        get => revision;
        set
        {
            if (value != revision)
            {
                revision = value;
                NotifyObserversOfPropertyChanged();
            }
        }
    }
    private int revision;

    public override int EngineVersion
    {
        get => engineVersion;
        set
        {
            if (value != engineVersion)
            {
                engineVersion = value;
                NotifyObserversOfPropertyChanged();
            }
        }
    }
    int engineVersion;

    public string Category => DeviceKind.GetCategoryName(CategoryId);
    public string ModelName => DeviceKind.GetModelName(CategoryId, SubCategory);
    public string ModelNumber => DeviceKind.GetModelNumber(CategoryId, SubCategory);
    public string ModelType => DeviceKind.GetModelTypeAsString(CategoryId, SubCategory);

    // -----------------------------------------------------------------------------------
    // Properties stored in the model but unknown to the physical device
    // ------------------------------------------------------------------------------------

    public int Wattage
    {
        get => wattage;
        set
        {
            if (value != wattage)
            {
                wattage = value;
                NotifyObserversOfPropertyChanged();
            }
        }
    }
    private int wattage;

    public string DisplayName
    {
        get => displayName;
        set
        {
            if (value != null && value != displayName)
            {
                displayName = value;
                NotifyObserversOfPropertyChanged();
            }
        }
    }
    private string displayName = string.Empty;

    public string DisplayNameAndId => this.DisplayName + " (" + this.Id + ")";

    public DateTime AddedDateTime
    {
        get => addedDateTime;
        set
        {
            if (value != addedDateTime)
            {
                addedDateTime = value;
                NotifyObserversOfPropertyChanged();
            }
        }
    }
    private DateTime addedDateTime = default;

    public string? Location
    {
        get => location;
        set
        {
            if (value != location)
            {
                location = value;
                NotifyObserversOfPropertyChanged();
            }
        }
    }
    private string? location;

    public string? Room
    {
        get => room;
        set
        {
            if (value != room)
            {
                room = value;
                if (isDeserialized)
                {
                    House.Rooms.OnRoomsChanged();
                }
                NotifyObserversOfPropertyChanged();
            }
        }
    }
    private string? room;

    // -----------------------------------------------------------------------------------
    // Properties stored in the model only for legacy reasons, unknown to the device.
    // Only here to roundrip to the HouseLinc.xml file
    // ------------------------------------------------------------------------------------

    public bool Powerline
    {
        get => powerline; 
        set
        {
            if (value != powerline)
            {
                powerline = value;
                NotifyObserversOfPropertyChanged();
            }
        }
    }
    private bool powerline = true;

    public bool Radio 
    { 
        get => radio;
        set
        {
            if (value != radio)
            {
                radio = value;
                NotifyObserversOfPropertyChanged();
            }
        }
    }
    private bool radio = true; 

    public string? YakityYak 
    { 
        get => yakityYak;
        set
        {
            if (value != yakityYak)
            {
                yakityYak = value;
                NotifyObserversOfPropertyChanged();
            }
        
        }
    }
    private string? yakityYak;

    public bool HopIfModSyncFails 
    { 
        get => hopIfModSyncFails; 
        set
        {
            if (value != hopIfModSyncFails)
            {
                hopIfModSyncFails = value;
                NotifyObserversOfPropertyChanged();
            }
        } 
    } 
    private bool hopIfModSyncFails = false;

    public int WebDeviceID 
    { 
        get => webDeviceID; 
        set
        {
            if (value != webDeviceID)
            {
                webDeviceID = value;
                NotifyObserversOfPropertyChanged();
            }
        } 
    }
    private int webDeviceID;

    public int WebGatewayControllerGroup 
    { 
        get => webGatewayControllerGroup; 
        set
        {
            if (value != webGatewayControllerGroup)
            {
                webGatewayControllerGroup = value;
                NotifyObserversOfPropertyChanged();
            }
        } 
    }
    private int webGatewayControllerGroup;

    public string Driver
    { 
        get => driver; 
        set
        {
            if (value != driver)
            {
                driver = value;
                NotifyObserversOfPropertyChanged();
            }
        } 
    } 
    private string driver = string.Empty;

    // -----------------------------------------------------------------------------------
    // Open ended property bag
    // Can handle properties specific to certain device type without having to change the persistence code
    // Limited to properties of type "byte" for now.
    // ------------------------------------------------------------------------------------

    internal readonly DeviceProperties PropertyBag = new();

    public override Bits OperatingFlags
    {
        get => PropertyBag.GetValue(OperatingFlagsName);
        set
        {
            Bits oldValue = OperatingFlags;
            if (PropertyBag.SetValue(OperatingFlagsName, value))
            {
                foreach (var prop in OperatingFlagsProperties)
                {
                    if (oldValue[prop.index] != value[prop.index])
                        NotifyObserversOfPropertyChanged(prop.name);
                }

                NotifyObserversOfPropertyChanged();
                UpdatePropertiesSyncStatus();
            }
        }
    }
    const string OperatingFlagsName = "operatingFlags";

    public override byte LEDBrightness
    {
        get => PropertyBag.GetValue(LEDBrightnessName);
        set
        {
            if (PropertyBag.SetValue(LEDBrightnessName, value))
            {
                NotifyObserversOfPropertyChanged();
                UpdatePropertiesSyncStatus();
            }
        }
    }
    const string LEDBrightnessName = "ledDimming";

    // Indicate that a device is not responding to pings and should be considered disconnected,
    // Currently only avalable on a remotelinc (out of immediate reach)
    public bool Snoozed
    {
        get => PropertyBag.GetValue(SnoozedName) > 0;
        set
        {
            if (PropertyBag.SetValue(SnoozedName, (byte)(value ? 1 : 0)))
            {
                NotifyObserversOfPropertyChanged();
                // Not syncable
                //UpdatePropertiesSyncStatus();
            }
        }
    }
    const string SnoozedName = "snoozed";

    /// <summary>
    /// OnLevel for devices with no channel.
    /// For devices with channels/groups this corresponds to group 1.
    /// </summary>
    public override byte OnLevel
    {
        get => PropertyBag.GetValue(OnLevelName);
        set
        {
            if (PropertyBag.SetValue(OnLevelName, value))
            {
                NotifyObserversOfPropertyChanged();
                UpdatePropertiesSyncStatus();
            }
        }
    }
    private const string OnLevelName = "onLevel";

    /// <summary>
    /// Ramp Rate for devices with no channel.
    /// For devices with channels/groups this corresponds to group 1.
    /// </summary>
    public override byte RampRate
    {
        get => PropertyBag.GetValue(RampRateName);
        set
        {
            if (PropertyBag.SetValue(RampRateName, value))
            {
                NotifyObserversOfPropertyChanged();
                UpdatePropertiesSyncStatus();
            }
        }
    }
    private const string RampRateName = "rampRate";

    /// <summary>
    /// Mask identifying the NonToggle buttons on a KeypadLinc.
    /// No effect if this is not a KepacLinc.
    /// </summary>
    public byte NonToggleMask
    {
        get => PropertyBag.GetValue(NonToggleMaskName);
        set
        {
            if (PropertyBag.SetValue(NonToggleMaskName, value))
            {
                NotifyObserversOfPropertyChanged();
                // Synced via channel ToggleMode
                //UpdatePropertiesSyncStatus();
            }
        }
    }
    private const string NonToggleMaskName = "nonToggleMask";

    /// <summary>
    /// Mask identifying the NonToggle buttons on a KeypadLinc.
    /// No effect if this is not a KepadLinc.
    /// </summary>
    public byte NonToggleOnOffMask
    { 
        get => PropertyBag.GetValue(NonToggleOnOffMaskName);
        set
        {
            if (PropertyBag.SetValue(NonToggleOnOffMaskName, value))
            {
                NotifyObserversOfPropertyChanged();
                // Synced via channel ToggleMode
                //UpdatePropertiesSyncStatus();
            }
        }
    }
    private const string NonToggleOnOffMaskName = "nonToggleOnOffMask";

    /// <summary>
    /// Not implemented. Stores the value, but has not effect.
    /// </summary>
    public byte X10House
    {
        get => PropertyBag.GetValue(X10HouseName) ;
        set
        {
            if (PropertyBag.SetValue(X10HouseName, value))
            {
                NotifyObserversOfPropertyChanged();
                // If implementing, update UpdatePropertiesSyncStatus and uncomment
                //UpdatePropertiesSyncStatus();
            }
        }
    } 
    private const string X10HouseName = "X10House";

    /// <summary>
    /// Not implemented. Stores the value, but has not effect.
    /// </summary>
    public byte X10Unit
    {
        get => PropertyBag.GetValue(X10UnitName);
        set
        {
            if (PropertyBag.SetValue(X10UnitName, value))
            {
                NotifyObserversOfPropertyChanged();
                // If implementing, update UpdatePropertiesSyncStatus and uncomment
                //UpdatePropertiesSyncStatus();
            }
        }
    }
    private const string X10UnitName = "X10Unit";

    public override Bits OpFlags2
    {
        get => PropertyBag.GetValue(OpFlags2Name);
        set
        {
            if (PropertyBag.SetValue(OpFlags2Name, value))
            {
                NotifyObserversOfPropertyChanged();
                UpdatePropertiesSyncStatus();
            }
        }
    }
    private const string OpFlags2Name = "opflags2";

    // For round tripping to the HouseLinc.xml file
    public object? customProperties { get; set; }

    // -----------------------------------------------------------------------------------
    // Read-write properties from the physical device, stored as bits in OperatingFlags.
    // PropertiesSyncStatus is updated every time these properties get changed and
    // the OnDevicePropertyChange event is raised.
    // ------------------------------------------------------------------------------------

    // We use this array as the central place to describe properties stored in OperatingFlags
    // to make it easy to fire OnPropertyChanged notifications, whether the result of a single
    // property changing or the whole OperatingFlags or OpFlags2 bytes.
    private readonly static (int index, string name)[] OperatingFlagsProperties =
    {
        (0, nameof(ProgramLock)),
        (1, nameof(LEDOnTx)),
        (2, nameof(ResumeDim)),
        (2, nameof(BeeperOn)),
        (3, nameof(Is8Button)),
        (3, nameof(StayAwake)),
        (4, nameof(LEDOn)),
        (5, nameof(LoadSenseOn)),
        (5, nameof(NoHeartbeat)),
        (5, nameof(KeyBeep)),
        (6, nameof(RFEnabled)),
        (7, nameof(PowerlineEnabled)),
        (8+5, nameof(DetachedLoad)) // in OpFlags2
    };

    // Returns the bit index of a given property in the array above
    private static int BitInOperatingFlags([CallerMemberName] string name = "")
    {
        foreach (var prop in OperatingFlagsProperties)
            if (name == prop.name)
                return prop.index;

        Debug.Assert(false, $"Unknown OperatingFlags property: {name}");
        return 0;
    }

    /// <summary>
    /// Operating flags bits - Program Lock
    /// </summary>
    public bool ProgramLock
    {
        get { return OperatingFlags[BitInOperatingFlags()]; }
        set
        {
            var bit = BitInOperatingFlags();
            if (value != OperatingFlags[bit])
            {
                Bits flags = OperatingFlags; 
                flags[bit] = value;
                OperatingFlags = flags;
                NotifyObserversOfPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Operating flags bits - LED on during Transmit
    /// </summary>
    public bool LEDOnTx
    {
        get { return OperatingFlags[BitInOperatingFlags()]; }
        set
        {
            var bit = BitInOperatingFlags();
            if (value != OperatingFlags[bit])
            {
                Bits flags = OperatingFlags;
                flags[bit] = value;
                OperatingFlags = flags;
                NotifyObserversOfPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Operating flags bits - Resume Dim 
    /// </summary>
    public bool ResumeDim
    {
        get { return OperatingFlags[BitInOperatingFlags()]; }
        set
        {
            var bit = BitInOperatingFlags();
            if (value != OperatingFlags[bit])
            {
                Bits flags = OperatingFlags;
                flags[bit] = value;
                OperatingFlags = flags;
                NotifyObserversOfPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Operating flags bits - Beeper on (ControlLinc)
    /// </summary>
    public bool BeeperOn
    {
        get { return OperatingFlags[BitInOperatingFlags()]; }
        set
        {
            var bit = BitInOperatingFlags();
            if (value != OperatingFlags[bit])
            {
                Bits flags = OperatingFlags;
                flags[bit] = value;
                OperatingFlags = flags;
                NotifyObserversOfPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Operating flags bits - 8 button keypad
    /// </summary>
    public bool Is8Button
    {
        get { return OperatingFlags[BitInOperatingFlags()]; }
        set
        {
            var bit = BitInOperatingFlags();
            if (value != OperatingFlags[bit])
            {
                Bits flags = OperatingFlags;
                flags[bit] = value;
                OperatingFlags = flags;
                NotifyObserversOfPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Operating flags bits - Stay Awake (RemoteLinc)
    /// </summary>
    public bool StayAwake
    {
        get { return OperatingFlags[BitInOperatingFlags()]; }
        set
        {
            var bit = BitInOperatingFlags();
            if (value != OperatingFlags[bit])
            {
                Bits flags = OperatingFlags;
                flags[bit] = value;
                OperatingFlags = flags;
                NotifyObserversOfPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Operating flags bits - LED On
    /// OperatingFlags[4] is 1 when LEDBacklight is off
    /// </summary>
    public bool LEDOn
    {
        get => !OperatingFlags[BitInOperatingFlags()];
        set
        {
            var bit = BitInOperatingFlags();
            if (!value != OperatingFlags[bit])
            {
                Bits flags = OperatingFlags;
                flags[bit] = !value;
                OperatingFlags = flags;
                NotifyObserversOfPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Operating flags bits - Receive Only (RemoteLinc)
    /// </summary>
    public bool ReceiveOnly
    {
        get => OperatingFlags[BitInOperatingFlags()];
        set
        {
            var bit = BitInOperatingFlags();
            if (value != OperatingFlags[bit])
            {
                Bits flags = OperatingFlags;
                flags[bit] = value;
                OperatingFlags = flags;
                NotifyObserversOfPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Operating flags bits - Load SenseOn
    /// </summary>
    public bool LoadSenseOn
    {
        get => OperatingFlags[BitInOperatingFlags()];
        set
        {
            var bit = BitInOperatingFlags();
            if (value != OperatingFlags[bit])
            {
                Bits flags = OperatingFlags;
                flags[bit] = value;
                OperatingFlags = flags;
                NotifyObserversOfPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Operating flags bits - No Heartbeat (RemoteLinc)
    /// </summary>
    public bool NoHeartbeat
    {
        get => OperatingFlags[BitInOperatingFlags()];
        set
        {
            var bit = BitInOperatingFlags();
            if (value != OperatingFlags[bit])
            {
                Bits flags = OperatingFlags;
                flags[bit] = value;
                OperatingFlags = flags;
                NotifyObserversOfPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Operating flags bits - KeyBeep
    /// </summary>
    public bool KeyBeep
    {
        get => OperatingFlags[BitInOperatingFlags()];
        set
        {
            var bit = BitInOperatingFlags();
            if (value != OperatingFlags[bit])
            {
                Bits flags = OperatingFlags;
                flags[bit] = value;
                OperatingFlags = flags;
                NotifyObserversOfPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Operating flags bits - RFEnabled (RF)
    /// OperatingFlags[6] is 1 when RF is disabled
    /// </summary>
    public bool RFEnabled
    {
        get => !OperatingFlags[BitInOperatingFlags()];
        set
        {
            var bit = BitInOperatingFlags();
            if (!value != OperatingFlags[bit])
            {
                Bits flags = OperatingFlags;
                flags[bit] = !value;
                OperatingFlags = flags;
                NotifyObserversOfPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Operating flags bits - Powerline Enabled
    /// OperatingFlags[7] is 1 when RF is disabled
    /// </summary>
    public bool PowerlineEnabled
    {
        get => !OperatingFlags[BitInOperatingFlags()];
        set
        {
            var bit = BitInOperatingFlags();
            if (!value != OperatingFlags[bit])
            {
                Bits flags = OperatingFlags;
                flags[bit] = !value;
                OperatingFlags = flags;
                NotifyObserversOfPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Load is detached from button 1 on a KeypadLinc
    /// and instead controlled by channel 9
    /// </summary>
    public bool DetachedLoad
    {
        get => OpFlags2[BitInOperatingFlags() - 8];
        set
        {
            var bit = BitInOperatingFlags() - 8;
            if (value != OpFlags2[bit])
            {
                Bits flags = OpFlags2;
                flags[bit] = value;
                OpFlags2 = flags;
                NotifyObserversOfPropertyChanged();
            }
        }
    }

    // Helper to handle notifications when a property value is changed
    private void NotifyObserversOfPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        observers.ForEach(o => o.DevicePropertyChanged(this, propertyName));
    }

    /// <summary>
    /// Overall device properties sync status
    /// </summary>
    public SyncStatus PropertiesSyncStatus
    {
        get => propertiesSyncStatus;
        set
        {
            if (value != propertiesSyncStatus)
            {
                propertiesSyncStatus = value;
                observers.ForEach(o => o.DevicePropertiesSyncStatusChanged(this));
            }
        }
    }
    private SyncStatus propertiesSyncStatus;

    // Helper to update the overal sync status for the device.
    // Parameter afterSync indicates why this is called:
    // -> false: because one of the tracked properties changed in the model.
    // -> true: because of a read from or write to the device.
    private void UpdatePropertiesSyncStatus(bool afterSync = false)
    {
        // No action before this device is being deserialized, as we want to track only user changes here.
        // No action if updating sync status is deferred (because we are setting multiple properties in a row).
        if (!isDeserialized || deferPropertiesSyncStatusUpdate)
        {
            return;
        }

        // If we are called as a result of a read/write to the device, we should have a device driver.
        Debug.Assert(!afterSync || deviceDriver != null);

        // Determine if we have read the properties from the physical device.
        // Note that we avoid creating a device driver if we don't have one yet for two reasons:
        // 1. We don't need to: if we don't have a DeviceDriver yet, we have not read properties.
        // 2. This method might be called when applying changes to a secondary house model,
        //    not the one presented to the user, in which case we don't need/want any driver.
        if (deviceDriver == null || !((DeviceDriver as DeviceDriver)?.ArePropertiesRead ?? false))
        {
            // We don't know the property values in the physical device.
            // If we are called because of a model change, we mark properties "Changed" to ensure the change
            // is propagated to the physical device next sync.
            if (!afterSync)
                PropertiesSyncStatus = SyncStatus.Changed;
        }
        else if (
            // If we have read the properties from the device, we determine sync status
            // by comparing values between model and device driver
            OperatingFlags == DeviceDriver.OperatingFlags &&
            OpFlags2 == DeviceDriver.OpFlags2 &&
            LEDBrightness == DeviceDriver.LEDBrightness &&
            OnLevel == DeviceDriver.OnLevel &&
            RampRate == DeviceDriver.RampRate)
        {
            PropertiesSyncStatus = SyncStatus.Synced;
        }
        else if (PropertiesSyncStatus != SyncStatus.Changed)
        {
            // If model values are different from the physcial device and not already marked changed in the model,
            // we mark them "Changed" to reflect a change in the model (afterSync = false) and force a write next sync,
            // or "Unknown" to reflect a change in the physical device (afterSync = true) and acquire the values next sync.
            PropertiesSyncStatus = afterSync ? SyncStatus.Unknown : SyncStatus.Changed;
        }
    }

    private bool deferPropertiesSyncStatusUpdate = false;

    /// <summary>
    /// Set the device properties SyncStatus to "Unknown" to force reading from physical device
    /// unless it is already marked "Changed", which will also force a read (and possibly a write)
    /// </summary>
    internal void ResetPropertiesSyncStatus()
    {
        if (PropertiesSyncStatus == SyncStatus.Synced)
            PropertiesSyncStatus = SyncStatus.Unknown;
    }

    // -----------------------------------------------------------------------------------
    // Channels
    // ------------------------------------------------------------------------------------
    public Channels Channels
    {
        get => channels ??= new Channels(this);
        set
        {
            if (value != channels)
            {
                channels = value;
                channels.Device = this;
                observers.ForEach(o => o.DeviceChannelsChanged(this));
            }
        }
    }
    private Channels? channels;

    public bool HasChannels => Channels.Count() > 1;

    // TODO: find a way to reconcile this with ChannelCount on the physical device
    // which is not always available (only if we have read product and operating flags
    // information from the physical device on the network).
    public int ChannelCount => IsMultiChannelDevice ? DeviceDriver.ChannelCount : 1;
    public int FirstChannelId => DeviceDriver.FirstChannelId;

    public Channel GetChannel(int channelId)
    {
        // Channel ids can be either 0 or 1 based
        Debug.Assert(channelId - Channels[0].Id >= 0 && channelId - Channels[0].Id < Channels.Count);
        return Channels[channelId - Channels[0].Id];
    }

    internal int GetChannelBitMask(int channelId)
    {
        return 1 << (channelId - Channels[0].Id);
    }

    /// <summary>
    /// Ensure this device has the proper number of channels allocated 
    /// If we have a physical device already, ensure that the channels match those of the physical device
    /// If not, we go by the product category and model
    /// </summary>
    internal void EnsureChannels()
    {
        if (IsMultiChannelDevice)
        {
            if (Channels.Count > ChannelCount)
            {
                // If we have too many channels in the list, remove them
                var newChannels = new Channels();
                for (int i = 0; i < ChannelCount; i++)
                {
                    newChannels.Add(Channels[i]);
                }
                Channels = newChannels;
                Debug.Assert(Channels.Count == ChannelCount);
            }
            else if (Channels.Count < ChannelCount)
            {
                // If we have too few channels in the list, add more
                for (int i = Channels.Count; i < ChannelCount; i++)
                {
                    Channel channel = new Channel(Channels) { Id = i + FirstChannelId }.AddObserver(House.ModelObserver);
                    Channels.Add(channel);
                }
                Debug.Assert(Channels.Count == ChannelCount);
                observers.ForEach(o => o.DeviceChannelsChanged(this));
            }

            Debug.Assert(Channels.Count == 0 || Channels[0].Id == FirstChannelId);
        }
    }

    // -----------------------------------------------------------------------------------
    // All-Link Database
    // ------------------------------------------------------------------------------------

    /// <summary>
    /// The all-link database for this device, as stored in the model.
    /// </summary>
    public AllLinkDatabase AllLinkDatabase
    {
        get => allLinkDatabase ??= new AllLinkDatabase(this).AddObserver(House.ModelObserver);
        set
        {
            if (allLinkDatabase != null)
                allLinkDatabase.Device = null;
            allLinkDatabase = value;
            if (allLinkDatabase != null)
                allLinkDatabase.Device = this;
            observers.ForEach(o => o.AllLinkDatabaseChanged(this, AllLinkDatabase));
        }
    }
    private AllLinkDatabase? allLinkDatabase;

    // -----------------------------------------------------------------------------------
    // Device connection status
    // ------------------------------------------------------------------------------------

    /// <summary>
    /// Device connection status
    /// </summary>
    public enum ConnectionStatus
    {
        Unknown,        // No last known status
        Connected,      // Last known status was: connected to the hub
        Disconnected,   // Last known status was: not connected to the hub
        GatewayError    // Hub/Gateway error, the gateway might not have been found
    }

    /// <summary>
    /// Lkg connection status of the device
    /// To obtain the true connection status from the device at this time,
    /// call ScheduleRetrieveConnectionStatus or RetrieveConnectionStatusAsync
    /// </summary>
    public ConnectionStatus Status
    {
        get => connectionStatus;
        set
        {
            if (value != connectionStatus)
            {
                connectionStatus = value;
                NotifyObserversOfPropertyChanged();
            }
        }
    }
    private ConnectionStatus connectionStatus = ConnectionStatus.Unknown;

    /// <summary>
    /// Retrieve connection status, i.e., whether this device is connected to the hub and responding to pings.
    /// The connection status is persisted and will be queried from the device only once, unless something, 
    /// such as a failing read/write operation, invalidates it by setting isConnectionStatusKnown to false, 
    /// or the force flag is on.
    /// If the connection status is believe to be known, this function simply returns it. If not, a retrieval 
    /// of the current connection status is schedule, and callers will be notified when it is retreived through
    /// a callback they provide.
    /// If multiple calls are made to this method while a ping is pending or running, we simply add the callback, 
    /// if any, to a list, and will use the same ping job to notify all listeners.
    /// </summary>
    /// <param name="callback">This will be called when status is determined, if it has changed</param>
    /// <param name="force">Force a re-ping of the device</param>
    public ConnectionStatus ScheduleRetrieveConnectionStatus(ConnectionStatusCallback? callback = null, bool force = false)
    {
        if (!isConnectionStatusKnown || force)
        {
            // Remember the completion callback if any
            if (callback != null)
            {
                connectionStatusCallbacks.Add(callback);
            }

            // If we don't have a pending request, schedule a new one
            if (pingJob == null)
            {
                pingJob = InsteonScheduler.Instance.AddAsyncJob(
                    $"Pinging Device - {DisplayNameAndId}",
                    () => RetrieveConnectionStatusAsync(force),
                    (status) =>
                    {
                        // And run all callbacks on success
                        pingJob = null;
                        var callbacks = new List<ConnectionStatusCallback>(connectionStatusCallbacks);
                        connectionStatusCallbacks.Clear();
                        callbacks.ForEach(c => c.Invoke(Status));
                        return;
                    },
                    maxRunCount: 1);
            }
        }
        return Status;
    }

    public delegate void ConnectionStatusCallback(ConnectionStatus connectionStatus);
    private List<ConnectionStatusCallback> connectionStatusCallbacks = new List<ConnectionStatusCallback>();
    private object? pingJob;

    /// <summary>
    /// Return connection status
    /// </summary>
    /// <param name="force">Ping the device regardless of isConnectionStatusKnown</param>
    /// <returns>Connection status</returns>
    internal async Task<ConnectionStatus> RetrieveConnectionStatusAsync(bool force = false)
    {
        ConnectionStatus status = Status;

        // Make sure we have the product data (no cost if we already have it)
        if (!await TryReadProductDataAsync())
        {
            status = ConnectionStatus.Disconnected;
        }

        // And ping the device if we have not already
        if (!isConnectionStatusKnown || force)
        {
            status = await TryPingAsync();
        }

        SetKnownConnectionStatus(status);
        return Status;
    }
    private bool isConnectionStatusKnown = false;

    /// <summary>
    /// Return whether the device is unreachable, i.e., not connected to the hub
    /// </summary>
    /// <returns></returns>
    internal async Task<bool> IsUnreachableAsync()
    {
        await RetrieveConnectionStatusAsync();
        return Status == ConnectionStatus.Disconnected || Status == ConnectionStatus.GatewayError;
    }

    /// <summary>
    /// Helper to set a known (just acquired) connection status
    /// </summary>
    /// <param name="status">Connection status to set</param>
    internal void SetKnownConnectionStatus(ConnectionStatus status)
    {
        Status = status;
        isConnectionStatusKnown = true;
    }

    /// <summary>
    /// Called when we complete a read or write operation with this device.
    /// On error, we reset the connection status to force a new retrieval
    /// - to propagate the new status to the UI
    /// - to cut short any batch of operation (such as a sync of the device)
    /// </summary>
    internal void OnTryReadWriteComplete(bool success)
    {
        if (!success)
        {
            // Reset the connection status to force a re-ping,
            // which might propagate the failure to the UI
            isConnectionStatusKnown = false;
        }
    }

    /// <summary>
    /// Called when the Hub/Gateway has changed or its settings have changed
    /// Force a re-ping of the device to update the connection status
    /// </summary>
    public void OnGatewayChanged()
    {
        isConnectionStatusKnown = false;
    }

    // -----------------------------------------------------------------------------------
    // Scheduled Jobs
    // ------------------------------------------------------------------------------------

    /// <summary>
    /// Cancel scheduled job - See Insteon Scheduler
    /// </summary>
    public static void CancelScheduledJob(object? job)
    {
        InsteonScheduler.Instance.CancelJob(job);
    }

    /// <summary>
    /// Schedule a ping a ping of this physical device
    /// </summary>
    /// <param name="completionCallback">called on completion if not null</param>
    /// <returns>handle to scheduled job</returns>
    public object SchedulePingDevice(Scheduler.JobCompletionCallback<Device.ConnectionStatus>? completionCallback = null,
        object? group = null, int maxRunCount = 3)
    {
        // And add a job to read physcial device properties
        return InsteonScheduler.Instance.AddAsyncJob(
            $"Pinging Device - {DisplayNameAndId}",
            TryPingAsync,
            completionCallback,
            group: group,
            maxRunCount: maxRunCount);
    }

    /// <summary>
    /// Schedule turning the lights on for a light controlling device
    /// </summary>
    /// <param name="completionCallback"></param>
    /// <returns></returns>
    public object ScheduleLightOn(double level, Scheduler.JobCompletionCallback<bool>? completionCallback = null)
    {
        return InsteonScheduler.Instance.AddAsyncJob(
            $"Turning Light On {level*100}% - {DisplayNameAndId}",
            () => DeviceDriver.TryLightOn(level),
            completionCallback);
    }

    /// <summary>
    /// Schedule turning the lights off for a light controlling device
    /// </summary>
    /// <param name="completionCallback"></param>
    /// <returns></returns>
    public object ScheduleLightOff(Scheduler.JobCompletionCallback<bool>? completionCallback = null)
    {
        return InsteonScheduler.Instance.AddAsyncJob(
            $"Turning Light off - {DisplayNameAndId}",
            DeviceDriver.TryLightOff,
            completionCallback);
    }

    /// <summary>
    /// Schedule acquiring the current on-level for a light controlling device
    /// </summary>
    /// <param name="completionCallback"></param>
    /// <returns></returns>
    public object ScheduleGetLightOnLevel(Scheduler.JobCompletionCallback<double>? completionCallback = null)
    {
        return InsteonScheduler.Instance.AddAsyncJob(
            $"Getting Current Light level - {DisplayNameAndId}",
            DeviceDriver.TryGetLightOnLevel,
            completionCallback);
    }

    /// <summary>
    /// Schedule a full read of the physical device
    /// </summary>
    /// <param name="completionCallback"></param>
    /// <param name="group"></param>
    /// <param name="delay"></param>
    /// <param name="priority"></param>
    /// <returns></returns>
    public object ScheduleReadDevice(Scheduler.JobCompletionCallback<bool>? completionCallback = null, 
        object? group = null, TimeSpan delay = new TimeSpan(), Scheduler.Priority priority = Scheduler.Priority.Medium, bool forceRead = false)
    {
        object localGroup = InsteonScheduler.Instance.AddGroup($"Reading Device {DisplayNameAndId}", completionCallback, group);
        ScheduleReadDeviceProperties(null, localGroup, delay, priority: priority, forceSync: true);
        ScheduleReadChannelsProperties(null, localGroup, delay, priority: priority, forceSync: true);
        ScheduleReadAllLinkDatabase(null, localGroup, delay, priority: priority, forceRead: forceRead);
        EnsureChannels();
        return localGroup;
    }

    /// <summary>
    /// Read product data from the device.
    /// This is readonly information about the product, such as model category, sub-category, revision, etc.
    /// This records whether it has been called already and is a not-op if so, unless parameter force is set.
    /// </summary>
    /// <param name="completionCallback"></param>
    /// <param name="group"></param>
    /// <param name="delay"></param>
    /// <param name="priority"></param>
    /// <param name="forceRead"></param>
    /// <returns></returns>
    public object ScheduleReadProductData(Scheduler.JobCompletionCallback<bool>? completionCallback = null,
        Object? group = null, TimeSpan delay = new TimeSpan(), Scheduler.Priority priority = Scheduler.Priority.Medium, bool forceRead = true)
    {
        // And add a job to read physcial device properties
        return InsteonScheduler.Instance.AddAsyncJob(
            $"Reading Product Data - {DisplayNameAndId}",
            () => TryReadProductDataAsync(forceRead),
            completionCallback,
            prehandlerAsync: null,
            group,
            delay, priority);    }

    /// <summary>
    /// Schedule reading the device properties from the network
    /// Properties are read in the physical device, and only copied in this logical device if the forceSync parameter is true
    /// </summary>
    /// <param name="forceSync">propagate properties to this logical device</param>
    /// <param name="completionCallback">called on completion if not null</param>
    /// <param name="delay">delay before running job</param>
    /// <returns>handle to scheduled job</returns>
    public object ScheduleReadDeviceProperties(Scheduler.JobCompletionCallback<bool>? completionCallback = null, 
        Object? group = null, TimeSpan delay = new TimeSpan(), Scheduler.Priority priority = Scheduler.Priority.Medium, bool forceSync = true)
    {
        // And add a job to read physcial device properties
        return InsteonScheduler.Instance.AddAsyncJob(
            $"Reading Device Properties - {DisplayNameAndId}",
            () => TryReadDevicePropertiesAsync(forceSync),
            completionCallback,
            async () => !await IsUnreachableAsync(),
            group,
            delay, priority);
    }

    /// <summary>
    /// Schedule writing the device properties from the network
    /// </summary>
    /// <returns>handle to schedule job, null if the device is not connected</returns>
    public object ScheduleWriteDeviceProperties(Scheduler.JobCompletionCallback<bool>? completionCallback = null, 
        object? group = null, TimeSpan delay = new TimeSpan(), Scheduler.Priority priority = Scheduler.Priority.Medium)
    {
        // And add a job to read physcial device properties
        return InsteonScheduler.Instance.AddAsyncJob(
            $"Updating Device Properties - {DisplayNameAndId}",
            TryWriteDevicePropertiesAsync,
            completionCallback,
            async () => !await IsUnreachableAsync(),
            group,
            delay, priority);
    }

    /// <summary>
    /// Schedule reading properties from one channel of this device from the network
    /// Properties are read in the physical device channel, and only copied in this logical device channel if the forceSync parameter is true
    /// </summary>
    /// <param name="forceSync">copy properties to this logical device</param>
    /// <param name="completionCallback">called on completion if not null</param>
    /// <param name="delay">delay before running job</param>
    /// <returns>handle to scheduled job</returns>
    public object ScheduleReadChannelProperties(int channelId, Scheduler.JobCompletionCallback<bool>? completionCallback = null, 
        object? group = null, TimeSpan delay = new TimeSpan(), Scheduler.Priority priority = Scheduler.Priority.Medium, bool forceSync = true)
    {
        return InsteonScheduler.Instance.AddAsyncJob(
            $"Reading Channel {channelId} Properties - {DisplayNameAndId}",
            () => TryReadChannelPropertiesAsync(channelId, forceSync),
            completionCallback,
            async () => !await IsUnreachableAsync(),
            group,
            delay, priority);
    }

    /// <summary>
    /// Schedule reading all channels properties to the network
    /// </summary>
    /// <returns>handle to schedule job</returns>
    public object ScheduleReadChannelsProperties(Scheduler.JobCompletionCallback<bool>? completionCallback = null, 
        object? group = null, TimeSpan delay = new TimeSpan(), Scheduler.Priority priority = Scheduler.Priority.Medium, bool forceSync = true)
    {
        return InsteonScheduler.Instance.AddAsyncJob(
            $"Reading All Channels Properties - {DisplayNameAndId}",
            () => TryReadChannelsPropertiesAsync(forceSync),
            completionCallback,
            async () => !await IsUnreachableAsync(),
            group,
            delay, priority);
    }

    /// <summary>
    /// Schedule writing all channels properties to the network
    /// </summary>
    /// <returns>handle to schedule job</returns>
    public object ScheduleWriteChannelProperties(int channelId, Scheduler.JobCompletionCallback<bool>? completionCallback = null, 
        object? group = null, TimeSpan delay = new TimeSpan(), Scheduler.Priority priority = Scheduler.Priority.Medium)
    {
        return InsteonScheduler.Instance.AddAsyncJob(
            $"Updating Channel {channelId} Properties - {DisplayNameAndId}",
            () => TryWriteChannelPropertiesAsync(channelId),
            completionCallback,
            async () => !await IsUnreachableAsync(),
            group,
            delay, priority);
    }

    /// <summary>
    /// Schedule writing all channels properties to the network
    /// </summary>
    /// <returns>handle to schedule job</returns>
    public object ScheduleWriteChannelsProperties(Scheduler.JobCompletionCallback<bool>? completionCallback = null, 
        object? group = null, TimeSpan delay = new TimeSpan(), Scheduler.Priority priority = Scheduler.Priority.Medium)
    {
        return InsteonScheduler.Instance.AddAsyncJob(
            $"Updating All Channels Properties - {DisplayNameAndId}",
            TryWriteChannelsPropertiesAsync,
            completionCallback,
            async () => !await IsUnreachableAsync(),
            group,
            delay,
            priority);
    }

    /// <summary>
    /// Schedule writing all channels properties expressed as masks on the device to the network
    /// </summary>
    /// <returns>handle to schedule job</returns>
    public object ScheduleWriteChannelsBitMasks(Scheduler.JobCompletionCallback<bool>? completionCallback = null, 
        object? group = null, TimeSpan delay = new TimeSpan(), Scheduler.Priority priority = Scheduler.Priority.Medium)
    {
        return InsteonScheduler.Instance.AddAsyncJob(
            $"Updating All Channels Bit Masks - {DisplayNameAndId}",
            TryWriteChannelsBitMasksAsync,
            completionCallback,
            async () => !await IsUnreachableAsync(),
            group,
            delay,
            priority);
    }

    /// <summary>
    /// Schedule reading the all-link database from the network
    /// Merge AllLinkDatabase of this logical device with the one of the physical device, updating the status of each link 
    /// </summary>
    /// <returns>handle to schedule job</returns>
    public object ScheduleReadAllLinkDatabase(Scheduler.JobCompletionCallback<bool>? completionCallback = null, 
        object? group = null, TimeSpan delay = new TimeSpan(), Scheduler.Priority priority = Scheduler.Priority.Medium, bool forceRead = false)
    {
        // Since this is a potentially long operation, it should be designed in such a way that repeat calls
        // create minimum overhead after first call if the database did not change from under us. Even the
        // interruptible job should be designed to allow multiple concurrent jobs on the same database,
        // interruptable or not

        // Unless this job is high priority, run it in an interruptible way
        return (priority == Scheduler.Priority.High) ?
            InsteonScheduler.Instance.AddAsyncJob(
                $"Reading Device Database - {DisplayNameAndId}",
                () => TryReadAllLinkDatabaseAsync(forceRead),
                completionCallback,
                async () => !await IsUnreachableAsync(),
                group,
                delay, priority) :
            InsteonScheduler.Instance.AddAsyncSteppedJob(
                $"Reading Device Database Step by Step - {DisplayNameAndId}",
                (restart) => TryReadAllLinkDatabaseStepAsync(restart, forceRead),
                completionCallback,
                async () => !await IsUnreachableAsync(),
                group,
                delay, priority);
    }

    /// <summary>
    /// Schedule writing the alllink database to the network
    /// </summary>
    /// <param name="forceRead">Force reading the database from the physical device prior to writing to it</param>
    /// <returns>handle to schedule job</returns>
    public object ScheduleWriteAllLinkDatabase(Scheduler.JobCompletionCallback<bool>? completionCallback = null, 
        object? group = null, TimeSpan delay = new TimeSpan(), Scheduler.Priority priority = Scheduler.Priority.Medium, bool forceRead = false)
    {
        // Since this is a potentially long operation, it should be designed in such a way that repeat calls
        // create minimum overhead after first call if the database did not change from under us.

        return InsteonScheduler.Instance.AddAsyncJob(
            $"Updating Device Database - {DisplayNameAndId}",
            () => TryWriteAllLinkDatabaseAsync(forceRead: forceRead),
            completionCallback,
            async () => !await IsUnreachableAsync(), 
            group,
            delay, priority);
    }

    /// <summary>
    /// Whether this device needs synchronization with the physical device
    /// </summary>
    /// <returns></returns>
    public bool DeviceNeedsSync =>
        PropertiesSyncStatus == SyncStatus.Unknown ||
        PropertiesSyncStatus == SyncStatus.Changed ||
        (HasChannels && DeviceDriver.HasChannelProperties && 
            Channels.Any(c => c.PropertiesSyncStatus == SyncStatus.Unknown || 
                         c.PropertiesSyncStatus == SyncStatus.Changed)) ||
        AllLinkDatabase.LastStatus == SyncStatus.Unknown ||
        AllLinkDatabase.LastStatus == SyncStatus.Changed;

    /// <summary>
    /// Schedule sync of properties, channels, or all-link database if needed (if not synchronized)
    /// Returns a job and calls the completion callback only if the device actually had to be brought up to date.
    /// </summary>
    /// <param name="forceRead">Force reading properties, channels, and all-link database from the physical device prior to writing to it</param>
    /// <returns>job if a sync operation has started, null if no sync was needed</returns>
    public object? ScheduleSync(Scheduler.JobCompletionCallback<bool>? completionCallback = null, object? group = null, TimeSpan delay = new TimeSpan(), 
        Scheduler.Priority priority = Scheduler.Priority.Medium, bool forceRead = false)
    {
        if (!DeviceNeedsSync)
        {
            return null;
        }

        // If the device status is known to be unreachable, don't try to sync
        // The user can go to the device view and attempt to connect it.
        if (connectionStatus == ConnectionStatus.Disconnected || connectionStatus == ConnectionStatus.GatewayError)
        {
            return null;
        }

        object? localGroup = InsteonScheduler.Instance.AddGroup($"Updating Device {DisplayNameAndId}", completionCallback, group);

        // Then sync device properties
        if (PropertiesSyncStatus == SyncStatus.Unknown || forceRead)
        {
            // If no property was changed yet (status unknown), we read the property values into this device (forceSync: true)
            ScheduleReadDeviceProperties(null, localGroup, delay, priority, forceSync: true);
        }

        if (PropertiesSyncStatus == SyncStatus.Changed)
        {
            // If at least one property has changed already, write all properties values to the physical device.
            // TODO: consider maintaining that state per property to avoid overriding values in the physical device.
            ScheduleWriteDeviceProperties(null, localGroup, delay, priority);
        }

        // Then sync channel properties if the device has any
        if (HasChannels && DeviceDriver.HasChannelProperties)
        {
            bool changed = false;

            foreach (Channel channel in Channels)
            {
                // Same as for device properties (see above)
                if (channel.PropertiesSyncStatus == SyncStatus.Unknown || forceRead)
                {
                    ScheduleReadChannelProperties(channel.Id, null, localGroup, delay, priority, forceSync: true);
                }

                if (channel.PropertiesSyncStatus == SyncStatus.Changed)
                {
                    ScheduleWriteChannelProperties(channel.Id, null, localGroup, delay, priority);
                    changed = true;
                }
            }

            if (changed)
            {
                // If one of the channels had changed, also write the properties stored as bit masks in the device
                ScheduleWriteChannelsBitMasks(null, localGroup, delay, priority);
            }
        }

        // And finally sync links
        if (AllLinkDatabase.LastStatus != SyncStatus.Synced || forceRead)
        {
            ScheduleReadAllLinkDatabase(null, localGroup, delay, priority, forceRead: forceRead);
        }

        if (AllLinkDatabase.LastStatus == SyncStatus.Changed)
        {
            ScheduleWriteAllLinkDatabase(null, localGroup, delay, priority);
        }

        if (IsRemoteLinc)
        {
            ScheduleCleanUpAfterSync();
        }

        return localGroup;
    }

    /// <summary>
    /// Schedule a clean-up after a sync operation, e.g., letting remotelinc go back to sleep
    /// </summary>
    /// <param name="completionCallback"></param>
    /// <returns></returns>
    internal object ScheduleCleanUpAfterSync(Scheduler.JobCompletionCallback<bool>? completionCallback = null)
    {
        return InsteonScheduler.Instance.AddAsyncJob(
            $"Cleaning up after sync - {DisplayNameAndId}",
            TryCleanUpAfterSyncAsync,
            completionCallback);
    }

    // -----------------------------------------------------------------------------------
    // Async implementations of the scheduled jobs
    // ------------------------------------------------------------------------------------

    // Try to ping device to check whether Hub can communicate with device.
    internal async Task<Device.ConnectionStatus> TryPingAsync()
    {
        return await DeviceDriver.TryPingAsync(maxAttempts: 1);
    }

    // Try cleaning up after a Sync operation, e.g., letting remotelinc go back to sleep
    private async Task<bool> TryCleanUpAfterSyncAsync()
    {
        return await DeviceDriver.TryCleanUpAfterSyncAsync();
    }

    // Acquire the product information data from the device if we don't have it already.
    // This does NOT use DeviceDriver and can therefore be called before creating a DeviceDriver.
    // It should be called to acquire this information on a newly added device.
    internal async Task<bool> TryReadProductDataAsync(bool force = false)
    {
        if (!IsProductDataKnown || EngineVersion == 0 || force)
        {
            var productData = (Id == House.Gateway.DeviceId) ?
                await ProductData.GetIMProductDataAsync(House) :
                await ProductData.GetDeviceProductDataAsync(House, Id);

            if (productData == null)
            {
                // Try to get just the engine version (which will succeed even if the device
                // is not connected to the hub) to show that information in the UI.
                EngineVersion = await ProductData.GetInsteonEngineVersionAsync(House, Id);
                return false;
            }

            CategoryId = productData.CategoryId;
            SubCategory = productData.SubCategory;
            ProductKey = productData.ProductKey;
            Revision = productData.Revision;
            EngineVersion = productData.EngineVersion;
            IsProductDataKnown = true;
        }
        return true;
    }

    // Whether we already have the ProductData for this device
    internal bool IsProductDataKnown = false;

    // Try to read physical device properties from the network and notifies via PropertiesSyncStatusChanged event
    // Properties are read into the device driver, but hard propagated to this logical device only if forceSync is true
    // Returns: sucess or failure
    internal async Task<bool> TryReadDevicePropertiesAsync(bool forceSync)
    {
        bool success = true;

        // Fail silently if device is disconnected so as to not trigger a retry
        if (!await IsUnreachableAsync())
        {
            // We defer the sync status updates until we have read all the properties
            // and indicate that properties were modified by syncing with the physical device, not by the user.
            using (new Common.DeferredExecution<bool>(UpdatePropertiesSyncStatus, param: true,
                deferExecutionCallback: (bool defer) => deferPropertiesSyncStatusUpdate = defer))
            {
                // Read operating flags
                if (DeviceDriver.HasOperatingFlags)
                {
                    if (await DeviceDriver.TryReadOperatingFlagsAsync())
                    {
                        // If we had no operatingFlags or opflag2 property persisted with the model,
                        // initialize with set flags in the physical device 
                        if (forceSync || !PropertyBag.Exists(OperatingFlagsName))
                        {
                            this.OperatingFlags |= DeviceDriver.OperatingFlags;
                        }

                        if (forceSync || !PropertyBag.Exists(OpFlags2Name))
                        {
                            this.OpFlags2 |= DeviceDriver.OpFlags2;
                        }

                        // Channels may have changed as operating flags control the number of channels
                        // on a certain devices such as KeypadLinc for example
                        EnsureChannels();
                    }
                    else
                    {
                        success = false;
                    }
                }

                // Read other properties
                if (DeviceDriver.HasOtherProperties)
                {
                    if (await DeviceDriver.TryReadLEDBrightnessAsync())
                    {
                        if (forceSync)
                        {
                            this.LEDBrightness = DeviceDriver.LEDBrightness;
                        }
                    }
                    else
                    {
                        success = false;
                    }

                    if (await DeviceDriver.TryReadOnLevelAsync())
                    {
                        if (forceSync)
                        {
                            this.OnLevel = DeviceDriver.OnLevel;
                        }
                    }
                    else
                    {
                        success = false;
                    }

                    if (await DeviceDriver.TryReadRampRateAsync())
                    {
                        if (forceSync)
                        {
                            this.RampRate = DeviceDriver.RampRate;
                        }
                    }
                    else
                    {
                        success = false;
                    }
                }
            }
        }

        OnTryReadWriteComplete(success);
        return success;
    }

    // Try to write properties to the physical device via the network 
    // Returns success or failure
    private async Task<bool> TryWriteDevicePropertiesAsync()
    {
        bool success = true;

        // Fail silently if device is disconnected so as to not trigger a retry
        if (!await IsUnreachableAsync())
        {
            if (DeviceDriver.HasOperatingFlags)
            {
                // Then write
                if (!await DeviceDriver.TryWriteOperatingFlagsAsync(OperatingFlags, OpFlags2))
                {
                    success = false;
                }
            }

            if (DeviceDriver.HasOtherProperties)
            {
                if (!await DeviceDriver.TryWriteLEDBrightnessAsync(LEDBrightness))
                {
                    success = false;
                }

                if (!await DeviceDriver.TryWriteOnLevelAsync(OnLevel))
                {
                    success = false;
                }

                if (!await DeviceDriver.TryWriteRampRateAsync(RampRate))
                {
                    success = false;
                }
            }

            // Reflect property changes in the sync status
            UpdatePropertiesSyncStatus(afterSync: true);
        }

        OnTryReadWriteComplete(success);
        return success;
    }

    // Try to read all channels properties from the physical device 
    // Properties are read into the device driver, but hard propagated to this logical device only if forceSync is true
    // Returns: success or failure
    private async Task<bool> TryReadChannelsPropertiesAsync(bool forceSync)
    {
        bool success = true;

        // Fail silently if device is disconnected so as to not trigger a retry
        if (!await IsUnreachableAsync())
        {
            EnsureChannels();

            if (ChannelCount > 1)
            {
                Debug.Assert(Channels.Count == ChannelCount);

                for (int i = 0; i < Channels.Count; i++)
                {
                    if (!await Channels[i].TryReadChannelPropertiesAsync(forceSync))
                    {
                        success = false;
                    }
                }
            }
        }

        OnTryReadWriteComplete(success);
        return success;
    }

    // Try to write all channels properties to the physical device 
    // Properties are read in the physical channels but this logical device's channels remains unchanged
    // Returns: success or failure
    private async Task<bool> TryWriteChannelsPropertiesAsync()
    {
        bool success = true;

        // Fail silently if device is disconnected so as to not trigger a retry
        if (!await IsUnreachableAsync())
        {
            // Now write the properties that have a per channel set command
            // This will update the property synced flags in each ChannelViewModel
            // including for the ToggleMode property written separately above
            for (int i = 0; i < ChannelCount; i++)
            {
                if (!await Channels[i].TryWriteChannelPropertiesAsync())
                {
                    success = false;
                }
            }
        }

        OnTryReadWriteComplete(success);
        return success;
    }

    // Try to read properties of a channel from the physical device
    // Returns: success or failure
    private async Task<bool> TryReadChannelPropertiesAsync(int channelId, bool forceSync)
    {
        bool success = true;

        // Fail silently if device is disconnected so as to not trigger a retry
        if (!await IsUnreachableAsync())
        {
            Channel channel = GetChannel(channelId);
            success = await channel.TryReadChannelPropertiesAsync(forceSync);
        }

        OnTryReadWriteComplete(success);
        return success;
    }

    // Try to write properties of a channel to the physical device
    // Returns: success or failure
    private async Task<bool> TryWriteChannelPropertiesAsync(int channelId)
    {
        bool success = true;

        // Fail silently if device is disconnected so as to not trigger a retry
        if (!await IsUnreachableAsync())
        {
            Channel channel = GetChannel(channelId);
            success = await channel.TryWriteChannelPropertiesAsync();
        }

        OnTryReadWriteComplete(success);
        return success;
    }

    // Try to write channel properties that are stored as bit masks on the device itself
    // Returns: success or failure
    private async Task<bool> TryWriteChannelsBitMasksAsync()
    {
        Debug.Assert(DeviceDriver is KeypadLincDriver);
        bool success = true;

        // Fail silently if device is disconnected so as to not trigger a retry
        if (!await IsUnreachableAsync())
        {
            if (IsKeypadLinc && DeviceDriver is DeviceDriver pdp && pdp.Channels != null)
            {
                bool toggleModeSynced = true;
                bool ledOnSynced = true;
                byte nonToggleMask = 0;
                byte onOffMask = 0;
                byte ledOnMask = 0;

                // Accumulate button Toggle/LEDOn masks that will be set in one command
                // Detect whether we need to write 
                for (int i = 0; i < ChannelCount; i++)
                {
                    switch (Channels[i].ToggleMode)
                    {
                        case ToggleMode.On:
                            nonToggleMask |= (byte)(1 << i);
                            onOffMask |= (byte)(1 << i);
                            break;
                        case ToggleMode.Off:
                            nonToggleMask |= (byte)(1 << i);
                            break;
                    }

                    if (Channels[i].ToggleModeSyncStatus == SyncStatus.Changed)
                    {
                        toggleModeSynced = false;
                    }
                    else
                    {
                        Debug.Assert(Channels[i].ToggleModeSyncStatus == SyncStatus.Unknown || Channels[i].ToggleMode == pdp.Channels[i].ToggleMode);
                    }

                    if (Channels[i].LEDOn)
                    {
                        ledOnMask |= (byte)(1 << i);
                    }

                    if (Channels[i].LEDOnSyncStatus == SyncStatus.Changed)
                    {
                        ledOnSynced = false;
                    }
                    else
                    {
                        Debug.Assert(Channels[i].LEDOnSyncStatus == SyncStatus.Unknown || Channels[i].LEDOn == pdp.Channels[i].LEDOn);
                    }
                }

                // Set Toggle masks
                // This will update the ToggleMode on all physical channels
                if (!toggleModeSynced)
                {
                    if (await (DeviceDriver as KeypadLincDriver)!.TryWriteToggleModeMask(nonToggleMask, onOffMask))
                    {
                        for (int i = 0; i < ChannelCount; i++)
                        {
                            Channels[i].ToggleModeLastUpdate = DateTime.Now;
                        }
                    }
                    else
                    {
                        success = false;
                    }
                }

                // Set LEDOn mask
                // This will update the LEDOn property on all physical channels
                if (!ledOnSynced)
                {
                    if (await (DeviceDriver as KeypadLincDriver)!.TryWriteLEDOnMask(ledOnMask))
                    {
                        for (int i = 0; i < ChannelCount; i++)
                        {
                            Channels[i].LEDOnLastUpdate = DateTime.Now;
                        }
                    }
                    else
                    {
                        success = false;
                    }
                }
            }
        }

        OnTryReadWriteComplete(success);
        return success;
    }

    // Try to read the next unread record of the all-link database from the physical device
    // and merge it into the corresponding record of this device All-link database.
    // Called with restart parameter to true to start reading the first link in the database,
    // then called with that parameter set to false to read all other link records in sequence.
    // Returns: success or failure
    private async Task<(bool success, bool done)> TryReadAllLinkDatabaseStepAsync(bool restart, bool force)
    {
        (bool success, bool done) ret = (true, true);

        // Fail silently if device is disconnected so as to not trigger a retry
        if (!await IsUnreachableAsync())
        {
            ret = await DeviceDriver.TryReadAllLinkDatabaseStepAsync(AllLinkDatabase, restart, force);
        }

        SetAllLinkDatabaseSyncStatus();
        OnTryReadWriteComplete(ret.success);
        return ret;
    }

    // Try to read yet unread records of the ALL-Link database from the physical device and
    // merge them into the All-Link Database of this device
    // Parameter: forceRead force reading from start of the database, regardless of what was read before
    // Returns: success or failure
    // Marked "internal" for unit-testing
    internal async Task<bool> TryReadAllLinkDatabaseAsync(bool forceRead = false)
    {
        bool success = true;

        // Fail silently if device is disconnected so as to not trigger a retry
        if (!await IsUnreachableAsync())
        {
            success = await DeviceDriver.TryReadAllLinkDatabaseAsync(AllLinkDatabase, force: forceRead);
        }

        SetAllLinkDatabaseSyncStatus();
        OnTryReadWriteComplete(success);
        return success;
    }

    // Try to write modified or new records of the AllLink database to the physical device
    // Parameter: forceRead forces reading the database from the physical device prior to writing to it
    // Returns: success or failure
    private async Task<bool> TryWriteAllLinkDatabaseAsync(bool forceRead = false)
    {
        bool success = true;

        // Fail silently if device is disconnected so as to not trigger a retry
        if (!await IsUnreachableAsync())
        {
            if (await DeviceDriver.TryWriteAllLinkDatabaseAsync(AllLinkDatabase, forceRead))
            {
                // If all records are synced, set the passed-in database status to synced and return success
                success = SetAllLinkDatabaseSyncStatus() == SyncStatus.Synced;
            }
            else
            {
                success = false;
            }
        }

        OnTryReadWriteComplete(success);
        return success;
    }

    // Helper to compute and set the sync status of the database
    // Returns the sync status that was set
    private SyncStatus SetAllLinkDatabaseSyncStatus()
    {
        SyncStatus syncStatus = SyncStatus.Synced;
        foreach (var record in AllLinkDatabase)
        {
            if (record.SyncStatus == SyncStatus.Unknown && syncStatus != SyncStatus.Changed)
            {
                syncStatus = SyncStatus.Unknown;
            }
            if (record.SyncStatus == SyncStatus.Changed)
            {
                syncStatus = SyncStatus.Changed;
                break;
            }
        }

        AllLinkDatabase.LastStatus = syncStatus;
        AllLinkDatabase.LastUpdate = DateTime.Now;
        return syncStatus;
    }


    // -----------------------------------------------------------------------------------
    // Useful tasks on the model
    // ------------------------------------------------------------------------------------

    /// <summary>
    /// Purge garbage links from the IM memory
    /// - controller Links for groups 200 and above
    /// - responder links with no data1-2-3
    /// </summary>
    public void SchedulePurgeIMLinks(Scheduler.JobCompletionCallback<bool>? completionCallback)
    {
        if (!IsHub)
        {
            completionCallback?.Invoke(false);
        }

        ScheduleReadAllLinkDatabase(
            (success) =>
            {
                foreach (AllLinkRecord record in AllLinkDatabase)
                {
                    if (record.IsController && record.Group >= 200)
                    {
                        AllLinkDatabase.RemoveRecord(new(record) { SyncStatus = SyncStatus.Changed });
                    }

                    if (record.IsResponder && record.Data1 == 0 && record.Data2 == 0 && record.Data3 == 0)
                    {
                        AllLinkDatabase.RemoveRecord(new(record) { SyncStatus = SyncStatus.Changed });
                    }
                }
            });
    }

    /// <summary>
    /// Check whether this device has a link record of any kind to a given device 
    /// </summary>
    /// <param name="deviceId"></param>
    /// <returns>true if that link record exists</returns>
    public bool HasLinkToDevice(InsteonID deviceId)
    {
        if (this.AllLinkDatabase != null)
        {
            if (AllLinkDatabase.Contains(GetHashCodeFromId(deviceId)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Retrieve all links to a specific device in this device
    /// </summary>
    /// <param name="deviceId"></param>
    /// <returns>List of links</returns>
    public List<AllLinkRecord>? GetLinksToDevice(InsteonID deviceId)
    {
        if (this.AllLinkDatabase != null)
        {
            if (AllLinkDatabase.TryGetMatchingEntries(GetHashCodeFromId(deviceId), out List<AllLinkRecord>? linkRecords))
            {
                return linkRecords;
            }
        }

        return null;
    }

    /// <summary>
    /// Check whether this device has a controller or responder link record to a given device, on a given group
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="group"></param>
    /// <param name="controller"></param>
    /// <returns></returns>
    public bool HasLinkToDevice(InsteonID deviceId, int group, bool controller)
    {
        if (this.AllLinkDatabase != null)
        {
            AllLinkRecord record = new AllLinkRecord(deviceId, controller, (byte)group);
            if (AllLinkDatabase.Contains(record, ReferenceEqualityComparer.Instance))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Check whether this device is fully linked (both directions) as a controller to a given responder device, on a given group
    /// </summary>
    /// <param name="responder"></param>
    /// <param name="group"></param>
    /// <returns></returns>
    public bool IsControllerOf(Device responder, int group)
    {
        return HasLinkToDevice(responder.Id, group, controller: true) &&
               responder.HasLinkToDevice(this.Id, group, controller: false);
    }

    /// <summary>
    /// Check whether this device is a responder to the Hub on group 0
    /// This is necessary for the hub to send commands to this device
    /// </summary>
    /// <returns></returns>
    public bool IsResponderToHubGroup0()
    {
        return House.Hub?.IsControllerOf(this, 0) ?? false;
    }

    /// <summary>
    /// Find the destination device of a link in this device database
    /// </summary>
    /// <param name="allLinkRecord"></param>
    /// <returns></returns>
    public Device? GetDestDevice(AllLinkRecord allLinkRecord)
    {
        return House.GetDeviceByID(allLinkRecord.DestID);
    }

    /// <summary>
    /// Same as above with Try pattern
    /// </summary>
    /// <param name="allLinkRecord"></param>
    /// <param name="destDevice"></param>
    /// <returns></returns>
    public bool TryGetDestDevice(AllLinkRecord allLinkRecord, [NotNullWhen(true)] out Device? destDevice)
    {
        destDevice = GetDestDevice(allLinkRecord);
        return destDevice != null;
    }

    /// <summary>
    ///  Find the complement (other half) of a complete Insteon link in this device database
    /// </summary>
    /// <param name="allLinkRecord">All-Link record to find the complement of</param>
    /// <param name="complementLinkRecords">returns list of complement link records</param>
    /// <returns>true if at least one complement link found</returns>
    public bool TryFindLinkComplementLinks(AllLinkRecord allLinkRecord, [NotNullWhen(true)] out List<AllLinkRecord>? complementLinkRecords)
    {
        bool fReturn = false;
        complementLinkRecords = null;

        // Get the destination device, i.e., the source device for the complement link
        if (TryGetDestDevice(allLinkRecord, out Device? destDevice))
        {
            AllLinkDatabase destDatabase = destDevice.AllLinkDatabase;

            // Get the list of candidate links on the destination device
            if (destDatabase.TryGetMatchingEntries(GetHashCodeFromId(this.Id), out List<AllLinkRecord>? destMatchingRecords))
            {
                // And filter only links that are in use and complement to allLinkRecord
                foreach (var destRecord in destMatchingRecords!)
                {
                    if (destRecord.IsInUse)
                    {
                        if (destRecord.Group == allLinkRecord.Group)
                        {
                            if (destRecord.IsController != allLinkRecord.IsController)
                            {
                                // We found a complement link
                                // Create our returned list if needed
                                if (complementLinkRecords == null)
                                {
                                    complementLinkRecords = new List<AllLinkRecord>();
                                }

                                // Add the link to our returned list and return success
                                complementLinkRecords.Add(destRecord);
                                fReturn = true;
                            }
                        }
                    }
                }
            }
        }

        return fReturn;
    }

    /// <summary>
    /// Remove all duplicate links in this device database 
    /// This only affects the model, the background sync will commit changes to the physical device.
    /// </summary>
    public void RemoveDuplicateLinks()
    {
        AllLinkDatabase.RemoveDuplicateRecords();
    }

    /// <summary>
    /// Remove all links in this device database to non-existant devices.
    /// This only affects the model, the background sync will commit changes to the physical device.
    /// </summary>
    public void RemoveStaleLinks()
    {
        var linksToRemove = new List<AllLinkRecord>();

        foreach (var record in AllLinkDatabase)
        {
            if (record.IsInUse)
            {
                if (record.DestID == Id || GetDestDevice(record) == null)
                {
                    linksToRemove.Add(record);
                }
            }
        }

        foreach (var record in linksToRemove)
        {
            AllLinkDatabase.RemoveRecord(record);
        }
    }

    /// <summary>
    /// Replace this device by another configured in the same way:
    /// - Copy properties and channels from this device to the replacement device,
    /// - Move all links in this device database to the replacement device (overriding all links on the replacement device),
    /// - Update all links pointing to this device to point to the replacement device.
    /// This device database is emptied but this device is not removed from the house.
    /// Schedule removing after calling this if desired.
    /// </summary>
    /// <param name="replacementDeviceId">Device to move the link database to</param>
    /// <returns>true if success</returns>
    public bool TryReplaceDeviceBy(InsteonID replacementDeviceId)
    {
        bool success = true;

        // Move AllLink database from the source to the replacement device
        var replacementDevice = House.GetDeviceByID(replacementDeviceId);
        if (replacementDevice != null)
        {
            // This does not handle hubs
            if (IsHub)
                return false;

            // Ensure that this and replacement devices have are compatible
            // TODO: move this to DeviceKind and handle all compatible devices
            if (DeviceKind.GetModelType(CategoryId, SubCategory) !=
                DeviceKind.GetModelType(replacementDevice.CategoryId, replacementDevice.SubCategory))
            {
                return false;
            }

            // Copy properties
            replacementDevice.CopyPropertiesFrom(this);

            // Move channels if we have any
            // and mark them changed to cause them to sync with the physical replacement device
            if (HasChannels && replacementDevice.HasChannels)
            {
                replacementDevice.Channels = new Channels(Channels).AddChannelObserver(House.ModelObserver);
                replacementDevice.Channels.MarkAllChannelsChanged();
            }

            // Move all links in this device database to the replacement device database
            // and mark them changed to cause them to sync with the physical device.
            // TODO: this assignment ignores the fact that the replacement device, if newly added, has at least one responder record
            // toward the hub, which might have to be retained rather than overwritten with the one of the source device
            replacementDevice.AllLinkDatabase = this.AllLinkDatabase;
            replacementDevice.AllLinkDatabase.MarkAllRecordsChanged();

            // This device does not have any link anymore, assign it an empty database
            this.AllLinkDatabase = new AllLinkDatabase() { LastStatus = SyncStatus.Changed }.AddObserver(House.ModelObserver);

            // Fix-up links pointing to this device to point to the replacement device
            FixupLinksToThis(replacementDeviceId);

            // Fix-up all scenes referring to this device to refer to the replacement device instead
            House.Scenes.ReplaceDevice(Id, replacementDeviceId);
        }
        else
        {
            success = false;
        }

        return success;
    }

    /// <summary>
    /// Modify all links in any device pointing to this device to point to another device
    /// Note: leave the link from group 0 of the hub intact, assuming it will be removed if/when this device gets removed
    /// </summary>
    /// <param name="replacementDeviceId">Device to move the links to</param>
    public void FixupLinksToThis(InsteonID replacementDeviceId)
    {
        foreach (Device device in House.Devices)
        {
            if (device.AllLinkDatabase.TryGetMatchingEntries(GetHashCodeFromId(this.Id), out List<AllLinkRecord>? matchingRecords))
            {
                foreach (var matchingRecord in matchingRecords!)
                {
                    Debug.Assert(matchingRecord.DestID == this.Id);
                    if (!device.IsHub || matchingRecord.Group != 0)
                    {
                        AllLinkRecord newRecord = new(matchingRecord){ DestID = replacementDeviceId, SyncStatus = SyncStatus.Changed };
                        device.AllLinkDatabase.ReplaceRecord(matchingRecord, newRecord);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Copy properties and channels from this device to another.
    /// Copy all links in this device database to the other device, overriding all links on that other device.
    /// Copy all links pointing to this device to new links pointing to that other device
    /// TODO: Add the other device to same scenes as this device
    /// </summary>
    /// <param name="copyDeviceId">Device to move the link database to</param>
    /// <returns>true if success</returns>
    public bool TryCopyDeviceTo(InsteonID copyDeviceId)
    {
        bool success = true;

        // Move AllLink database from the source to the replacement device
        var copyDevice = House.GetDeviceByID(copyDeviceId);
        if (copyDevice != null)
        {
            // This does not handle hubs
            if (IsHub)
                return false;

            // Ensure that this and replacement devices have are compatible
            // TODO: move this to DeviceKind and handle all compatible devices
            if (DeviceKind.GetModelType(CategoryId, SubCategory) !=
                DeviceKind.GetModelType(copyDevice.CategoryId, copyDevice.SubCategory))
            {
                return false;
            }

            // Copy properties
            copyDevice.CopyPropertiesFrom(this);

            // Deep copy channels if we have any
            // and mark them changed to cause them to sync with the physical replacement device
            if (HasChannels && ChannelCount == copyDevice.ChannelCount)
            {
                copyDevice.Channels = new Channels(Channels).AddChannelObserver(House.ModelObserver);
                copyDevice.Channels.MarkAllChannelsChanged();
            }

            // Move all links in this device database to the copy device database
            // TODO: this assignment ignores the fact that the copy device, if newly added, has at least one responder record
            // toward the hub, which might have to be retained rather than overwritten with the one of the source device
            copyDevice.AllLinkDatabase = new AllLinkDatabase(AllLinkDatabase, idToExclude: copyDevice.Id) { IsRead = false }
                .AddObserver(House.ModelObserver);
            copyDevice.AllLinkDatabase.MarkAllRecordsChanged();

            // Duplicate links in other devices pointing to this device into links pointing to the copy device
            DuplicateLinksToThis(copyDeviceId);

            // Fix-up all scenes referring to this device to refer to also refer to the replacement device
            House.Scenes.CopyDevice(Id, copyDeviceId);
        }
        else
        {
            success = false;
        }

        return success;
    }

    // Duplicate all links in any device pointing to this device into a link pointing to another device
    // This only affects the model, call ScheduleWriteAllLinkDatabase afterward to commit changes to the physical network
    // or wait for the backgroud sync to commit changes
    private void DuplicateLinksToThis(InsteonID copyDeviceId)
    {
        foreach (Device device in House.Devices)
        {
            if (device.AllLinkDatabase.TryGetMatchingEntries(GetHashCodeFromId(this.Id), out List<AllLinkRecord>? matchingRecords))
            {
                foreach (var matchingRecord in matchingRecords!)
                {
                    if (matchingRecord.IsInUse)
                    {
                        Debug.Assert(matchingRecord.DestID == this.Id);
                        AllLinkRecord newRecord = new(matchingRecord){ DestID = copyDeviceId, SyncStatus = SyncStatus.Changed };
                        device.AllLinkDatabase.AddRecord(newRecord);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Ensure that the first link in the database of this device is a responder from the hub, group 0
    /// This ensures minimal NAK rate when a command is sent to this device
    /// This only affects the model, call ScheduleWriteAllLinkDatabase afterward to commit changes to the physical network
    /// or wait for the backgroud sync to commit changes
    /// </summary>
    /// <returns></returns>
    public bool EnsureLinkToHubIsFirst()
    {
        if (!IsHub)
        {
            AllLinkRecord firstRecord = AllLinkDatabase[0];
            if (firstRecord.DestID != House.Gateway.DeviceId || firstRecord.Group != 0 || !firstRecord.IsResponder || !firstRecord.IsInUse)
            {
                // First look for a responder to the hub on group 0
                AllLinkRecord? hubRecord = null;
                {
                    if (AllLinkDatabase.TryGetMatchingEntries(new AllLinkRecord(House.Gateway.DeviceId, isController: false, 0), IdGroupTypeComparer.Instance, out List<AllLinkRecord>? matchingRecords))
                    {
                        foreach (AllLinkRecord record in matchingRecords!)
                        {
                            if (record.IsInUse)
                            {
                                hubRecord = record;
                                break;
                            }
                        }
                    }
                }

                if (hubRecord != null)
                {
                    // If found, swap with first link in database
                    int i = AllLinkDatabase.IndexOf(hubRecord);
                    AllLinkDatabase.SwapRecords(0, i);
                }
                else
                {
                    // We did not find a responder from the hub on group 0
                    // We should try connecting that device again
                    return false;
                }
            }
        }

        return true;
    }
}
