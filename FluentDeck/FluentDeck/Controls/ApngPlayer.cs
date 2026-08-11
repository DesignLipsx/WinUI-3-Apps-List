using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FluentDeck.Controls;

public sealed class ApngPlayer : UserControl
{
    public static readonly DependencyProperty SourcePathProperty =
        DependencyProperty.Register(
            nameof(SourcePath),
            typeof(string),
            typeof(ApngPlayer),
            new PropertyMetadata(string.Empty, OnSourcePathChanged));

    public string SourcePath
    {
        get => (string)GetValue(SourcePathProperty);
        set => SetValue(SourcePathProperty, value);
    }

    private readonly CanvasControl _canvas;
    private readonly DispatcherTimer _timer;

    private List<CanvasBitmap>? _frames;
    private int[]? _frameDelaysMs;
    private int _currentFrame;
    private bool _isAnimated;

    public ApngPlayer()
    {
        _canvas = new CanvasControl();
        _canvas.CreateResources += OnCreateResources;
        _canvas.Draw += OnDraw;
        Content = _canvas;

        _timer = new DispatcherTimer();
        _timer.Tick += OnTimerTick;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _canvas.Invalidate();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Stop();
        FreeFrames();
        _canvas.RemoveFromVisualTree();
    }

    private static void OnSourcePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ApngPlayer player)
        {
            player.Stop();
            player.FreeFrames();
            player._canvas.Invalidate();
            _ = player.LoadResourcesAsync(player._canvas);
        }
    }

    private void OnCreateResources(CanvasControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
    {
        args.TrackAsyncAction(LoadResourcesAsync(sender).AsAsyncAction());
    }

    private async Task LoadResourcesAsync(CanvasControl sender)
    {
        if (string.IsNullOrEmpty(SourcePath)) return;

        try
        {
            FreeFrames();

            string filePath = ResolvePath(SourcePath);
            _isAnimated = filePath.Contains("animated", StringComparison.OrdinalIgnoreCase);

            if (!File.Exists(filePath)) return;

            byte[] fileData = await File.ReadAllBytesAsync(filePath);
            var parsedFrames = await Task.Run(() => ApngDecoder.Decode(fileData));

            if (parsedFrames != null && parsedFrames.Count > 0)
            {
                var framesList = new List<CanvasBitmap>();
                var delaysList = new List<int>();

                foreach (var frame in parsedFrames)
                {
                    using var stream = new MemoryStream(frame.PngBytes);
                    var canvasBmp = await CanvasBitmap.LoadAsync(sender, stream.AsRandomAccessStream());
                    framesList.Add(canvasBmp);
                    delaysList.Add(Math.Max(frame.DelayMs, 20));
                }

                _frames = framesList;
                _frameDelaysMs = delaysList.ToArray();
                if (parsedFrames.Count > 1) _isAnimated = true;
                _currentFrame = 0;
                AdvanceTimer();
            }
            else
            {
                // Fallback static image
                using var stream = new MemoryStream(fileData);
                var staticBmp = await CanvasBitmap.LoadAsync(sender, stream.AsRandomAccessStream());
                _frames = new List<CanvasBitmap> { staticBmp };
                _frameDelaysMs = new[] { 1000 };
                _currentFrame = 0;
            }

            _canvas.Invalidate();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ApngPlayer] Error loading image: {ex}");
        }
    }

    private void AdvanceTimer()
    {
        if (_frameDelaysMs == null || _frameDelaysMs.Length == 0) return;

        _timer.Stop();
        int frameIndex = _currentFrame;
        if (_frameDelaysMs != null && frameIndex >= 0 && frameIndex < _frameDelaysMs.Length)
        {
            _timer.Interval = TimeSpan.FromMilliseconds(_frameDelaysMs[frameIndex]);
            _timer.Start();
        }
    }

    private void OnTimerTick(object? sender, object e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            var frames = _frames;
            if (frames == null || frames.Count == 0) return;

            _currentFrame = (_currentFrame + 1) % frames.Count;
            AdvanceTimer();
            _canvas.Invalidate();
        });
    }

    private void Stop()
    {
        _timer.Stop();
        _currentFrame = 0;
    }

    private void OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        float canvasWidth = (float)sender.ActualWidth;
        float canvasHeight = (float)sender.ActualHeight;
        if (canvasWidth <= 0 || canvasHeight <= 0) return;

        var frames = _frames;
        if (frames == null || frames.Count == 0) return;

        int index = _currentFrame;
        if (index < 0 || index >= frames.Count) return;

        var masterBmp = frames[0];
        var bmp = frames[index];
        if (masterBmp == null || bmp == null) return;

        float scaleX = canvasWidth / (float)masterBmp.SizeInPixels.Width;
        float scaleY = canvasHeight / (float)masterBmp.SizeInPixels.Height;
        float scale = Math.Min(scaleX, scaleY);

        float drawW = (float)bmp.SizeInPixels.Width * scale;
        float drawH = (float)bmp.SizeInPixels.Height * scale;
        float offsetXImg = (canvasWidth - drawW) / 2f;
        float offsetYImg = (canvasHeight - drawH) / 2f;

        var destRectImg = new Windows.Foundation.Rect(offsetXImg, offsetYImg, drawW, drawH);

        args.DrawingSession.DrawImage(bmp, destRectImg, new Windows.Foundation.Rect(0, 0, bmp.SizeInPixels.Width, bmp.SizeInPixels.Height), 1.0f, Microsoft.Graphics.Canvas.CanvasImageInterpolation.HighQualityCubic);
    }

    private static string ResolvePath(string path)
    {
        if (path.StartsWith("ms-appx:///", StringComparison.OrdinalIgnoreCase))
        {
            string relative = path["ms-appx:///".Length..].Replace('/', '\\');
            return Path.Combine(AppContext.BaseDirectory, relative);
        }
        return path;
    }

    private void FreeFrames()
    {
        if (_frames != null)
        {
            foreach (var f in _frames) f?.Dispose();
            _frames = null;
        }
        _frameDelaysMs = null;
    }
}

public class ApngFrame
{
    public byte[] PngBytes { get; set; } = null!;
    public int DelayMs { get; set; }
}

public static class ApngDecoder
{
    private static readonly byte[] PngHeader = { 137, 80, 78, 71, 13, 10, 26, 10 };

    public static List<ApngFrame>? Decode(byte[] bytes)
    {
        if (bytes.Length < 8) return null;
        for (int i = 0; i < 8; i++)
            if (bytes[i] != PngHeader[i]) return null;

        int width = 0, height = 0;
        byte[]? ihdrChunkData = null;
        byte[]? plteChunkData = null;
        byte[]? trnsChunkData = null;

        var fcTls = new List<FcTlData>();
        var frameIdats = new List<List<byte[]>>();

        int offset = 8;
        FcTlData? currentFcTl = null;

        while (offset < bytes.Length - 4)
        {
            int length = ReadInt32BE(bytes, offset);
            string type = Encoding.ASCII.GetString(bytes, offset + 4, 4);
            int dataOffset = offset + 8;
            offset += 12 + length; // 4 len + 4 type + data + 4 crc

            if (type == "IHDR")
            {
                width = ReadInt32BE(bytes, dataOffset);
                height = ReadInt32BE(bytes, dataOffset + 4);
                ihdrChunkData = new byte[length];
                Buffer.BlockCopy(bytes, dataOffset, ihdrChunkData, 0, length);
            }
            else if (type == "PLTE")
            {
                plteChunkData = new byte[length];
                Buffer.BlockCopy(bytes, dataOffset, plteChunkData, 0, length);
            }
            else if (type == "tRNS")
            {
                trnsChunkData = new byte[length];
                Buffer.BlockCopy(bytes, dataOffset, trnsChunkData, 0, length);
            }
            else if (type == "fcTL")
            {
                currentFcTl = new FcTlData
                {
                    Width = ReadInt32BE(bytes, dataOffset + 4),
                    Height = ReadInt32BE(bytes, dataOffset + 8),
                    XOffset = ReadInt32BE(bytes, dataOffset + 12),
                    YOffset = ReadInt32BE(bytes, dataOffset + 16),
                    DelayNum = ReadUInt16BE(bytes, dataOffset + 20),
                    DelayDen = ReadUInt16BE(bytes, dataOffset + 22),
                    DisposeOp = bytes[dataOffset + 24],
                    BlendOp = bytes[dataOffset + 25]
                };
                if (currentFcTl.DelayDen == 0) currentFcTl.DelayDen = 100;

                fcTls.Add(currentFcTl);
                frameIdats.Add(new List<byte[]>());
            }
            else if (type == "IDAT")
            {
                if (frameIdats.Count > 0)
                {
                    byte[] chunk = new byte[length];
                    Buffer.BlockCopy(bytes, dataOffset, chunk, 0, length);
                    frameIdats[frameIdats.Count - 1].Add(chunk);
                }
                else
                {
                    byte[] chunk = new byte[length];
                    Buffer.BlockCopy(bytes, dataOffset, chunk, 0, length);
                    var fallbackFcTl = new FcTlData
                    {
                        Width = width,
                        Height = height,
                        XOffset = 0,
                        YOffset = 0,
                        DelayNum = 100,
                        DelayDen = 1000,
                        DisposeOp = 0,
                        BlendOp = 0
                    };
                    fcTls.Add(fallbackFcTl);
                    frameIdats.Add(new List<byte[]> { chunk });
                }
            }
            else if (type == "fdAT")
            {
                if (frameIdats.Count > 0 && length > 4)
                {
                    byte[] chunk = new byte[length - 4];
                    Buffer.BlockCopy(bytes, dataOffset + 4, chunk, 0, length - 4);
                    frameIdats[frameIdats.Count - 1].Add(chunk);
                }
            }
        }

        if (fcTls.Count == 0 || ihdrChunkData == null) return null;

        var resultFrames = new List<ApngFrame>();

        for (int i = 0; i < fcTls.Count; i++)
        {
            var fctl = fcTls[i];
            var idatParts = frameIdats[i];

            if (idatParts.Count == 0) continue;

            byte[] framePngBytes = BuildSinglePng(ihdrChunkData, plteChunkData, trnsChunkData, fctl.Width, fctl.Height, idatParts);
            int delayMs = (int)((fctl.DelayNum * 1000.0) / fctl.DelayDen);
            resultFrames.Add(new ApngFrame { PngBytes = framePngBytes, DelayMs = delayMs });
        }

        return resultFrames;
    }

    private static byte[] BuildSinglePng(byte[] origIhdr, byte[]? plte, byte[]? trns, int width, int height, List<byte[]> idatParts)
    {
        using var ms = new MemoryStream();
        ms.Write(PngHeader, 0, 8);

        byte[] ihdr = new byte[origIhdr.Length];
        Buffer.BlockCopy(origIhdr, 0, ihdr, 0, origIhdr.Length);
        WriteInt32BE(ihdr, 0, width);
        WriteInt32BE(ihdr, 4, height);

        WriteChunk(ms, "IHDR", ihdr);

        if (plte != null) WriteChunk(ms, "PLTE", plte);
        if (trns != null) WriteChunk(ms, "tRNS", trns);

        foreach (var idat in idatParts)
        {
            WriteChunk(ms, "IDAT", idat);
        }

        WriteChunk(ms, "IEND", Array.Empty<byte>());
        return ms.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        byte[] lenBytes = new byte[4];
        WriteInt32BE(lenBytes, 0, data.Length);
        stream.Write(lenBytes, 0, 4);

        byte[] typeBytes = Encoding.ASCII.GetBytes(type);
        stream.Write(typeBytes, 0, 4);

        if (data.Length > 0)
            stream.Write(data, 0, data.Length);

        uint crc = Crc32(typeBytes, data);
        byte[] crcBytes = new byte[4];
        WriteUInt32BE(crcBytes, 0, crc);
        stream.Write(crcBytes, 0, 4);
    }

    private static int ReadInt32BE(byte[] b, int offset) =>
        (b[offset] << 24) | (b[offset + 1] << 16) | (b[offset + 2] << 8) | b[offset + 3];

    private static ushort ReadUInt16BE(byte[] b, int offset) =>
        (ushort)((b[offset] << 8) | b[offset + 1]);

    private static void WriteInt32BE(byte[] b, int offset, int val)
    {
        b[offset] = (byte)((val >> 24) & 0xFF);
        b[offset + 1] = (byte)((val >> 16) & 0xFF);
        b[offset + 2] = (byte)((val >> 8) & 0xFF);
        b[offset + 3] = (byte)(val & 0xFF);
    }

    private static void WriteUInt32BE(byte[] b, int offset, uint val)
    {
        b[offset] = (byte)((val >> 24) & 0xFF);
        b[offset + 1] = (byte)((val >> 16) & 0xFF);
        b[offset + 2] = (byte)((val >> 8) & 0xFF);
        b[offset + 3] = (byte)(val & 0xFF);
    }

    private static readonly uint[] CrcTable = InitializeCrcTable();
    private static uint[] InitializeCrcTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint c = i;
            for (int k = 0; k < 8; k++)
            {
                if ((c & 1) != 0)
                    c = 0xedb88320 ^ (c >> 1);
                else
                    c >>= 1;
            }
            table[i] = c;
        }
        return table;
    }

    private static uint Crc32(byte[] type, byte[] data)
    {
        uint crc = 0xffffffff;
        foreach (byte b in type)
            crc = CrcTable[(crc ^ b) & 0xff] ^ (crc >> 8);
        foreach (byte b in data)
            crc = CrcTable[(crc ^ b) & 0xff] ^ (crc >> 8);
        return crc ^ 0xffffffff;
    }

    private class FcTlData
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int XOffset { get; set; }
        public int YOffset { get; set; }
        public ushort DelayNum { get; set; }
        public ushort DelayDen { get; set; }
        public byte DisposeOp { get; set; }
        public byte BlendOp { get; set; }
    }
}
