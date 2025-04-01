/* Copyright 2022 Christian Fortini7

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

using UnoApp.Views.Base;
using System.ComponentModel;
using ViewModel.Hub;
using ViewModel.Settings;

namespace UnoApp.Views.Hub;

/// <summary>
/// Base class for the hub scene details page 
/// Used in the XAML as the main page control type
/// </summary>
public abstract partial class HubChannelDetailsPageBase : ItemDetailsPage<HubChannelViewModel, HubChannelListPage>
{
}

/// <summary>
/// Hub scene detail page
/// </summary>
[Bindable(true)]
public sealed partial class HubChannelDetailsPage : HubChannelDetailsPageBase
{
    // TODO: this should be made a string resource and fetch from the code and XAML as needed
    public string PageHeaderProperty => PageHeader;
    public const string PageHeader = "Hub Channel";

    public HubChannelDetailsPage()
    {
        this.InitializeComponent();
    }

    protected override HubChannelViewModel? GetOrCreateItemByKey(string itemKey)
    {
        return HubChannelViewModel.GetOrCreateItemByKey(Holder.House, itemKey);
    }

    protected override ContentControl ItemDetailsPresenter => HubChannelDetailsPresenter;
}
