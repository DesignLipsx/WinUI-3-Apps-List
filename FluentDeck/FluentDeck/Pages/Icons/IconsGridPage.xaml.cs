using FluentDeck.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace FluentDeck.Pages.Icons;

public sealed partial class IconsGridPage : Page
{
    public IconsPageViewModel? ViewModel { get; private set; }

    public IconsGridPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is IconsPageViewModel vm)
        {
            ViewModel = vm;
            Bindings.Update(); // Required: x:Bind holds a direct reference to the page property,
                               // not DataContext — Update() makes it re-evaluate with the injected ViewModel.
        }
    }

    private void IconGridView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is GridView gridView && gridView.ItemsPanelRoot is ItemsWrapGrid panel)
        {
            double minWidth = 130;
            double availableWidth = e.NewSize.Width - 16;
            int columns = (int)Math.Max(1, Math.Floor(availableWidth / minWidth));
            panel.ItemWidth = Math.Max(minWidth, availableWidth / columns);
        }
    }

    private bool _isUpdating = false;

    private void IconGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdating || ViewModel == null) return;
        if (sender is GridView gridView && gridView.SelectedItem is IconItem icon)
        {
            _isUpdating = true;
            try
            {
                ViewModel.SelectedIcon = icon;
            }
            finally
            {
                _isUpdating = false;
            }
        }
    }
}
