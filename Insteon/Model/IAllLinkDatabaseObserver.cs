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

using System.Runtime.CompilerServices;

namespace Insteon.Model;

/// <summary>
/// Abstract class to observe change notifications from an AllLinkDatabase
/// </summary>
public interface IAllLinkDatabaseObserver
{
    public void AllLinkDatabaseSyncStatusChanged(Device? device);
    public void AllLinkDatabasePropertiesChanged(Device? device);
    public void AllLinkDatabaseCleared(Device? device);
    public void AllLinkRecordAdded(Device? device, AllLinkRecord record);
    public void AllLinkRecordRemoved(Device? device, AllLinkRecord record);
    public void AllLinkRecordReplaced(Device? device, AllLinkRecord recordToReplace, AllLinkRecord newRecord);
}
