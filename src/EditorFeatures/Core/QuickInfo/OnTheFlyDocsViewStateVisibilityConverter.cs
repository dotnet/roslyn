// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo;

/// <summary>
/// Converts the <see cref="OnTheFlyDocsState"/> of the view to a <see cref="Visibility"/> value.
/// </summary>
internal sealed class OnTheFlyDocsViewStateVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is OnTheFlyDocsState state && parameter is OnTheFlyDocsState targetState && state == targetState ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
