// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.CodeAnalysis.Utilities
{
    internal class BrushToColorConverter : ValueConverter<Brush, Color>
    {
        protected override Color Convert(Brush brush, object parameter, CultureInfo culture)
            => brush switch
            {
                SolidColorBrush solidColorBrush => solidColorBrush.Color,
                GradientBrush gradientBrush => gradientBrush.GradientStops.FirstOrDefault()?.Color ?? Colors.Transparent,
                _ => Colors.Transparent
            };
    }
}
