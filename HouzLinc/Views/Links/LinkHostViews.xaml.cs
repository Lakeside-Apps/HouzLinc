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
using HouzLinc.Dialogs;
using HouzLinc.Views.Devices;
using HouzLinc.Views.Hub;
using Microsoft.UI.Xaml.Media.Animation;
using ViewModel.Links;

namespace HouzLinc.Views.Links;

// Kinds of links this view can present
public enum LinkType 
{
    Controller,
    Responder,
    Both
}

// Template selector based on the kind of links
public sealed class LinkListViewTemplateSelector : DataTemplateSelector
{
    //  Returns the proper template to view the details of the given item
    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        if (item != null)
        {
#if DESKTOP || __WASM__
            // On Desktop or WASM, "container" is the ContentPresenter.
            // Either its TemplatedParent or Parent contains the LinkHostView with the template resource.
            // Note that TemplatedParent and Parent can sometimes be null (not sure exactly when).
            // We ignore the call and return null in that case. We will get called back.
            var lhv = (container is ContentPresenter cp) ? (cp.TemplatedParent ?? cp.Parent) as LinkHostViews : null;
#else
            // On WindowsAppSDK, WASM, Android, iOS, "container" is the LinkHostViews content control
            var lhv = container as LinkHostViews;
#endif
            if (lhv != null)
            {
                string resourceName;
                switch (lhv.LinkType)
                {
                    case LinkType.Controller:
                        resourceName = "DeviceControllerLinkListView";
                        break;
                    case LinkType.Responder:
                        resourceName = "DeviceResponderLinkListView";
                        break;
                    default:
                        resourceName = "DeviceLinkListView";
                        break;
                }
                return (DataTemplate)lhv.Resources[resourceName];
            }
        }
        return null;
    }
}

public sealed partial class LinkHostViews : ContentControl
{
    public LinkHostViews()
    {
        this.InitializeComponent();
    }

    // Which kind of links this view can present
    public LinkType LinkType { get; set; }

    /// <summary>
    /// Called when the user clicks on an link in the list
    /// Navigates to the item detail page or hub page if the link is to the hub
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is LinkViewModel lvm)
        {
            string navParam = string.Empty;
            if (!lvm.DestDeviceIsHub)
            {
                navParam = lvm.DestDeviceId.ToString();
                if (lvm.DestDeviceHasChannels)
                {
                    navParam += $"/{(lvm.IsResponder? lvm.Group : lvm.ResponderDestDeviceFirstChannelId)}";
                }
                (App.MainWindow.Content as AppShell)?.Navigate(typeof(DeviceDetailsPage), navParam, new DrillInNavigationTransitionInfo());
            }
            else
            {
                navParam = (lvm.IsResponder ? lvm.Group : lvm.ResponderDestDeviceFirstChannelId).ToString();
                (App.MainWindow.Content as AppShell)?.Navigate(typeof(HubChannelDetailsPage), navParam, new DrillInNavigationTransitionInfo());
            }
        }
    }

    /// <summary>
    /// Add new controller link, bringing up UI to create it
    /// </summary>
    public async void AddNewControllerLinkAsync(Object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is LinkHostViewModel lhvm)
        {
            var dialog = new NewLinkDialog(fe.XamlRoot);
            var newLink = lhvm.CreateNewControllerLink();
            dialog.DataContext = newLink;

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                lhvm.ControllerLinks.AddNewLink(newLink);
            }
        }
    }

    /// <summary>
    /// Add new responder link, bringing up UI to create it
    /// </summary>
    public async void AddNewResponderLinkAsync(Object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is LinkHostViewModel lhvm)
        {
            var dialog = new NewLinkDialog(fe.XamlRoot);
            var newLink = lhvm.CreateNewResponderLink();
            dialog.DataContext = newLink;

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                lhvm.ResponderLinks.AddNewLink(newLink);
            }
        }
    }

    /// <summary>
    /// Add a new link to either controllers or responders, depending on group information in the data context
    /// To be used by combined lists of controllers and responders
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public async void AddNewLinkAsync(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is LinkGroup linkGroup)
        {
            var dialog = new NewLinkDialog(fe.XamlRoot);
            var lhvm = linkGroup.LinkHost;
            var newLink = linkGroup.IsController ? lhvm.CreateNewControllerLink() : lhvm.CreateNewResponderLink();
            dialog.DataContext = newLink;

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                lhvm.ResponderLinks.AddNewLink(newLink);
            }
        }
    }

    /// <summary>
    /// Edit or remove the link identified by the DataContext of the sender, bringing up UI to edit/remove it
    /// This is designed to respond to edit button click on the link to edit/remove
    /// </summary>
    public async void EditLinkAsync(Object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is LinkViewModel link)
        {
            var dialog = new EditLinkDialog(fe.XamlRoot)
            {
                // TODO: consider binding Title to a property of LinkViewModel instead
                Title = link.IsController ? "Modify Responder" : "Modify Controller"
            };

            // We edit a copy that will replace the original after the dialog returns
            var editedLink = new LinkViewModel(link);
            dialog.DataContext = editedLink;
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                link.Replace(editedLink);
            }
            else if (result == ContentDialogResult.Secondary)
            {
                await RemoveLinkAsync(fe, link);
            }
        }
    }

    /// <summary>
    /// Remove the link after confirmation by the user
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public async void RemoveLinkAsync(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is LinkViewModel link)
        {
            await RemoveLinkAsync(fe, link);
        }
    }

    private async Task RemoveLinkAsync(FrameworkElement fe, LinkViewModel link)
    {
        var dialog = new ConfirmRemoveLinkDialog(fe.XamlRoot);

        dialog.DataContext = link;
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            link.Remove();
        }
    }
}
