// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Core;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Input;
using UnoApp.Views.Devices;
using UnoApp.Views.Scenes;
using UnoApp.Views.Hub;
using UnoApp.Views.Console;
using UnoApp.Views.Settings;
using UnoApp.Views.Tools;
using ViewModel.Settings;
using Windows.Storage.Pickers;
using UnoApp.Dialogs;
using System.Collections.ObjectModel;
using Windows.System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Common;

namespace UnoApp;

#pragma warning disable CS8618
// Members of these classes are initialized when creating a list of them in BuildNavItems
public class NavItem
{
    public string Label { get; set; }
    public string Tag { get; set; }
    public Type Page { get; set; }
}

[Bindable(true)]
public class NavMenuItem : NavItem
{
    public string Glyph { get; set; }
    public string Tooltip { get; set; }
}
#pragma warning restore CS8618

// Data context for the NavigationView header
[Bindable(true)]
public sealed class NavViewHeaderDataContext : ObservableObject
{
    public string? Header;
    public string? AddItemBtnLabel;
    public string? RemoveItemBtnLabel;

    public bool IsAddItemBtnVisible
    {
        get => isAddItemBtnVisible;
        set { if (value != isAddItemBtnVisible) { isAddItemBtnVisible = value; OnPropertyChanged(); } }
    }
    private bool isAddItemBtnVisible;

    public bool IsRemoveItemBtnVisible
    {
        get => isRemoveItemBtnVisible;
        set { if (value != isRemoveItemBtnVisible) { isRemoveItemBtnVisible = value; OnPropertyChanged(); } }
    }
    private bool isRemoveItemBtnVisible;
}

/// <summary>
/// The "chrome" layer of the app that provides top-level navigation with
/// proper keyboarding navigation.
/// </summary>
public sealed partial class AppShell : Page, INotifyPropertyChanged
{
    // Guaranted not null while AppShell is loaded
    public static AppShell Current = null!;

    // Data binding support
    public event PropertyChangedEventHandler? PropertyChanged = delegate { };
    public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // TODO: try to have a single place where app title is defined, 
    // either in resources or in the app manifest
    public string AppTitleText = "HouzLinc";

    /// <summary>
    /// Initializes a new instance of the AppShell, sets the static 'Current' reference,
    /// adds callbacks for Back requests and changes in the SplitView's DisplayMode, and
    /// provide the nav menu list with the data to display.
    /// </summary>
    public AppShell()
    {
        this.InitializeComponent();

        BuildNavItems();

        this.Loaded += async (sender, args) =>
        {
            Current = this;

            /* TOTO: sort out (UWP -> WinUI)
            var titleBar = Windows.ApplicationModel.Core.CoreApplication.GetCurrentView().TitleBar;
            titleBar.IsVisibleChanged += TitleBar_IsVisibleChanged;
            */

            // Setup callback into the App layer for ViewModels to show file picker
            FileStorageProvider.ShowFileOpenPickerHandler += ShowFileOpenPicker;
            FileStorageProvider.ShowFileSavePickerHandler += ShowFileSavePicker;
            SettingsViewModel.ShowOpenHouseDialogHandler += ShowOpenHouseDialog;

            // Load or create the house configuration
            if (!await SettingsViewModel.Instance.EnsureHouse())
                return;

            // Attempt to discover and connect to the Hub
            await SettingsViewModel.Instance.FindHub();

            // We now have a house config and maybe even a Hub, show it.
            SplashMode = false;
        };

        this.Unloaded += (sender, args) =>
        {
            Current = null!;
            FileStorageProvider.ShowFileOpenPickerHandler -= ShowFileOpenPicker;
            FileStorageProvider.ShowFileSavePickerHandler -= ShowFileSavePicker;
            SettingsViewModel.ShowOpenHouseDialogHandler -= ShowOpenHouseDialog;
        };
    }

    // Whether this Shell is in splash screen mode, i.e.,
    // the house configuration has not been loaded yet.
    // The UI only displays a "loading" message in this mode.
    public bool SplashMode
    {
        get => splashMode;
        set
        {
            if (value != splashMode)
            {
                splashMode = value;
                OnPropertyChanged();
            }
        }
    }
    private bool splashMode = true;

    /// <summary>
    /// Set-up the custom titlebar
    /// </summary>
    public void SetTitleBar()
    {
        App.MainWindow.ExtendsContentIntoTitleBar = true;
        App.MainWindow.SetTitleBar(AppTitleBar);
    }

    // We use the width of the NavView pane in the compact form as the height of the titlebar,
    // with a default of 48 as we are loading things up before showing the NavView
    private double TitleBarHeight => NavView?.CompactPaneLength ?? 48;
    

    /// <summary>
    /// Default keyboard focus movement for any unhandled keyboarding
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void AppShell_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        FocusNavigationDirection direction = FocusNavigationDirection.None;
        switch (e.Key)
        {
            case Windows.System.VirtualKey.Left:
            case Windows.System.VirtualKey.GamepadDPadLeft:
            case Windows.System.VirtualKey.GamepadLeftThumbstickLeft:
            case Windows.System.VirtualKey.NavigationLeft:
                direction = FocusNavigationDirection.Left;
                break;
            case Windows.System.VirtualKey.Right:
            case Windows.System.VirtualKey.GamepadDPadRight:
            case Windows.System.VirtualKey.GamepadLeftThumbstickRight:
            case Windows.System.VirtualKey.NavigationRight:
                direction = FocusNavigationDirection.Right;
                break;

            case Windows.System.VirtualKey.Up:
            case Windows.System.VirtualKey.GamepadDPadUp:
            case Windows.System.VirtualKey.GamepadLeftThumbstickUp:
            case Windows.System.VirtualKey.NavigationUp:
                direction = FocusNavigationDirection.Up;
                break;

            case Windows.System.VirtualKey.Down:
            case Windows.System.VirtualKey.GamepadDPadDown:
            case Windows.System.VirtualKey.GamepadLeftThumbstickDown:
            case Windows.System.VirtualKey.NavigationDown:
                direction = FocusNavigationDirection.Down;
                break;
        }

        if (direction != FocusNavigationDirection.None)
        {
            // In a WinUI Desktop app, the app must call FindNextElement overload with the
            // FindNextElementOptions parameter, and the FindNextElementOptions.SearchRoot
            // must be set to a loaded DependencyObject.
            var options = new FindNextElementOptions() { SearchRoot = AppShell.Current };
            var control = FocusManager.FindNextElement(direction, options) as Control;
            if (control != null)
            {
                control.Focus(FocusState.Keyboard);
                e.Handled = true;
            }
        }
    }

    // Navigation items shown in the NavigationView menu
    private ObservableCollection<NavMenuItem> NavMenuItems = null!;

    // Navigation items shown in the footer NavigationView menu
    private ObservableCollection<NavMenuItem> FooterNavMenuItems = null!;

    // Other navigation items not in the menu
    private List<NavItem> OtherNavItems = null!;

    private void BuildNavItems()
    {
        NavMenuItems = new ObservableCollection<NavMenuItem>
        {
            new NavMenuItem
            {
                Glyph = "\uE7E8",
                Label = DeviceListPage.PageHeader,
                Tag = DeviceListPage.PageHeader,
                Page = typeof(DeviceListPage)
            },
            new NavMenuItem
            {
                Glyph="\uE8B2",
                Label = SceneListPage.PageHeader,
                Tag = SceneListPage.PageHeader,
                Page = typeof(SceneListPage)
            },
            new NavMenuItem
            {
                Glyph="\uEB77",
                Label = HubChannelListPage.PageHeader,
                Tag = HubChannelListPage.PageHeader,
                Page = typeof(HubChannelListPage)
            },
            new NavMenuItem
            {
                Glyph="\uE786",
                Label = ConsolePage.PageHeader,
                Tag = ConsolePage.PageHeader,
                Page = typeof(ConsolePage)
            },
            new NavMenuItem
            {
                Glyph="\uEC7A",
                Label = ToolsPage.PageHeader,
                Tag = ToolsPage.PageHeader,
                Page = typeof(ToolsPage)
            },
            new NavMenuItem
            {
                Glyph="\uE713",
                Label = SettingsPage.PageHeader,
                Tag = SettingsPage.PageHeader,
                Page = typeof(SettingsPage)
            },

        };

        FooterNavMenuItems = new ObservableCollection<NavMenuItem>
        {
            // Add or more NavMenuItems here to make them appear in the footer area of the navigtation menu.
        };

        OtherNavItems = new List<NavItem>
        {
            new NavItem
            {
                Label = DeviceDetailsPage.PageHeader,
                Tag = DeviceDetailsPage.PageHeader,
                Page = typeof(DeviceDetailsPage)
            },
            new NavItem
            {
                Label = HubChannelDetailsPage.PageHeader,
                Tag = HubChannelDetailsPage.PageHeader,
                Page = typeof(HubChannelDetailsPage)
            },
            new NavItem
            {
                Label = SceneDetailsPage.PageHeader,
                Tag = SceneDetailsPage.PageHeader,
                Page = typeof(SceneDetailsPage)
            },
            new NavItem
            {
                Label = HubSettingsPage.PageHeader,
                Tag = HubSettingsPage.PageHeader,
                Page = typeof(HubSettingsPage)
            }
        };
    }

    private NavItem? GetNavItemFromTag(string tag)
    {
        NavItem? item = NavMenuItems.FirstOrDefault(p => p.Tag.Equals(tag));
        item ??= FooterNavMenuItems.FirstOrDefault(p => p.Tag.Equals(tag));
        item ??= OtherNavItems.FirstOrDefault(p => p.Tag.Equals(tag));
        return item;
    }

    private NavItem? GetNavItemFromPageType(Type page)
    {
        NavItem? item = NavMenuItems.FirstOrDefault(p => p.Page == page);
        item ??= FooterNavMenuItems.FirstOrDefault(p => p.Page == page);
        item ??= OtherNavItems.FirstOrDefault(p => p.Page == page);
        return item;
    }

    /// <summary>
    /// Navigate the NavView control and content frame
    /// </summary>
    /// <param name="navItemTag">Tag of the NavItem or NavMenuItem</param>
    /// <param name="parameter">Parameter to pass to the page we are navigating to</param>
    /// <param name="transitionInfo">Visual transition</param>
    public void Navigate(
        string navItemTag,
        object? parameter,
        Microsoft.UI.Xaml.Media.Animation.NavigationTransitionInfo transitionInfo)
    {
        NavItem? item = GetNavItemFromTag(navItemTag);
        if (item != null)
            Navigate(item.Page, parameter, transitionInfo);
    }

    /// <summary>
    /// Navigate the NavView control and content frame
    /// </summary>
    /// <param name="page">Page type to navigate to</param>
    /// <param name="parameter">See above</param>
    /// <param name="transitionInfo">See above</param>
    public void Navigate(
        Type page,
        object? parameter,
        Microsoft.UI.Xaml.Media.Animation.NavigationTransitionInfo transitionInfo)
    {
        ContentFrame.Navigate(page, parameter, transitionInfo);
    }

    /// <summary>
    /// This supresses the navigation parameter at the top of the backstack.
    /// Use if that parameter has stale context
    /// </summary>
    public bool GoBackNoContext()
    {
        if (!ContentFrame.CanGoBack)
            return false;

        var backStack = ContentFrame.BackStack;
        var firstBackEntry = backStack[backStack.Count - 1];
        var newEntry = new PageStackEntry(firstBackEntry.SourcePageType, null, firstBackEntry.NavigationTransitionInfo);
        backStack[backStack.Count - 1] = newEntry;
        ContentFrame.GoBack();
        return true;
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        // Adjust the height of the titlebar to be propoertionate to the NavView pane
        OnPropertyChanged(nameof(TitleBarHeight));

        // Add handler for ContentFrame navigation.
        ContentFrame.Navigated += On_Navigated;

        // First navigation depends on whether we can connect to the Hub
        NavItem navItem;
        navItem = SettingsViewModel.Instance.IsHubFound ?
            GetNavItemFromPageType(typeof(DeviceListPage))! : 
            GetNavItemFromPageType(typeof(HubSettingsPage))!;
        NavView.SelectedItem = navItem;

        // If navigation occurs on SelectionChanged, this isn't needed.
        // Because we use ItemInvoked to navigate, we need to call Navigate
        // here to load the home page.
        Navigate(navItem.Page, parameter: null, new EntranceNavigationTransitionInfo());

        // Listen to the window directly so the app responds
        // to accelerator keys regardless of which element has focus.
        // TODO: figure this out for WinUI
        //Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated +=
        //    CoreDispatcher_AcceleratorKeyActivated;

        //Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;

#if HAS_UNO
        // Handle the system back button (does not work on WinUI yet, only on Uno)
        SystemNavigationManager.GetForCurrentView().BackRequested += System_BackRequested;
#endif
    }

    // User selected a navigation menu item
    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer != null)
        {
            var navItemTag = args.InvokedItemContainer.Tag.ToString() ?? string.Empty;
            Navigate(navItemTag, parameter: null, args.RecommendedNavigationTransitionInfo);
        }
    }

    private void NavView_BackRequested(NavigationView sender,
                                       NavigationViewBackRequestedEventArgs args)
    {
        TryGoBack();
    }

    private void ContentFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
    {

    }

#if !HAS_UNO
    private void CoreDispatcher_AcceleratorKeyActivated(CoreDispatcher sender, AcceleratorKeyEventArgs e)
    {
        // When Alt+Left are pressed navigate back
        if (e.EventType == CoreAcceleratorKeyEventType.SystemKeyDown
            && e.VirtualKey == VirtualKey.Left
            && e.KeyStatus.IsMenuKeyDown == true
            && !e.Handled)
        {
            e.Handled = TryGoBack();
        }
    }
#endif

    private void System_BackRequested(object? sender, BackRequestedEventArgs e)
    {
        if (!e.Handled)
        {
            e.Handled = TryGoBack();
        }
    }

    private void CoreWindow_PointerPressed(CoreWindow sender, PointerEventArgs e)
    {
        // Handle mouse back button.
        if (e.CurrentPoint.Properties.IsXButton1Pressed)
        {
            e.Handled = TryGoBack();
        }
    }

    private bool TryGoBack()
    {
        if (!ContentFrame.CanGoBack)
            return false;

        // Don't go back if the nav pane is overlayed.
        if (NavView.IsPaneOpen &&
            (NavView.DisplayMode == NavigationViewDisplayMode.Compact ||
             NavView.DisplayMode == NavigationViewDisplayMode.Minimal))
            return false;

        ContentFrame.GoBack();
        return true;
    }

    private void On_Navigated(object sender, NavigationEventArgs e)
    {
        NavView.IsBackEnabled = ContentFrame.CanGoBack;
        NavView.SelectedItem = GetNavItemFromPageType(e.SourcePageType);
    }

    /*-------------------------------------------------------------------------------*/

    #region BackRequested Handlers

    private void SystemNavigationManager_BackRequested(object sender, BackRequestedEventArgs e)
    {
        bool handled = e.Handled;
        this.BackRequested(ref handled);
        e.Handled = handled;
    }

    private void BackRequested(ref bool handled)
    {
        // Get a hold of the current frame so that we can inspect the app back stack.

        if (ContentFrame == null)
            return;

        // Check to see if this is the top-most page on the app back stack.
        if (this.ContentFrame.CanGoBack && !handled)
        {
            // If not, set the event to handled and go back to the previous page in the app.
            handled = true;
            this.ContentFrame.GoBack();
        }
    }

    #endregion

    private async Task<SettingsViewModel.OpenHouseDialogActions> ShowOpenHouseDialog(bool showPreviousError)
    {
        OpenHouseDialog dialog = new OpenHouseDialog(this.XamlRoot, showPreviousError);
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            return dialog.Action;
        }

        return SettingsViewModel.OpenHouseDialogActions.Exit;
    }

    /// <summary>
    /// Opens appropriate dialog and file picker to choose whether to load a house model 
    /// from a Houselinc-like file or to create new.
    /// This method may terminate the app if the user chooses to cancel out of the dialogs
    /// If/when we support cloud stoage of the house model, we will need to generalize
    /// this function and calling points to return something else than a storage file
    /// </summary>
    /// <returns>null if we need to create a new house model</returns>
    private async Task<StorageFile> ShowFileOpenPicker()
    {
        FileOpenPicker openPicker = new FileOpenPicker();
        openPicker.ViewMode = PickerViewMode.List;
        openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        openPicker.FileTypeFilter.Add(".xml");
        openPicker.CommitButtonText = "Open House Configuration";

        // Initialize the folder picker with the window handle (HWND).
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

        return await openPicker.PickSingleFileAsync();
    }

    private async Task<StorageFile> ShowFileSavePicker()
    {
        FileSavePicker savePicker = new FileSavePicker();
        savePicker.FileTypeChoices.Add("Houselinc file", new List<string>() { ".xml" });
        savePicker.CommitButtonText = "Create New House Configuration";
        savePicker.SuggestedFileName = "houselinc";

        // Initialize the folder picker with the window handle (HWND).
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);

        return await savePicker.PickSaveFileAsync();
    }
}
