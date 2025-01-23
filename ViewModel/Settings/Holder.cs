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

using Windows.Storage;
using Common;
using Insteon.Model;
using Insteon.Serialization.Houselinc;
using System.Diagnostics;

namespace ViewModel.Settings;

// Static class used to hold the house configuration.
// Provides a static reference to the house config that can be used throughout the code.
// Also provides methods to load and save the config from/to file
public static class Holder
{
    static Holder()
    {
    }

    /// <summary>
    /// House configuration (model)
    /// Set by EnsureHouse()
    /// </summary>
    public static House House { get; set; } = null!;

    /// <summary>
    /// Ensure that we have a house configuration (model)
    /// </summary>
    public static void CreateNewHouse()
    {
        House = new House().EnsureNew();
    }

    /// <summary>
    /// Does the house configuration (model) already exist on OneDrive?
    /// </summary>
    /// <returns>true if exists</returns>
    /// <summary>
    /// Initialize this house config (model)
    /// </summary>
    public static void StartHouse()
    {
        Debug.Assert(House != null);

        // Listen for changes in the house model
        // Indicating that it needs to be saved
        House.ModelObserver.OnModelChanged += () => { DoesHouseNeedSave = true; };

        // Listen for changes in the synchronization of the model with the physical network of devices
        // Indicating that we can kick a sync pass
        House.ModelObserver.OnModelNeedsSync += () => { DoesHouseNeedSync = true; };

        // Listen for whether we are pushing network traffic through the gateway
        House.OnGatewayTraffic += (bool hasTraffic) => { HasGatewayTraffic = hasTraffic; };

        // Start recording changes to the model
        House.StartRecordingChanges();

        // Start background sync between house model and physical devices
        House.StartBackgroundDeviceSync();
    }

    /// <summary>
    /// Does the house model need to be saved?
    /// </summary>
    public static bool DoesHouseNeedSave
    {
        get => doesHouseNeedSave;
        internal set
        {
            if (value != doesHouseNeedSave)
            {
                doesHouseNeedSave = value;
                OnHouseSaveStatusChanged?.Invoke();
            }
        }
    }
    private static bool doesHouseNeedSave = false;

    /// <summary>
    /// Notify house model save status change to listneners
    /// </summary>
    public static event Action? OnHouseSaveStatusChanged;

    /// <summary>
    /// Does the house model need to be synced with the physical devices?
    /// </summary>
    public static bool DoesHouseNeedSync
    {
        get => doesHouseNeedSync;
        private set
        {
            if (value != doesHouseNeedSync)
            {
                doesHouseNeedSync = value;
                OnHouseSyncStatusChanged?.Invoke();
            }
        }
    }
    private static bool doesHouseNeedSync = false;

    /// <summary>
    /// Notify house model sync state change to listneners
    /// </summary>
    public static event Action? OnHouseSyncStatusChanged;

    /// <summary>
    /// Does the house model need to be synced with the physical devices?
    /// </summary>
    public static bool HasGatewayTraffic
    {
        get => hasGatewayTraffic;
        private set
        {
            if (value != hasGatewayTraffic)
            {
                hasGatewayTraffic = value;
                HasGatewayTrafficChanged?.Invoke();
            }
        }
    }
    private static bool hasGatewayTraffic = false;

    /// <summary>
    /// Notify house model sync state change to listneners
    /// </summary>
    public static event Action ? HasGatewayTrafficChanged;

    /// <summary>
    /// Synchronize the house model with the physical devices
    /// </summary>
    public static void SyncHouse()
    {
        House.Devices.SyncDevicesNow(success =>
        {
            if (!success)
                DoesHouseNeedSync = true;
        });
        DoesHouseNeedSync = false;
    }

    /// <summary>
    /// User confirms taking requested action (see House.cs for details)
    /// </summary>
    public static void ConfirmUserAction()
    {
        House.ConfirmUserAction();
        SyncHouse();
    }

    /// <summary>
    /// User declines taking requested action (see House.cs for details)
    /// </summary>
    public static void DeclineUserAction()
    {
        House.DeclineUserAction();
        SyncHouse();
    }
}
