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
using Insteon.Model;
using Insteon.Base;
using static Insteon.Model.AllLinkRecord;

namespace Insteon.Mock;

/// <summary>
/// Simulates a real physical IM for testing purposes. 
/// Hub (IM) commands can take a MockPhysicalIM as a parameter and will be simulated on a mock device.
/// </summary>
public sealed class MockPhysicalIM : MockPhysicalDevice
{
    public MockPhysicalIM(InsteonID Id) : base(Id)
    {
    }

    public MockPhysicalIM(InsteonID Id, AllLinkDatabase allLinkDatabase) : base(Id, allLinkDatabase)
    {
    }

    // Index for ManageIMAllLinkRecordCommand findNext
    internal int nextRecordIndex = 0;

    internal InsteonAllLinkingCompletedMessage HandleLinkingAction(LinkingAction action, byte group, InsteonID deviceId, MockPhysicalDevice? mockPhysicalDevice)
    {
        if (action == LinkingAction.CreateAutoLink)
        {
            // TODO:figure out what to do here.
            // CreateAutoLink is supposed to create a controller link when the IM initiate the linking process
            // but a receiver link if another device initiate the linking process.
            // For now, we create a controller link
            action = LinkingAction.CreateControllerLink;
        }

        switch (action)
        {
            case LinkingAction.DeleteLink:
                {
                    var record = new AllLinkRecord(deviceId, isController: true, group);
                    if (AllLinkDatabase.TryGetEntry(record, IdGroupTypeComparer.Instance, out var recordToDelete))
                    {
                        AllLinkDatabase.RemoveRecord(recordToDelete);
                    }
                    break;
                }
            case LinkingAction.CreateControllerLink:
                {
                    // Add the controller link to the IM database
                    var record = new AllLinkRecord(deviceId, isController: true, group) { Data1 = 1, Data2 = 32, Data3 = 65 };
                    AllLinkDatabase.AddRecord(record);

                    // And the corresponding responder link to the device
                    mockPhysicalDevice?.AllLinkDatabase.AddRecord(new AllLinkRecord(Id, isController: false, group) { Data1 = 255, Data2 = 28, Data3 = 1 });
                    break;
                }
            case LinkingAction.CreateResponderLink:
                {
                    // Add the responder link to the IM database
                    var record = new AllLinkRecord(deviceId, isController: false, group);
                    AllLinkDatabase.AddRecord(record);

                    // And the corresponding controller link to the device
                    mockPhysicalDevice?.AllLinkDatabase.AddRecord(new AllLinkRecord(Id, isController: true, group) );
                    break;
                }
        }

        // The IM (hub) keeps its all link database with only undeleted records
        AllLinkDatabase.Compress();

        return new InsteonAllLinkingCompletedMessage(action, group, deviceId, 
            mockPhysicalDevice?.CategoryId ?? 0, (byte)(mockPhysicalDevice?.SubCategory ?? 0), (byte)(mockPhysicalDevice?.Revision ?? 0));
    }
}
