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

using ViewModel.Scenes;
using ViewModel.Base;
using ViewModel.Settings;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HouzLinc.Controls;
internal partial class ScenesComboBox : ComboBox, INotifyPropertyChanged
{
    public ScenesComboBox()
    {
        RecreateSceneList();
        DisplayMemberPath = nameof(SceneViewModel.DisplayNameAndId);
        SelectionChanged += OnSelectedItemChanged;
    }

    // Data binding support
    public event PropertyChangedEventHandler? PropertyChanged = delegate { };
    public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void RecreateSceneList()
    {
        SceneListViewModel svm;
        // TODO: this won't work if we have have multiple house configs
        svm = SceneListViewModel.Create(Holder.House.Scenes);
        svm.SortByRoom(SortDirection.Ascending);
        ItemsSource = svm.Items;
    }

    /// <summary>
    /// ID of the Currently selected device
    /// </summary>
    public int SelectedSceneId
    {
        get => (int)GetValue(SelectedSceneIdProperty);
        set => SetValue(SelectedSceneIdProperty, value);
    }

    // Sets the SelectedDeviceID property to reflect a selection change in the control
    private void OnSelectedItemChanged(object sender, SelectionChangedEventArgs args)
    {
        if (SelectedItem != null && SelectedItem is SceneViewModel selectedItem)
        {
            SelectedSceneId = selectedItem.Id;
        }
        OnPropertyChanged(nameof(IsAnySceneSelected));
    }

    public bool IsAnySceneSelected => SelectedItem != null;

    private static void OnSelectedSceneIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScenesComboBox thisComboBox)
        {
            if (e.NewValue != null)
            {
                if (e.NewValue is int newValue && newValue != ((int)e.OldValue))
                {
                    thisComboBox.SelectedItem = SceneViewModel.GetOrCreateItemByKey(Holder.House, newValue.ToString());
                }
            }
            else
            {
                thisComboBox.SelectedItem = null;
            }
        }
    }

    public static readonly DependencyProperty SelectedSceneIdProperty =
        DependencyProperty.Register(nameof(SelectedSceneId), typeof(int), typeof(ScenesComboBox),
            new PropertyMetadata(null, new PropertyChangedCallback(OnSelectedSceneIdChanged)));
}
