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

using Insteon.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Insteon;

/// <summary>
/// Tests of the background synchronization of model changes to the physical devices
/// </summary>
[TestClass]
public sealed class TestDeviceSync : SyncTestsBase
{
    [TestMethod]
    public async Task EditedProperty_IsWrittenToDevice()
    {
        var (device, mock) = await AddDimmer("11.22.33");
        StartSync();

        device.OnLevel = 100;
        dispatcher.RunUntilIdle();

        Assert.AreEqual((byte)100, mock.OnLevel);
        Assert.AreEqual(SyncStatus.Synced, device.PropertiesSyncStatus);
    }

    [TestMethod]
    public async Task UnreachableDevice_DoesNotStopSyncOfOtherDevices()
    {
        var (unreachable, unreachableMock) = await AddDimmer("11.22.33");
        var (other, otherMock) = await AddDimmer("11.22.44");
        StartSync();

        unreachableMock.IsReachable = false;
        unreachable.OnLevel = 100;
        dispatcher.AdvanceBy(TimeSpan.FromMinutes(1));
        other.OnLevel = 50;
        dispatcher.AdvanceBy(TimeSpan.FromMinutes(1));

        Assert.AreEqual((byte)50, otherMock.OnLevel);
        Assert.AreNotEqual((byte)100, unreachableMock.OnLevel);
    }

    // Edits made while a device already has pending changes used to never sync,
    // since a sync was only requested when the device status changed to "Changed"
    [TestMethod]
    public async Task EditToDeviceWithPendingChanges_IsSynced()
    {
        var (device, mock) = await AddDimmer("11.22.33");
        StartSync();

        mock.IsReachable = false;
        device.OnLevel = 100;
        dispatcher.AdvanceBy(TimeSpan.FromSeconds(30));
        Assert.AreEqual(SyncStatus.Changed, device.PropertiesSyncStatus);

        // Device is back, user makes another edit, short of any background retry
        mock.IsReachable = true;
        device.RampRate = 10;
        dispatcher.AdvanceBy(TimeSpan.FromSeconds(10));

        Assert.AreEqual((byte)100, mock.OnLevel);
        Assert.AreEqual((byte)10, mock.RampRate);
        Assert.AreEqual(SyncStatus.Synced, device.PropertiesSyncStatus);
    }

    // One lost message should not leave pending changes unsynced
    [TestMethod]
    public async Task SingleMissedPing_DoesNotPreventSync()
    {
        var (device, mock) = await AddDimmer("11.22.33");
        StartSync();

        // Force the connection status to be retrieved again, and lose the ping
        device.OnGatewayChanged();
        mock.FailNextCommands = 1;
        device.OnLevel = 100;
        dispatcher.AdvanceBy(TimeSpan.FromSeconds(10));

        Assert.AreEqual((byte)100, mock.OnLevel);
    }

    // Pending changes of a device that was unreachable get synced when it comes back, without a new edit
    [TestMethod]
    public async Task UnreachableDeviceWithPendingChanges_IsSyncedWhenBack()
    {
        var (device, mock) = await AddDimmer("11.22.33");
        StartSync();

        mock.IsReachable = false;
        device.OnLevel = 100;
        dispatcher.AdvanceBy(TimeSpan.FromMinutes(1));

        mock.IsReachable = true;
        dispatcher.AdvanceBy(TimeSpan.FromMinutes(10));

        Assert.AreEqual((byte)100, mock.OnLevel);
        Assert.AreEqual(SyncStatus.Synced, device.PropertiesSyncStatus);
    }

    // Retrying an unreachable device should not flood the slow Insteon network
    [TestMethod]
    public async Task UnreachableDevice_RetryTrafficIsBounded()
    {
        var (device, mock) = await AddDimmer("11.22.33");
        StartSync();

        mock.IsReachable = false;
        mock.ReceivedCommands.Clear();
        device.OnLevel = 100;
        dispatcher.AdvanceBy(TimeSpan.FromHours(1));

        var pings = mock.ReceivedCommands.Count(c => c == "Ping");
        TestContext.WriteLine($"Commands sent in 1 hour: {mock.ReceivedCommands.Count}, pings: {pings}");
        TestContext.WriteLine(string.Join(", ", mock.ReceivedCommands));
        Assert.IsTrue(mock.ReceivedCommands.Count <= 25, $"Too many commands sent to an unreachable device: {mock.ReceivedCommands.Count}");
    }

    // An unreachable device with changes to sync is pinged with exponential backoff, capped at 30 minutes
    [TestMethod]
    public async Task UnreachableDevice_IsRetriedWithBackoff()
    {
        var (device, mock) = await AddDimmer("11.22.33");
        StartSync();

        mock.IsReachable = false;
        device.OnLevel = 100;
        dispatcher.AdvanceBy(TimeSpan.FromSeconds(30));

        // Retries after 1, 2, 4, 8 minutes: one ping each
        foreach (var retryDelay in new[] { 1, 2, 4, 8 })
        {
            mock.ReceivedCommands.Clear();
            dispatcher.AdvanceBy(TimeSpan.FromMinutes(retryDelay));
            Assert.AreEqual(1, mock.ReceivedCommands.Count(c => c == "Ping"), $"Expected one ping for the retry after {retryDelay} min");
        }

        // Once capped at 30 minutes, two pings an hour
        dispatcher.AdvanceBy(TimeSpan.FromHours(2));
        mock.ReceivedCommands.Clear();
        dispatcher.AdvanceBy(TimeSpan.FromHours(1));
        Assert.AreEqual(2, mock.ReceivedCommands.Count(c => c == "Ping"));
    }

    // A command throwing an exception must not leave the command semaphore held,
    // which would block all further communication with the hub
    [TestMethod]
    public async Task CommandThrowing_DoesNotBlockLaterCommands()
    {
        var (device, mock) = await AddDimmer("11.22.33");

        mock.ThrowOnNextCommand = true;
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => device.TryPingAsync());

        var ping = device.TryPingAsync();
        Assert.AreSame(ping, await Task.WhenAny(ping, Task.Delay(TimeSpan.FromSeconds(5))), "Command after the exception never ran");
        Assert.AreEqual(Device.ConnectionStatus.Connected, await ping);
    }

    // A forced read requested from Tools ("Sync all devices") must not be dropped
    // when a regular sync request follows before it starts
    [TestMethod]
    public async Task ForceReadRequest_IsNotLost_WhenFollowedByRegularRequest()
    {
        var (device, mock) = await AddDimmer("11.22.33");
        StartSync();

        bool? forceReadResult = null;
        house.Devices.SyncDevicesNow(success => forceReadResult = success, forceRead: true);
        house.Devices.SyncDevicesNow();
        mock.ReceivedCommands.Clear();
        dispatcher.AdvanceBy(TimeSpan.FromMinutes(10));

        Assert.IsNotNull(forceReadResult, "Completion callback of the forced read was never called");
        Assert.IsTrue(mock.ReceivedCommands.Count > 0, "Forced read did not read the device");
    }
}
