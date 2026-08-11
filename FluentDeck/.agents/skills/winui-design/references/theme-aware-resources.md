# Theme-Aware Resources

## ThemeResource vs StaticResource

| Markup Extension | Evaluated | Updates on Theme Change | Use Case |
|------------------|-----------|-------------------------|----------|
| `{ThemeResource}` | Runtime | Yes | Theme-dependent values at usage sites |
| `{StaticResource}` | Load time | No | Static values, inside theme dictionaries |

### Runtime Theme Switching

Verify runtime theme switching. `{ThemeResource}` updates when the system theme changes; `{StaticResource}` does not.

## StaticResource Redirects (Preferred Pattern)

Use `<StaticResource>` with `ResourceKey` to redirect to an existing WinUI brush. This reuses the existing brush object (zero allocation) instead of creating a new `SolidColorBrush` inline.

```xml
<!-- Correct: redirect -->
<StaticResource x:Key="ButtonBackground" ResourceKey="ControlFillColorDefaultBrush" />

<!-- Wrong: new brush object -->
<SolidColorBrush x:Key="ButtonBackground" Color="{StaticResource ControlFillColorDefault}" />
```

## Theme Dictionary Structure

Always define all three variants. Never use `x:Key="Default"`.

```xml
<ResourceDictionary.ThemeDictionaries>
    <ResourceDictionary x:Key="Light">
        <StaticResource x:Key="MyBrush" ResourceKey="ControlFillColorDefaultBrush" />
    </ResourceDictionary>
    <ResourceDictionary x:Key="Dark">
        <StaticResource x:Key="MyBrush" ResourceKey="ControlFillColorDefaultBrush" />
    </ResourceDictionary>
    <ResourceDictionary x:Key="HighContrast">
        <StaticResource x:Key="MyBrush" ResourceKey="SystemColorWindowTextColorBrush" />
    </ResourceDictionary>
</ResourceDictionary.ThemeDictionaries>
```

**Rules inside theme dictionaries:**
- `{StaticResource}` in Light/Dark (not `{ThemeResource}` — circular lookup risk)
- `{ThemeResource}` only for `SystemColor*` in HighContrast
- `ResourceKey` must end in `Brush`
- Keep `x:Key` order identical across Light/Dark/HighContrast
- Light and Dark should typically reference the same semantic WinUI keys

## Accent Colors

Use the system accent color resources:

```xml
<!-- User accent color -->
<Border Background="{ThemeResource SystemAccentColor}" />

<!-- Lighter/darker variants -->
<Border Background="{ThemeResource SystemAccentColorLight1}" />
<Border Background="{ThemeResource SystemAccentColorDark1}" />
```

## High Contrast System Colors

### The Eight Valid Brushes

| Resource | Purpose |
|----------|---------|
| `SystemColorWindowTextColorBrush` | Text on window background |
| `SystemColorWindowColorBrush` | Window/content background |
| `SystemColorHighlightTextColorBrush` | Selected text foreground |
| `SystemColorHighlightColorBrush` | Selection/hover background |
| `SystemColorButtonTextColorBrush` | Button text/foreground |
| `SystemColorButtonFaceColorBrush` | Button background |
| `SystemColorGrayTextColorBrush` | Disabled/inactive text |
| `SystemColorHotlightColorBrush` | Hyperlinks |

For color animations, use the matching Color resource (without "Brush" suffix).

### HC Color Pairings

| Background | Foreground | Use Case |
|------------|------------|----------|
| `SystemColorWindowColorBrush` | `SystemColorWindowTextColorBrush` | General content |
| `SystemColorHighlightColorBrush` | `SystemColorHighlightTextColorBrush` | Selected/hover states |
| `SystemColorButtonFaceColorBrush` | `SystemColorButtonTextColorBrush` | Buttons |
| `SystemColorWindowColorBrush` | `SystemColorHotlightColorBrush` | Hyperlinks |
| `SystemColorWindowColorBrush` | `SystemColorGrayTextColorBrush` | Disabled content |

**Never mix incompatible pairs.**

### HC Prohibitions

- No hardcoded colors
- No opacity on elements or brushes
- No accent colors (`SystemAccentColor`)
- No regular WinUI brushes (`TextFillColorPrimaryBrush`, etc.)
- No gradient animations — use one solid SystemColor
- No `SystemColor*` resources in Light/Dark dictionaries

### HC Border Thickness

Use 2px border in HC (vs 1px in Light/Dark) for flyouts, dialogs, cards:

```xml
<ResourceDictionary x:Key="Light">
    <Thickness x:Key="CardBorderThickness">1</Thickness>
</ResourceDictionary>
<ResourceDictionary x:Key="Dark">
    <Thickness x:Key="CardBorderThickness">1</Thickness>
</ResourceDictionary>
<ResourceDictionary x:Key="HighContrast">
    <Thickness x:Key="CardBorderThickness">2</Thickness>
</ResourceDictionary>
```

### HighContrastAdjustment

Set at app level to prevent system from doubling HC overrides:

```csharp
Application.Current.HighContrastAdjustment = ApplicationHighContrastAdjustment.None;
```

## ARGB Encoding for Opacity

Encode opacity in alpha channel rather than using `Opacity` property:

```xml
<!-- 25% opacity via alpha channel -->
<SolidColorBrush x:Key="BackplateBrush" Color="#40000000" />
```

## Acrylic Surface Pairings

| Surface Type | Background | Border |
|--------------|------------|--------|
| Menu flyouts, tooltips | `AcrylicBackgroundFillColorDefaultBrush` | `SurfaceStrokeColorFlyoutBrush` |
| UI surfaces (Start, Action Center) | `AcrylicBackgroundFillColorBaseBrush` | `SurfaceStrokeColorDefaultBrush` |

```xml
<Border Background="{ThemeResource AcrylicBackgroundFillColorDefaultBrush}"
        BorderBrush="{ThemeResource SurfaceStrokeColorFlyoutBrush}"
        BorderThickness="1"
        CornerRadius="{StaticResource OverlayCornerRadius}"
        BackgroundSizing="InnerBorderEdge"
        Translation="0,0,32">
    <Border.Shadow>
        <ThemeShadow />
    </Border.Shadow>
</Border>
```

Overlays on acrylic use `LayerOnAcrylicFillColorDefaultBrush`. Dividers use `DividerStrokeColorDefaultBrush`.

## Dialog Overlays (Smoke)

For dim overlays behind dialogs or modals, use:

```xml
<Border Background="{ThemeResource ContentDialogSmokeFill}" />
```

## Windows 10 to 11 Migration

| Windows 10 Resource | Windows 11 (WinUI) Equivalent |
|---------------------|-------------------------------|
| `SystemControlForegroundBaseMediumBrush` | `TextFillColorSecondaryBrush` |
| `SystemControlHighlightAltAccentBrush` | HC: `SystemColorHighlightTextColorBrush` |
| `SystemControlHyperlinkTextBrush` | HC: `SystemColorHotlightColorBrush` |
| `SystemAltHighColor` | Use appropriate WinUI theme resource |