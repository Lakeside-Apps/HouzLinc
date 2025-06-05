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
using UnoApp.Controls;
using Windows.Foundation;

public static class XAMLHelpers
{
    /// <summary>
    /// Find the first visual ancestor of a given element with a given name
    /// </summary>
    /// <param name="start"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static DependencyObject? FindVisualAncestorByName(DependencyObject? start, string name)
    {
        while (start != null)
        {
            if (start is FrameworkElement fe && fe.Name == name)
                return start;

            start = VisualTreeHelper.GetParent(start) as FrameworkElement;
        }

        return null;
    }

    /// <summary>
    /// Find the first visual ancestor of a given FrameworkElement of a specific type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="start"></param>
    /// <returns></returns>
    public static T? FindVisualAncestor<T>(DependencyObject? start) where T : DependencyObject
    {
        while (start != null)
        {
            if (start is T ancestor)
                return ancestor;

            start = VisualTreeHelper.GetParent(start);
        }

        return default(T);
    }

    /// <summary>
    /// Find a descendant element by name within the visual tree of a given parent.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static DependencyObject? FindElementByName(DependencyObject parent, string name)
    {
        if (parent == null || string.IsNullOrEmpty(name))
            return null;

        // If it's a control with a template, ensure the template is applied
        if (parent is Control control)
        {
            control.ApplyTemplate();
        }

        // Check immediate children
        int childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);

            if (child is FrameworkElement fe && fe.Name == name)
                return fe;

            // Recursively search the child
            DependencyObject? result = FindElementByName(child, name);
            if (result != null)
                return result;
        }

        return null;
    }

    /// <summary>
    /// Find a descendant element of a specific type within the visual tree of a given parent.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="targetType"></param>
    /// <returns></returns>
    public static FrameworkElement? FindElementByType(DependencyObject parent, Type targetType)
    {
        if (parent == null)
            return null;

        // If it's a control with a template, ensure the template is applied
        if (parent is Control control)
        {
            control.ApplyTemplate();
        }

        // Check immediate children
        int childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);

            if (child is FrameworkElement fe && fe.GetType() == targetType)
                return fe;

            // Recursively search the child
            FrameworkElement? result = FindElementByType(child, targetType);
            if (result != null)
                return result;
        }

        return null;
    }

    /// <summary>
    /// Scroll an item into view within an ItemsControl using a ScrollViewer.
    /// The scrollview can be the one of the ItemsControl or a parent of the ItemsControl.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="scrollViewer"></param>
    /// <param name="itemsControl"></param>
    /// <param name="item"></param>
    public static void ScrollItemIntoView<T>(ScrollViewer scrollViewer, ItemsControl itemsControl, T item)
    {
        if (itemsControl == null || item == null)
            return;
        
        // Get the container for the item
        var container = itemsControl.ContainerFromItem(item) as FrameworkElement;
        if (container != null)
        {
            // Scroll the item into view
            ScrollContainerIntoView(scrollViewer, container);
        }
    }

    /// <summary>
    /// Scroll a specific FrameworkElement within a ScrollViewer into view
    /// </summary>
    /// <param name="scrollViewer"></param>
    /// <param name="container"></param>
    public static void ScrollContainerIntoView(ScrollViewer scrollViewer, FrameworkElement container)
    {
        if (container != null && scrollViewer != null)
        {
            Point? offset = XAMLHelpers.GetElementOffsetRelativeTo(container, scrollViewer);
            if (offset != null)
            {
                scrollViewer.ChangeView(offset.Value.X, offset.Value.Y, null);
            }
        }
    }

    /// <summary>
    /// Get the offset of a FrameworkElement relative to another FrameworkElement.
    /// </summary>
    /// <param name="element"></param>
    /// <param name="relativeTo"></param>
    /// <returns></returns>
    public static Point? GetElementOffsetRelativeTo(FrameworkElement element, FrameworkElement relativeTo)
    {
        if (element == null || relativeTo == null)
            return null;

        // Transform the top-left point (0,0) of the element to the coordinate space of 'relativeTo'
        GeneralTransform transform = element.TransformToVisual(relativeTo);
        Point offset = transform.TransformPoint(new Point(0, 0));
        return offset;
    }

    /// <summary>
    /// Set focus to an editable text block, dispatching the request so that it works
    /// even if the text block will only be made editable after this function returns
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="name"></param>
    public static void FocusEditableTextBlock(DependencyObject sender, string name)
    {
        var top = FindVisualAncestor<DataTemplate>(sender);
        if (top != null)
        {
            var editableTextBlock = FindElementByName(top, name) as EditableTextBlock;

            if (editableTextBlock != null)
            {
                var dq = DispatcherQueue.GetForCurrentThread();
                dq.TryEnqueue(() =>
                {
                    editableTextBlock.Focus(FocusState.Programmatic);
                    editableTextBlock.SelectAll();
                });
            }
        }
    }

}
