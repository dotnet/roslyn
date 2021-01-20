﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options.Converters
{
    internal class NullableBoolOptionConverter : IValueConverter
    {
        private readonly Func<bool> _onNullValue;
        public NullableBoolOptionConverter(Func<bool> onNullValue)
        {
            _onNullValue = onNullValue;
        }

        public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
            => value switch
            {
                null => _onNullValue(),
                bool b => b,
                _ => DependencyProperty.UnsetValue
            };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value;
    }
}
