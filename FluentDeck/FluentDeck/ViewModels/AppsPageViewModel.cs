using FluentDeck.Dialogs;
using FluentDeck.Helpers;
using FluentDeck.Models;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FluentDeck.ViewModels;

public class AppsPageViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _searchQuery = "";
    private string _categorySearchQuery = "";
    private string _appCountText = "";
    private string _syncChangesText = "Sync Changes (0)";
    private bool _isSyncButtonVisible = false;
    private bool _isSyncButtonEnabled = true;
    private bool _isLoading = true;
    private bool _isError = false;
    private string _errorMessage = "";
    private bool _isDeveloperMode = false;
    private int _layoutModeIndex = 0; // 0 = List, 1 = Grid
    private int _viewModeIndex = 0;

    private string _repoPath = "";
    private List<ICatalogItem> _allParsedItems = new();
    private List<CategoryNode> _allCategoryNodes = new();
    private readonly HashSet<string> _leafCategoryNames = new(StringComparer.OrdinalIgnoreCase);

    public ObservableCollection<ICatalogItem> FilteredItems { get; } = new();
    public ObservableCollection<GridAppGroup> GroupedGridApps { get; } = new();
    public ObservableCollection<CategoryNode> CategoryNodes { get; } = new();

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                ExecuteSearch();
            }
        }
    }

    private string _pricingFilter = "All";
    private string _styleFilter = "All";

    public string PricingFilter
    {
        get => _pricingFilter;
        set
        {
            if (SetProperty(ref _pricingFilter, value))
            {
                ExecuteSearch();
            }
        }
    }

    public string StyleFilter
    {
        get => _styleFilter;
        set
        {
            if (SetProperty(ref _styleFilter, value))
            {
                ExecuteSearch();
            }
        }
    }

    public string CategorySearchQuery
    {
        get => _categorySearchQuery;
        set
        {
            if (SetProperty(ref _categorySearchQuery, value))
            {
                ExecuteCategorySearch();
            }
        }
    }

    public string AppCountText
    {
        get => _appCountText;
        set => SetProperty(ref _appCountText, value);
    }

    public string SyncChangesText
    {
        get => _syncChangesText;
        set => SetProperty(ref _syncChangesText, value);
    }

    public bool IsSyncButtonVisible
    {
        get => _isSyncButtonVisible;
        set => SetProperty(ref _isSyncButtonVisible, value);
    }

    public bool IsSyncButtonEnabled
    {
        get => _isSyncButtonEnabled;
        set => SetProperty(ref _isSyncButtonEnabled, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool IsError
    {
        get => _isError;
        set => SetProperty(ref _isError, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsDeveloperMode
    {
        get => _isDeveloperMode;
        set => SetProperty(ref _isDeveloperMode, value);
    }

    public int LayoutModeIndex
    {
        get => _layoutModeIndex;
        set => SetProperty(ref _layoutModeIndex, value);
    }

    public int ViewModeIndex
    {
        get => _viewModeIndex;
        set => SetProperty(ref _viewModeIndex, value);
    }

    public bool IsViewModeEditor => ViewModeIndex == 1;

    public string RepoPath => _repoPath;
    public string DataJsonPath => GetJsonPath();
    public HashSet<string> LeafCategoryNames => _leafCategoryNames;

    private string _searchPlaceholder = "Search apps...";
    public string SearchPlaceholder
    {
        get => _searchPlaceholder;
        set => SetProperty(ref _searchPlaceholder, value);
    }

    public Func<Task>? OnDataRefreshed;
    public Action<string>? OnScrollToCategoryRequested;
    public Action<FrameworkElement>? OnShowCategoryFlyoutRequested;
    public Action? OnFindNextRequested;
    public Action? OnFindPrevRequested;

    public AppsPageViewModel()
    {
        UpdateDeveloperMode();
    }

    public void UpdateDeveloperMode()
    {
        IsDeveloperMode = FeatureManager.IsDeveloperMode;
    }

    public async Task InitializeAsync()
    {
        var mainWindow = App.MainWindowInstance;
        if (mainWindow == null) return;

        string jsonPath = GetJsonPath();
        _repoPath = !string.IsNullOrEmpty(jsonPath) ? Path.GetDirectoryName(jsonPath) ?? "" : "";

        await LoadAndDisplayDataAsync();
    }

    public async Task LoadAndDisplayDataAsync()
    {
        IsLoading = true;
        IsError = false;

        var mainWindow = App.MainWindowInstance;
        if (mainWindow == null) return;

        string jsonPath = GetJsonPath();

        if (File.Exists(jsonPath))
        {
            try
            {
                var root = AppsDataParser.LoadRoot(jsonPath);
                if (root != null)
                {
                    var (pNodes, pItems, _, _) = AppsDataParser.ParseData(root);

                    _allCategoryNodes = pNodes;
                    _leafCategoryNames.Clear();
                    CollectLeafCategories(pNodes, _leafCategoryNames);

                    CategoryNodes.Clear();
                    foreach (var node in pNodes)
                    {
                        CategoryNodes.Add(node);
                    }

                    _allParsedItems = pItems;
                    ExecuteSearch();

                    int totalCount = root.TotalCount > 0 ? root.TotalCount : mainWindow.UniqueAppsCount;
                    AppCountText = $"{totalCount} unique apps";
                    SearchPlaceholder = $"Search {totalCount} apps...";
                    IsLoading = false;
                    return;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading catalog from apps_data.json: {ex.Message}";
                IsError = true;
                IsLoading = false;
                return;
            }
        }

        IsError = true;
        ErrorMessage = "Could not find apps_data.json in the application path. Please run the app within the WinUI-3-Apps-List repository.";
        IsLoading = false;
    }

    public async Task RefreshDataAsync()
    {
        await LoadAndDisplayDataAsync();
        if (OnDataRefreshed != null)
        {
            await OnDataRefreshed.Invoke();
        }
    }

    private class SectionNode
    {
        public CatalogHeaderItem? Header { get; set; }
        public List<ICatalogItem> DirectItems { get; } = new();
        public List<SectionNode> Children { get; } = new();

        public int FilterAndCollect(Func<CatalogAppItem, bool> appFilter, List<ICatalogItem> output)
        {
            var matchingDirectItems = new List<ICatalogItem>();
            int directAppMatchCount = 0;

            foreach (var item in DirectItems)
            {
                if (item is CatalogAppItem app)
                {
                    if (appFilter(app))
                    {
                        matchingDirectItems.Add(app);
                        directAppMatchCount++;
                    }
                }
                else if (item is CatalogTextItem textItem)
                {
                    matchingDirectItems.Add(textItem);
                }
                else if (item is CatalogDividerItem dividerItem)
                {
                    matchingDirectItems.Add(dividerItem);
                }
            }

            var childrenOutput = new List<ICatalogItem>();
            int childrenMatchCount = 0;
            foreach (var child in Children)
            {
                childrenMatchCount += child.FilterAndCollect(appFilter, childrenOutput);
            }

            int totalMatchCount = directAppMatchCount + childrenMatchCount;

            if (totalMatchCount > 0)
            {
                if (Header != null)
                {
                    output.Add(Header);
                }

                if (directAppMatchCount > 0)
                {
                    output.AddRange(matchingDirectItems);
                }

                output.AddRange(childrenOutput);
            }

            return totalMatchCount;
        }
    }

    private static SectionNode BuildSectionTree(List<ICatalogItem> items)
    {
        var root = new SectionNode { Header = null };
        var stack = new Stack<SectionNode>();
        stack.Push(root);

        foreach (var item in items)
        {
            if (item is CatalogHeaderItem header)
            {
                var newNode = new SectionNode { Header = header };

                while (stack.Count > 1 && stack.Peek().Header != null && stack.Peek().Header!.Level >= header.Level)
                {
                    stack.Pop();
                }

                stack.Peek().Children.Add(newNode);
                stack.Push(newNode);
            }
            else
            {
                stack.Peek().DirectItems.Add(item);
            }
        }

        return root;
    }

    private bool MatchesPricing(CatalogAppItem app)
    {
        if (string.Equals(_pricingFilter, "Free", StringComparison.OrdinalIgnoreCase))
            return !app.IsPaid;
        if (string.Equals(_pricingFilter, "Foss", StringComparison.OrdinalIgnoreCase) || string.Equals(_pricingFilter, "FOSS", StringComparison.OrdinalIgnoreCase))
            return app.IsFoss;
        if (string.Equals(_pricingFilter, "Paid", StringComparison.OrdinalIgnoreCase))
            return app.IsPaid;
        return true;
    }

    private bool MatchesStyle(CatalogAppItem app)
    {
        if (string.Equals(_styleFilter, "WD", StringComparison.OrdinalIgnoreCase))
            return string.Equals(app.Indicator, "WD", StringComparison.OrdinalIgnoreCase);
        if (string.Equals(_styleFilter, "WDM", StringComparison.OrdinalIgnoreCase))
            return string.Equals(app.Indicator, "WDM", StringComparison.OrdinalIgnoreCase);
        if (string.Equals(_styleFilter, "WDA", StringComparison.OrdinalIgnoreCase))
            return string.Equals(app.Indicator, "WDA", StringComparison.OrdinalIgnoreCase);
        return true;
    }

    private void ExecuteSearch()
    {
        string query = _searchQuery.Trim();
        bool hasSearch = !string.IsNullOrEmpty(query);
        bool hasPricingFilter = !string.Equals(_pricingFilter, "All", StringComparison.OrdinalIgnoreCase);
        bool hasStyleFilter = !string.Equals(_styleFilter, "All", StringComparison.OrdinalIgnoreCase);

        List<ICatalogItem> targetList;

        if (!hasSearch && !hasPricingFilter && !hasStyleFilter)
        {
            targetList = _allParsedItems;
        }
        else
        {
            targetList = new List<ICatalogItem>();

            Func<CatalogAppItem, bool> combinedFilter = (app) =>
            {
                if (hasSearch && !app.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) return false;
                if (!MatchesPricing(app)) return false;
                if (!MatchesStyle(app)) return false;
                return true;
            };

            var sectionTree = BuildSectionTree(_allParsedItems);
            sectionTree.FilterAndCollect(combinedFilter, targetList);

            if (targetList.Count == 0)
            {
                targetList.Add(new CatalogTextItem { Text = "No apps found matching your search or filter criteria." });
            }
        }

        Remove_NonMatching(targetList);
        AddBack_Items(targetList);

        UpdateGridAppsCollection();
    }

    private void Remove_NonMatching(IEnumerable<ICatalogItem> filteredData)
    {
        for (int i = FilteredItems.Count - 1; i >= 0; i--)
        {
            var item = FilteredItems[i];
            if (!filteredData.Contains(item))
            {
                FilteredItems.RemoveAt(i);
            }
        }
    }

    private void AddBack_Items(IEnumerable<ICatalogItem> filteredData)
    {
        int index = 0;
        foreach (var item in filteredData)
        {
            if (!FilteredItems.Contains(item))
            {
                if (index < FilteredItems.Count)
                    FilteredItems.Insert(index, item);
                else
                    FilteredItems.Add(item);
            }
            index++;
        }
    }

    private void UpdateGridAppsCollection()
    {
        GroupedGridApps.Clear();
        var groups = new List<GridAppGroup>();

        for (int i = 0; i < FilteredItems.Count; i++)
        {
            if (FilteredItems[i] is CatalogHeaderItem header)
            {
                var groupApps = new List<CatalogAppItem>();
                for (int j = i + 1; j < FilteredItems.Count; j++)
                {
                    if (FilteredItems[j] is CatalogHeaderItem)
                    {
                        break;
                    }
                    if (FilteredItems[j] is CatalogAppItem app)
                    {
                        groupApps.Add(app);
                    }
                }

                if (groupApps.Count > 0)
                {
                    groups.Add(new GridAppGroup(header, groupApps));
                }
            }
        }

        foreach (var group in groups)
        {
            GroupedGridApps.Add(group);
        }
    }

    private void ExecuteCategorySearch()
    {
        string query = _categorySearchQuery.Trim();
        CategoryNodes.Clear();

        if (string.IsNullOrEmpty(query))
        {
            var displayNodes = _allCategoryNodes.Count == 1 && _allCategoryNodes[0].Name.Contains("Apps List")
                ? _allCategoryNodes[0].Children
                : (IEnumerable<CategoryNode>)_allCategoryNodes;

            foreach (var node in displayNodes)
            {
                CategoryNodes.Add(node);
            }
            return;
        }

        var filtered = FilterCategoryNodes(_allCategoryNodes, query);
        var filteredDisplayNodes = filtered.Count == 1 && filtered[0].Name.Contains("Apps List")
            ? filtered[0].Children
            : (IEnumerable<CategoryNode>)filtered;

        foreach (var node in filteredDisplayNodes)
        {
            CategoryNodes.Add(node);
        }
    }

    private List<CategoryNode> FilterCategoryNodes(List<CategoryNode> source, string query)
    {
        var result = new List<CategoryNode>();
        foreach (var node in source)
        {
            var filteredChildren = FilterCategoryNodes(node.Children.ToList(), query);
            bool nameMatches = node.Name.Contains(query, StringComparison.OrdinalIgnoreCase);

            if (nameMatches || filteredChildren.Count > 0)
            {
                var copy = new CategoryNode { Name = node.Name, Tag = node.Tag, IconPath = node.IconPath, IsExpanded = true };
                foreach (var child in filteredChildren) copy.Children.Add(child);
                result.Add(copy);
            }
        }
        return result;
    }

    public void ScrollToCategory(string tag)
    {
        OnScrollToCategoryRequested?.Invoke(tag);
    }

    public async Task CheckRemoteChangesAsync()
    {
        if (string.IsNullOrEmpty(_repoPath)) return;

        try
        {
            await RunGitCommandAsync("fetch");
            var (exitCode, output, _) = await RunGitCommandAsync("status -uno");
            if (exitCode == 0)
            {
                var match = Regex.Match(output, @"behind '.*' by (\d+) commit");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int count) && count > 0)
                {
                    SyncChangesText = $"Sync Changes ({count})";
                    IsSyncButtonVisible = true;
                    return;
                }
            }
        }
        catch { }

        IsSyncButtonVisible = false;
    }

    public async Task SyncChangesAsync()
    {
        if (string.IsNullOrEmpty(_repoPath)) return;
        IsSyncButtonEnabled = false;

        try
        {
            var (pullExit, _, pullErr) = await RunGitCommandAsync("pull --rebase");
            if (pullExit == 0)
            {
                var mainWindow = App.MainWindowInstance;
                if (mainWindow != null)
                {
                    await mainWindow.LoadAllCategoriesAndAppsAsync();
                    await RefreshDataAsync();
                }
            }
            else
            {
                ErrorMessage = $"Git pull failed: {pullErr}";
                IsError = true;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Git pull failed: {ex.Message}";
            IsError = true;
        }
        finally
        {
            IsSyncButtonEnabled = true;
            await CheckRemoteChangesAsync();
        }
    }

    public async Task AddAppToReadmeAsync(CatalogHeaderItem headerItem, AddAppDialog dialog) => await AddAppAsync(headerItem, dialog);

    public async Task AddAppAsync(CatalogHeaderItem headerItem, AddAppDialog dialog)
    {
        if (headerItem == null || dialog == null) return;
        string jsonPath = GetJsonPath();

        if (File.Exists(jsonPath))
        {
            try
            {
                var root = AppsDataParser.LoadRoot(jsonPath);
                if (root != null)
                {
                    var newApp = new AppsDataParser.JsonAppItem
                    {
                        Name = dialog.AppName,
                        Url = dialog.AppUrl,
                        Indicator = dialog.DesignIndicator,
                        Logo = dialog.LogoUrl,
                        IsFoss = dialog.IsFoss,
                        IsPaid = dialog.IsPaid,
                        IsPlanned = dialog.IsPlanned,
                        IsDiscontinued = dialog.IsDiscontinued,
                        IsTheme = dialog.IsTheme
                    };

                    bool added = AppsDataParser.AddAppToCategory(root, headerItem.Text, newApp);
                    if (added)
                    {
                        await AppsDataParser.SaveRootAsync(jsonPath, root);
                        var mainWin = App.MainWindowInstance;
                        if (mainWin != null) await mainWin.LoadAllCategoriesAndAppsAsync();
                        await RefreshDataAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to add app: {ex.Message}";
                IsError = true;
            }
        }
    }

    private string GetJsonPath()
    {
        var mainWin = App.MainWindowInstance;
        string? foundPath = mainWin?.FindDataJsonPath();
        if (!string.IsNullOrEmpty(foundPath) && foundPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase) && File.Exists(foundPath))
        {
            return foundPath;
        }

        string directory = !string.IsNullOrEmpty(_repoPath) ? _repoPath : AppDomain.CurrentDomain.BaseDirectory;
        string assetPath = Path.Combine(directory, "FluentDeck", "Assets", "data", "apps_data.json");
        if (File.Exists(assetPath)) return assetPath;

        string directAssetPath = Path.Combine(directory, "Assets", "data", "apps_data.json");
        if (File.Exists(directAssetPath)) return directAssetPath;

        return Path.Combine(directory, "apps_data.json");
    }

    public async Task UpdateAppAsync(AppItem appToEdit, AddAppDialog dialog)
    {
        string jsonPath = GetJsonPath();

        if (File.Exists(jsonPath))
        {
            try
            {
                var root = AppsDataParser.LoadRoot(jsonPath);
                if (root != null)
                {
                    var updatedApp = new AppsDataParser.JsonAppItem
                    {
                        Name = dialog.AppName,
                        Url = dialog.AppUrl,
                        Indicator = dialog.DesignIndicator,
                        Logo = dialog.LogoUrl,
                        IsFoss = dialog.IsFoss,
                        IsPaid = dialog.IsPaid,
                        IsPlanned = dialog.IsPlanned,
                        IsDiscontinued = dialog.IsDiscontinued,
                        IsTheme = dialog.IsTheme
                    };

                    bool updated = AppsDataParser.UpdateAppInJson(root, appToEdit.Name, appToEdit.Url, updatedApp);
                    if (updated)
                    {
                        await AppsDataParser.SaveRootAsync(jsonPath, root);
                        var mainWin = App.MainWindowInstance;
                        if (mainWin != null) await mainWin.LoadAllCategoriesAndAppsAsync();
                        await RefreshDataAsync();
                    }
                }
            }
            catch { }
        }
    }

    private async Task RunExtractLogosScriptAsync()
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "python",
                Arguments = "extract_logos.py",
                WorkingDirectory = _repoPath,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process != null) await process.WaitForExitAsync();
        }
        catch { }
    }

    private async Task<(int ExitCode, string Output, string Error)> RunGitCommandAsync(string arguments)
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = _repoPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new System.Diagnostics.Process { StartInfo = startInfo };
            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            return (process.ExitCode, output, error);
        }
        catch (Exception ex)
        {
            return (-1, "", ex.Message);
        }
    }

    private static int FindNextHeading(string content, int startIndex, int maxLevel)
    {
        int pos = startIndex;
        while (pos < content.Length)
        {
            int nl = content.IndexOf('\n', pos);
            if (nl == -1) break;
            int lineStart = nl + 1;
            if (lineStart >= content.Length) break;

            int hCount = 0;
            while (lineStart + hCount < content.Length && content[lineStart + hCount] == '#') hCount++;

            if (hCount >= 1 && hCount <= maxLevel && lineStart + hCount < content.Length && content[lineStart + hCount] == ' ')
                return nl;

            if (content.Length > lineStart + 2 && content[lineStart] == '-' && content[lineStart + 1] == '-' && content[lineStart + 2] == '-')
                return nl;

            pos = lineStart;
        }
        return -1;
    }

    private void CollectLeafCategories(IEnumerable<CategoryNode> nodes, HashSet<string> leafNames)
    {
        foreach (var node in nodes)
        {
            if (node.Children.Count == 0)
                leafNames.Add(CleanCategoryName(node.Name));
            else
                CollectLeafCategories(node.Children, leafNames);
        }
    }

    private static string CleanCategoryName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        string clean = Regex.Replace(name, @"[^a-zA-Z0-9\s]", " ");
        return Regex.Replace(clean, @"\s+", " ").Trim();
    }

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value)) return false;
        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
