// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.InlineHints
{
    internal readonly struct InlineTypeHint
    {
        public readonly ITypeSymbol Type;
        public readonly int Position;

        public InlineTypeHint(
            ITypeSymbol type,
            int position)
        {
            Type = type;
            Position = position;
        }
    }
}
