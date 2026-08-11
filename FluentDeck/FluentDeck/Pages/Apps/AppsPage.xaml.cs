using FluentDeck.Helpers;
using FluentDeck.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;

namespace FluentDeck.Pages.Apps;

public sealed partial class AppsPage : Page
{
    public AppsPageViewModel ViewModel { get; } = new();
    private readonly DispatcherTimer _gitTimer;
    private bool _isInitialNavigation = true;

    public AppsPage()
    {
        InitializeComponent();
        this.DataContext = ViewModel;

        _gitTimer = new DispatcherTimer();
        _gitTimer.Interval = TimeSpan.FromMinutes(2);
        _gitTimer.Tick += GitTimer_Tick;
        Unloaded += (s, e) => _gitTimer.Stop();

        Loaded += AppsPage_Loaded;
    }

    private async void AppsPage_Loaded(object sender, RoutedEventArgs e)
    {
        SearchShortcutHelper.ApplySearchShortcut(this, SearchBox);
        ViewModel.UpdateDeveloperMode();

        if (ViewModel.FilteredItems.Count == 0)
        {
            await ViewModel.InitializeAsync();
        }

        _gitTimer.Start();
        _ = ViewModel.CheckRemoteChangesAsync();

        if (_isInitialNavigation)
        {
            NavigateToCurrentViewMode(false);
            _isInitialNavigation = false;
        }
    }

    private void NavigateToCurrentViewMode(bool useTransition, bool isBackward = false)
    {
        if (ContentFrame == null) return;

        if (LayoutModeSegment != null) LayoutModeSegment.Visibility = Visibility.Visible;

        bool isGrid = LayoutModeSegment != null && LayoutModeSegment.SelectedIndex == 1;
        Type targetType = isGrid ? typeof(AppsPreviewGridPage) : typeof(AppsPreviewListPage);

        var transition = useTransition
            ? new SlideNavigationTransitionInfo { Effect = isBackward ? SlideNavigationTransitionEffect.FromLeft : SlideNavigationTransitionEffect.FromRight }
            : null;
        ContentFrame.Navigate(targetType, ViewModel, transition);
    }

    private void LayoutModeSegment_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel != null && LayoutModeSegment != null)
        {
            ViewModel.LayoutModeIndex = LayoutModeSegment.SelectedIndex;
            bool isBackward = LayoutModeSegment.SelectedIndex == 0;
            NavigateToCurrentViewMode(true, isBackward);
        }
    }

    private void PricingFilter_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null && sender is RadioMenuFlyoutItem item && item.Tag is string tag)
        {
            ViewModel.PricingFilter = tag;
        }
    }

    private void StyleFilter_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null && sender is RadioMenuFlyoutItem item && item.Tag is string tag)
        {
            ViewModel.StyleFilter = tag;
        }
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (ViewModel == null) return;
        ViewModel.SearchQuery = sender.Text;
    }

    private void InfoButton_Click(object sender, RoutedEventArgs e) =>
        HelpTeachingTip.IsOpen = !HelpTeachingTip.IsOpen;

    private async void SyncChangesButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            await ViewModel.SyncChangesAsync();
        }
    }

    private async void GitTimer_Tick(object? sender, object e)
    {
        if (ViewModel != null)
        {
            await ViewModel.CheckRemoteChangesAsync();
        }
    }

    // ── Static x:Bind helpers (view-layer converters, referenced from DataTemplates) ──

    public static Visibility BoolToVisibility(bool value) =>
        value ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility AddAppButtonVisibility(bool isLeafCategory) =>
        (isLeafCategory && FeatureManager.IsDeveloperMode) ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility InverseBoolToVisibility(bool value) =>
        value ? Visibility.Collapsed : Visibility.Visible;

    public static Style HeaderTextStyle(int level) =>
        (Style)Application.Current.Resources[level switch
        {
            1 or 2 => "SubtitleTextBlockStyle",
            3 => "BodyStrongTextBlockStyle",
            4 => "BodyStrongTextBlockStyle",
            _ => "BodyTextBlockStyle"
        }];

    public static Thickness HeaderMargin(int level) => level switch
    {
        1 or 2 => new Thickness(0, 24, 24, 4),
        3 => new Thickness(0, 8, 24, 2),
        4 => new Thickness(12, 4, 24, 0),
        _ => new Thickness(24, 2, 24, 0)
    };

    public static Visibility HeaderSeparatorVisibility(int level) =>
        level == 2 ? Visibility.Visible : Visibility.Collapsed;

    public static Thickness TextMargin(int headingLevel) => headingLevel switch
    {
        1 or 2 or 3 => new Thickness(12, 2, 0, 2),
        4 => new Thickness(24, 2, 0, 2),
        _ => new Thickness(36, 2, 0, 2)
    };

    public static Thickness AppMargin(int headingLevel) => headingLevel switch
    {
        1 or 2 or 3 => new Thickness(12, 0, 0, 0),
        4 => new Thickness(12, 0, 0, 0),
        _ => new Thickness(24, 0, 0, 0)
    };

    public static Microsoft.UI.Xaml.Media.ImageSource? PathToImageSource(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        try
        {
            return new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(path))
            {
                DecodePixelWidth = 64,
                DecodePixelHeight = 64
            };
        }
        catch
        {
            return null;
        }
    }
}
