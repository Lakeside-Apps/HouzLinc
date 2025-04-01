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

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UnoApp.Controls;

/// <summary>
/// A control to turn the load on and off on a load controlling device
/// For light controller, also allows to dim to any level, and brighten and dim incrementally
/// - Full on button
/// - Full off button
/// - Optional Slider to set level
/// - Brighten/dim with long presses to on/off buttons
/// </summary>
public sealed partial class LoadController: UserControl, INotifyPropertyChanged
{
    public LoadController()
    {
        this.InitializeComponent();
    }

    // Property binding support
    public event PropertyChangedEventHandler? PropertyChanged = delegate { };
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public bool IsSliderVisible
    {
        get => (bool)GetValue(IsSliderVisibleProperty);
        set => SetValue(IsSliderVisibleProperty, value);
    }
    public static readonly DependencyProperty IsSliderVisibleProperty =
        DependencyProperty.Register(nameof(IsSliderVisible), typeof(bool), typeof(LoadController), null);

    public double Level
    {
        get => (double)GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    // We use the callback to handle the change instead of the setter because of
    // https://github.com/unoplatform/uno/issues/9886
    private static void OnLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LoadController lc)
        {
            lc.SliderValue = (int)(lc.Level * 100);
        }
    }

    public static readonly DependencyProperty LevelProperty =
        DependencyProperty.Register(nameof(Level), typeof(double), typeof(LoadController), 
            new PropertyMetadata(0.0d, new PropertyChangedCallback(OnLevelChanged)));

    public event RoutedEventHandler? LoadFullOn;
    public event RoutedEventHandler? LoadOn;
    public event RoutedEventHandler? LoadOff;

    private void RaiseLightOnEvent()
    {
        LoadOn?.Invoke(this, null);
    }

    private void RaiseLightOnFullEvent()
    {
        Level = 1.0;
        LoadFullOn?.Invoke(this, null);
    }

    private void RaiseLightOffEvent()
    {
        Level = 0;
        LoadOff?.Invoke(this, null);
    }

    // Bindable to XAML UI
    public int SliderValue
    {
        get => sliderValue;
        set
        {
            if (value != sliderValue)
            {
                sliderValue = value;
                Level = (double)value / 100.0;
                OnPropertyChanged();
            }
        }
    }
    private int sliderValue;

    private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (e.NewValue != SliderValue)
        {
            SliderValue = (int)e.NewValue;

            // This helps reduce the number of notifications
            if (timer == null)
            {
                timer = DispatcherQueue.CreateTimer();
                timer.Interval = TimeSpan.FromMilliseconds(200);
                timer.Tick += (sender, e) => { RaiseLightOnEvent(); timer.Stop(); };
            }

            if (!timer.IsRunning)
            {
                timer.Start();
            }
        }
    }
    private DispatcherQueueTimer? timer;
}
