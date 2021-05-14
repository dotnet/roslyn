// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Information about the arguments of an implicit or explicit Index or Range indexer pattern used in list patterns.
    /// </summary>
    internal sealed class IndexerArgumentInfo
    {
        public readonly Symbol Symbol;
        public readonly ImmutableArray<BoundExpression> ArgumentsOpt;
        public readonly bool Expanded;

        public IndexerArgumentInfo(Symbol symbol, ImmutableArray<BoundExpression> argumentsOpt = default, bool expanded = default)
        {
            Debug.Assert(
                symbol is PropertySymbol { IsIndexer: true } ||
                symbol is MethodSymbol { Name: WellKnownMemberNames.SliceMethodName or nameof(string.Substring) } && argumentsOpt.IsDefault);

            Symbol = symbol;
            ArgumentsOpt = argumentsOpt;
            Expanded = expanded;
        }
    }
}
