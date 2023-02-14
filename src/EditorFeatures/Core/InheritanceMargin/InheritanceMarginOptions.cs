// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.InheritanceMargin
{
    internal static class InheritanceMarginOptions
    {
        public static readonly PerLanguageOption2<bool?> ShowInheritanceMargin = new("dotnet_inheritance_margin_options_show_inheritance_margin", defaultValue: true);

        public static readonly Option2<bool> InheritanceMarginCombinedWithIndicatorMargin = new("dotnet_inheritance_margin_options_inheritance_margin_combined_with_indicator_margin", defaultValue: false);

        public static readonly PerLanguageOption2<bool> InheritanceMarginIncludeGlobalImports = new("dotnet_inheritance_margin_options_inheritance_margin_include_global_imports", defaultValue: true);
    }
}
