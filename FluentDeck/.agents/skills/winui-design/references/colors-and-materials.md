# Colors and Materials Reference

## Colors — Theme Resources

**Never hardcode colors.** Always use `{ThemeResource}` brushes so your app works in Light, Dark, and High Contrast modes.

### Text brushes

| Resource | Use for |
|----------|---------|
| `TextFillColorPrimaryBrush` | Primary text (headings, body) |
| `TextFillColorSecondaryBrush` | Secondary / supporting text |
| `TextFillColorTertiaryBrush` | Pressed state text |
| `TextFillColorDisabledBrush` | Disabled text only |
| `TextOnAccentFillColorPrimaryBrush` | Text on accent-colored backgrounds |
| `AccentTextFillColorPrimaryBrush` | Hyperlinks and accent text |

### Control fill brushes

| Resource | Use for |
|----------|---------|
| `ControlFillColorDefaultBrush` | Control rest state |
| `ControlFillColorSecondaryBrush` | Control hover state |
| `ControlFillColorTertiaryBrush` | Control pressed state |
| `ControlFillColorDisabledBrush` | Disabled controls |
| `ControlFillColorInputActiveBrush` | Focused text input fields |

### Background brushes

| Resource | Use for |
|----------|---------|
| `CardBackgroundFillColorDefaultBrush` | Card backgrounds |
| `CardBackgroundFillColorSecondaryBrush` | Alternate card rows |
| `LayerFillColorDefaultBrush` | Layered surface backgrounds |
| `SolidBackgroundFillColorBaseBrush` | Opaque page backgrounds |
| `SmokeFillColorDefaultBrush` | Overlay dimming (behind dialogs) |
| `AcrylicBackgroundFillColorBaseBrush` | Acrylic material surfaces |

### Accent fill (for primary action buttons)

| Resource | Use for |
|----------|---------|
| `AccentFillColorDefaultBrush` | Primary button rest |
| `AccentFillColorSecondaryBrush` | Primary button hover |
| `AccentFillColorTertiaryBrush` | Primary button pressed |
| `AccentFillColorDisabledBrush` | Disabled primary button |

### Stroke / border brushes

| Resource | Use for |
|----------|---------|
| `CardStrokeColorDefaultBrush` | Card borders |
| `ControlStrokeColorDefaultBrush` | Control borders |
| `DividerStrokeColorDefaultBrush` | Separators and dividers |

### Color code examples

```xml
<!-- GOOD -->
<Border Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
        BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}"
        BorderThickness="1"
        CornerRadius="{StaticResource OverlayCornerRadius}"
        Padding="16">
    <TextBlock Text="Card content"
               Foreground="{ThemeResource TextFillColorPrimaryBrush}" />
</Border>

<!-- BAD — hardcoded colors break Dark mode and High Contrast -->
<Border Background="#FFFFFF" BorderBrush="#E0E0E0">
    <TextBlock Text="Card content" Foreground="#000000" />
</Border>
```

---

## Materials — Mica & Acrylic

**Mica** — use for the app's main window background. It samples the desktop wallpaper for a subtle tinted translucency.

**Acrylic** — use for transient surfaces (flyouts, menus, sidebars) layered on top of the main window.

```xml
<!-- Window-level Mica (set in MainWindow.xaml) -->
<Window.SystemBackdrop>
    <MicaBackdrop />
</Window.SystemBackdrop>

<!-- Alternative: Mica Base Alt (slightly different tint) -->
<Window.SystemBackdrop>
    <MicaBackdrop Kind="BaseAlt" />
</Window.SystemBackdrop>

<!-- Acrylic for in-app surfaces -->
<Window.SystemBackdrop>
    <DesktopAcrylicBackdrop />
</Window.SystemBackdrop>
```

| Material | Surface lifetime | Example |
|----------|-----------------|---------|
| **Mica** | Long-lived (app window) | Main window background |
| **Mica Base Alt** | Long-lived (alternate tint) | Secondary window background |
| **Acrylic** | Transient (overlays) | Flyouts, sidebars, command bars |

Materials fall back to solid color on unsupported systems — no code needed.
