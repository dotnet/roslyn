// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.InlineHints
{
    internal readonly struct InlineParameterHint
    {
        public readonly SymbolKey ParameterSymbolKey;
        public readonly string Name;
        public readonly int Position;
        public readonly InlineParameterHintKind Kind;

        public InlineParameterHint(SymbolKey parameterSymbolKey, string name, int position, InlineParameterHintKind kind)
        {
            ParameterSymbolKey = parameterSymbolKey;
            Name = name;
            Position = position;
            Kind = kind;
        }
    }
}
