// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public static string? From(ISymbol symbol)
        {
            var node = RQNodeBuilder.Build(symbol);

            if (node == null)
            {
                return null;
            }

            return ParenthesesTreeWriter.ToParenthesesFormat(node.ToSimpleTree());
        }
    }
}
