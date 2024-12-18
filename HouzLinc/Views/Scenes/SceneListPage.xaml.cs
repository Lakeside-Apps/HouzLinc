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

using System.Collections.ObjectModel;
using ViewModel.Scenes;
using HouzLinc.Views.Base;
using HouzLinc.Dialogs;
using ViewModel.Settings;
using ViewModel.Base;

namespace HouzLinc.Views.Scenes;

/// <summary>
/// Base class for master detail style pages
/// Used in the XAML as the main page control type
/// </summary>
public abstract partial class SceneListPageBase : ItemListPage<SceneViewModel, SceneDetailsPage>
{
}

/// <summary>
/// Scene master detail page
/// </summary>
public sealed partial class SceneListPage : SceneListPageBase
{
    // TODO: this should be made a string resource and fetch from the code and XAML as needed
    public string PageHeaderProperty => PageHeader;
    public const string PageHeader = "Scenes";

    public SceneListPage()
    {
        this.InitializeComponent();
    }

    // Control accessors for base page
    protected override ListView ItemListView => SceneListView;
    protected override ContentControl ItemDetailsPresenter => SceneDetailsPresenter;
    protected override VisualStateGroup PageSizeVisualStateGroup => PageSizeStatesGroup;
    protected override VisualStateGroup MasterDetailVisualStateGroup => MasterDetailsStatesGroup;

    // Main view model
    protected override SceneListViewModel ItemListViewModel => 
        itemsListViewModel ??= SceneListViewModel.Create(Holder.House.Scenes)
            .ApplyFilterAndSortOrderFromSettings();
    private SceneListViewModel? itemsListViewModel;
    private SceneListViewModel sceneListViewModel => (ItemListViewModel as SceneListViewModel)!;

    private async void AddSceneBtnClick(object sender, RoutedEventArgs e)
    {
        if (XamlRoot == null)
        {
            throw new InvalidOperationException("XamlRoot is null");
        }
        NewSceneDialog dialog = new NewSceneDialog(this.XamlRoot, sceneListViewModel);
        await dialog.ShowAsync();
    }

    private async void RemoveSceneBtnClick(object sender, RoutedEventArgs e)
    {
        if (SelectedItem != null)
        {
            var confirmDialog = new ConfirmDialog(XamlRoot)
            {
                // TODO: localize
                Title = $"About to Remove Scene: {SelectedItem.DisplayName}",
                Content = $"Are you sure you want to remove this scene?"
            };

            if (await confirmDialog.ShowAsync())
            {
                // Remove this scene and unselect it
                sceneListViewModel.RemoveScene(SelectedItem.Id);
                SelectedItem = null;
            }
        }
    }
}
