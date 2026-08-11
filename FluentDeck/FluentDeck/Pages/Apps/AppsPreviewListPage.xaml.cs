using FluentDeck.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace FluentDeck.Pages.Apps;

public sealed partial class AppsPreviewListPage : Page
{
    public AppsPageViewModel? ViewModel { get; private set; }

    public AppsPreviewListPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is AppsPageViewModel vm)
        {
            ViewModel = vm;
            CatalogCVS.Source = ViewModel.GroupedGridApps;

            ViewModel.OnScrollToCategoryRequested = ScrollToCategory;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            UpdateLayoutVisibility();
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        if (ViewModel != null)
        {
            ViewModel.OnScrollToCategoryRequested = null;
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppsPageViewModel.IsLoading))
        {
            UpdateLayoutVisibility();
        }
    }

    private void UpdateLayoutVisibility()
    {
        if (ViewModel == null || ReadmeListView == null) return;

        if (ViewModel.IsLoading)
        {
            ReadmeListView.Visibility = Visibility.Collapsed;
            LoadingRing.Visibility = Visibility.Visible;
            LoadingRing.IsActive = true;
        }
        else
        {
            ReadmeListView.Visibility = Visibility.Visible;
            LoadingRing.Visibility = Visibility.Collapsed;
            LoadingRing.IsActive = false;
        }
    }

    private void HeaderButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && ViewModel != null)
        {
            AppViewActionHelpers.ShowCategoryFlyout(fe, ViewModel);
        }
    }

    private async void AddAppButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && ViewModel != null)
        {
            await AppViewActionHelpers.HandleAddAppAsync(fe, ViewModel, this.XamlRoot);
        }
    }

    private async void EditApp_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && ViewModel != null)
        {
            await AppViewActionHelpers.HandleEditAppAsync(fe, ViewModel, this.XamlRoot);
        }
    }

    private void ScrollToCategory(string tag)
    {
        try
        {
            string categoryName = tag.Contains("::") ? tag.Split(new[] { "::" }, StringSplitOptions.None)[1] : tag;
            int groupIndex = -1;
            for (int i = 0; i < ViewModel!.GroupedGridApps.Count; i++)
            {
                var group = ViewModel.GroupedGridApps[i];
                if (group.Header.Text.Contains(categoryName, StringComparison.OrdinalIgnoreCase) ||
                    group.Header.RawText.Contains(categoryName, StringComparison.OrdinalIgnoreCase) ||
                    categoryName.Contains(group.Header.Text, StringComparison.OrdinalIgnoreCase))
                {
                    groupIndex = i;
                    break;
                }
            }

            if (groupIndex != -1)
            {
                var group = ViewModel.GroupedGridApps[groupIndex];
                ReadmeListView.ScrollIntoView(group, ScrollIntoViewAlignment.Leading);
            }
        }
        catch { }
    }
}
