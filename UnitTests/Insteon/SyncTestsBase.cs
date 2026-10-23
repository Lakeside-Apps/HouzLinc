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
using Insteon.Model;
using Insteon.Synchronization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitTests.Infrastructure;

namespace UnitTests.Insteon;

/// <summary>
/// Base class for tests of the background synchronization of the model with the physical devices.
/// Every device of the test house is backed by a mock physical device, so no command reaches a real hub.
/// The Insteon scheduler runs on a TestDispatcher: jobs only run when a test pumps it.
/// </summary>
public abstract class SyncTestsBase
{
    private protected TestDispatcher dispatcher = null!;
    private protected House house = null!;

    [TestInitialize]
    public async Task InitSyncTest()
    {
        // Fresh scheduler on a fresh simulated event loop for each test,
        // so that no job or timer leaks from one test to the next
        dispatcher = new TestDispatcher();
        InsteonScheduler.Instance = new InsteonScheduler { Dispatcher = dispatcher };

        // Commands retry with real-time waits otherwise
        Command.WaitBeforeRetryAfterError = TimeSpan.Zero;

        TestLog.Instance.Clear();

        house = (await ModelHolderForTest.LoadFromFile("Changes", "Changes1"))!;
        Assert.IsNotNull(house);
        KeepOnlyMockedHub();
    }

    public TestContext TestContext { get; set; } = null!;

    [TestCleanup]
    public void CleanupSyncTest()
    {
        // Show the app log of failing tests, e.g., in Test Explorer test details
        if (TestContext.CurrentTestOutcome != UnitTestOutcome.Passed)
        {
            TestContext.WriteLine("App log (last 200 lines):");
            foreach (var line in TestLog.Instance.Lines.TakeLast(200))
            {
                TestContext.WriteLine(line);
            }
        }

        InsteonScheduler.Instance = new InsteonScheduler { Dispatcher = TestAssemblySetup.InsteonDispatcher };
    }

    /// <summary>
    /// Add a SwitchLinc dimmer backed by a mock physical device, connected and with its properties read
    /// </summary>
    private protected async Task<(Device device, MockPhysicalDevice mock)> AddDimmer(string id)
    {
        var deviceId = new InsteonID(id);
        var allLinkDatabase = new AllLinkDatabase();
        allLinkDatabase.AddRecord(AllLinkRecord.CreateHighWaterMark());
        var mock = new MockPhysicalDevice(deviceId, allLinkDatabase)
        {
            CategoryId = DeviceKind.CategoryId.DimmableLightingControl,
            SubCategory = 0x01,     // SwitchLinc dimmer
            Revision = 63,
            EngineVersion = 2,
            OnLevel = 255,
            RampRate = 28,
            LEDBrightness = 64,
        };
        house.WithMockPhysicalDevice(mock);

        await house.Devices.AddOrConnectDeviceAsync(deviceId);
        var device = house.Devices.GetDeviceByID(deviceId)!;
        Assert.IsNotNull(device);
        Assert.IsTrue(await device.TryReadDevicePropertiesAsync(forceSync: true));
        Assert.IsTrue(await device.TryReadAllLinkDatabaseAsync());
        return (device, mock);
    }

    /// <summary>
    /// Mark everything in sync and start background sync the way the app does
    /// (see Holder and SettingsViewModel in the ViewModel project, which tests can't reference):
    /// a sync pass is requested when the model needs sync, and requested again if a pass fails.
    /// </summary>
    private protected void StartSync()
    {
        house.PretendSync();
        house.ModelObserver.OnModelNeedsSync += () => SetNeedsSync(true);
        house.StartBackgroundDeviceSync();
        dispatcher.RunUntilIdle();
    }

    // Mirrors Holder.DoesHouseNeedSync and SettingsViewModel auto-sync without the Sync button
    private void SetNeedsSync(bool value)
    {
        if (value != needsSync)
        {
            needsSync = value;
            if (needsSync)
            {
                // Mirrors Holder.SyncHouse
                house.Devices.SyncDevicesNow(success =>
                {
                    if (!success)
                        SetNeedsSync(true);
                });
                SetNeedsSync(false);
            }
        }
    }
    private bool needsSync;

    // Keep only the hub, backed by a mock IM mirroring its current state. Tests add the devices they need with
    // AddDimmer: not every command has a mock implementation yet (e.g., KeypadLinc channels, RemoteLinc flags).
    private void KeepOnlyMockedHub()
    {
        var hub = house.Hub!;
        Assert.IsNotNull(hub);
        foreach (var device in house.Devices.Where(d => d.Id != hub.Id).ToList())
        {
            house.Devices.Remove(device);
        }

        house.WithMockPhysicalDevice(new MockPhysicalIM(hub.Id, hub, new AllLinkDatabase(hub.AllLinkDatabase)));
    }
}
