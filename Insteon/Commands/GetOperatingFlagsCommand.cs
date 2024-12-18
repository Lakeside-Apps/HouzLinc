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

public class GetOperatingFlagsBaseCommand : DeviceCommand
{
    private protected override string GetLogName() { return ""; }
    private protected override string GetLogParams() { return ""; }

    internal GetOperatingFlagsBaseCommand(Gateway gateway, InsteonID deviceID) : base(gateway, deviceID)
    { }

    /// <summary>
    ///  Called when a standard NAK is received from the device the command was addressed to
    ///  See base class for details
    /// </summary>
    /// <param name="message">message received</param>
    /// <returns>true if response message was acceptable</returns>
    private protected override bool OnDirectNAKReceived(InsteonStandardMessage message)
    {
        StandardResponseMessage = message;

        // Some devices appear to send a NAK response instead of an ACK 
        // where Command2 is the valid response if not one of the DirectNAKErrorCodes
        if (message.Command2 < (byte)DirectNAKErrorCodes.LowestValid)
        {
            Logger.Log.Debug("DirectNAKErrorCode unknown, NAK considered as ACK");
            StandardResponseMessage = message;
            SetComplete();
        }
        else
        {
            SetComplete(ErrorReasons.NAK);
        }
        return true;
    }
}

public sealed class GetOperatingFlagsCommand : GetOperatingFlagsBaseCommand
{
    public const string Name = "GetOperatingFlags";
    public const string Help = "<DeviceID>";
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return ""; }

    public GetOperatingFlagsCommand(Gateway gateway, InsteonID deviceID) : base(gateway, deviceID)
    {
        Command1 = CommandCode_ReadOperatingFlags;
        Command2 = 0;
    }

    private protected override async Task<bool> RunAsync()
    {
        // Mock implementation of this command for testing purposes
        // Simulate a response from the device
        if (MockPhysicalDevice != null)
        {
            OnStandardResponseReceived(new InsteonStandardMessage(
                InsteonMessage.BuildHexString(ToDeviceID, InsteonID.Null, (byte)MessageType.Direct | (byte)MessageLength.Standard, command1: CommandCode_ReadOperatingFlags, command2: MockPhysicalDevice.OperatingFlags)));
            return true;
        }
        return await base.RunAsync();
    }

    private protected override void Done()
    {
        base.Done();
        LogOutput($"{StandardResponseMessage.FromDeviceId.ToString()}, Operating Flags: 0x{OperatingFlags.ToString("X2")}");
    }

    /// <summary>
    /// Command results
    /// Will throw if accessed before command completed successfully
    /// </summary>
    internal byte OperatingFlags => StandardResponseMessage.Command2;

    // ControlLinc, ICON Tabletop (Cat: 0x00, Subcats: 0x04, 0x06)
    internal bool ProgramLock => (OperatingFlags & (1 << 0)) != 0;
    internal bool LEDOn => (OperatingFlags & (1 << 1)) != 0;
    internal bool BeeperOn => (OperatingFlags & (1 << 2)) != 0;

    // RemoteLinc (Cat: 0x00, Subcats: 0x05, 0x0E, 0x10, etc.)
    //internal bool ProgramLock => (OperatingFlags & (1 << 0)) != 0;
    //internal bool LEDOn => (OperatingFlags & (1 << 1)) != 0;
    //internal bool BeeperOn => (OperatingFlags & (1 << 2)) != 0;
    internal bool AllowSleep => !StayAwake;
    internal bool StayAwake => (OperatingFlags & (1 << 3)) != 0;
    internal bool AllowTransmit => !ReceiveOnly;
    internal bool ReceiveOnly => (OperatingFlags & (1 << 4)) != 0;
    internal bool AllowHeartbeat => !NoHeartbeat;
    internal bool NoHeartbeat => (OperatingFlags & (1 << 5)) != 0;

    // KeypadLinc dimmers (Cat: 0x01, SubCat: 0x09, 0x0A)
    //internal bool ProgramLock => (OperatingFlags & (1 << 0)) != 0;
    //internal bool LEDOn => (OperatingFlags & (1 << 1)) != 0;
    internal bool ResumeDimEnabled => (OperatingFlags & (1 << 2)) != 0;
    internal bool Is6ButtonKeypad => !Is8ButtonKeypad;
    internal bool Is8ButtonKeypad => (OperatingFlags & (1 << 3)) != 0;
    internal bool BacklightOn => (OperatingFlags & (1 << 4)) != 0;
    internal bool KeyBeepOn => (OperatingFlags & (1 << 5)) != 0;

    // Other dimmable devices (Cat: 0x01, SubCat: other)
    //internal bool ProgramLock => (OperatingFlags & (1 << 0)) != 0;
    internal bool LEDOnTx => (OperatingFlags & (1 << 1)) != 0;
    //internal bool ResumeDimEnabled => return (OperatingFlags & (1 << 2)) != 0;
    //internal bool Unused => (OperatingFlags & (1 << 3)) != 0;
    internal bool LEDAlwaysOn => (OperatingFlags & (1 << 4)) != 0;
    internal bool LoadSenseOn => (OperatingFlags & (1 << 5)) != 0;

    // KeypadLinc Relay (cat: 0x02, SubCat: 0x0F)
    //internal bool ProgramLock => (OperatingFlags & (1 << 0)) != 0;
    //internal bool LEDOn => (OperatingFlags & (1 << 1)) != 0;
    //internal bool ResumeDimEnabled => (OperatingFlags & (1 << 2)) != 0;
    //internal bool Is8ButtonKeypad => (OperatingFlags & (1 << 3)) != 0;
    //internal bool BacklightOn => (OperatingFlags & (1 << 4)) != 0;
    //internal bool KeyBeepOn => (OperatingFlags & (1 << 5)) != 0;

    // Other on/off switches (cat: 0x02, SubCats: other)
    //internal bool ProgramLock => (OperatingFlags & (1 << 0)) != 0;
    //internal bool LEDOn => (OperatingFlags & (1 << 1)) != 0;
    //internal bool ResumeDimEnabled => (OperatingFlags & (1 << 2)) != 0;
    //internal bool Unused => (OperatingFlags & (1 << 3)) != 0;
    //internal bool LEDOn => (OperatingFlags & (1 << 4)) != 0;
    //internal bool LoadSenseOn => (OperatingFlags & (1 << 5)) != 0;
}

/// <summary>
///  Command to get the current version of the device database
/// </summary>
public sealed class GetDBDeltaCommand : GetOperatingFlagsBaseCommand
{
    public const string Name = "GetDBDelta";
    public const string Help = "<DeviceID>";
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return ""; }

    public GetDBDeltaCommand(Gateway gateway, InsteonID deviceID) : base(gateway, deviceID)
    {
        Command1 = CommandCode_ReadOperatingFlags;
        Command2 = 0x01;
    }

    private protected override void Done()
    {
        base.Done();
        LogOutput($"{StandardResponseMessage.FromDeviceId.ToString()}, DB Delta: {DBDelta.ToString()}");
    }

    private protected override async Task<bool> RunAsync()
    {
        // Mock implementation of this command for testing purposes
        // Nothing to do here, DBDelta returns value from the mock physical device
        if (MockPhysicalDevice != null)
            return true;

        return await base.RunAsync();
    }

    /// <summary>
    /// Command results
    /// Will throw if accessed before command completed successfully
    /// </summary>
    internal int DBDelta => (MockPhysicalDevice == null) ? StandardResponseMessage.Command2 : MockPhysicalDevice.AllLinkDatabase.Revision;
}

public sealed class GetOpFlags2Command : GetOperatingFlagsBaseCommand
{
    public const string Name = "GetOpFlags2";
    public const string Help = "<DeviceID>";
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return ""; }

    public GetOpFlags2Command(Gateway gateway, InsteonID deviceID) : base(gateway, deviceID)
    {
        Command1 = CommandCode_ReadOperatingFlags;
        Command2 = 0x05;
    }

    private protected override void Done()
    {
        base.Done();
        LogOutput($"{StandardResponseMessage.FromDeviceId.ToString()}, OpFlags2: 0x{OpFlags2.ToString("X2")}");
    }

    private protected override async Task<bool> RunAsync()
    {
        // Mock implementation of this command for testing purposes
        // Simulate a response from the device
        if (MockPhysicalDevice != null)
        {
            OnStandardResponseReceived(new InsteonStandardMessage(
                InsteonMessage.BuildHexString(ToDeviceID, InsteonID.Null, (byte)MessageType.Direct | (byte)MessageLength.Standard, command1: CommandCode_ReadOperatingFlags, command2: MockPhysicalDevice.OpFlags2)));
            return true;
        }
        return await base.RunAsync();
    }

    /// <summary>
    /// Command results
    /// Will throw if accessed before command completed successfully
    /// </summary>
    internal byte OpFlags2 => StandardResponseMessage.Command2;
}
