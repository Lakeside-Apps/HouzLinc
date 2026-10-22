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
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace UnitTests.Insteon;

[TestClass]
public class TestInsteonID
{
    [TestMethod]
    public void TestInsteonID_FromCommandString()
    {
        string test = "23e45a00003e567b";
        InsteonID id = new InsteonID(test);
        Assert.AreEqual(id.ToString(), "23.E4.5A");
    }

    [TestMethod]
    public void TestInsteonID_FromUserString()
    {
        string test = "23.e4.5a";
        InsteonID id = new InsteonID(test);
        Assert.AreEqual(id.ToString(), "23.E4.5A");
    }

    [TestMethod]
    public void TestInsteonID_NullFromUserString()
    {
        string test = "00.00.00";
        InsteonID id = new InsteonID(test);
        Assert.AreEqual(id, InsteonID.Null);
    }

    [TestMethod]
    public void TestInsteonID_NullFromUserString2()
    {
        string test = "0.0.0";
        InsteonID id = new InsteonID(test);
        Assert.AreEqual(id, InsteonID.Null);
    }

    [TestMethod]
    public void TestInsteonID_NullFromCommandString()
    {
        string test = "000000";
        InsteonID id = new InsteonID(test);
        Assert.AreEqual(id, InsteonID.Null);
    }

    [TestMethod]
    public void TestInsteonID_InvalidCommandString1()
    {
        try
        {
            string test = "1R.23.85";
            InsteonID id = new InsteonID(test);
            Assert.Fail("Expected exception: Invalid Insteon ID");
        }
        catch (Exception e)
        {
            Assert.AreEqual(e.Message, "Invalid Insteon ID");
        }
    }

    [TestMethod]
    public void TestInsteonID_InvalidCommandString2()
    {
        try
        {
            string test = "3E4g56";
            InsteonID id = new InsteonID(test);
            Assert.Fail("Expected exception: Invalid Insteon ID");
        }
        catch (Exception e)
        {
            Assert.AreEqual(e.Message, "Invalid Insteon ID");
        }
    }

    [TestMethod]
    public void TestInsteonID_InvalidCommandString3()
    {
        try
        {
            string test = "22F3E";
            InsteonID id = new InsteonID(test);
            Assert.Fail("Expected exception: Invalid Insteon ID");
        }
        catch (Exception e)
        {
            Assert.AreEqual(e.Message, "Invalid Insteon ID");
        }
    }

    [TestMethod]
    public void TestInsteonID_InvalidUserString1()
    {
        try
        {
            string test = "3E:4a.56";
            InsteonID id = new InsteonID(test);
            Assert.Fail("Expected exception: Invalid Insteon ID");
        }
        catch (Exception e)
        {
            Assert.AreEqual(e.Message, "Invalid Insteon ID");
        }
    }

    [TestMethod]
    public void TestInsteonID_InvalidUserString2()
    {
        try
        {
            string test = "3E.4g.56";
            InsteonID id = new InsteonID(test);
            Assert.Fail("Expected exception: Invalid Insteon ID"    );
        }
        catch (Exception e)
        {
            Assert.AreEqual(e.Message, "Invalid Insteon ID");
        }
    }

    [TestMethod]
    public void TestInsteonID_Equality()
    {
        InsteonID id1 = new InsteonID("2e5a78");
        InsteonID id2 = new InsteonID("2e.5A.78");
        Assert.AreEqual(id1, id2);
    }

    [TestMethod]
    public void TestInsteonID_Inequality()
    {
        Assert.IsTrue(new InsteonID("20.30.40") < new InsteonID("21.0.01"));
        Assert.IsTrue(new InsteonID("20.30.40") < new InsteonID("20.31.40"));
        Assert.IsTrue(new InsteonID("20.30.40") < new InsteonID("20.30.41"));
        Assert.IsFalse(new InsteonID("20.30.40") < new InsteonID("20.30.40"));

        Assert.IsTrue(new InsteonID("20.30.40") > new InsteonID("19.30.40"));
        Assert.IsTrue(new InsteonID("20.30.40") > new InsteonID("20.29.40"));
        Assert.IsTrue(new InsteonID("20.30.40") > new InsteonID("20.30.39"));
        Assert.IsFalse(new InsteonID("20.30.40") > new InsteonID("20.30.40"));
    }
}
