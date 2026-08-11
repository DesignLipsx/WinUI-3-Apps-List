# Iconography and Motion Reference

## Iconography

Use **Segoe Fluent Icons** (Windows 11) via the `SymbolThemeFontFamily` resource, which falls back to **Segoe MDL2 Assets** on Windows 10 automatically.

### Icon types in order of preference

| Type | When to use | Example |
|------|-------------|---------|
| `SymbolIcon` | Standard named icons (simplest) | `<SymbolIcon Symbol="Save" />` |
| `FontIcon` | Specific glyph codes from Segoe Fluent Icons | `<FontIcon FontFamily="{StaticResource SymbolThemeFontFamily}" Glyph="&#xE946;" />` |
| `AnimatedIcon` | Interactive states (checkbox, nav, toggle) | Built-in with some controls |
| `ImageIcon` | Custom brand icons or images | `<ImageIcon Source="ms-appx:///Assets/logo.png" />` |
| `PathIcon` | Custom vector shapes | `<PathIcon Data="M 0,0 L 10,10" />` |
| `BitmapIcon` | Legacy bitmap icons | Avoid — prefer `ImageIcon` |

**Standard icon sizes:** 16px (inline/compact), 20px (default control size), 24px (emphasis), 32px (large), 48px (hero/feature).

### Icon code examples

```xml
<!-- MenuFlyout with icons -->
<MenuFlyoutItem Text="Copy" Icon="{ui:SymbolIcon Symbol=Copy}">
    <MenuFlyoutItem.KeyboardAccelerators>
        <KeyboardAccelerator Key="C" Modifiers="Control" />
    </MenuFlyoutItem.KeyboardAccelerators>
</MenuFlyoutItem>

<!-- NavigationViewItem with icon -->
<NavigationViewItem Content="Settings" Icon="{ui:SymbolIcon Symbol=Setting}" />

<!-- FontIcon for glyphs not in SymbolIcon enum -->
<FontIcon FontFamily="{StaticResource SymbolThemeFontFamily}"
          Glyph="&#xE8C8;"
          FontSize="16" />
```

Browse available icons in the **WinUI Gallery** app → Design guidance → Iconography, or search [Segoe Fluent Icons](https://learn.microsoft.com/windows/apps/design/style/segoe-fluent-icons-font).

---

## Corner Radius

Use the **built-in theme resources** — never hardcode `CornerRadius` values:

| Resource | Value | Use for |
|----------|-------|---------|
| `ControlCornerRadius` | 4px | In-page controls (buttons, inputs, list items) |
| `OverlayCornerRadius` | 8px | Top-level containers (cards, dialogs, flyouts, app window) |
| 0px | — | Edges that intersect with other straight edges (no resource needed) |

```xml
<!-- GOOD — use theme resources -->
<Button CornerRadius="{StaticResource ControlCornerRadius}" Content="Save" />
<Border CornerRadius="{StaticResource OverlayCornerRadius}" Padding="16">
    <!-- Card content -->
</Border>

<!-- BAD — hardcoded values -->
<Button CornerRadius="4" Content="Save" />
<Border CornerRadius="12" />
```

---

## Motion & Transitions

**Prefer built-in theme transitions** — they animate automatically and respect user "reduce motion" settings.

```xml
<!-- Implicit transitions — animate property changes automatically -->
<Button Opacity="1">
    <Button.OpacityTransition>
        <ScalarTransition />
    </Button.OpacityTransition>
</Button>

<!-- Page transitions via Frame -->
<Frame x:Name="ContentFrame">
    <Frame.ContentTransitions>
        <TransitionCollection>
            <NavigationThemeTransition />
        </TransitionCollection>
    </Frame.ContentTransitions>
</Frame>
```

### Connected animations

Animate elements between pages (e.g., list item → detail page):

```csharp
// Source page — prepare animation
var service = ConnectedAnimationService.GetForCurrentView();
service.PrepareToAnimate("itemAnimation", sourceElement);
Frame.Navigate(typeof(DetailPage), item);

// Destination page — play animation
var animation = ConnectedAnimationService.GetForCurrentView()
    .GetAnimation("itemAnimation");
animation?.TryStart(destinationElement);
```
