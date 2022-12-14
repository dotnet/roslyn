// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal readonly struct FindReferencesCandidate
    {
        /// <inheritdoc cref="SymbolInfo.Symbol"/>
        public readonly ISymbol? Symbol;

        /// <inheritdoc cref="SymbolInfo.CandidateSymbols"/>
        public readonly ImmutableArray<ISymbol> CandidateSymbols;

        /// <inheritdoc cref="SymbolInfo.CandidateReason"/>
        public readonly CandidateReason CandidateReason;
    }
}
