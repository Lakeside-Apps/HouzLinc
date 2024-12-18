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

using Microsoft.UI.Xaml.Input;
using ViewModel.Console;
using System.ComponentModel;
using Common;
using System.Runtime.CompilerServices;
using ViewModel.Settings;
using HouzLinc.Views.Base;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace HouzLinc.Views.Console;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ConsolePage : PageWithViewModels
{
    // TODO: this should be made a string resource and fetch from the code and XAML as needed
    public string PageHeaderProperty => PageHeader;
    public const string PageHeader = "Console";

    public ConsolePage()
    {
        this.InitializeComponent();

        AddViewModel(ConsoleViewModel);

        ICommandProcessor commandProcessor = new CommandProcessor();
        ConsoleViewModel.CommandProcessor = commandProcessor;
    }

    // Main view model
    private ConsoleViewModel ConsoleViewModel => ConsoleViewModel.Instance;

    // View Model for the hub info on this page
    private SettingsViewModel SettingsViewModel => SettingsViewModel.Instance;

    protected override void OnPageLoaded()
    {
        base.OnPageLoaded();
        ConsoleViewModel.LogItems.CollectionChanged += OnLogCollectionChanged;
        ScrollLogToBottom();
        ConsoleCommand.Focus(FocusState.Programmatic);
    }

    protected override void OnPageUnloaded()
    {
        base.OnPageUnloaded();
        ConsoleViewModel.LogItems.CollectionChanged -= OnLogCollectionChanged;
    }

    private void Console_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case Windows.System.VirtualKey.Enter:
                {
                    // It appears that on Android, scheduling the ProcessCommand and
                    // returning immeidately works better than awaiting it especially
                    // when using the phone emulator with the desktop keyboard.
                    DispatcherQueue.TryEnqueue(async () => await ProcessCommand());

                    // TODO: we could instead consider running the ProcessCommand on a 
                    // different thread, but this would require the EventSource that
                    // the command logs to to be created on that thread.
                    //Task.Run(async () => await ProcessCommand());

                    e.Handled = true;
                    break;
                }
            case Windows.System.VirtualKey.Up:
            case Windows.System.VirtualKey.GamepadDPadUp:
                {
                    ConsoleViewModel.PreviousCommand();
                    e.Handled = true;
                    break;
                }
            case Windows.System.VirtualKey.Down:
            case Windows.System.VirtualKey.GamepadDPadDown:
                {
                    ConsoleViewModel.NextCommand();
                    e.Handled = true;
                    break;
                }
        }
    }

    private void Console_PreviewKeyUp(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case Windows.System.VirtualKey.Enter:
            case Windows.System.VirtualKey.Up:
            case Windows.System.VirtualKey.Down:
                //e.Handled = true;
                break;
        }
    }

    private async void GoButton_Click(object sender, RoutedEventArgs e)
    {
        await ProcessCommand();
    }

    private async Task ProcessCommand ()
    {
        string command = ConsoleCommand.Text;
        await ConsoleViewModel.ProcessCommandAsync(command);
    }

    void OnLogCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs args)
    {
        if (args.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
        {
            // Give the console log time to update before scrolling to the bottom
            var t = DispatcherQueue.CreateTimer();
            t.Interval = TimeSpan.FromMilliseconds(100);
            t.Tick += (sender, e) => { ScrollLogToBottom(); t.Stop(); };
            t.Start();
        }
    }

    // Scroll to the bottom of the console log when it is changed
    private void ScrollLogToBottom()
    {
        // Tentative implemention if iOS does not support ChangeView
        //var items = ConsoleLog.Items;
        //ConsoleLog.ScrollIntoView(items[items.Count() - 1]);

        double height = ConsoleLog.ScrollableHeight;
        ConsoleLog.ChangeView(0, height, null);
    }
}
