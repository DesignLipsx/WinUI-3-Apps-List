# Code Review Checklist

## Theme Support

- [ ] Uses `{ThemeResource}` for colors/brushes at usage sites
- [ ] Uses `{StaticResource}` with `ResourceKey` redirects inside theme dictionaries
- [ ] Custom theme resources define `Light`, `Dark`, and `HighContrast` variants
- [ ] No `x:Key="Default"` — uses explicit `Light`/`Dark`/`HighContrast`
- [ ] `ResourceKey` values end in `Brush` (not the Color name)
- [ ] HC dictionary uses only the 8 system color brushes (no accent, no hardcoded, no WinUI brushes)
- [ ] No opacity on elements or brushes in HC dictionaries
- [ ] No HC resources (`SystemColor*`) used in Light/Dark dictionaries
- [ ] `StaticResource` redirects preferred over inline `SolidColorBrush` in theme dicts
- [ ] Theme dictionary `x:Key` order consistent across Light/Dark/HighContrast
- [ ] No partial theme updates — Light/Dark changes include matching HighContrast in same PR
- [ ] No ad-hoc themed literals (`White`/`Transparent` for themed surfaces)
- [ ] Acrylic surfaces use correct border/background pairings
- [ ] Light and Dark dicts reference the same semantic WinUI keys; differences are intentional
- [ ] Empty HC dict used when WinUI defaults suffice (`<ResourceDictionary x:Key="HighContrast" />`)
- [ ] `HighContrastAdjustment="None"` set at app level
- [ ] Accent colors use `SystemAccentColor*` resources (no hardcoded accent values)
- [ ] Verify runtime theme switching (ThemeResource updates; StaticResource does not)

## Data Binding

- [ ] Uses `{x:Bind}` over `{Binding}` where possible
- [ ] `Mode` explicitly set on `x:Bind` when values change (`OneWay`/`TwoWay`)
- [ ] `DataTemplate` has `x:DataType` specified
- [ ] No `IValueConverter` — uses `x:Bind` with functions
- [ ] Button text uses `Content` directly, not a nested `TextBlock`
- [ ] `IsEnabled` bound from ViewModel readiness state
- [ ] Commands used instead of Click/Tapped event handlers (MVVM)
- [ ] `VisualStateManager` used for visual property changes (not code-behind)
- [ ] ViewModel state mapped to named properties (bool/enum) instead of complex converter stacks
- [ ] Converters are only simple type conversions; business logic stays in ViewModel
- [ ] No code-behind for styles/colors/layout (exception: `HighContrastAdjustment` at app level)

## Typography

- [ ] Uses system text styles, not hardcoded font properties
- [ ] `FontWeight` is `SemiBold`, never `Bold`
- [ ] `BasedOn` styles do not re-declare inherited properties
- [ ] Default `TextFillColorPrimaryBrush` foreground not explicitly set
- [ ] No font sizes below 12px
- [ ] Icon TextBlocks set `IsTextScaleFactorEnabled="False"`
- [ ] Icon font uses `{ThemeResource SymbolThemeFontFamily}`, not hardcoded

## Layout

- [ ] Uses `ControlCornerRadius`/`OverlayCornerRadius` (not hardcoded)
- [ ] Selective corner rounding uses standard radii (e.g., `8,8,0,0`)
- [ ] Margins/padding use multiples of 4
- [ ] Uses `MinHeight`/`MinWidth` instead of fixed sizing
- [ ] No fixed heights on text containers
- [ ] No fixed button widths (content-driven or `MinWidth`)
- [ ] `Border` for single-child containers (not nested Grids)
- [ ] `StackPanel` does not contain TextBlocks needing `TextTrimming`
- [ ] `RowSpacing`/`ColumnSpacing` used instead of spacer elements
- [ ] No negative margins
- [ ] `ThemeShadow` has `Translation="0,0,32"` and 12px parent padding
- [ ] Shadow receiver is behind the elevated element (z-order)
- [ ] `BackgroundSizing="InnerBorderEdge"` on bordered acrylic elements
- [ ] Mixed-control rows vertically centered

## Styles

- [ ] Styles referenced with `{StaticResource}` (not `{ThemeResource}`)
- [ ] Default WinUI property values not explicitly set (Padding, CornerRadius, etc.)
- [ ] Single-use styles inlined, named style deleted
- [ ] Existing WinUI styles checked before creating custom ones
- [ ] No no-op style churn
- [ ] App-specific resources stay in app or feature dictionaries
- [ ] `ThemeResource` used inside style setters for themed values
- [ ] VisualStateManager uses AdaptiveTrigger for responsive layout when needed
- [ ] Unused VisualState definitions removed

## Resource Organization & Naming

- [ ] Semantic resource keys used (avoid location-based names)
- [ ] Shared resources promoted only when reused across features
- [ ] One-off values remain local (no global resources for single-use)

## Accessibility

- [ ] `AutomationProperties.Name` on icon-only controls
- [ ] Light-dismiss targets are hit-test visible (`Background="Transparent"`)
- [ ] `DividerStrokeColorDefaultBrush` for dividers (not custom opacity brushes)

## Performance

- [ ] `x:Load` for conditional content
- [ ] `x:Phase` for list item incremental loading
- [ ] `OneTime` binding for static content
- [ ] Minimal container nesting

## Formatting

- [ ] Uniform indentation (spaces, no tabs)
- [ ] Self-closing tags for childless elements
- [ ] No `px` suffix on numeric values
- [ ] No commented-out XAML
- [ ] Unused VisualState definitions removed
- [ ] ThemeDictionaries before non-themed resources
- [ ] Namespace declarations ordered consistently (default, `x:`, platform, WinUI, local)
- [ ] Attribute order consistent across related files
- [ ] No checkpoint files checked in (use `_CP` locally and remove before commit)

## Testing Reminders

**If changing brushes:** Test in NightSky HC theme, hover on all interactive elements. Include Light/Dark/HC screenshot evidence.

**If changing text/containers:** Test with text scaling at max and with long/localized strings.

**If changing layout:** Test at 100%, 150%, 200%, 250% display scaling. Validate Figma at 100% scale.
