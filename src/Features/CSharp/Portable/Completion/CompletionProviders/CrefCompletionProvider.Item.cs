// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal partial class CrefCompletionProvider
    {
        private class Item : CompletionItem
        {
            public readonly string InsertionText;

            public Item(
                CompletionListProvider completionProvider,
                string displayText,
                string insertionText,
                TextSpan textSpan,
                Func<CancellationToken,
                Task<ImmutableArray<SymbolDisplayPart>>> descriptionFactory,
                Glyph? glyph,
                string sortText)
                : base(completionProvider, displayText, textSpan, descriptionFactory, glyph, sortText, rules: ItemRules.Instance)
            {
                this.InsertionText = insertionText;
            }
        }
    }
}
