# Control Styles

Built-in WinUI 3 styles and patterns. Use `{StaticResource}` to reference, `BasedOn` to extend.

## Button Styles

| Style | Description |
|-------|-------------|
| `DefaultButtonStyle` | Standard button |
| `AccentButtonStyle` | Accent-colored primary action button |
| `NavigationBackButtonNormalStyle` | Back navigation (40x40) |
| `NavigationBackButtonSmallStyle` | Small back navigation (30x30) |

```xml
<Button Content="Save" Style="{StaticResource AccentButtonStyle}" />
<Button Content="Cancel" Style="{StaticResource DefaultButtonStyle}" />
```

## Subtle Button Pattern

Override `Button.Resources` with StaticResource redirects. This preserves the WinUI template and all visual states:

```xml
<Button Content="Action">
    <Button.Resources>
        <ResourceDictionary>
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key="Light">
                    <StaticResource x:Key="ButtonBackground" ResourceKey="SubtleFillColorTransparentBrush" />
                    <StaticResource x:Key="ButtonBackgroundPointerOver" ResourceKey="SubtleFillColorSecondaryBrush" />
                    <StaticResource x:Key="ButtonBackgroundPressed" ResourceKey="SubtleFillColorTertiaryBrush" />
                    <StaticResource x:Key="ButtonBorderBrush" ResourceKey="SubtleFillColorTransparentBrush" />
                    <StaticResource x:Key="ButtonBorderBrushPointerOver" ResourceKey="SubtleFillColorTransparentBrush" />
                    <StaticResource x:Key="ButtonBorderBrushPressed" ResourceKey="SubtleFillColorTransparentBrush" />
                </ResourceDictionary>
                <ResourceDictionary x:Key="Dark">
                    <StaticResource x:Key="ButtonBackground" ResourceKey="SubtleFillColorTransparentBrush" />
                    <StaticResource x:Key="ButtonBackgroundPointerOver" ResourceKey="SubtleFillColorSecondaryBrush" />
                    <StaticResource x:Key="ButtonBackgroundPressed" ResourceKey="SubtleFillColorTertiaryBrush" />
                    <StaticResource x:Key="ButtonBorderBrush" ResourceKey="SubtleFillColorTransparentBrush" />
                    <StaticResource x:Key="ButtonBorderBrushPointerOver" ResourceKey="SubtleFillColorTransparentBrush" />
                    <StaticResource x:Key="ButtonBorderBrushPressed" ResourceKey="SubtleFillColorTransparentBrush" />
                </ResourceDictionary>
                <ResourceDictionary x:Key="HighContrast" />
            </ResourceDictionary.ThemeDictionaries>
        </ResourceDictionary>
    </Button.Resources>
</Button>
```

Empty HC dictionary lets WinUI defaults apply. Light and Dark usually have identical redirects. Border brush states should match background states.

## Typography Styles

| Style | Size | Weight | Use Case |
|-------|------|--------|----------|
| `CaptionTextBlockStyle` | 12px | Regular | Small labels, timestamps |
| `BodyTextBlockStyle` | 14px | Regular | Default body text (applied by default) |
| `BaseTextBlockStyle` | 14px | Semibold | Base body style (less common) |
| `BodyStrongTextBlockStyle` | 14px | Semibold | Emphasized body text |
| `BodyLargeTextBlockStyle` | 18px | Regular | Prominent body text |
| `SubtitleTextBlockStyle` | 20px | Semibold | Section headers, card titles |
| `TitleTextBlockStyle` | 28px | Semibold | Page titles |
| `TitleLargeTextBlockStyle` | 40px | Semibold | Large feature titles |
| `DisplayTextBlockStyle` | 68px | Semibold | Hero text |

**Rules:**
- `BodyTextBlockStyle` is the default — do not explicitly apply it.
- `TextWrapping="NoWrap"` is the default — do not set it.
- Use `SemiBold`, never `Bold`.
- Minimum 12px for CJK legibility.
- `BasedOn` styles must not re-declare inherited properties (FontSize, FontFamily, FontWeight, LineHeight).

## Style Hygiene

- Reference styles with `{StaticResource}` (not `{ThemeResource}` — unnecessary overhead).
- Check for existing WinUI styles before creating custom ones.
- Single-use styles: inline the properties, delete the named style.
- Keep one-off values inline; promote to shared dictionary only when reused.
- Prefer setters/resource overrides for minor visual tweaks; avoid replacing `ControlTemplate` unless structurally required.
- Use `ThemeResource` inside style setters for theme-dependent values.
- Avoid no-op style churn.

## Naming Conventions

| Type | Convention | Example |
|------|------------|---------|
| Style | `{Purpose}Style` | `PrimaryButtonStyle` |
| Brush | `{Usage}{Property}Brush` | `HeaderBackgroundBrush` |
| DataTemplate | `{DataType}Template` | `UserItemTemplate` |
| Element `x:Name` | PascalCase + suffix | `SearchTextBox`, `SaveButton` |
