using System;

namespace FluentDeck.Models;

// ──────────────────────────────────────────────────────────────────────────────
// Pure data models for virtualized catalog rendering.
// UI concerns (Visibility, Thickness, FontSize) live as static functions
// on AppsPage — referenced from XAML via x:Bind function syntax.
// ──────────────────────────────────────────────────────────────────────────────

public interface ICatalogItem { }

public class CatalogHeaderItem : ICatalogItem
{
    public string Text { get; set; } = "";
    public string RawText { get; set; } = "";
    public string IconPath { get; set; } = "";
    public Microsoft.UI.Xaml.Visibility IconVisibility => !string.IsNullOrEmpty(IconPath) ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
    public int Level { get; set; } = 3;
    public bool IsLeafCategory { get; set; } = false;
    public string AddButtonToolTip => $"Add new {Text}";

    public override bool Equals(object? obj) =>
        obj is CatalogHeaderItem other && Text == other.Text && Level == other.Level && IsLeafCategory == other.IsLeafCategory && IconPath == other.IconPath;

    public override int GetHashCode() => HashCode.Combine(Text, Level, IsLeafCategory, IconPath);
}

public class CatalogTextItem : ICatalogItem
{
    public string Text { get; set; } = "";
    public int HeadingLevel { get; set; } = 2;

    public override bool Equals(object? obj) =>
        obj is CatalogTextItem other && Text == other.Text && HeadingLevel == other.HeadingLevel;

    public override int GetHashCode() => HashCode.Combine(Text, HeadingLevel);
}

public class CatalogDividerItem : ICatalogItem
{
    // Use RuntimeHelpers.GetHashCode so each divider instance has a unique hash,
    // preventing hash collisions when multiple dividers appear in the same collection.
    public override bool Equals(object? obj) => ReferenceEquals(this, obj);
    public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
}

public class CatalogAppItem : AppItem, ICatalogItem
{
    public bool IsNewlyAddedSectionApp { get; set; }
    public bool IsNew { get; set; } = false;
    public int HeadingLevel { get; set; } = 2;
    public string CategoryName { get; set; } = "";

    public override bool Equals(object? obj) =>
        obj is CatalogAppItem other &&
        Name == other.Name && Url == other.Url && Indicator == other.Indicator &&
        IsFoss == other.IsFoss && IsPaid == other.IsPaid && IsPlanned == other.IsPlanned &&
        IsDiscontinued == other.IsDiscontinued && LogoUrl == other.LogoUrl &&
        IsNewlyAddedSectionApp == other.IsNewlyAddedSectionApp && HeadingLevel == other.HeadingLevel &&
        CategoryName == other.CategoryName;

    public override int GetHashCode() =>
        HashCode.Combine(Name, Url, Indicator, IsNewlyAddedSectionApp, HeadingLevel, CategoryName);
}

public partial class CatalogItemTemplateSelector : Microsoft.UI.Xaml.Controls.DataTemplateSelector
{
    public Microsoft.UI.Xaml.DataTemplate HeaderTemplate { get; set; } = null!;
    public Microsoft.UI.Xaml.DataTemplate TextTemplate { get; set; } = null!;
    public Microsoft.UI.Xaml.DataTemplate AppTemplate { get; set; } = null!;
    public Microsoft.UI.Xaml.DataTemplate DividerTemplate { get; set; } = null!;

    protected override Microsoft.UI.Xaml.DataTemplate SelectTemplateCore(object item)
    {
        if (item is CatalogHeaderItem) return HeaderTemplate;
        if (item is CatalogAppItem) return AppTemplate;
        if (item is CatalogDividerItem) return DividerTemplate;
        return TextTemplate;
    }
}
