// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Snippets
{
    internal readonly struct RoslynLSPSnippetItem
    {
        public readonly string? Identifier;
        public readonly int Priority;
        public readonly int? CaretPosition;
        public readonly ImmutableArray<TextSpan> PlaceHolderSpans;

        public RoslynLSPSnippetItem(string? identifier, int priority, int? caretPosition, ImmutableArray<TextSpan> placeholderSpans)
        {
            Identifier = identifier;
            Priority = priority;
            CaretPosition = caretPosition;
            PlaceHolderSpans = placeholderSpans;
        }
    }
}
