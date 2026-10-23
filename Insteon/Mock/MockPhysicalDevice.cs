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
using Insteon.Commands;
using Insteon.Model;

namespace Insteon.Mock;

/// <summary>
/// Simulates a real physical device for testing purposes. 
/// Commands can take a MockPhysicalDevice as a parameter and will be simulated on a mock device.
/// </summary>
public class MockPhysicalDevice : DeviceBase
{
    public MockPhysicalDevice(InsteonID Id) : base(Id)
    {
        this.AllLinkDatabase = new AllLinkDatabase();
    }

    public MockPhysicalDevice(InsteonID Id, AllLinkDatabase allLinkDatabase) : base(Id)
    {
        this.AllLinkDatabase = allLinkDatabase;
    }

    public MockPhysicalDevice(InsteonID Id, ProductData productData, AllLinkDatabase allLinkDatabase) : base(Id)
    {
        CopyFrom(productData);
        this.AllLinkDatabase = allLinkDatabase;
    }

    internal AllLinkDatabase AllLinkDatabase;

    /// <summary>
    /// Names of the commands this device received, in order, to let tests check network traffic.
    /// Includes commands that failed, as they were still sent on the network.
    /// </summary>
    public List<string> ReceivedCommands { get; } = new();

    /// <summary>
    /// Whether the device responds. When false, every command sent to it fails with FailureReason.
    /// </summary>
    public bool IsReachable { get; set; } = true;

    /// <summary>
    /// Number of upcoming commands that fail with FailureReason, e.g., to simulate messages lost on the network
    /// </summary>
    public int FailNextCommands { get; set; }

    /// <summary>
    /// Error reported by failing commands
    /// </summary>
    public Command.ErrorReasons FailureReason { get; set; } = Command.ErrorReasons.NoDeviceResponse;

    /// <summary>
    /// When true, the next command sent to this device throws, to simulate unexpected errors
    /// </summary>
    public bool ThrowOnNextCommand { get; set; }

    // Called by commands sent to this device. Returns the error to simulate, or NoError to respond normally.
    internal Command.ErrorReasons ReceiveCommand(string commandName)
    {
        ReceivedCommands.Add(commandName);

        if (ThrowOnNextCommand)
        {
            ThrowOnNextCommand = false;
            throw new InvalidOperationException($"Simulated exception in command {commandName}");
        }

        if (!IsReachable)
        {
            return FailureReason;
        }

        if (FailNextCommands > 0)
        {
            FailNextCommands--;
            return FailureReason;
        }

        return Command.ErrorReasons.NoError;
    }
}
