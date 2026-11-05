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

        // Separate shade controls for each state; 60% reproduces the original color.
        public int NormalBrightness = 60;
        public int GoHomeBrightness = 60;
        public int HomeBrightness = 60;

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

        public int GetBrightness(int mode)
        {
            return mode switch
            {
                1 => GoHomeBrightness,
                2 => HomeBrightness,
                _ => NormalBrightness
            };
        }

        public void SetBrightness(int mode, int brightness)
        {
            brightness = ClampBrightness(brightness);
            switch (mode)
            {
                case 1: GoHomeBrightness = brightness; break;
                case 2: HomeBrightness = brightness; break;
                default: NormalBrightness = brightness; break;
            }
        }

        // This transforms particle emission RGB only. The saved swatch color remains
        // unchanged so its selection highlight always matches the base color.
        public int[] ActiveRgb(int mode)
        {
            int[] rgb = ParseRgb(GetColor(mode));
            int brightness = ClampBrightness(GetBrightness(mode));
            if (brightness == 60) return rgb;

            if (brightness < 60)
            {
                // Minimum at 30% is half of the original RGB: dark, but not black.
                double scale = 0.5 + (brightness - 30) / 60d;
                for (int i = 0; i < 3; i++)
                    rgb[i] = (int)Math.Round(rgb[i] * scale);
            }
            else
            {
                // At 100%, blend 58% toward white for a visible pastel shade.
                // Values never whiten out entirely unless the base swatch is white.
                double whiteMix = 0.58 * (brightness - 60) / 40d;
                for (int i = 0; i < 3; i++)
                    rgb[i] = (int)Math.Round(rgb[i] + (255 - rgb[i]) * whiteMix);
            }
            return rgb;
        }

        private static int ClampBrightness(int brightness)
        {
            return Math.Clamp(30 + (int)Math.Round((Math.Clamp(brightness, 30, 100) - 30) / 5d) * 5, 30, 100);
        }

        public void ResetToDefaults()
        {
            Normal = DefaultNormal;
            GoHome = DefaultGoHome;
            NetworkHome = DefaultHome;
            ParticleFlow = 100;
            NormalBrightness = 60;
            GoHomeBrightness = 60;
            HomeBrightness = 60;
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
            NormalBrightness = ClampBrightness(NormalBrightness);
            GoHomeBrightness = ClampBrightness(GoHomeBrightness);
            HomeBrightness = ClampBrightness(HomeBrightness);
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
