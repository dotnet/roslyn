using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Roslyn.SyntaxVisualizer.Control
{
    internal static class ControlExtensions
    {
        internal static Point Clamp(this Point p, FrameworkElement element)
        {
            var pos = Mouse.GetPosition(element);
            pos.X = Math.Min(Math.Max(0, pos.X), element.ActualWidth);
            pos.Y = Math.Min(Math.Max(0, pos.Y), element.ActualHeight);
            return pos;
        }

        internal static Color GetColorAtOffset(this GradientStopCollection collection, double offset)
        {
            GradientStop[] stops = collection.OrderBy(x => x.Offset).ToArray();
            if (offset <= 0)
            {
                return stops[0].Color;
            }
            else if (offset >= 1)
            {
                return stops[stops.Length - 1].Color;
            }

            GradientStop left = stops[0];
            GradientStop? right = null;

            foreach (GradientStop stop in stops)
            {
                if (stop.Offset >= offset)
                {
                    right = stop;
                    break;
                }

                left = stop;
            }

            if (right is null)
            {
                return left.Color;
            }

            double percent = Math.Round((offset - left.Offset) / (right.Offset - left.Offset), 3);
            byte a = (byte)((right.Color.A - left.Color.A) * percent + left.Color.A);
            byte r = (byte)((right.Color.R - left.Color.R) * percent + left.Color.R);
            byte g = (byte)((right.Color.G - left.Color.G) * percent + left.Color.G);
            byte b = (byte)((right.Color.B - left.Color.B) * percent + left.Color.B);
            return Color.FromArgb(a, r, g, b);
        }
    }
}
