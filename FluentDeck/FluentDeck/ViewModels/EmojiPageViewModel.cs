using FluentDeck.Pages.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FluentDeck.ViewModels;

public class EmojiItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name { get; set; } = "";
    public string Glyph { get; set; } = "";
    public string Unicode { get; set; } = "";
    public string Group { get; set; } = "";
    public List<string> Keywords { get; set; } = new();

    public string Path3D { get; set; } = "";
    public string PathAnimated { get; set; } = "";
    public string PathColor { get; set; } = "";
    public string PathFlat { get; set; } = "";
    public string PathHighContrast { get; set; } = "";

    // Grid always shows 3D images
    public string ImagePath { get; set; } = "";

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }
    }
}

public class EmojiPageViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private static List<EmojiItem> _allEmojis = new();
    private ObservableCollection<EmojiItem> _filteredEmojis = new();
    private string _searchQuery = "";
    private string _selectedStyle = "3D";
    private string _selectedCategory = "All";
    private bool _isLoading = true;
    private string _searchPlaceholder = "Search emojis...";
    private EmojiItem? _selectedEmoji;
    private string? _previewImagePath;

    public EmojiItem? SelectedEmoji
    {
        get => _selectedEmoji;
        set
        {
            if (_selectedEmoji != value)
            {
                if (_selectedEmoji != null)
                    _selectedEmoji.IsSelected = false;

                _selectedEmoji = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedEmoji)));

                if (_selectedEmoji != null)
                    _selectedEmoji.IsSelected = true;
            }

            UpdatePreviewImagePath();
        }
    }

    public string? PreviewImagePath
    {
        get => _previewImagePath;
        private set => SetProperty(ref _previewImagePath, value);
    }

    public ObservableCollection<EmojiItem> FilteredEmojis
    {
        get => _filteredEmojis;
        set => SetProperty(ref _filteredEmojis, value);
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
                ExecuteFilter();
        }
    }

    public string SelectedStyle
    {
        get => _selectedStyle;
        set
        {
            if (SetProperty(ref _selectedStyle, value))
                // Only update the sidebar preview — NOT the entire grid
                UpdatePreviewImagePath();
        }
    }

    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
                ExecuteFilter();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string SearchPlaceholder
    {
        get => _searchPlaceholder;
        set => SetProperty(ref _searchPlaceholder, value);
    }

    public EmojiPageViewModel()
    {
        _ = InitializeAsync();
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;

        if (_allEmojis.Count == 0)
        {
            await Task.Run(() =>
            {
                try
                {
                    string jsonPath = Path.Combine(AppContext.BaseDirectory, "Assets", "data", "emoji_metadata.json");
                    if (File.Exists(jsonPath))
                    {
                        using var stream = File.OpenRead(jsonPath);
                        using var doc = JsonDocument.Parse(stream);
                        var root = doc.RootElement;
                        var emojisArray = root.GetProperty("emojis");

                        var list = new List<EmojiItem>();
                        foreach (var item in emojisArray.EnumerateArray())
                        {
                            var path3d = item[5].GetString() ?? "";
                            var emoji = new EmojiItem
                            {
                                Name = item[0].GetString() ?? "",
                                Glyph = item[1].GetString() ?? "",
                                Unicode = item[2].GetString() ?? "",
                                Group = item[3].GetString() ?? "",
                                Keywords = item[4].EnumerateArray().Select(x => x.GetString() ?? "").ToList(),
                                Path3D = path3d,
                                PathAnimated = item[6].GetString() ?? "",
                                PathColor = item[7].GetString() ?? "",
                                PathFlat = item[8].GetString() ?? "",
                                PathHighContrast = item[9].GetString() ?? "",
                                ImagePath = ResolveAssetPath(path3d) ?? ""
                            };
                            list.Add(emoji);
                        }
                        _allEmojis = list;
                    }
                }
                catch { }
            });
        }

        SearchPlaceholder = $"Search {_allEmojis.Count} emojis...";
        ExecuteFilter();
        IsLoading = false;
        CheckBackgroundDownload();
    }

    private bool _isPlayingAnimation;
    private bool _isDownloadingAnimation;
    private bool _isAnimatedDownloaded;
    private string? _animatedAssetPath;
    private static readonly System.Net.Http.HttpClient _httpClient = CreateHttpClient();

    private static System.Net.Http.HttpClient CreateHttpClient()
    {
        var client = new System.Net.Http.HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "FluentDeck-App");
        return client;
    }

    public static string LocalAppDataEmojiFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FluentDeck",
        "Emoji",
        "animated"
    );

    public bool IsPlayingAnimation
    {
        get => _isPlayingAnimation;
        set
        {
            if (SetProperty(ref _isPlayingAnimation, value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActivePreviewPath)));
            }
        }
    }

    public bool IsDownloadingAnimation
    {
        get => _isDownloadingAnimation;
        set => SetProperty(ref _isDownloadingAnimation, value);
    }

    public bool IsAnimatedDownloaded
    {
        get => _isAnimatedDownloaded;
        set => SetProperty(ref _isAnimatedDownloaded, value);
    }

    public string? AnimatedAssetPath
    {
        get => _animatedAssetPath;
        private set
        {
            if (SetProperty(ref _animatedAssetPath, value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActivePreviewPath)));
            }
        }
    }

    public string? ActivePreviewPath => IsPlayingAnimation && !string.IsNullOrEmpty(AnimatedAssetPath) ? AnimatedAssetPath : PreviewImagePath;

    public bool HasAnimation => !string.IsNullOrEmpty(SelectedEmoji?.PathAnimated) && SelectedStyle == "3D";

    public void TogglePlayAnimation()
    {
        if (!HasAnimation) return;

        if (!IsAnimatedDownloaded)
        {
            _ = DownloadAnimatedEmojiAsync();
            return;
        }

        IsPlayingAnimation = !IsPlayingAnimation;
    }

    private double _downloadProgress;

    public double DownloadProgress
    {
        get => _downloadProgress;
        set => SetProperty(ref _downloadProgress, value);
    }

    public async Task DownloadAnimatedEmojiAsync()
    {
        if (SelectedEmoji == null || string.IsNullOrEmpty(SelectedEmoji.PathAnimated) || IsDownloadingAnimation) return;

        string fileName = Path.GetFileName(SelectedEmoji.PathAnimated);
        if (string.IsNullOrEmpty(fileName)) return;

        IsDownloadingAnimation = true;
        DownloadProgress = 0;

        try
        {
            string localFolder = LocalAppDataEmojiFolder;
            if (!Directory.Exists(localFolder))
            {
                Directory.CreateDirectory(localFolder);
            }

            string targetFilePath = Path.Combine(localFolder, fileName);
            string remoteUrl = GetGitHubAnimatedEmojiUrl(SelectedEmoji.PathAnimated);

            using var response = await _httpClient.GetAsync(remoteUrl, System.Net.Http.HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            long totalBytes = response.Content.Headers.ContentLength ?? -1L;
            using var contentStream = await response.Content.ReadAsStreamAsync();

            using (var fileStream = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true))
            {
                byte[] buffer = new byte[8192];
                long totalRead = 0;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalRead += bytesRead;

                    if (totalBytes > 0)
                    {
                        DownloadProgress = (double)totalRead / totalBytes * 100.0;
                    }
                }
                await fileStream.FlushAsync();
            }

            IsAnimatedDownloaded = true;
            AnimatedAssetPath = targetFilePath;
            IsPlayingAnimation = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[EmojiPageViewModel] Failed to download animated emoji: {ex.Message}");
        }
        finally
        {
            IsDownloadingAnimation = false;
        }
    }

    public static string GetGitHubAnimatedEmojiUrl(string relativePath)
    {
        // User specified format:
        // Base URL: https://raw.githubusercontent.com/jishnu-kv/fluentdeck/refs/heads/main/public/emoji/png/
        // JSON: "animated/aerial_tramway_animated.png" or "Default/animated/..."
        string cleaned = relativePath.Replace('\\', '/');
        if (cleaned.StartsWith("Default/", StringComparison.OrdinalIgnoreCase))
            cleaned = cleaned.Substring(8);

        return "https://raw.githubusercontent.com/jishnu-kv/fluentdeck/refs/heads/main/public/emoji/png/" + cleaned;
    }

    public static string? GetLocalAnimatedFilePath(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return null;
        string fileName = Path.GetFileName(relativePath);
        if (string.IsNullOrEmpty(fileName)) return null;

        string localPath = Path.Combine(LocalAppDataEmojiFolder, fileName);
        return File.Exists(localPath) ? localPath : null;
    }

    private static bool _isBackgroundDownloading;

    public void CheckBackgroundDownload()
    {
        string downloadMode = SettingsManager.GetAnimatedEmojiDownloadMode();
        if (downloadMode == "BackgroundDownload" && !_isBackgroundDownloading && _allEmojis.Count > 0)
        {
            _ = StartBackgroundEmojiDownloadAsync();
        }
    }

    private async Task StartBackgroundEmojiDownloadAsync()
    {
        if (_isBackgroundDownloading) return;
        _isBackgroundDownloading = true;

        try
        {
            string localFolder = LocalAppDataEmojiFolder;
            if (!Directory.Exists(localFolder))
            {
                Directory.CreateDirectory(localFolder);
            }

            var animList = _allEmojis.Where(e => !string.IsNullOrEmpty(e.PathAnimated)).ToList();
            foreach (var emoji in animList)
            {
                if (SettingsManager.GetAnimatedEmojiDownloadMode() != "BackgroundDownload")
                    break;

                string fileName = Path.GetFileName(emoji.PathAnimated);
                if (string.IsNullOrEmpty(fileName)) continue;

                string targetFilePath = Path.Combine(localFolder, fileName);
                if (File.Exists(targetFilePath)) continue;

                try
                {
                    string remoteUrl = GetGitHubAnimatedEmojiUrl(emoji.PathAnimated);
                    byte[] data = await _httpClient.GetByteArrayAsync(remoteUrl);
                    await File.WriteAllBytesAsync(targetFilePath, data);

                    // If currently viewing this emoji on main UI, update it
                    if (SelectedEmoji?.PathAnimated == emoji.PathAnimated)
                    {
                        IsAnimatedDownloaded = true;
                        AnimatedAssetPath = targetFilePath;
                    }
                }
                catch { }
            }
        }
        finally
        {
            _isBackgroundDownloading = false;
        }
    }

    private void UpdatePreviewImagePath()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasAnimation)));

        if (_selectedEmoji == null)
        {
            IsPlayingAnimation = false;
            IsAnimatedDownloaded = false;
            IsDownloadingAnimation = false;
            PreviewImagePath = null;
            AnimatedAssetPath = null;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasAnimation)));
            return;
        }

        // Check if animated version exists locally in AppData
        string? localAnimPath = GetLocalAnimatedFilePath(_selectedEmoji.PathAnimated);
        if (localAnimPath != null)
        {
            IsAnimatedDownloaded = true;
            AnimatedAssetPath = localAnimPath;
        }
        else
        {
            IsAnimatedDownloaded = false;
            AnimatedAssetPath = null;
            IsPlayingAnimation = false;

            // Auto Download if setting enabled
            string downloadMode = SettingsManager.GetAnimatedEmojiDownloadMode();
            if (downloadMode == "AutoDownload" && HasAnimation)
            {
                _ = DownloadAnimatedEmojiAsync();
            }
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasAnimation)));

        bool newHasAnimation = HasAnimation && IsAnimatedDownloaded;
        if (!newHasAnimation)
            IsPlayingAnimation = false;

        string relativePath = _selectedStyle switch
        {
            "3D" => _selectedEmoji.Path3D,
            "Color" => !string.IsNullOrEmpty(_selectedEmoji.PathColor) ? _selectedEmoji.PathColor : _selectedEmoji.Path3D,
            "Flat" => !string.IsNullOrEmpty(_selectedEmoji.PathFlat) ? _selectedEmoji.PathFlat : _selectedEmoji.Path3D,
            "High Contrast" => !string.IsNullOrEmpty(_selectedEmoji.PathHighContrast) ? _selectedEmoji.PathHighContrast : _selectedEmoji.Path3D,
            _ => _selectedEmoji.Path3D
        };

        if (string.IsNullOrEmpty(relativePath))
            relativePath = _selectedEmoji.Path3D;

        PreviewImagePath = ResolveAssetPath(relativePath);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActivePreviewPath)));
    }

    public static string? ResolveAssetPath(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return null;

        string cleanedPath = relativePath;
        if (cleanedPath.StartsWith("Default/", StringComparison.OrdinalIgnoreCase))
            cleanedPath = cleanedPath.Substring(8);

        string baseFolder = "";
        string adjustedPath = cleanedPath;

        if (cleanedPath.StartsWith("3D/", StringComparison.OrdinalIgnoreCase))
        {
            baseFolder = "png";
            adjustedPath = "3D/" + cleanedPath.Substring(3);
        }
        else if (cleanedPath.StartsWith("animated/", StringComparison.OrdinalIgnoreCase))
        {
            // Check local AppData folder first
            string? localPath = GetLocalAnimatedFilePath(cleanedPath);
            if (localPath != null) return localPath;

            baseFolder = "png";
            adjustedPath = "animated/" + cleanedPath.Substring(9);
        }
        else if (cleanedPath.StartsWith("Color/", StringComparison.OrdinalIgnoreCase))
        {
            baseFolder = "svg";
            adjustedPath = "color/" + cleanedPath.Substring(6);
        }
        else if (cleanedPath.StartsWith("Flat/", StringComparison.OrdinalIgnoreCase))
        {
            baseFolder = "svg";
            adjustedPath = "flat/" + cleanedPath.Substring(5);
        }
        else if (cleanedPath.StartsWith("High Contrast/", StringComparison.OrdinalIgnoreCase))
        {
            baseFolder = "svg";
            adjustedPath = "high_contrast/" + cleanedPath.Substring(14);
        }

        return string.IsNullOrEmpty(baseFolder) ? null : $"ms-appx:///Assets/emoji/{baseFolder}/{adjustedPath}";
    }

    private CancellationTokenSource? _filterCts;

    public async void ExecuteFilter()
    {
        _filterCts?.Cancel();
        _filterCts?.Dispose();
        var cts = new CancellationTokenSource();
        _filterCts = cts;

        var query = _searchQuery.Trim();
        var category = _selectedCategory;
        var style = _selectedStyle;
        var previousGlyph = _selectedEmoji?.Glyph;

        List<(EmojiItem emoji, string imagePath)>? results;
        try
        {
            results = await Task.Run(() => ComputeFilter(query, category, style), cts.Token);
        }
        catch (OperationCanceledException) { return; }

        if (cts.IsCancellationRequested) return;

        foreach (var (emoji, path) in results)
        {
            emoji.ImagePath = path;
            emoji.IsSelected = false;
        }

        FilteredEmojis = new ObservableCollection<EmojiItem>(results.Select(r => r.emoji));

        var matchingItem = FilteredEmojis.FirstOrDefault(e => e.Glyph == previousGlyph);
        SelectedEmoji = matchingItem ?? FilteredEmojis.FirstOrDefault();
    }

    private List<(EmojiItem emoji, string imagePath)> ComputeFilter(string query, string category, string style)
    {
        var filtered = _allEmojis.AsEnumerable();

        if (category != "All")
            filtered = filtered.Where(x => string.Equals(x.Group, category, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(query))
        {
            filtered = filtered.Where(x =>
                x.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                x.Glyph.Contains(query) ||
                x.Keywords.Any(k => k.Contains(query, StringComparison.OrdinalIgnoreCase))
            );
        }

        var result = new List<(EmojiItem, string)>();
        foreach (var x in filtered)
        {
            string path = style switch
            {
                "3D" => ResolveAssetPath(x.Path3D),
                "Flat" => ResolveAssetPath(x.PathFlat),
                "High Contrast" => ResolveAssetPath(x.PathHighContrast),
                _ => ResolveAssetPath(x.Path3D)
            } ?? "";
            if (!string.IsNullOrEmpty(path))
                result.Add((x, path));
        }
        return result;
    }

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value)) return false;
        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
