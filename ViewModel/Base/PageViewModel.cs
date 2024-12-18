using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace ViewModel.Base;

/// <summary>
/// A base class for view models that are associated with a XAML page
/// and need to be notified on page lifecycle events.
/// </summary>
public class PageViewModel : ObservableObject
{
    /// <summary>
    /// Called when the page (view) had loaded
    /// </summary>
    public virtual void ViewLoaded() { }

    /// <summary>
    /// Called when the page (view) is unloaded
    /// </summary>
    public virtual void ViewUnloaded() { }

    /// <summary>
    /// Called when the page (view) is navigated to
    /// </summary>
    public virtual void ViewNavigatedTo() { }

    /// <summary>
    /// Called when the page (view) is navigated from
    /// </summary>
    public virtual void ViewNavigatedFrom() { }
}
