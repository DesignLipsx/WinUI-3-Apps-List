using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace FluentDeck.Pages.Diagnostics;

public sealed partial class DiagnosticsUrlPage : Page
{
    private DiagnosticsPage? _parent;

    public DiagnosticsUrlPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is DiagnosticsPage parent)
        {
            _parent = parent;
            UrlIssuesList.ItemsSource = _parent.FilteredUrlResults;
            UpdateEmptyState();
            _parent.FilteredUrlResults.CollectionChanged += FilteredUrlResults_CollectionChanged;
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        if (_parent != null)
        {
            _parent.FilteredUrlResults.CollectionChanged -= FilteredUrlResults_CollectionChanged;
        }
    }

    private void FilteredUrlResults_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateEmptyState();
    }

    private void UpdateEmptyState()
    {
        if (_parent == null) return;

        var list = _parent.FilteredUrlResults;
        bool isEmpty = list.Count == 0;

        if (isEmpty)
        {
            if (_parent.AllUrlResults.Count > 0)
            {
                UrlEmptyIcon.Glyph = "\uE930"; // Checkmark
                UrlEmptyIcon.Foreground = (Brush)Application.Current.Resources["SystemFillColorSuccessBrush"];
                UrlEmptyText.Text = "All clear! All checked URLs and Logos are active and healthy.";
            }
            else
            {
                UrlEmptyIcon.Glyph = "\uE774"; // Globe
                UrlEmptyIcon.Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
                UrlEmptyText.Text = "No URL check results to display. Click 'Check URLs & Logos' to run a diagnostics scan.";
            }
            UrlEmptyState.Visibility = Visibility.Visible;
            UrlIssuesList.Visibility = Visibility.Collapsed;
        }
        else
        {
            UrlEmptyState.Visibility = Visibility.Collapsed;
            UrlIssuesList.Visibility = Visibility.Visible;
        }
    }

    private async void MarkArchivedBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is UrlCheckResult result && _parent != null)
        {
            await _parent.MarkAllArchivedAsync(new List<UrlCheckResult> { result });
        }
    }

    private void UpdateUrlBtn_Click(object sender, RoutedEventArgs e)
    {
        _parent?.UpdateUrlBtn_Click(sender, e);
    }

    private void ClearLogoBtn_Click(object sender, RoutedEventArgs e)
    {
        _parent?.ClearLogoBtn_Click(sender, e);
    }

    private void OpenUrlBtn_Click(object sender, RoutedEventArgs e)
    {
        _parent?.OpenUrlBtn_Click(sender, e);
    }
}
