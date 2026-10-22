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
using Insteon.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Insteon;

[TestClass]
public sealed class TestAllLinkRecord
{
    public TestAllLinkRecord()
    {
    }

    [TestMethod]
    public void TestAllLinkDatabase_CreateFromHexString()
    {
        HexString hexString = new HexString("225D703420E811512F0000010F8F01A20322F3C9B21C01");
        InsteonExtendedMessage message = new InsteonExtendedMessage(hexString);
        try
        {
            AllLinkRecord allLinkRecord = new AllLinkRecord(message);
        }
        catch (AllLinkRecord.InvalidAllLinkRecordException)
        {
            return;
        }
        Assert.Fail("Checksum should have been determined to be invalid");

        //Assert.AreEqual(allLinkRecord.DestID, new InsteonID("34.20.E8"));
        //Assert.AreEqual(allLinkRecord.);
    }
}
