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

using Microsoft.UI.Xaml.Media.Animation;
using ViewModel.Base;
using ViewModel.Settings;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Controls;

namespace UnoApp.Views.Base;

/// <summary>
/// Item details page.
/// This page can be navigated to from a list of items
/// </summary>
public abstract partial class ItemDetailsPage<ItemViewModelType, ItemListPage> : PageWithViewModels, INotifyPropertyChanged
    where ItemViewModelType : ItemViewModel
{
    public ItemDetailsPage()
    {
        AddViewModel(StatusBarViewModel);
    }

    /// <summary>
    /// View model of the item this page is showing
    /// Set by OnNavigatedTo
    /// </summary>
    public ItemViewModelType? ItemViewModel
    {
        get => itemViewModel;
        private set
        {
            if (itemViewModel != value)
            {
                itemViewModel = value;
                OnPropertyChanged();
            }
        }
    }
    ItemViewModelType? itemViewModel;

    // Get or create an item view model for the given item key
    // Implemented by derived classes
    protected abstract ItemViewModelType? GetOrCreateItemByKey(string itemKey);

    // View model for the status bar on this page
    protected StatusBarViewModel StatusBarViewModel => StatusBarViewModel.Instance;

    // Control holding the details of the presented item
    protected abstract ContentControl ItemDetailsPresenter { get; }

    /// <summary>
    /// Data binding support
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged = delegate { };
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        // Raise the PropertyChanged event, passing the name of the property whose value has changed.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected override void OnPageLoaded()
    {
        base.OnPageLoaded();
        App.MainWindow.SizeChanged += OnWindowSizeChanged;
    }

    protected override void OnPageUnloaded()
    {
        App.MainWindow.SizeChanged -= OnWindowSizeChanged;
        base.OnPageUnloaded();
    }

    // We are navigating to this page (called before Loaded)
    // See https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.page.onnavigatedto?view=winrt-26100
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        string? viewModelParam = null;
        var navParams = e.Parameter as string;
        if (navParams != null)
        {
            var navParamArray = navParams.Split("/");
            if (navParamArray != null && navParamArray.Length > 0)
            {
                // The navigation parameter before the "/" is the item key
                ItemViewModel = GetOrCreateItemByKey(navParamArray[0]);
            }

            // If there is a second parameter after the "/", it's for the item view model
            if (navParamArray?.Length > 1)
            {
                viewModelParam = navParamArray[1];
            }
        }

        // Ensure that the view model knows it is active before consuming the navigation parameter
        if (ItemViewModel != null)
        {
            ItemViewModel.IsActive = true;
        }

        // Now, let the view model consume the navigation parameter
        if (viewModelParam != null)
        {
            ItemViewModel?.SetNavigationParameter(viewModelParam);
        }
    }

    // We are navigating away from this page
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        if (e.NavigationMode != NavigationMode.Back)
        {
            // We are navigating forward.
            // Modify the last entry in the back stack to point to the this item,
            // plus any additional local context (e.g., selected device channel id),
            // so that we get back to that item on navigation back
            var backStack = Frame.BackStack;
            if (backStack.Count > 0 && ItemViewModel != null)
            {
                var firstBackEntry = backStack[backStack.Count - 1];
                var navParams = ItemViewModel.ItemKey;
                var itemNavParam = ItemViewModel.GetNavigationParameter();
                if (itemNavParam != null)
                {
                    navParams += "/" + itemNavParam;
                }
                var newEntry = new PageStackEntry(firstBackEntry.SourcePageType, navParams, firstBackEntry.NavigationTransitionInfo);
                backStack[backStack.Count - 1] = newEntry;
            }
        }

        if (ItemViewModel != null)
        {
            ItemViewModel.IsActive = false;
        }

        ItemViewModel = null;
    }

    private void NavigateBackForWideState(bool useTransition)
    {
        // Evict this page from the cache as we may not need it again.
        NavigationCacheMode = NavigationCacheMode.Disabled;

        if (useTransition)
        {
            Frame.GoBack(new EntranceNavigationTransitionInfo());
        }
        else
        {
            Frame.GoBack(new SuppressNavigationTransitionInfo());
        }
    }

    private bool ShouldGoToWideState()
    {
        return App.MainWindow.Bounds.Width >= 960;
    }

    protected void OnWindowSizeChanged(object sender, Microsoft.UI.Xaml.WindowSizeChangedEventArgs e)
    {
        if (ShouldGoToWideState())
        {
            // Make sure we are no longer listening to window change events.
            App.MainWindow.SizeChanged -= OnWindowSizeChanged;

            // We shouldn't see this page since we are in "wide master-detail" mode.
            NavigateBackForWideState(useTransition: false);
        }
    }
}
