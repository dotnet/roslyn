// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Windows;
using System.Windows.Data;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options.Converters
{
    public class MarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => new Thickness(System.Convert.ToDouble(value), 0, 0, 0);

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => throw new NotSupportedException();
    }
}
