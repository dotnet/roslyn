// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    internal abstract partial class CommonSemanticQuickInfoProvider
    {
        public struct TokenInformation
        {
            public readonly ImmutableArray<ISymbol> Symbols;

            /// <summary>
            /// True if this quick info came from hovering over an 'await' keyword, which we show the return
            /// type of with special text.
            /// </summary>
            public readonly bool ShowAwaitReturn;

            public TokenInformation(ImmutableArray<ISymbol> symbols, bool showAwaitReturn = false)
            {
                Symbols = symbols;
                ShowAwaitReturn = showAwaitReturn;
            }
        }
    }
}
