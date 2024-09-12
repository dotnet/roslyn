// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ReferenceHighlighting;

internal static class ReferenceHighlightingOptionsStorage
{
    public static readonly PerLanguageOption2<bool> ReferenceHighlighting = new("dotnet_highlight_references", defaultValue: true);
}
