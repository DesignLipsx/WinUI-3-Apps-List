using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.Specialized;

namespace FluentDeck.Pages.Diagnostics;

public sealed partial class DiagnosticsFormattingPage : Page
{
    private DiagnosticsPage? _parent;

    public DiagnosticsFormattingPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is DiagnosticsPage parent)
        {
            _parent = parent;
            FormattingIssuesList.ItemsSource = _parent.AllIssues;
            UpdateEmptyState();
            _parent.AllIssues.CollectionChanged += AllIssues_CollectionChanged;
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        if (_parent != null)
        {
            _parent.AllIssues.CollectionChanged -= AllIssues_CollectionChanged;
        }
    }

    private void AllIssues_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateEmptyState();
    }

    private void UpdateEmptyState()
    {
        if (_parent == null) return;
        bool isEmpty = _parent.AllIssues.Count == 0;
        FormattingEmptyState.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
        FormattingIssuesList.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;
    }

    private void FixIssueBtn_Click(object sender, RoutedEventArgs e)
    {
        _parent?.FixIssueBtn_Click(sender, e);
    }
}
