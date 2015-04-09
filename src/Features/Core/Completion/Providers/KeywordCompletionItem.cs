// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal class KeywordCompletionItem : CompletionItem
    {
        public KeywordCompletionItem(ICompletionProvider completionProvider, string displayText, TextSpan filterSpan, Func<CancellationToken, Task<ImmutableArray<SymbolDisplayPart>>> descriptionFactory, Glyph? glyph, bool isIntrinsic)
            : base(completionProvider, displayText, filterSpan, descriptionFactory, glyph)
        {
            this.IsIntrinsic = isIntrinsic;
        }

        public bool IsIntrinsic { get; }
    }
}
