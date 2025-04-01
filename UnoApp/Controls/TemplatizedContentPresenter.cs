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

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace UnoApp.Controls
{
    /// <summary>
    /// A version of the ContentPresenter that works with a ContentTemplateSelector.
    /// On WindowsAppSDK, ContentPresenter has a bug when used with a DataTemplate selector: 
    /// it does not call SelectTemplate on the DataTemplateSelector when the Content changes.
    /// This derived class fixes this issue (temporarily, until this is fixed). 
    /// </summary>
    public partial class TemplatizedContentPresenter : ContentPresenter
    {
#if WINDOWS // See above
        public TemplatizedContentPresenter()
        {
            RegisterPropertyChangedCallback(ContentProperty, OnContentChanged);
        }

        // Invoked when the value of the Content property changes. 
        protected void OnContentChanged(DependencyObject sender, DependencyProperty dp)
        {
            DataTemplateSelector dataTemplateSelector = this.ContentTemplateSelector as DataTemplateSelector;
            if (dataTemplateSelector != null)
            {
                this.ContentTemplate = dataTemplateSelector.SelectTemplate(this.Content, this);
            }
        }
#endif
    }
}
