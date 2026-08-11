using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;

namespace FluentDeck.Helpers;

/// <summary>
/// Shared UI utility methods for clipboard copying, feedback animations, and file exports.
/// Eliminates code duplication across Emoji, Icon, and App gallery pages.
/// </summary>
public static class ClipboardAndFileHelpers
{
    public static void CopyTextToClipboard(string text)
    {
        try
        {
            var package = new DataPackage();
            package.SetText(text);
            Clipboard.SetContent(package);
        }
        catch { }
    }

    public static async Task AnimateCopySuccessAsync(Button button, string originalText, string originalIconGlyph = "\uE8C8")
    {
        if (button.Content is not StackPanel stackPanel) return;

        FontIcon? icon = null;
        TextBlock? textBlock = null;
        foreach (var child in stackPanel.Children)
        {
            if (child is FontIcon f) icon = f;
            else if (child is TextBlock t) textBlock = t;
        }

        if (icon == null || textBlock == null) return;

        if (button.Tag is CancellationTokenSource oldCts)
        {
            oldCts.Cancel();
            oldCts.Dispose();
        }
        var currentCts = new CancellationTokenSource();
        button.Tag = currentCts;

        textBlock.Text = "Copied";
        icon.Glyph = "\uE73E";

        var scaleTransform = stackPanel.RenderTransform as ScaleTransform;
        if (scaleTransform == null)
        {
            scaleTransform = new ScaleTransform();
            stackPanel.RenderTransform = scaleTransform;
        }

        var storyboard = new Storyboard();

        var animX = new DoubleAnimation
        {
            From = 0.88,
            To = 1.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(200)),
            EasingFunction = new BackEase { Amplitude = 0.35, EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(animX, scaleTransform);
        Storyboard.SetTargetProperty(animX, "ScaleX");

        var animY = new DoubleAnimation
        {
            From = 0.88,
            To = 1.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(200)),
            EasingFunction = new BackEase { Amplitude = 0.35, EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(animY, scaleTransform);
        Storyboard.SetTargetProperty(animY, "ScaleY");

        storyboard.Children.Add(animX);
        storyboard.Children.Add(animY);
        storyboard.Begin();

        try
        {
            await Task.Delay(2000, currentCts.Token);
            if (!currentCts.Token.IsCancellationRequested)
            {
                textBlock.Text = originalText;
                icon.Glyph = originalIconGlyph;
            }
        }
        catch (TaskCanceledException)
        {
            // Cancelled by a subsequent click
        }
    }

    public static async Task SaveImageFromAppRelativePathAsync(string msAppxPath)
    {
        if (string.IsNullOrEmpty(msAppxPath)) return;

        try
        {
            string relativePath = msAppxPath.Replace("ms-appx:///", "").Replace("/", "\\");
            string sourceFile = Path.Combine(AppContext.BaseDirectory, relativePath);

            if (!File.Exists(sourceFile)) return;

            string ext = Path.GetExtension(sourceFile);
            var picker = new FileSavePicker();
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.SuggestedFileName = Path.GetFileNameWithoutExtension(sourceFile);

            if (ext.Equals(".svg", StringComparison.OrdinalIgnoreCase))
            {
                picker.FileTypeChoices.Add("SVG Image", new List<string> { ".svg" });
            }
            else
            {
                picker.FileTypeChoices.Add("PNG Image", new List<string> { ".png" });
            }

            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);

            var targetFile = await picker.PickSaveFileAsync();
            if (targetFile != null)
            {
                using (var sourceStream = File.OpenRead(sourceFile))
                using (var targetStream = await targetFile.OpenStreamForWriteAsync())
                {
                    targetStream.SetLength(0);
                    await sourceStream.CopyToAsync(targetStream);
                }
            }
        }
        catch { }
    }
}
