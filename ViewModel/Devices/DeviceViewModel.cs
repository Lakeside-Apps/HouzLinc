/* Copyright 2022 Christian Fortini
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
using Common;
using ViewModel.Links;
using Insteon.Model;
using ViewModel.Scenes;
using System.ComponentModel;
using Insteon.Base;
using ViewModel.Base;
using System.Diagnostics.CodeAnalysis;

namespace ViewModel.Devices;

[Bindable(true)]
public class DeviceViewModel : LinkHostViewModel, IDeviceObserver, IRoomsObserver
{
    // This can't be private because of Activator requirements
    // Please use GetOrCreate methods to create the proper type of view model
    public DeviceViewModel(Device device) : base(device)
    {
        LightOnCommand = new RelayCommand(LightOn);
        LightOffCommand = new RelayCommand(LightOff);
    }

    // Helper to create the proper DeviceViewModel type for a given device
    private static DeviceViewModel Create(Device device)
    {
        return Create(GetDeviceViewModelType(device), device);
    }

    // Helper to create the specified DeviceViewModel type for a given device
    private static DeviceViewModel Create(Type viewModelType, Device device)
    {
        DeviceViewModel deviceViewModel = (Activator.CreateInstance(viewModelType, device) as DeviceViewModel)!;
        deviceViewModels?.Add(device.Id, deviceViewModel);
        device.AddObserver(deviceViewModel);
        return deviceViewModel;
    }

    // Helper to get the type of view model to create for a device
    private static Type GetDeviceViewModelType(Device device)
    {
        var deviceModel = DeviceKind.GetModelType(device.CategoryId, device.SubCategory);
        foreach(var (model, viewModelType) in deviceViewModelTypeFromModel)
        {
            if (model == deviceModel)
            {
                return viewModelType;
            }
        }
        return typeof(DeviceViewModel);
    }

    // Which view model to create for a given device model
    private static (DeviceKind.ModelType model, Type viewModelType)[] deviceViewModelTypeFromModel =
    {
        (DeviceKind.ModelType.Unknown, typeof(DeviceViewModel)),
        (DeviceKind.ModelType.Hub, typeof(HubViewModel)),
        (DeviceKind.ModelType.PowerLinc, typeof(DeviceViewModel)),
        (DeviceKind.ModelType.SmartLinc, typeof(DeviceViewModel)),
        (DeviceKind.ModelType.SerialLinc, typeof(DeviceViewModel)),
        (DeviceKind.ModelType.SwitchLincRelay, typeof(DeviceViewModel)),
        (DeviceKind.ModelType.SwitchLincDimmer, typeof(DeviceViewModel)),
        (DeviceKind.ModelType.KeypadLincRelay, typeof(KeypadLincViewModel)),
        (DeviceKind.ModelType.KeypadLincDimmer, typeof(KeypadLincViewModel)),
        (DeviceKind.ModelType.InlineLincRelay, typeof(DeviceViewModel)),
        (DeviceKind.ModelType.InlineLincDimmer, typeof(DeviceViewModel)),
        (DeviceKind.ModelType.RemoteLinc, typeof(RemoteLincViewModel)),
        (DeviceKind.ModelType.ControlLinc, typeof(DeviceViewModel)),
        (DeviceKind.ModelType.LampLinc, typeof(DeviceViewModel)),
        (DeviceKind.ModelType.SocketLinc, typeof(DeviceViewModel)),
        (DeviceKind.ModelType.OutletLinc, typeof(DeviceViewModel)),
        (DeviceKind.ModelType.FanLinc, typeof(DeviceViewModel)),
        (DeviceKind.ModelType.DimmableBulb, typeof(DeviceViewModel)),
        (DeviceKind.ModelType.ApplianceLinc, typeof(DeviceViewModel)),
        (DeviceKind.ModelType.TimerLinc, typeof(DeviceViewModel)),
        (DeviceKind.ModelType.LowVoltageLinc, typeof(DeviceViewModel)),
    };

    /// <summary>
    /// Get a device view model for a given device
    /// </summary>
    /// <param name="deviceId"></param>
    /// <returns></returns>
    [MemberNotNull("deviceViewModels")]
    public static DeviceViewModel? GetById(InsteonID deviceId)
    {
        deviceViewModels ??= new Dictionary<InsteonID, DeviceViewModel>();

        if (deviceViewModels.TryGetValue(deviceId, out DeviceViewModel? deviceViewModel))
            return deviceViewModel;

        return null;
    }

    /// <summary>
    /// Factory method.
    /// Looks up a view model for the given device, creates if none exists, 
    /// or if the existing view model is not of the right type.
    /// Garantees that only one view model will be created for a given device
    /// </summary>
    /// <param name="device">Device to build a view model for</param>
    /// <returns>the created view model</returns>
    public static DeviceViewModel GetOrCreate(Device device)
    {
        DeviceViewModel? deviceViewModel = GetById(device.Id);
        if (deviceViewModel == null)
        {
            deviceViewModel = Create(device);
        }
        else
        {
            var deviceViewModelType = GetDeviceViewModelType(device);
            if (deviceViewModel.GetType() != deviceViewModelType)
            {
                deviceViewModels.Remove(device.Id);
                device.RemoveObserver(deviceViewModel);
                deviceViewModel = Create(deviceViewModelType, device);
            }
        }

        return deviceViewModel;
    }

    /// <summary>
    /// Factory method. 
    /// Looks up a view model for the given device id, creates if none exists.
    /// Garantees that only one view model will be create for a given device id
    /// </summary>
    /// <param name="deviceId">The id of the device for which to create a view model</param>
    /// <returns>the found or created view model or null if none exists with this id</returns>
    public static DeviceViewModel? GetOrCreateById(House house, InsteonID deviceId)
    {
        var device = house.GetDeviceByID(deviceId);
        return device != null ? GetOrCreate(device) : null;
    }

    /// <summary>
    /// Looks up a view model for the given device key, creates if none exists.
    /// Garantees that only one view model will be created for a given key
    /// </summary>
    /// <param name="itemKey"></param>
    /// <returns>Found DeviceViewModel or null if no device exists with this key</returns>
    public static DeviceViewModel? GetOrCreateItemByKey(House house, string itemKey)
    {
        return GetOrCreateById(house, new InsteonID(itemKey));
    }

    // List of already created device view models
    // Allows to return already created view model for a given device, if any
    private static Dictionary<InsteonID, DeviceViewModel>? deviceViewModels;

    /// <summary>
    /// Returns the name of the data template to use to present this Device
    /// Derived classes can override
    /// </summary>
    public virtual string DeviceTemplateName => "SimpleDeviceView";

    /// <summary>
    /// Notifies this device view model that it is made active
    /// (presented on screen) or inactive (hidden from screen)
    /// Also called when the connected state changes (IsConnected)
    /// </summary>
    public override void ActiveStateChanged()
    {
        base.ActiveStateChanged();

        if (IsActive)
        {
            // Start listening to interesting changes in the model
            // other than our device
            Device.House.Rooms.AddObserver(this);

            // Read interesting info from the device
            // but only attempt it if the device is connected
            Device.ScheduleRetrieveConnectionStatus(status => 
            {
                if (status == Device.ConnectionStatus.Connected )
                {
                    // Update the current on-level on the live view of the device
                    getOnLevelJob = Device.ScheduleGetLightOnLevel((level) =>
                    {
                        if (level >= 0) { LightOnLevel = level; }
                    });

                    // Schedule a job to read the device properties in 5 seconds
                    // That job will be cancelled if the device is deactivated before that delay expires
                    readCurrentDevicePropertiesJob = Device.ScheduleReadDeviceProperties(
                        (bool success) => { readCurrentDevicePropertiesJob = null; },
                        delay: new TimeSpan(0, 0, 5),
                        forceSync: false);
                }
            }, 
            force: true);
        }
        else
        {
            // Cancel jobs to read info from the device if any is ongoing
            Device.CancelScheduledJob(readCurrentDevicePropertiesJob);
            readCurrentDevicePropertiesJob = null;
            Device.CancelScheduledJob(getOnLevelJob);
            getOnLevelJob = null;
            optCurrentChannelViewModel?.ActiveStateChanged(false);

            // Done listening to changes in the model
            Device.House.Rooms.RemoveObserver(this);
        }

        OnPropertyChanged(nameof(IsOnOffButtonShown));
    }
    private object? readCurrentDevicePropertiesJob;
    private object? getOnLevelJob;

    /// <summary>
    /// Should show the device on/off buttons in the device list
    /// </summary>
#if ANDROID || IOS
    public bool IsOnOffButtonShown => true;
#else
    public bool IsOnOffButtonShown => IsActive || IsPointerOver;
#endif

    // Show the On/Off button when the mouse is over the channel
    public override void PointerOverChanged()
    {
        OnPropertyChanged(nameof(IsOnOffButtonShown));
    }

    //-----------------------------------------------------------------------------
    // Readonly one-time bindable properties, not obtained from the physical device
    //-----------------------------------------------------------------------------
    public InsteonID Id => this.Device.Id;

    public override string ItemKey => Id.ToString();

    public bool IsDimmableLightingControl => this.Device.IsDimmableLightingControl;

    public string Powerline => this.Device.Powerline ? "Powerline" : "No Powerline";

    public string Radio => this.Device.Radio ? "Radio" : "No Radio";

    public bool IsHub => this.Device.IsHub;

    //-----------------------------------------------------------------------------
    // Readonly one-time bindable properties, not obtained from the physical device
    // pertaining to this device's channels and defined in LinkHostViewModel
    // ----------------------------------------------------------------------------

    public override bool HasChannels => this.Device.HasChannels;

    // Derived classes provide these from the state of the UI.
    // (1-based, 0 means no channel currently active)
    public override int CurrentChannelId => 0;

    // Name of the current channel, "Any" "if no current channel
    public override string CurrentChannelName
    {
        get
        {
            if (CurrentChannelId == 0)
                return "Any";
            else
                return GetChannelName(CurrentChannelId);
        }
    }

    //---------------------------------------------------------------------------
    // Readonly one-way bindable properties, not obtained from the physical Device
    //---------------------------------------------------------------------------
    public string DisplayNameAndId => DisplayName + " (" + Id + ")";

    public string LocationDisplayNameAndId => (Room != null ? Room + " - " : "") + DisplayName + " (" + Id + ")";

    public string DeviceIdInParens => "(" + Id + ")";

    //---------------------------------------------------------------------------
    // Read/write, two-way bindable bindable properties, not obtained from the 
    // physical device
    //---------------------------------------------------------------------------
    public string DisplayName
    {
        get => this.Device.DisplayName ?? string.Empty;
        set
        {
            if (value != this.Device.DisplayName)
            {
                this.Device.DisplayName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayNameAndId));
                OnPropertyChanged(nameof(LocationDisplayNameAndId));
            }
        }
    }

    public string Room
    {
        get => this.Device.Room ?? string.Empty;
        set
        {
            if (value != this.Device.Room)
            {
                this.Device.Room = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LocationDisplayNameAndId));
            }
        }
    }

    public bool HasRoom => Room != null && Room != "";

    public string Location
    {
        get => this.Device.Location ?? string.Empty;
        set
        {
            if (value != this.Device.Location)
            {
                this.Device.Location = value;
                OnPropertyChanged();
            }
        }
    }

    public bool HasLocation => Location != null && Location != "";

    public bool IsKeypadLinc => this.Device != null ? this.Device.IsKeypadLinc : false;

    public string Wattage
    {
        get => this.Device.Wattage > 0 ? this.Device.Wattage.ToString() + "W" : "n/a";
        set => this.Device.Wattage = int.Parse(value);
    }

    /// <summary>
    /// Indicate wherer the Device name, room and location can be edited in the UI at the moment
    /// </summary>
    public bool IsNameEditable
    {
        get => isNameEditable;
        set
        {
            if (isNameEditable != value)
            {
                isNameEditable = value;
                OnPropertyChanged();
            }
        }
    }
    bool isNameEditable;

    //---------------------------------------------------------------------------
    // Read only (one way bindable) propoerties, synced with the physical device
    //---------------------------------------------------------------------------

    /// <summary>
    /// Category
    /// Read only (one way bindable), synced from the physical device
    /// </summary>
    public string Category => Device.Category;

    /// <summary>
    /// ModelName, ModelNumber
    /// Read only (one time bindable), synced from the physical device
    /// </summary>
    public string ModelName => Device.ModelName;

    public string ModelNumber => Device.ModelNumber;

    public string ModelIconPath_72x72
    {
        get
        {
            string iconName = Device.ModelType;
            if (Device.IsKeypadLinc)
            {
                iconName += Device.Is8Button ? "8" : "6";
            }
            //return "..\\..\\Assets\\Devices\\" + iconName + "_72x72.png";
            return "ms-appx:///Assets/Devices/" + iconName + "_72x72.png";
        }
    }

    public string GetModelIconPath_72x72(int group)
    {
        string iconName = Device.ModelType;
        if (Device.IsKeypadLinc)
        {
            iconName += Device.Is8Button ? "8" : "6";
        }
        //return "..\\..\\Assets\\Devices\\" + iconName + "_NoBgrd_" + group + ".png";
        //return "..\\..\\Assets\\Devices\\" + iconName + "_72x72.png";
        return "ms-appx:///Assets/Devices/" + iconName + "_72x72.png";
    }

    /// <summary>
    /// Revision
    /// Read only (one-time bindable), obtained from the physical device during linking
    /// </summary>
    public string Revision => this.Device.Revision.ToString();

    /// <summary>
    /// Engine version (1-3)
    /// Read only (one-time bindable), obtained from the physical device during linking
    /// </summary>
    public string EngineVersion => this.Device.EngineVersion.ToString();

    //---------------------------------------------------------------------------
    // Read/write, 2-way bindable properties synced with the physical device
    //---------------------------------------------------------------------------

    /// <summary>
    /// Operating flags bits - Program Lock
    /// Read/write (two way bindable), synced with the physical device
    /// Change notification is fired in OnDevicePropertyChanged
    /// </summary>
    public bool ProgramLock
    {
        get => Device.ProgramLock;
        set
        {
            // This ensures properties have the same name as on the device
            // and change notifications coming from the device via IDeviceObserver
            // can flow to the UI
            Debug.Assert(nameof(ProgramLock) == nameof(Device.ProgramLock));
            Device.ProgramLock = value;
        }
    }

    /// <summary>
    /// Operating flags bits - LED on during Transmit
    /// Read/write (two way bindable), synced with the physical device
    /// Change notification is fired in OnDevicePropertyChanged
    /// </summary>
    public bool LEDOnTx
    {
        get => Device.LEDOnTx;
        set
        {
            // See above
            Debug.Assert(nameof(LEDOnTx) == nameof(Device.LEDOnTx));
            Device.LEDOnTx = value;
        }
    }

    /// <summary>
    /// Operating flags bits - Resume Dim 
    /// Read/write (two way bindable), synced with the physical device
    /// Change notification is fired in OnDevicePropertyChanged
    /// </summary>
    public bool ResumeDim
    {
        get => Device.ResumeDim;
        set
        {
            // See above
            Debug.Assert(nameof(ResumeDim) == nameof(Device.ResumeDim));
            Device.ResumeDim = value;
        }
    }

    /// <summary>
    /// Operating flags bits - LED On
    /// Read/write (two way bindable), synced with the physical device
    /// Change notification is fired in OnDevicePropertyChanged
    /// </summary>
    public virtual bool LEDOn
    {
        get => Device.LEDOn;
        set
        {
            // See above
            Debug.Assert(nameof(LEDOn) == nameof(Device.LEDOn));
            Device.LEDOn = value;
        }
    }

    /// <summary>
    /// Operating flags bits - Load SenseOn
    /// Read/write (two way bindable), synced with the physical device
    /// Change notification is fired in OnDevicePropertyChanged
    /// </summary>
    public bool LoadSenseOn
    {
        get => Device.LoadSenseOn;
        set
        {
            // See above
            Debug.Assert(nameof(LoadSenseOn) == nameof(Device.LoadSenseOn));
            Device.LoadSenseOn = value;
        }
    }

    /// <summary>
    /// RFEnabled (RF)
    /// Read/write (two way bindable), synced with the physical device
    /// Change notification is fired in OnDevicePropertyChanged
    /// </summary>
    public bool RFEnabled
    {
        get => Device.RFEnabled;
        set
        {
            // See above
            Debug.Assert(nameof(RFEnabled) == nameof(Device.RFEnabled));
            Device.RFEnabled = value;
        }
    }

    /// <summary>
    /// PowerlineEnabled (RF)
    /// Read/write (two way bindable), synced with the physical device
    /// Change notification is fired in OnDevicePropertyChanged
    /// </summary>
    public bool PowerlineEnabled
    {
        get => Device.PowerlineEnabled;
        set
        {
            // See above
            Debug.Assert(nameof(PowerlineEnabled) == nameof(Device.PowerlineEnabled));
            Device.PowerlineEnabled = value;
        }
    }

    /// <summary>
    /// LED brightness
    /// Read/write (two way bindable), synced with the physical device
    /// Changed notification is fired in OnDevicePropertyChanged
    /// </summary>
    public int LEDBrightness
    {
        get => Device.LEDBrightness;
        set
        {
            // See above
            Debug.Assert(nameof(LEDBrightness) == nameof(Device.LEDBrightness));
            Device.LEDBrightness = (byte)value;
        }
    }

    /// <summary>
    /// On Level
    /// Read/write (two way bindable), synced with the physical device
    /// Changed notification is fired in OnDevicePropertyChanged
    /// </summary>
    public int OnLevel
    {
        get => Device.OnLevel;
        set
        {
            // See above
            Debug.Assert(nameof(OnLevel) == nameof(Device.OnLevel));
            Device.OnLevel = (byte)value;
        }
    }

    /// <summary>
    /// Ramp Rate
    /// Read/write (two way bindable), synced with the physical device
    /// Changed notification is fired in OnDevicePropertyChanged
    /// </summary>
    public int RampRate
    {
        get => Device.RampRate;
        set
        {
            // See above
            Debug.Assert(nameof(RampRate) == nameof(Device.RampRate));
            Device.RampRate = (byte)value;
        }
    }

    // IDeviceObserver implementation to catch model change notifications
    void IDeviceObserver.DevicePropertyChanged(Device device, string? propertyName)
    {
        if (propertyName != null)
        {
            OnDevicePropertyChanged(propertyName);
        }
    }

    void IDeviceObserver.DevicePropertiesSyncStatusChanged(Device device)
    {
    }

    void IDeviceObserver.AllLinkDatabaseChanged(Device? device, AllLinkDatabase allLinkDatabase)
    {
        ControllerLinks.Clear();
        ResponderLinks.Clear();
        ScheduleRebuildLinksIfActive();
    }

    void IDeviceObserver.DeviceChannelsChanged(Device device)
    {
        // Force recreating the channels views on next get
        channelViewModels = null;
    }

    // Helper to let derived classes optionally override and change property name
    protected virtual void OnDevicePropertyChanged(string propertyName)
    {
        OnPropertyChanged(propertyName);

        switch (propertyName)
        {
            case nameof(Device.Status):
                OnPropertyChanged(nameof(DeviceConnectionStatus));
                OnPropertyChanged(nameof(IsStatusPending));
                OnPropertyChanged(nameof(IsConnected));
                OnPropertyChanged(nameof(IsDisconnected));
                OnPropertyChanged(nameof(IsGatewayError));
                break;

            case nameof(Device.CategoryId):
                OnPropertyChanged(nameof(ModelIconPath_72x72));
                break;

            case nameof(Device.SubCategory):
                OnPropertyChanged(nameof(ModelIconPath_72x72));
                break;
        }
    }

    /// <summary>
    /// Returns whether this device is connected so that we can send commands to it via the Hub.
    /// The callback is called when the connection status changes.
    /// </summary>
    public Device.ConnectionStatus DeviceConnectionStatus => Device.ScheduleRetrieveConnectionStatus();
    public bool IsStatusPending => DeviceConnectionStatus == Device.ConnectionStatus.Unknown;
    public bool IsConnected => DeviceConnectionStatus == Device.ConnectionStatus.Connected;
    public bool IsDisconnected => DeviceConnectionStatus == Device.ConnectionStatus.Disconnected;
    public bool IsGatewayError => DeviceConnectionStatus == Device.ConnectionStatus.GatewayError;

    /// <summary>
    /// Commands for load controller devices
    /// </summary>
    public ICommand LightOnCommand { get; }
    public ICommand LightOffCommand { get; }

    /// <summary>
    /// For a light controller device, turns light on at current requested level
    /// </summary>
    public void LightOn(object? level)
    {
        Device.ScheduleLightOn((level != null) ? (double)level : 1.0d);
    }

    /// <summary>
    /// For a light controller device, turns light off
    /// </summary>
    public void LightOff()
    {
        Device.ScheduleLightOff();
    }

    /// <summary>
    /// For a light controller device, returns current on-level
    /// </summary>
    public void GetOnLevel(Scheduler.JobCompletionCallback<double>? completionCallback = null)
    {
        Device.ScheduleGetLightOnLevel(completionCallback);
    }

    /// <summary>
    /// Requested light on level
    /// </summary>
    public double LightOnLevel
    {
        get => lightOnLevel;
        set
        {
            if (value != lightOnLevel)
            {
                lightOnLevel = value;
                OnPropertyChanged();
            }
        }
    }
    private double lightOnLevel;

    /// <summary>
    /// List of view models for the channels on this device
    /// The list is built and cached on first get
    /// </summary>
    public List<ChannelViewModel> ChannelViewModels
    {
        get
        {
            // Create list of channels if not done yet
            channelViewModels ??= new List<ChannelViewModel>();

            // Make sure it has the proper number of channels
            for (int i = channelViewModels.Count; i < Device.ChannelCount; i++)
            {
                // Channel ids are 1-based
                ChannelViewModel channelViewModel = new ChannelViewModel(this, Device.Channels[i].Id);
                RegisterChannel(channelViewModel);
                channelViewModels.Add(channelViewModel);
            }

            if (channelViewModels.Count > Device.ChannelCount)
            {
                for (int i = channelViewModels.Count - 1; i >= Device.ChannelCount; i--)
                {
                    channelViewModels.RemoveAt(i);
                }
            }

            return channelViewModels;
        }
    }
    private List<ChannelViewModel>? channelViewModels;

    // Return the view model of the current channel, null if none
    private ChannelViewModel? optCurrentChannelViewModel => GetChannelViewModel(CurrentChannelId);

    /// <summary>
    /// Current channel view model, only bind to it if HasCurrentChannelViewModel is true
    /// </summary>
    public ChannelViewModel CurrentChannelViewModel => optCurrentChannelViewModel!;

    /// <summary>
    /// Whether we have a current channel view model.
    /// Bind visibility or x:Load to it to prevent binding to CurrentChannelViewModel if null.
    /// </summary>
    public bool HasCurrentChannelViewModel => CurrentChannelViewModel != null;

    // Helper to react to change of currently active channel 
    protected void OnCurrentChannelChanged(ChannelViewModel oldActiveChannelViewModel)
    {
        OnPropertyChanged(nameof(HasCurrentChannelViewModel));
        OnPropertyChanged(nameof(CurrentChannelViewModel));

        oldActiveChannelViewModel?.ActiveStateChanged(false);
        optCurrentChannelViewModel?.ActiveStateChanged(true);
    }

    // Return a the view model of given channel on this device
    // Returns null if channel id is out of bounds
    protected virtual ChannelViewModel? GetChannelViewModel(int id)
    {
        // Channels are 1-based
        if (id == 0)
        {
            return null;
        }

        // When discovering devices from the physical network,
        // channels might not have been discovered on the device yet
        var index = id - Device.FirstChannelId;
        if (index >= ChannelViewModels.Count)
        {
            return null;
        }

        return ChannelViewModels[index];
    }

    /// <summary>
    /// Return the name of a given channel on this device.
    /// If no name has been provided, return a string containing the device id.
    /// </summary>
    /// <param name="id">Channel id (1 based)</param>
    /// <returns></returns>
    public string GetChannelName(int id)
    {
        var name = GetChannelViewModel(id)?.Name;
        if (name == null || name == string.Empty)
            name = id.ToString();
        return name;
    }

    /// <summary>
    /// Register a new channel
    /// Provide derived classes an opportunity to add property listeners on the channel
    /// </summary>
    /// <param name="channelViewModel"></param>
    private protected virtual void RegisterChannel(ChannelViewModel channelViewModel) { }

    /// <summary>
    /// Scenes using this device/channel
    /// </summary>
    public SceneListViewModel ScenesUsingThis
    {
        get
        {
            if (scenesWithThisDeviceViewModel == null)
            {
                var scenesWithThisDevice = GetScenesUsingThis();
                scenesWithThisDeviceViewModel = SceneListViewModel.Create(scenesWithThisDevice);
            }

            return scenesWithThisDeviceViewModel;
        }
    }
    public SceneListViewModel? scenesWithThisDeviceViewModel;

    // Helper to invalidate the list of scenes using this device/channel
    // and notify changes to the UI
    internal void RebuildScenesUsingThis()
    {
        scenesWithThisDeviceViewModel = null;
        OnPropertyChanged(nameof(ScenesUsingThis));
    }

    // Return the scenes using this device/channel that also pass FilterScenes.
    // FilterScenes filters to a given channel (button in KeypadlincViewModel).
    private Insteon.Model.Scenes GetScenesUsingThis()
    {
        // No need to listen to changes to this list of scenes, as it is not part of the model per se
        var scenesWithDevice = new Insteon.Model.Scenes(Device.House);
        foreach (Scene scene in Device.House.Scenes)
        {
            foreach (SceneMember member in scene.Members)
            {
                if (member.DeviceId == Id && FilterScenes(member))
                {
                    if (!scenesWithDevice.Contains(scene))
                        scenesWithDevice.Add(scene);
                }
            }
        }
        return scenesWithDevice;
    }

    /// <summary>
    /// Custom filtering for derived classes to subset the list of scenes this device belongs to
    /// </summary>
    /// <param name="member"></param>
    /// <returns>Returns true to include</returns>
    protected virtual bool FilterScenes(SceneMember member)
    {
        return true;
    }

    /// <summary>
    /// Bindable - List of rooms devices are in
    /// </summary>
    /// <returns></returns>
    public SortableObservableCollection<string> Rooms
    {
        get
        {
            if (rooms == null)
            {
                rooms = new SortableObservableCollection<string>(Device.House.Rooms);
                // TODO: localize
                rooms.Insert(0, "None");
            }
            return rooms;
        }
    }

    // TODO: multiple houses - static reduces the number of rebuilds of that list 
    // but will not work if we have multiple houses in future.
    private static SortableObservableCollection<string>? rooms;

    /// <summary>
    /// Notification handler that the rooms list may have changed
    /// </summary>
    public void RoomsChanged()
    {
        rooms = null;
        OnPropertyChanged(nameof(Rooms));
    }

    /// <summary>
    /// ViewModel level helper to schedule adding a device to the network by Id.
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="completionCallback"></param>
    /// <param name="delay"></param>
    /// <returns>handle to scheduled job, null if the device was already in the network</returns>
    public static object? ScheduleAddOrConnectDevice(InsteonID deviceId, House house,
        Scheduler.JobCompletionCallback<(DeviceViewModel? deviceViewModel, bool isNew)>? completionCallback = null,
        TimeSpan delay = new TimeSpan())
    {
        DeviceViewModel? deviceViewModel = DeviceViewModel.GetById(deviceId);
        // If no device exists with this id or the device is not connected to the network, add and connect it now
        if (deviceViewModel == null || deviceViewModel.DeviceConnectionStatus != Device.ConnectionStatus.Connected)
        {
            return house.Devices.ScheduleAddOrConnectDevice(deviceId, (d) =>
            {
                DeviceViewModel? deviceViewModel = null;
                if (d.device != null)
                {
                    deviceViewModel = DeviceViewModel.GetById(d.device.Id);
                }
                completionCallback?.Invoke((deviceViewModel, d.isNew));
            },
            delay);
        }

        return null;
    }

    /// <summary>
    /// ViewModel level helper to schedule adding a new device manually to the network, 
    /// The user has to press the SET button for 3 sec on the actual device.
    /// If successful, update properties and links from the new device and links the hub
    /// </summary>
    /// <param name="completionCallback">called on completion if not null</param>
    /// <param name="delay">delay before running job</param>
    /// <returns>handle to scheduled job</returns>
    public static object ScheduleAddDeviceManually(House house,
        Scheduler.JobCompletionCallback<(DeviceViewModel? deviceViewModel, bool isNew)>? completionCallback = null, TimeSpan delay = new TimeSpan())
    {
        return house.Devices.ScheduleAddDeviceManually((d) =>
        {
            DeviceViewModel? deviceViewModel = null;
            if (d.device != null)
            {
                deviceViewModel = DeviceViewModel.GetOrCreateById(d.device.House, d.device.Id);
            }
            completionCallback?.Invoke((deviceViewModel, d.isNew));
        },
            delay);
    }

    /// <summary>
    /// ViewModel level helper to cancel adding a device manually
    /// </summary>
    /// <param name="job">Add device job to cancel</param>
    /// <returns>handle to scheduled job</returns>
    public static object ScheduleCancelAddDevice(object job, House house)
    {
        return house.Devices.ScheduleCancelAddDevice(job);
    }

    /// <summary>
    /// ViewModel level helper to schedule removing this device from the model
    /// </summary>
    /// <param name="completionCallback"></param>
    /// <param name="delay"></param>
    /// <returns>handle to scheduled job</returns>
    public Object? ScheduleRemoveDevice(Scheduler.JobCompletionCallback<bool>? completionCallback = null, TimeSpan delay = new TimeSpan())
    {
        return Device.House.Devices.ScheduleRemoveDevice(Device.Id, completionCallback, delay);
    }

    /// <summary>
    /// Handler for the "Force Sync Device" menu
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void ForceSyncDevice_Click(object sender, RoutedEventArgs e)
    {
        Device.ScheduleSync(forceRead: true);
    }

    /// <summary>
    /// Handler for the "Force Check Device Connection Status" menu
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void ForceCheckDeviceConnectionStatus_Click(object sender, RoutedEventArgs e)
    {
        Device.ScheduleRetrieveConnectionStatus(force: true);
    }

    /// <summary>
    /// Replace this device by another
    /// </summary>
    public void ReplaceDevice(InsteonID replacementDeviceId)
    {
        Device.TryReplaceDeviceBy(replacementDeviceId);
        ScheduleRemoveDevice();
    }

    /// <summary>
    /// Copy this device to another
    /// </summary>
    /// <param name="copyDeviceId"></param>
    public void CopyDevice(InsteonID copyDeviceId)
    {
        Device.TryCopyDeviceTo(copyDeviceId);
    }

    /// <summary>
    /// Handler for the "Remove Stale Links" menu
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void RemoveDuplicateLinks_Click(Object sender, RoutedEventArgs e)
    {
        Device.RemoveDuplicateLinks();
    }

    /// <summary>
    /// Handler for the "Remove Stale Links" menu
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void RemoveStaleLinks_Click(Object sender, RoutedEventArgs e)
    {
        Device.RemoveStaleLinks();
    }

    /// <summary>
    /// Handler for "Connect Device" button
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void ConnectDevice_Click(object sender, RoutedEventArgs e)
    {
        Device.House.Devices.ScheduleAddOrConnectDevice(Id,
            (d) =>
            {
                if (d.device != null)
                {
                    Debug.Assert(d.device == Device);
                    ActiveStateChanged();
                }
            });
    }

    /// <summary>
    /// Handler for the "Ensure Link to hub is First" menu
    /// Ensure that the link to the hub is first in the device database
    /// to avoid pre-nak errors
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void EnsureLinkToHubIsFirst_Click(object sender, RoutedEventArgs e)
    {
        Device.EnsureLinkToHubIsFirst();
    }

    /// <summary>
    /// Create a new responder link as input to the "Add New Controller" dialog,
    /// prepopulate Data3 (the responding channel) with the id of the channel currently in view
    /// </summary>
    /// <returns>LinkViewModel for the new link</returns>
    public override LinkViewModel CreateNewResponderLink()
    {
        LinkViewModel newLink = base.CreateNewResponderLink();
        newLink.DeviceChannelId = CurrentChannelId;
        return newLink;
    }

    /// <summary>
    /// Create a new controller link as input to the "Add New Responder" dialog,
    /// prepopulate Group (the controlling channel) with the id of the channel currently in view
    /// </summary>
    /// <returns>LinkViewModel for the new link</returns>
    public override LinkViewModel CreateNewControllerLink()
    {
        LinkViewModel newLink = base.CreateNewControllerLink();
        newLink.Group = CurrentChannelId;
        return newLink;
    }
}
