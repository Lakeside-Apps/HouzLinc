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

using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media.Animation;
using System.Collections.ObjectModel;
using ViewModel.Base;
using ViewModel.Settings;

namespace HouzLinc.Views.Base;

/// <summary>
/// Item list page
/// This page is a master detail style list of items
/// It support wide and narrow modes:
/// - in wide mode, both list and item details are presented
/// - In narrow mode, clicking on the list navigate to the detail page for a given item
/// This class should be derived for each type of master detail page
/// The derived class should have the following elements:
/// - ItemListView: the ListView for the items
/// - ItemDetailsPresenter: a Control presenter to display item details in master detail mode
/// - PageSizeVisualStateGroup: a VisualStateGroup defining states:
///     NarrowState
///     WideState
/// - MasterDetailsVisualStateGroup: a VisualStateGroup defining states:
///     MasterState
///     MasterDetailsState
/// </summary>
/// <typeparam name="ItemViewModelType">type of items</typeparam>
/// <typeparam name="ItemDetailsPage">type of the details page used when the view is too narrow</typeparam>
public abstract partial class ItemListPage<ItemViewModelType, ItemDetailsPage> : PageWithViewModels
    where ItemViewModelType : ItemViewModel
{
    public ItemListPage()
    {
        AddViewModel(ItemListViewModel);
        AddViewModel(StatusBarViewModel);
    }

    // Main view model - provided by derived pages
    protected abstract ItemListViewModel<ItemViewModelType> ItemListViewModel { get; }

    // View model for the status bar on this page
    protected StatusBarViewModel StatusBarViewModel => StatusBarViewModel.Instance;

    // Currently selected item in the main view model
    protected ItemViewModelType? SelectedItem
    {
        get => ItemListViewModel.SelectedItem;
        set => ItemListViewModel.SelectedItem = value;
    }

    // Provided by derived pages

    // Controls on the associated page
    protected abstract ListView ItemListView { get; }
    protected abstract ContentControl ItemDetailsPresenter { get; }

    // Visual state group tracking the size of the associated page: WideState or NarrowState
    protected abstract VisualStateGroup PageSizeVisualStateGroup { get; }

    // Visual state group tracking the master detail states: MasterState, MasterDetailsState, ExtendedSelectionState, MultipleSelectionState
    protected abstract VisualStateGroup MasterDetailVisualStateGroup { get; }

    public bool IsMasterDetailState => MasterDetailVisualStateGroup.CurrentState != null &&
        MasterDetailVisualStateGroup.CurrentState.Name == "MasterDetailsState";

    // This is to track the current visual state of PageSizeStatesGroup
    // because the OldState passed to OnCurrentStateChange is not always correct
    private string? pageSizeCurrentState;

    // Used to pass navigation parameters between OnNavigatedTo and OnLoaded
    private string? navigationParameters;

    // Page loaded event handler
    protected override void OnPageLoaded()
    {
        base.OnPageLoaded();

        // Transition to the appropriate visual state (Master or MasterDetail)
        if (PageSizeVisualStateGroup.CurrentState != null)
        {
            // Keep ItemListViewModel in sync with the PageSizeVisualStateGroup
            // and trigger the correct MasterDetailsStatesGroup
            pageSizeCurrentState = PageSizeVisualStateGroup.CurrentState.Name;
            if (pageSizeCurrentState == "NarrowState")
            {
                ItemListViewModel.PageSizeStateChanged(isNarrow: true);
                VisualStateManager.GoToState(this, "MasterState", true);
            }
            else if (pageSizeCurrentState == "WideState")
            {
                ItemListViewModel.PageSizeStateChanged(isNarrow: false);
                VisualStateManager.GoToState(this, "MasterDetailsState", true);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        // Restore last selected item plus any additional context (e.g., presented channel)
        // either from navigation parameter or, as a fallback, from the "last used" value in the Settings Store
        // In the absence of either, select first item
        if (SelectedItem == null && ItemListViewModel.Items.Count > 0)
        {
            if (navigationParameters == null || navigationParameters == string.Empty)
                navigationParameters = ItemListViewModel.ReadLastSelectedItemFromSettingsStore();

            if (navigationParameters != null)
            {
                var navParamArray = navigationParameters?.Split("/");

                if (navParamArray != null && navParamArray.Length > 0)
                {
                    if (ItemListViewModel.TrySelectItemByKey(navParamArray[0]))
                    {
                        if (navParamArray.Length > 1)
                        {
                            SelectedItem?.SetNavigationParameter(navParamArray[1]);
                        }
                    }
                }
            }

            if (SelectedItem == null)
                SelectedItem = ItemListViewModel.Items.First();
        }
    }

    // Page unloaded event
    protected override void OnPageUnloaded()
    {
        SelectedItem = null;
        base.OnPageUnloaded();
    }

    // We are navigating to this page - Called before Loaded.
    // See https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.page.onnavigatedto?view=winrt-26100
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // We will set the selection and scroll it into view in OnLoaded
        // as if we do it here it will be reset by the ListView.ItemsSource
        navigationParameters = e.Parameter as string;
    }

    // We are navigating aways from this page
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        if (e.NavigationMode != NavigationMode.Back)
        {
            // We are nagivating forward.
            // Modify the last entry in the back stack to point to the currently selected item,
            // and any additional current context (e.g., device channel id),
            // so that we get back to that item on navigation back
            var backStack = Frame.BackStack;
            if (backStack.Count > 0 && SelectedItem != null)
            {
                var firstBackEntry = backStack[backStack.Count - 1];
                var navParams = SelectedItem.ItemKey;
                var itemNavParam = SelectedItem.GetNavigationParameter();
                if (itemNavParam != null)
                {
                    navParams += "/" + itemNavParam;
                }
                var newEntry = new PageStackEntry(firstBackEntry.SourcePageType, navParams, firstBackEntry.NavigationTransitionInfo);
                backStack[backStack.Count - 1] = newEntry;
            }
        }
    }

    // PageSizeStatesGroup state change event
    protected void OnCurrentStateChanged(object sender, VisualStateChangedEventArgs e)
    {
        // With Uno on Android, this is first called with e.NewState == null
        // We just ignore the call in that case
        if (e.NewState == null)
            return;

        var newStateName = e.NewState.Name;

        // We use our own recording of the current PageSizeStatesGroup state
        // because this function seems to be called with incorrect e.OldState at times
        if (pageSizeCurrentState != null && newStateName != pageSizeCurrentState)
        {
            pageSizeCurrentState = newStateName;
            bool isNarrow = newStateName == "NarrowState";
            if (isNarrow && SelectedItem != null)
            {
                // If we are transitioning from WideState to NarrowState, we just navigate
                // to the detail page.No need to transition to MasterDetailState, since we
                // are nagivating out, and in fact that would null out SelectedItem.
                Frame.Navigate(typeof(ItemDetailsPage), SelectedItem.ItemKey, new SuppressNavigationTransitionInfo());
            }
            else
            {
                // If we are transitioning from NarrowState to WideState, we trigger the
                // transition to MasterDetailsState and activate the item view model.
                VisualStateManager.GoToState(this, "MasterDetailsState", true);
                if (SelectedItem != null)
                {
                    SelectedItem.IsActive = true;
                }
            }

            ItemListViewModel.PageSizeStateChanged(isNarrow);

            EntranceNavigationTransitionInfo.SetIsTargetElement(ItemListView, isNarrow);
            if (ItemDetailsPresenter != null)
            {
                EntranceNavigationTransitionInfo.SetIsTargetElement(ItemDetailsPresenter, !isNarrow);
            }
        }
    }

    // ItemClick event only happens when user is a Master state. In this state, 
    // selection mode is none and click event navigates to the details view.
    protected void OnItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ItemViewModelType ivm)
        {
            // The clicked item it is the new selected content
            SelectedItem = ivm;
            if (PageSizeVisualStateGroup.CurrentState != null && PageSizeVisualStateGroup.CurrentState.Name == "NarrowState")
            {
                // Go to the details page and display the item 
                Frame.Navigate(typeof(ItemDetailsPage), SelectedItem.ItemKey, new DrillInNavigationTransitionInfo());
            }
            else
            {
                // Play a refresh animation when the user switches detail items.
                EnableContentTransitions();
            }
        }
    }

    private void EnableContentTransitions()
    {
        ItemDetailsPresenter.ContentTransitions?.Clear();
        ItemDetailsPresenter.ContentTransitions?.Add(new EntranceThemeTransition());
    }
}

