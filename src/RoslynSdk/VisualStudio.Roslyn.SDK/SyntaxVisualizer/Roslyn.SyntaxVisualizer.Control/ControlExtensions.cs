using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Roslyn.SyntaxVisualizer.Control
{
    internal static class ControlExtensions
    {
        // for now, keep unused parameter to ease migration into dotnet/roslyn
        // post-migration, we can fixup this unused param
        // https://github.com/dotnet/roslyn/issues/84573
        internal static Point Clamp(this Point _, FrameworkElement element)
        {
            var pos = Mouse.GetPosition(element);
            pos.X = Math.Min(Math.Max(0, pos.X), element.ActualWidth);
            pos.Y = Math.Min(Math.Max(0, pos.Y), element.ActualHeight);
            return pos;
        }

        internal static Color GetColorAtOffset(this GradientStopCollection collection, double offset)
        {
            var stops = collection.OrderBy(x => x.Offset).ToArray();
            if (offset <= 0)
            {
                return stops[0].Color;
            }
            else if (offset >= 1)
            {
                return stops[stops.Length - 1].Color;
            }

            var left = stops[0];
            GradientStop? right = null;

            foreach (var stop in stops)
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

            var percent = Math.Round((offset - left.Offset) / (right.Offset - left.Offset), 3);
            var a = (byte)((right.Color.A - left.Color.A) * percent + left.Color.A);
            var r = (byte)((right.Color.R - left.Color.R) * percent + left.Color.R);
            var g = (byte)((right.Color.G - left.Color.G) * percent + left.Color.G);
            var b = (byte)((right.Color.B - left.Color.B) * percent + left.Color.B);
            return Color.FromArgb(a, r, g, b);
        }
    }
}
