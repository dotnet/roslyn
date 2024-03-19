// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.InheritanceMargin;

internal static class InheritanceMarginOptionsStorage
{
    public static readonly PerLanguageOption2<bool?> ShowInheritanceMargin = new("dotnet_show_inheritance_margin", defaultValue: true);

    public static readonly Option2<bool> InheritanceMarginCombinedWithIndicatorMargin = new("dotnet_combine_inheritance_and_indicator_margins", defaultValue: false);

    public static readonly PerLanguageOption2<bool> InheritanceMarginIncludeGlobalImports = new("dotnet_show_global_imports_in_inheritance_margin", defaultValue: true);
}
