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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Insteon;

[TestClass]
public sealed class TestScenes
{
    House house = null!;

    public TestScenes()
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
        var h = await ModelHolderForTest.LoadFromFile("Scenes", "Scene1");
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
        Assert.IsTrue(house.Scenes.GetSceneById(1)!.Members.Count == 7, "Scene 1 has wrong count of scenes members");
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
        Assert.IsTrue(scene.Members[5].DeviceId == "44.44.44");
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

        Assert.IsTrue(scene.Members.TryGetMember(InsteonID.FromString("55.55.55"), group: 6, isController: true, isResponder: false, out member));
        Assert.IsTrue(member != null);
        Assert.IsTrue(member.DeviceId == "55.55.55");
        Assert.IsTrue(member.Group == 6);

        Assert.IsTrue(scene.Members.TryGetMember(InsteonID.FromString("44.44.44"), group: 5, isController: false, isResponder: true, out member));
        Assert.IsTrue(member != null);
        Assert.IsTrue(member.DeviceId == "44.44.44");
        Assert.IsTrue(member.Group == 5);

        Assert.IsTrue(scene.Members.TryGetMember(InsteonID.FromString("11.11.11"), group: 3, isController: true, isResponder: false, out member));
        Assert.IsTrue(member != null);
        Assert.IsTrue(member.DeviceId == "11.11.11");
        Assert.IsTrue(member.Group == 3);

        // Query all Controllers on a single device
        Assert.IsTrue(scene.Members.TryGetMatchingControllers(InsteonID.FromString("11.11.11"), out List<SceneMember>? matchingControllers));
        Assert.IsTrue(matchingControllers!.Count == 2);

        // Query all responders on a single device
        Assert.IsTrue(scene.Members.TryGetMatchingResponders(InsteonID.FromString("44.44.44"), out List<SceneMember>? matchingResponders));
        Assert.IsTrue(matchingResponders!.Count == 2);
    }

    [TestMethod]
    public async Task TestExpandScene()
    {
        Scene scene = house.Scenes.GetSceneById(1)!;
        scene.Expand();
        LogFilePath(await ModelHolderForTest.SaveToFile("Scenes", TestContext.TestName!, house));
        var result = await ModelHolderForTest.CompareFiles("Scenes", TestContext.TestName!);
        Assert.IsNull(result, result);
    }

    [TestMethod]
    public async Task TestAddController()
    {
        Scene scene = house.Scenes.GetSceneById(1)!;
        scene.Expand();
        scene.AddSceneMember(InsteonID.FromString("AA.AA.AA"), 7, isController: true, isResponder: false, 255, 27);
        LogFilePath(await ModelHolderForTest.SaveToFile("Scenes", TestContext.TestName!, house));
        var result = await ModelHolderForTest.CompareFiles("Scenes", TestContext.TestName!);
        Assert.IsNull(result, result);
    }

    [TestMethod]
    public async Task TestAddResponder()
    {
        Scene scene = house.Scenes.GetSceneById(1)!;
        scene.Expand();
        scene.AddSceneMember(InsteonID.FromString("BB.BB.BB"), 8, isController: false, isResponder: true, 200, 27);
        LogFilePath(await ModelHolderForTest.SaveToFile("Scenes", TestContext.TestName!, house));
        var result = await ModelHolderForTest.CompareFiles("Scenes", TestContext.TestName!);
        Assert.IsNull(result, result);
    }

    [TestMethod]
    public async Task TestAddControllerResponder()
    {
        Scene scene = house.Scenes.GetSceneById(1)!;
        scene.Expand();
        scene.AddSceneMember(InsteonID.FromString("CC.CC.CC"), 1, isController: true, isResponder: true, 0, 27);
        LogFilePath(await ModelHolderForTest.SaveToFile("Scenes", TestContext.TestName!, house));
        var result = await ModelHolderForTest.CompareFiles("Scenes", TestContext.TestName!);
        Assert.IsNull(result, result);
    }

    [TestMethod]
    public async Task TestRemoveController()
    {
        Scene scene = house.Scenes.GetSceneById(1)!;
        scene.Expand();
        scene.RemoveSceneMember(InsteonID.FromString("55.55.55"), 6, isController: true, isResponder: false);
        LogFilePath(await ModelHolderForTest.SaveToFile("Scenes", TestContext.TestName!, house));
        var result = await ModelHolderForTest.CompareFiles("Scenes", TestContext.TestName!);
        Assert.IsNull(result, result);
    }

    [TestMethod]
    public async Task TestRemoveResponder()
    {
        Scene scene = house.Scenes.GetSceneById(1)!;
        scene.Expand();
        scene.RemoveSceneMember(InsteonID.FromString("33.33.33"), 1, isController: false, isResponder: true);
        LogFilePath(await ModelHolderForTest.SaveToFile("Scenes", TestContext.TestName!, house));
        var result = await ModelHolderForTest.CompareFiles("Scenes", TestContext.TestName!);
        Assert.IsNull(result, result);
    }

    [TestMethod]
    public async Task TestRemoveControllerResponder()
    {
        Scene scene = house.Scenes.GetSceneById(1)!;
        scene.Expand();
        scene.RemoveSceneMember(InsteonID.FromString("11.11.11"), 2, isController: true, isResponder: true);
        LogFilePath(await ModelHolderForTest.SaveToFile("Scenes", TestContext.TestName!, house));
        var result = await ModelHolderForTest.CompareFiles("Scenes", TestContext.TestName!);
        Assert.IsNull(result, result);
    }

    [TestMethod]
    public async Task TestRemoveResponderWithAnotherResponderOnSameDevice()
    {
        Scene scene = house.Scenes.GetSceneById(1)!;
        scene.Expand();
        scene.RemoveSceneMember(InsteonID.FromString("44.44.44"), 4, isController: false, isResponder: true);
        LogFilePath(await ModelHolderForTest.SaveToFile("Scenes", TestContext.TestName!, house));
        var result = await ModelHolderForTest.CompareFiles("Scenes", TestContext.TestName!);
        Assert.IsNull(result, result);
    }

    [TestMethod]
    // Removes controller on keypad 11.11.11 to test Follow behavior on responder button are updated correctly
    public async Task TestRemoveControllerButton()
    {
        Scene scene = house.Scenes.GetSceneById(1)!;
        scene.Expand();
        scene.RemoveSceneMember(InsteonID.FromString("11.11.11"), 3, isController: true, isResponder: false);
        LogFilePath(await ModelHolderForTest.SaveToFile("Scenes", TestContext.TestName!, house));
        var result = await ModelHolderForTest.CompareFiles("Scenes", TestContext.TestName!);
        Assert.IsNull(result, result);
    }

    [TestMethod]
    // Add responder on group 1 of keypad 11.11.11 to ensure that a regular link is created 
    // since this is the load controller group
    public async Task TestAddGroup1Controller()
    {
        Scene scene = house.Scenes.GetSceneById(1)!;
        scene.Expand();
        scene.AddSceneMember(InsteonID.FromString("11.11.11"), group: 1, isController: true, isResponder: true, level: 0, rampRate: 28);
        LogFilePath(await ModelHolderForTest.SaveToFile("Scenes", TestContext.TestName!, house));
        var result = await ModelHolderForTest.CompareFiles("Scenes", TestContext.TestName!);
        Assert.IsNull(result, result);
    }

    [TestMethod]
    // Add the IM (hub) as a controller to the scene
    public async Task TestAddIMController()
    {
        Scene scene = house.Scenes.GetSceneById(1)!;
        scene.Expand();
        scene.AddSceneMember(InsteonID.FromString("AA.00.00"), group: 12, isController: true, isResponder: false, level: 0, rampRate: 28);
        LogFilePath(await ModelHolderForTest.SaveToFile("Scenes", TestContext.TestName!, house));
        var result = await ModelHolderForTest.CompareFiles("Scenes", TestContext.TestName!);
        Assert.IsNull(result, result);
    }

    [TestMethod]
    public async Task TestEditControllerData()
    {
        Scene scene = house.Scenes.GetSceneById(1)!;
        scene.Expand();
        scene.ReplaceSceneMember(InsteonID.FromString("55.55.55"), group: 6, isController: true, isResponder: false, newGroup: 6, newIsController: true, newIsResponder: false, newOnLevel: 100, newRampRate: 27);
        LogFilePath(await ModelHolderForTest.SaveToFile("Scenes", TestContext.TestName!, house));
        var result = await ModelHolderForTest.CompareFiles("Scenes", TestContext.TestName!);
        Assert.IsNull(result, result);
    }

    [TestMethod]
    public async Task TestEditResponderData()
    {
        Scene scene = house.Scenes.GetSceneById(1)!;
        scene.Expand();
        scene.ReplaceSceneMember(InsteonID.FromString("44.44.44"), group: 4, isController: false, isResponder: true, newGroup: 4, newIsController: false, newIsResponder: true, newOnLevel: 100, newRampRate: 27);
        LogFilePath(await ModelHolderForTest.SaveToFile("Scenes", TestContext.TestName!, house));
        var result = await ModelHolderForTest.CompareFiles("Scenes", TestContext.TestName!);
        Assert.IsNull(result, result);
    }

    [TestMethod]
    public async Task TestEditControllerResponderData()
    {
        Scene scene = house.Scenes.GetSceneById(1)!;
        scene.Expand();
        scene.ReplaceSceneMember(InsteonID.FromString("11.11.11"), group: 2, isController: true, isResponder: true, newGroup: 2, newIsController: true, newIsResponder: true, newOnLevel: 100, newRampRate: 27);
        LogFilePath(await ModelHolderForTest.SaveToFile("Scenes", TestContext.TestName!, house));
        var result = await ModelHolderForTest.CompareFiles("Scenes", TestContext.TestName!);
        Assert.IsNull(result, result);
    }

    [TestMethod]
    public async Task TestEditControllerToResponder()
    {
        Scene scene = house.Scenes.GetSceneById(1)!;
        scene.Expand();
        scene.ReplaceSceneMember(InsteonID.FromString("11.11.11"), group: 3, isController: true, isResponder: false, newGroup: 3, newIsController: false, newIsResponder: true, newOnLevel: 160, newRampRate: 28);
        LogFilePath(await ModelHolderForTest.SaveToFile("Scenes", TestContext.TestName!, house));
        var result = await ModelHolderForTest.CompareFiles("Scenes", TestContext.TestName!);
        Assert.IsNull(result, result);
    }

    [TestMethod]
    public async Task TestEditControllerResponderToResponderOnly()
    {
        Scene scene = house.Scenes.GetSceneById(1)!;
        scene.Expand();
        scene.ReplaceSceneMember(InsteonID.FromString("22.22.22"), group: 1, isController: true, isResponder: true, newGroup: 1, newIsController: false, newIsResponder: true, newOnLevel: 76, newRampRate: 28);
        LogFilePath(await ModelHolderForTest.SaveToFile("Scenes", TestContext.TestName!, house));
        var result = await ModelHolderForTest.CompareFiles("Scenes", TestContext.TestName!);
        Assert.IsNull(result, result);
    }

    [TestMethod]
    public async Task TestEditControllerGroup()
    {
        Scene scene = house.Scenes.GetSceneById(1)!;
        scene.Expand();
        scene.ReplaceSceneMember(InsteonID.FromString("11.11.11"), group: 3, isController: true, isResponder: false, newGroup: 5, newIsController: true, newIsResponder: false, newOnLevel: 100, newRampRate: 28);
        LogFilePath(await ModelHolderForTest.SaveToFile("Scenes", TestContext.TestName!, house));
        var result = await ModelHolderForTest.CompareFiles("Scenes", TestContext.TestName!);
        Assert.IsNull(result, result);
    }

    [TestMethod]
    public async Task TestEditResponderGroup()
    {
        Scene scene = house.Scenes.GetSceneById(1)!;
        scene.Expand();
        scene.ReplaceSceneMember(InsteonID.FromString("33.33.33"), group: 1, isController: false, isResponder: true, newGroup: 5, newIsController: false, newIsResponder: true, newOnLevel: 153, newRampRate: 28);
        LogFilePath(await ModelHolderForTest.SaveToFile("Scenes", TestContext.TestName!, house));
        var     result = await ModelHolderForTest.CompareFiles("Scenes", TestContext.TestName!);
        Assert.IsNull(result, result);
    }
}

public static class SceneExtensions
{
    /// <summary>
    /// Add a member to a scene, by member properties
    /// </summary>
    /// <param name="rampRate"></param>
    public static void AddSceneMember(this Scene scene, InsteonID id, byte group, bool isController, bool isResponder, byte level, byte rampRate)
    {
        SceneMember member = new SceneMember(scene)
        {
            DeviceId = id,
            Group = group,
            IsController = isController,
            IsResponder = isResponder,
            Data1 = level,
            Data2 = rampRate,
            Data3 = group,
        };

        scene.AddMember(member);
    }

    /// <summary>
    /// Remove a scene member from a scene and remove corresponding links
    /// This overload is primarily used by unit-tests
    /// </summary>
    /// <param name="memberId"></param>
    /// <param name="memberGroup"></param>
    /// <param name="isController"></param>
    /// <param name="isResponder"></param>
    /// <returns>false if the scene member to remove did not exist</returns>
    public static bool RemoveSceneMember(this Scene scene, InsteonID memberId, byte memberGroup, bool isController, bool isResponder)
    {
        SceneMember member = new SceneMember(scene)
        {
            DeviceId = memberId,
            Group = memberGroup,
            IsController = isController,
            IsResponder = isResponder,
        };

        return scene.RemoveMember(member, removeLinks: true);
    }

    /// <summary>
    /// Replace a scene member of given deviceId and group by another one with different properties, same device
    /// </summary>
    /// <returns>false if member with this deviceId and group did not exist in the scene</returns>
    public static bool ReplaceSceneMember(this Scene scene, InsteonID deviceId, byte group, bool isController, bool isResponder, byte newGroup, bool newIsController, bool newIsResponder, byte newOnLevel, byte newRampRate)
    {
        SceneMember oldMember = new SceneMember(scene)
        {
            DeviceId = deviceId,
            Group = group,
            IsController = isController,
            IsResponder = isResponder,
        };

        SceneMember newMember = new SceneMember(scene)
        {
            DeviceId = deviceId,
            Group = newGroup,
            IsController = newIsController,
            IsResponder = newIsResponder,
            Data1 = newOnLevel,
            Data2 = newRampRate,
            Data3 = newGroup
        };
        return scene.ReplaceMember(oldMember, newMember);
    }
}
