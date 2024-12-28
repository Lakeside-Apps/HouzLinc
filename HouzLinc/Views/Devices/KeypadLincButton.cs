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
using Microsoft.UI.Input;
using ViewModel.Devices;
using Windows.UI.Core;
using System.Diagnostics;

namespace HouzLinc.Views.Devices;

public sealed class KeypadLincButtonVisualStateManager : VisualStateManager
{
    protected override bool GoToStateCore(Control control, FrameworkElement templateRoot, string stateName, VisualStateGroup group, VisualState state, bool useTransitions)
    {
        Debug.Assert(control is KeypadLincButton);
        VisualState newState = state;
        string newStateName = (control as KeypadLincButton)!.FixupState(stateName);
        
        if (newStateName != stateName)
        {
            // find the new state in the group
            bool isFound = false;
            foreach (VisualState s in group.States)
            {
                if (s.Name == newStateName)
                {
                    isFound = true;
                    newState = s;
                    break;
                }
            }
            if (!isFound)
            {
                throw new ArgumentException("Visual state " + newStateName + " not present in the VisualStateGroup");
            }
        }

        return base.GoToStateCore(control, templateRoot, newStateName, group, newState, useTransitions);
    }
}

/// <summary>
/// This class implements a ToggleButton with additional visual states when the button is not checked, 
/// representing how the button would follows the currently checked one in the device. 
/// These are  referred to as Follow Behaviors:
/// - Don't follow: button does not follow any other button
/// - Follow Off: button follows another button, off when that button is checked on
/// - Follow On: button follows another button, on when that button is checked on
/// These visual states can be represented visually using the KeypadLincButtonVisualStateManager
/// In the normal mode, this button is a ToggleButton with the extra visual states above when unchecked
/// In Cycle Follow Behavior mode, the button can't be checked, instead each click on the button cycles 
/// to the next of the 3 Follow Behaviors listed above.
/// </summary>
public partial class KeypadLincButton : ToggleButton
{
    public KeypadLincButton()
    {
        Checked += OnChecked;
        Click += OnClick;
    }

    // Helper
    private static bool IsShiftOn()
    {
        return  (InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.LeftShift) & CoreVirtualKeyStates.Down) != 0 ||
                (InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.RightShift) & CoreVirtualKeyStates.Down) != 0 ||
                (InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift) & CoreVirtualKeyStates.Down) != 0;
    }

    // Checked handler
    // Block checking the button in Cycle Follow Behavior mode
    void OnChecked(object sender, RoutedEventArgs args)
    {
        if (IsCycleFollowBehaviorMode || IsShiftOn())
        {
            IsChecked = false;
        }
        else
        {
            FollowBehavior = FollowBehaviorType.None;
        }
    }

    // Click handler
    // Cycles through the 3 Follow Behaviors in Cycle Follow Behaviors mode
    void OnClick(object sender, RoutedEventArgs args)
    {
        if (IsCycleFollowBehaviorMode || IsShiftOn())
        {
            switch (FollowBehavior)
            {
                case FollowBehaviorType.None:
                    {
                        break;
                    }
                case FollowBehaviorType.Not:
                    {
                        FollowBehavior = FollowBehaviorType.On; 
                        break;
                    }
                case FollowBehaviorType.On:
                    {
                        FollowBehavior = FollowBehaviorType.Off;
                        break;
                    }
                case FollowBehaviorType.Off:
                    {
                        FollowBehavior = FollowBehaviorType.Not;
                        break;
                    }
            }
        }
    }

    /// <summary>
    /// Follow Behavior dependency property
    /// </summary>
    public static readonly DependencyProperty FollowBehaviorProperty =
        DependencyProperty.Register(
            nameof(FollowBehavior), typeof(FollowBehaviorType), typeof(KeypadLincButton),
                new PropertyMetadata(0, new PropertyChangedCallback(OnFollowBehaviorChanged)));

    public FollowBehaviorType FollowBehavior
    {
        get => (FollowBehaviorType)GetValue(FollowBehaviorProperty);
        set => SetValue(FollowBehaviorProperty, value);
    }

    // We use the callback to handle the change instead of the setter because of
    // https://github.com/unoplatform/uno/issues/9886
    private static void OnFollowBehaviorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KeypadLincButton klb)
        {
            klb.GoToProperVisualState();
        }
    }

    /// <summary>
    /// Cycle Follow Behavior mode dependency property
    /// </summary>
    public static readonly DependencyProperty IsCycleFollowBehaviorModeProperty =
        DependencyProperty.Register(
            nameof(IsCycleFollowBehaviorMode), typeof(bool), typeof(KeypadLincButton), null);

    public bool IsCycleFollowBehaviorMode
    {
        get => (bool)GetValue(IsCycleFollowBehaviorModeProperty);
        set => SetValue(IsCycleFollowBehaviorModeProperty, value);
    }

    // Helper method to go to proper visual state when the Follow Behavior is changed
    private void GoToProperVisualState()
    {
        if (IsChecked != true)
        {
            string stateName = "Normal";

            if (IsPressed)
            {
                stateName = "Pressed";
            }
            else if (IsPointerOver)
            {
                stateName = "PointerOver";
            }
            
            VisualStateManager.GoToState(this, FixupState(stateName), true);
        }
    }

    /// <summary>
    /// Helper called by the KeypadLincButtonVisualStateManager during GotoState to determine what visual state to actually go to
    /// based on requested visual state and current Follow behavior
    /// public only because needs to be called by KeypadLincButtonVisualStateManager
    /// </summary>
    /// <param name="stateName">State the visual state manager would like to go to</param>
    /// <returns>State the visual state manager should go to instead</returns>
    public string FixupState(string stateName)
    {
        switch (stateName)
        {
            case "Normal":
                {
                    if (FollowBehavior != FollowBehaviorType.None)
                    {
                        stateName = (FollowBehavior == FollowBehaviorType.Not) ? "NotFollowing" :
                                    ((FollowBehavior == FollowBehaviorType.On) ? "FollowingOn" : "FollowingOff");
                    }
                    break;
                }

            case "PointerOver":
                {
                    if (FollowBehavior != FollowBehaviorType.None)
                    {
                        stateName = (FollowBehavior == FollowBehaviorType.Not) ? "NotFollowingPointerOver" :
                                    ((FollowBehavior == FollowBehaviorType.On) ? "FollowingOnPointerOver" : "FollowingOffPointerOver");
                    }
                    break;
                }

            case "Pressed":
                {
                    if (FollowBehavior != FollowBehaviorType.None)
                    {
                        stateName = (FollowBehavior == FollowBehaviorType.Not) ? "NotFollowingPressed" :
                                    ((FollowBehavior == FollowBehaviorType.On) ? "FollowingOnPressed" : "FollowingOffPressed");
                    }
                    break;
                }

            case "Checked":
                {
                    break;
                }
        }
        return stateName;
    }
}
