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
using Insteon.Mock;
using System.Diagnostics;

namespace Insteon.Commands;

/// <summary>
/// Command to start all-linking the IM with a device on a given group
/// or to unlink the IM from a device on a given group
/// Two different behaviors:
/// - if the deviceId is passed-in, the EnterLinkingMode command (EnterUnlinkingMode for removing a device) is sent to auto-link that id
/// - if the deviceId is not specified, the user must press the SET button on the target device for more than 3s to link it 
/// Note that I have not been able to make the EnterUnlinkingMode command work.
/// </summary>
public sealed class StartIMAllLinkingCommand : HubCommand
{
    // Command logging 
    public const string Name = "StartIMLinking";
    public static string Help = $"[<DeviceID>] <Group> {LA_CreateControllerLink}|{LA_CreateResponderLink}|{LA_CreateAutoLink}|{LA_DeleteLink}";
    private protected override string GetLogName() { return Name; }
    private protected override string GetLogParams()
    {
        String actionStr = "";
        switch (action)
        {
            case LinkingAction.DeleteLink:
                actionStr = LA_DeleteLink;
                break;
            case LinkingAction.CreateResponderLink:
                actionStr = LA_CreateResponderLink;
                break;
            case LinkingAction.CreateControllerLink:
                actionStr = LA_CreateControllerLink;
                break;
            case LinkingAction.CreateAutoLink:
                actionStr = LA_CreateAutoLink;
                break;
        }

        return "Group: " + group.ToString() +
            ", " + actionStr;
    }

    public static LinkingAction LinkingActionFromString(string s)
    {
        if (String.Compare(s, LA_DeleteLink, true) == 0)
        {
            return LinkingAction.DeleteLink;
        }
        else if (String.Compare(s, LA_CreateControllerLink, true) == 0)
        {
            return LinkingAction.CreateControllerLink;
        }
        else if (String.Compare(s, LA_CreateAutoLink, true) == 0)
        {
            return LinkingAction.CreateAutoLink;
        }
        else if (String.Compare(s, LA_CreateResponderLink, true) == 0)
        {
            return LinkingAction.CreateResponderLink;
        }
        else
        {
            throw new Exception($"Invalid linking action: {s}");
        }
    }

    const string LA_DeleteLink = "Unlink";
    const string LA_CreateControllerLink = "Controller";
    const string LA_CreateResponderLink = "Responder";
    const string LA_CreateAutoLink = "Auto";


    /// <summary>
    /// Construct a command to ALL-Link or delete the ALL-Link when the user presses the SET button on the device
    /// </summary>
    /// <param name="group"></param>
    /// <param name="action"></param>
    public StartIMAllLinkingCommand(Gateway gateway, byte group, LinkingAction action) : base(gateway)
    {
        this.group = group;
        this.action = action;

        IMCommandType = IMCommandTypes.IM;
        IMCommandCode = IMCommandCode_StartAllLinking;
        switch (action)
        {
            case LinkingAction.CreateResponderLink:
                IMCommandParams = "00";
                break;
            case LinkingAction.CreateControllerLink:
                IMCommandParams = "01";
                break;
            case LinkingAction.CreateAutoLink:
                IMCommandParams = "03";
                break;
            case LinkingAction.DeleteLink:
                IMCommandParams = "FF";
                break;
        }
        IMCommandParams += this.group.ToString("X2");
        IMResponseLength = 2;
        ResponseTimeout = TimeSpan.FromMinutes(4);    // increase timeout to give user the time to hit the SET button
    }

    /// <summary>
    /// Construct a command to ALL-Link or delete the ALL-Link to a specific device
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="group"></param>
    /// <param name="action"></param>
    public StartIMAllLinkingCommand(Gateway gateway, InsteonID deviceId, byte group, LinkingAction action) : this(gateway, group, action)
    {
        this.deviceId = deviceId;
    }

    // Private data
    private InsteonID? deviceId;                                        // id of the device to ALL-Link (simulates user pressing set button if not null)
    private byte group;                                                 // group to ALL-Link for
    private LinkingAction action;                                       // action (create controller or responder ALL-Link or delete ALL-Link)
    private InsteonAllLinkingCompletedMessage responseMessage = null!;  // returned properties of the ALL-Linked device

    /// <summary>
    /// internal properties retrievable after this command has run successfully
    /// </summary>
    internal byte Group => responseMessage.Group;
    internal LinkingAction LinkAction => responseMessage.Action;
    internal InsteonID DeviceId => responseMessage.FromDeviceId;
    internal DeviceKind.CategoryId DeviceCategory => responseMessage.DeviceCategory;
    internal int DeviceSubCategory => responseMessage.DeviceSubCategory;
    internal byte DeviceFirmwareRevision => responseMessage.DeviceFirmwareRevision;

    // For testing purposes, we can pass a mock device to simulate the linking process
    internal MockPhysicalDevice? MockPhysicalDevice { get; init; }

    private protected override async Task<bool> RunAsync()
    {
        if (MockPhysicalIM != null)
        {
            // Mock implementation of this command for testing purposes.
            // Properties above return the values of the MockPhysicalIM and MockPhysicalDevice that were passed in.

            // We add or remove the link to the device from group 0 of the mock IM
            Debug.Assert(deviceId != null);
            responseMessage = MockPhysicalIM.HandleLinkingAction(action, group, deviceId, MockPhysicalDevice);
            return true;
        }

        return await base.RunAsync();
    }

    // Async method called after OnIMResponseReceived has been called
    // We sent the "SET Button Pressed" command for the device to ALL-Link if one is specificied
    // (otherwise, the user needs to press the SET button on the actual device)
    private protected override bool OnIMResponseReceived(HexString message)
    {
        IMResponse = message;

        // Wait for user to press the SET button
        return true;
    }

    protected override async Task OnAfterIMReponseReceivedAsync()
    {
        // If we were passed a device to add or to delete, get this device to enter linking or unlinking mode respectively.
        // If no device was passed, the user must press the SET button on the device.
        if (deviceId != null && !deviceId.IsNull)
        {
            Command cmd = action != LinkingAction.DeleteLink ?
                new EnterLinkingModeCommand(gateway, deviceId, group) {SuppressLogging = true } :
                new EnterUnlinkingModeCommand(gateway, deviceId, group) { SuppressLogging = true };

            if (!await cmd.TryRunAsync(parentCommand: this))
            {
                SetComplete(ErrorReasons.SubCommandFailed);
            }
        }
    }

    // Called when we have received the "ALL-Linking Complete" message
    private protected override bool OnAllLinkingCompletedResponseReceived(InsteonAllLinkingCompletedMessage message)
    {
        if (deviceId != null && message.FromDeviceId != deviceId)
        {
            return false;
        }

        responseMessage = message;

        SetComplete();
        return true;
    }

    // We are done. If the command has not been cancelled, verify we got the propper response
    private protected override void Done()
    {
        if (!IsCancelled)
        {
            base.Done();

            if (responseMessage == null)
            {
                throw new Exception("All-Link Completed response message was not received");
            }
            
            LogOutput((action == LinkingAction.DeleteLink ? "Unlinked" : "Linked") + 
                " IM, group " + Group.ToString() + " as " +
                (LinkAction == LinkingAction.CreateResponderLink ? "Responder" : "") +
                (LinkAction == LinkingAction.CreateControllerLink ? "Controller" : "") +
                " to device " + DeviceId.ToString() + 
                ", Model: " + DeviceKind.GetModelName(DeviceCategory, DeviceSubCategory) + ", Rev: " + DeviceFirmwareRevision);
        }
    }

    /// <summary>
    /// Cancel the linking process
    /// If this command is run when cancelling, this method must be called instead of just running a CancelIMAllLinking command 
    /// to ensure execution of this command is properly cancelled and terminated
    /// </summary>
    public override async Task Cancel()
    {
        CancelIMAllLinkingCommand deviceCommand = new CancelIMAllLinkingCommand(gateway);
        await deviceCommand.TryRunAsync(parentCommand: this);
        await base.Cancel();
    }
}
