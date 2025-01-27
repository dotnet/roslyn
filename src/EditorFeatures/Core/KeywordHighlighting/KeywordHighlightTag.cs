// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Editor.Shared.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Highlighting;

internal sealed class KeywordHighlightTag : NavigableHighlightTag
{
    internal const string TagId = "MarkerFormatDefinition/HighlightedReference";

    public static readonly KeywordHighlightTag Instance = new();

    private KeywordHighlightTag()
        : base(TagId)
    {
    }
}
