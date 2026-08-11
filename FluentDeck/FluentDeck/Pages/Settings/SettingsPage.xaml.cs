using FluentDeck.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FluentDeck.Pages.Settings;

internal class SettingsData
{
    public string GitHubPat { get; set; } = "";
    public string AppTheme { get; set; } = "Default";
    public bool DeveloperMode { get; set; } = false;
    public string SearchShortcut { get; set; } = "Ctrl+F";
    public string AnimatedEmojiDownloadMode { get; set; } = "OnDemand";
}

[JsonSerializable(typeof(SettingsData))]
internal partial class SettingsSerializationContext : JsonSerializerContext
{
}

public static class SettingsManager
{
    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FluentDeck",
        "settings.json"
    );

    private static SettingsData LoadData()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                return JsonSerializer.Deserialize(json, SettingsSerializationContext.Default.SettingsData) ?? new SettingsData();
            }
            // Migration check from legacy directory
            string legacyPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WinUI-Cataloger",
                "settings.json"
            );
            if (File.Exists(legacyPath))
            {
                string json = File.ReadAllText(legacyPath);
                return JsonSerializer.Deserialize(json, SettingsSerializationContext.Default.SettingsData) ?? new SettingsData();
            }
        }
        catch { }
        return new SettingsData();
    }

    private static void SaveData(SettingsData data)
    {
        try
        {
            string dir = Path.GetDirectoryName(SettingsFilePath)!;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            string json = JsonSerializer.Serialize(data, SettingsSerializationContext.Default.SettingsData);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch { }
    }

    public static string GetGitHubPat() => LoadData().GitHubPat;
    public static void SaveGitHubPat(string pat)
    {
        var data = LoadData();
        data.GitHubPat = pat;
        SaveData(data);
    }

    public static string GetAppTheme() => LoadData().AppTheme;
    public static void SaveAppTheme(string theme)
    {
        var data = LoadData();
        data.AppTheme = theme;
        SaveData(data);
    }


    public static string GetSearchShortcut() => LoadData().SearchShortcut;
    public static void SaveSearchShortcut(string shortcut)
    {
        var data = LoadData();
        data.SearchShortcut = shortcut;
        SaveData(data);
    }

    public static string GetAnimatedEmojiDownloadMode() => LoadData().AnimatedEmojiDownloadMode ?? "OnDemand";
    public static void SaveAnimatedEmojiDownloadMode(string mode)
    {
        var data = LoadData();
        data.AnimatedEmojiDownloadMode = mode;
        SaveData(data);
    }
}

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        Loaded += SettingsPage_Loaded;
    }

    private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        LoadSettings();
    }

    private void LoadSettings()
    {
        // GitHub PAT (Dev build only)
        if (GitHubPatCard != null)
        {
            GitHubPatCard.Visibility = FeatureManager.IsDeveloperMode ? Visibility.Visible : Visibility.Collapsed;
        }

        // Load PAT
        GitHubPatBox.Password = SettingsManager.GetGitHubPat();

        // Load Theme
        string theme = SettingsManager.GetAppTheme();
        int themeIndex = 0; // Default
        if (theme == "Light") themeIndex = 1;
        else if (theme == "Dark") themeIndex = 2;
        ThemeModeCombo.SelectedIndex = themeIndex;

        // Load Search Shortcut
        string shortcut = SettingsManager.GetSearchShortcut();
        SearchShortcutCombo.SelectedIndex = (shortcut == "F3") ? 1 : 0;

        // Load Animated Emoji Download Mode
        EmojiDownloadModeCombo.SelectionChanged -= EmojiDownloadModeCombo_SelectionChanged;
        string mode = SettingsManager.GetAnimatedEmojiDownloadMode();
        EmojiDownloadModeCombo.SelectedIndex = mode switch
        {
            "AutoDownload" => 1,
            "BackgroundDownload" => 2,
            _ => 0
        };
        EmojiDownloadModeCombo.SelectionChanged += EmojiDownloadModeCombo_SelectionChanged;
    }

    private void EmojiDownloadModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (EmojiDownloadModeCombo == null) return;
        string mode = EmojiDownloadModeCombo.SelectedIndex switch
        {
            1 => "AutoDownload",
            2 => "BackgroundDownload",
            _ => "OnDemand"
        };
        SettingsManager.SaveAnimatedEmojiDownloadMode(mode);
    }


    private void ThemeModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeModeCombo == null) return;

        string theme = "Default";
        ElementTheme elementTheme = ElementTheme.Default;

        int selected = ThemeModeCombo.SelectedIndex;
        if (selected == 1)
        {
            theme = "Light";
            elementTheme = ElementTheme.Light;
        }
        else if (selected == 2)
        {
            theme = "Dark";
            elementTheme = ElementTheme.Dark;
        }

        SettingsManager.SaveAppTheme(theme);
        ApplyTheme(elementTheme);
    }

    private void SearchShortcutCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SearchShortcutCombo == null) return;
        string shortcut = (SearchShortcutCombo.SelectedIndex == 1) ? "F3" : "Ctrl+F";
        SettingsManager.SaveSearchShortcut(shortcut);
    }

    public static void ApplyTheme(ElementTheme theme)
    {
        if (App.MainWindowInstance != null && App.MainWindowInstance.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme;
        }
    }

    public static ElementTheme GetSavedTheme()
    {
        string theme = SettingsManager.GetAppTheme();
        if (theme == "Light") return ElementTheme.Light;
        if (theme == "Dark") return ElementTheme.Dark;
        return ElementTheme.Default;
    }

    public static string GetSavedGitHubPat()
    {
        return SettingsManager.GetGitHubPat();
    }

    private void SavePatBtn_Click(object sender, RoutedEventArgs e)
    {
        SettingsManager.SaveGitHubPat(GitHubPatBox.Password);
        PatStatusTxt.Text = "Token saved successfully.";
        PatStatusTxt.Visibility = Visibility.Visible;
    }

    private void GitHubPatBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (PatStatusTxt != null) PatStatusTxt.Visibility = Visibility.Collapsed;
    }
}
