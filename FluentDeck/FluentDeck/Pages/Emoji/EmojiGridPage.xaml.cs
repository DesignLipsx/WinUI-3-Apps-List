using FluentDeck.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace FluentDeck.Pages.Emoji;

public sealed partial class EmojiGridPage : Page
{
    public EmojiPageViewModel? ViewModel { get; private set; }

    public EmojiGridPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is EmojiPageViewModel vm)
        {
            ViewModel = vm;
            Bindings.Update();
        }
    }

    private void EmojiGridView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is GridView gridView && gridView.ItemsPanelRoot is ItemsWrapGrid panel)
        {
            double minWidth = 140;
            double availableWidth = e.NewSize.Width - 16;
            int columns = (int)Math.Max(1, Math.Floor(availableWidth / minWidth));
            panel.ItemWidth = Math.Max(minWidth, availableWidth / columns);
        }
    }

    private bool _isUpdating = false;

    private void EmojiGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdating || ViewModel == null) return;
        if (sender is GridView gridView && gridView.SelectedItem is EmojiItem emoji)
        {
            _isUpdating = true;
            try
            {
                ViewModel.SelectedEmoji = emoji;
            }
            finally
            {
                _isUpdating = false;
            }
        }
    }
}
