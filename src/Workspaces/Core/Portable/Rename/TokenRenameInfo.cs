// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename
{
    internal sealed class TokenRenameInfo
    {
        public bool HasSymbols { get; private set; }
        public IEnumerable<ISymbol> Symbols { get; private set; }
        public bool IsMemberGroup { get; private set; }

        public static TokenRenameInfo CreateMemberGroupTokenInfo(IEnumerable<ISymbol> symbols)
        {
            return new TokenRenameInfo
            {
                HasSymbols = true,
                IsMemberGroup = true,
                Symbols = symbols
            };
        }

        public static TokenRenameInfo CreateSingleSymbolTokenInfo(ISymbol symbol)
        {
            return new TokenRenameInfo
            {
                HasSymbols = true,
                IsMemberGroup = false,
                Symbols = SpecializedCollections.SingletonEnumerable(symbol)
            };
        }

        public static TokenRenameInfo NoSymbolsTokenInfo = new TokenRenameInfo
        {
            HasSymbols = false,
            IsMemberGroup = false,
            Symbols = SpecializedCollections.EmptyEnumerable<ISymbol>()
        };
    }
}
