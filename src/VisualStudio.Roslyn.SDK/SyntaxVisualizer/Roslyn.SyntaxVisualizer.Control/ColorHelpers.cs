using System;
using System.Windows.Media;

namespace Roslyn.SyntaxVisualizer.Control
{
    internal static class ColorHelpers
    {
        /// <summary>
        /// From https://en.wikipedia.org/wiki/HSL_and_HSV
        /// </summary> 
        internal static Color HSVToColor(double hue, double saturation, double value)
        {
            hue = Clamp(hue, 0, 360);
            saturation = Clamp(saturation, 0, 1);
            value = Clamp(value, 0, 1);

            byte f(double d)
            {
                var k = (d + hue / 60) % 6;
                var v = value - value * saturation * Math.Max(Math.Min(Math.Min(k, 4 - k), 1), 0);
                return (byte)Math.Round(v * 255);
            }

            return Color.FromRgb(f(5), f(3), f(1));
        }

        internal static double GetHue(Color color)
            => System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B).GetHue();

        internal static double GetBrightness(Color color)
            => System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B).GetBrightness();

        internal static double GetSaturation(Color color)
            => System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B).GetSaturation();

        private static double Clamp(double value, double min, double max)
            => Math.Max(min, Math.Min(value, max));
    }
}
