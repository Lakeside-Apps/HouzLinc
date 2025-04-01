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

using HouzLinc.Views.Devices;
using HouzLinc.Dialogs;
using ViewModel.Scenes;
using Microsoft.UI.Xaml.Media.Animation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace HouzLinc.Views.Scenes;

public sealed partial class MemberListView : ContentControl
{
    public MemberListView()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Edit or remove the scene member identified by the DataContext of the sender, bringing up UI to edit/remove it
    /// This is designed to respond to edit button click on the scene member to edit/remove
    /// </summary>
    public async void EditSceneMemberAsync(Object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is MemberViewModel member)
        {
            if (fe.XamlRoot == null)
            {
                throw new NotImplementedException("XamlRoot is null");
            }

            var dialog = new EditSceneMemberDialog(fe.XamlRoot);

            // We edit a copy that will replace the original after the dialog returns
            var editedMember = new MemberViewModel(member);
            dialog.DataContext = editedMember;
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                member.Replace(editedMember);
            }
            else if (result == ContentDialogResult.Secondary)
            {
                await RemoveSceneMemberAsync(fe, member);
            }
        }
    }

    /// <summary>
    /// Remove the scene member after confirmation from the user
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public async void RemoveSceneMemberAsync(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is MemberViewModel member)
        {
            await RemoveSceneMemberAsync(fe, member);
        }
    }

    private async Task RemoveSceneMemberAsync(FrameworkElement fe, MemberViewModel member)
    {
        if (fe.XamlRoot == null)
        {
            throw new InvalidOperationException("XamlRoot is null");
        }

        var dialog = new ConfirmRemoveSceneMemberDialog(fe.XamlRoot);
        dialog.DataContext = member;
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            member.Remove();
        }
    }

    // Called when the user clicks on a member of the scene
    // Navigates to the item detail page or hub page if the member device is the hub
    private void OnItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is MemberViewModel mvm)
        {
            string navParam = string.Empty;
            var dvm = mvm.DeviceViewModel;
            if (dvm != null) 
            {
                navParam = dvm.Id.ToString();
                if (dvm.HasChannels)
                {
                    navParam += "/" + mvm.Group;
                }
            }
            (App.MainWindow.Content as AppShell)?.Navigate(typeof(DeviceDetailsPage), navParam, new DrillInNavigationTransitionInfo());
        }
    }
}
