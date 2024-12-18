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
using Insteon.Commands;
using Insteon.Model;
using Common;
using System.Diagnostics;

namespace Insteon.Drivers;

// Driver of a physical multi-button keypad device on the network.
// It caches some of the state and provides facilities to read and write the physical
// keypad specific state
internal sealed class KeypadLincDriver : DeviceDriver
{
    internal KeypadLincDriver(House house, InsteonID Id) : base(house, Id)
    {
    }

    internal KeypadLincDriver(Device device) : base(device)
    {
    }

    /// <summary>
    /// Number of channels for device with channels
    /// TODO: Consider moving to derived class
    /// </summary>
    internal bool Is8ButtonKeypad
    {
        get => OperatingFlags[3];
        set 
        {
            Bits flags = OperatingFlags;
            flags[3] = value;
            OperatingFlags = flags;
        }
    }

    /// <summary>
    /// Channel count
    /// Channel count depends on operating flags, bit 3, which appears to override product data.
    /// Note that the OperatingFlags get populated in the DeviceDriverBase constructor
    /// so it's ok to call this before the OperatingFlags are read from the physical device.
    /// TODO: verify that all KeypadLincs expose OperatingFlags bit 3 as the 6/8 button flag,
    /// if not, we should use product data for those who don't.
    /// </summary>
    internal override int ChannelCount => Is8ButtonKeypad ? 8 : 6;

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
        if (Is8ButtonKeypad)
        {
            switch (channelId)
            {
                case 1:
                    return "MAIN On/Off";
                case 2:
                    return "B";
                case 3:
                    return "C";
                case 4:
                    return "D";
                case 5:
                    return "E";
                case 6:
                    return "F";
                case 7:
                    return "G";
                case 8:
                    return "H";
            }
        }
        else
        {
            switch (channelId)
            {
                case 1:
                    return "MAIN On";
                case 2:
                    return "B";
                case 3:
                    return "C";
                case 4:
                    return "D";
                case 5:
                    return "E";
                case 6:
                    return "MAIN off";
            }
        }
        return string.Empty;
    }

    /// <summary>
    /// KeypadLinc have channel properties
    /// </summary>
    internal override bool HasChannelProperties => true;

    /// <summary>
    /// For a keypad, write ToggleMode Masks to device
    /// The Non Togge Mask governs which buttons are Toggle, always On or always Off
    /// (See command classes for details)
    /// </summary>
    /// <param name="nonToggleMask">which buttons are always on/off vs. toggle</param>
    /// <param name="onOffMask">for buttons with nonToggleMask bit set, which are always on vs. always off</param>
    /// <returns>true if success</returns>
    internal async Task<bool> TryWriteToggleModeMask(byte nonToggleMask, byte onOffMask)
    {
        bool success = true;

        if (!await new SetNonToggleMaskCommand(House.Gateway, Id, nonToggleMask).TryRunAsync())
        {
            success = false;
        }

        if (!await new SetOnOffBitMaskCommand(House.Gateway, Id, onOffMask).TryRunAsync())
        {
            success = false;
        }

        if (success && Channels != null)
        {
            for (int i = 0; i < ChannelCount; i++)
            {
                if (Channels[i] != null)
                {
                    Channels[i].ToggleMode = (nonToggleMask & (1 << i)) != 0 ? ((onOffMask & (1 << i)) != 0 ? ToggleMode.On : ToggleMode.Off) : ToggleMode.Toggle;
                }
            }
        }

        return success;
    }

    /// <summary>
    /// Write bit mask controlling on/off state of each button
    /// </summary>
    /// <param name="ledOnBitMask">LED on bit mask for each button</param>
    /// <returns></returns>
    internal async Task<bool> TryWriteLEDOnMask(byte ledOnBitMask)
    {
        bool success = true;

        var command = new SetLEDBitMaskCommand(House.Gateway, Id, ledOnBitMask);
        if (!await command.TryRunAsync())
        {
            success = false;
        }

        if (success && Channels != null)
        {
            for (int i = 0; i < ChannelCount; i++)
            {
                if (Channels[i] != null)
                {
                    Channels[i].LEDOn = (ledOnBitMask & (1 << i)) != 0 ? true : false;
                }
            }
        }

        return success;

    }

    /// <summary>
    /// Read device operating flags
    /// </summary>
    /// <param name="force">whether force a new read from the network even if we already read previously</param>
    /// <returns></returns>
    internal async override Task<bool> TryReadOperatingFlagsAsync(bool force = false)
    {
        bool success = true;

        if (!AreOperatingFlagsRead || force)
        {
            success = false;
            if (await base.TryReadOperatingFlagsAsync(force))
            {
                AreOperatingFlagsRead = false;
                var command2 = new GetOpFlags2Command(House.Gateway, Id) { MockPhysicalDevice = House.GetMockPhysicalDevice(Id) };
                if (await command2.TryRunAsync())
                {
                    OpFlags2 = command2.OpFlags2;
                    success = true;
                    AreOperatingFlagsRead = true;
                }
            }
        }

        return success;
    }

    /// <summary>
    /// Try to write Operating Flags to the device, if different.
    /// </summary>
    /// <param name="operatingFlags">Operating flags to write</param>
    /// <param name="opFlags2">Operating flags to write, second byte</param>
    /// <returns>true on success</returns>
    internal async override Task<bool> TryWriteOperatingFlagsAsync(Bits operatingFlags, Bits opFlags2)
    {
        bool success = await base.TryWriteOperatingFlagsAsync(operatingFlags, opFlags2);

        if (AreOperatingFlagsRead)
        {
            success = true;

            // Write the flags that are different
            // TODO: For now, only write the Detached Load flag (5)
            Bits writeMask = 0x40;
            for (int i = 0; i < 8; i++)
            {
                if (writeMask[i] && opFlags2[i] != OpFlags2[i])
                {
                    var command = new SetOpFlag2Command(House.Gateway, Id, (byte)(opFlags2[i] ? (i + 8) * 2 : (i + 8) * 2 + 1));
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
            OpFlags2 = opFlags2;
        }

        return success;
    }


}
