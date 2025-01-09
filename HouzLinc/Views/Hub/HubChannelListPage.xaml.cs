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

using HouzLinc.Views.Base;
using ViewModel.Hub;
using ViewModel.Settings;
using Microsoft.UI.Xaml.Media.Animation;
using HouzLinc.Views.Settings;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace HouzLinc.Views.Hub;

/// <summary>
/// Base class for the Hub scene master detail page 
/// Used in the XAML as the main page control type
/// </summary>
public abstract partial class HubChannelListPageBase : ItemListPage<HubChannelViewModel, HubChannelDetailsPage>
{
}

/// <summary>
/// Hub scene master detail page
/// </summary>
public sealed partial class HubChannelListPage : HubChannelListPageBase
{
    // TODO: this should be made a string resource and fetch from the code and XAML as needed
    public string PageHeaderProperty => PageHeader;
    public const string PageHeader = "Hub";

    public HubChannelListPage()
    {
        this.InitializeComponent();
    }

    // Main view model
    protected override HubChannelListViewModel ItemListViewModel => itemsListViewModel ??= HubChannelListViewModel.Create();
    private HubChannelListViewModel? itemsListViewModel;
    private HubChannelListViewModel hubChannelListViewModel => (ItemListViewModel as HubChannelListViewModel)!;
    private SettingsViewModel settingsViewModel => SettingsViewModel.Instance;

    // Control accessors for base page
    protected override ListView ItemListView => ChannelListView;
    protected override ContentControl ItemDetailsPresenter => HubChannelDetailsPresenter;
    protected override VisualStateGroup PageSizeVisualStateGroup => PageSizeStatesGroup;
    protected override VisualStateGroup MasterDetailVisualStateGroup => MasterDetailsStatesGroup;

    private void NavigateToHubSettings(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
    {
        (App.MainWindow.Content as AppShell)?.Navigate(typeof(HubSettingsPage), null, new DrillInNavigationTransitionInfo());

    }
}
