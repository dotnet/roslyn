// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Features.RQName
{
    /// <summary>
    /// Helpers related to <see cref="RQName"/>s.
    /// </summary>
    internal static class RQNameInternal
    {
        /// <summary>
        /// Returns an RQName for the given symbol, or <see langword="null"/> if the symbol cannot be represented by an RQName.
        /// </summary>
        /// <param name="symbol">The symbol to build an RQName for.</param>
        public static string From(ISymbol symbol)
        {
            var node = RQNodeBuilder.Build(symbol);
            return ParenthesesTreeWriter.ToParenthesesFormat(node?.ToSimpleTree());
        }
    }
}
