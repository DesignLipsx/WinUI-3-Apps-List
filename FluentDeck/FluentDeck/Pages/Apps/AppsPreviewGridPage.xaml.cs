using FluentDeck.Models;
using FluentDeck.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace FluentDeck.Pages.Apps;

public sealed partial class AppsPreviewGridPage : Page
{
    public AppsPageViewModel? ViewModel { get; private set; }

    public AppsPreviewGridPage()
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
        if (ViewModel == null || GroupedGridView == null) return;

        if (ViewModel.IsLoading)
        {
            GroupedGridView.Visibility = Visibility.Collapsed;
            LoadingRing.Visibility = Visibility.Visible;
            LoadingRing.IsActive = true;
        }
        else
        {
            GroupedGridView.Visibility = Visibility.Visible;
            LoadingRing.Visibility = Visibility.Collapsed;
            LoadingRing.IsActive = false;
        }
    }

    private void GroupedGridView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is GridView gridView && gridView.ItemsPanelRoot is ItemsWrapGrid panel)
        {
            double minWidth = 200;
            double availableWidth = e.NewSize.Width - 16; // account for 8px right item margin only
            int columns = (int)Math.Max(1, Math.Floor(availableWidth / minWidth));
            panel.ItemWidth = Math.Max(minWidth, availableWidth / columns);
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
                GroupedGridView.ScrollIntoView(group, ScrollIntoViewAlignment.Leading);
            }
        }
        catch { }
    }

    private async void GroupedGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is CatalogAppItem app && app.NavigateUri != null)
        {
            try
            {
                await Windows.System.Launcher.LaunchUriAsync(app.NavigateUri);
            }
            catch { }
        }
    }


    private async void EditApp_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && ViewModel != null)
        {
            await AppViewActionHelpers.HandleEditAppAsync(fe, ViewModel, this.XamlRoot);
        }
    }
}
