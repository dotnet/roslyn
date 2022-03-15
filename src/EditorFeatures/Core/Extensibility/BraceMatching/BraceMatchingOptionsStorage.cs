// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor;

internal static class BraceMatchingOptionsStorage
{
    public static BraceMatchingOptions GetBraceMatchingOptions(this IGlobalOptionService globalOptions, string language)
        => new(HighlightingOptions: globalOptions.GetHighlightingOptions(language));
}
