using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using Windows.UI;

namespace FluentDeck.Models
{
    public class AppItem
    {
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
        public string Indicator { get; set; } = "WDM";
        public string LogoUrl { get; set; } = "";

        public string DisplayLogoUrl
        {
            get
            {
                if (string.IsNullOrWhiteSpace(LogoUrl) || LogoUrl == "nan")
                    return "ms-appx:///Assets/StoreLogo.scale-200.png";

                if (LogoUrl.StartsWith("/assets/", StringComparison.OrdinalIgnoreCase))
                    return "ms-appx:///Assets/" + LogoUrl.Substring(8);

                if (LogoUrl.StartsWith("assets/", StringComparison.OrdinalIgnoreCase))
                    return "ms-appx:///" + LogoUrl;

                return LogoUrl;
            }
        }

        public bool IsFoss { get; set; }
        public bool IsPaid { get; set; }
        public bool IsPlanned { get; set; }
        public bool IsDiscontinued { get; set; }
        public bool IsTheme { get; set; }

        public Uri NavigateUri => Uri.TryCreate(Url, UriKind.Absolute, out var uri) ? uri : new Uri("about:blank");

        /// <summary>True when the app has a non-empty, non-placeholder logo URL.</summary>
        public bool HasLogo => !string.IsNullOrWhiteSpace(LogoUrl) && LogoUrl != "nan";

        private static readonly string[] FallbackColors = new string[]
        {
            "#E81123", "#0078D7", "#107C41", "#603CBA", "#FF8C00",
            "#00B7C3", "#B4009E", "#00CC6A", "#7A7574", "#D83B01",
            "#8764B8", "#00188F", "#008272", "#10893E", "#FFB900"
        };

        public string FirstLetter => string.IsNullOrWhiteSpace(Name) ? "?" : Name.TrimStart().Substring(0, 1).ToUpper();

        public Microsoft.UI.Xaml.Media.Brush FallbackBrush
        {
            get
            {
                string hex = FallbackColors[Math.Abs(Name.GetHashCode()) % FallbackColors.Length];
                hex = hex.Replace("#", "");
                byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                return new SolidColorBrush(Color.FromArgb(255, r, g, b));
            }
        }

        private Microsoft.UI.Xaml.Media.ImageSource? _logoSource;
        public Microsoft.UI.Xaml.Media.ImageSource? LogoSource
        {
            get
            {
                if (_logoSource == null && !string.IsNullOrWhiteSpace(DisplayLogoUrl))
                {
                    if (Uri.TryCreate(DisplayLogoUrl, UriKind.Absolute, out var uri))
                    {
                        if (DisplayLogoUrl.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                        {
                            _logoSource = new Microsoft.UI.Xaml.Media.Imaging.SvgImageSource(uri);
                        }
                        else
                        {
                            _logoSource = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(uri)
                            {
                                DecodePixelWidth = 180,
                                DecodePixelType = Microsoft.UI.Xaml.Media.Imaging.DecodePixelType.Logical
                            };
                        }
                    }
                }
                return _logoSource;
            }
        }

        public static T? ParseAppLine<T>(string line, Dictionary<string, string>? logoFallbackCache = null) where T : AppItem, new()
        {
            string indicator = "WDM";
            int firstTick = line.IndexOf('`');
            if (firstTick != -1)
            {
                int secondTick = line.IndexOf('`', firstTick + 1);
                if (secondTick != -1)
                    indicator = line.Substring(firstTick + 1, secondTick - firstTick - 1);
            }

            int openBracket = line.IndexOf('[');
            int linkSeparator = line.IndexOf("](");

            if (openBracket == -1 || linkSeparator == -1 || linkSeparator < openBracket)
                return null;

            int closeParen = line.IndexOf(')', linkSeparator + 2);
            if (closeParen == -1)
                return null;

            string name = line.Substring(openBracket + 1, linkSeparator - openBracket - 1);
            string url = line.Substring(linkSeparator + 2, closeParen - linkSeparator - 2);

            bool isFoss = line.Contains("FOSS", StringComparison.OrdinalIgnoreCase);
            bool isPaid = line.Contains("💰");
            bool isPlanned = line.Contains("Planned", StringComparison.OrdinalIgnoreCase) || line.Contains("📆");
            bool isDiscontinued = line.Contains("Discontinued", StringComparison.OrdinalIgnoreCase) || line.Contains('❎');

            string logoUrl = "";
            int logoCommentStart = line.IndexOf("<!-- logo:");
            if (logoCommentStart != -1)
            {
                int logoCommentEnd = line.IndexOf("-->", logoCommentStart);
                if (logoCommentEnd != -1)
                {
                    logoUrl = line.Substring(logoCommentStart + 10, logoCommentEnd - logoCommentStart - 10).Trim();
                    if (logoUrl == "nan") logoUrl = "";
                }
            }
            else if (logoFallbackCache != null)
            {
                if (logoFallbackCache.TryGetValue(url.Trim(), out string? cachedLogo))
                    logoUrl = cachedLogo;
            }

            return new T
            {
                Name = name,
                Url = url,
                Indicator = indicator,
                IsFoss = isFoss,
                IsPaid = isPaid,
                IsPlanned = isPlanned,
                IsDiscontinued = isDiscontinued,
                LogoUrl = logoUrl
            };
        }
    }
}
