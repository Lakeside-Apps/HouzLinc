/* Copyright 2022 ChristianGa Fortini

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

using ViewModel.Base;

namespace HouzLinc.Views.Base;

/// <summary>
/// Base class for pages that use one or more view models.
/// View models can be added and removed.
/// Once added, they are notified of page life-cyle events:
/// loaded, unloaded, navigated to, or navigated from.
/// </summary>
public abstract partial class PageWithViewModels : Page
{
    public PageWithViewModels()
    {
        this.Loaded += OnLoaded;
        this.Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        foreach (var viewModel in viewModels)
        {
            viewModel.ViewLoaded();
        }

        OnPageLoaded();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        foreach (var viewModel in viewModels)
        {
            viewModel.ViewUnloaded();
        }

        OnPageUnloaded();
    }

    protected virtual void OnPageLoaded() { }

    protected virtual void OnPageUnloaded() { }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        foreach (var viewModel in viewModels)
        {
            viewModel.ViewNavigatedTo();
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        foreach (var viewModel in viewModels)
        {
            viewModel.ViewNavigatedFrom();
        }
    }

    protected void AddViewModel(PageViewModel? viewModel)
    {
        if (viewModel == null)
            return;

        viewModels.Add(viewModel);
    }

    protected void RemoveViewModel(PageViewModel? viewModel)
    {
        if (viewModel == null)
            return;

        viewModels.Remove(viewModel);
    }

    private List<PageViewModel> viewModels = new();
}
