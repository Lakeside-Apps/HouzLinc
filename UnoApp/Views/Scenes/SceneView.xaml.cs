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

using UnoApp.Dialogs;
using ViewModel.Scenes;
using ViewModel.Base;

namespace UnoApp.Views.Scenes;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SceneView : ContentControl
{
    public SceneView()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Add new member, bringing up UI to create it
    /// </summary>
    public async void AddNewSceneMemberAsync(Object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is SceneViewModel svm)
        {
            var dialog = new NewSceneMemberDialog(fe.XamlRoot);
            var newMember = svm.SceneMembers.CreateNewSceneMemberWithLastUsedValue();
            dialog.DataContext = newMember;

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                svm.SceneMembers.StoreSceneMemberLastUsedValues(newMember);
                svm.SceneMembers.AddNewMember(newMember);

                // AddNewMember created a different view model to add to the view model list,
                // or might not have created any if it already existed.
                // Either way, retrieve it from the list
                var newMemberIdx = svm.SceneMembers.IndexOf(newMember);
                if (newMemberIdx != -1)
                {
                    newMember = svm.SceneMembers[newMemberIdx];

                    // Delay the scrolling a bit to let the UI catch up
                    UIScheduler.Instance.AddJob("Showing newly added scene", () =>
                    {
                        // To bring the new member into view, this is the scroller we will scroll 
                        var scrollViewer = XAMLHelpers.FindVisualAncestorByName(fe, "SceneScrollViewer") as ScrollViewer;
                        if (scrollViewer != null)
                        {
                            // And this is the grid containing the member item.
                            // The grid itself does not scroll, it rellies on SceneScrollViewer to scroll.
                            var gridView = XAMLHelpers.FindElementByName(scrollViewer, "MemberGridView") as GridView;
                            if (gridView != null)
                            {
                                gridView.SelectedItem = newMember;
                                XAMLHelpers.ScrollItemIntoView(scrollViewer, gridView, newMember, XAMLHelpers.ScrollIntoViewAlignment.Center);
                            }
                        }
                        return true;
                    }, delay: TimeSpan.FromMilliseconds(100));

                }
            }
        }
    }
}
