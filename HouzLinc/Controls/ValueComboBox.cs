using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouzLinc.Controls;

/// <summary>
/// A ComboBox and a Value reflecting the selected item and maintained across changes in the list of items.
/// If Value is set programmatically, it will set the SelectedItem in the ComboBox if there is a match.
/// If the user selects an item in the ComboBox, Value is set to that item.
/// Combobox items and value are of type string.
/// </summary>
internal partial class ValueComboBox : ComboBox
{
    public ValueComboBox()
    {
        SelectionChanged += OnSelectionChanged;
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(string), typeof(ValueComboBox), new PropertyMetadata(null, new PropertyChangedCallback(OnValueChanged)));

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if ((string)e.NewValue != (string)e.OldValue)
        {
            if (d is ValueComboBox cb)
            {
                cb.SelectedItem = (string)e.NewValue;
            }
        }
    }

    protected override void OnItemsChanged(object e)
    {
        SelectedItem = Value;
        base.OnItemsChanged(e);
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SelectedItem != null && SelectedItem is string selectedItem)
        {
            Value = selectedItem;
        }
    }
}
