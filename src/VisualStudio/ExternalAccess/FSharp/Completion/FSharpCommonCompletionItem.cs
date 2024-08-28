// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Completion
{
    internal static class FSharpCommonCompletionItem
    {
        public static CompletionItem Create(
            string displayText,
            string displayTextSuffix,
            CompletionItemRules rules,
            FSharpGlyph? glyph = null,
            ImmutableArray<SymbolDisplayPart> description = default,
            string sortText = null,
            string filterText = null,
            bool showsWarningIcon = false,
            ImmutableDictionary<string, string> properties = null,
            ImmutableArray<string> tags = default,
            string inlineDescription = null)
        {
            var roslynGlyph = glyph.HasValue ? FSharpGlyphHelpers.ConvertTo(glyph.Value) : (Glyph?)null;
            return CommonCompletionItem.Create(
                displayText, displayTextSuffix, rules, roslynGlyph, description, sortText, filterText, showsWarningIcon, properties.AsImmutableOrNull(), tags, inlineDescription);
        }
    }
}
