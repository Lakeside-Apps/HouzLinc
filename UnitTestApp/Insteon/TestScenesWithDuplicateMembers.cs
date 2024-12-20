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
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Insteon;

[TestClass]
public sealed class TestScenesWithDuplicateMembers
{
    House house = null!;

    public TestScenesWithDuplicateMembers()
    {
    }

    public TestContext TestContext { get; set; } = null!;

    private void LogFilePath(string path)
    {
        // This does not seem to actually be working
        Microsoft.VisualStudio.TestTools.UnitTesting.Logging.Logger.LogMessage(path);
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        // Place breakpoint here to collect the test files
    }

    [TestInitialize]
    public async Task Init()
    {
        ApplicationType.IsUnitTestFramework = true;
        var h = await ModelHolderForTest.LoadFromFile("Scenes", "Scene2Expanded");
        Assert.IsNotNull(h);
        this.house = h!;
    }

    [TestCleanup]
    public void Cleanup()
    {
        house = null!;
    }

    [TestMethod]
    public void TestSceneLoaded()
    {
        Assert.IsNotNull(house);
        Assert.IsNotNull(house.Scenes, "House has no scenes");
        Assert.IsNotNull(house.Devices, "House has no devices");
        Assert.IsTrue(house.Devices.Count == 9, "Wrong count of devices");
        Assert.IsNotNull(house.Scenes.GetSceneById(1), "House doesn't have a scene with id == 1");
        Assert.IsTrue(house.Scenes.GetSceneById(1)!.Members.Count == 9, "Scene 1 has wrong count of scenes members");
    }

    [TestMethod]
    public void TestSceneMembers()
    {
        Scene scene = house.Scenes.GetSceneById(1)!;
        Assert.IsTrue(scene.Members[0].DeviceId == "11.11.11");
        Assert.IsTrue(scene.Members[1].DeviceId == "22.22.22");
        Assert.IsTrue(scene.Members[2].DeviceId == "33.33.33");
        Assert.IsTrue(scene.Members[3].DeviceId == "44.44.44");
        Assert.IsTrue(scene.Members[4].DeviceId == "55.55.55");
        Assert.IsTrue(scene.Members[5].DeviceId == "55.55.55");
        Assert.IsTrue(scene.Members[6].DeviceId == "44.44.44");
        Assert.IsTrue(scene.Members[7].DeviceId == "44.44.44");
        Assert.IsTrue(scene.Members[8].DeviceId == "11.11.11");
    }

    [TestMethod]
    public void TestSceneMemberQueries()
    {
        Scene scene = house.Scenes.GetSceneById(1)!;

        // Get a few members by id/group
        SceneMember? member;
        Assert.IsTrue(scene.Members.TryGetMember(InsteonID.FromString("11.11.11"), group: 2, isController: true, isResponder: true, out member));
        Assert.IsTrue(member != null);
        Assert.IsTrue(member.DeviceId == "11.11.11");
        Assert.IsTrue(member.Group == 2);

        Assert.IsTrue(scene.Members.TryGetMember(InsteonID.FromString("22.22.22"), group: 1, isController: true, isResponder: true, out member));
        Assert.IsTrue(member != null);
        Assert.IsTrue(member.DeviceId == "22.22.22");
        Assert.IsTrue(member.Group == 1);

        Assert.IsTrue(scene.Members.TryGetMember(InsteonID.FromString("33.33.33"), group: 1, isController: false, isResponder: true, out member));
        Assert.IsTrue(member != null);
        Assert.IsTrue(member.DeviceId == "33.33.33");
        Assert.IsTrue(member.Group == 1);

        Assert.IsTrue(scene.Members.TryGetMember(InsteonID.FromString("44.44.44"), group: 4, isController: false, isResponder: true, out member));
        Assert.IsTrue(member != null);
        Assert.IsTrue(member.DeviceId == "44.44.44");
        Assert.IsTrue(member.Group == 4);

        Assert.IsTrue(scene.Members.TryGetMatchingMembers(InsteonID.FromString("55.55.55"), group: 6, out var matchingMembers));
        Assert.IsTrue(matchingMembers.Count == 2);

        Assert.IsTrue(scene.Members.TryGetMatchingMembers(InsteonID.FromString("44.44.44"), group: 5, out var matchingMembers2));
        Assert.IsTrue(matchingMembers2.Count == 2);

        Assert.IsTrue(scene.Members.TryGetMember(InsteonID.FromString("11.11.11"), group: 3, isController: true, isResponder: false, out member));
        Assert.IsTrue(member != null);
        Assert.IsTrue(member.DeviceId == "11.11.11");
        Assert.IsTrue(member.Group == 3);

        // Query all Controllers on a single device
        Assert.IsTrue(scene.Members.TryGetMatchingControllers(InsteonID.FromString("11.11.11"), out List<SceneMember>? matchingControllers));
        Assert.IsTrue(matchingControllers!.Count == 2);

        // Query all responders on a single device
        Assert.IsTrue(scene.Members.TryGetMatchingResponders(InsteonID.FromString("44.44.44"), out List<SceneMember>? matchingResponders));
        Assert.IsTrue(matchingResponders!.Count == 3);
    }

    /// <summary>
    /// This expands a scene with duplicated scene members, creating duplicate links for duplicate members as well as 
    /// for controllers with multiple responders on same device, so that we can test the removal of duplicates.
    /// IMPORTANT: This test works off Scenes/Scene2.xml, and creates the content of Refs/Scenes/Scene2Expanded.xml.
    /// Subsequent Tests work off Scene2Expanded.xml. 
    /// This allows for more stable record uids making it easier to compare files.
    /// </summary>
    [TestMethod]
    public async Task TestExpandScene()
    {
        var house = await ModelHolderForTest.LoadFromFile("Scenes", "Scene2");
        Assert.IsNotNull(house);
        Scene scene = house.Scenes.GetSceneById(1)!;
        scene.Expand(allowDuplicateLinks: true);
        house.PretendSync();
        LogFilePath(await ModelHolderForTest.SaveToFile("Scenes2", TestContext.TestName!, this.house));
        var result = await ModelHolderForTest.CompareFiles("Scenes2", TestContext.TestName!);
        Assert.IsNull(result, result);
    }

    /// <summary>
    /// This tests removing duplicate memmbers one at a time and making sure we end up with the correct links.
    /// </summary>
    [TestMethod]
    public async Task TestRemoveDuplicateMembersOneByOne()
    {
        Scene scene = house.Scenes.GetSceneById(1)!;
        if (scene.Members.TryGetMember(new InsteonID("44.44.44"), group: 5, isController: false, isResponder: true, out var member1))
        {
            scene.RemoveMember(member1, removeLinks: true);
            if (scene.Members.TryGetMember(new InsteonID("55.55.55"), group: 6, isController: true, isResponder: false, out var member2))
            {
                scene.RemoveMember(member2, removeLinks: true);
            }
        }
        LogFilePath(await ModelHolderForTest.SaveToFile("Scenes2", TestContext.TestName!, house));
        var result = await ModelHolderForTest.CompareFiles("Scenes2", TestContext.TestName!);
        Assert.IsNull(result, result);
    }

    /// <summary>
    /// This tests removing duplicate memmbers all at once, simulating using the "Remove Duplicate Members" menu option
    /// and making sure we end up with the correct links.
    /// </summary>
    [TestMethod]
    public async Task TestRemoveDuplicateMembers()
    {
        Scene scene = house.Scenes.GetSceneById(1)!;
        scene.RemoveDuplicateMembers();
        LogFilePath(await ModelHolderForTest.SaveToFile("Scenes2", TestContext.TestName!, house));
        var result = await ModelHolderForTest.CompareFiles("Scenes2", TestContext.TestName!);
        Assert.IsNull(result, result);
    }
}
