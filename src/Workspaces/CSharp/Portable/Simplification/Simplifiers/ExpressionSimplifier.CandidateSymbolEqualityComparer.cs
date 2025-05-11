// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp.Simplification.Simplifiers;

internal partial class ExpressionSimplifier
{
    /// <summary>
    /// Compares symbols by their original definition.
    /// </summary>
    private sealed class CandidateSymbolEqualityComparer : IEqualityComparer<ISymbol>
    {
        public static CandidateSymbolEqualityComparer Instance { get; } = new CandidateSymbolEqualityComparer();

        private CandidateSymbolEqualityComparer()
        {
        }

        public bool Equals(ISymbol x, ISymbol y)
        {
            if (x is null || y is null)
            {
                return x == y;
            }

            return x.OriginalDefinition.Equals(y.OriginalDefinition);
        }

        public int GetHashCode(ISymbol obj)
            => obj?.OriginalDefinition.GetHashCode() ?? 0;
    }
}
