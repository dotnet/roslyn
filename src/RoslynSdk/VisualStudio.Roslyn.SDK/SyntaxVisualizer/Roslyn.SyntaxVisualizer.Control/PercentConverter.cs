// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Windows.Data;

namespace Roslyn.SyntaxVisualizer.Control
{
    public class PercentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (int)((double)value * 100);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => (int)value / 100.0d;
    }
}
