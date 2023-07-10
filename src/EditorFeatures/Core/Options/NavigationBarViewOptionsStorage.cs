// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Options;

internal sealed class NavigationBarViewOptionsStorage
{
    public static readonly PerLanguageOption2<bool> ShowNavigationBar = new(
        "dotnet_show_navigation_bar", defaultValue: true);
}
