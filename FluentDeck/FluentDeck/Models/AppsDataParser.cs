using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace FluentDeck.Models;

public static class AppsDataParser
{
    public class JsonAppItem
    {
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
        public string Indicator { get; set; } = "";
        public string Logo { get; set; } = "";
        public bool IsFoss { get; set; }
        public bool IsPaid { get; set; }
        public bool IsPlanned { get; set; }
        public bool IsDiscontinued { get; set; }
        public bool IsTheme { get; set; }
    }

    public class JsonCategoryNode
    {
        public string Name { get; set; } = "";
        public string RawName { get; set; } = "";
        public List<JsonCategoryNode> Subcategories { get; set; } = new();
        public List<JsonAppItem> Apps { get; set; } = new();
    }

    public class AppsDataRoot
    {
        public string Version { get; set; } = "1.0";
        public int TotalCount { get; set; }
        public List<JsonAppItem> BestImplementation { get; set; } = new();
        public List<JsonAppItem> NewlyAdded { get; set; } = new();
        public List<JsonCategoryNode> Categories { get; set; } = new();
    }

    public static AppsDataRoot? LoadRoot(string jsonPath)
    {
        if (string.IsNullOrEmpty(jsonPath) || !File.Exists(jsonPath)) return null;
        try
        {
            string json = File.ReadAllText(jsonPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<AppsDataRoot>(json, options);
        }
        catch
        {
            return null;
        }
    }

    public static async Task SaveRootAsync(string jsonPath, AppsDataRoot root)
    {
        if (string.IsNullOrEmpty(jsonPath) || root == null) return;
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        string json = JsonSerializer.Serialize(root, options);
        await File.WriteAllTextAsync(jsonPath, json);
    }

    public static Dictionary<string, CategoryMetadataItem> LoadCategoryMetadata(string? jsonPath = null)
    {
        try
        {
            string path = "";
            if (!string.IsNullOrEmpty(jsonPath))
            {
                string dir = Path.GetDirectoryName(jsonPath) ?? "";
                path = Path.Combine(dir, "category-metadata.json");
            }
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                path = Path.Combine(AppContext.BaseDirectory, "Assets", "data", "category-metadata.json");
            }
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var root = JsonSerializer.Deserialize<CategoryMetadataRoot>(json, options);
                if (root?.Categories != null) return root.Categories;
            }
        }
        catch { }
        return new Dictionary<string, CategoryMetadataItem>(StringComparer.OrdinalIgnoreCase);
    }

    public static (List<CategoryNode> Nodes, List<ICatalogItem> DisplayItems, List<FlatAppItem> FlatApps, Dictionary<string, string> Logos) ParseData(AppsDataRoot root, string? jsonPath = null)
    {
        var categoryNodes = new List<CategoryNode>();
        var displayItems = new List<ICatalogItem>();
        var flatApps = new List<FlatAppItem>();
        var logos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (root == null) return (categoryNodes, displayItems, flatApps, logos);

        var metadata = LoadCategoryMetadata(jsonPath);

        // 1. Newly Added Apps section
        if (root.NewlyAdded != null && root.NewlyAdded.Count > 0)
        {
            string newlyAddedRaw = "🆕 Newly Added Apps!";
            string newlyAddedClean = newlyAddedRaw;
            string newlyAddedIcon = "";
            if (metadata.TryGetValue(newlyAddedRaw, out var metaItem))
            {
                newlyAddedClean = metaItem.Name;
                newlyAddedIcon = ResolveIconPath(metaItem.Icon);
            }

            displayItems.Add(new CatalogHeaderItem
            {
                Text = newlyAddedClean,
                RawText = newlyAddedRaw,
                IconPath = newlyAddedIcon,
                Level = 2,
                IsLeafCategory = true
            });

            foreach (var app in root.NewlyAdded)
            {
                var catalogApp = MapToCatalogApp(app, newlyAddedClean, 2, true);
                displayItems.Add(catalogApp);

                var flatApp = MapToFlatApp(app, newlyAddedClean);
                flatApps.Add(flatApp);
                if (!string.IsNullOrEmpty(app.Logo) && !string.IsNullOrEmpty(app.Url))
                    logos[app.Url] = app.Logo;
            }

            displayItems.Add(new CatalogDividerItem());
        }

        // 2. Main Categories Tree
        if (root.Categories != null)
        {
            var leafCategoryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CollectLeafCategoryNames(root.Categories, leafCategoryNames);

            foreach (var catNode in root.Categories)
            {
                var uiNode = BuildCategoryNodeTree(catNode, metadata);
                categoryNodes.Add(uiNode);

                ProcessCategoryForDisplay(catNode, 2, "", displayItems, flatApps, logos, leafCategoryNames, metadata);
            }
        }

        return (categoryNodes, displayItems, flatApps, logos);
    }

    private static string ResolveIconPath(string? iconName)
    {
        if (string.IsNullOrWhiteSpace(iconName)) return "";
        string localPath = Path.Combine(AppContext.BaseDirectory, "Assets", "category", iconName);
        if (File.Exists(localPath))
        {
            return $"ms-appx:///Assets/category/{iconName}";
        }
        return "";
    }

    private static CategoryNode BuildCategoryNodeTree(JsonCategoryNode jsonNode, Dictionary<string, CategoryMetadataItem> metadata)
    {
        string raw = !string.IsNullOrEmpty(jsonNode.RawName) ? jsonNode.RawName : jsonNode.Name;
        string clean = raw;
        string icon = "";
        if (metadata.TryGetValue(raw, out var m) || metadata.TryGetValue(jsonNode.Name, out m))
        {
            clean = m.Name;
            icon = ResolveIconPath(m.Icon);
        }

        var node = new CategoryNode
        {
            Name = clean,
            RawName = raw,
            Tag = clean,
            IconPath = icon
        };

        if (jsonNode.Subcategories != null && jsonNode.Subcategories.Count > 0)
        {
            foreach (var sub in jsonNode.Subcategories)
            {
                node.Children.Add(BuildCategoryNodeTree(sub, metadata));
            }
        }
        return node;
    }

    private static void CollectLeafCategoryNames(List<JsonCategoryNode> nodes, HashSet<string> leafNames)
    {
        foreach (var node in nodes)
        {
            if (node.Subcategories == null || node.Subcategories.Count == 0)
            {
                leafNames.Add(node.Name);
            }
            else
            {
                CollectLeafCategoryNames(node.Subcategories, leafNames);
            }
        }
    }

    private static void ProcessCategoryForDisplay(
        JsonCategoryNode node,
        int level,
        string parentPath,
        List<ICatalogItem> displayItems,
        List<FlatAppItem> flatApps,
        Dictionary<string, string> logos,
        HashSet<string> leafCategoryNames,
        Dictionary<string, CategoryMetadataItem> metadata)
    {
        string rawName = !string.IsNullOrEmpty(node.RawName) ? node.RawName : node.Name;
        string cleanName = rawName;
        string iconPath = "";
        if (metadata.TryGetValue(rawName, out var meta) || metadata.TryGetValue(node.Name, out meta))
        {
            cleanName = meta.Name;
            iconPath = ResolveIconPath(meta.Icon);
        }

        bool isLeaf = (node.Subcategories == null || node.Subcategories.Count == 0);

        displayItems.Add(new CatalogHeaderItem
        {
            Text = cleanName,
            RawText = rawName,
            IconPath = iconPath,
            Level = level,
            IsLeafCategory = isLeaf
        });

        string currentCategoryPath = string.IsNullOrEmpty(parentPath) ? cleanName : $"{parentPath} › {cleanName}";

        if (node.Apps != null && node.Apps.Count > 0)
        {
            foreach (var app in node.Apps)
            {
                var catalogApp = MapToCatalogApp(app, cleanName, level, false);
                displayItems.Add(catalogApp);

                var flatApp = MapToFlatApp(app, currentCategoryPath);
                flatApps.Add(flatApp);

                if (!string.IsNullOrEmpty(app.Logo) && !string.IsNullOrEmpty(app.Url))
                    logos[app.Url] = app.Logo;
            }
        }

        if (node.Subcategories != null)
        {
            foreach (var sub in node.Subcategories)
            {
                ProcessCategoryForDisplay(sub, level + 1, currentCategoryPath, displayItems, flatApps, logos, leafCategoryNames, metadata);
            }
        }
    }

    private static CatalogAppItem MapToCatalogApp(JsonAppItem app, string categoryName, int level, bool isNewlyAdded)
    {
        return new CatalogAppItem
        {
            Name = app.Name,
            Url = app.Url,
            Indicator = app.Indicator,
            LogoUrl = app.Logo,
            IsFoss = app.IsFoss,
            IsPaid = app.IsPaid,
            IsPlanned = app.IsPlanned,
            IsDiscontinued = app.IsDiscontinued,
            IsTheme = app.IsTheme,
            CategoryName = categoryName,
            HeadingLevel = level,
        };
    }

    private static FlatAppItem MapToFlatApp(JsonAppItem app, string categoryPath)
    {
        return new FlatAppItem
        {
            Name = app.Name,
            Url = app.Url,
            Indicator = app.Indicator,
            LogoUrl = app.Logo,
            IsFoss = app.IsFoss,
            IsPaid = app.IsPaid,
            IsPlanned = app.IsPlanned,
            IsDiscontinued = app.IsDiscontinued,
            IsTheme = app.IsTheme,
            GroupKey = categoryPath
        };
    }

    public static bool AddAppToCategory(AppsDataRoot root, string categoryName, JsonAppItem newApp)
    {
        if (root == null || root.Categories == null) return false;

        var targetNode = FindCategoryNode(root.Categories, categoryName);
        if (targetNode != null)
        {
            targetNode.Apps.Add(newApp);
            if (root.NewlyAdded == null) root.NewlyAdded = new List<JsonAppItem>();
            root.NewlyAdded.Insert(0, newApp);
            return true;
        }
        return false;
    }

    private static JsonCategoryNode? FindCategoryNode(List<JsonCategoryNode> nodes, string categoryName)
    {
        foreach (var node in nodes)
        {
            if (node.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase) ||
                node.RawName.Equals(categoryName, StringComparison.OrdinalIgnoreCase) ||
                CleanName(node.RawName).Equals(CleanName(categoryName), StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }

            if (node.Subcategories != null && node.Subcategories.Count > 0)
            {
                var childMatch = FindCategoryNode(node.Subcategories, categoryName);
                if (childMatch != null) return childMatch;
            }
        }
        return null;
    }

    private static string CleanName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        return System.Text.RegularExpressions.Regex.Replace(name, @"<[^>]+>", "").Trim();
    }

    public static bool UpdateAppInJson(AppsDataRoot root, string originalName, string originalUrl, JsonAppItem updatedApp)
    {
        if (root == null) return false;
        bool updated = false;

        void UpdateList(List<JsonAppItem>? list)
        {
            if (list == null) return;
            for (int i = 0; i < list.Count; i++)
            {
                if ((!string.IsNullOrEmpty(originalUrl) && list[i].Url.Equals(originalUrl, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(originalName) && list[i].Name.Equals(originalName, StringComparison.OrdinalIgnoreCase)))
                {
                    list[i] = updatedApp;
                    updated = true;
                }
            }
        }

        UpdateList(root.NewlyAdded);
        UpdateList(root.BestImplementation);

        void UpdateTree(List<JsonCategoryNode>? nodes)
        {
            if (nodes == null) return;
            foreach (var node in nodes)
            {
                UpdateList(node.Apps);
                UpdateTree(node.Subcategories);
            }
        }

        UpdateTree(root.Categories);
        return updated;
    }
}
