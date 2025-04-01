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

using UnoApp.Views.Base;
using UnoApp.Dialogs;
using ViewModel.Scenes;
using ViewModel.Settings;

namespace UnoApp.Views.Scenes;

/// <summary>
/// Base class for the scene details page 
/// Used in the XAML as the main page control type
/// </summary>
public abstract partial class SceneDetailsPageBase : ItemDetailsPage<SceneViewModel, SceneDetailsPage>
{
}

/// <summary>
/// Scene detail page
/// </summary>
public sealed partial class SceneDetailsPage : SceneDetailsPageBase
{
    public SceneDetailsPage()
    {
        this.InitializeComponent();
    }

    protected override SceneViewModel? GetOrCreateItemByKey(string itemKey)
    {
        return SceneViewModel.GetOrCreateItemByKey(Holder.House, itemKey);
    }

    protected override ContentControl ItemDetailsPresenter => SceneDetailsPresenter;

    // TODO: this should be made a string resource and fetch from the code and XAML as needed
    public string PageHeaderProperty => PageHeader;
    public const string PageHeader = "Scene";

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private async void AddSceneBtnClick(object sender, RoutedEventArgs e)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        // Currently not implemented - Detail page does not have Add button
        throw new NotImplementedException();
    }

    private async void RemoveSceneBtnClick(object sender, RoutedEventArgs e)
    {
        if (ItemViewModel != null)
        {
            var confirmDialog = new ConfirmDialog(XamlRoot)
            {
                // TODO: localize
                Title = $"About to Remove Scene: {ItemViewModel.DisplayName}",
                Content = $"Are you sure you want to remove this scene?"
            };

            if (await confirmDialog.ShowAsync())
            {
                // Remove Scene from the model and navigate away from this detail page
                ItemViewModel.RemoveScene();
                (App.MainWindow.Content as AppShell)?.GoBackNoContext();
            }
        }
    }
}
