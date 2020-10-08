// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.InlineHints
{
    /// <summary>
    /// The simple tag that only holds information regarding the associated parameter name
    /// for the argument
    /// </summary>
    internal class InlineHintDataTag : ITag
    {
        public readonly ImmutableArray<SymbolDisplayPart> Parts;
        public readonly SymbolKey? SymbolKey;

        public InlineHintDataTag(ImmutableArray<SymbolDisplayPart> parts, SymbolKey? symbolKey)
        {
            if (parts.Length == 0)
                throw new ArgumentException("Must have a length greater than 0", nameof(parts));

            Parts = parts;
            SymbolKey = symbolKey;
        }
    }
}
