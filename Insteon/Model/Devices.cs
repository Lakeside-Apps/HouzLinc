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
using Insteon.Base;
using Insteon.Commands;
using Insteon.Mock;
using Insteon.Synchronization;
using Microsoft.UI.Dispatching;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleToAttribute("UnitTestApp")]

namespace Insteon.Model;

/// <summary>
/// Collection of Devices in sequence and searchable by InsteonID
/// </summary>
public sealed class Devices : OrderedKeyedList<Device>
{
    internal Devices(House house)
    {
        House = house;
    }

    // Copy state from another Devices object.
    // Generates observer notifications of collection or property changes.
    internal void CopyFrom(Devices fromDevices)
    {
        var fromDevices2 = new Devices(House);
        foreach(var device in fromDevices)
        {
            fromDevices2.Add(device);
        }

        // Copy devices that exist in both lists, from the "from" to this list.
        // And prepare to remove devices from this list that do not exist in the "from" list
        var devicesToRemove = new List<Device>();
        foreach(var device in this)
        {
            if (fromDevices2.TryGetEntry(device.GetHashCode(), out var fromDevice))
            {
                device.CopyFrom(fromDevice);
                fromDevices2.Remove(fromDevice);
            }
            else
            {
                devicesToRemove.Add(device);
            }
        }

        // Remove devices from this list that do not exist in the "from" list
        foreach (var device in devicesToRemove)
        {
            Remove(device);
        }

        // Add a copy of any new device in the "from" list
        foreach (var device in fromDevices2)
        {
            var newDevice = new Device(this, device.Id).AddObserver(House.ModelObserver);
            Add(newDevice);
            newDevice.CopyFrom(device);
        }
    }

    internal bool IsIdenticalTo(Devices devices)
    {
        if (Count != devices.Count)
        {
            return false;
        }
        for (int i = 0; i < Count; i++)
        {
            if (!this[i].IsIdenticalTo(devices[i]))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// House this list of devices belongs to
    /// </summary>
    public House House { get; init; } = null!;

    /// <summary>
    /// Called after deserialization to initialize other data members
    /// </summary>
    internal void OnDeserialized()
    {
        foreach (Device device in this)
        {
            device.OnDeserialized();
        }
        AddObserver(House.ModelObserver);
    }

    /// <summary>
    /// For observers to subscribe to change notifications
    /// </summary>
    /// <param name="observer"></param>
    public Devices AddObserver(IDevicesObserver observer)
    {
        observers.Add(observer);
        return this;
    }

    /// <summary>
    /// For observers to unsubscribe to change notifications
    /// </summary>
    /// <param name="observer"></param>
    public Devices RemoveObserver(IDevicesObserver observer)
    {
        observers.Remove(observer);
        return this;
    }

    private List<IDevicesObserver> observers = new List<IDevicesObserver>();

    /// <summary>
    /// Find a device by ID
    /// </summary>
    internal Device? GetDeviceByID(InsteonID Id)
    {
        if (!TryGetEntry(Device.GetHashCodeFromId(Id), out Device? device))
        {
            return null;
        }
        return device;
    }

    /// <summary>
    /// Same as above with Try pattern
    /// </summary>
    /// <param name="Id"></param>
    /// <param name="device"></param>
    /// <returns></returns>
    internal bool TryGetDeviceByID(InsteonID Id, [NotNullWhen(true)] out Device? device)
    {
        return TryGetEntry(Device.GetHashCodeFromId(Id), out device);
    }

    /// <summary>
    /// Contact specified gateway/hub via Http and retrieve its properties
    /// If hub is not in the list of devices, add it
    /// </summary>
    internal async Task<Device?> CheckHubAsync(Gateway gateway)
    {
        // Check whether we have access to a hub at that new location
        var cmd = new GetIMInfoCommand(gateway);
        if (await cmd.TryRunAsync())
        {
            var device = GetDeviceByID(cmd.InsteonID);
            if (device == null)
            {
                // Create the hub and add to this list if not already here
                device = new Device(this, cmd.InsteonID);
                device.AddedDateTime = DateTime.Now;
                device.AddObserver(House.ModelObserver);
                Insert(0, device);

                // Set these after inserting the device so that they can generate change deltas
                device.CategoryId = cmd.CategoryId;
                device.SubCategory = cmd.Subcategory;
                device.Revision = cmd.FirmwareRevision;
                device.IsProductDataKnown = true;
            }

            device.EnsureChannels();

            return device;
        }
        return null;
    }

    /// <summary>
    /// Schedule adding a new device manually to the network, i.e., the user has to press the SET button for 3 sec on the actual device
    /// If successful, update properties and links from the new device and links the hub 
    /// </summary>
    /// <param name="completionCallback">called on completion if not null</param>
    /// <param name="delay">delay before running job</param>
    /// <returns>handle to scheduled job</returns>
    public object ScheduleAddDeviceManually(Scheduler.JobCompletionCallback<(Device? device, bool isNew)>? completionCallback = null,
        TimeSpan delay = new TimeSpan())
    {
        return InsteonScheduler.Instance.AddAsyncJob(
            "Adding Device Manually",
            AddDeviceManuallyAsync,
            (deviceWithIsNew) => { completionCallback?.Invoke((deviceWithIsNew?.Device, deviceWithIsNew?.IsNew ?? false)); },
            delay: delay);
    }

    /// <summary>
    /// Schedule adding a new device to the network by Id, or connecting the device to the hub if it exist already
    /// If successful, update properties and links from the new device and links the hub 
    /// </summary>
    /// <param name="DeviceId">device to add</param>
    /// <param name="completionCallback">called on completion if not null</param>
    /// <param name="delay">delay before running job</param>
    /// <returns>handle to scheduled job</returns>
    public object ScheduleAddOrConnectDevice(InsteonID DeviceId, 
        Scheduler.JobCompletionCallback<(Device? device, bool isNew)>? completionCallback = null, 
        TimeSpan delay = new TimeSpan())
    {
        return InsteonScheduler.Instance.AddAsyncJob(
            $"Adding or Connecting Device - {DeviceId}",
            () => AddOrConnectDeviceAsync(DeviceId),
            (deviceWithIsNew) => { completionCallback?.Invoke((deviceWithIsNew?.Device, deviceWithIsNew?.IsNew ?? false)); }, 
            delay: delay, maxRunCount: 1);
    }

    /// <summary>
    /// schedule cancelling adding a device manually
    /// </summary>
    /// <param name="job">Manual Add Device job to cancel</param>
    /// <returns>success</returns>
    public bool ScheduleCancelAddDevice(object job)
    {
        return InsteonScheduler.Instance.CancelJob(job, CancelAddDeviceAsync);
    }

    /// <summary>
    /// Schedule removing a device from the network.
    /// This takes a deviceId (not a device) and as such can remove a device that is not in the model anymore.
    /// It will remove all references to this device id.
    /// </summary>
    /// <param name="deviceId">device to remove</param>
    /// <param name="completionCallback">called on completion if not null</param>
    /// <param name="delay">delay before running job</param>
    /// <returns>handle to scheduled job</returns>
    public object ScheduleRemoveDevice(InsteonID deviceId, Scheduler.JobCompletionCallback<bool>? completionCallback = null, object? group = null, TimeSpan delay = new TimeSpan())
    {
        return InsteonScheduler.Instance.AddJob(
            "Removing Device - " + deviceId,
            () => RemoveDevice(deviceId),
            completionCallback,
            prehandler: null,
            group,
            delay,
            Scheduler.Priority.Medium);
    }

    /// <summary>
    /// Schedule import all devices from the state of the Insteon network
    /// </summary>
    /// <param name="completionCallback">called on completion if not null</param>
    /// <param name="delay">delay before running job</param>
    /// <returns>handle to scheduled job</returns>
    public void ScheduleImportAllDevices(Scheduler.JobCompletionCallback<bool>? completionCallback = null, TimeSpan delay = new TimeSpan())
    {
        var hub = House.Hub;

        // Hub discovery should have added a device for the hub
        Debug.Assert(hub != null);

        readHubJob = hub.ScheduleReadDevice(success =>
        {
            if (!InsteonScheduler.Instance.IsCancelled(readHubJob))
            {
                readDevicesGroup = InsteonScheduler.Instance.AddGroup("Importing devices", success =>
                {
                    readDevicesGroup = null;
                    completionCallback?.Invoke(success);
                });

                HashSet<InsteonID> alreadyReadDevices = new();

                foreach (var record in hub.AllLinkDatabase)
                {
                    // If command is cancelled, terminate the loop and invoke completion callback
                    if (readDevicesGroup == null)
                        break;

                    // If this device was already imported, skip
                    var deviceId = record.DestID;
                    if (alreadyReadDevices.Contains(deviceId))
                        continue;

                    alreadyReadDevices.Add(deviceId);

                    // Read existing device if we have one, create new and add to this otherwise
                    var device = GetDeviceByID(deviceId);
                    if (device == null)
                    {
                        device = new Device(this, deviceId).AddObserver(House.ModelObserver);
                        device.AddedDateTime = DateTime.Now;
                        Add(device);
                    }

                    // Device should really not be the hub
                    Debug.Assert(!device.IsHub);
                    if (device.IsHub)
                        continue;

                    // For now skip devices that require user action to wake up.
                    if (device.IsRemoteLinc)
                        continue;

                    // Read device properties from the physical device
                    // Read Product Data as soon as possible for all devices, so that we can determine what driver they need,
                    // create the proper view model and show the proper properties and channels as soon as possible.
                    device.ScheduleReadProductData(success =>
                    {
                        if (success)
                        {
                            device.ScheduleReadDevice(group: readDevicesGroup, priority: Scheduler.Priority.Low, forceRead: true);
                        }
                    },
                    group: readDevicesGroup, priority: Scheduler.Priority.Low, forceRead: true);
                }
            }

            readHubJob = null;
        },
        delay,
        priority: Scheduler.Priority.Low);
    }

    /// <summary>
    /// Cancel import of all devices to a new hub
    /// </summary>
    public void CancelImportAllDevices()
    {
        if (readHubJob != null)
        {
            InsteonScheduler.Instance.CancelJob(readHubJob);
            readHubJob = null;
        }

        if (readDevicesGroup != null)
        {
            InsteonScheduler.Instance.CancelJob(readDevicesGroup);
            readDevicesGroup = null;
        }
    }
    private object? readHubJob;
    private object? readDevicesGroup;

    /// <summary>
    /// SchedulecConnection of every not yet connected device to the Hub.
    /// Can be used to connect all devices to a new hub, for example.
    /// </summary>
    /// <param name="completionCallback">called on completion if not null</param>
    /// <param name="delay">delay before running job</param>
     public void ScheduleConnectAllDevices(Scheduler.JobCompletionCallback<bool>? completionCallback = null, TimeSpan delay = new TimeSpan())
    {
        HashSet<Device> devicesToConnect = new();

        // First we ping all devices and collect the ones not responding
        // which we then attempt to connect in ScheduleConnectDevices.
        pingAllDevicesGroup = InsteonScheduler.Instance.AddGroup("Pinging all Devices", success =>
        {
            pingAllDevicesGroup = null;
            if (devicesToConnect.Count > 0)
            {
                ScheduleConnectDevices(devicesToConnect, completionCallback);
            }
            else
            {
                completionCallback?.Invoke(success);
            }
        });

        foreach (var device in this)
        {
            // If command is cancelled, terminate the loop and invoke completion callback
            if (pingAllDevicesGroup == null)
                break;

            // If this is the Hub, skip.
            if (device.IsHub)
                continue;

            // if device is already known to be connected, move to next.
            if (device.IsResponderToHubGroup0())
                continue;

            // For now skip devices that require user action to wake up.
            if (device.IsRemoteLinc)
                continue;

            // Otherwise, try pinging.
            device.SchedulePingDevice(status =>
            {
                if (status == Device.ConnectionStatus.Connected)
                    return;

                // If ping did not work, add device to the list of devices to connect.
                // This can be called several times for the same device because of retries,
                // but only one instance of the device will be in the set.
                devicesToConnect.Add(device);
            },
            group:pingAllDevicesGroup);
        }
    }
    private object? pingAllDevicesGroup = null;

    // Helper to connect all devices in a set.
    // TODO: at the end of this job, it would be interesting to show the list of devices we failed to connect.
    private void ScheduleConnectDevices(HashSet<Device> devices, Scheduler.JobCompletionCallback<bool>? completionCallback = null)
    {
        connectDevicesGroup = InsteonScheduler.Instance.AddGroup("Attempting to Connect Devices", success =>
        {
            connectDevicesGroup = null;
            completionCallback?.Invoke(success);
        });

        foreach (var device in devices)
        {
            // For now skip devices that require user action to wake up.
            if (device.IsRemoteLinc)
                continue;

            InsteonScheduler.Instance.AddAsyncJob(
                "Attempting to Connect - " + device.DisplayNameAndId,
                handlerAsync: () => AddOrConnectDeviceAsync(device.Id),
                completionCallback: (d) =>
                {
                    Debug.Assert(d == null || d.Device == device);

                    // Read the properties of the newly connected device
                    device.ScheduleReadDeviceProperties(null, forceSync: true);
                },
                group: connectDevicesGroup,
                maxRunCount: 1);
        }
    }
    private object? connectDevicesGroup = null;

    /// <summary>
    /// Cancel Connection of all devices to a new hub
    /// </summary>
    public void CancelConnectAllDevices()
    {
        if (pingAllDevicesGroup != null)
        {
            InsteonScheduler.Instance.CancelJob(pingAllDevicesGroup);
            pingAllDevicesGroup = null;
        }
        if (connectDevicesGroup != null)
        {
            InsteonScheduler.Instance.CancelJob(connectDevicesGroup);
            connectDevicesGroup = null;
        }
    }

    // Context for the devices synchronization passes
    private struct SyncDevicesContext
    {
        public SyncDevicesContext() { }
        internal Scheduler.JobCompletionCallback<bool>? complectionCallback = default;
        internal bool forceRead = default;
        internal object? syncJob = default;
        internal IEnumerator? devicesEnumerator = default;
        internal bool hasFailure = default;
        internal List<Device> devicesNeedingUserAction = new();
    }
    private SyncDevicesContext syncContext = new();

    // Pending user action request for a device
    private struct UserActionContext
    {
        internal void Reset() { device = null; handler = null; }
        internal Device? device;
        internal GatedHandler? handler;
    }
    private UserActionContext userActionContext = new();

    // Handler for user action gated calls.
    // Action was confirmed is confirmAction is true, declined otherwise.
    private delegate object? GatedHandler(bool confirmAction);

    // TODO: temporary, should start at 1 to allow pending changes in deserialized model to be synced.
    // We keep it at 0 for now to require pressing the sync button to start the background sync.
    private SemaphoreSlim syncSemaphore = new(0, 1);

    /// <summary>
    /// Starts background syncing of all devices with their physical counterparts.
    /// This perform a synchronization pass by looping over all devices and syncing the ones that need it.
    /// It then waits for a call to SyncDevicesNow to start a new pass again and so on.
    /// </summary>
    internal void StartBackgroundDeviceSync()
    {
        RunBackgroundDeviceSync();
        // TODO: temporary, won't be necessary when we start the syncSemaphore at 1 (see above)
        House.ModelObserver.NotifyModelNeedsSync();
        Logger.Log.Debug("Background update of devices started");
    }

    // Helper to run the background syncing of devices
    private void RunBackgroundDeviceSync()
    {
        var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        dispatcherQueue.TryEnqueue(async () =>
        {
            // Wait until SyncDevicesNow is called
            await syncSemaphore.WaitAsync();

            // Capture completion callback and forceRead here for this pass
            // and reset them for the next pass after that
            var completionCallback = syncContext.complectionCallback;
            var forceRead = syncContext.forceRead;
            syncContext.complectionCallback = null;
            syncContext.forceRead = false;

            // Schedule a pass of syncing devices
            ScheduleSyncDevices((success) =>
            {
                completionCallback?.Invoke(success);
                RunBackgroundDeviceSync();
            }, forceRead: forceRead);
            
        });
    }

    /// <summary>
    /// Cancel current pass of background synchronizing of devices
    /// </summary>
    public void CancelBackgroundDeviceSync()
    {
        if (syncContext.syncJob != null)
        {
            InsteonScheduler.Instance.CancelJob(syncContext.syncJob);
            syncContext.syncJob = null;
        }
        syncContext.devicesEnumerator = null;
    }

    /// <summary>
    /// Allows a synchronization pass to start. If a pass is currently running,
    /// another one will be allowed to start once the current one is done.
    /// The passed forceRead is used only for the next pass to run, then reset.
    /// The passed callback is called when the next pass is done, then reset.
    /// <param name="completionCallback"></param>
    /// <param name="forceRead"></param>  
    /// </summary>
    public void SyncDevicesNow(Scheduler.JobCompletionCallback<bool>? completionCallback = null, bool forceRead = false)
    {
        syncContext.forceRead = forceRead;
        syncContext.complectionCallback = completionCallback;
        if (syncSemaphore.CurrentCount == 0)
            syncSemaphore.Release();
    }

    /// <summary>
    /// Sync all logical devices with their coresponding physical devices as a low-priority activity.
    /// This only perform work for a given device if sync is needed.
    /// <param name="completionCallback">called when the last device is reached</param>
    /// <param name="forceRead">forces reading properties and link database from the physical device, even if already marked in sync</param>
    /// <return>job object</return>
    /// </summary>
    private void ScheduleSyncDevices(Scheduler.JobCompletionCallback<bool>? completionCallback = null, Scheduler.Priority priority = Scheduler.Priority.Low, bool forceRead = false)
    {
        // If the device enumerator is null, restart it
        if (syncContext.devicesEnumerator == null)
        {
            syncContext.devicesEnumerator = GetEnumerator();
            syncContext.hasFailure = false;
        }

        // Continue from where the enumerator is at
        syncContext.syncJob = ScheduleSyncNextDevice((success) =>
        {
            if (!success)
                syncContext.hasFailure = true;

            syncContext.syncJob = null;
            if (syncContext.devicesEnumerator != null)
            {
                ScheduleSyncDevices(completionCallback, priority, forceRead: forceRead);
            }
            else
            {
               completionCallback?.Invoke(!syncContext.hasFailure);
            }
        }, priority, forceRead: forceRead);
    }

    // Helper for a single step of ScheduleSyncDevices.
    // Schedule a synchronization for the next device that needs sync.
    private object? ScheduleSyncNextDevice(Scheduler.JobCompletionCallback<bool>? completionCallback, Scheduler.Priority priority = Scheduler.Priority.Low, bool forceRead = false)
    {
        object? job = null;
        if (syncContext.devicesEnumerator != null)
        {
            // Iterate over devices until the first one that needs sync, schedule it and return.
            // The completion callback is called when the scheduled device has been synced.
            do
            {
                if (syncContext.devicesEnumerator.MoveNext())
                {
                    var device = syncContext.devicesEnumerator.Current as Device;

                    if (device != null)
                    {
                        if (device.IsRemoteLinc && !device.Snoozed && device.DeviceNeedsSync)
                        {
                            // If the device needs sync but requires user action, ensure it's in the list of devices needing user action
                            if (!syncContext.devicesNeedingUserAction.Contains(device))
                                syncContext.devicesNeedingUserAction.Add(device);
                        }
                        else
                        {
                            // ...otherwise schedule a sync for it.
                            // This returns null if the device does not need sync.
                            job = device.ScheduleSync((success) => { completionCallback?.Invoke(success); },
                                priority: priority, forceRead: forceRead);
                        }
                    }
                }
                else
                {
                    // We haved run through all the devices.
                    // Null out the enumerator to indicate we are done.
                    syncContext.devicesEnumerator = null;

                    // Handle next device needing user action
                    var device = syncContext.devicesNeedingUserAction.FirstOrDefault();
                    if (device != null)
                    {
                        GateOnUserAction(device, (confirmAction) =>
                        {
                            object? job = null;
                            if (confirmAction)
                            {
                                job = device.ScheduleSync((success) => { completionCallback?.Invoke(success); },
                                    priority, forceRead: forceRead);
                            }
                            else
                            {
                                // If the user decline action, snooze the device.
                                // User will need to go to the device view to un-snooze.
                                device.Snoozed = true;
                            }

                            syncContext.devicesNeedingUserAction.Remove(device);

                            // If we have more devices needing user action, trigger another pass to pick up the next one
                            if (syncContext.devicesNeedingUserAction.Count > 0)
                            {
                                device.PropertiesSyncStatus = SyncStatus.Changed;
                            }
                            completionCallback?.Invoke(true);
                            return job;
                        });
                    }
                    else
                    {
                        completionCallback?.Invoke(true);
                    }
                    break;
                }
            } while (job == null);  // loop until we schedule the next device that needs sync
        }
        return job;
    }

    // Request and wait for user to confirm or decline to execute a handler.
    private void GateOnUserAction(Device device, GatedHandler handler)
    {
        userActionContext.device = device;
        userActionContext.handler = handler;

        device.ScheduleGetConnectionStatus((connectionStatus) =>
        {
            if (connectionStatus == Device.ConnectionStatus.Connected)
            {
                Logger.Log.ClearUserAction($"Device '{device.DisplayNameAndId}' awake and about to be updated");
                handler.Invoke(confirmAction: true);
                userActionContext.Reset();
            }
            else
            {
                Logger.Log.RequestUserAction($"To wake up '{device.DisplayNameAndId}', please tap 'Play' while holding any button down. Tap 'X' to snooze the device.");
            }
        },
        force: true);
    }

    // Request user action and wait for user to confirm if necessary
    private object? GateOnUserActionIfNecessary(Device? device, GatedHandler handler)
    {
        if (device != null && device.IsRemoteLinc)
        {
            GateOnUserAction(device, handler);
            return null;
        }
        else
        {
            return handler.Invoke(confirmAction: true);
        }
    }

    /// <summary>
    /// User confirms taking requested action (see House)
    /// </summary>
    internal void ConfirmUserAction()
    {
        Debug.Assert(userActionContext.device != null);
        Debug.Assert(userActionContext.handler != null);
        GateOnUserAction(userActionContext.device, userActionContext.handler);
    }

    /// <summary>
    /// User declines taking requested action (see House)
    /// </summary>
    internal void DeclineUserAction()
    {
        if (userActionContext.device != null)
        {
            Debug.Assert(userActionContext.handler != null);
            Logger.Log.ClearUserAction($"Device '{userActionContext.device.DisplayNameAndId}' - Snoozed");
            userActionContext.handler(confirmAction: false);
        }
    }

    /// <summary>
    /// These perform a basic Add, Remove or Insert to this collection without any side effects
    /// (i.e., no other state is affected). To be used for example when building the list
    /// from the serialization state.
    /// These methods reset the devices enumerator if used since C# will throw an exception 
    /// when a collection changes under an iterator.
    /// </summary>
    /// <param name="device"></param>
    public override void Add(Device device)
    {
        if (syncContext.devicesEnumerator != null)
        {
            syncContext.devicesEnumerator = null;
            base.Add(device);
            syncContext.devicesEnumerator = GetEnumerator();
        }
        else
            base.Add(device);

        observers.ForEach(o => o.DeviceAdded(device));
    }

    public override void Insert(int seq, Device device)
    {
        if (syncContext.devicesEnumerator != null)
        {
            syncContext.devicesEnumerator = null;
            base.Insert(seq, device);
            syncContext.devicesEnumerator = GetEnumerator();
        }
        else
            base.Insert(seq, device);

        observers.ForEach(o => o.DeviceInserted(seq, device));
    }

    public override bool Remove(Device device)
    {
        var success = true;

        if (syncContext.devicesEnumerator != null)
        {
            syncContext.devicesEnumerator = null;
            success = base.Remove(device);
            syncContext.devicesEnumerator = GetEnumerator();
        }
        else
            success = base.Remove(device);

        if (success)
            observers.ForEach(o => o.DeviceRemoved(device));

        return success;
    }

    // Helper to remove all links to a specified device from any device, including the hub.
    private void RemoveAllLinksTo(InsteonID id)
    {
        foreach (Device device in this)
        {
            var linkRecords = device.GetLinksToDevice(id);
            if (linkRecords != null)
            {
                foreach (AllLinkRecord record in linkRecords)
                {
                    if (record.IsInUse)
                    {
                        device.AllLinkDatabase.RemoveRecord(new(record){ SyncStatus = SyncStatus.Changed });
                    }
                }
            }
        }
    }

    /// <summary>
    /// Schedule read of all properties of all devices on the network
    /// This method only updates the devices, not the hub
    /// </summary>
    /// <param name="completionCallback"></param>
    public object ScheduleReadAllDevices(Scheduler.JobCompletionCallback<bool>? completionCallback = null, TimeSpan delay = new TimeSpan())
    {
        object localGroup = InsteonScheduler.Instance.AddGroup("Reading all Devices", completionCallback);
        foreach (Device device in this)
        {
            if (!device.IsHub)
            {
                device.ScheduleReadDevice(null, localGroup, delay, Scheduler.Priority.Low);
            }
        }

        return localGroup;
    }

    /// <summary>
    /// Cancel scheduled job - See Insteon Scheduler
    /// </summary>
    /// <param name="job"></param>
    public void CancelScheduledJob(object? job)
    {
        if (job != null)
            InsteonScheduler.Instance.CancelJob(job);
    }

    // Return value for AddDeviceManually and AddOrConnectDeviceAsync.
    // Returns the device and indicates whether it was just created.
    // We use a record rather than a tuple to use existing handling of 
    // an object-type return value from the handler in Scheduler.
    internal record DeviceWithIsNew
    {
        internal Device Device = null!;
        internal bool IsNew;
    }

    // Add device to the Inteon network, if not already in, by the user pressing the SET button
    // ALL-Link the device to the Hub
    // Returns an instance of DevivceWithIsNew with the new device, whether the device was created,
    // or null if failed (e.g., device does not exist on the network</returns>).
    internal async Task<DeviceWithIsNew?> AddDeviceManuallyAsync()
    {
        DeviceWithIsNew deviceWithIsNew = new();

        var cmd = new StartIMAllLinkingCommand(House.Gateway, 0, LinkingAction.CreateControllerLink);
        if (await cmd.TryRunAsync() && cmd.ErrorReason != Command.ErrorReasons.Cancelled)
        {
            if (cmd.LinkAction == LinkingAction.CreateControllerLink)
            {
                var device = GetDeviceByID(cmd.DeviceId);

                if (device == null)
                {
                    device = new Device(this, cmd.DeviceId).AddObserver(House.ModelObserver);
                    device.AddedDateTime = DateTime.Now;
                    Add(device);
                    deviceWithIsNew.IsNew = true;
                }

                // Set product data given by the IMAllLinking command
                // (after adding the device to this Devices list to generate proper change deltas)
                device.CategoryId = cmd.DeviceCategory;
                device.SubCategory = cmd.DeviceSubCategory;

                // Get the rest of the product data
                // If we fail to acquire product data, we'll run with the information obtained
                // from the IMAllLinking command, and try again later as part of device synchronization.
                if (await device.TryReadProductDataAsync())
                {
                    // Observers might have to adjust to the new product data
                    // (e.g., view model created when adding the device might have to change)
                    observers.ForEach(o => o.DeviceTypeChanged(device));

                    // Channels may have changed as operating flags control the number of channels
                    // on a certain devices such as KeypadLinc for example
                    device.EnsureChannels();

                    // Force reading the properties of the new device
                    // We schedule this here so that it occurs as soon as possible after the interaction
                    // with the user is finished 
                    device.ResetPropertiesSyncStatus();
                    device.ScheduleReadDeviceProperties(forceSync: true);

                    // Mark link database of the device unknown to force reading from physical device
                    device.AllLinkDatabase.ResetSyncStatus();

                    // Mark hub database unknown to force reading from physical device
                    // and acquire link just added for the new device
                    var hub = GetDeviceByID(House.Gateway.DeviceId);
                    hub?.AllLinkDatabase.ResetSyncStatus();

                    // Record that this device is known to be connected
                    device.SetKnownConnectionStatus(Device.ConnectionStatus.Connected);

                    deviceWithIsNew.Device = device;
                }

                return deviceWithIsNew;
            }
        }

        return null;
    }

    // Cancel attempt to add a device with AddDeviceManually
    private async Task<bool> CancelAddDeviceAsync()
    {
        // If the StartIMLinking command is running, cancel it, 
        // otherwise just queue and run the CancelIMLinking command
        if (Command.Running != null && Command.Running.GetType() == typeof(StartIMAllLinkingCommand))
        {
            await Command.Running.Cancel();
            return true;
        }
        else
        {
            CancelIMAllLinkingCommand deviceCommand = new CancelIMAllLinkingCommand(House.Gateway);
            return await deviceCommand.TryRunAsync();
        }
    }

    // Add a device to the Insteon network by Id, if not already in
    // ALL-Link the device to the Hub
    // Returns an instance of DevivceWithIsNew with the new device, whether the device was created,
    // or null if failed (e.g., device does not exist on the network</returns>).
    internal async Task<DeviceWithIsNew?> AddOrConnectDeviceAsync(InsteonID deviceId)
    {
        DeviceWithIsNew deviceWithIsNew = new();

        var cmd = new StartIMAllLinkingCommand(House.Gateway, deviceId, 0, LinkingAction.CreateControllerLink)
        {
            MockPhysicalIM = House.GetMockPhysicalDevice(House.Gateway.DeviceId) as MockPhysicalIM,
            MockPhysicalDevice = House.GetMockPhysicalDevice(deviceId),
        };
        if (await cmd.TryRunAsync())
        {
            if (cmd.LinkAction == LinkingAction.CreateControllerLink && cmd.ErrorReason != Command.ErrorReasons.Cancelled)
            {
                var device = GetDeviceByID(cmd.DeviceId);

                if (device == null)
                {
                    device = new Device(this, cmd.DeviceId).AddObserver(House.ModelObserver);
                    device.AddedDateTime = DateTime.Now;
                    Add(device);
                    deviceWithIsNew.IsNew = true;
                }

                // Set product data given by the IMAllLinking command
                // (after adding the device to this Devices list to generate proper change deltas)
                device.CategoryId = cmd.DeviceCategory;
                device.SubCategory = cmd.DeviceSubCategory;

                // Get the rest of the product data
                // If we fail to acquire product data, we'll run with the information obtained
                // from the IMAllLinking command, and try again later as part of device synchronization.
                if (await device.TryReadProductDataAsync())
                {
                    // Observers might have to adjust to the new product data
                    // (e.g., view model created when adding the device might have to change)
                    observers.ForEach(o => o.DeviceTypeChanged(device));

                    // Channels may have changed as operating flags control the number of channels
                    // on a certain devices such as KeypadLinc for example
                    device.EnsureChannels();

                    // Force reading the properties of the new device
                    // We schedule this here so that it occurs as soon as possible after the interaction
                    // with the user is finished 
                    device.ResetPropertiesSyncStatus();
                    device.ScheduleReadDeviceProperties(forceSync: true);

                    // Mark link database of the device unknown to force reading from physical device
                    device.AllLinkDatabase.ResetSyncStatus();

                    // Mark hub database unknown to force reading from physical network via background sync
                    // and acquire link just added for the new device
                    var hub = GetDeviceByID(House.Gateway.DeviceId);
                    hub?.AllLinkDatabase.ResetSyncStatus();

                    // Record that this device is known to be connected
                    device.SetKnownConnectionStatus(Device.ConnectionStatus.Connected);

                    deviceWithIsNew.Device = device;
                }

                return deviceWithIsNew;
            }
        }

        // We failed to add the device, e.g., because no device with this id is found on the network
        // Attempt to cancel the StartIMAllLinking command
        CancelIMAllLinkingCommand deviceCommand = new CancelIMAllLinkingCommand(House.Gateway);
        await deviceCommand.TryRunAsync();

        return null;
    }

    // Remove a device from the Insteon network.
    // This remove the device id from the model entirely, including delinking it from the hub.
    internal bool RemoveDevice(InsteonID deviceId)
    {
        // First, remove all links to the device (including the hub) and all scene membership.
        // This will be synced to the physical device in the next sync pass.
        RemoveAllLinksTo(deviceId);
        House.Scenes.RemoveDevice(deviceId);

        // If this device is in this list of devices, remove it
        var device = GetDeviceByID(deviceId);
        if (device != null)
        {
            Remove(device);
        }

        return true;
    }

    // Async method to connect all devices in this list to the Hub
    // Return success or failure
    private async Task<bool> TryConnectAllDevicesAsync()
    {
        // Copy the list of devices as it may change
        Device[] deviceArray = new Device[Count];
        CopyTo(deviceArray, 0);

        bool success = true;

        foreach (Device device in deviceArray)
        {
            bool connected = false;
            if (device.IsResponderToHubGroup0())
            {
                connected = true;
            }
            else
            {
                connected = await device.GetConnectionStatusAsync() == Device.ConnectionStatus.Connected;
            }

            if (!connected)
            {
                if (await AddOrConnectDeviceAsync(device.Id) == null)
                {
                    success = false;
                }
            }
        }

        return success;
    }
    
    // Return the list of devices linked to the given device
    private List<Device> DevicesLinkedTo(InsteonID id)
    {
        List<Device> foundDevices = new List<Device>();

        foreach (Device device in this)
        {
            if (device.HasLinkToDevice(id))
            {
                foundDevices.Add(device);
            }
        }

        return foundDevices;
    }

    /// <summary>
    /// Schedule ensuring that the first link in the database of all devices is a link to the hub
    /// Swap existing responder link to the Hub on group 0 with the first link in the database
    /// </summary>
    public void EnsureLinkToHubIsFirst()
    {
        foreach (Device device in this)
        {
            device.EnsureLinkToHubIsFirst();
        }
    }
}
