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
using Insteon.Commands;

namespace Insteon.Drivers;

internal class ChannelDriver : ChannelBase
{
    // Driver of a channel of a multi-channel physical device on the network.
    // It caches some of the channel state and contains facilities to read and write the state of the channel 
    internal ChannelDriver(DeviceDriver device, int channelId)
    {
        DeviceDriver = device;
        Id = channelId;
    }

    internal DeviceDriver DeviceDriver { get; private set; }

    /// <summary>
    /// Have properties been read from the physical device into this device driver?
    /// </summary>
    internal bool ArePropertiesRead { get; set; }

    /// <summary>
    /// Read channel properties for a KeypadLinc device
    /// </summary>
    /// <returns></returns>
    internal async Task<bool> TryReadChannelProperties()
    {
        bool success = true;

        // Note that the group in the command is 1-based
        var command = new GetPropertiesForGroupCommand(DeviceDriver.House.Gateway, DeviceDriver.Id, (byte)(Id));
        if (await command.TryRunAsync())
        {
            FollowMask = command.FollowMask;
            FollowOffMask = command.FollowOffMask;
            OnLevel = command.OnLevel;
            RampRate = command.RampRate;

            // NonToggleMask, OnOffMask are returned by this command, but the same mask of 8 bits
            // is returned for all channels/groups. Each bit represent a given channel/group.
            ToggleMode = ((command.NonToggleMask & (byte)(1 << (Id - 1))) != 0) ?
                (((command.OnOffMask & (byte)(1 << (Id - 1))) != 0) ? ToggleMode.On : ToggleMode.Off) : ToggleMode.Toggle;

            // LEDOn seems to be incorrectly reported, defaulting to true
            // LEDOn = ((command.LEDOnMask & (byte)(1 << (Id - 1))) != 0) ? true : false;
            LEDOn = true;

            ArePropertiesRead = true;
        }
        else
        {
            success = false;
        }

        return success;
    }

    /// <summary>
    /// For a keypad, write Follow Mask to device channel (group)
    /// The Follow Mask governs which button follows (either on or off) when a button (channel/group) is pressed
    /// </summary>
    /// <param name="followMask">Value to write</param>
    /// <returns>true if success</returns>
    internal async Task<bool> TryWriteFollowMask(byte followMask)
    {
        bool success = false;

        var command = new SetFollowMaskForGroupCommand(DeviceDriver.House.Gateway, DeviceDriver.Id, (byte)Id, followMask);
        if (await command.TryRunAsync())
        {
            FollowMask = followMask;
            success = true;
        }

        return success;
    }

    /// <summary>
    /// For a keypad, write Follow Off Mask to device channel (group)
    /// The Follow Off Mask governs which whether a following button turns either on or off, when a button (channel/group) is pressed
    /// </summary>
    /// <param name="followOffMask">Value to write</param>
    /// <returns>true if success</returns>
    internal async Task<bool> TryWriteFollowOffMask(byte followOffMask)
    {
        bool success = false;

        var command = new SetFollowOffMaskForGroupCommand(DeviceDriver.House.Gateway, DeviceDriver.Id, (byte)Id, followOffMask);
        if (await command.TryRunAsync())
        {
            FollowOffMask = followOffMask;
            success = true;
        }
        return success;
    }

    /// <summary>
    /// For a keypad, write On Level to device channel (group)
    /// </summary>
    /// <param name="onLevel">Value to write</param>
    /// <returns>true if success</returns>
    internal async Task<bool> TryWriteOnLevel(byte onLevel)
    {
        bool success = false;

        var command = new SetOnLevelForGroupCommand(DeviceDriver.House.Gateway, DeviceDriver.Id, (byte)Id, onLevel);
        if (await command.TryRunAsync())
        {
            this.OnLevel = onLevel;
            success = true;
        }
        return success;
    }

    /// <summary>
    /// For a keypad, write Ramp Rate to device channel (group)
    /// </summary>
    /// <param name="rampRate">Value to write</param>
    /// <returns>true if success</returns>
    internal async Task<bool> TryWriteRampRate(byte rampRate)
    {
        bool success = false;

        var command = new SetRampRateForGroupCommand(DeviceDriver.House.Gateway, DeviceDriver.Id, (byte)Id, rampRate);
        if (await command.TryRunAsync())
        {
            this.RampRate = rampRate;
            success = true;
        }
        return success;
    }
}
