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
using Insteon.Synchronization;
using System.Diagnostics;
using Insteon.Mock;
using System.Diagnostics.CodeAnalysis;

namespace Insteon.Model;

public sealed class House
{
    public House()
    {
        ModelRecorder = new();
        ModelObserver = new(ModelRecorder);
    }

#if DEBUG
    // Assign an immutable Id to a house for easier debugging.
    private static int nextId = 1;
    public readonly int Id = nextId++;
    public override string ToString() => $"House - {Id}";
#endif

    public string Name { get; set; } = "";

    public string Version { get; set; } = "";

    public HouseLocation? HouseLocation { get; set; } = null;

    public Gateways Gateways = null!;

    public Devices Devices = null!;

    public Scenes Scenes = null!;

    public Rooms Rooms = null!;

    public void CopyFrom(House house)
    {
        // The model we are copying from is already the result of applying these changes
        // and we have no need to re-record them.
        var wasRecording = ModelRecorder.StopRecording();

        Name = house.Name;
        Version = house.Version;
        HouseLocation = house.HouseLocation;
        Gateways.CopyFrom(house.Gateways);
        Devices.CopyFrom(house.Devices);
        Scenes.CopyFrom(house.Scenes);
        Rooms ??= new Rooms(this);

        if (wasRecording) ModelRecorder.StartRecording();
    }

    public bool IsIdenticalTo(House house)
    {
        return
            Name == house.Name &&
            Version == house.Version &&
            HouseLocation == house.HouseLocation &&
            Gateways.IsIdenticalTo(house.Gateways) &&
            Devices.IsIdenticalTo(house.Devices) &&
            Scenes.IsIdenticalTo(house.Scenes);
    }

    /// <summary>
    /// Called after deserialization to allow for post-processing
    /// </summary>
    internal void OnDeserialized()
    {
        Gateways.OnDeserialized();
        Devices.OnDeserialized();
        Scenes.OnDeserialized();
        Rooms ??= new Rooms(this);
    }

    /// <summary>
    /// Used to request persisting the model immediately after deserializing it
    /// to handle changes in the model format that need to be saved immediately,
    /// before we attempt load the model again to play local changes against it.
    /// </summary>
    public bool RequestSaveAfterLoad { get; internal set; } = false;

    /// <summary>
    /// Allows merging of changes to the model from different sources
    /// Can play recorded changes against the previous version of the model
    /// </summary>
    internal ModelRecorder ModelRecorder { get; private set; }

    /// <summary>
    /// Play recorded but yet unplayed changes to this model against a target model
    /// </summary>
    public void PlayChanges(House targetHouse)
    {
        ModelRecorder.Play(targetHouse);
    }

    /// <summary>
    /// Observer of changes in the model
    /// </summary>
    public ModelObserver ModelObserver { get; private set; }

    /// <summary>
    /// Ensure that a house has a gateway
    /// </summary>
    /// <returns>this, to allow chaining behind new() or other methods returning this</returns>
    public House WithGateway()
    {
        Gateways ??= new Gateways(this).Ensure().AddObserver(ModelObserver);
        return this;
    }

    /// <summary>
    /// Ensure that this house has all the required objects of the model
    /// </summary>
    /// <returns>this, to allow chaining behind new() or other methods returning this</returns>
    public House EnsureNew()
    {
        WithGateway();
        Devices ??= new Devices(this).AddObserver(ModelObserver);
        Scenes ??= new Scenes(this).AddObserver(ModelObserver);
        Rooms ??= new Rooms(this);
        return this;
    }

    /// <summary>
    /// Starts change recording changes to the model
    /// </summary>
    public void StartRecordingChanges()
    {
        ModelRecorder.StartRecording();
    }

    /// <summary>
    /// Start background sync of physical devices
    /// </summary>
    public void StartBackgroundDeviceSync()
    {
        Devices.StartBackgroundDeviceSync();
    }

    /// <summary>
    /// Find a device by ID in the DeviceList
    /// </summary>
    /// <param name="Id"></param>
    /// <returns>Device</returns>
    public Device? GetDeviceByID(InsteonID Id)
    {
        return Devices.GetDeviceByID(Id);
    }

    /// <summary>
    /// Same as above with Try pattern
    /// </summary>
    /// <param name="Id"></param>
    /// <param name="device"></param>
    /// <returns></returns>
    public bool TryGetDeviceByID(InsteonID Id, [NotNullWhen(true)] out Device? device)
    {
        device = Devices.GetDeviceByID(Id);
        return device != null;
    }

    /// <summary>
    /// Return the active gateway
    /// </summary>
    public Gateway Gateway
    {
        get => Gateways[0];
    }

    /// <summary>
    /// Returns the hub, i.e., the gateway device
    /// </summary>
    public Device? Hub
    {
        get
        {
            Device? hub = null;
            if (Gateways != null && Gateways.Count > 0)
            {
                hub = GetDeviceByID(Gateways[0].DeviceId);
                Debug.Assert(hub == null || hub.IsHub);
            }

            return hub;
        }
    }

    /// <summary>
    /// Schedule pushing a new gateway to the list of gateways and making it active.
    /// The list can contain multiple gateways on the local network, but only the first one (top) is active at any time.
    /// The other ones might have been active before and not (yet) removed, e.g., the user is in the process 
    /// of replacing the gateway, or keeping the old gateway around for a while.
    /// The list can contain an entry at the top for a gateway that has not been found (has not responded to a ping 
    /// for gateway information). This will prevent accessing device views until the gateway settings have been fixed
    /// and the gateway has been found.
    /// </summary>
    /// <param name="macAddress"></param>
    /// <param name="hostName"></param>
    /// <param name="ipAddress"></param>
    /// <param name="port"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    public void SchedulePushNewGateway(string macAddress, string hostName, string ipAddress, string port, string username, string password,
        Common.Scheduler.JobCompletionCallback<bool>? completionCallback, int maxRunCount)
    {
        InsteonScheduler.Instance.AddAsyncJob(
            $"Checking Gateway at http://{ipAddress}:{port}, Username: {username}",
            handlerAsync: async () =>
            {
                return await TryPushNewGatewayAsync(macAddress, hostName, ipAddress, port, username, password);
            },
            completionCallback, maxRunCount: maxRunCount);
    }

    // Async helper to push a new gateway to the list of gateways and make it active
    private async Task<bool> TryPushNewGatewayAsync(string macAddress, string hostName, string ipAddress, string port, string username, string password)
    {
        // Try to ping the new gateway and if found, create the new hub device in the model if it does not already exist
        Gateway newGateway = new Gateway(this, hostName, macAddress, ipAddress, port, username, password);
        var hub = await Devices.CheckHubAsync(newGateway);
        if (hub != null)
        {
            newGateway = new Gateway(newGateway) { DeviceId = hub.Id };
        }
        PushNewGateway(newGateway);
        return hub != null;
    }

    /// <summary>
    /// Push a new gateway to the list of gateways and make it active.
    /// </summary>
    /// <param name="newGateway"></param>
    public void PushNewGateway(Gateway newGateway)
    {
        if (newGateway.DeviceId != null && !newGateway.DeviceId.IsNull)
        {
            // If new gateway exists (has a Id) and is same as at the top, we're set
            if (Gateways.Count > 0 && newGateway.Equals(Gateways[0]))
                return;

            // Store credentials for the new gateway in the credential store, indexed by Id;
            newGateway.StoreCredentials();
        }

        // Remove any entry from the list of gateways that does not have a device id
        // i.e., for which we did not find the gateway on the IP network
        for (int i = 0; i < Gateways.Count; i++)
        {
            if (Gateways[i].DeviceId.IsNull)
            {
                Gateways.RemoveAt(i);
                i--;
            }
        }

        // Remove any entry we may have for the new gateway as we are going to add it at the top
        if (newGateway.DeviceId != null && !newGateway.DeviceId.Equals(InsteonID.Null))
        {
            int j = 0;
            foreach (var gateway in Gateways)
            {
                if (newGateway.DeviceId == gateway.DeviceId)
                {
                    Gateways.RemoveAt(j);
                    break;
                }
                j++;
            }
        }

        // Set the new gateway at the top of the list
        Gateways.Insert(0, newGateway);

        NotifyDevicesOfGatewayChange();
        Gateways.NotifyObservers(newGateway);
    }

    // Helper method to notify all devices that the hub/gateway has changed
    private void NotifyDevicesOfGatewayChange()
    {
        foreach (var device in Devices)
        {
            device.OnGatewayChanged();
        }
    }

    /// <summary>
    /// Schedule removal of last gateway in the list of gateways if this gateway is not used anymore
    /// Removes all links to the gateway, then removes the gateway device and finally removes the gateway from the list of gateways
    /// </summary>
    /// <returns>scheduled job or null is there is no gateway</returns>
    public object? ScheduleRemoveOldGateway(Scheduler.JobCompletionCallback<bool>? completionCallback = null)
    {
        object? RemoveOldGatewayJob = null;

        if (Gateways.Count > 1)
        {
            Gateway gateway = Gateways[Gateways.Count - 1];

                RemoveOldGatewayJob = InsteonScheduler.Instance.AddGroup("Removing Old Gateway", (success) =>
                {
                    if (success && gateway == Gateways[Gateways.Count - 1])
                    {
                        Gateways.RemoveAt(Gateways.Count - 1);
                    }
                    completionCallback?.Invoke(success);
                });

            Devices.ScheduleRemoveDevice(gateway.DeviceId, null, RemoveOldGatewayJob);
        }

        return RemoveOldGatewayJob;
    }

    /// <summary>
    /// Cancel job - See Insteon Scheduler
    /// </summary>
    /// <param name="job"></param>
    public static void CancelJob(object? job)
    {
        InsteonScheduler.Instance.CancelJob(job);
    }

    /// <summary>
    /// Schedule group job - See Insteon Scheduler
    /// </summary>
    /// <param name="description"></param>
    /// <param name="jobCompletionCallback"></param>
    public static object ScheduleGroupJob(string description, InsteonScheduler.JobCompletionCallback<bool> jobCompletionCallback, object? parentGroupJob = null)
    {
        return InsteonScheduler.Instance.AddGroup(description, jobCompletionCallback, parentGroupJob);
    }

    /// <summary>
    /// Event fired when we are pushing network traffic through the gateway
    /// </summary>
    public event Action<bool>? OnGatewayTraffic;
    internal void NotifyGatewayTraffic(bool hasTraffic)
    {
        OnGatewayTraffic?.Invoke(hasTraffic);
    }

    /// <summary>
    /// User confirms taking requested action.
    /// This is used to attempt to connect to a device that requires user action,
    /// e.g., a remotelink2/mini remote, on which a button must be held down while sending a command to wake it up
    /// </summary>
    public void ConfirmUserAction()
    {
        Devices.ConfirmUserAction();
    }

    /// <summary>
    /// User declines taking the requested user action
    /// See above for more details
    /// </summary>
    public void DeclineUserAction()
    {
        Devices.DeclineUserAction();
    }

    /// <summary>
    /// For unit-testing purposes
    /// When unit-testing this holds the list of mock physical devices necessary for the tests, keyed on the device ids. 
    /// This enables the tests to create the mock physical devices they need.
    /// </summary>
    public House WithMockPhysicalDevice(MockPhysicalDevice device)
    {
        mockPhysicalDevices ??= new List<MockPhysicalDevice>();
        mockPhysicalDevices.Add(device);
        return this;
    }

    /// <summary>
    /// For unit-testing purposes
    /// Returns the mock physical device associated with a device, if any
    /// </summary>
    /// <param name="deviceId"></param>
    /// <returns></returns>
    public MockPhysicalDevice? GetMockPhysicalDevice(InsteonID deviceId)
    {
        return mockPhysicalDevices?.FirstOrDefault(d => d.Id == deviceId);
    }
    private List<MockPhysicalDevice>? mockPhysicalDevices;
}
