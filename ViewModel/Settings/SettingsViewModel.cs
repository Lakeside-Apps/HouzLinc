/* Copyright 2022 ChristianGa Fortini

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
using Insteon.Base;
using System.Diagnostics;
using System.ComponentModel;
using HouzLinc.Utils;
using System.Text.RegularExpressions;
using ViewModel.Base;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;

namespace ViewModel.Settings;

/// <summary>
/// Feature flags configured in appsettings.json
/// </summary>
public sealed record FeatureFlags
{
    public bool EnableHouseSaveButton { get; init; }
    public bool EnableHouseSyncButton { get; init; }
}

/// <summary>
/// Features configuration service registered with the host.
/// We grab the option values from the parameter passed to the constructor.
/// </summary>
public sealed class FeatureFlagsConfiguration
{
    internal readonly FeatureFlags FeatureFlagsOptions;

    public FeatureFlagsConfiguration(IOptions<FeatureFlags> options)
    {
        this.FeatureFlagsOptions = options.Value;
    }
}

/// <summary>
/// This class is the view model for the Settings page
/// and for anywhere in the UI where house settings are shown
/// </summary>
public class SettingsViewModel : PageViewModel
{
    /// <summary>
    /// Must be called before using this class
    /// </summary>
    public SettingsViewModel()
    {
        if (Gateway.CredentialLocker == null)
        {
            Gateway.CredentialLocker = new GatewayCredentialLocker();
        }

        Holder.OnHouseSaveStatusChanged += () =>
        {
            OnPropertyChanged(nameof(DoesHouseConfigNeedSave));

            // If the "Save" button is not shown, schedule a save.
            if (!ShowHouseSaveButton && DoesHouseConfigNeedSave)
            {
                ScheduleSaveHouse();
            }
        };

        Holder.OnHouseSyncStatusChanged += () =>
        {
            OnPropertyChanged(nameof(DoesHouseConfigNeedSync));

            // if the "Sync" button is not shown, start a sync pass.
            if (!ShowHouseSyncButton && DoesHouseConfigNeedSync)
            {
                SyncHouse();
            }
        };

        Holder.HasGatewayTrafficChanged += () =>
        {
            OnPropertyChanged(nameof(HasGatewayTraffic));
            OnPropertyChanged(nameof(ShowHouseSyncButton));
        };
    }

    public static SettingsViewModel Instance => instance ??= new SettingsViewModel();
    private static SettingsViewModel? instance;

    // Initialize the bindable state when the page is loaded
    public override void ViewLoaded()
    {
        HouseFilePath = HouseStoragePath;
        if (!string.IsNullOrEmpty(HouseFilePath))
            AutoLoadHouse = true;
    }

    public override void ViewUnloaded()
    {
    }

    public override void ViewNavigatedTo()
    {
    }

    public override void ViewNavigatedFrom()
    {
        NetworkScanner.CancelSubnetScan();
    }

    public InsteonID GatewayInsteonID => Holder.House.Gateway.DeviceId;

    /// <summary>
    /// Called from the UI to pass the values read from the configuration file (appsettings.json)
    /// </summary>
    /// <param name="featureFlagsConfiguration"></param>
    public void SetFeatureFlagsConfiguration(FeatureFlagsConfiguration featureFlagsConfiguration)
    {
        featureFlags = featureFlagsConfiguration.FeatureFlagsOptions;
    }

    private FeatureFlags featureFlags = null!;

    /// <summary>
    /// Whether the House Save button should be shown
    /// Bindable one-time from the UI
    /// </summary>
    public bool ShowHouseSaveButton => featureFlags.EnableHouseSaveButton;

    /// <summary>
    /// Whether the House Sync button should be shown
    /// Bindable one-time from the UI
    /// </summary>
    public bool ShowHouseSyncButton => featureFlags.EnableHouseSyncButton && !Holder.HasGatewayTraffic;

    /// <summary>
    /// Whether house config needs saving
    /// </summary>
    public bool DoesHouseConfigNeedSave => Holder.DoesHouseNeedSave;

    /// <summary>
    /// Whether house config needs syncing
    /// </summary>
    public bool DoesHouseConfigNeedSync => Holder.DoesHouseNeedSync;

    /// <summary>
    /// Whether house configuration is currently syncing
    /// </summary>
    public bool HasGatewayTraffic => Holder.HasGatewayTraffic;

    /// <summary>
    /// Load the house config from storage or create a new one
    /// This method sets following statics:
    /// - HouseStorageMode
    /// - HouseStoragePath
    /// </summary>
    /// <returns>Whether the model is ready to use</returns>
    public async Task<bool> EnsureHouse()
    {
       if (Holder.House != null)
            return true;

        StorageProvider? storageProvider = null;
        object? storage = null;
        bool isNew = false;

        // If we are auto-loading, grab the storage settings (mode and file) from the app settings
        if (AutoLoadHouse)
        {
            (storageProvider, storage) = await GetStorageFromSettings();
        }

        while (true)
        {
            bool error = false;

            // If we are auto-loading or have a storage mode, attempt to load the house config
            if (storageProvider != null)
            {
                var house = await storageProvider.LoadFromStorage(storage, isNew);
                if (house != null)
                {
                    Holder.House = house;
                    HouseStoragePath = storageProvider.GetStoragePath(storage);
                    AutoLoadHouse = true;
                    SetStorageToSettings(storageProvider, storage);
                }
                else
                {
                    storage = null;
                    error = true;
                }
            }

            // If we have a storage option and successfully loaded the house config using it, we are done
            if (storageProvider != null && !error)
            {
                break;
            }

            // Otherwise, show the dialog to choose a storage option
            Debug.Assert(ShowOpenHouseDialogHandler != null);
            var action = await ShowOpenHouseDialogHandler(showPreviousError: error);
            switch (action)
            {
                // User cancelled, return false will exit app
                case OpenHouseDialogActions.Exit:
                    return false;

                // Let user choose a local file and load the house config from it
                case OpenHouseDialogActions.File:
                    storageProvider = new FileStorageProvider();
                    storage = await storageProvider.PickExistingStorage() as StorageFile;
                    break;

                // Create new house config, new file storage
                case OpenHouseDialogActions.New:
                    isNew = true;
                    storageProvider = new FileStorageProvider();
                    storage = await storageProvider.PickNewStorage() as StorageFile;
                    break;

                // Create/open file in the Apps folder of OneDrive
                case OpenHouseDialogActions.OneDrive:
                    storageProvider = new OneDriveStorageProvider();
                    await storageProvider.PickExistingStorage();
                    break;
            }
        }

        houseStorageProvider = storageProvider;
        houseStorage = storage;
        InitializeHubProperties();
        Holder.StartHouse();
        return true;
    }

    /// <summary>
    /// Schedule saving the house configuration to file
    /// with a delay to reduce the number of saves
    /// </summary>
    public void ScheduleSaveHouse()
    {
        if (saveHouseJob == null)
        {
            saveHouseJob = UIScheduler.Instance.AddAsyncJob("Saving house configuration",
                handlerAsync: async () => 
                { 
                    saveHouseJob = null; 
                    return await SaveHouse(); 
                },
                delay: TimeSpan.FromSeconds(5)
            );
        }
    }
    private object? saveHouseJob;

    /// <summary>
    /// Save house config to file
    /// This also keeps the previous version of the file around
    /// TODO: consider saving n previous versions of the file.
    /// </summary>
    /// <returns></returns>
    public async Task<bool> SaveHouse()
    {
        bool success = false;

        if (houseStorageProvider is FileStorageProvider && HouseStoragePath != null)
        {
            // If saving to a local file, create a backup in the ApplicationData LocalFolder
            StorageFile? file = await StorageFile.GetFileFromPathAsync(HouseStoragePath);
            if (file != null)
            {
                StorageFile backupFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(file.Name, CreationCollisionOption.OpenIfExists);
                await file.CopyAndReplaceAsync(backupFile);
                success = await houseStorageProvider.SaveToStorage(file, Holder.House);
            }
        }
        else if (houseStorageProvider != null)
        {
            success = await houseStorageProvider.SaveToStorage(houseStorage, Holder.House);
        }

        if (success)
        {
            Holder.DoesHouseNeedSave = false;
        }

        return success;
    }

    /// <summary>
    /// Sync the house config with the physical devices
    /// </summary>
    public void SyncHouse()
    {
        Holder.SyncHouse();
    }

    private const string houseStorageModeSetting = "HouseStorageMode";
    private const string houseStoragePathSetting = "HouseStoragePath";

    private async Task<(StorageProvider storageProvider, object? storage)> GetStorageFromSettings()
    {
        string? path = null;
        object? file = null;
        StorageProvider? storageProvider = null;

        // Determine where we are storing the house config file
        string? str = SettingsStore.ReadValue(houseStorageModeSetting) as string;
        switch (str)
        {
            case FileStorageProvider.Name:
                {
                    storageProvider = new FileStorageProvider();

                    // If we have already loaded the house config from a file, use that file
                    if (HouseStoragePath != null && HouseStoragePath != "")
                    {
                        path = HouseStoragePath;
                    }

                    // If we have a path in our settings, try to get that file
                    if (path == null && AutoLoadHouse)
                    {
                        path = SettingsStore.ReadValue(houseStoragePathSetting) as string;
                    }

                    if (path != null)
                    {
                        try
                        {
                            // Dispatching this call to a different thread , otherwise we sometimes get an unhandled exception
                            // apparently caused by some reentrency at the message pump level during the RPC call.
                            //  [0x0]   KERNELBASE!RaiseFailFastException + 0x152   0x27c0978cf0   0x7ffe3d3d3a69
                            //  [0x1]   combase!RoFailFastWithErrorContextInternal2 + 0x4d9   0x27c09792d0   0x7ffceb249125
                            //  [0x2]   Microsoft_UI_Xaml!FailFastWithStowedExceptions + 0x7d   0x27c0979540   0x7ffceb4bb24c
                            //  [0x3]   Microsoft_UI_Xaml!CXcpDispatcher::OnReentrancyProtectedWindowMessage + 0x88   0x27c09795b0   0x7ffceb4bb1bf
                            //  [0x4]   Microsoft_UI_Xaml!CXcpDispatcher::ProcessMessage + 0x7b   0x27c0979620   0x7ffceb4ba6e7
                            //  [0x5]   Microsoft_UI_Xaml!CDeferredInvoke::DispatchQueuedMessage + 0x83   0x27c0979650   0x7ffceb4be77d
                            //  ...
                            //  [0x2c]   RPCRT4!NdrpClientCall3 + 0x3de   0x27c097a960   0x7ffe3d25a8bc
                            //  [0x2d]   combase!ObjectStublessClient + 0x14c   0x27c097ac80   0x7ffe3d2e4b82
                            //  [0x2e]   combase!ObjectStubless + 0x42   0x27c097b010   0x7ffe3d228705
                            //  [0x2f]   combase!CRpcResolver::DelegateActivationToSCM + 0x2f9   0x27c097b060   0x7ffe3d1d9140
                            //  [0x30]   combase!CRpcResolver::GetClassObject + 0x14(Inline Function)(Inline Function)
                            //  ...
                            //  [0x39]   Microsoft_Windows_SDK_NET!ABI.Windows.Storage.IStorageFileStaticsMethods.GetFileFromPathAsync + 0x135   0x27c097c720   0x7ffd5874a93c
                            file = await Task.Run(async () => await StorageFile.GetFileFromPathAsync(path));
                        }
                        catch (Exception ex)
                        {
                            // Handle any other exceptions
                            Logger.Log.Debug($"An error occurred opening {path}: {ex.Message}");
                        }
                    }
                    break;
                }

            case OneDriveStorageProvider.Name:
                {
                    storageProvider = new OneDriveStorageProvider();
                    break;
                }

            default:
                throw new InvalidEnumArgumentException();
        }

        return (storageProvider, file);
    }

    private void SetStorageToSettings(StorageProvider storageProvider, object? storage = null)
    {
        if (storageProvider is FileStorageProvider)
        {
#if WINDOW
            // If the user picked a file, store its path in app settings and set permission to reopen it later
            StorageApplicationPermissions.FutureAccessList.Add(file);
#endif
            Debug.Assert(storage != null);
            SettingsStore.WriteValue(houseStoragePathSetting, storageProvider.GetStoragePath(storage));
        }
        SettingsStore.WriteValue(houseStorageModeSetting, storageProvider.ProviderName);
    }

    // Storage provider to save the house config on (e.g., file, OneDrive)
    private StorageProvider? houseStorageProvider;

    // Storage on that provider (e.g., a specific file)
    // Exact type depends on the StorageProvider
    // Some storage providers may not need this as they only store the config in one place for now (e.g., OneDrive)
    private object? houseStorage;

    /// <summary>
    /// Path of a local house config file when one is used and config has been loaded from that file.
    /// Empty string if not.
    /// </summary>
    public string? HouseStoragePath { get; private set; }

    /// <summary>
    /// Name of the house config
    /// </summary>
    public string HouseName => Holder.House.Name;

    /// <summary>
    /// Path of the House Configuration file, as should be displayed in the UI
    /// </summary>
    public string? HouseFilePath
    {
        get => houseFilePath;
        set
        {
            if (value != houseFilePath)
            {
                houseFilePath = value;
                OnPropertyChanged();
            }
        }
    }
    private string? houseFilePath;

    /// <summary>
    /// Whether to automatically load the house file on startup 
    /// if a path for it is specified in the Settings
    /// </summary>
    public bool AutoLoadHouse
    {
        get 
        {
            autoLoadHouse ??= SettingsStore.ReadLastUsedValue("AutoLoadModel") as bool?;
            return (autoLoadHouse == true);
        }
        
        set
        {
            autoLoadHouse = value;
            SettingsStore.WriteLastUsedValue("AutoLoadModel", value);
        }
    }
    internal bool? autoLoadHouse;

    /// <summary>
    /// Action to be taken after exiting OpenHouseDialog
    /// </summary>
    public enum OpenHouseDialogActions
    {
        Exit,           // User cancelled, exit the app
        File,           // Load config from local file
        New,            // New config, needs to be saved to file
        OneDrive,       // Load to/sync with OneDrive
    }

    /// <summary>
    /// Possible storage modes for the house configuration
    /// </summary>
    internal enum HouseStorageModes
    {
        None,           // None chosen yet
        File,           // Local file
        OneDrive        // OneDrive service
    }

    /// <summary>
    /// Event to App layer to show the file open picker
    /// </summary>
    /// <returns></returns>
    public delegate Task<OpenHouseDialogActions> ShowOpenHouseDialogDelegate(bool showPreviousError);
    public static event ShowOpenHouseDialogDelegate ShowOpenHouseDialogHandler = null!;

    // Hub Discovery States
    // Input: fields Mac Address, port, username, password, and IP address of the hub, Find Hub button
    // States: NotFound, Found, Searching, Failed
    // NotFound
    // - Find Hub button is shown if any data is in the fields
    // - User edits any of the fields -> no change (i.e., NotFound)
    // - User presses Find Hub
    //      -> we have data in at least port, username and password -> Searching
    //      -> we don't -> NotFound
    // HubFound
    // - Shows: "Hub Found!" is shown along with Hub Inteon ID
    // - Hub is reported found to all devices and model synchronization can proceed
    // - User edits any of the fields -> NotFound
    // Searching
    // - Shows: searching mention and progress animation
    // - User edits any of the field -> NotFound (search cancelled)
    // - User entered correct information -> Found (after a delay)
    // - Search for hub fails -> Failed
    // Failed
    // - Shows: "Hub not Found!"
    // - User edits any of the fields -> NotFound

    public enum HubDiscoveryStates
    {
        NotFound,
        Found,
        Searching,
        Failed
    }

    public HubDiscoveryStates HubDiscoveryState
    {
        get => hubDiscoveryState;
        set
        {
            if (value != hubDiscoveryState)
            {
                if (hubDiscoveryState == HubDiscoveryStates.Searching)
                    NetworkScanner.CancelSubnetScan();

                hubDiscoveryState = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsHubFound));
                OnPropertyChanged(nameof(IsHubNotFound));
                OnPropertyChanged(nameof(IsSearchingForHub));
                OnPropertyChanged(nameof(HasFailedToFindHub));
            }
        }
    }
    HubDiscoveryStates hubDiscoveryState = HubDiscoveryStates.NotFound;

    // UI bindable expressions of the Discovery State
    public bool IsHubFound => HubDiscoveryState == SettingsViewModel.HubDiscoveryStates.Found;
    public bool IsHubNotFound => HubDiscoveryState == SettingsViewModel.HubDiscoveryStates.NotFound || 
                                  HubDiscoveryState == SettingsViewModel.HubDiscoveryStates.Failed;
    public bool IsSearchingForHub => HubDiscoveryState == SettingsViewModel.HubDiscoveryStates.Searching;
    public bool HasFailedToFindHub => HubDiscoveryState == SettingsViewModel.HubDiscoveryStates.Failed;

    /// <summary>
    /// Hub properties
    /// </summary>
    public string HubMacAddress
    {
        get => hubMacAddress;
        set
        {
            if (value != hubMacAddress)
            {
                hubMacAddress = value;
                OnPropertyChanged();
                HubDiscoveryState = HubDiscoveryStates.NotFound;
            }
        }
    }
    private string hubMacAddress = string.Empty;

    public string HubIPHostName
    {
        get => hubIPHostName;
        set
        {
            if (value != hubIPHostName)
            {
                hubIPHostName = value;
                OnPropertyChanged();
                HubDiscoveryState = HubDiscoveryStates.NotFound;
            }
        }
    }
    private string hubIPHostName = string.Empty;

    public string HubIPAddress
    {
        get => hubIPAddress;
        set
        {
            if (value != hubIPAddress)
            {
                hubIPAddress = value;
                OnPropertyChanged();
                HubDiscoveryState = HubDiscoveryStates.NotFound;
            }
        }
    }
    private string hubIPAddress = string.Empty;

    public string HubIPPort
    {
        get => hubIPPort;
        set
        {
            if (value != hubIPPort)
            {
                hubIPPort = value;
                OnPropertyChanged();
                HubDiscoveryState = HubDiscoveryStates.NotFound;
            }
        }
    }
    private string hubIPPort = string.Empty;

    public string HubUsername
    {
        get => hubUsername;
        set
        {
            if (value != hubUsername)
            {
                hubUsername = value;
                OnPropertyChanged();
                HubDiscoveryState = HubDiscoveryStates.NotFound;
            }
        }
    }
    private string hubUsername = string.Empty;

    public string HubPassword
    {
        get => hubPassword;
        set
        {
            if (value != hubPassword)
            {
                hubPassword = value;
                OnPropertyChanged();
                HubDiscoveryState = HubDiscoveryStates.NotFound;
            }
        }
    }
    private string hubPassword = string.Empty;

    public InsteonID HubInsteonID
    {
        get => hubInsteonID;
        set
        {
            if (value != hubInsteonID)
            {
                hubInsteonID = value;
                OnPropertyChanged();
            }
        }
    }
    private InsteonID hubInsteonID = InsteonID.Null;

    // Helper to initialize the properties declared above
    private void InitializeHubProperties()
    {
        HubMacAddress = Holder.House.Gateway.MacAddress;
        HubIPHostName = Holder.House.Gateway.HostName;
        HubIPAddress = Holder.House.Gateway.IPAddress;
        HubIPPort = Holder.House.Gateway.Port.ToString();
        HubUsername = Holder.House.Gateway.Username ?? string.Empty;
        HubPassword = Holder.House.Gateway.Password ?? string.Empty;
    }

    // Helper to find the hub on the local network, based on infomration in this Settings page
    public async Task<bool> FindHub()
    {
        HubDiscoveryState = HubDiscoveryStates.Searching;

        // If we don't have at least port, username and password
        // we can't find the hub
        if (string.IsNullOrWhiteSpace(HubIPPort) ||
            string.IsNullOrWhiteSpace(HubUsername) ||
            string.IsNullOrWhiteSpace(HubPassword))
        {
            HubDiscoveryState = HubDiscoveryStates.Failed;
            return false;
        }

        // If we have an IP address, try it
        if (IsValidIPAddress(HubIPAddress))
        {
            if (await TryPushNewGatewayAsync())
            {
                HubDiscoveryState = HubDiscoveryStates.Found;
                HubInsteonID = GatewayInsteonID;
                return true;
            }
        }

        // If we have a valid MAC address, try to use it to derive the IP address
        // Note: can't do this on Android because the ARP table is not accessible
        // See: RunnArpCommand() below
        if (IsValidMacAddress(HubMacAddress))
        {
            HubIPAddress = string.Empty;
            var ip = GetIpAddressByMac(HubMacAddress);
            if (IsValidIPAddress(ip))
            {
                HubIPAddress = ip;
                if (await TryPushNewGatewayAsync())
                {
                    HubDiscoveryState = HubDiscoveryStates.Found;
                    HubInsteonID = GatewayInsteonID;
                    return true;
                }
            }
        }

        // Scan for a candidate hub on the local networks this device is connected to.
        // TODO: consider moving this to the Insteon project as ScheduleScanSubnetsForHostWithOpenPort.
        Logger.Log.Running("Scanning Local Subnet");
        if (await NetworkScanner.ScanSubnetsForHostWithOpenPort(int.Parse(hubIPPort), async (ipAddress) =>
        {
            HubIPAddress = ipAddress.ToString();
            if (await TryPushNewGatewayAsync())
                return true;
            Logger.Log.Running("Scanning Local Subnet");
            return false;
        }))
        {
            HubDiscoveryState = HubDiscoveryStates.Found;
            HubInsteonID = GatewayInsteonID;
            return true;
        }
        else
        {
            Logger.Log.Failed("Scanning Local Subnet - Failed");
        }

        // We've failed to locate the hub
        HubDiscoveryState = HubDiscoveryStates.Failed;
        return false;
    }

    // Awaitable version of SchedulePushNewGatewayAsync()
    private Task<bool> TryPushNewGatewayAsync()
    {
        var tcs = new TaskCompletionSource<bool>();
        SettingsViewModel.SchedulePushNewGateway(HubMacAddress, HubIPHostName, HubIPAddress, HubIPPort, HubUsername, HubPassword,
            (hubFound) =>
            {
                tcs.SetResult(hubFound);
            }, maxRunCount: 1);
        return tcs.Task;
    }

    // Clear the hub settings
    public void ClearAllSettings()
    {
        SettingsStore.Clear();
        HubInsteonID = InsteonID.Null;
        HubMacAddress = string.Empty;
        HubIPHostName = string.Empty;
        HubIPAddress = string.Empty;
        hubIPPort = string.Empty;
        AutoLoadHouse = false;
    }

    // Clear only the hub credentials
    public async void ClearCredentials()
    {
        SettingsViewModel.ClearGatewayCredentials();
        HubUsername = string.Empty;
        HubPassword = string.Empty;
        await FindHub();
    }

    // Helper to validate an IP address
    public static bool IsValidIPAddress([NotNullWhen(true)] string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return false;
        }

        // Regular expression to match IP address format "xxx.xxx.xxx.xxx"
        string ipAddressPattern = @"^(\d{1,3}\.){3}\d{1,3}$";
        Regex regex = new Regex(ipAddressPattern);

        if (!regex.IsMatch(ipAddress))
        {
            return false;
        }

        // Further validate that each segment is between 0 and 255
        string[] segments = ipAddress.Split('.');
        foreach (string segment in segments)
        {
            if (int.TryParse(segment, out int value))
            {
                if (value < 0 || value > 255)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    // Helper to validate a MAC address
    public static bool IsValidMacAddress(string macAddress)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
        {
            return false;
        }

        // Regular expression to match MAC address format "xx:xx:xx:xx:xx:xx"
        string macAddressPattern = @"^([0-9A-Fa-f]{2}:){5}[0-9A-Fa-f]{2}$";
        Regex regex = new Regex(macAddressPattern);

        return regex.IsMatch(macAddress);
    }

    // Helper to get the IP address of a host on the local network from the MAC address using the ARP table
    // This does not work on Android because the ARP table is not accessible on Android 10+
    static string? GetIpAddressByMac(string macAddress)
    {
        string? arpOutput = RunArpCommand();

        if (arpOutput != null)
        {
            string pattern = $@"(?<IPAddress>(\d{{1,3}}\.){{3}}\d{{1,3}})\s+{macAddress.Replace(":", "-")}";

            var matches = Regex.Matches(arpOutput, pattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    return match.Groups["IPAddress"].Value;
                }
            }
        }

        return null;
    }

    // Helper to Run the ARP command to get the ARP table
    // Does not work on Android because the ARP table is not accessible on Android 10+.
    public static string? RunArpCommand()
    {
#if ANDROID
        return null;

        //try
        //{
        //    // Get the runtime instance
        //    var runtime = Java.Lang.Runtime.GetRuntime();
        //    if (runtime == null)
        //        throw new NullReferenceException("Java.Lang.Runtime.GetRuntime() returned null");

        //    // Execute the arp command
        //    var process = runtime.Exec("arp -a");
        //    if (process == null)
        //        throw new NullReferenceException("Runtime.Exec() returned null");

        //    // Read the output
        //    var output = new StringBuilder();
        //    using (var reader = new System.IO.StreamReader(process.InputStream!))
        //    {
        //        string? line;
        //        while ((line = reader.ReadLine()) != null)
        //        {
        //            output.AppendLine(line);
        //        }
        //    }
        //    return output.ToString();
        //}
        //catch (Exception ex)
        //{
        //    Logger.Log.Error($"Error executing arp command: {ex.Message}");
        //    return null;
        //}
#elif IOS || MACCATALYST
        return null;
#else
        ProcessStartInfo psi = new ProcessStartInfo("arp", "-a")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process? process = Process.Start(psi))
        {
            return process?.StandardOutput.ReadToEnd() ?? null;
        }
#endif
}

    /// <summary>
    /// Clear the credentials of the current gateway
    /// </summary>
    public static void ClearGatewayCredentials()
    {
        Holder.House.Gateway.ClearCredentials();
    }

    /// <summary>
    /// Install a new active gateway, pushing the current one down in the gateway list
    /// This pushes the new gateway at the top of the list of gateways and creates a device for it if there is not one already
    /// </summary>
    public static void SchedulePushNewGateway(string hubMacAddress, string hubIPHostName, string hubIPAddress, string hubIPPort, string hubUsername, string hubPassword, 
        Common.Scheduler.JobCompletionCallback<bool> completionCallback, int maxRunCount)
    {
        // Push the new gateway/hub
        Holder.House.SchedulePushNewGateway(hubMacAddress, hubIPHostName, hubIPAddress, hubIPPort, hubUsername, hubPassword, completionCallback, maxRunCount: maxRunCount);
    }
}
