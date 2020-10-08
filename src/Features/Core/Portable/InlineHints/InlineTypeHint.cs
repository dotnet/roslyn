// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.InlineHints
{
    internal readonly struct InlineTypeHint
    {
        public readonly int Position;
        public readonly ImmutableArray<SymbolDisplayPart> Parts;
        public readonly SymbolKey SymbolKey;

        public InlineTypeHint(int position, ImmutableArray<SymbolDisplayPart> parts, SymbolKey symbolKey)
        {
            Position = position;
            Parts = parts;
            SymbolKey = symbolKey;
        }
    }
}
