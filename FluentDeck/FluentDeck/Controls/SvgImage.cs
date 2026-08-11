using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;
using Svg.Skia;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;

namespace FluentDeck.Controls;

/// <summary>
/// Renders SVG files using SkiaSharp for monochrome icons (with dark-theme inversion)
/// and WinUI native SvgImageSource for color icons and emoji.
/// Assets are loaded from the robocopy-synced output folder at AppContext.BaseDirectory.
/// </summary>
public sealed class SvgImage : UserControl
{
    public static readonly DependencyProperty SourcePathProperty =
        DependencyProperty.Register(
            nameof(SourcePath),
            typeof(string),
            typeof(SvgImage),
            new PropertyMetadata(null, OnSourcePathChanged));

    public string? SourcePath
    {
        get => (string?)GetValue(SourcePathProperty);
        set => SetValue(SourcePathProperty, value);
    }

    public static readonly DependencyProperty InvertColorsProperty =
        DependencyProperty.Register(
            nameof(InvertColors),
            typeof(bool),
            typeof(SvgImage),
            new PropertyMetadata(false, OnInvertColorsChanged));

    public bool InvertColors
    {
        get => (bool)GetValue(InvertColorsProperty);
        set => SetValue(InvertColorsProperty, value);
    }

    private static void OnInvertColorsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SvgImage ctrl)
            _ = ctrl.LoadSvgAsync(ctrl.SourcePath);
    }

    private readonly Image _image;
    private CancellationTokenSource? _cts;

    public SvgImage()
    {
        _image = new Image
        {
            Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        Content = _image;
        ActualThemeChanged += (s, e) => _ = LoadSvgAsync(SourcePath);
    }

    private static void OnSourcePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SvgImage ctrl)
            _ = ctrl.LoadSvgAsync(e.NewValue as string);
    }

    private async Task LoadSvgAsync(string? path)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        var cts = new CancellationTokenSource();
        _cts = cts;

        _image.Source = null;

        if (string.IsNullOrWhiteSpace(path)) return;

        // Resolve ms-appx:/// to physical path (assets are robocopy'd to OutDir/Assets)
        string? filePath = ResolveToFilePath(path);
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;

        // Color SVG: use WinUI native SvgImageSource (supports gradients, multi-color)
        if (IsColorSvg(path))
        {
            try
            {
                if (Uri.TryCreate(path, UriKind.Absolute, out var svgUri))
                {
                    var svgSource = new SvgImageSource(svgUri);
                    if (!cts.IsCancellationRequested)
                        _image.Source = svgSource;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SvgImage] Color SVG error '{path}': {ex.Message}");
            }
            return;
        }

        // Non-SVG (PNG, WebP, etc.)
        if (!path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
                {
                    var bitmapImage = new BitmapImage(uri);
                    if (!cts.IsCancellationRequested)
                        _image.Source = bitmapImage;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SvgImage] BitmapImage error '{path}': {ex.Message}");
            }
            return;
        }

        // Monochrome SVG (Regular / Filled): rasterize with SkiaSharp + invert in dark theme
        try
        {
            double targetDim = 160;
            if (!double.IsNaN(Width) && !double.IsInfinity(Width) && Width > 0)
                targetDim = Width;
            else if (!double.IsNaN(Height) && !double.IsInfinity(Height) && Height > 0)
                targetDim = Height;
            else if (!double.IsNaN(ActualWidth) && !double.IsInfinity(ActualWidth) && ActualWidth > 0)
                targetDim = ActualWidth;
            else if (!double.IsNaN(ActualHeight) && !double.IsInfinity(ActualHeight) && ActualHeight > 0)
                targetDim = ActualHeight;

            int displayPx = (int)Math.Clamp(targetDim, 16, 512);
            int renderPx = displayPx * 2; // 2× HiDPI

            bool shouldInvert = InvertColors || (ActualTheme == ElementTheme.Dark);

            byte[]? pixels = await Task.Run(
                () => RasterizeSvg(filePath, renderPx, shouldInvert, cts.Token),
                cts.Token);

            if (cts.IsCancellationRequested || pixels == null) return;

            var bmp = new WriteableBitmap(renderPx, renderPx);
            using (var stream = bmp.PixelBuffer.AsStream())
                await stream.WriteAsync(pixels, 0, pixels.Length, cts.Token);
            bmp.Invalidate();

            if (!cts.IsCancellationRequested)
                _image.Source = bmp;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SvgImage] SVG render error '{path}': {ex.Message}");
        }
    }

    /// <summary>Returns true if this path points to a multi-color SVG asset (color icons, color emoji SVGs, and flat emoji SVGs).</summary>
    private static bool IsColorSvg(string path) =>
        path.Contains("icon_color", StringComparison.OrdinalIgnoreCase) ||
        path.Contains("_color.svg", StringComparison.OrdinalIgnoreCase) ||
        path.Contains("/flat/", StringComparison.OrdinalIgnoreCase) ||
        (path.Contains("/color/", StringComparison.OrdinalIgnoreCase) && !path.Contains("/high_contrast/", StringComparison.OrdinalIgnoreCase));

    /// <summary>Resolves ms-appx:/// URIs to a physical file path under AppContext.BaseDirectory.</summary>
    private static string? ResolveToFilePath(string path)
    {
        if (path.StartsWith("ms-appx:///", StringComparison.OrdinalIgnoreCase))
        {
            string relative = path["ms-appx:///".Length..].Replace('/', '\\');
            return Path.Combine(AppContext.BaseDirectory, relative);
        }
        if (Path.IsPathRooted(path)) return path;
        return null;
    }

    private static byte[]? RasterizeSvg(string filePath, int sizePx, bool invertColors, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            using var svg = new SKSvg();
            var picture = svg.Load(filePath);
            if (picture == null) return null;

            ct.ThrowIfCancellationRequested();

            var bounds = picture.CullRect;
            if (bounds.Width <= 0 || bounds.Height <= 0) return null;

            float scale = Math.Min(sizePx / bounds.Width, sizePx / bounds.Height);
            float dx = (sizePx - bounds.Width * scale) / 2f;
            float dy = (sizePx - bounds.Height * scale) / 2f;

            var info = new SKImageInfo(sizePx, sizePx, SKColorType.Bgra8888, SKAlphaType.Premul);
            using var surface = SKSurface.Create(info);
            if (surface == null) return null;

            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);
            canvas.Translate(dx, dy);
            canvas.Scale(scale, scale);

            ct.ThrowIfCancellationRequested();

            using var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };

            if (invertColors)
            {
                float[] m =
                [
                    -1f,  0f,  0f,  0f, 255f,
                     0f, -1f,  0f,  0f, 255f,
                     0f,  0f, -1f,  0f, 255f,
                     0f,  0f,  0f,  1f,   0f
                ];
                paint.ColorFilter = SKColorFilter.CreateColorMatrix(m);
            }

            canvas.DrawPicture(picture, paint);
            canvas.Flush();

            using var skImage = surface.Snapshot();
            byte[] buffer = new byte[info.RowBytes * sizePx];
            var gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                skImage.ReadPixels(info, gcHandle.AddrOfPinnedObject(), info.RowBytes, 0, 0);
            }
            finally
            {
                gcHandle.Free();
            }
            return buffer;
        }
        catch (OperationCanceledException) { throw; }
        catch { return null; }
    }
}
