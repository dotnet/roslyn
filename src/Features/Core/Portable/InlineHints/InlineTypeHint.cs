// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.InlineHints
{
    internal readonly struct InlineHint
    {
        public readonly TextSpan Span;
        public readonly ImmutableArray<SymbolDisplayPart> Parts;
        public readonly SymbolKey? SymbolKey;

        public InlineHint(TextSpan span, ImmutableArray<SymbolDisplayPart> parts, SymbolKey? symbolKey)
        {
            Span = span;
            Parts = parts;
            SymbolKey = symbolKey;
        }
    }
}
