---
name: winui-design
description: "WinUI 3 UI design and XAML correctness — layout planning, control selection, Fluent Design, theming (Light/Dark/HighContrast), typography styles, spacing, brushes, accessibility, data binding review. Use when designing new pages, converting from WPF/Electron/web, reviewing XAML, fixing theme issues, or applying Fluent Design."
---

### UI Planning

> **Before picking controls, search the catalogue.** This skill ships `winui-search.exe` alongside this `SKILL.md`. It indexes 100+ WinUI Gallery controls, every Windows Community Toolkit scenario, and a curated set of platform integration patterns (JumpList, Share, file pickers, drag-drop). Use it to ground every control choice in a real shipping sample **before writing any XAML** — this is the difference between guessing property names and copying canonical code.
>
> ```powershell
> .\winui-search.exe search "<feature 1>" "<feature 2>" ...   # batch one focused query per feature
> .\winui-search.exe get <id 1> <id 2> ...                     # batch up to 3 IDs — full XAML + C# + pitfall notes
> .\winui-search.exe list                                       # browse all patterns (heavy — prefer search)
> .\winui-search.exe update                                     # force refresh now
> ```
>
> **Workflow:** in **one** `search` call, list every feature you need for the current page (one focused query per feature, not a bag of keywords) → from each shortlist pick the best ID → grab full code with `get` (batch up to 3 IDs per call) → then write XAML using those samples as reference. **Do NOT interleave searching with coding** — front-load all lookups, then code. BM25 rewards focused per-query phrasing, so keep each query about one control or pattern.

#### Step 1: Identify App Type and Anchor Control
| App Type | Anchor Control | Reference App |
|----------|---------------|---------------|
| Settings / config tool | `NavigationView` Left + `SettingsCard` | Windows Settings |
| Document / session editor | `TabView` + full-width content | Windows Terminal, Notepad |
| Hierarchical browser | `TreeView` + `ListView` + `BreadcrumbBar` | File Explorer |
| Developer tool / dashboard | `NavigationView` + card layout | Dev Home |
| Single-purpose utility | Mode switcher + compact grid | Calculator |

#### Step 2: Map Requirements to Controls
**Navigation:** 2-7 sections → `NavigationView`; document tabs → `TabView`; breadcrumb trail → `BreadcrumbBar`; 2-3 modes → `SelectorBar`.

**Data display:** Vertical list → `ListView`; tiles/grid → `GridView` or `ItemsRepeater` + `UniformGridLayout`; hierarchy → `TreeView`; tabular → `ListView` with Grid column headers; master-detail → `ListView` + detail `Grid`.

**Input:** Text → `TextBox`; number → `NumberBox`; search → `AutoSuggestBox`; date → `CalendarDatePicker`; boolean → `ToggleSwitch`; pick one from 2-3 → `RadioButtons`; pick one from 4+ → `ComboBox`.

**Feedback:** Blocking decision → `ContentDialog`; contextual action → `Flyout`/`MenuFlyout`; onboarding → `TeachingTip`; inline status → `InfoBar`; system notification → `AppNotification`.

#### Step 3: Plan Layout
- **Content fills the window** — no floating cards on empty backgrounds
- `Grid` for structure, `StackPanel` only for simple stacking of few items
- Sidebar: fixed 300-360px width; main content: `Width="*"` with 24px padding
- Status bar: `Grid` row at bottom; toolbar: `CommandBar` or title bar buttons

#### Step 4: Size the Window to the App

> **WinUI 3 has no `SizeToContent`.** Without an explicit size, Windows defaults the main window to ~1024×768 — oversized for most utilities. **Size the window in `MainWindow`'s constructor; derive from the layout, not a generic.**

**Rubric.** Width = widest row + 48 padding (24 each side), rounded **up** to nearest 20. Height = 32 (titlebar) + Σ(row heights) + Σ(spacing) + 48 padding, rounded up to 20. Round up — clipped content is a worse failure than a slightly-wide window.

**Sanity check** (ranges, not targets — derive yours from the rubric):
- Single-purpose utility → ~440–560 wide
- Form / single-page tool → ~600–800 wide, ~640–800 tall
- Multi-pane (nav + content) → ~1100–1300 wide, ~720–840 tall
- Document / canvas / media editor → 1280+ wide

If your derived number is well below its range, you missed a row — re-check.

`AppWindow.Resize` takes **physical pixels**, not DIPs — multiply by the monitor's DPI scale:

```csharp
using Microsoft.UI;
using Microsoft.UI.Windowing;
using System.Runtime.InteropServices;
using Windows.Graphics;

public sealed partial class MainWindow : Window
{
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);

    public MainWindow()
    {
        InitializeComponent();
        var hwnd  = Win32Interop.GetWindowFromWindowId(AppWindow.Id);
        var scale = GetDpiForWindow(hwnd) / 96.0;
        AppWindow.Resize(new SizeInt32((int)(460 * scale), (int)(860 * scale)));
    }
}
```

`XamlRoot.RasterizationScale` is null in the ctor and stale after `AppWindow.Move`, so `[DllImport]` is the cleanest path. Don't try to size the window by setting `Width`/`Height` on the root `Grid` — that clips content, not the window.

If the user asks for UI validation, see `winui-ui-testing` Step 3.5 to verify the rubric against the visual checklist.

#### Step 5: Design Anti-Patterns
| ❌ Don't | ✅ Do Instead |
|----------|--------------|
| Centered floating card on background | Content fills window with padding |
| Custom pill/segment tab switcher | `NavigationView` Top or `SelectorBar` |
| Equal-width 50/50 column split | Fixed sidebar (300-360px) + flexible main |
| Hardcoded colors (`#FF0000`) | `{ThemeResource}` brushes |
| `ScrollViewer` around `ListView` | ListView has built-in scrolling |
| Custom ControlTemplate for standard controls | Built-in controls with style overrides |

### XAML Correctness

#### Theming Rules
- **`{ThemeResource BrushName}`** at usage sites — updates on theme change
- **`{StaticResource}`** with `ResourceKey` redirects inside theme dictionaries — zero allocation
- **`ResourceKey` must end in `Brush`** (target the `SolidColorBrush`, not the `Color`)
- Always define all three variants: `x:Key="Light"`, `x:Key="Dark"`, `x:Key="HighContrast"` — never use `x:Key="Default"`
- Verify runtime theme switching: `{ThemeResource}` updates; `{StaticResource}` does not

```xml
<!-- Correct: StaticResource redirect in theme dictionary -->
<StaticResource x:Key="MyBrush" ResourceKey="ControlFillColorDefaultBrush" />

<!-- Wrong: inline SolidColorBrush allocates new object -->
<SolidColorBrush x:Key="MyBrush" Color="{StaticResource ControlFillColorDefault}" />
```

#### High Contrast
Only 8 system color brushes allowed in HC dictionaries:

| Background | Foreground | Use Case |
|------------|------------|----------|
| `SystemColorWindowColorBrush` | `SystemColorWindowTextColorBrush` | General content |
| `SystemColorHighlightColorBrush` | `SystemColorHighlightTextColorBrush` | Selected/hover |
| `SystemColorButtonFaceColorBrush` | `SystemColorButtonTextColorBrush` | Buttons |
| `SystemColorWindowColorBrush` | `SystemColorHotlightColorBrush` | Hyperlinks |
| `SystemColorWindowColorBrush` | `SystemColorGrayTextColorBrush` | Disabled content |

**HC prohibitions:** No hardcoded colors, no opacity, no accent colors, no regular WinUI brushes, no `SystemColor*` in Light/Dark dicts. Use empty HC dict when WinUI defaults suffice. Set `HighContrastAdjustment = None` at app level.

#### Typography — Use Styles, Not Raw FontSize
| Style | Size | Weight | Use For |
|-------|------|--------|---------|
| `CaptionTextBlockStyle` | 12px | Regular | Small labels, timestamps |
| `BodyTextBlockStyle` | 14px | Regular | Body text (default — don't set explicitly) |
| `BodyStrongTextBlockStyle` | 14px | Semibold | Emphasized body text |
| `SubtitleTextBlockStyle` | 20px | Semibold | Section headers, card titles |
| `TitleTextBlockStyle` | 28px | Semibold | Page titles |
| `TitleLargeTextBlockStyle` | 40px | Semibold | Large feature titles |
| `DisplayTextBlockStyle` | 68px | Semibold | Hero text |

Use `SemiBold`, never `Bold`. Minimum 12px. `BasedOn` styles must not re-declare inherited properties.

#### Spacing and Layout
- **4px grid:** margins, padding, sizes must be multiples of 4 (4, 8, 12, 16, 24, 32, 48)
- `ControlCornerRadius` (4px) for controls, `OverlayCornerRadius` (8px) for overlays — never hardcode
- `RowSpacing`/`ColumnSpacing` instead of spacer elements
- `MinHeight`/`MinWidth` instead of fixed sizing
- No negative margins

#### Remove Defaults
Don't set WinUI default values — blocks future updates:
- `BodyTextBlockStyle` on TextBlock, `TextFillColorPrimaryBrush` foreground, `TextWrapping="NoWrap"`, `Padding="0"`, `Margin="0"`

#### Acrylic Pairings
| Surface | Background | Border |
|---------|-----------|--------|
| Flyouts, tooltips | `AcrylicBackgroundFillColorDefaultBrush` | `SurfaceStrokeColorFlyoutBrush` |
| UI surfaces | `AcrylicBackgroundFillColorBaseBrush` | `SurfaceStrokeColorDefaultBrush` |

Use `BackgroundSizing="InnerBorderEdge"` on bordered acrylic. `ThemeShadow` requires `Translation="0,0,32"` and 12px parent padding.

#### Data Binding
- `{x:Bind}` over `{Binding}`, explicit `Mode=OneWay`/`TwoWay`, `x:DataType` on `DataTemplate`
- **TextBox `x:Bind TwoWay` — always add `UpdateSourceTrigger=PropertyChanged`** so the ViewModel updates on each keystroke instead of waiting for `LostFocus`. Without it, UIA automation (`set-value`) and programmatic changes won't commit to the ViewModel.
  ```xml
  <TextBox Text="{x:Bind ViewModel.Name, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
  ```
- Commands over Click/Tapped handlers (MVVM)
- `VisualStateManager` for visual property changes, not code-behind
- No `IValueConverter` — prefer `x:Bind` with functions

**Bool negation and Visibility functions** — define static methods in code-behind:
```csharp
// In code-behind (e.g., MainPage.xaml.cs)
public static Visibility BoolToVisibility(bool value) =>
    value ? Visibility.Visible : Visibility.Collapsed;
public static Visibility InvertBoolToVisibility(bool value) =>
    value ? Visibility.Collapsed : Visibility.Visible;
public static bool IsNotBusy(bool isLoading) => !isLoading;
```
```xml
<!-- Usage in XAML -->
Visibility="{x:Bind local:MainPage.BoolToVisibility(ViewModel.IsLoading), Mode=OneWay}"
IsEnabled="{x:Bind local:MainPage.IsNotBusy(ViewModel.IsLoading), Mode=OneWay}"
```
❌ NEVER use `Converter={x:Null}` — it crashes at runtime.

#### Accessibility
- `AutomationProperties.Name` on icon-only controls
- `AutomationProperties.AutomationId` on all interactive controls
- Semantic controls (`Button`, `HyperlinkButton`) — not clickable `Border`/`TextBlock`
- `DividerStrokeColorDefaultBrush` for dividers

**Setting attached properties in code-behind** — WinUI attached properties use static methods, NOT object initializer syntax:
```csharp
using Microsoft.UI.Xaml.Automation; // required for AutomationProperties

// ❌ WRONG — object initializer doesn't work for attached properties
var btn = new Button { AutomationProperties = { AutomationId = "BtnSave" } };

// ✅ CORRECT — static setter method
var btn = new Button { Content = "Save" };
AutomationProperties.SetAutomationId(btn, "BtnSave");
AutomationProperties.SetName(btn, "Save button");
Grid.SetRow(btn, 1);
Grid.SetColumn(btn, 0);
ToolTipService.SetToolTip(btn, "Save the current document");
```

#### Formatting
- Self-closing tags for childless elements
- Styles referenced with `{StaticResource}` not `{ThemeResource}`
- No `px` suffix on numeric values, no commented-out XAML
- Consistent attribute order: x:Name, AutomationProperties, layout, content, style

### References

| File | Read when... |
|------|-------------|
| `references/approved-brushes.md` | Looking up correct WinUI brush names and usage rules |
| `references/theme-aware-resources.md` | Implementing ThemeResource/StaticResource, High Contrast, acrylic pairings |
| `references/code-review-checklist.md` | Reviewing XAML changes for correctness |
| `references/pr-review-patterns.md` | Applying concrete review fixes and patterns |
| `references/control-styles.md` | Customizing built-in control styles |
| `references/typography-and-spacing.md` | Detailed type ramp, spacing grid, and sizing examples |
| `references/colors-and-materials.md` | Theme brush catalog, Mica/Acrylic surface pairings, material usage |
| `references/iconography-and-motion.md` | Icon guidelines, animation patterns, connected animations |
