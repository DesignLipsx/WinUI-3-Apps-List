using FluentDeck.Models;
using FluentDeck.Pages.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FluentDeck.Pages.Diagnostics;

public class FormattingIssue
{
    public string AppName { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public string LocationString => !string.IsNullOrEmpty(CategoryName) ? $"{AppName} ({CategoryName})" : AppName;
    public string LineNumberString => LocationString;
    public string Description { get; set; } = "";
    public string OriginalText { get; set; } = "";
    public string Suggestion { get; set; } = "";
    public string IssueType { get; set; } = ""; // "StoreUrl", "GitHubLogo", "DuplicateName", "DuplicateUrl"

    public string DisplayOriginalText => OriginalText;
    public string DisplaySuggestionText => Suggestion;

    public Visibility HasSuggestionVisibility => !string.IsNullOrEmpty(Suggestion) ? Visibility.Visible : Visibility.Collapsed;
    public Visibility CanFixVisibility => (!string.IsNullOrEmpty(Suggestion) || IssueType == "DuplicateName" || IssueType == "DuplicateUrl") ? Visibility.Visible : Visibility.Collapsed;

    public string DisplayTextBeforeQuery { get; set; } = "";
    public string DisplayQueryParam { get; set; } = "";
    public string DisplayTextAfterQuery { get; set; } = "";

    public Visibility IsStoreUrlIssueVisibility => IssueType == "StoreUrl" ? Visibility.Visible : Visibility.Collapsed;
    public Visibility IsNormalIssueVisibility => IssueType != "StoreUrl" ? Visibility.Visible : Visibility.Collapsed;
}

public class UrlCheckResult
{
    public string AppName { get; set; } = "";
    public string Url { get; set; } = "";
    public string StatusText { get; set; } = "";
    public string Message { get; set; } = "";
    public bool IsError { get; set; }
    public bool IsRedirect { get; set; }
    public bool IsArchived { get; set; }
    public bool IsLogo { get; set; }
    public string? NewUrl { get; set; }

    public string IconGlyph => IsLogo ? "\uEB9F" : "\uE774";
    public string GroupTag => IsArchived ? "Archived Repo" : IsRedirect ? "Redirected" : IsLogo ? "Broken Logo" : "Broken Repo";

    public Visibility HasMessageVisibility => !string.IsNullOrEmpty(Message) ? Visibility.Visible : Visibility.Collapsed;
    public Visibility CanUpdateUrlVisibility => !string.IsNullOrEmpty(NewUrl) ? Visibility.Visible : Visibility.Collapsed;
    public Visibility CanClearLogoVisibility => IsLogo && IsError ? Visibility.Visible : Visibility.Collapsed;
    public Visibility CanMarkArchivedVisibility => IsArchived ? Visibility.Visible : Visibility.Collapsed;

    public Brush BadgeBackground => IsArchived || IsRedirect
        ? (Brush)Application.Current.Resources["SystemFillColorCautionBackgroundBrush"]
        : (Brush)Application.Current.Resources["SystemFillColorCriticalBackgroundBrush"];

    public Brush BadgeForeground => IsArchived || IsRedirect
        ? (Brush)Application.Current.Resources["SystemFillColorCautionBrush"]
        : (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
}

public class UrlResultGroup : List<UrlCheckResult>
{
    public string Title { get; set; } = "";
    public string Key { get; set; } = "";
    public string CountText => $"{Count} issue(s)";
    public Visibility CanMarkAllArchivedVisibility => Key == "Archived" ? Visibility.Visible : Visibility.Collapsed;
    public Visibility CanUpdateAllUrlsVisibility => Key == "Redirect" ? Visibility.Visible : Visibility.Collapsed;
    public Visibility CanRemoveAllVisibility => Key == "Error" ? Visibility.Visible : Visibility.Collapsed;
    public Visibility CanClearAllLogosVisibility => Key == "LogoError" ? Visibility.Visible : Visibility.Collapsed;

    public UrlResultGroup(string title, string key, IEnumerable<UrlCheckResult> items) : base(items)
    {
        Title = title;
        Key = key;
    }
}

public sealed partial class DiagnosticsPage : Page
{
    private string? _jsonPath;
    private readonly ObservableCollection<FormattingIssue> _allIssues = new();
    private readonly ObservableCollection<UrlCheckResult> _allUrlResults = new();
    private readonly ObservableCollection<UrlCheckResult> _filteredUrlResults = new();
    private int _totalAppsCount = 0;
    private CancellationTokenSource? _urlCheckCts;
    private int _previousSelectedIndex = 0;

    public ObservableCollection<FormattingIssue> AllIssues => _allIssues;
    public ObservableCollection<UrlCheckResult> AllUrlResults => _allUrlResults;
    public ObservableCollection<UrlCheckResult> FilteredUrlResults => _filteredUrlResults;

    public DiagnosticsPage()
    {
        InitializeComponent();
        Loaded += DiagnosticsPage_Loaded;
    }

    private void DiagnosticsPage_Loaded(object sender, RoutedEventArgs e)
    {
        var mainWindow = App.MainWindowInstance;
        if (mainWindow != null)
        {
            _jsonPath = mainWindow.FindDataJsonPath();
            RunInitialScan();
        }
        ContentFrame.Navigate(typeof(DiagnosticsFormattingPage), this);
    }

    private class JsonAppRef
    {
        public string CategoryName { get; set; } = "";
        public AppsDataParser.JsonAppItem App { get; set; } = new();
        public List<AppsDataParser.JsonAppItem> ContainerList { get; set; } = new();
    }

    private bool IsSpecialSection(string? categoryName)
    {
        if (string.IsNullOrEmpty(categoryName)) return false;
        return categoryName.Contains("Newly Added", StringComparison.OrdinalIgnoreCase) ||
               categoryName.Contains("Best Implementation", StringComparison.OrdinalIgnoreCase);
    }

    private List<JsonAppRef> GetAllApps(AppsDataParser.AppsDataRoot root)
    {
        var list = new List<JsonAppRef>();
        if (root == null) return list;

        if (root.BestImplementation != null)
        {
            foreach (var app in root.BestImplementation)
            {
                list.Add(new JsonAppRef
                {
                    CategoryName = "Best Implementation of WinUI",
                    App = app,
                    ContainerList = root.BestImplementation
                });
            }
        }

        if (root.NewlyAdded != null)
        {
            foreach (var app in root.NewlyAdded)
            {
                list.Add(new JsonAppRef
                {
                    CategoryName = "Newly Added Apps!",
                    App = app,
                    ContainerList = root.NewlyAdded
                });
            }
        }

        if (root.Categories != null)
        {
            foreach (var catNode in root.Categories)
            {
                TraverseCategoryNode(catNode, catNode.Name, list);
            }
        }

        return list;
    }

    private void TraverseCategoryNode(AppsDataParser.JsonCategoryNode node, string categoryPath, List<JsonAppRef> list)
    {
        if (node == null) return;
        if (node.Apps != null)
        {
            foreach (var app in node.Apps)
            {
                list.Add(new JsonAppRef
                {
                    CategoryName = categoryPath,
                    App = app,
                    ContainerList = node.Apps
                });
            }
        }
        if (node.Subcategories != null)
        {
            foreach (var sub in node.Subcategories)
            {
                string path = string.IsNullOrEmpty(categoryPath) ? sub.Name : $"{categoryPath} > {sub.Name}";
                TraverseCategoryNode(sub, path, list);
            }
        }
    }

    private void RunInitialScan()
    {
        if (string.IsNullOrEmpty(_jsonPath) || !File.Exists(_jsonPath))
        {
            ProgressStatusTxt.Text = "apps_data.json not found.";
            return;
        }

        try
        {
            var root = AppsDataParser.LoadRoot(_jsonPath);
            if (root == null)
            {
                ProgressStatusTxt.Text = "Failed to parse apps_data.json.";
                return;
            }

            var allAppRefs = GetAllApps(root);
            var scannedIssues = new List<FormattingIssue>();

            var namesSeen = new Dictionary<string, (string Category, AppsDataParser.JsonAppItem App)>(StringComparer.OrdinalIgnoreCase);
            var urlsSeen = new Dictionary<string, (string Category, AppsDataParser.JsonAppItem App)>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in allAppRefs)
            {
                var app = item.App;
                string catName = item.CategoryName;

                // 1. Check Store URLs for query parameters
                if (!string.IsNullOrEmpty(app.Url))
                {
                    var storeUrlMatch = Regex.Match(app.Url, @"(https://apps\.microsoft\.com/(?:[a-zA-Z0-9\-]+/)?(?:store/)?detail/[^\?\)\s]+)\?([^\)\s]+)");
                    if (storeUrlMatch.Success)
                    {
                        string fullUrl = storeUrlMatch.Value;
                        string cleanedUrl = storeUrlMatch.Groups[1].Value;
                        string queryParam = storeUrlMatch.Groups[2].Value;

                        scannedIssues.Add(new FormattingIssue
                        {
                            AppName = app.Name,
                            CategoryName = catName,
                            Description = $"Microsoft Store URL for \"{app.Name}\" contains unnecessary query parameters.",
                            OriginalText = app.Url,
                            Suggestion = cleanedUrl,
                            IssueType = "StoreUrl",
                            DisplayTextBeforeQuery = cleanedUrl,
                            DisplayQueryParam = queryParam,
                            DisplayTextAfterQuery = ""
                        });
                    }
                }

                // 2. Check GitHub Logo URLs (blob vs raw)
                if (!string.IsNullOrEmpty(app.Logo))
                {
                    var githubLogoMatch = Regex.Match(app.Logo, @"https://github\.com/([a-zA-Z0-9\-\._]+)/([a-zA-Z0-9\-\._]+)/blob/([a-zA-Z0-9\-\._]+)/([^?\s>]+)");
                    if (githubLogoMatch.Success)
                    {
                        string owner = githubLogoMatch.Groups[1].Value;
                        string repo = githubLogoMatch.Groups[2].Value;
                        string branch = githubLogoMatch.Groups[3].Value;
                        string path = githubLogoMatch.Groups[4].Value;

                        string correctedUrl = $"https://raw.githubusercontent.com/{owner}/{repo}/refs/heads/{branch}/{path}";

                        scannedIssues.Add(new FormattingIssue
                        {
                            AppName = app.Name,
                            CategoryName = catName,
                            Description = $"GitHub logo URL for \"{app.Name}\" uses blob view instead of raw user content link.",
                            OriginalText = app.Logo,
                            Suggestion = correctedUrl,
                            IssueType = "GitHubLogo"
                        });
                    }
                }

                // 3. Duplicate checks (ignoring if entry is in Newly Added Apps! or Best Implementation of WinUI)
                if (!IsSpecialSection(catName))
                {
                    (string Category, AppsDataParser.JsonAppItem App) prevNameSeen = default;
                    bool isNameDup = namesSeen.TryGetValue(app.Name, out prevNameSeen) && !IsSpecialSection(prevNameSeen.Category);

                    (string Category, AppsDataParser.JsonAppItem App) prevUrlSeen = default;
                    bool isUrlDup = !string.IsNullOrEmpty(app.Url) && urlsSeen.TryGetValue(app.Url, out prevUrlSeen) && !IsSpecialSection(prevUrlSeen.Category);

                    if (isNameDup || isUrlDup)
                    {
                        string desc = "";
                        string issueType = "";

                        if (isNameDup && isUrlDup)
                        {
                            desc = $"Duplicate App Listing: Name \"{app.Name}\" and URL are duplicates of an entry in '{prevNameSeen.Category}'.";
                            issueType = "DuplicateName";
                        }
                        else if (isNameDup)
                        {
                            desc = $"Duplicate App Name: \"{app.Name}\" is already listed in '{prevNameSeen.Category}'.";
                            issueType = "DuplicateName";
                        }
                        else
                        {
                            desc = $"Duplicate App URL for \"{app.Name}\": URL is already used by an app in '{prevUrlSeen.Category}'.";
                            issueType = "DuplicateUrl";
                        }

                        scannedIssues.Add(new FormattingIssue
                        {
                            AppName = app.Name,
                            CategoryName = catName,
                            Description = desc,
                            OriginalText = $"{app.Name} - {app.Url}",
                            IssueType = issueType
                        });
                    }
                    else
                    {
                        namesSeen[app.Name] = (catName, app);
                        if (!string.IsNullOrEmpty(app.Url))
                        {
                            urlsSeen[app.Url] = (catName, app);
                        }
                    }
                }
            }

            _totalAppsCount = allAppRefs.Select(x => x.App.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            TotalAppsTxt.Text = _totalAppsCount.ToString();

            Remove_NonMatchingIssues(scannedIssues);
            AddBack_Issues(scannedIssues);

            FormattingIssuesTxt.Text = _allIssues.Count.ToString();
            ProgressStatusTxt.Text = $"Scan completed. Scanned {allAppRefs.Count} apps in JSON. Found {_allIssues.Count} issue(s).";

            UpdateButtonStates();
        }
        catch (Exception ex)
        {
            ProgressStatusTxt.Text = "Error scanning JSON file: " + ex.Message;
        }
    }

    private void Remove_NonMatchingIssues(IEnumerable<FormattingIssue> filteredData)
    {
        for (int i = _allIssues.Count - 1; i >= 0; i--)
        {
            var item = _allIssues[i];
            if (!filteredData.Any(x => x.AppName == item.AppName && x.IssueType == item.IssueType))
            {
                _allIssues.RemoveAt(i);
            }
        }
    }

    private void AddBack_Issues(IEnumerable<FormattingIssue> filteredData)
    {
        foreach (var item in filteredData)
        {
            if (!_allIssues.Any(x => x.AppName == item.AppName && x.IssueType == item.IssueType))
            {
                _allIssues.Add(item);
            }
        }
    }

    private void UpdateButtonStates()
    {
        if (CleanStoreUrlsBtn == null || FixGitHubLogosBtn == null || FixAllBtn == null) return;

        bool canCleanStore = _allIssues.Any(x => x.IssueType == "StoreUrl");
        CleanStoreUrlsBtn.IsEnabled = canCleanStore;
        CleanStoreUrlsBtn.Visibility = canCleanStore ? Visibility.Visible : Visibility.Collapsed;

        bool canFixGitHubLogos = _allIssues.Any(x => x.IssueType == "GitHubLogo");
        FixGitHubLogosBtn.IsEnabled = canFixGitHubLogos;
        FixGitHubLogosBtn.Visibility = canFixGitHubLogos ? Visibility.Visible : Visibility.Collapsed;

        bool canFixAll = _allIssues.Any(x => !string.IsNullOrEmpty(x.Suggestion));
        FixAllBtn.IsEnabled = canFixAll;
        FixAllBtn.Visibility = canFixAll ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ScanBtn_Click(SplitButtonClickEventArgs args) => RunInitialScan();
    private void ScanBtn_Click(object sender, RoutedEventArgs e) => RunInitialScan();

    private async void CheckUrlsBtn_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_jsonPath) || !File.Exists(_jsonPath)) return;

        _urlCheckCts?.Cancel();
        _urlCheckCts = new CancellationTokenSource();
        var token = _urlCheckCts.Token;

        _allUrlResults.Clear();
        _filteredUrlResults.Clear();
        DeadUrlsTxt.Text = "0";

        WorkProgressBar.Visibility = Visibility.Visible;
        WorkProgressBar.Value = 0;

        try
        {
            var root = AppsDataParser.LoadRoot(_jsonPath);
            if (root == null)
            {
                ProgressStatusTxt.Text = "Failed to load JSON for URL check.";
                WorkProgressBar.Visibility = Visibility.Collapsed;
                return;
            }

            var allAppRefs = GetAllApps(root);
            var targets = new List<(string AppName, string Url, bool IsLogo)>();

            foreach (var item in allAppRefs)
            {
                var app = item.App;
                if (!string.IsNullOrWhiteSpace(app.Url) && app.Url.Contains("github", StringComparison.OrdinalIgnoreCase))
                {
                    targets.Add((app.Name, app.Url, false));
                }
                if (!string.IsNullOrWhiteSpace(app.Logo) && app.Logo != "nan")
                {
                    targets.Add((app.Name, app.Logo, true));
                }
            }

            int total = targets.Count;
            if (total == 0)
            {
                ProgressStatusTxt.Text = "No GitHub URLs or Logos found to check.";
                WorkProgressBar.Visibility = Visibility.Collapsed;
                return;
            }

            int processed = 0;
            int warnings = 0;
            DateTime lastFilterUpdate = DateTime.MinValue;

            using var httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var semaphore = new SemaphoreSlim(15);
            var tasks = targets.Select(async target =>
            {
                await semaphore.WaitAsync(token);
                try
                {
                    if (token.IsCancellationRequested) return;

                    var result = new UrlCheckResult
                    {
                        AppName = target.AppName,
                        Url = target.Url,
                        IsLogo = target.IsLogo
                    };

                    if (target.IsLogo)
                    {
                        await ValidateLogoUrlAsync(target.Url, result, httpClient, token);
                    }
                    else if (target.Url.Contains("github.com", StringComparison.OrdinalIgnoreCase))
                    {
                        await ValidateGitHubRepositoryAsync(target.Url, result, httpClient, token);
                    }
                    else
                    {
                        await ValidateGeneralUrlAsync(target.Url, result, httpClient, token);
                    }

                    DispatcherQueue.TryEnqueue(() =>
                    {
                        _allUrlResults.Add(result);
                        if (result.IsError || result.IsRedirect)
                        {
                            warnings++;
                            DeadUrlsTxt.Text = warnings.ToString();
                        }

                        processed++;
                        WorkProgressBar.Value = (double)processed / total * 100;
                        ProgressStatusTxt.Text = $"Checking GitHub URLs & Logos: {processed} / {total}...";

                        // Throttle list UI updates to once per 1.5 seconds to prevent jumpy/flickering UI during scan
                        if ((DateTime.Now - lastFilterUpdate).TotalMilliseconds > 1500 || processed == total)
                        {
                            lastFilterUpdate = DateTime.Now;
                            ApplyUrlFilter();
                        }
                    });
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToArray();

            await Task.WhenAll(tasks);
            ApplyUrlFilter();
            ProgressStatusTxt.Text = $"Finished scan. Checked {total} GitHub links & logos. Found {warnings} warnings/errors.";
        }
        catch (OperationCanceledException)
        {
            ApplyUrlFilter();
            ProgressStatusTxt.Text = "URL check cancelled.";
        }
        catch (Exception ex)
        {
            ApplyUrlFilter();
            ProgressStatusTxt.Text = "URL check failed: " + ex.Message;
        }
        finally
        {
            WorkProgressBar.Visibility = Visibility.Collapsed;
        }
    }

    private async Task ValidateLogoUrlAsync(string url, UrlCheckResult result, HttpClient client, CancellationToken token)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            int statusCode = (int)response.StatusCode;

            if (statusCode >= 200 && statusCode < 300)
            {
                result.StatusText = "✔ OK (200)";
                result.IsError = false;
            }
            else if (statusCode == 301 || statusCode == 302 || statusCode == 307 || statusCode == 308)
            {
                string? redirectUrl = response.Headers.Location?.ToString();
                result.StatusText = "🔁 Logo Redirected";
                result.IsRedirect = true;
                result.NewUrl = redirectUrl;
                result.Message = !string.IsNullOrEmpty(redirectUrl)
                    ? $"Logo image redirected. Suggestion: {redirectUrl}"
                    : "Logo image redirected.";
            }
            else if (statusCode == 404)
            {
                result.StatusText = "❌ Logo Not Found (404)";
                result.IsError = true;
                result.Message = "Logo image link is broken (404 Not Found).";
            }
            else if (statusCode == 403)
            {
                result.StatusText = "🚫 Forbidden (403)";
                result.IsError = true;
                result.Message = "Access forbidden to logo image.";
            }
            else
            {
                result.StatusText = $"HTTP {statusCode}";
                result.IsError = true;
                result.Message = $"Logo returned HTTP status {statusCode}";
            }
        }
        catch (TaskCanceledException)
        {
            result.StatusText = "⏳ Logo Timeout";
            result.IsError = true;
            result.Message = "Timeout loading logo image.";
        }
        catch (Exception ex)
        {
            result.StatusText = "❌ Logo Check Failed";
            result.IsError = true;
            result.Message = ex.Message;
        }
    }

    private async Task ValidateGitHubRepositoryAsync(string url, UrlCheckResult result, HttpClient client, CancellationToken token)
    {
        var match = Regex.Match(url, @"github\.com/([a-zA-Z0-9\-\._]+)/([a-zA-Z0-9\-\._]+)");
        if (!match.Success)
        {
            await ValidateGeneralUrlAsync(url, result, client, token);
            return;
        }

        string owner = match.Groups[1].Value;
        string repo = match.Groups[2].Value;

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{owner}/{repo}");
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            request.Headers.Add("Accept", "application/json");

            string pat = SettingsPage.GetSavedGitHubPat();
            if (!string.IsNullOrEmpty(pat))
            {
                request.Headers.Add("Authorization", $"token {pat}");
            }

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            int statusCode = (int)response.StatusCode;

            if (statusCode == 200)
            {
                string json = await response.Content.ReadAsStringAsync(token);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                bool archived = root.TryGetProperty("archived", out var archProp) && archProp.GetBoolean();
                bool disabled = root.TryGetProperty("disabled", out var disProp) && disProp.GetBoolean();

                if (archived)
                {
                    result.StatusText = "⚠ Archived Repo";
                    result.IsRedirect = true;
                    result.IsArchived = true;
                    result.Message = "GitHub repository is archived.";
                }
                else if (disabled)
                {
                    result.StatusText = "❌ Disabled Repo";
                    result.IsError = true;
                    result.Message = "GitHub repository is disabled.";
                }
                else
                {
                    result.StatusText = "✔ OK (200)";
                    result.IsError = false;
                }
            }
            else if (statusCode == 301 || statusCode == 302 || statusCode == 307)
            {
                string? redirectUrl = response.Headers.Location?.ToString();
                result.StatusText = "🔁 Redirected (301)";
                result.IsRedirect = true;
                result.NewUrl = redirectUrl;
                result.Message = $"Repo renamed. Suggestion: {redirectUrl}";

                if (!string.IsNullOrEmpty(redirectUrl))
                {
                    try
                    {
                        var subReq = new HttpRequestMessage(HttpMethod.Get, redirectUrl);
                        subReq.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                        subReq.Headers.Add("Accept", "application/json");
                        if (!string.IsNullOrEmpty(pat))
                        {
                            subReq.Headers.Add("Authorization", $"token {pat}");
                        }
                        var subResp = await client.SendAsync(subReq, HttpCompletionOption.ResponseHeadersRead, token);
                        if (subResp.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            string subJson = await subResp.Content.ReadAsStringAsync(token);
                            using var subDoc = JsonDocument.Parse(subJson);
                            if (subDoc.RootElement.TryGetProperty("html_url", out var htmlProp))
                            {
                                string userFacingUrl = htmlProp.GetString() ?? "";
                                if (!string.IsNullOrEmpty(userFacingUrl))
                                {
                                    result.NewUrl = userFacingUrl;
                                    result.Message = $"Repo renamed. Suggestion: {userFacingUrl}";
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            else if (statusCode == 404)
            {
                result.StatusText = "❌ Not Found (404)";
                result.IsError = true;
                result.Message = "Repository does not exist.";
            }
            else if (statusCode == 403)
            {
                result.StatusText = "🚫 Forbidden (403)";
                result.IsError = true;
                result.Message = "Rate limited or access forbidden.";
            }
            else
            {
                result.StatusText = $"HTTP {statusCode}";
                result.IsError = true;
            }
        }
        catch (Exception ex)
        {
            result.StatusText = "⏳ Timeout / Failed";
            result.IsError = true;
            result.Message = ex.Message;
        }
    }

    private async Task ValidateMicrosoftStoreProductAsync(string url, UrlCheckResult result, HttpClient client, CancellationToken token)
    {
        var match = Regex.Match(url, @"/detail/([a-zA-Z0-9]+)");
        if (!match.Success)
        {
            await ValidateGeneralUrlAsync(url, result, client, token);
            return;
        }

        string productId = match.Groups[1].Value;
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://apps.microsoft.com/detail/{productId}");
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
            request.Headers.Add("Accept-Language", "en-US,en;q=0.9");
            request.Headers.Add("Cache-Control", "no-cache");

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            int statusCode = (int)response.StatusCode;

            if (statusCode == 200)
            {
                result.StatusText = "✔ OK (200)";
                result.IsError = false;
            }
            else if (statusCode == 404)
            {
                result.StatusText = "❌ Not Found (404)";
                result.IsError = true;
                result.Message = "App removed or product ID is invalid.";
            }
            else if (statusCode == 301 || statusCode == 302)
            {
                result.StatusText = "🔁 Redirected (301)";
                result.IsRedirect = true;
                result.Message = "Redirected to region page or new detail link.";
            }
            else
            {
                result.StatusText = $"HTTP {statusCode}";
                result.IsError = true;
            }
        }
        catch (Exception ex)
        {
            result.StatusText = "⏳ Timeout / Failed";
            result.IsError = true;
            result.Message = ex.Message;
        }
    }

    private async Task ValidateGeneralUrlAsync(string url, UrlCheckResult result, HttpClient client, CancellationToken token)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Head, url);
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
            request.Headers.Add("Accept-Language", "en-US,en;q=0.9");

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            int statusCode = (int)response.StatusCode;

            if (statusCode == 405 || statusCode == 403 || statusCode == 501)
            {
                var getRequest = new HttpRequestMessage(HttpMethod.Get, url);
                getRequest.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                getRequest.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
                getRequest.Headers.Add("Accept-Language", "en-US,en;q=0.9");

                response = await client.SendAsync(getRequest, HttpCompletionOption.ResponseHeadersRead, token);
                statusCode = (int)response.StatusCode;
            }

            if (statusCode >= 200 && statusCode < 300)
            {
                result.StatusText = "✔ OK (200)";
                result.IsError = false;
            }
            else if (statusCode >= 300 && statusCode < 400)
            {
                result.StatusText = "🔁 Redirected (301)";
                result.IsRedirect = true;
                result.NewUrl = response.Headers.Location?.ToString();
                result.Message = "Redirects to: " + result.NewUrl;
            }
            else if (statusCode == 403)
            {
                result.StatusText = "🚫 Forbidden (403)";
                result.IsError = true;
                result.Message = "Access forbidden. Page might still load in browser.";
            }
            else if (statusCode == 429)
            {
                result.StatusText = "⚠ Rate Limited (429)";
                result.IsRedirect = true;
                result.Message = "Too many requests to this server.";
            }
            else if (statusCode == 404)
            {
                result.StatusText = "❌ Not Found (404)";
                result.IsError = true;
            }
            else
            {
                result.StatusText = $"HTTP {statusCode}";
                result.IsError = true;
            }
        }
        catch (TaskCanceledException)
        {
            result.StatusText = "⏳ Timeout";
            result.IsError = true;
        }
        catch (Exception ex)
        {
            result.StatusText = "❌ Failed";
            result.IsError = true;
            result.Message = ex.Message;
        }
    }

    private void ApplyUrlFilter()
    {
        Remove_NonMatching(_allUrlResults);
        AddBack_Urls(_allUrlResults);
    }

    private void Remove_NonMatching(IEnumerable<UrlCheckResult> filteredData)
    {
        for (int i = _filteredUrlResults.Count - 1; i >= 0; i--)
        {
            var item = _filteredUrlResults[i];
            if (!filteredData.Contains(item))
            {
                _filteredUrlResults.RemoveAt(i);
            }
        }
    }

    private void AddBack_Urls(IEnumerable<UrlCheckResult> filteredData)
    {
        foreach (var item in filteredData)
        {
            if (!_filteredUrlResults.Contains(item))
            {
                _filteredUrlResults.Add(item);
            }
        }
    }

    internal async void FixIssueBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is FormattingIssue issue && !string.IsNullOrEmpty(_jsonPath))
        {
            if (issue.IssueType == "DuplicateName")
            {
                await ShowDuplicateResolutionDialogAsync(issue, "name");
                return;
            }
            else if (issue.IssueType == "DuplicateUrl")
            {
                await ShowDuplicateResolutionDialogAsync(issue, "url");
                return;
            }

            try
            {
                var root = AppsDataParser.LoadRoot(_jsonPath);
                if (root == null) return;

                var allAppRefs = GetAllApps(root);
                var targetAppRef = allAppRefs.FirstOrDefault(x => x.App.Name.Equals(issue.AppName, StringComparison.OrdinalIgnoreCase));
                if (targetAppRef != null)
                {
                    if (issue.IssueType == "StoreUrl")
                    {
                        targetAppRef.App.Url = issue.Suggestion;
                    }
                    else if (issue.IssueType == "GitHubLogo")
                    {
                        targetAppRef.App.Logo = issue.Suggestion;
                    }

                    await AppsDataParser.SaveRootAsync(_jsonPath, root);
                    if (App.MainWindowInstance != null)
                    {
                        await App.MainWindowInstance.LoadAllCategoriesAndAppsAsync();
                    }
                    RunInitialScan();
                }
            }
            catch (Exception ex)
            {
                ProgressStatusTxt.Text = "Error fixing issue in JSON: " + ex.Message;
            }
        }
    }

    private async Task ShowDuplicateResolutionDialogAsync(FormattingIssue issue, string type)
    {
        if (string.IsNullOrEmpty(_jsonPath) || !File.Exists(_jsonPath)) return;

        try
        {
            var root = AppsDataParser.LoadRoot(_jsonPath);
            if (root == null) return;

            var allAppRefs = GetAllApps(root);
            var matchingApps = allAppRefs.Where(x => x.App.Name.Equals(issue.AppName, StringComparison.OrdinalIgnoreCase) ||
                                                    (!string.IsNullOrEmpty(issue.OriginalText) && issue.OriginalText.Contains(x.App.Url))).ToList();

            if (matchingApps.Count < 2) return;

            var appA = matchingApps[0];
            var appB = matchingApps[1];

            var stack = new StackPanel { Spacing = 12 };
            stack.Children.Add(new TextBlock
            {
                Text = $"Duplicate app {type} found. Please select which entry to KEEP (the other will be deleted):",
                TextWrapping = TextWrapping.Wrap
            });

            var radio1 = new RadioButton
            {
                Content = $"Keep entry in '{appA.CategoryName}'",
                IsChecked = true
            };
            var previewA = new TextBlock
            {
                Text = $"{appA.App.Name} ({appA.App.Url})",
                Margin = new Thickness(24, 0, 0, 4),
                FontStyle = Windows.UI.Text.FontStyle.Italic,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            };

            var radio2 = new RadioButton
            {
                Content = $"Keep entry in '{appB.CategoryName}'"
            };
            var previewB = new TextBlock
            {
                Text = $"{appB.App.Name} ({appB.App.Url})",
                Margin = new Thickness(24, 0, 0, 4),
                FontStyle = Windows.UI.Text.FontStyle.Italic,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            };

            stack.Children.Add(radio1);
            stack.Children.Add(previewA);
            stack.Children.Add(radio2);
            stack.Children.Add(previewB);

            var dialog = new ContentDialog
            {
                Title = type == "url" ? "Resolve Duplicate URL" : "Resolve Duplicate App Name",
                Content = stack,
                PrimaryButtonText = "Save Resolution",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
                Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"]
            };

            var dialogResult = await dialog.ShowAsync();
            if (dialogResult == ContentDialogResult.Primary)
            {
                if (radio1.IsChecked == true)
                {
                    appB.ContainerList.Remove(appB.App);
                }
                else
                {
                    appA.ContainerList.Remove(appA.App);
                }

                await AppsDataParser.SaveRootAsync(_jsonPath, root);
                if (App.MainWindowInstance != null)
                {
                    await App.MainWindowInstance.LoadAllCategoriesAndAppsAsync();
                }
                RunInitialScan();
                ProgressStatusTxt.Text = $"Successfully resolved duplicate app {type}.";
            }
        }
        catch (Exception ex)
        {
            ProgressStatusTxt.Text = "Error resolving duplicate: " + ex.Message;
        }
    }

    private async void OpenUrl(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }
    }

    private void FixAllBtn_Click(object sender, RoutedEventArgs e)
    {
        ApplyAllFixesOfTypes(null);
    }

    private void CleanStoreUrls_Click(object sender, RoutedEventArgs e)
    {
        ApplyAllFixesOfTypes("StoreUrl");
    }

    private void FixGitHubLogos_Click(object sender, RoutedEventArgs e)
    {
        ApplyAllFixesOfTypes("GitHubLogo");
    }

    private async void ApplyAllFixesOfTypes(string? filterType)
    {
        if (string.IsNullOrEmpty(_jsonPath) || !File.Exists(_jsonPath)) return;

        try
        {
            var root = AppsDataParser.LoadRoot(_jsonPath);
            if (root == null) return;

            int fixCount = 0;
            var fixable = _allIssues.Where(x => !string.IsNullOrEmpty(x.Suggestion) && (filterType == null || x.IssueType == filterType)).ToList();

            var allAppRefs = GetAllApps(root);

            foreach (var issue in fixable)
            {
                var targetAppRef = allAppRefs.FirstOrDefault(x => x.App.Name.Equals(issue.AppName, StringComparison.OrdinalIgnoreCase));
                if (targetAppRef != null)
                {
                    if (issue.IssueType == "StoreUrl")
                    {
                        targetAppRef.App.Url = issue.Suggestion;
                        fixCount++;
                    }
                    else if (issue.IssueType == "GitHubLogo")
                    {
                        targetAppRef.App.Logo = issue.Suggestion;
                        fixCount++;
                    }
                }
            }

            if (fixCount > 0)
            {
                await AppsDataParser.SaveRootAsync(_jsonPath, root);
                if (App.MainWindowInstance != null)
                {
                    await App.MainWindowInstance.LoadAllCategoriesAndAppsAsync();
                }
                RunInitialScan();
                ProgressStatusTxt.Text = $"Successfully auto-fixed {fixCount} issue(s) in JSON.";
            }
            else
            {
                ProgressStatusTxt.Text = "No fixable issues found.";
            }
        }
        catch (Exception ex)
        {
            ProgressStatusTxt.Text = "Error applying fixes to JSON: " + ex.Message;
        }
    }

    internal void OpenUrlBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is string url)
        {
            OpenUrl(url);
        }
    }

    internal async void UpdateUrlBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is UrlCheckResult result && !string.IsNullOrEmpty(result.NewUrl) && !string.IsNullOrEmpty(_jsonPath))
        {
            try
            {
                var root = AppsDataParser.LoadRoot(_jsonPath);
                if (root == null) return;

                var allAppRefs = GetAllApps(root);
                int fixCount = 0;

                foreach (var item in allAppRefs)
                {
                    if (result.IsLogo && item.App.Logo == result.Url)
                    {
                        item.App.Logo = result.NewUrl;
                        fixCount++;
                    }
                    else if (!result.IsLogo && item.App.Url == result.Url)
                    {
                        item.App.Url = result.NewUrl;
                        fixCount++;
                    }
                }

                if (fixCount > 0)
                {
                    await AppsDataParser.SaveRootAsync(_jsonPath, root);
                    if (App.MainWindowInstance != null)
                    {
                        await App.MainWindowInstance.LoadAllCategoriesAndAppsAsync();
                    }
                    ProgressStatusTxt.Text = $"Successfully updated {fixCount} link(s) in JSON.";
                    result.Url = result.NewUrl;
                    result.NewUrl = null;
                    result.StatusText = "✔ OK (200)";
                    result.IsRedirect = false;
                    result.Message = "Link successfully updated in JSON.";
                    ApplyUrlFilter();
                }
            }
            catch (Exception ex)
            {
                ProgressStatusTxt.Text = "Error updating URL in JSON: " + ex.Message;
            }
        }
    }

    internal async void ClearLogoBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is UrlCheckResult result && !string.IsNullOrEmpty(_jsonPath))
        {
            try
            {
                var root = AppsDataParser.LoadRoot(_jsonPath);
                if (root == null) return;

                var allAppRefs = GetAllApps(root);
                int fixCount = 0;

                foreach (var item in allAppRefs)
                {
                    if (item.App.Logo == result.Url || item.App.Name.Equals(result.AppName, StringComparison.OrdinalIgnoreCase))
                    {
                        item.App.Logo = "";
                        fixCount++;
                    }
                }

                if (fixCount > 0)
                {
                    await AppsDataParser.SaveRootAsync(_jsonPath, root);
                    if (App.MainWindowInstance != null)
                    {
                        await App.MainWindowInstance.LoadAllCategoriesAndAppsAsync();
                    }
                    ProgressStatusTxt.Text = $"Cleared logo link for {result.AppName} in JSON.";
                    _allUrlResults.Remove(result);
                    ApplyUrlFilter();
                }
            }
            catch (Exception ex)
            {
                ProgressStatusTxt.Text = "Error clearing logo link: " + ex.Message;
            }
        }
    }

    internal async Task ClearAllLogosAsync(List<UrlCheckResult> results)
    {
        if (string.IsNullOrEmpty(_jsonPath) || !File.Exists(_jsonPath) || results.Count == 0) return;

        try
        {
            var root = AppsDataParser.LoadRoot(_jsonPath);
            if (root == null) return;

            var allAppRefs = GetAllApps(root);
            int totalFixCount = 0;

            foreach (var result in results.ToList())
            {
                foreach (var item in allAppRefs)
                {
                    if (item.App.Logo == result.Url || item.App.Name.Equals(result.AppName, StringComparison.OrdinalIgnoreCase))
                    {
                        item.App.Logo = "";
                        totalFixCount++;
                    }
                }
                _allUrlResults.Remove(result);
            }

            if (totalFixCount > 0)
            {
                await AppsDataParser.SaveRootAsync(_jsonPath, root);
                if (App.MainWindowInstance != null)
                {
                    await App.MainWindowInstance.LoadAllCategoriesAndAppsAsync();
                }
                ProgressStatusTxt.Text = $"Successfully cleared {totalFixCount} broken logo link(s) in JSON.";
                ApplyUrlFilter();
            }
        }
        catch (Exception ex)
        {
            ProgressStatusTxt.Text = "Error clearing logo links: " + ex.Message;
        }
    }

    internal async Task MarkAllArchivedAsync(List<UrlCheckResult> results)
    {
        if (string.IsNullOrEmpty(_jsonPath) || !File.Exists(_jsonPath) || results.Count == 0) return;

        try
        {
            var root = AppsDataParser.LoadRoot(_jsonPath);
            if (root == null) return;

            var allAppRefs = GetAllApps(root);
            int totalFixCount = 0;

            foreach (var result in results.ToList())
            {
                foreach (var item in allAppRefs)
                {
                    if (item.App.Url == result.Url || item.App.Name.Equals(result.AppName, StringComparison.OrdinalIgnoreCase))
                    {
                        item.App.IsDiscontinued = true;
                        totalFixCount++;
                    }
                }
                _allUrlResults.Remove(result);
            }

            if (totalFixCount > 0)
            {
                await AppsDataParser.SaveRootAsync(_jsonPath, root);
                if (App.MainWindowInstance != null)
                {
                    await App.MainWindowInstance.LoadAllCategoriesAndAppsAsync();
                }
                ProgressStatusTxt.Text = $"Successfully marked {totalFixCount} app(s) as discontinued/archived in JSON.";
                ApplyUrlFilter();
            }
        }
        catch (Exception ex)
        {
            ProgressStatusTxt.Text = "Error marking apps as archived: " + ex.Message;
        }
    }

    internal async Task UpdateAllUrlsAsync(List<UrlCheckResult> results)
    {
        if (string.IsNullOrEmpty(_jsonPath) || !File.Exists(_jsonPath) || results.Count == 0) return;

        try
        {
            var root = AppsDataParser.LoadRoot(_jsonPath);
            if (root == null) return;

            var allAppRefs = GetAllApps(root);
            int totalFixCount = 0;

            foreach (var result in results.ToList())
            {
                if (string.IsNullOrEmpty(result.NewUrl)) continue;

                foreach (var item in allAppRefs)
                {
                    if (result.IsLogo && item.App.Logo == result.Url)
                    {
                        item.App.Logo = result.NewUrl;
                        totalFixCount++;
                    }
                    else if (!result.IsLogo && item.App.Url == result.Url)
                    {
                        item.App.Url = result.NewUrl;
                        totalFixCount++;
                    }
                }
                _allUrlResults.Remove(result);
            }

            if (totalFixCount > 0)
            {
                await AppsDataParser.SaveRootAsync(_jsonPath, root);
                if (App.MainWindowInstance != null)
                {
                    await App.MainWindowInstance.LoadAllCategoriesAndAppsAsync();
                }
                ProgressStatusTxt.Text = $"Successfully updated {totalFixCount} link(s) in JSON.";
                ApplyUrlFilter();
            }
        }
        catch (Exception ex)
        {
            ProgressStatusTxt.Text = "Error updating URLs in JSON: " + ex.Message;
        }
    }

    internal async Task RemoveAllAppsAsync(List<UrlCheckResult> results)
    {
        if (string.IsNullOrEmpty(_jsonPath) || !File.Exists(_jsonPath) || results.Count == 0) return;

        try
        {
            var root = AppsDataParser.LoadRoot(_jsonPath);
            if (root == null) return;

            var allAppRefs = GetAllApps(root);
            int totalRemoved = 0;

            foreach (var result in results.ToList())
            {
                var match = allAppRefs.FirstOrDefault(x => x.App.Url == result.Url || x.App.Name.Equals(result.AppName, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    match.ContainerList.Remove(match.App);
                    totalRemoved++;
                }
                _allUrlResults.Remove(result);
            }

            if (totalRemoved > 0)
            {
                await AppsDataParser.SaveRootAsync(_jsonPath, root);
                if (App.MainWindowInstance != null)
                {
                    await App.MainWindowInstance.LoadAllCategoriesAndAppsAsync();
                }
                ProgressStatusTxt.Text = $"Successfully removed {totalRemoved} broken app(s) from JSON.";
                ApplyUrlFilter();
            }
        }
        catch (Exception ex)
        {
            ProgressStatusTxt.Text = "Error removing broken apps: " + ex.Message;
        }
    }

    private void DiagnosticsSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (TabActionsPanel == null || FormattingActions == null || UrlActions == null || ContentFrame == null) return;

        SelectorBarItem selectedItem = sender.SelectedItem;
        int currentSelectedIndex = sender.Items.IndexOf(selectedItem);
        System.Type pageType;

        switch (currentSelectedIndex)
        {
            case 0:
                pageType = typeof(DiagnosticsFormattingPage);
                break;
            default:
                pageType = typeof(DiagnosticsUrlPage);
                break;
        }

        var slideNavigationTransitionEffect = currentSelectedIndex - _previousSelectedIndex > 0
            ? Microsoft.UI.Xaml.Media.Animation.SlideNavigationTransitionEffect.FromRight
            : Microsoft.UI.Xaml.Media.Animation.SlideNavigationTransitionEffect.FromLeft;

        ContentFrame.Navigate(pageType, this, new Microsoft.UI.Xaml.Media.Animation.SlideNavigationTransitionInfo()
        {
            Effect = slideNavigationTransitionEffect
        });

        _previousSelectedIndex = currentSelectedIndex;

        FormattingActions.Visibility = currentSelectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
        UrlActions.Visibility = currentSelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
    }
}
