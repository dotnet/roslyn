// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Windows.Data;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Styles
{
    internal class FontSizeConverter : IValueConverter
    {
        // Scaling percentage. E.g. 122 means 122%.
        public int Scale { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => (double)value * Scale / 100.0;

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
}
