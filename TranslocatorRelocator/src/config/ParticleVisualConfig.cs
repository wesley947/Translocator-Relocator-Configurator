using System;
using System.Globalization;

namespace TranslocatorRelocator.src.config
{
    // Client-only appearance preferences; never stored in world/network data.
    public class ParticleVisualConfig
    {
        public const string FileName = "translocatorconfigurator-particles.json";
        public const string DefaultNormal = "#16A064";
        public const string DefaultGoHome = "#FF3C24";
        public const string DefaultHome = "#6E37DC";

        // Legacy field retained for migrating old toggle-based settings.
        public bool UseDefaults = false;
        public string Normal = DefaultNormal;
        public string GoHome = DefaultGoHome;
        public string NetworkHome = DefaultHome;
        public int ParticleFlow = 100;

        public int EffectiveFlow => Math.Clamp(ParticleFlow, 60, 140);

        public string GetColor(int mode)
        {
            return mode switch
            {
                1 => ValidateHex(GoHome, DefaultGoHome),
                2 => ValidateHex(NetworkHome, DefaultHome),
                _ => ValidateHex(Normal, DefaultNormal)
            };
        }

        public string GetCustomColor(int mode)
        {
            return mode switch
            {
                1 => GoHome,
                2 => NetworkHome,
                _ => Normal
            };
        }

        public void SetCustomColor(int mode, string color)
        {
            color = ValidateHex(color, DefaultFor(mode));
            switch (mode)
            {
                case 1: GoHome = color; break;
                case 2: NetworkHome = color; break;
                default: Normal = color; break;
            }
        }

        public int[] ActiveRgb(int mode)
        {
            return ParseRgb(GetColor(mode));
        }

        public void ResetToDefaults()
        {
            Normal = DefaultNormal;
            GoHome = DefaultGoHome;
            NetworkHome = DefaultHome;
            ParticleFlow = 100;
            UseDefaults = false;
        }

        public void Sanitize()
        {
            // A previous version used UseDefaults as a persistent editing lock.
            // Convert that state to the same actual values as the new Reset button.
            if (UseDefaults) ResetToDefaults();
            Normal = ValidateHex(Normal, DefaultNormal);
            GoHome = ValidateHex(GoHome, DefaultGoHome);
            NetworkHome = ValidateHex(NetworkHome, DefaultHome);
            ParticleFlow = Math.Clamp(ParticleFlow, 60, 140);
        }

        public static string DefaultFor(int mode)
        {
            return mode switch { 1 => DefaultGoHome, 2 => DefaultHome, _ => DefaultNormal };
        }

        public static string ValidateHex(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            string text = value.Trim().TrimStart('#');
            return text.Length == 6 &&
                   int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _)
                ? "#" + text.ToUpperInvariant() : fallback;
        }

        public static int[] ParseRgb(string hex)
        {
            string text = ValidateHex(hex, DefaultNormal).Substring(1);
            return new[]
            {
                int.Parse(text.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                int.Parse(text.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                int.Parse(text.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
            };
        }

        public static string HsvHex(double hue, double saturation, double value)
        {
            hue = ((hue % 360) + 360) % 360;
            saturation = Math.Clamp(saturation, 0, 1);
            value = Math.Clamp(value, 0, 1);
            double c = value * saturation;
            double x = c * (1 - Math.Abs((hue / 60) % 2 - 1));
            double m = value - c;
            double r = 0, g = 0, b = 0;
            if (hue < 60) { r = c; g = x; }
            else if (hue < 120) { r = x; g = c; }
            else if (hue < 180) { g = c; b = x; }
            else if (hue < 240) { g = x; b = c; }
            else if (hue < 300) { r = x; b = c; }
            else { r = c; b = x; }
            return $"#{(int)Math.Round((r + m) * 255):X2}{(int)Math.Round((g + m) * 255):X2}{(int)Math.Round((b + m) * 255):X2}";
        }

        public static void RgbToHsv(string hex, out double hue, out double saturation, out double value)
        {
            int[] rgb = ParseRgb(hex);
            double r = rgb[0] / 255d, g = rgb[1] / 255d, b = rgb[2] / 255d;
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double diff = max - min;
            value = max;
            saturation = max <= 0 ? 0 : diff / max;
            hue = 0;
            if (diff <= 0) return;
            if (max == r) hue = 60 * (((g - b) / diff) % 6);
            else if (max == g) hue = 60 * ((b - r) / diff + 2);
            else hue = 60 * ((r - g) / diff + 4);
            if (hue < 0) hue += 360;
        }
    }
}
