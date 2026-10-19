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
public sealed class TestModelMerge : ModelTestsBase
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
        remoteHouse = null!;
    }

    [TestMethod]
    public async Task TestAddDeviceLocalAddAnotherRemote()
    {
        await LoadModels("Changes1");

        // Add a device to the working (local) house model
        await house.AddNewDevice(new InsteonID("11.22.33"),
            categoryId: DeviceKind.CategoryId.DimmableLightingControl,
            subCategory: 0x41, /*keypad dimmer*/
            revision: 72,
            operatingFlags: 0x08, /*8 channels*/
            name:"New device");

        // Add another device to the target model (containing remote changes)
        await remoteHouse.AddNewDevice(new InsteonID("44.55.66"),
            categoryId: DeviceKind.CategoryId.DimmableLightingControl,
            subCategory: 0x41, /*keypad dimmer*/
            revision: 72,
            operatingFlags: 0x08, /*8 channels*/
            name:"New device added remotly");

        // Merge local and remote and check
        await PlayMergeAndCheck();
    }

    [TestMethod]
    public async Task TestAddDeviceLocalAddSameRemote()
    {
        await LoadModels("Changes1");

        // Add a device to the working (local) house model
        await house.AddNewDevice(new InsteonID("11.22.33"),
            categoryId: DeviceKind.CategoryId.DimmableLightingControl,
            subCategory: 0x41, /*keypad dimmer*/
            revision: 72,
            operatingFlags: 0x08, /*8 channels*/
            name: "New device");

        // Add another device to the target model (remote changes)
        await remoteHouse.AddNewDevice(new InsteonID("11.22.33"),
            categoryId: DeviceKind.CategoryId.DimmableLightingControl,
            subCategory: 0x41, /*keypad dimmer*/
            revision: 72,
            operatingFlags: 0x08, /*8 channels*/
            name: "New device added remotly");

        // Merge local and remote and check
        await PlayMergeAndCheck();
    }

    [TestMethod]
    public async Task TestAddDeviceLocalRemoveOneRemote()
    {
        await LoadModels("Changes1");

        // Add a device to the working (local) house model
        await house.AddNewDevice(new InsteonID("11.22.33"),
            categoryId: DeviceKind.CategoryId.DimmableLightingControl,
            subCategory: 0x41, /*keypad dimmer*/
            revision: 72,
            operatingFlags: 0x08, /*8 channels*/
            name: "New device");

        // Add delete another device from the target model (remote changes)
        await remoteHouse.RemoveDevice(new InsteonID("BB.22.22"));

        // Merge local and remote and check
        await PlayMergeAndCheck();
    }

    [TestMethod]
    public async Task TestRemoveDeviceLocalRemoveAnotherRemote()
    {
        await LoadModels("Changes1");

        // Add a device to the working (local) house model
        await house.RemoveDevice(new InsteonID("BB.11.11"));

        // Add delete another device from the target model (remote changes)
        await remoteHouse.RemoveDevice(new InsteonID("BB.33.33"));

        // Merge local and remote and check
        await PlayMergeAndCheck();
    }

    [TestMethod]
    public async Task TestRemoveDeviceLocalRemoveSameRemote()
    {
        await LoadModels("Changes1");

        // Add a device to the working (local) house model
        await house.RemoveDevice(new InsteonID("BB.44.44"));

        // Add delete another device from the target model (remote changes)
        await remoteHouse.RemoveDevice(new InsteonID("BB.44.44"));

        // Merge local and remote and check
        await PlayMergeAndCheck();
    }

    [TestMethod]
    public async Task TestChangeDevicePropertiesLocalAndRemote()
    {
        await LoadModels("Changes1");

        var deviceLocal = house.GetDeviceByID(new InsteonID("BB.11.11"));
        if (deviceLocal is null)
        {
            Assert.Fail("Device BB.11.11 not found in the local model");
        }

        var deviceRemote = remoteHouse.GetDeviceByID(new InsteonID("BB.11.11"));
        if (deviceRemote is null)
        {
            Assert.Fail("Device BB.11.11 not found in the remote model");
        }

        deviceLocal.DisplayName = "Local name";
        deviceRemote.DisplayName = "Remote name";

        deviceLocal.OperatingFlags = 0x08;
        deviceRemote.OperatingFlags = 0x00;

        deviceLocal.BeeperOn = true;
        deviceLocal.LEDBrightness = 0xFF;
        deviceRemote.NonToggleMask = 0xFF;

        deviceRemote.EngineVersion = 3;
        deviceRemote.Revision = 73;
        deviceRemote.OnLevel = 0x7F;

        // Merge local and remote and check
        await PlayMergeAndCheck();
    }

    [TestMethod]
    public async Task TestRemoveSceneControllerLocalRemoveSameSceneResponderRemote()
    {
        await LoadModels("Changes1");

        Scene scene = house.Scenes.GetSceneById(6)!;
        scene.Expand();
        scene.RemoveSceneMember(InsteonID.FromString("AA.EE.EE"), memberGroup: 8, isController: true, isResponder: false);

        Scene remoteScene = remoteHouse.Scenes.GetSceneById(6)!;
        remoteScene.Expand();
        remoteScene.RemoveSceneMember(InsteonID.FromString("AA.BB.BB"), memberGroup: 4, isController: false, isResponder: true);

        // Merge local and remote and check
        await PlayMergeAndCheck();
    }

    [TestMethod]
    public async Task TestAddSceneLocalAddAnotherRemote()
    {
        await LoadModels("Changes1");

        // Add a scene to the working (local) house model using AddNewScene
        house.Scenes.AddNewScene("Local Scene");

        // Add another scene to the target model (containing remote changes) using AddNewScene
        remoteHouse.Scenes.AddNewScene("Remote Scene");

        // Merge local and remote and check
        await PlayMergeAndCheck();
    }

    [TestMethod]
    public async Task TestAddSceneWithMembersLocalAndRemote()
    {
        await LoadModels("Changes1");

        // Add a scene to the working (local) house model and add a member
        var localScene = house.Scenes.AddNewScene("Local Scene With Member");
        localScene.AddMember(new SceneMember(localScene)
        {
            DeviceId = new InsteonID("AA.DD.DD"),
            Group = 1,
            IsController = true,
            IsResponder = true
        });

        localScene.AddMember(new SceneMember(localScene)
        {
            DeviceId = new InsteonID("AA.BB.BB"),
            Group = 3,
            IsController = true,
            IsResponder = false
        });

        localScene.AddMember(new SceneMember(localScene)
        {
            DeviceId = new InsteonID("BB.33.33"),
            Group = 1,
            IsController = true,
            IsResponder = false
        });

        localScene.Expand();

        // Add a scene to the remote house and add a different member
        var remoteScene = remoteHouse.Scenes.AddNewScene("Remote Scene With Member");
        remoteScene.AddMember(new SceneMember(remoteScene)
        {
            DeviceId = new InsteonID("BB.66.66"),
            Group = 2,
            IsController = false,
            IsResponder = true
        });

        remoteScene.AddMember(new SceneMember(remoteScene)
        {
            DeviceId = new InsteonID("AA.11.11"),
            Group = 5,
            IsController = true,
            IsResponder = true
        });

        remoteScene.Expand();

        // Merge local and remote and check
        await PlayMergeAndCheck();
    }

    [TestMethod]
    public async Task TestMakeSceneMemberController()
    {
        await LoadModels("Changes1");

        var scene = house.Scenes.GetSceneById(5);
        Assert.IsNotNull(scene, "Scene not found in starting model");

        var member = scene.Members[0];
        Assert.IsNotNull(member, "Member not found in starting model");
        Assert.IsTrue(!member.IsController, "Member is already a controller");

        // Set the IsController property of the member
        bool replaced = scene.ReplaceMember(
            member,
            new SceneMember(scene)
            {
                DeviceId = member.DeviceId,
                Group = member.Group,
                IsController = true,
                IsResponder = member.IsResponder,
                Data1 = member.Data1,
                Data2 = member.Data2,
                Data3 = member.Data3,
                Status = member.Status,
                Tag = member.Tag
            }
        );
        Assert.IsTrue(replaced, "Failed to replace scene member");

        scene.Expand();

        // Merge local and remote and check
        await PlayMergeAndCheck();
    }

    [TestMethod]
    public async Task TestMakeSceneMemberResponder()
    {
        await LoadModels("Changes1");

        var scene = house.Scenes.GetSceneById(3);
        Assert.IsNotNull(scene, "Scene not found in starting model");

        var member = scene.Members[3];
        Assert.IsNotNull(member, "Member not found in starting model");
        Assert.IsTrue(!member.IsResponder, "Member is already a responder");

        // Set the IsResponder property of the member
        bool replaced = scene.ReplaceMember(
            member,
            new SceneMember(scene)
            {
                DeviceId = member.DeviceId,
                Group = member.Group,
                IsController = member.IsController,
                IsResponder = true,
                Data1 = member.Data1,
                Data2 = member.Data2,
                Data3 = member.Data3,
                Status = member.Status,
                Tag = member.Tag
            }
        );
        Assert.IsTrue(replaced, "Failed to replace scene member");

        scene.Expand();

        // Merge local and remote and check
        await PlayMergeAndCheck();
    }

}
