// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                displayText, displayTextSuffix, rules, roslynGlyph, description, sortText, filterText, showsWarningIcon, properties, tags, inlineDescription);
        }
    }
}
