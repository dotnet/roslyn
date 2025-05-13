// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.CodeAnalysis;

internal interface IWpfThemeService
{
    void ApplyThemeToElement(FrameworkElement element);
    Color GetThemeColor(ThemeResourceKey resourceKey);
}
