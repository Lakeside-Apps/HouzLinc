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

using System.Xml.Serialization;
using Windows.Storage.Streams;
using Insteon.Model;

namespace Insteon.Serialization.Houselinc;

// This static class is a helper to serialize and deserialize the model to xml (in an houselinc.xml like format)
public static class HLSerializer
{
    private static XmlSerializer CreateXmlSerializer(Type type)
    {
        // This constructor throws a System.IO.FileNotFoundException exception that we can't handle
        // XmlSerializer serializer = new XmlSerializer(typeof(HLSettings));

        // We use this instead
        // (https://stackoverflow.com/questions/1127431/xmlserializer-giving-filenotfoundexception-at-constructor)
        var importer = new XmlReflectionImporter();
        var mapping = importer.ImportTypeMapping(type, null, null);
        XmlSerializer serializer = new XmlSerializer(mapping);

        return serializer;
    }

    public static async Task<House?> Deserialize(StorageFile file)
    {
        House? house = null;

        try
        {
            using (Stream stream = (await file.OpenAsync(FileAccessMode.Read)).AsStreamForRead())
            {
                house = await Task.Run(() =>
                {
                    XmlSerializer serializer = CreateXmlSerializer(typeof(HLSettings));
                    HLSettings ? hlSettings = serializer.Deserialize(stream) as HLSettings;
                    if (hlSettings != null)
                    {
                        var house = hlSettings.BuildModel();
                        house.OnDeserialized();
                        return house;
                    }
                    return null;
                });
            }
        }
        catch (Exception) { }

        return house;
    }

    public static async Task<House?> Deserialize(Stream stream)
    {
        House? house = null;

        try
        {
            house = await Task.Run(() =>
            {
                XmlSerializer serializer = CreateXmlSerializer(typeof(HLSettings));
                HLSettings? hlSettings = serializer.Deserialize(stream) as HLSettings;
                if (hlSettings != null)
                {
                    var house = hlSettings.BuildModel();
                    house.OnDeserialized();
                    return house;
                }
                return null;
            });
        }
        catch (Exception) { }

        return house;
    }

    // This semaphore is used to ensure we don't have multiple serializations at the same time
    static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

    public static async Task<bool> Serialize(StorageFile file, House house)
    {
        bool success = false;
        await semaphoreSlim.WaitAsync();
        try
        {
            success = true;
            HLSettings settings = new HLSettings(house);
            using (IRandomAccessStream stream = (await file.OpenAsync(FileAccessMode.ReadWrite)))
            {
                // Replace all data in the file
                stream.Size = 0;

                await Task.Run(() =>
                {
                    XmlSerializer serializer = CreateXmlSerializer(typeof(HLSettings));
                    serializer.Serialize(stream.AsStreamForWrite(), settings);
                });

                await stream.FlushAsync();
            }
        }
        catch (Exception)
        {
            success = false;
        }
        finally
        {
            semaphoreSlim.Release();
        }
        return success;
    }

    public static async Task<bool> Serialize(Stream stream, House house)
    {
        bool success = false;
        await semaphoreSlim.WaitAsync();
        try
        {
            success = true;
            HLSettings settings = new HLSettings(house);
            await Task.Run(() =>
            {
                XmlSerializer serializer = CreateXmlSerializer(typeof(HLSettings));
                serializer.Serialize(stream, settings);
            });

            await stream.FlushAsync();
        }
        catch (Exception)
        {
            success = false;
        }
        finally
        {
            semaphoreSlim.Release();
        }
        return success;
    }
}
