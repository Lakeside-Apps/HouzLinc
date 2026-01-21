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

namespace Insteon.Commands;

/// <summary>
///  Command to turn on the load
/// </summary>
public sealed class LightOnCommand : DeviceCommand
{
    public const string Name = "LightOn";
    public const string Help = "<DeviceID> <Level (0-255 or 0-100% default 255 (100%))>";
    public const byte CommandCode = CommandCode_LightON;
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return $"Level: {Command2 / 255.0:P0} ({Command2})"; }

    public LightOnCommand(Gateway gateway, InsteonID deviceID, byte level) : base(gateway, deviceID)
    {
        Command1 = (byte)CommandCode;
        Command2 = level;
    }

    private protected override void Done()
    {
        base.Done();
        LogOutput($"Device {StandardResponseMessage.FromDeviceId} Level: {Level / 255.0:P0} ({Level})");
    }

    /// <summary>
    /// Command results
    /// Will throw if accessed before command complete successfully
    /// </summary>
    internal int Level => StandardResponseMessage.Command2;
}

/// <summary>
///  Command to turn off the load
/// </summary>
public sealed class LightOffCommand : DeviceCommand
{
    public const string Name = "LightOff";
    public const string Help = "<DeviceID>";
    public const byte CommandCode = CommandCode_LightOFF;
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return string.Empty; }

    public LightOffCommand(Gateway gateway, InsteonID deviceID) : base(gateway, deviceID)
    {
        Command1 = (byte)CommandCode;
    }

    private protected override void Done()
    {
        base.Done();
        LogOutput($"Device {StandardResponseMessage.FromDeviceId} Off");
    }
}

/// <summary>
///  Command to turn on the load fast (ignoring ramp)
/// </summary>
public sealed class FastLightOnCommand : DeviceCommand
{
    public const string Name = "FastLightOn";
    public const string Help = "<DeviceID> <Level (0-255 or 0-100% default 255 (100%))>";
    public const byte CommandCode = CommandCode_FastLightON;
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() {return $"Level: {Command2 / 255.0:P0} ({Command2})";}

    public FastLightOnCommand(Gateway gateway, InsteonID deviceID, byte level) : base(gateway, deviceID)
    {
        Command1 = (byte)CommandCode;
        Command2 = level;
    }

    private protected override void Done()
    {
        base.Done();
        LogOutput($"Device {StandardResponseMessage.FromDeviceId} Level: {Level / 255.0:P0} ({Level})");
    }

    /// <summary>
    /// Command results
    /// Will throw if accessed before command completed successfully
    /// </summary>
    internal int Level => StandardResponseMessage.Command2;
}

/// <summary>
///  Command to turn off the load
/// </summary>
public sealed class FastLightOffCommand : DeviceCommand
{
    public const string Name = "FastLightOff";
    public const string Help = "<DeviceID>";
    public const byte CommandCode = CommandCode_FastLightOFF;
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return string.Empty; }

    public FastLightOffCommand(Gateway gateway, InsteonID deviceID) : base(gateway, deviceID)
    {
        Command1 = (byte)CommandCode;
    }

    private protected override void Done()
    {
        base.Done();
        LogOutput($"Device {StandardResponseMessage.FromDeviceId} Off");
    }
}

/// <summary>
///  Command to incrementally brighten the load
/// </summary>
public sealed class BrighterCommand : DeviceCommand
{
    public const string Name = "Brighter";
    public const string Help = "<DeviceID>";
    public const byte CommandCode = CommandCode_IncrementalBright;
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return string.Empty; }

    public BrighterCommand(Gateway gateway, InsteonID deviceID) : base(gateway, deviceID)
    {
        Command1 = CommandCode_IncrementalBright;
    }

    private protected override void Done()
    {
        base.Done();
        LogOutput($"Device {StandardResponseMessage.FromDeviceId} is brighter");
    }
}

/// <summary>
///  Comamnd to incrementally dim the load
/// </summary>
public sealed class DimmerCommand : DeviceCommand
{
    public const string Name = "Dimmer";
    public const string Help = "<DeviceID>";
    public const byte CommandCode = CommandCode_IncrementalDim;
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return string.Empty; }

    public DimmerCommand(Gateway gateway, InsteonID deviceID) : base(gateway, deviceID)
    {
        Command1 = CommandCode_IncrementalDim;
    }

    private protected override void Done()
    {
        base.Done();
        LogOutput($"Device {StandardResponseMessage.FromDeviceId} is dimmer");
    }
}

/// <summary>
///  Command to get the level to which load is turned on
/// </summary>
public sealed class GetOnLevelCommand : DeviceCommand
{
    public const string Name = "GetOnLevel";
    public const string Help = "<DeviceID>";
    public const byte CommandCode = CommandCode_LightStatusRequest;
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return string.Empty; }

    public GetOnLevelCommand(Gateway gateway, InsteonID deviceID) : base(gateway, deviceID)
    {
        Command1 = CommandCode_LightStatusRequest;
        Command2 = 0;
    }

    private protected override void Done()
    {
        base.Done();
        LogOutput($"Device {StandardResponseMessage.FromDeviceId}, On-Level: {OnLevel / 255.0:P0} ({OnLevel}), DB Delta: {DBDelta}");
    }

    /// <summary>
    /// Command results
    /// Will throw if accessed before command completed successfully
    /// </summary>
    internal int DBDelta => StandardResponseMessage.Command1;
    internal int OnLevel => StandardResponseMessage.Command2;
}

/// <summary>
///  Command to get on level we are moving to 
///  TODO: need better explaination
/// </summary>
public sealed class GetOnLevelMovingToCommand : DeviceCommand
{
    public const string Name = "GetOnLevelMovingTo";
    public const string Help = "";
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return string.Empty; }

    internal GetOnLevelMovingToCommand(Gateway gateway) : base(gateway)
    {
        Command1 = CommandCode_LightStatusRequest;
        Command2 = 2;
    }

    private protected override void Done()
    {
        base.Done();
        LogOutput($"Device {StandardResponseMessage.FromDeviceId}, OnLevelMovingTo: {OnLevelMovingTo / 255.0:P0} ({OnLevelMovingTo})");
    }

    internal int OnLevelMovingTo => Command2;
}

/// <summary>
/// Get the version of the Insteon Engine for a device
/// </summary>
public sealed class GetInsteonEngineVersionCommand : DeviceCommand
{
    public const string Name = "GetInsteonEngineVersion";
    public const string Help = "<DeviceID>";
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return string.Empty; }

    public GetInsteonEngineVersionCommand(Gateway gateway, InsteonID deviceID) : base(gateway, deviceID)
    {
        Command1 = CommandCode_GetInsteonEngineVersion;
        Command2 = 0;
    }

    private protected override async Task<bool> RunAsync()
    {
        if (MockPhysicalDevice != null)
        {
            // Mock implementation of this command for testing purposes only
            // Simulate a response from the device
            OnStandardResponseReceived(new InsteonStandardMessage(
                InsteonMessage.BuildHexString(ToDeviceID, InsteonID.Null, (byte)MessageType.Direct | (byte)MessageLength.Standard, command1: CommandCode_GetInsteonEngineVersion, command2: (byte)MockPhysicalDevice.EngineVersion)));
            return true;
        }
        return await base.RunAsync();
    }

    private protected override void Done()
    {
        base.Done();
        LogOutput($"Device {StandardResponseMessage.FromDeviceId} Engine version: {ReturnedEngineVersion}");
    }

    // Insteon engine version returned by the device
    internal int ReturnedEngineVersion => StandardResponseMessage.Command2;
}

/// <summary>
/// Pings a device
/// </summary>
public sealed class PingCommand : DeviceCommand
{
    public const string Name = "Ping";
    public const string Help = "<DeviceID>";
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return string.Empty; }

    public PingCommand(Gateway gateway, InsteonID deviceID) : base(gateway, deviceID)
    {
        Command1 = CommandCode_Ping;
        Command2 = 0;
    }

    private protected override async Task<bool> RunAsync()
    {
        // Mock implementation of this command for testing purposes
        if (MockPhysicalDevice != null)
            return true;

        return await base.RunAsync();
    }

    private protected override void Done()
    {
        base.Done();
        LogOutput($"Device {StandardResponseMessage.FromDeviceId} responded with ACK!");
    }
}
