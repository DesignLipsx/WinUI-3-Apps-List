using FluentDeck.Dialogs;
using FluentDeck.Models;
using FluentDeck.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FluentDeck.Pages.Apps;

/// <summary>
/// Shared helper routines for app page user interaction handlers (dialog management, category routing).
/// Eliminates code duplication across list and grid sub-view implementations.
/// </summary>
public static class AppViewActionHelpers
{
    public static async Task HandleAddAppAsync(FrameworkElement targetElement, AppsPageViewModel viewModel, XamlRoot pageXamlRoot)
    {
        CatalogHeaderItem? headerItem = null;
        if (targetElement.DataContext is CatalogHeaderItem item)
        {
            headerItem = item;
        }
        else if (targetElement.DataContext is GridAppGroup group)
        {
            headerItem = group.Header;
        }

        if (headerItem == null) return;

        var xamlRoot = targetElement.XamlRoot ?? pageXamlRoot ?? App.MainWindowInstance?.Content?.XamlRoot;
        if (xamlRoot == null) return;

        var existingApps = App.MainWindowInstance?.GetFlatApps() ?? new List<FlatAppItem>();
        var dialog = new AddAppDialog(headerItem.Text, existingApps) { XamlRoot = xamlRoot };
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await viewModel.AddAppAsync(headerItem, dialog);
        }
    }

    public static async Task HandleEditAppAsync(FrameworkElement targetElement, AppsPageViewModel viewModel, XamlRoot pageXamlRoot)
    {
        if (targetElement.DataContext is AppItem app)
        {
            var xamlRoot = targetElement.XamlRoot ?? pageXamlRoot ?? App.MainWindowInstance?.Content?.XamlRoot;
            if (xamlRoot == null) return;

            var existingApps = App.MainWindowInstance?.GetFlatApps() ?? new List<FlatAppItem>();
            string category = app is CatalogAppItem mApp ? mApp.CategoryName : "General";
            var dialog = new AddAppDialog(category, app, existingApps) { XamlRoot = xamlRoot };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await viewModel.UpdateAppAsync(app, dialog);
            }
        }
    }

    public static void ShowCategoryFlyout(FrameworkElement targetElement, AppsPageViewModel viewModel)
    {
        if (targetElement == null || viewModel == null) return;

        var flyout = new Flyout
        {
            Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.BottomEdgeAlignedLeft
        };

        var headerStack = new StackPanel { Spacing = 8, Width = 300 };

        var titleText = new TextBlock
        {
            Text = "Jump to Category",
            Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"],
            Margin = new Thickness(0, 0, 0, 4)
        };
        headerStack.Children.Add(titleText);

        var treeView = new TreeView
        {
            MaxHeight = 380,
            SelectionMode = TreeViewSelectionMode.None,
            CanReorderItems = false,
            CanDragItems = false
        };

        treeView.ItemTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(@"
            <DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                          xmlns:models=""using:FluentDeck.Models"">
                <TreeViewItem ItemsSource=""{Binding Children}"" IsExpanded=""True"">
                    <StackPanel Orientation=""Horizontal"" Spacing=""8"" VerticalAlignment=""Center"">
                        <Image Source=""{Binding IconPath}"" Width=""20"" Height=""20"" VerticalAlignment=""Center"" Visibility=""{Binding IconVisibility}""/>
                        <TextBlock Text=""{Binding Name}"" Style=""{StaticResource BodyTextBlockStyle}"" VerticalAlignment=""Center""/>
                    </StackPanel>
                </TreeViewItem>
            </DataTemplate>");

        treeView.ItemsSource = viewModel.CategoryNodes;

        treeView.ItemInvoked += (s, args) =>
        {
            if (args.InvokedItem is CategoryNode node)
            {
                flyout.Hide();
                viewModel.OnScrollToCategoryRequested?.Invoke(node.Name);
            }
        };

        headerStack.Children.Add(treeView);
        flyout.Content = headerStack;

        Microsoft.UI.Xaml.Controls.Primitives.FlyoutBase.SetAttachedFlyout(targetElement, flyout);
        Microsoft.UI.Xaml.Controls.Primitives.FlyoutBase.ShowAttachedFlyout(targetElement);
    }
}
