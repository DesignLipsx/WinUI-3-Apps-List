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

public class IconItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name { get; set; } = "";

    // Size -> Filename maps
    public Dictionary<string, string> RegularSizes { get; set; } = new();
    public Dictionary<string, string> FilledSizes { get; set; } = new();
    public Dictionary<string, string> ColorSizes { get; set; } = new();

    // Default display path for grid item
    private string _imagePath = "";
    public string ImagePath
    {
        get => _imagePath;
        set
        {
            if (_imagePath != value)
            {
                _imagePath = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ImagePath)));
            }
        }
    }

    public List<string> Metaphors { get; set; } = new();

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

public class IconsPageViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsStoreBuild => FluentDeck.Helpers.FeatureManager.IsStoreBuild;

    private static List<IconItem> _allIcons = new();
    private ObservableCollection<IconItem> _filteredIcons = new();
    private string _searchQuery = "";
    private string _selectedStyle = "Regular"; // Regular, Filled, Color
    private string _selectedSize = "24"; // Default size
    private bool _isLoading = true;
    private string _searchPlaceholder = "Search icons...";
    private IconItem? _selectedIcon;
    private string? _previewImagePath;

    private ObservableCollection<string> _availableSizes = new() { "16", "20", "24", "28", "32", "48" };

    public IconItem? SelectedIcon
    {
        get => _selectedIcon;
        set
        {
            if (_isUpdatingState) return;
            _isUpdatingState = true;
            try
            {
                if (_selectedIcon != null)
                    _selectedIcon.IsSelected = false;

                SetProperty(ref _selectedIcon, value);

                if (_selectedIcon != null)
                    _selectedIcon.IsSelected = true;

                UpdateAvailableStyles();
                if (AvailableStyles.Contains(SelectedStyle))
                {
                    PreviewStyle = SelectedStyle;
                }
                else
                {
                    PreviewStyle = AvailableStyles.FirstOrDefault() ?? "Regular";
                }
                UpdateAvailableSizes();
                UpdatePreviewImagePath();
            }
            finally
            {
                _isUpdatingState = false;
            }
        }
    }

    public string? PreviewImagePath
    {
        get => _previewImagePath;
        private set => SetProperty(ref _previewImagePath, value);
    }

    public ObservableCollection<IconItem> FilteredIcons
    {
        get => _filteredIcons;
        set => SetProperty(ref _filteredIcons, value);
    }

    public ObservableCollection<string> AvailableSizes
    {
        get => _availableSizes;
        set => SetProperty(ref _availableSizes, value);
    }

    private ObservableCollection<string> _availableStyles = new() { "Regular", "Filled", "Color" };
    public ObservableCollection<string> AvailableStyles
    {
        get => _availableStyles;
        set => SetProperty(ref _availableStyles, value);
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
            {
                ExecuteFilter();
            }
        }
    }

    private string _previewStyle = "Regular";
    public string PreviewStyle
    {
        get => _previewStyle;
        set
        {
            if (SetProperty(ref _previewStyle, value))
            {
                UpdateAvailableSizes();
                UpdatePreviewImagePath();
            }
        }
    }

    public string SelectedSize
    {
        get => _selectedSize;
        set
        {
            if (SetProperty(ref _selectedSize, value))
                UpdatePreviewImagePath();
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

    private bool _isUpdatingState = false;

    public IconsPageViewModel()
    {
        _ = InitializeAsync();
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;

        if (_allIcons.Count == 0)
        {
            await Task.Run(() =>
            {
                try
                {
                    string jsonPath = Path.Combine(AppContext.BaseDirectory, "Assets", "data", "icon_metadata.json");
                    if (File.Exists(jsonPath))
                    {
                        using var stream = File.OpenRead(jsonPath);
                        using var doc = JsonDocument.Parse(stream);
                        var root = doc.RootElement;
                        var iconsArray = root.GetProperty("icons");

                        var list = new List<IconItem>();

                        // Build a lookup of all color SVG filenames available on disk (without .svg extension)
                        string colorDir = Path.Combine(AppContext.BaseDirectory, "Assets", "icons", "icon_color");
                        var colorFilesOnDisk = Directory.Exists(colorDir)
                            ? new HashSet<string>(
                                Directory.GetFiles(colorDir, "*.svg")
                                         .Select(f => Path.GetFileNameWithoutExtension(f)),
                                StringComparer.OrdinalIgnoreCase)
                            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        foreach (var item in iconsArray.EnumerateArray())
                        {
                            var icon = new IconItem
                            {
                                Name = item.GetProperty("name").GetString() ?? ""
                            };

                            if (item.TryGetProperty("regular", out var regProp) && regProp.ValueKind == JsonValueKind.Object)
                            {
                                foreach (var p in regProp.EnumerateObject())
                                    icon.RegularSizes[p.Name] = p.Value.GetString() ?? "";
                            }

                            if (item.TryGetProperty("filled", out var fillProp) && fillProp.ValueKind == JsonValueKind.Object)
                            {
                                foreach (var p in fillProp.EnumerateObject())
                                    icon.FilledSizes[p.Name] = p.Value.GetString() ?? "";
                            }

                            if (item.TryGetProperty("color", out var colorProp) && colorProp.ValueKind == JsonValueKind.Object)
                            {
                                foreach (var p in colorProp.EnumerateObject())
                                    icon.ColorSizes[p.Name] = p.Value.GetString() ?? "";
                            }

                            // JSON has "color": {} for all icons. Populate from disk scan instead.
                            if (icon.ColorSizes.Count == 0)
                            {
                                var sourceSizes = icon.RegularSizes.Count > 0 ? icon.RegularSizes : icon.FilledSizes;
                                foreach (var kvp in sourceSizes)
                                {
                                    // Convert e.g. ic_fluent_add_circle_24_regular → ic_fluent_add_circle_24_color
                                    string colorFilename = kvp.Value
                                        .Replace("_regular", "_color", StringComparison.OrdinalIgnoreCase)
                                        .Replace("_filled", "_color", StringComparison.OrdinalIgnoreCase);
                                    if (colorFilesOnDisk.Contains(colorFilename))
                                        icon.ColorSizes[kvp.Key] = colorFilename;
                                }
                            }

                            if (item.TryGetProperty("metaphor", out var metaProp) && metaProp.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var m in metaProp.EnumerateArray())
                                {
                                    if (m.ValueKind == JsonValueKind.String)
                                    {
                                        string val = m.GetString() ?? "";
                                        if (!string.IsNullOrWhiteSpace(val))
                                            icon.Metaphors.Add(val);
                                    }
                                }
                            }

                            list.Add(icon);
                        }
                        _allIcons = list;
                    }
                }
                catch { }
            });
        }

        var dispatcher = App.MainWindowInstance?.DispatcherQueue;
        if (dispatcher != null)
        {
            dispatcher.TryEnqueue(() =>
            {
                SearchPlaceholder = $"Search {_allIcons.Count} icons...";
                ExecuteFilter();
                IsLoading = false;
            });
        }
        else
        {
            SearchPlaceholder = $"Search {_allIcons.Count} icons...";
            ExecuteFilter();
            IsLoading = false;
        }
    }

    private void UpdateAvailableStyles()
    {
        var styles = new List<string>();
        if (_selectedIcon != null)
        {
            if (_selectedIcon.RegularSizes.Count > 0) styles.Add("Regular");
            if (_selectedIcon.FilledSizes.Count > 0) styles.Add("Filled");
            if (_selectedIcon.ColorSizes.Count > 0) styles.Add("Color");
        }
        if (styles.Count == 0)
            styles.Add("Regular");

        if (!AvailableStyles.SequenceEqual(styles))
        {
            for (int i = AvailableStyles.Count - 1; i >= 0; i--)
            {
                if (!styles.Contains(AvailableStyles[i]))
                    AvailableStyles.RemoveAt(i);
            }
            foreach (var s in styles)
            {
                if (!AvailableStyles.Contains(s))
                    AvailableStyles.Add(s);
            }
        }

        if (!AvailableStyles.Contains(PreviewStyle))
        {
            PreviewStyle = AvailableStyles.FirstOrDefault() ?? "Regular";
        }
    }

    private void UpdateAvailableSizes()
    {
        if (_selectedIcon == null) return;

        var map = PreviewStyle switch
        {
            "Regular" => _selectedIcon.RegularSizes,
            "Filled" => _selectedIcon.FilledSizes,
            "Color" => _selectedIcon.ColorSizes,
            _ => _selectedIcon.RegularSizes
        };

        var keys = map.Keys.OrderBy(k => int.TryParse(k, out int v) ? v : 99).ToList();
        if (keys.Count == 0)
            keys = new List<string> { "24" };

        if (!AvailableSizes.SequenceEqual(keys))
        {
            for (int i = AvailableSizes.Count - 1; i >= 0; i--)
            {
                if (!keys.Contains(AvailableSizes[i]))
                    AvailableSizes.RemoveAt(i);
            }
            foreach (var k in keys)
            {
                if (!AvailableSizes.Contains(k))
                    AvailableSizes.Add(k);
            }
        }

        if (!AvailableSizes.Contains(SelectedSize))
            SelectedSize = AvailableSizes.FirstOrDefault() ?? "24";
    }

    private void UpdatePreviewImagePath()
    {
        if (_selectedIcon == null)
        {
            PreviewImagePath = "";
            return;
        }

        var map = PreviewStyle switch
        {
            "Regular" => _selectedIcon.RegularSizes,
            "Filled" => _selectedIcon.FilledSizes,
            "Color" => _selectedIcon.ColorSizes,
            _ => _selectedIcon.RegularSizes
        };

        string? sizeKey = _selectedSize;
        string? filename = null;
        if (!string.IsNullOrEmpty(sizeKey) && map != null)
        {
            map.TryGetValue(sizeKey, out filename);
        }

        if (string.IsNullOrEmpty(filename) && map != null)
        {
            filename = map.Values.FirstOrDefault();
        }

        PreviewImagePath = ResolveIconAssetPath(PreviewStyle, filename);
    }

    public static string? ResolveIconAssetPath(string style, string? filename)
    {
        if (string.IsNullOrEmpty(filename)) return null;

        string subFolder = style switch
        {
            "Regular" => "icon_regular",
            "Filled" => "icon_filled",
            "Color" => "icon_color",
            _ => "icon_regular"
        };

        if (!filename.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            filename += ".svg";

        return $"ms-appx:///Assets/icons/{subFolder}/{filename}";
    }

    private CancellationTokenSource? _filterCts;

    public async void ExecuteFilter()
    {
        // Cancel any in-flight filter from rapid style/search changes
        _filterCts?.Cancel();
        _filterCts?.Dispose();
        var cts = new CancellationTokenSource();
        _filterCts = cts;

        var query = _searchQuery.Trim();
        var style = _selectedStyle;
        var previousName = _selectedIcon?.Name;

        // Compute entirely off the UI thread — pure read, no PropertyChanged calls
        List<(IconItem icon, string imagePath)>? results;
        try
        {
            results = await Task.Run(() => ComputeFilter(query, style), cts.Token);
        }
        catch (OperationCanceledException) { return; }

        if (cts.IsCancellationRequested) return;

        // Apply on UI thread: set ImagePath (fires PropertyChanged) and build collection
        foreach (var (icon, path) in results)
        {
            icon.ImagePath = path;
            icon.IsSelected = false;
        }

        FilteredIcons = new ObservableCollection<IconItem>(results.Select(r => r.icon));

        var matchingItem = FilteredIcons.FirstOrDefault(i => i.Name == previousName);
        SelectedIcon = matchingItem ?? FilteredIcons.FirstOrDefault();
    }

    /// <summary>Pure computation — safe to run on a background thread. No PropertyChanged calls.</summary>
    private List<(IconItem icon, string imagePath)> ComputeFilter(string query, string style)
    {
        var filtered = _allIcons.AsEnumerable();

        if (style == "Regular")
            filtered = filtered.Where(x => x.RegularSizes.Count > 0);
        else if (style == "Filled")
            filtered = filtered.Where(x => x.FilledSizes.Count > 0);
        else if (style == "Color")
            filtered = filtered.Where(x => x.ColorSizes.Count > 0);

        if (!string.IsNullOrEmpty(query))
        {
            filtered = filtered.Where(x => x.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                          x.Metaphors.Any(m => m.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        var result = new List<(IconItem, string)>();
        foreach (var x in filtered)
        {
            var map = style switch
            {
                "Regular" => x.RegularSizes,
                "Filled" => x.FilledSizes,
                "Color" => x.ColorSizes,
                _ => x.RegularSizes
            };

            string filename = map.TryGetValue("24", out var fn) ? fn
                            : map.Count > 0 ? map.Values.First() : "";

            string? imagePath = ResolveIconAssetPath(style, filename);
            if (!string.IsNullOrEmpty(imagePath))
                result.Add((x, imagePath));
        }
        return result;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value)) return false;
        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
