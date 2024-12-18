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

using Insteon.Base;
using Insteon.Model;
using Common;
using Insteon.Commands;
using System.Diagnostics;

namespace Insteon.Drivers;

// Driver of a physical multi-button remote device on the network.
// It caches some of the state and provides facilities to read and write the physical
// keypad specific state
internal sealed class  RemoteLincDriver : DeviceDriver
{
    internal RemoteLincDriver(House house, InsteonID Id) : base(house, Id)
    {
    }

    internal RemoteLincDriver(Device device) : base(device)
    {
    }

    /// <summary>
    /// Remotelinc 2 does not appear to support product data request.
    /// It only publishes product data at IM linking time.
    /// </summary>
    internal override bool HasProductDataRequest => false;

    /// <summary>
    /// Channel count
    /// Note that the product information gets populated in the DeviceDriver constructor
    /// so it's ok to call this before the product information is read from the physical device.
    /// </summary>
    internal override int ChannelCount => DeviceKind.GetChannelCount(CategoryId, SubCategory);

    /// <summary>
    /// First channel on a Keypadlinc is numbered 1
    /// </summary>
    internal override int FirstChannelId => 1;

    /// <summary>
    /// Get channel default name
    /// </summary>
    /// <param name="channelId"></param>
    /// <returns></returns>
    internal override string? GetChannelDefaultName(int channelId)
    {
        if (ChannelCount == 8)
        {
            switch (channelId)
            {
                case 1:
                    return "av";
                case 2:
                    return "a^";
                case 3:
                    return "bv";
                case 4:
                    return "b^";
                case 5:
                    return "cv";
                case 6:
                    return "c^";
                case 7:
                    return "dv";
                case 8:
                    return "d^";
            }
        }
        else
        {
            switch (channelId)
            {
                case 1:
                    return "a";
                case 2:
                    return "b";
                case 3:
                    return "c";
                case 4:
                    return "d";
            }
        }
        return string.Empty;
    }

    /// <summary>
    /// Ping this remotelinc to check that it is connected to the hub
    /// by attempting to wake it up and keeping it awake.
    /// If the device is sleeping, the user should hold down a button on the device while calling this to wake the device up.
    /// The device should be allowed to go back to sleep once whatever operation that required it to be awake is complete.
    /// </summary>
    /// <returns>whether it is</returns>
    internal async override Task<Device.ConnectionStatus> TryPingAsync(int maxAttempts = 1)
    {
        var command = new SetOperatingFlagCommand(
            House.Gateway, 
            Id, 
            (byte)SetOperatingFlagCommand.CommandCodeForRemoteLinc.StayAwakeOn) { MockPhysicalDevice = MockPhysicalDevice };

        if (await command.TryRunAsync(maxAttempts: maxAttempts))
        {
            return Device.ConnectionStatus.Connected;
        }

        if (command.ErrorReason == Command.ErrorReasons.TransientHttpError)
        {
            return Device.ConnectionStatus.Unknown;
        }

        if (command.ErrorReason == Command.ErrorReasons.HttpRequestError ||
            command.ErrorReason == Command.ErrorReasons.Timeout)
        {
            return Device.ConnectionStatus.GatewayError;
        }

        return Device.ConnectionStatus.Disconnected;
    }

    /// <summary>
    /// Try to write Operating Flags to the device, if different.
    /// </summary>
    /// <param name="operatingFlags">Operating flags to write</param>
    /// <param name="opFlags2">Operating flags to write, second byte. Only used by derived classes.</param>
    /// <returns>true on success</returns>
    internal async override Task<bool> TryWriteOperatingFlagsAsync(Bits operatingFlags, Bits opFlags2)
    {
        bool success = false;

        // TODO: we need to handle IM separately with SetIMConfirmation command
        Debug.Assert(GetType() != typeof(IMDriver));

        // Ensure flags have been read into this device driver
        // so that we can just write the diffs
        await TryReadOperatingFlagsAsync(false);

        if (AreOperatingFlagsRead)
        {
            success = true;

            // Then write the flags that are different
            for (int i = 0; i < 8; i++)
            {
                if (operatingFlags[i] != OperatingFlags[i])
                {
                    byte commandCode = (byte)(i * 2 + (operatingFlags[i] ? 0 : 1));

                    // Skip StayAwakeOff which will be turn back on once we are done writing to the device
                    // (see TryCleanuUpAfterSyncAsync below)
                    if (commandCode == (byte)SetOperatingFlagCommand.CommandCodeForRemoteLinc.StayAwakeOff)
                    {
                        continue;
                    }

                    var command = new SetOperatingFlagCommand(House.Gateway, Id, commandCode);
                    if (!await command.TryRunAsync(maxAttempts: 15))
                    {
                        success = false;
                        break;
                    }
                }
            }
        }

        if (success)
        {
            OperatingFlags = operatingFlags;
        }

        return success;
    }

    /// <summary>
    /// Try cleaning up after sync.
    /// Put the device back to sleep.
    /// </summary>
    /// <returns></returns>
    internal async override Task<bool> TryCleanUpAfterSyncAsync()
    {
        var command = new SetOperatingFlagCommand(House.Gateway, Id, (byte)SetOperatingFlagCommand.CommandCodeForRemoteLinc.StayAwakeOff);
        return await command.TryRunAsync();
    }


    /// <summary>
    /// RemoteLinc2 does not have LED Brightness property
    /// </summary>
    internal override bool HasOtherProperties => false;

    /// <summary>
    /// RemoteLinc2 does not have any channel properties
    /// The Extended Set/Get (a.k.a., GetPropertyForGroup) command fails for all channels
    /// </summary>
    internal override bool HasChannelProperties => false;


}
