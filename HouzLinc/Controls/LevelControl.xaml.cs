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

using Microsoft.UI.Xaml.Controls.Primitives;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HouzLinc.Controls;

public sealed partial class LevelControl : UserControl, INotifyPropertyChanged
{
    public LevelControl()
    {
        this.InitializeComponent();
    }

    // Property binding support
    public event PropertyChangedEventHandler? PropertyChanged = delegate { };
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Bindable property for the level value (MinLevel - MaxLevel)
    /// </summary>
    public int Level
    {
        get => (int)GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    public static readonly DependencyProperty LevelProperty =
        DependencyProperty.Register(nameof(Level), typeof(int), typeof(LevelControl), new PropertyMetadata(0, (sender, e) => 
        {
            // We use the callback to handle the change instead of the setter because of
            // https://github.com/unoplatform/uno/issues/9886
            if (sender is LevelControl levelControl)
            {
                levelControl.LevelChanged((int)e.NewValue);
            }
        }));

    public int MaxLevel
    {
        get => (int)GetValue(MaxLevelProperty);
        set => SetValue(MaxLevelProperty, value);
    }

    public static readonly DependencyProperty MaxLevelProperty =
        DependencyProperty.Register(nameof(MaxLevel), typeof(int), typeof(LevelControl), new PropertyMetadata(255));

    public int MinLevel
    {
        get => (int)GetValue(MinLevelProperty);
        set => SetValue(MinLevelProperty, value);
    }

    public static readonly DependencyProperty MinLevelProperty =
        DependencyProperty.Register(nameof(MinLevel), typeof(int), typeof(LevelControl), new PropertyMetadata(0));

    /// <summary>
    /// Bindable property for the header text
    /// </summary>
    public string Header
    {
        get => (string)GetValue(IsLabelVisibleProperty);
        set => SetValue(IsLabelVisibleProperty, value);
    }

    public static readonly DependencyProperty IsLabelVisibleProperty =
        DependencyProperty.Register(nameof(Header), typeof(string), typeof(LevelControl), new PropertyMetadata(""));

    /// <summary>
    /// Bindable property to indicate whether the dim/bright icons should be visible
    /// </summary>
    public bool AreIconsVisible
    {
        get => (bool)GetValue(AreIconsVisibleProperty);
        set => SetValue(AreIconsVisibleProperty, value);
    }

    public static readonly DependencyProperty AreIconsVisibleProperty =
        DependencyProperty.Register(nameof(AreIconsVisible), typeof(bool), typeof(LevelControl), new PropertyMetadata(true));

    private bool isHeaderVisible => Header != null && Header != string.Empty;

    // Bindable from the LevelControl UI
    // TODO: make private when https://github.com/unoplatform/uno/pull/14521 is fixed
    public double doublePercentLevel
    {
        get => _doublePercentLevel;
        set
        {
            if (value != _doublePercentLevel)
            {
                _doublePercentLevel = value;
                OnPropertyChanged();
            }
        }
    }
    private double _doublePercentLevel;

    // Bindable from the LevelControl UI
    // TODO: make private when https://github.com/unoplatform/uno/pull/14521 is fixed
    public string stringPercentLevel
    {
        get => $"{doublePercentLevel:F0}%";
        set
        {
            if (value != _stringPercentLevel)
            {
                _stringPercentLevel = value;
                OnPropertyChanged();
            }
        }
    }
    private string _stringPercentLevel = string.Empty;

    // Called when Level has changed
    // Updates the textbox text and the slider position
    private void LevelChanged(int level)
    {
        if (level < MinLevel)
        {
            level = MinLevel;
        }
        else if (level > MaxLevel)
        {
            level = MaxLevel;
        }

        doublePercentLevel = (double)(level - MinLevel) * 100d / (MaxLevel - MinLevel);
        stringPercentLevel = $"{doublePercentLevel:F0}%";
    }

    // Called when the slider value had changed
    // Updates level and textbox showing the value in percent
    private void SliderValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        doublePercentLevel = e.NewValue;
        stringPercentLevel = $"{doublePercentLevel:F0}%";
        Level = (int)Math.Round(doublePercentLevel * (MaxLevel - MinLevel) / 100d) + MinLevel;
    }

    // Called when the textbox value has changed
    // Updates level and slider position
    private void TextValueChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            stringPercentLevel = textBox.Text;
            if (double.TryParse(stringPercentLevel.Replace("%", ""), out double level))
            {
                doublePercentLevel = level;
                Level = (int)Math.Round(doublePercentLevel * (MaxLevel - MinLevel) / 100d) + MinLevel;
            }
        }
    }
}
