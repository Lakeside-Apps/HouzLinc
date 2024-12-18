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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Insteon.Model;
using Insteon.Mock;
using Insteon.Base;

namespace UnitTests.Insteon;

[TestClass]
public class TestModelChanges : ModelTestsBase
{
    [ClassCleanup]
    public static void ClassCleanup()
    {
        // Place breakpoint here to collect the test files
    }

    [TestInitialize]
    public void Init()
    {
        ApplicationType.IsUnitTestFramework = true;
    }

    [TestCleanup]
    public void Cleanup()
    {
        house = null!;
        targetHouse = null!;
    }

    // Add a device without channels, sync the new device and the hub databases,
    // and check that the correct changes are made to the model and recorded
    [TestMethod]
    public async Task TestAddDevice()
    {
        await LoadModels("Changes1");
        await house.AddNewDevice(new InsteonID("11.22.33"),
            DeviceKind.CategoryId.DimmableLightingControl,
            subCategory: 0x01 /*switchlinc dimmer*/,
            revision: 63,
            operatingFlags: 0);
        await PlayAndCheck();
    }

    // Add a device with channels, sync the new device and the hub databases,
    // and check that the correct changes are made to the model and recorded
    [TestMethod]
    public async Task TestAddDeviceWithChannels()
    {
        await LoadModels("Changes1");
        await house.AddNewDevice(new InsteonID("11.22.33"),
            categoryId: DeviceKind.CategoryId.DimmableLightingControl,
            subCategory: 0x41, /*keypad dimmer*/
            revision: 72,
            operatingFlags: 0x08 /*8 channels*/);
        await PlayAndCheck();
    }

    // Remove a device, sync the hub database
    // and check that the correct changes are made to the model and recorded
    [TestMethod]
    public async Task TestRemoveDevice()
    {
        var deviceId = new InsteonID("AA.22.22");
        await LoadModels("Changes1");

        house.WithMockPhysicalDevice(new MockPhysicalIM(house.Hub!.Id, new AllLinkDatabase(house.Hub.AllLinkDatabase)));

        house.Devices.RemoveDevice(deviceId);

        await house.Hub.TryReadAllLinkDatabaseAsync();

        await PlayAndCheck();
    }

    // Sets a number of device properties on different devices
    // and check that the correct changes are made to the model and recorded
    [TestMethod]
    public async Task TestDevicePropertyChange()
    {
        await LoadModels("Changes1");

        var device = house.Devices.GetDeviceByID(new InsteonID("AA.11.11"))!;
        device.Status = Device.ConnectionStatus.Disconnected;

        device = house.Devices.GetDeviceByID(new InsteonID("AA.22.22"))!;
        device.Wattage = 10;
        device.DisplayName = "New Name";
        device.Room = "New Room";
        device.Location = "New Location";

        device = house.Devices.GetDeviceByID(new InsteonID("AA.33.33"))!;
        device.OperatingFlags = 0x04;
        device.LEDBrightness = 0x80;
        Assert.IsTrue(device.PropertiesSyncStatus == SyncStatus.Changed);

        device = house.Devices.GetDeviceByID(new InsteonID("AA.44.44"))!;
        device.OperatingFlags = 0x20;
        device.LEDBrightness = 0x80;
        device.OnLevel = 128;
        device.RampRate = 10;
        Assert.IsTrue(device.PropertiesSyncStatus == SyncStatus.Changed);

        await PlayAndCheck();
    }

    // Sets a number of channel properties on different channels of a device
    // and check that the correct changes are made to the model and recorded
    [TestMethod]
    public async Task TestChannelPropertyChange()
    {
        await LoadModels("Changes1");

        var device = house.Devices.GetDeviceByID(new InsteonID("AA.55.55"))!;
        var channel = device.GetChannel(1);
        device.GetChannel(1).Name = "New Main";
        device.GetChannel(3).Name = "Vanity";
        device .GetChannel(1).FollowMask = 0x4F;
        device .GetChannel(2).FollowOffMask = 0x40;
        device.GetChannel(3).FollowMask = 0xC0;
        device.GetChannel(3).ToggleMode = ToggleMode.On;
        device.GetChannel(3).OnLevel = 128;
        device.GetChannel(3).RampRate = 10; /*2 minutes*/

        Assert.IsTrue(device.GetChannel(1).PropertiesSyncStatus == SyncStatus.Changed);
        Assert.IsTrue(device.GetChannel(2).PropertiesSyncStatus == SyncStatus.Changed);
        Assert.IsTrue(device.GetChannel(3).PropertiesSyncStatus == SyncStatus.Changed);

        await PlayAndCheck();
    }

    // Test replacing a device by another
    // and check that the correct changes are made to the model and recorded
    [TestMethod]
    public async Task TestReplaceDevice()
    {
        await LoadModels("Changes1");

        var replacementDeviceId = new InsteonID("11.22.33");
        await house.AddNewDevice(replacementDeviceId,
            DeviceKind.CategoryId.DimmableLightingControl,
            subCategory: 0x01 /*switchlinc dimmer*/,
            revision: 63,
            operatingFlags: 0);

        var deviceId = new InsteonID("AA.22.22");
        house.Devices.GetDeviceByID(deviceId)!.TryReplaceDeviceBy(replacementDeviceId);

        await PlayAndCheck();
    }

    // Test copying a device to another
    // and check that the correct changes are made to the model and recorded
    [TestMethod]
    public async Task TestCopyDevice()
    {
        await LoadModels("Changes1");

        var copyDeviceId = new InsteonID("11.22.33");
        await house.AddNewDevice(copyDeviceId,
            DeviceKind.CategoryId.DimmableLightingControl,
            subCategory: 0x01 /*switchlinc dimmer*/,
            revision: 63,
            operatingFlags: 0);

        var deviceId = new InsteonID("AA.99.99");
        house.Devices.GetDeviceByID(deviceId)!.TryCopyDeviceTo(copyDeviceId);

        await PlayAndCheck();
    }

    // Test replacing a device with channels by another
    // and check that the correct changes are made to the model and recorded
    [TestMethod]
    public async Task TestReplaceDeviceWithChannels()
    {
        await LoadModels("Changes1");

        var replacementDeviceId = new InsteonID("11.22.33");
        await house.AddNewDevice(replacementDeviceId,
            categoryId: DeviceKind.CategoryId.DimmableLightingControl,
            subCategory: 0x41, /*keypad dimmer*/
            revision: 72,
            operatingFlags: 0x08 /*8 channels*/);

        var deviceId = new InsteonID("AA.44.44");
        house.Devices.GetDeviceByID(deviceId)!.TryReplaceDeviceBy(replacementDeviceId);

        await PlayAndCheck();
    }

    // Test replacing a device by another
    // and check that the correct changes are made to the model and recorded
    [TestMethod]
    public async Task TestCopyDeviceWithChannels()
    {
        await LoadModels("Changes1");

        var copyDeviceId = new InsteonID("11.22.33");
        await house.AddNewDevice(copyDeviceId,
            categoryId: DeviceKind.CategoryId.DimmableLightingControl,
            subCategory: 0x41, /*keypad dimmer*/
            revision: 72,
            operatingFlags: 0x08 /*8 channels*/);

        var deviceId = new InsteonID("AA.BB.BB");
        house.Devices.GetDeviceByID(deviceId)!.TryCopyDeviceTo(copyDeviceId);

        await PlayAndCheck();
    }

    [TestMethod]
    public async Task TestAddDuplicateRecordToDatabase()
    {
        await LoadModels("Changes1");

        var device = house.Devices.GetDeviceByID(new InsteonID("AA.22.22"))!;
        device.AllLinkDatabase.AddRecord(new AllLinkRecord(device.AllLinkDatabase[4]));

        device = house.Devices.GetDeviceByID(new InsteonID("AA.44.44"))!;
        device.AllLinkDatabase.AddRecord(new AllLinkRecord(device.AllLinkDatabase[7]));
        device.AllLinkDatabase.AddRecord(new AllLinkRecord(device.AllLinkDatabase[11]));

        await PlayAndCheck();
    }

    [TestMethod]
    public async Task TestRemoveDuplicateDatabaseRecords()
    {
        await LoadModels("Changes1");

        var device = house.Devices.GetDeviceByID(new InsteonID("AA.11.11"))!;
        device.AllLinkDatabase.RemoveDuplicateRecords();

        device = house.Devices.GetDeviceByID(new InsteonID("BB.66.66"))!;
        device.AllLinkDatabase.RemoveDuplicateRecords();

        await PlayAndCheck();
    }

    [TestMethod]
    public async Task TestRemoveDuplicateSceneMember()
    {
        await LoadModels("Changes1");

        var scene = house.Scenes.GetSceneById(20)!;
        scene.RemoveDuplicateMembers();

        await PlayAndCheck();
    }

    [TestMethod]
    public async Task TestAddScene()
    {
        await LoadModels("Changes1");

        var scene = house.Scenes.AddNewScene("New Kitchen");

        var member = new SceneMember(scene)
        {
            DeviceId = new InsteonID("AA.11.11"),
            Group = 2,
            IsController = true,
            IsResponder = true,
            Data1 = 128,
            Data2 = 28,
            Data3 = 2,
        };
        scene.AddMember(member);

        member = new SceneMember(scene)
        {
            DeviceId = new InsteonID("AA.22.22"),
            Group = 1,
            IsController = false,
            IsResponder = true,
            Data1 = 36,
            Data2 = 28,
            Data3 = 1,
        };
        scene.AddMember(member);

        member = new SceneMember(scene)
        {
            DeviceId = new InsteonID("AA.33.33"),
            Group = 1,
            IsController = false,
            IsResponder = true,
            Data1 = 75,
            Data2 = 28,
            Data3 = 1
    };
        scene.AddMember(member);

        member = new SceneMember(scene)
        {
            DeviceId = new InsteonID("AA.44.44"),
            Group = 2,
            IsController = true,
            IsResponder = true,
            Data1 = 128,
            Data2 = 28,
            Data3 = 2
        };
        scene.AddMember(member);

        member = new SceneMember(scene)
        {
            DeviceId = new InsteonID("AA.44.44"),
            Group = 1,
            IsController = false,
            IsResponder = true,
            Data1 = 0,
            Data2 = 28,
            Data3 = 1,
        };
        scene.AddMember(member);

        member = new SceneMember(scene)
        {
            DeviceId = new InsteonID("AA.55.55"),
            Group = 4,
            IsController = false,
            IsResponder = true,
            Data1 = 0,
            Data2 = 28,
            Data3 = 4,
        };
        scene.AddMember(member);

        member = new SceneMember(scene)
        {
            DeviceId = new InsteonID("AA.66.66"),
            Group = 3,
            IsController = true,
            IsResponder = false,
            Data1 = 128,
            Data2 = 28,
            Data3 = 3
        };
        scene.AddMember(member);

        await PlayAndCheck();
    }

    [TestMethod]
    public async Task TestRemoveScene()
    {
        await LoadModels("Changes1");

        house.Scenes.RemoveSceneById(3);

        await PlayAndCheck();
    }

// Test changing the channel list of a device 
// (Used in particular when descovering device from physcial network)
[TestMethod]
    public async Task TestChangeChannels()
    {
        await LoadModels("Changes2");

        // Simulate discovering devices from the network
        List<Device> devicesSpec = new List<Device>()
        {
            new(house.Devices, new InsteonID("AA.11.11")) { CategoryId = DeviceKind.CategoryId.DimmableLightingControl, SubCategory = 65, DisplayName = "Keypad 8, channels" },
            new(house.Devices, new InsteonID("AA.22.22")) { CategoryId = DeviceKind.CategoryId.DimmableLightingControl, SubCategory = 32, DisplayName = "Switchlinc Dimmer" },
            new(house.Devices, new InsteonID("AA.33.33")) { CategoryId = DeviceKind.CategoryId.DimmableLightingControl, SubCategory = 32, DisplayName = "Switchlinc Dimmer" },
            new(house.Devices, new InsteonID("AA.55.55")) { CategoryId = DeviceKind.CategoryId.DimmableLightingControl, SubCategory = 66, DisplayName = "Keypad 6, channels" },
            new(house.Devices, new InsteonID("AA.77.77")) { CategoryId = DeviceKind.CategoryId.SensorAndAcuators, SubCategory = 0, DisplayName = "IOLinc" },
            new(house.Devices, new InsteonID("AA.FF.FF")) { CategoryId = DeviceKind.CategoryId.DimmableLightingControl, SubCategory = 33, DisplayName = "OutletLinc" },
            new(house.Devices, new InsteonID("AA.CC.CC")) { CategoryId = DeviceKind.CategoryId.SwitchedLightingControl, SubCategory = 42, DisplayName = "Switchlinc Relay" },
            new(house.Devices, new InsteonID("BB.44.44")) { CategoryId = DeviceKind.CategoryId.GeneralizedControllers, SubCategory = 16, DisplayName = "Mini-Remote, 4 channels" },
        };

        foreach (var deviceSpec in devicesSpec)
        {
            var device = new Device(house.Devices, deviceSpec.Id)
                .AddObserver(house.ModelObserver);
            house.Devices.Add(device);

            // Simulate acquiring Product Data from the physical device
            device.CategoryId = deviceSpec.CategoryId;
            device.SubCategory = deviceSpec.SubCategory;
            device.DisplayName = deviceSpec.DisplayName;
        }

        foreach (var device in house.Devices)
        {
            device.EnsureChannels();
        }

        await PlayAndCheck();
    }

    // Takes a model and keep only devices that have an id of the form XX.XX.XX and scenes containing them
    // Purge all other devices and scenes
    [TestMethod]
    public async Task TestPrepareModel()
    {
        try
        {
            await LoadModels("Houselinc.xml");
        }
        catch (Exception)
        {
            // This test requires a private model file that is not in the repository
            return;
        }

        // Build the list of devices to remove
        var deviceIdsToRemove = new List<InsteonID>();
        foreach (var device in house.Devices)
        {
            if (!device.Id.IsXXYYZZ())
            {
                deviceIdsToRemove.Add(device.Id);
            }
        }

        // Remove devices
        var mockPhysicalIM = new MockPhysicalIM(house.Hub!.Id, new AllLinkDatabase(house.Hub!.AllLinkDatabase));
        house.WithMockPhysicalDevice(mockPhysicalIM);

        foreach (var deviceId in deviceIdsToRemove)
        {
            house.Devices.RemoveDevice(deviceId);
        }

        // Remove any stale links in the devices (including the hub)
        foreach (var device in house.Devices)
        {
            device.RemoveStaleLinks();
        }

        // Remove stale scene members
        foreach (var scene in house.Scenes)
        {
            scene.RemoveStaleMembers();
        }

        // Remove scenes that are now (almost) empty
        house.Scenes.RemoveEmptyScenes(minControllerCount: 2, minResponderCount: 3);

        // Renumber the scenes and the associated links
        house.Scenes.RenumberScenes();

        // Compress all link databases, i.g., remove deleted links
        foreach (var device in house.Devices)
        {
            device.AllLinkDatabase.Compress();
        }

        await ModelHolderForTest.SaveToFile("Changes", TestContext.TestName!, house);
    }
}

public static class ScenesExtensions
{
    //  Remove scenes that have too few members
    public static void RemoveEmptyScenes(this Scenes scenes, int minControllerCount, int minResponderCount)
    {
        List<Scene> scenesToRemove = new List<Scene>();
        foreach (Scene scene in scenes)
        {
            var countOfResponderMembers = scene.Members.Count(member => member.IsResponder);
            var countOfControllerMembers = scene.Members.Count(member => member.IsController);
            if (countOfResponderMembers < minResponderCount || countOfControllerMembers < minControllerCount)
            {
                scenesToRemove.Add(scene);
            }
        }

        foreach (var scene in scenesToRemove)
        {
            scenes.RemoveScene(scene);
        }
    }

    /// <summary>
    /// Renumber all scenes, changing their Ids to be consecutive starting at 1
    /// </summary>
    public static void RenumberScenes(this Scenes scenes)
    {
        // Rebuild the scene list
        int nextSceneId = 1;
        var newScenes = new Scenes(scenes.House);
        foreach (Scene scene in scenes)
        {
            newScenes.Add(new Scene(scene, nextSceneId));

            // Update all links that reference this scene with SceneId
            foreach (Device device in scenes.House.Devices)
            {
                for (var i = 0; i < device.AllLinkDatabase.Count; i++)
                {
                    var link = device.AllLinkDatabase[i];
                    if (link.SceneId == scene.Id)
                    {
                        device.AllLinkDatabase.SetRecordAt(i, new AllLinkRecord(link) { SceneId = nextSceneId, SyncStatus = link.SyncStatus });
                    }
                }
            }

            nextSceneId++;
        }
        newScenes.NextSceneID = nextSceneId;
        scenes.House.Scenes = newScenes;
    }
}
