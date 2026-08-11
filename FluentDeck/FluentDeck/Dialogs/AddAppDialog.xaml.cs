using CommunityToolkit.WinUI.Controls;
using FluentDeck.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Windows.UI;

namespace FluentDeck.Dialogs;

public sealed partial class AddAppDialog : ContentDialog
{
    private string _suggestedLogoUrl = "";
    private string _category = "";
    private readonly AppItem? _appToEdit;
    private readonly IReadOnlyList<AppItem> _existingApps;

    public AddAppDialog(string category, IReadOnlyList<AppItem>? existingApps)
        : this(category, null, existingApps)
    {
    }

    public AddAppDialog(string category, AppItem? appToEdit, IReadOnlyList<AppItem>? existingApps = null)
    {
        InitializeComponent();
        _category = category;
        _appToEdit = appToEdit;
        _existingApps = existingApps ?? Array.Empty<AppItem>();
        CategoryItem.Content = category;

        if (_appToEdit != null)
        {
            Title = $"Edit App - {_appToEdit.Name}";
            PrimaryButtonText = "Save Changes";

            NameInput.Text = _appToEdit.Name;
            UrlInput.Text = _appToEdit.Url;
            LogoUrlInput.Text = _appToEdit.LogoUrl;

            // Indicator
            if (_appToEdit.Indicator == "WD") IndicatorSegment.SelectedIndex = 0;
            else if (_appToEdit.Indicator == "WDA") IndicatorSegment.SelectedIndex = 2;
            else IndicatorSegment.SelectedIndex = 1; // WDM

            // Pricing
            if (_appToEdit.IsFoss) PricingSegment.SelectedIndex = 1;
            else if (_appToEdit.IsPaid) PricingSegment.SelectedIndex = 2;
            else PricingSegment.SelectedIndex = 0;

            // State
            if (_appToEdit.IsPlanned) ProjectStateSegment.SelectedIndex = 1;
            else if (_appToEdit.IsDiscontinued) ProjectStateSegment.SelectedIndex = 2;
            else ProjectStateSegment.SelectedIndex = 0;

            // Type
            if (_appToEdit.IsTheme) TypeSegment.SelectedIndex = 1;
            else TypeSegment.SelectedIndex = 0;

            // Markdown Before/After Preview
            if (PreviewHeaderTextBlock != null) PreviewHeaderTextBlock.Text = "Markdown Preview (Before & After)";
            if (BeforePreviewContainer != null) BeforePreviewContainer.Visibility = Visibility.Visible;
            if (BeforePreviewTextBlock != null)
            {
                string beforeLine = BuildMarkdownLine(
                    _appToEdit.Name, _appToEdit.Url, _appToEdit.LogoUrl, _appToEdit.Indicator,
                    _appToEdit.IsFoss, _appToEdit.IsPaid, _appToEdit.IsPlanned, _appToEdit.IsDiscontinued, _appToEdit.IsTheme);
                MarkdownSyntaxHighlighter.HighlightLine(BeforePreviewTextBlock, beforeLine);
            }
        }

        UpdatePreview();
        ValidateInputs();
    }

    public string AppName => NameInput.Text.Trim();
    public string AppUrl => UrlInput.Text.Trim();
    public string LogoUrl => LogoUrlInput.Text.Trim();
    public string SelectedCategory => _category;

    public string DesignIndicator
    {
        get
        {
            if (IndicatorSegment.SelectedItem is SegmentedItem item)
                return item.Tag?.ToString() ?? "WDM";
            return "WDM";
        }
    }

    public bool IsFoss => PricingSegment.SelectedItem is SegmentedItem foss && foss.Tag?.ToString() == "FOSS";
    public bool IsPaid => PricingSegment.SelectedItem is SegmentedItem paid && paid.Tag?.ToString() == "Paid";
    public bool IsPlanned => ProjectStateSegment.SelectedItem is SegmentedItem planned && planned.Tag?.ToString() == "Planned";
    public bool IsDiscontinued => ProjectStateSegment.SelectedItem is SegmentedItem disc && disc.Tag?.ToString() == "Discontinued";
    public bool IsTheme => TypeSegment.SelectedItem is SegmentedItem theme && theme.Tag?.ToString() == "Theme";

    private void Input_TextChanged(object sender, object e)
    {
        CheckForDuplicates();
        UpdatePreview();
        ValidateInputs();
    }

    private void UrlInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        string url = UrlInput.Text.Trim();
        if (url.Contains("github.com"))
        {
            if (PricingSegment != null) PricingSegment.SelectedIndex = 1; // FOSS
        }
        else if (url.Contains("apps.microsoft.com"))
        {
            if (PricingSegment != null && PricingSegment.SelectedIndex == 1)
                PricingSegment.SelectedIndex = 0; // Reset to Free
        }
        Input_TextChanged(sender, e);
    }

    private void LogoUrlInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        string url = LogoUrlInput.Text.Trim();
        CheckLogoUrlSuggestion(url);
        UpdateLogoPreview(url);
        Input_TextChanged(sender, e);
    }

    // ── Duplicate detection ───────────────────────────────────────────────────

    private void CheckForDuplicates()
    {
        string name = AppName;
        string url = AppUrl;

        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(url))
        {
            DuplicateWarningBar.IsOpen = false;
            HideDuplicateCard();
            return;
        }

        AppItem? matchByName = null;
        AppItem? matchByUrl = null;

        foreach (var app in _existingApps)
        {
            if (_appToEdit != null)
            {
                if (ReferenceEquals(app, _appToEdit)) continue;
                if (!string.IsNullOrEmpty(_appToEdit.Url) && app.Url.Equals(_appToEdit.Url, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.IsNullOrEmpty(_appToEdit.Name) && app.Name.Equals(_appToEdit.Name, StringComparison.OrdinalIgnoreCase)) continue;
            }

            if (!string.IsNullOrWhiteSpace(name) &&
                app.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                matchByName = app;

            if (!string.IsNullOrWhiteSpace(url) &&
                app.Url.Equals(url, StringComparison.OrdinalIgnoreCase))
                matchByUrl = app;

            if (matchByName != null && matchByUrl != null) break;
        }

        if (matchByName != null && matchByUrl != null && ReferenceEquals(matchByName, matchByUrl))
        {
            DuplicateWarningBar.Title = "Exact duplicate found";
            DuplicateWarningBar.Message = "An app with this name and URL already exists in the list.";
            DuplicateWarningBar.IsOpen = true;
            ShowDuplicateCard(matchByName);
        }
        else if (matchByUrl != null)
        {
            DuplicateWarningBar.Title = "URL already exists";
            DuplicateWarningBar.Message = "This URL is already used by another app in the list.";
            DuplicateWarningBar.IsOpen = true;
            ShowDuplicateCard(matchByUrl);
        }
        else if (matchByName != null)
        {
            DuplicateWarningBar.Title = "Name already exists";
            DuplicateWarningBar.Message = "An app with this name is already in the list. Verify this isn't a duplicate.";
            DuplicateWarningBar.IsOpen = true;
            ShowDuplicateCard(matchByName);
        }
        else
        {
            DuplicateWarningBar.IsOpen = false;
            HideDuplicateCard();
        }
    }

    private void ShowDuplicateCard(AppItem app)
    {
        DuplicateAppNameText.Text = app.Name;

        DuplicateAppUrlButton.Content = app.Url;
        if (Uri.TryCreate(app.Url, UriKind.Absolute, out var uri))
            DuplicateAppUrlButton.NavigateUri = uri;
        else
            DuplicateAppUrlButton.NavigateUri = null;

        // Logo
        if (app.HasLogo)
            DuplicateAppLogoImage.Source = new BitmapImage(new Uri(app.DisplayLogoUrl));
        else
            DuplicateAppLogoImage.Source = null;

        // Badges
        DuplicateIndicatorText.Text = app.Indicator;
        DuplicateFossBadge.Visibility = app.IsFoss ? Visibility.Visible : Visibility.Collapsed;
        DuplicatePaidBadge.Visibility = app.IsPaid ? Visibility.Visible : Visibility.Collapsed;
        DuplicatePlannedBadge.Visibility = app.IsPlanned ? Visibility.Visible : Visibility.Collapsed;
        DuplicateDiscontinuedBadge.Visibility = app.IsDiscontinued ? Visibility.Visible : Visibility.Collapsed;

        DuplicateAppCard.Visibility = Visibility.Visible;
    }

    private void HideDuplicateCard()
    {
        DuplicateAppCard.Visibility = Visibility.Collapsed;
        DuplicateAppLogoImage.Source = null;
    }

    // ── Logo link suggestion & image preview ──────────────────────────────────

    private void CheckLogoUrlSuggestion(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            LogoSuggestionLink.Visibility = Visibility.Collapsed;
            _suggestedLogoUrl = "";
            return;
        }

        var githubMatch = Regex.Match(url, @"^https?://github\.com/([^/]+)/([^/]+)/blob/(.+)$", RegexOptions.IgnoreCase);
        if (githubMatch.Success)
        {
            string user = githubMatch.Groups[1].Value;
            string repo = githubMatch.Groups[2].Value;
            string path = githubMatch.Groups[3].Value;
            _suggestedLogoUrl = $"https://raw.githubusercontent.com/{user}/{repo}/{path}";

            LogoSuggestionText.Text = $"💡 Convert to raw link: {_suggestedLogoUrl}";
            LogoSuggestionLink.Visibility = Visibility.Visible;
            return;
        }

        var rawGithubMatch = Regex.Match(url, @"^https?://raw\.githubusercontent\.com/([^/]+)/([^/]+)/refs/heads/(.+)$", RegexOptions.IgnoreCase);
        if (rawGithubMatch.Success)
        {
            string user = rawGithubMatch.Groups[1].Value;
            string repo = rawGithubMatch.Groups[2].Value;
            string path = rawGithubMatch.Groups[3].Value;
            _suggestedLogoUrl = $"https://raw.githubusercontent.com/{user}/{repo}/main/{path}";

            LogoSuggestionText.Text = $"💡 Convert to direct link: {_suggestedLogoUrl}";
            LogoSuggestionLink.Visibility = Visibility.Visible;
            return;
        }

        LogoSuggestionLink.Visibility = Visibility.Collapsed;
        _suggestedLogoUrl = "";
    }

    private void UpdateLogoPreview(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            LogoPreviewBorder.Visibility = Visibility.Collapsed;
            LogoPreviewImage.Source = null;
            return;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            (uri.Scheme == "https" || uri.Scheme == "http"))
        {
            LogoPreviewImage.Source = new BitmapImage(uri);
            LogoPreviewBorder.Visibility = Visibility.Visible;
        }
        else
        {
            LogoPreviewBorder.Visibility = Visibility.Collapsed;
            LogoPreviewImage.Source = null;
        }
    }

    private void LogoSuggestionLink_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_suggestedLogoUrl))
        {
            LogoUrlInput.Text = _suggestedLogoUrl;
            LogoSuggestionLink.Visibility = Visibility.Collapsed;
        }
    }

    // ── Markdown generation & Syntax Highlighting ───────────────────────────────────────────────────

    public static string BuildMarkdownLine(string name, string url, string logoUrl, string indicator, bool isFoss, bool isPaid, bool isPlanned, bool isDiscontinued, bool isTheme)
    {
        string n = string.IsNullOrWhiteSpace(name) ? "AppName" : name;
        string u = string.IsNullOrWhiteSpace(url) ? "https://..." : url;

        string fossBadge = isFoss ? " <sup>`FOSS`</sup>" : "";
        string paidBadge = isPaid ? " `💰`" : "";
        string plannedBadge = isPlanned ? " `📆 Planned`" : "";
        string discontinuedBadge = isDiscontinued ? " `❎ Discontinued`" : "";
        string themeBadge = isTheme ? " `🎨`" : "";
        string logoComment = !string.IsNullOrWhiteSpace(logoUrl) ? $" <!-- logo: {logoUrl} -->" : "";

        return $"- `{indicator}` [{n}]({u}){paidBadge}{plannedBadge}{fossBadge}{discontinuedBadge}{themeBadge}{logoComment}";
    }

    public string GenerateMarkdownLine()
    {
        return BuildMarkdownLine(AppName, AppUrl, LogoUrl, DesignIndicator, IsFoss, IsPaid, IsPlanned, IsDiscontinued, IsTheme);
    }

    private void UpdatePreview()
    {
        if (PreviewTextBlock != null)
        {
            MarkdownSyntaxHighlighter.HighlightLine(PreviewTextBlock, GenerateMarkdownLine());
        }
    }

    private void ValidateInputs()
    {
        bool isValid = !string.IsNullOrWhiteSpace(AppName) &&
                       !string.IsNullOrWhiteSpace(AppUrl) &&
                       (AppUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                        AppUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
        IsPrimaryButtonEnabled = isValid;
    }
}

// ── Lightweight Monaco Markdown Syntax Highlighter ──────────────────────────────

public static class MarkdownSyntaxHighlighter
{
    private static SolidColorBrush ColorFromHex(string hex)
    {
        hex = hex.Replace("#", "");
        byte a = 255;
        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        return new SolidColorBrush(Color.FromArgb(a, r, g, b));
    }

    // Monaco VS Code Dark Theme Palette
    private static readonly SolidColorBrush PunctuationBrush = ColorFromHex("#808080"); // Gray Punctuation
    private static readonly SolidColorBrush CodeTextBrush = ColorFromHex("#CE9178");    // Peach Code Text
    private static readonly SolidColorBrush LinkTitleBrush = ColorFromHex("#CE9178");   // Peach Link Title
    private static readonly SolidColorBrush LinkUrlBrush = ColorFromHex("#9CDCFE");     // Light Blue URL
    private static readonly SolidColorBrush HtmlTagBrush = ColorFromHex("#569CD6");     // Cyan/Blue HTML Tags
    private static readonly SolidColorBrush CommentBrush = ColorFromHex("#6A9955");     // Soft Green Comments

    public static void HighlightLine(TextBlock textBlock, string line)
    {
        if (textBlock == null) return;
        textBlock.Inlines.Clear();
        if (string.IsNullOrEmpty(line)) return;

        int pos = 0;
        int len = line.Length;

        while (pos < len)
        {
            // 1. Comment <!-- ... -->
            if (pos + 4 <= len && line.Substring(pos).StartsWith("<!--"))
            {
                int endComment = line.IndexOf("-->", pos);
                if (endComment != -1)
                {
                    string commentText = line.Substring(pos, endComment + 3 - pos);
                    textBlock.Inlines.Add(new Run { Text = commentText, Foreground = CommentBrush });
                    pos = endComment + 3;
                    continue;
                }
            }

            // 2. HTML Tag <sup> or </sup>
            if (line[pos] == '<')
            {
                int endTag = line.IndexOf('>', pos);
                if (endTag != -1)
                {
                    string tagText = line.Substring(pos, endTag + 1 - pos);
                    textBlock.Inlines.Add(new Run { Text = tagText, Foreground = HtmlTagBrush });
                    pos = endTag + 1;
                    continue;
                }
            }

            // 3. Inline Code `code`
            if (line[pos] == '`')
            {
                int endBacktick = line.IndexOf('`', pos + 1);
                if (endBacktick != -1)
                {
                    textBlock.Inlines.Add(new Run { Text = "`", Foreground = PunctuationBrush });
                    string codeContent = line.Substring(pos + 1, endBacktick - pos - 1);
                    textBlock.Inlines.Add(new Run { Text = codeContent, Foreground = CodeTextBrush });
                    textBlock.Inlines.Add(new Run { Text = "`", Foreground = PunctuationBrush });
                    pos = endBacktick + 1;
                    continue;
                }
            }

            // 4. Link [Name](Url)
            if (line[pos] == '[')
            {
                int endBracket = line.IndexOf(']', pos + 1);
                if (endBracket != -1 && endBracket + 1 < len && line[endBracket + 1] == '(')
                {
                    int endParen = line.IndexOf(')', endBracket + 2);
                    if (endParen != -1)
                    {
                        textBlock.Inlines.Add(new Run { Text = "[", Foreground = PunctuationBrush });
                        string nameText = line.Substring(pos + 1, endBracket - pos - 1);
                        textBlock.Inlines.Add(new Run { Text = nameText, Foreground = LinkTitleBrush });
                        textBlock.Inlines.Add(new Run { Text = "](", Foreground = PunctuationBrush });

                        string urlText = line.Substring(endBracket + 2, endParen - endBracket - 2);
                        var urlRun = new Run { Text = urlText, Foreground = LinkUrlBrush };
                        var underline = new Underline();
                        underline.Inlines.Add(urlRun);
                        textBlock.Inlines.Add(underline);

                        textBlock.Inlines.Add(new Run { Text = ")", Foreground = PunctuationBrush });

                        pos = endParen + 1;
                        continue;
                    }
                }
            }

            // 5. Dash Bullet -
            if (line[pos] == '-' && (pos == 0 || line[pos - 1] == ' ' || pos == line.Length - 1 || line[pos + 1] == ' '))
            {
                textBlock.Inlines.Add(new Run { Text = "-", Foreground = PunctuationBrush });
                pos++;
                continue;
            }

            // Regular characters
            int nextSpecial = line.IndexOfAny(new[] { '`', '[', '<', '-' }, pos);
            int count = (nextSpecial == -1) ? len - pos : nextSpecial - pos;
            if (count <= 0) count = 1;

            string normalSegment = line.Substring(pos, count);
            textBlock.Inlines.Add(new Run { Text = normalSegment, Foreground = (Brush)Microsoft.UI.Xaml.Application.Current.Resources["TextFillColorSecondaryBrush"] });
            pos += count;
        }
    }
}
