// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.EditorConfig.Features
{
    internal sealed class EditorConfigQuickInfo
    {
        public TextSpan Span { get; }

        public IEnumerable<TaggedText> Description { get; }

        public ISymbol Symbol { get; }

        private EditorConfigQuickInfo(
            TextSpan span,
            IEnumerable<TaggedText> description,
            ISymbol symbol)
        {
            Span = span;
            Description = description;
            Symbol = symbol;
        }

        public static EditorConfigQuickInfo Create(
            TextSpan span,
            IEnumerable<TaggedText> description,
            ISymbol symbol = null)
        {
            return new EditorConfigQuickInfo(span, description, symbol);
        }
    }
}
