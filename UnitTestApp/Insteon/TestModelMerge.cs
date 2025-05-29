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
        targetHouse = null!;
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
        await targetHouse.AddNewDevice(new InsteonID("44.55.66"),
            categoryId: DeviceKind.CategoryId.DimmableLightingControl,
            subCategory: 0x41, /*keypad dimmer*/
            revision: 72,
            operatingFlags: 0x08, /*8 channels*/
            name:"New device added remotly");

        // Merge local and others and check
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
        await targetHouse.AddNewDevice(new InsteonID("11.22.33"),
            categoryId: DeviceKind.CategoryId.DimmableLightingControl,
            subCategory: 0x41, /*keypad dimmer*/
            revision: 72,
            operatingFlags: 0x08, /*8 channels*/
            name: "New device added remotly");

        // Merge local and others and check
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
        await targetHouse.RemoveDevice(new InsteonID("BB.22.22"));

        // Merge local and others and check
        await PlayMergeAndCheck();
    }

    [TestMethod]
    public async Task TestRemoveDeviceLocalRemoveAnotherRemote()
    {
        await LoadModels("Changes1");

        // Add a device to the working (local) house model
        await house.RemoveDevice(new InsteonID("BB.11.11"));

        // Add delete another device from the target model (remote changes)
        await targetHouse.RemoveDevice(new InsteonID("BB.33.33"));

        // Merge local and others and check
        await PlayMergeAndCheck();
    }

    [TestMethod]
    public async Task TestRemoveDeviceLocalRemoveSameRemote()
    {
        await LoadModels("Changes1");

        // Add a device to the working (local) house model
        await house.RemoveDevice(new InsteonID("BB.44.44"));

        // Add delete another device from the target model (remote changes)
        await targetHouse.RemoveDevice(new InsteonID("BB.44.44"));

        // Merge local and others and check
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

        var deviceRemote = targetHouse.GetDeviceByID(new InsteonID("BB.11.11"));
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

        // Merge local and others and check
        await PlayMergeAndCheck();
    }
}
