using FluentDeck.Helpers;
using FluentDeck.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FluentDeck.Pages.Icons;

public sealed partial class IconsPage : Page
{
    public IconsPageViewModel ViewModel { get; } = new();

    private bool _isLoaded = false;
    private bool _isUpdating = false;

    public IconsPage()
    {
        InitializeComponent();
        this.DataContext = ViewModel;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        Loaded += (s, e) =>
        {
            _isLoaded = true;
            SearchShortcutHelper.ApplySearchShortcut(this, SearchBox);
            SyncSegmentsFromViewModel();
        };
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.PreviewStyle) ||
            e.PropertyName == nameof(ViewModel.SelectedSize) ||
            e.PropertyName == nameof(ViewModel.SelectedIcon))
        {
            SyncSegmentsFromViewModel();
        }
    }

    private void SyncSegmentsFromViewModel()
    {
        if (_isUpdating || ViewModel == null) return;
        _isUpdating = true;
        try
        {
            if (ViewModel.SelectedIcon != null)
            {
                bool hasRegular = ViewModel.SelectedIcon.RegularSizes.Count > 0;
                bool hasFilled = ViewModel.SelectedIcon.FilledSizes.Count > 0;
                bool hasColor = ViewModel.SelectedIcon.ColorSizes.Count > 0;

                if (RegularStyleItem != null) RegularStyleItem.Visibility = hasRegular ? Visibility.Visible : Visibility.Collapsed;
                if (FilledStyleItem != null) FilledStyleItem.Visibility = hasFilled ? Visibility.Visible : Visibility.Collapsed;
                if (ColorStyleItem != null) ColorStyleItem.Visibility = hasColor ? Visibility.Visible : Visibility.Collapsed;

                // Fallback style if current PreviewStyle is not available
                string currentStyle = ViewModel.PreviewStyle ?? "Regular";
                bool isCurrentStyleAvailable = currentStyle switch
                {
                    "Regular" => hasRegular,
                    "Filled" => hasFilled,
                    "Color" => hasColor,
                    _ => false
                };

                if (!isCurrentStyleAvailable)
                {
                    currentStyle = hasRegular ? "Regular" : (hasFilled ? "Filled" : (hasColor ? "Color" : "Regular"));
                    ViewModel.PreviewStyle = currentStyle;
                }

                // Update size item IsEnabled based on available sizes in current style
                var availableMap = currentStyle switch
                {
                    "Regular" => ViewModel.SelectedIcon.RegularSizes,
                    "Filled" => ViewModel.SelectedIcon.FilledSizes,
                    "Color" => ViewModel.SelectedIcon.ColorSizes,
                    _ => ViewModel.SelectedIcon.RegularSizes
                };

                if (Size16Item != null) Size16Item.Visibility = availableMap.ContainsKey("16") ? Visibility.Visible : Visibility.Collapsed;
                if (Size20Item != null) Size20Item.Visibility = availableMap.ContainsKey("20") ? Visibility.Visible : Visibility.Collapsed;
                if (Size24Item != null) Size24Item.Visibility = availableMap.ContainsKey("24") ? Visibility.Visible : Visibility.Collapsed;
                if (Size28Item != null) Size28Item.Visibility = availableMap.ContainsKey("28") ? Visibility.Visible : Visibility.Collapsed;
                if (Size32Item != null) Size32Item.Visibility = availableMap.ContainsKey("32") ? Visibility.Visible : Visibility.Collapsed;
                if (Size48Item != null) Size48Item.Visibility = availableMap.ContainsKey("48") ? Visibility.Visible : Visibility.Collapsed;

                // Fallback size if current SelectedSize is not available
                string currentSize = ViewModel.SelectedSize ?? "24";
                if (!availableMap.ContainsKey(currentSize))
                {
                    string fallbackSize = availableMap.Keys.OrderBy(k => int.TryParse(k, out int v) ? v : 99).FirstOrDefault() ?? "24";
                    ViewModel.SelectedSize = fallbackSize;
                }
            }

            if (StyleSegment != null)
            {
                string targetStyle = ViewModel.PreviewStyle ?? "Regular";
                foreach (var child in StyleSegment.Items)
                {
                    if (child is CommunityToolkit.WinUI.Controls.SegmentedItem item && item.Tag as string == targetStyle)
                    {
                        StyleSegment.SelectedItem = item;
                        break;
                    }
                }
            }

            if (SizeSegment != null)
            {
                string targetSize = ViewModel.SelectedSize ?? "24";
                foreach (var child in SizeSegment.Items)
                {
                    if (child is CommunityToolkit.WinUI.Controls.SegmentedItem item && item.Tag as string == targetSize)
                    {
                        SizeSegment.SelectedItem = item;
                        break;
                    }
                }
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (ContentFrame != null && ContentFrame.Content == null)
        {
            ContentFrame.Navigate(typeof(IconsGridPage), ViewModel, new SuppressNavigationTransitionInfo());
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
        if (StyleSegment.SelectedItem is CommunityToolkit.WinUI.Controls.SegmentedItem selectedItem && selectedItem.Tag is string style)
        {
            ViewModel.PreviewStyle = style;
            SyncSegmentsFromViewModel();
        }
    }


    private void TopStyleSegment_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoaded || _isUpdating || ViewModel == null || TopStyleSegment == null || ContentFrame == null) return;
        _isUpdating = true;
        try
        {
            var selectedItem = TopStyleSegment.SelectedItem as CommunityToolkit.WinUI.Controls.SegmentedItem;
            if (selectedItem != null && selectedItem.Tag is string style)
            {
                ViewModel.SelectedStyle = style;
                ViewModel.PreviewStyle = style;
                SyncSegmentsFromViewModel();
                ContentFrame.Navigate(typeof(IconsGridPage), ViewModel, new SuppressNavigationTransitionInfo());
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void SizeSegment_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoaded || _isUpdating || ViewModel == null || SizeSegment == null) return;
        _isUpdating = true;
        try
        {
            if (SizeSegment.SelectedItem is CommunityToolkit.WinUI.Controls.SegmentedItem selectedItem && selectedItem.Tag is string size)
            {
                ViewModel.SelectedSize = size;
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private async void CopySvg_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedIcon == null || string.IsNullOrEmpty(ViewModel.PreviewImagePath) || sender is not Button button) return;

        try
        {
            string svgContent = "";
            if (File.Exists(ViewModel.PreviewImagePath))
            {
                svgContent = File.ReadAllText(ViewModel.PreviewImagePath);
            }
            else
            {
                string relativePath = ViewModel.PreviewImagePath.Replace("ms-appx:///", "").Replace("/", "\\");
                string diskPath = Path.Combine(AppContext.BaseDirectory, relativePath);
                if (File.Exists(diskPath))
                {
                    svgContent = File.ReadAllText(diskPath);
                }
            }

            if (!string.IsNullOrEmpty(svgContent))
            {
                ClipboardAndFileHelpers.CopyTextToClipboard(svgContent);
                await ClipboardAndFileHelpers.AnimateCopySuccessAsync(button, "Copy SVG");
            }
        }
        catch { }
    }

    private async void SaveImage_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedIcon == null || string.IsNullOrEmpty(ViewModel.PreviewImagePath)) return;
        await ClipboardAndFileHelpers.SaveImageFromAppRelativePathAsync(ViewModel.PreviewImagePath);
    }



    public static string GetKeywordsString(IList<string>? keywords)
    {
        if (keywords == null || keywords.Count == 0) return "";
        return string.Join(", ", keywords);
    }

    public static Visibility BoolToVisibility(bool value) =>
        value ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility InverseBoolToVisibility(bool value) =>
        value ? Visibility.Collapsed : Visibility.Visible;

    public static bool IsMonoStyle(string style) =>
        style != "Color";

    public static bool ShouldInvertIcon(string? style, ElementTheme actualTheme) =>
        style != "Color" && actualTheme == ElementTheme.Dark;

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

    public static Visibility NullToVisibility(object? value) =>
        value == null ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility NotNullToVisibility(object? value) =>
        value != null ? Visibility.Visible : Visibility.Collapsed;
}

public class MonoStyleConverter : Microsoft.UI.Xaml.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string style)
            return style != "Color";
        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
