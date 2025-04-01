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
using UnoApp.Dialogs;
using ViewModel.Base;

namespace UnoApp.Controls;

public sealed partial class RoomControl : ContentControl, INotifyPropertyChanged
{
    public RoomControl()
    {
        this.InitializeComponent();
    }

    // Data binding to the UI
    public event PropertyChangedEventHandler? PropertyChanged = delegate { };
    void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Room name. Consumers of this control bind this property to the view model.
    /// </summary>
    public string Room
    {
        get => (string)GetValue(RoomProperty);
        set => SetValue(RoomProperty, value);
    }

    public static readonly DependencyProperty RoomProperty = DependencyProperty.Register(
        nameof(Room), typeof(string), typeof(RoomControl), new PropertyMetadata(null, new PropertyChangedCallback(OnValueChanged)));

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if ((string)e.NewValue != (string)e.OldValue)
        {
            if (d is RoomControl rc)
            {
                rc.OnPropertyChanged(nameof(RoomDisplayText));
            }
        }
    }

    // Value display text. Bound to the UI of the control.
    private string RoomDisplayText
    {
        get => Room != string.Empty ? Room : "None";
        set
        {
            // The databinding with the combobox sets this to null initially on Uno, we ignore it
            if (value != null)
            {
                if (value == "None")
                    value = string.Empty;

                if (value != Room)
                {
                    Room = value;
                }
            }
        }
    }

    // Collection of values in the room combobox
    // This is an observable collection so that we can add new rooms on the fly
    // when the user is assigning a new room
    public SortableObservableCollection<string> Rooms
    {
        get => (SortableObservableCollection<string>)GetValue(RoomsProperty);
        set => SetValue(RoomsProperty, value);
    }

    public static readonly DependencyProperty RoomsProperty = DependencyProperty.Register(
        nameof(Rooms), typeof(SortableObservableCollection<string>), typeof(RoomControl), new PropertyMetadata(null, new PropertyChangedCallback(OnRoomsChanged)));

    private static void OnRoomsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RoomControl rc)
        {
            rc.OnPropertyChanged(nameof(Rooms));
            rc.OnPropertyChanged(nameof(RoomDisplayText));
        }
    }

    // Add a new room to the list of rooms
    private async void AddNewRoomButton_Click(object sender, RoutedEventArgs e)
    {
        var newRoomDialog = new NewRoomDialog(XamlRoot);
        var result = await newRoomDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            var newRoom = newRoomDialog.Room;
            if (newRoom != null && newRoom != string.Empty)
            {
                // Add the new room to the list of rooms if not already there
                if (!Rooms.Contains(newRoom))
                {
                    Rooms.Remove("None");
                    Rooms.Add(newRoom);
                    Rooms.Sort(r => r, SortDirection.Ascending);
                    Rooms.Insert(0, "None");
                }

                // Report the new room and select it in the combobox list
                Room = newRoom;
            }
        }
    }
}
