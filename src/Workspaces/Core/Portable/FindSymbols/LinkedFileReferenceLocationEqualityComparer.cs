// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols;

/// <summary>
/// Helper comparer to enable consumers of <see cref="SymbolFinder.FindReferencesAsync(ISymbol, Solution,
/// CancellationToken)"/> to process references found in linked files only a single time.
/// </summary>
internal sealed class LinkedFileReferenceLocationEqualityComparer : IEqualityComparer<ReferenceLocation>
{
    public static readonly IEqualityComparer<ReferenceLocation> Instance = new LinkedFileReferenceLocationEqualityComparer();

    private LinkedFileReferenceLocationEqualityComparer()
    {
    }

    public bool Equals(ReferenceLocation x, ReferenceLocation y)
    {
        Contract.ThrowIfFalse(x.Document == y.Document);
        return x.Location.SourceSpan == y.Location.SourceSpan;
    }

    public int GetHashCode(ReferenceLocation obj)
        => obj.Location.SourceSpan.GetHashCode();
}
