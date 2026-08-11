# Typography and Spacing Reference

## Typography — Type Ramp

Use the **built-in TextBlock styles** — never set `FontSize` or `FontWeight` manually. The type ramp uses **Segoe UI Variable** and scales correctly across displays.

| Style | Use for |
|-------|---------|
| `CaptionTextBlockStyle` | Labels, timestamps, metadata |
| `BodyTextBlockStyle` | Body text, descriptions (default) |
| `BodyStrongTextBlockStyle` | Emphasized body text |
| `BodyLargeTextBlockStyle` | Introductory text |
| `SubtitleTextBlockStyle` | Section headings |
| `TitleTextBlockStyle` | Page titles |
| `TitleLargeTextBlockStyle` | Hero headings |
| `DisplayTextBlockStyle` | Splash / display only |

Always reference these `StaticResource` styles — never hardcode font sizes, weights, or line heights.

```xml
<!-- GOOD — use built-in styles -->
<TextBlock Text="Settings" Style="{StaticResource SubtitleTextBlockStyle}" />
<TextBlock Text="Choose your preferences below." Style="{StaticResource BodyTextBlockStyle}" />
<TextBlock Text="Last updated: 3/10/2026" Style="{StaticResource CaptionTextBlockStyle}" />

<!-- BAD — never hardcode font properties -->
<TextBlock Text="Settings" FontSize="20" FontWeight="SemiBold" />
```

**Minimum readable sizes:** 12px Regular for labels, 14px SemiBold for smallest bold text. Never go below 12px.

---

## Spacing — 4px Grid

All spacing and sizing values must be **multiples of 4px**. This ensures consistent alignment and scaling across DPI settings.

**Standard spacing scale (effective pixels):**

| Value | Use for |
|-------|---------|
| **4px** | Compact spacing between tightly related elements |
| **8px** | Spacing between a control and its label, between grouped controls |
| **12px** | Spacing between a control and its header, surface edge to text |
| **16px** | Padding inside cards and list items |
| **24px** | Spacing between content sections |
| **36px** | Page-level padding (content area margins) |
| **48px** | Spacing between major page sections with titles |

```xml
<!-- GOOD — multiples of 4 -->
<StackPanel Spacing="8">
    <TextBlock Text="Name" Style="{StaticResource BodyStrongTextBlockStyle}" />
    <TextBox PlaceholderText="Enter your name" />
</StackPanel>

<Grid Padding="36" RowSpacing="24" ColumnSpacing="16">
    <!-- Page content with standard padding and section spacing -->
</Grid>

<!-- BAD — arbitrary values -->
<StackPanel Spacing="10" Margin="15,7,15,7" />
```
