// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal static class IBlockFactsExtensions
    {
        /// <summary>
        /// Gets the statement container node for the statement <paramref name="node"/>.
        /// </summary>
        /// <returns>The statement container for <paramref name="node"/>.</returns>
        public static SyntaxNode? GetStatementContainer(this IBlockFacts blockFacts, SyntaxNode node)
        {
            for (var current = node; current is not null; current = current.Parent)
            {
                if (blockFacts.IsStatementContainer(current.Parent))
                    return current.Parent;
            }

            return null;
        }
    }
}
