// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    /// <summary>
    /// Converter for bool properties from an enum value.
    /// Usage: 
    ///   BoolProperty="{Binding EnumProperty, Converter={StaticResource converter}, ConverterParameter={x:Static namespace:Enum.Value}}"
    /// </summary>
    internal class EnumBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value?.Equals(parameter) ?? DependencyProperty.UnsetValue;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value?.Equals(true) == true ? parameter : Binding.DoNothing;
    }
}
