// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    internal class BrushToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SolidColorBrush solidColorBrush = value as SolidColorBrush;
            if (solidColorBrush != null)
            {
                return solidColorBrush.Color;
            }

            GradientBrush gradientBrush = value as GradientBrush;
            if (gradientBrush != null && gradientBrush.GradientStops.Count > 0)
            {
                return gradientBrush.GradientStops[0].Color;
            }

            if (value == null)
            {
                return Colors.Transparent;
            }

            throw new ArgumentException(nameof(value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
