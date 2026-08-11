# Approved Brushes

When reviewing XAML, verify that every brush is either:
1. A custom brush with explicit `Light`, `Dark`, and `HighContrast` theme dictionaries, OR
2. One of the approved brushes listed below.

If neither, request the author to switch to an approved brush or provide the complete theme-aware resource set.

## Usage

- `{ThemeResource BrushName}` at usage sites (updates on theme change)
- `{StaticResource BrushName}` inside theme dictionaries
- Names ending in `Brush` are `SolidColorBrush` resources; names without are `Color` resources
- Prefer the `Brush` key when assigning to Foreground/Background

## Common System Brushes (Quick Reference)

| Resource | Purpose |
|----------|---------|
| `TextFillColorPrimaryBrush` | Primary text |
| `TextFillColorSecondaryBrush` | Secondary text |
| `TextFillColorTertiaryBrush` | Tertiary / placeholder text |
| `TextFillColorDisabledBrush` | Disabled text |
| `AccentFillColorDefaultBrush` | Accent color fills |
| `ControlFillColorDefaultBrush` | Control backgrounds |
| `CardBackgroundFillColorDefaultBrush` | Card backgrounds |
| `LayerFillColorDefaultBrush` | Layer backgrounds |

## Text Fill

    TextFillColorPrimary / TextFillColorPrimaryBrush
    TextFillColorSecondary / TextFillColorSecondaryBrush
    TextFillColorTertiary / TextFillColorTertiaryBrush
    TextFillColorDisabled / TextFillColorDisabledBrush
    TextFillColorInverse / TextFillColorInverseBrush
    AccentTextFillColorPrimary / AccentTextFillColorPrimaryBrush
    AccentTextFillColorSecondary / AccentTextFillColorSecondaryBrush
    AccentTextFillColorTertiary / AccentTextFillColorTertiaryBrush
    AccentTextFillColorDisabled / AccentTextFillColorDisabledBrush
    TextOnAccentFillColorSelectedText / TextOnAccentFillColorSelectedTextBrush
    TextOnAccentFillColorPrimary / TextOnAccentFillColorPrimaryBrush
    TextOnAccentFillColorSecondary / TextOnAccentFillColorSecondaryBrush
    TextOnAccentFillColorDisabled / TextOnAccentFillColorDisabledBrush

## Control Fill

    ControlFillColorDefault / ControlFillColorDefaultBrush
    ControlFillColorSecondary / ControlFillColorSecondaryBrush
    ControlFillColorTertiary / ControlFillColorTertiaryBrush
    ControlFillColorQuarternary / ControlFillColorQuarternaryBrush
    ControlFillColorDisabled / ControlFillColorDisabledBrush
    ControlFillColorTransparent / ControlFillColorTransparentBrush
    ControlFillColorInputActive / ControlFillColorInputActiveBrush
    ControlStrongFillColorDefault / ControlStrongFillColorDefaultBrush
    ControlStrongFillColorDisabled / ControlStrongFillColorDisabledBrush
    ControlSolidFillColorDefault / ControlSolidFillColorDefaultBrush

## Subtle Fill

    SubtleFillColorTransparent / SubtleFillColorTransparentBrush
    SubtleFillColorSecondary / SubtleFillColorSecondaryBrush
    SubtleFillColorTertiary / SubtleFillColorTertiaryBrush
    SubtleFillColorDisabled / SubtleFillColorDisabledBrush

## Control Alt Fill

    ControlAltFillColorTransparent / ControlAltFillColorTransparentBrush
    ControlAltFillColorSecondary / ControlAltFillColorSecondaryBrush
    ControlAltFillColorTertiary / ControlAltFillColorTertiaryBrush
    ControlAltFillColorQuarternary / ControlAltFillColorQuarternaryBrush
    ControlAltFillColorDisabled / ControlAltFillColorDisabledBrush

## Control On Image Fill

    ControlOnImageFillColorDefault / ControlOnImageFillColorDefaultBrush
    ControlOnImageFillColorSecondary / ControlOnImageFillColorSecondaryBrush
    ControlOnImageFillColorTertiary / ControlOnImageFillColorTertiaryBrush
    ControlOnImageFillColorDisabled / ControlOnImageFillColorDisabledBrush

## Accent Fill

    AccentFillColorSelectedTextBackground / AccentFillColorSelectedTextBackgroundBrush
    AccentFillColorDefault / AccentFillColorDefaultBrush
    AccentFillColorSecondary / AccentFillColorSecondaryBrush
    AccentFillColorTertiary / AccentFillColorTertiaryBrush
    AccentFillColorDisabled / AccentFillColorDisabledBrush

## Stroke

    ControlStrokeColorDefault / ControlStrokeColorDefaultBrush
    ControlStrokeColorSecondary / ControlStrokeColorSecondaryBrush
    ControlStrokeColorOnAccentDefault / ControlStrokeColorOnAccentDefaultBrush
    ControlStrokeColorOnAccentSecondary / ControlStrokeColorOnAccentSecondaryBrush
    ControlStrokeColorOnAccentTertiary / ControlStrokeColorOnAccentTertiaryBrush
    ControlStrokeColorOnAccentDisabled / ControlStrokeColorOnAccentDisabledBrush
    ControlStrokeColorForStrongFillWhenOnImage / ControlStrokeColorForStrongFillWhenOnImageBrush
    CardStrokeColorDefault / CardStrokeColorDefaultBrush
    CardStrokeColorDefaultSolid / CardStrokeColorDefaultSolidBrush
    ControlStrongStrokeColorDefault / ControlStrongStrokeColorDefaultBrush
    ControlStrongStrokeColorDisabled / ControlStrongStrokeColorDisabledBrush
    SurfaceStrokeColorDefault / SurfaceStrokeColorDefaultBrush
    SurfaceStrokeColorFlyout / SurfaceStrokeColorFlyoutBrush
    SurfaceStrokeColorInverse / SurfaceStrokeColorInverseBrush
    DividerStrokeColorDefault / DividerStrokeColorDefaultBrush
    FocusStrokeColorOuter / FocusStrokeColorOuterBrush
    FocusStrokeColorInner / FocusStrokeColorInnerBrush

## Background Fill

    CardBackgroundFillColorDefault / CardBackgroundFillColorDefaultBrush
    CardBackgroundFillColorSecondary / CardBackgroundFillColorSecondaryBrush
    CardBackgroundFillColorTertiary / CardBackgroundFillColorTertiaryBrush
    SmokeFillColorDefault / SmokeFillColorDefaultBrush
    LayerFillColorDefault / LayerFillColorDefaultBrush
    LayerFillColorAlt / LayerFillColorAltBrush
    LayerOnAcrylicFillColorDefault / LayerOnAcrylicFillColorDefaultBrush
    LayerOnAccentAcrylicFillColorDefault / LayerOnAccentAcrylicFillColorDefaultBrush
    LayerOnMicaBaseAltFillColorDefault / LayerOnMicaBaseAltFillColorDefaultBrush
    LayerOnMicaBaseAltFillColorSecondary / LayerOnMicaBaseAltFillColorSecondaryBrush
    LayerOnMicaBaseAltFillColorTertiary / LayerOnMicaBaseAltFillColorTertiaryBrush
    LayerOnMicaBaseAltFillColorTransparent / LayerOnMicaBaseAltFillColorTransparentBrush

## Solid Background Fill

    SolidBackgroundFillColorBase / SolidBackgroundFillColorBaseBrush
    SolidBackgroundFillColorSecondary / SolidBackgroundFillColorSecondaryBrush
    SolidBackgroundFillColorTertiary / SolidBackgroundFillColorTertiaryBrush
    SolidBackgroundFillColorQuarternary / SolidBackgroundFillColorQuarternaryBrush
    SolidBackgroundFillColorQuinary / SolidBackgroundFillColorQuinaryBrush
    SolidBackgroundFillColorSenary / SolidBackgroundFillColorSenaryBrush
    SolidBackgroundFillColorTransparent / SolidBackgroundFillColorTransparentBrush
    SolidBackgroundFillColorBaseAlt / SolidBackgroundFillColorBaseAltBrush

## System Fill

    SystemFillColorSuccess / SystemFillColorSuccessBrush
    SystemFillColorCaution / SystemFillColorCautionBrush
    SystemFillColorCritical / SystemFillColorCriticalBrush
    SystemFillColorNeutral / SystemFillColorNeutralBrush
    SystemFillColorSolidNeutral / SystemFillColorSolidNeutralBrush
    SystemFillColorAttentionBackground / SystemFillColorAttentionBackgroundBrush
    SystemFillColorSuccessBackground / SystemFillColorSuccessBackgroundBrush
    SystemFillColorCautionBackground / SystemFillColorCautionBackgroundBrush
    SystemFillColorCriticalBackground / SystemFillColorCriticalBackgroundBrush
    SystemFillColorNeutralBackground / SystemFillColorNeutralBackgroundBrush
    SystemFillColorSolidAttentionBackground / SystemFillColorSolidAttentionBackgroundBrush
    SystemFillColorSolidNeutralBackground / SystemFillColorSolidNeutralBackgroundBrush

## High Contrast System Brushes

For `x:Key="HighContrast"` dictionaries only:

    SystemColorWindowTextColorBrush
    SystemColorWindowColorBrush
    SystemColorButtonFaceColorBrush
    SystemColorButtonTextColorBrush
    SystemColorHighlightColorBrush
    SystemColorHighlightTextColorBrush
    SystemColorHotlightColorBrush
    SystemColorGrayTextColorBrush
