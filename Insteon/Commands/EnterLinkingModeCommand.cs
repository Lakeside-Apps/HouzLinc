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
using Common;

namespace Insteon.Commands;

public sealed class EnterLinkingModeCommand : DeviceCommand
{
    public const string Name = "EnterLinkingMode";
    public const string Help = "<DeviceID> <Group>";
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return "Group: " + Command2.ToString(); }

    public EnterLinkingModeCommand(Gateway gateway, InsteonID deviceId, byte group) : base(gateway)
    {
        ToDeviceID = deviceId;
        Command1 = CommandCode_EnterLinkingMode;
        Command2 = group;
    }

    private protected override async Task<bool> RunAsync()
    {
        // Determine the version of the Insteon engine on the device
        var command = new GetInsteonEngineVersionCommand(gateway, ToDeviceID);
        if (await command.TryRunAsync(parentCommand: Running) && command.EngineVersion >= 2)
        {
            // If version 2 or above, send extended command
            ClearData();
        }

        return await base.RunAsync();
    }

    /// <summary>
    ///  Called when a standard response message is received from the device being linked
    ///  See base class for details
    /// </summary>
    private protected override bool OnStandardResponseReceived(InsteonStandardMessage message)
    {
        StandardResponseMessage = message;

        // Base class (DeviceCommand) complete the command if not waiting for an extended response message
        // Here, we complete the command only if we have also recevied the SET buttion pressed broadcast message
        if (setButtonPressedMessage != null)
        {
            SetComplete();
        }
        return true;
    }

    /// <summary>
    ///  Called when the SET button pressed broadcast message is received from the device being linked
    /// </summary>
    private protected override bool OnStandardBroadcastResponseReceived(InsteonStandardMessage message)
    {
        setButtonPressedMessage = new InsteonSetButtonPressedBroadcastMessage(message);

        // Complete the command if we have received all we were waiting for
        if (StandardResponseMessage != null)
        {
            SetComplete();
        }
        return true;
    }

    private protected override void Done()
    {
        base.Done();
        LogOutput($"Device {ToDeviceID} entered linking mode!");
    }

    // Broadcast message in response to "Set" button pressed on a device
    private InsteonSetButtonPressedBroadcastMessage? setButtonPressedMessage;

    // Helper to get the broadcast message, throwing if accessed before command completed successfully
    private InsteonSetButtonPressedBroadcastMessage ThrowingSetButtonPressedMessage
        => setButtonPressedMessage ?? throw new Exception("Device did not broadcast SET button pressed message");

    /// <summary>
    /// Command results
    /// Will throw if accessed before command completed successfully
    /// </summary>
    internal DeviceKind.CategoryId DeviceCategory => ThrowingSetButtonPressedMessage.DeviceCategory;
    internal int DeviceSubCategory => ThrowingSetButtonPressedMessage.DeviceSubCategory;
    internal int DeviceRevision => ThrowingSetButtonPressedMessage.DeviceRevision;
}

public sealed class EnterUnlinkingModeCommand : DeviceCommand
{
    public const string Name = "EnterUnlinkingMode";
    public const string Help = "<DeviceID> <Group>";
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams() { return Command2.ToString(); }

    public EnterUnlinkingModeCommand(Gateway gateway, InsteonID deviceId, byte group) : base(gateway)
    {
        ToDeviceID = deviceId;
        Command1 = CommandCode_EnterUnlinkingMode;
        Command2 = group;
        ClearData();
    }

    private protected override void Done()
    {
        base.Done();
        LogOutput($"Device {ToDeviceID} entered unlinking mode!");
    }
}
