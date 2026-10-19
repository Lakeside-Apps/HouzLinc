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
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace UnitTests.Insteon;

internal class FakeHexStream : CircularBufferHexStream
{
    public FakeHexStream() : base(200)
    {
    }

    protected async override Task<string> GetData()
    {
        return await Task<string>.Run(() =>
        {
            string contentBuffer = string.Empty;
            switch (_contentSequence)
            {
                case 0:
                    {
                        contentBuffer = "0123456789ABCDEF";
                        break;
                    }
                case 1:
                    {
                        contentBuffer = "456789AB00000123";
                        break;
                    }
            }
            _contentSequence++;
            return contentBuffer;
        });
    }

    protected async override Task ClearBuffer()
    {
        await Task.Run(() =>
        {
            Reset();
        });
    }

    int _contentSequence;
}

[TestClass]
public sealed class TestFakeHexStream
{
    [TestMethod]
    public async Task TestFakeHexStream_BasicRead()
    {
        FakeHexStream xcb = new FakeHexStream();
        byte[] data = (await xcb.Read(3)).Bytes(1, 3);
        byte[] expected = { 0x01, 0x23, 0x45 };
        Assert.AreEqual(data.ToString(), expected.ToString());
    }

    [TestMethod]
    public async Task TestFakeHexStream_ConsecutiveReadsWithAdvance()
    {
        FakeHexStream xcb = new FakeHexStream();

        byte[] data1 = (await xcb.Read(3)).Bytes();
        byte[] expected1 = { 0x01, 0x23, 0x45 };
        Assert.AreEqual(data1.ToString(), expected1.ToString());
        xcb.Advance();

        byte[] data2 = (await xcb.Read(3)).Bytes();
        byte[] expected2 = { 0x67, 0x89, 0xAB };
        Assert.AreEqual(data2.ToString(), expected2.ToString());
    }

    [TestMethod]
    public async Task TestFakeHexStream_ConsecutiveReadsWithoutAdvance()
    {
        FakeHexStream xcb = new FakeHexStream();

        byte[] data1 = (await xcb.Read(3)).Bytes();
        byte[] expected1 = { 0x01, 0x23, 0x45 };
        Assert.AreEqual(data1.ToString(), expected1.ToString());

        byte[] data2 = (await xcb.Read(3)).Bytes();
        Assert.AreEqual(data2.ToString(), expected1.ToString());
    }

    [TestMethod]
    public async Task TestFakeHexStream_ReadAndWrapAround()
    {
        FakeHexStream xcb = new FakeHexStream();

        byte[] data = (await xcb.Read(6)).Bytes();
        byte[] expected = { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB };
        Assert.AreEqual(data.ToString(), expected.ToString());
        xcb.Advance();

        data = (await xcb.Read(6)).Bytes();
        Assert.AreEqual(data.ToString(), expected.ToString());
        xcb.Advance();

        data = (await xcb.Read(2)).Bytes();
        byte[] expected2 = { 0, 0 };
        Assert.AreEqual(data.ToString(), expected2.ToString());
        xcb.Advance();
    }
}
