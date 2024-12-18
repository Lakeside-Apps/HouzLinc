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

using System.Text.RegularExpressions;
using Insteon.Model;
using Insteon.Serialization.Houselinc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Insteon;

public static class ModelHolderForTest
{
    /// <summary>
    /// Load a test file from an ms-appx:///Models folder
    /// </summary>
    /// <param name="subfolder"></param>
    /// <param name="filename"></param>
    /// <returns></returns>
    public static async Task<House?> LoadFromFile(string subfolder, string filename)
    {
        var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Models/{subfolder}/{filename}.xml"));

        // Deserialize the model
        return await HLSerializer.Deserialize(file);
    }

    /// <summary>
    /// Save test file to an ms-appdata:///temp folder
    /// </summary>
    /// <param name="filename"></param>
    /// <returns>Full path of the file</returns>
    public static async Task<string> SaveToFile(string subfolder, string filename, House house)
    {
        var tempFolder = ApplicationData.Current.TemporaryFolder;
        var testFolder = await tempFolder.CreateFolderAsync("Test", CreationCollisionOption.OpenIfExists);
        var testSubFolder = await testFolder.CreateFolderAsync(subfolder, CreationCollisionOption.OpenIfExists);
        var file = await testSubFolder.CreateFileAsync(filename + ".xml", CreationCollisionOption.ReplaceExisting);
        Assert.IsTrue(await HLSerializer.Serialize(file, house));
        return file.Path;
    }

    /// <summary>
    /// Compare a test file stored in the ms-appdata:///temp folder with a ref file stored in the ms-appx:///Refs folder
    /// </summary>
    /// <param name="subfolder">subfolder under these folders where the files are</param>
    /// <param name="testFilename">name of the test file without extension</param>
    /// <returns>null if files are the same, description of first difference otherwise</returns>
    public static async Task<string?> CompareFiles(string subfolder, string testFilename)
    {
        IList<string>? refLines = null;
        IList<string>? testLines = null;

        string pathRefFile = string.Empty;
        string pathTestFile = string.Empty;

        Uri uriRefFile = new Uri($"ms-appx:///Refs/{subfolder}/{testFilename}.xml");
        try
        {
            var refFile = await StorageFile.GetFileFromApplicationUriAsync(uriRefFile);
            pathRefFile = refFile.Path;
            refLines = await FileIO.ReadLinesAsync(refFile);
        }
        catch (Exception)
        {
            return $"Can't open ref file {uriRefFile}";
        }

        Uri uriTestFile = new Uri($"ms-appdata:///temp/Test/{subfolder}/{testFilename}.xml");
        try
        {
            var testFile = await StorageFile.GetFileFromApplicationUriAsync(uriTestFile);
            pathTestFile = testFile.Path;
            testLines = await FileIO.ReadLinesAsync(testFile);
        }
        catch (Exception)
        {
            return $"Can't open Test file {uriTestFile}";
        }

        string? result = null;
        int count = refLines.Count < testLines.Count ? refLines.Count : testLines.Count;
        int i = 0;
        for (; i < count; i++)
        {
            // Remove LastUpdate and notes/added attribute values as they are time stamps that change everytime 
            // Also remove uid values as they will change every run for newly created records/scene members.
            string pattern = @"(.*lastUpdate=\s*|.*notes added=\s*|.*uid=\s*)""[^""]*""(.*)";
            string replacement = "$1\"...\"$2";
            refLines[i] = Regex.Replace(refLines[i], pattern, replacement);
            testLines[i] = Regex.Replace(testLines[i], pattern, replacement);

            if (testLines[i] != refLines[i])
            {
                result = "\r\n" +
                    "Ref File:  " + pathRefFile + "\r\n" + 
                    "Test File: " + pathTestFile + "\r\n" +
                    "Differ line " + i + " \r\n" +
                    "Ref: " + refLines[i] + "\r\n" +
                    "Test:" + testLines[i];
                break;
            }
        }

        if (i == count)
        {
            if (i < refLines.Count)
            {
                result = "Ref file has more lines than test file";

            }
            else if (i < testLines.Count)
            {
                result = "Test file has more lines than ref file";
            }
        }

        return result;
    }
}
