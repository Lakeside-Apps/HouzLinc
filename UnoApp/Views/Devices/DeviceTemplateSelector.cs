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

using System.Diagnostics;
using ViewModel.Devices;

namespace HouzLinc.Views.Devices;

public sealed class DeviceTemplateSelector : DataTemplateSelector
{
    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        if (item != null && item is DeviceViewModel dvm)
        {
#if DESKTOP || __WASM__
            // On Desktop and WASM, "container" is the ContentPresenter.
            // Either its TemplatedParent or Parent contains the DeviceView with the template resource.
            // Note that TemplatedParent and Parent can sometimes be null (not sure exactly when).
            // We ignore the call and return null in that case. We will get called back.
            var dv = (container is ContentPresenter cp) ? (cp.TemplatedParent ?? cp.Parent) as DeviceView : null;
#else
            // On WindowsAppSDK, Android, iOS, "container" is the DeviceViews content control
            var dv = container as DeviceView;
#endif
            if (dv != null)
            {
                return (DataTemplate)dv.Resources[dvm.DeviceTemplateName];
            }
        }
        return null;
    }}