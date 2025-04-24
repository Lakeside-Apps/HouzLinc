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
using Insteon.Mock;
using Insteon.Model;

namespace Insteon.Drivers;


/// <summary>
/// This is a base class for the drivers of the physical devices on the network.
/// It caches common properties and defines an abstract interface implemented by 
/// derived classes for each specific types of physical devices.
/// </summary>
public abstract class DeviceDriverBase : DeviceBase
{
    public DeviceDriverBase(House house, InsteonID Id) : base(Id)
    {
        House = house;
    }

    public DeviceDriverBase(Device device) : base(device.Id, productData: device)
    {
        House = device.House;
        OperatingFlags = device.OperatingFlags;
    }

    // House this physical device belongs to
    internal readonly House House;

    /// <summary>
    /// For testing purposes.
    /// returns the mock physical device associated with this device, if any.
    /// </summary>
    internal MockPhysicalDevice? MockPhysicalDevice => House.GetMockPhysicalDevice(Id);

    /// <summary>
    /// Number of channels on this device
    /// </summary>
    internal abstract int ChannelCount { get; }

    /// <summary>
    /// Id of the first channel (0 or 1)
    /// </summary>
    internal abstract int FirstChannelId { get; }

    /// <summary>
    /// Get channel default names
    /// </summary>
    internal abstract string? GetChannelDefaultName (int channelId);

    /// <summary>
    /// Whether this device has channel properties
    /// </summary>
    internal virtual bool HasChannelProperties => false;

    /// <summary>
    /// Ping this device to check whether that device is connected to the hub.
    /// If the device needs to be kept awake for sync, this method should set that up.
    /// </summary>
    /// <returns></returns>
    internal abstract Task<Device.ConnectionStatus> TryPingAsync(int maxAttempts = 1);

    /// <summary>
    /// Try cleaning up after sync, e.g., letting a remotelinc go back to sleep
    /// </summary>
    /// <returns></returns>
    internal abstract Task<bool> TryCleanUpAfterSyncAsync();

    /// <summary>
    /// If this is a light controlling device, turns this device load on or off
    /// </summary>
    /// <returns></returns>
    internal abstract Task<bool> TryLightOn(double level);
    internal abstract Task<bool> TryLightOff();

    /// <summary>
    /// Return the current on-level of the device load
    /// </summary>
    /// <returns></returns>
    internal abstract Task<double> TryGetLightOnLevel();

    /// <summary>
    /// Whether the physical device has operating flags
    /// </summary>
    internal virtual bool HasOperatingFlags => true;

    /// <summary>
    /// Read device operating flags from the physical device
    /// </summary>
    /// <returns>success</returns>
    internal abstract Task<bool> TryReadOperatingFlagsAsync(bool force = false);

    /// <summary>
    /// Try to write Operating Flags to the physical device
    /// </summary>
    /// <param name="operatingFlags">Operating flags to write</param>
    /// <param name="opFlags2">Operating flags to write, second byte</param>
    /// <returns>success</returns>
    internal abstract Task<bool> TryWriteOperatingFlagsAsync(Bits operatingFlags, Bits opFlags2);

    /// <summary>
    /// Whether the physical device has LED Brightness
    /// </summary>
    internal virtual bool HasOtherProperties => true;

    /// <summary>
    /// Try to read overall LED brightness from the device
    /// </summary>
    /// <returns></returns>
    internal abstract Task<bool> TryReadLEDBrightnessAsync();

    /// <summary>
    /// Try to write the overall LED brightness to the device
    /// </summary>
    /// <param name="brightness">Desired brightness (0-255)</param>
    /// <returns>true on success</returns>
    internal abstract Task<bool> TryWriteLEDBrightnessAsync(byte brightness);

    /// <summary>
    /// Try to read OnLevel from the device.
    /// This is the level at which the load will be set the next time the device is activated.
    /// For multi-channel devices, this returns value for channel/group 1
    /// </summary>
    /// <returns>true on success</returns>
    internal abstract Task<bool> TryReadOnLevelAsync();

    /// <summary>
    /// Try to write the OnLevel to the device. 
    /// This is the level at which the load will be set the next time the device is activated.
    /// For multi-channel devices, this writes to Channel/Group 1
    /// </summary>
    /// <param name="onLevel">Desired OnLevel (0-255)</param>
    /// <returns>true on success</returns>
    internal abstract Task<bool> TryWriteOnLevelAsync(byte onLevel);

    /// <summary>
    /// Try to read RampRate from the device
    /// This is the ramp used to turn on or off the load when the device is activated/deactivated.
    /// For multi-channel devices, this returns the value from channel/group 1.
    /// </summary>
    /// <returns>true on success</returns>
    internal abstract Task<bool> TryReadRampRateAsync();

    /// <summary>
    /// Try to write RampRate to the device
    /// This is the ramp used to turn on or off the load when the device is activated/deactivated.
    /// For multi-channel devices, this writes to channel/group 1
    /// </summary>
    /// <param name="rampRate">Desired Ramp Rate</param>
    /// <returns>true on success</returns>
    internal abstract Task<bool> TryWriteRampRateAsync(byte rampRate);

    /// <summary>
    /// Read channel properties for a KeypadLinc device
    /// </summary>
    /// <returns>success</returns>
    internal abstract Task<bool> TryReadChannelPropertiesAsync(int channelId, bool force = false);

    /// <summary>
    /// Try to read yet unread all-link database records from the physical device and merge them in the passed database,
    /// setting their status (changed, synced, unknown) for a subsequent write pass. This method will read records
    /// until the end of the database or an unrecoved error.
    /// If possible, this method should read only records that have not been read yet by a preceding call unless
    /// the database is known to have changed from under us.
    /// </summary>
    /// <param name="allLinkDatabase">the database to merge into</param>
    /// <param name="force">Read from start of the database, regardless of what was read before</param>
    /// <returns>success</returns>
    internal abstract Task<bool> TryReadAllLinkDatabaseAsync(AllLinkDatabase allLinkDatabase, bool force = false);

    /// <summary>
    /// Try to read next unread all-link database record from the physical device and merge it 
    /// in the passed database, setting its status (changed, synced, unknown) for a subsequent write pass.
    /// If possible this method should read only records that have not been read yet by a preceding call unless 
    /// the database is known to have changed from under us.
    /// Usage: call this method first with restart = true to read the first record in the database, then call 
    /// it repeatedly with restart = false to read subsequent records, until "done" is true in the returned tuple,
    /// indicating that the whole database has been read or an unrecovered error has occured.
    /// Some devices may convert this to a full read of the database and only return when done.
    /// </summary>
    /// <param name="allLinkDatabase">Database to merge into</param>
    /// <param name="restart">Read the first link in the database if set</param>
    /// <param name="force">Read from start of the database, regardless of what was read before</param>
    /// <returns></returns>
    internal abstract Task<(bool success, bool done)> TryReadAllLinkDatabaseStepAsync(AllLinkDatabase allLinkDatabase, bool restart, bool force = false);

    /// <summary>
    /// Write not yet synced records in the All-link database to the physical device.
    /// This method should be able to be called repeatedly and only write what needs to be written each time.
    /// For example, writing to the database could be interrupted by the user closing the app and should resume
    /// when the app is restarted.
    /// </summary>
    /// <param name="forceRead">Force reading the database from the physical device prior to writing to it</param>
    /// <param name="allLinkDatabaseToWrite">the new database to write</param>
    /// <returns>true if success. On failure, part of the database might have been rewritten already</returns>
    internal abstract Task<bool> TryWriteAllLinkDatabaseAsync(AllLinkDatabase allLinkDatabaseToWrite, bool forceRead);

    /// <summary>
    /// Send an cmd via AllLink to all responders in the specificed group (a.k.a., channel)
    /// Command can be any of the direct commands (e.g., LightOn, LightOff, etc.)
    /// </summary>
    /// <param name="group">Group to target</param>
    /// <param name="command">Direct command code to send</param>
    /// <param name="level">Parameter for that command</param>
    /// <returns></returns>
    internal abstract Task<bool> TrySendAllLinkCommandToGroup(int group, byte command, double level);
}
