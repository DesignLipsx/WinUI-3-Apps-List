using FluentDeck.Helpers;
using FluentDeck.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel.DataTransfer;

namespace FluentDeck.Pages.Emoji;

public sealed partial class EmojiPage : Page
{
    public EmojiPageViewModel ViewModel { get; } = new();

    private bool _isLoaded = false;
    private bool _isUpdating = false;

    public EmojiPage()
    {
        InitializeComponent();
        this.DataContext = ViewModel;
        Loaded += (s, e) =>
        {
            _isLoaded = true;
            SearchShortcutHelper.ApplySearchShortcut(this, SearchBox);
        };
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (ContentFrame != null && ContentFrame.Content == null)
        {
            ContentFrame.Navigate(typeof(EmojiGridPage), ViewModel, new SuppressNavigationTransitionInfo());
        }
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (ViewModel != null)
        {
            ViewModel.SearchQuery = sender.Text;
        }
    }

    private void StyleSegment_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoaded || _isUpdating || ViewModel == null || StyleSegment == null) return;
        _isUpdating = true;
        try
        {
            if (StyleSegment.SelectedItem is CommunityToolkit.WinUI.Controls.SegmentedItem selectedItem)
            {
                if (selectedItem.Tag is string styleTag)
                {
                    ViewModel.SelectedStyle = styleTag;
                }
                else if (selectedItem.Content is string styleContent)
                {
                    ViewModel.SelectedStyle = styleContent;
                }
            }
            else if (StyleSegment.SelectedItem is string styleStr)
            {
                ViewModel.SelectedStyle = styleStr;
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }


    private void CategorySegment_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoaded || _isUpdating || ViewModel == null || CategorySegment == null || ContentFrame == null) return;
        _isUpdating = true;
        try
        {
            var selectedItem = CategorySegment.SelectedItem as CommunityToolkit.WinUI.Controls.SegmentedItem;
            if (selectedItem != null && selectedItem.Tag is string category)
            {
                ViewModel.SelectedCategory = category;

                ContentFrame.Navigate(typeof(EmojiGridPage), ViewModel, new SuppressNavigationTransitionInfo());
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void CopyTextToClipboard(string text)
    {
        try
        {
            var package = new DataPackage();
            package.SetText(text);
            Clipboard.SetContent(package);
        }
        catch { }
    }

    private async void CopyUnicode_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedEmoji != null && sender is Button button)
        {
            string label = FormatUnicodeLabel(ViewModel.SelectedEmoji.Unicode);
            ClipboardAndFileHelpers.CopyTextToClipboard(label);
            await ClipboardAndFileHelpers.AnimateCopySuccessAsync(button, "Copy Unicode");
        }
    }

    private async void CopyGlyph_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedEmoji != null && sender is Button button)
        {
            ClipboardAndFileHelpers.CopyTextToClipboard(ViewModel.SelectedEmoji.Glyph);
            await ClipboardAndFileHelpers.AnimateCopySuccessAsync(button, "Copy Emoji Glyph");
        }
    }

    private async void SaveImage_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedEmoji == null || string.IsNullOrEmpty(ViewModel.SelectedEmoji.ImagePath)) return;
        await ClipboardAndFileHelpers.SaveImageFromAppRelativePathAsync(ViewModel.SelectedEmoji.ImagePath);
    }



    private void AnimateTranslateY(TranslateTransform transform, double toValue)
    {
        var storyboard = new Storyboard();
        var animation = new DoubleAnimation
        {
            To = toValue,
            Duration = new Duration(TimeSpan.FromMilliseconds(150)),
            EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(animation, transform);
        Storyboard.SetTargetProperty(animation, "Y");
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }

    public static string FormatUnicodeLabel(string rawUnicode)
    {
        if (string.IsNullOrEmpty(rawUnicode)) return "";
        return $"U+{rawUnicode.ToUpper()}";
    }

    public static string GetKeywordsString(IList<string> keywords)
    {
        if (keywords == null || keywords.Count == 0) return "";
        return string.Join(", ", keywords);
    }

    public static Thickness GetBorderThickness(bool isSelected) =>
        isSelected ? new Thickness(2) : new Thickness(1);

    public static Microsoft.UI.Xaml.Media.Brush GetBorderBrush(bool isSelected)
    {
        if (isSelected)
        {
            // Accent color brush
            return (Microsoft.UI.Xaml.Media.Brush)Microsoft.UI.Xaml.Application.Current.Resources["AccentFillColorDefaultBrush"];
        }
        return (Microsoft.UI.Xaml.Media.Brush)Microsoft.UI.Xaml.Application.Current.Resources["CardStrokeColorDefaultBrush"];
    }

    public static Microsoft.UI.Xaml.Media.Brush GetCardBackground(bool isSelected)
    {
        if (isSelected)
        {
            return (Microsoft.UI.Xaml.Media.Brush)Microsoft.UI.Xaml.Application.Current.Resources["SubtleFillColorSecondaryBrush"];
        }
        return (Microsoft.UI.Xaml.Media.Brush)Microsoft.UI.Xaml.Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"];
    }

    private void PlayAnimation_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.TogglePlayAnimation();
    }

    public static string AnimationButtonIcon(bool isDownloaded, bool isPlaying)
    {
        if (!isDownloaded) return "\uE896"; // Download icon
        return isPlaying ? "\uE769" : "\uE768"; // Pause / Play icon
    }

    public static string AnimationButtonToolTip(bool isDownloaded, bool isPlaying)
    {
        if (!isDownloaded) return "Download Animated Version";
        return isPlaying ? "Pause Animation" : "Play Animation";
    }

    public static Visibility AnimationButtonVisibility(bool hasAnimation, bool isDownloading) =>
        (hasAnimation && !isDownloading) ? Visibility.Visible : Visibility.Collapsed;

    public static string PlayPauseIcon(bool isPlaying) =>
        isPlaying ? "\uE769" : "\uE768"; // \uE769 = Pause, \uE768 = Play

    public static bool InverseBool(bool value) => !value;

    public static Visibility BoolToVisibility(bool value) =>
        value ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility InverseBoolToVisibility(bool value) =>
        value ? Visibility.Collapsed : Visibility.Visible;

    public static bool IsMonoStyle(string style) =>
        style == "High Contrast";

    /// <summary>
    /// Returns true when the Mono (High Contrast) style is active AND the UI is in dark mode,
    /// so the SvgImage control inverts black strokes to white.
    /// </summary>
    public static bool ShouldInvertMono(string style, ElementTheme actualTheme) =>
        style == "High Contrast" && actualTheme == ElementTheme.Dark;

    public static Uri? SafeSvgUri(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        try
        {
            return new Uri(path);
        }
        catch
        {
            return null;
        }
    }

    public static Visibility SvgVisibility(string? path, bool isPlayingAnimation)
    {
        if (isPlayingAnimation) return Visibility.Collapsed;
        if (!string.IsNullOrEmpty(path) && path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            return Visibility.Visible;
        return Visibility.Collapsed;
    }

    public static Visibility PngVisibility(string? path, bool isPlayingAnimation)
    {
        if (isPlayingAnimation) return Visibility.Collapsed;
        if (!string.IsNullOrEmpty(path) && path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            return Visibility.Visible;
        return Visibility.Collapsed;
    }

    public static Visibility NullToVisibility(object? value) =>
        value == null ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility NotNullToVisibility(object? value) =>
        value != null ? Visibility.Visible : Visibility.Collapsed;
}
