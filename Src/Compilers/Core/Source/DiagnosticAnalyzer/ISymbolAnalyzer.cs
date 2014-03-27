// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// An analyzer that is invoked on each declared symbol in the compilation.
    /// </summary>
    public interface ISymbolAnalyzer : IDiagnosticAnalyzer
    {
        /// <summary>
        /// Returns the set of symbol kinds for which <see cref="AnalyzeSymbol"/> should be called.
        /// </summary>
        ImmutableArray<SymbolKind> SymbolKindsOfInterest { get; }
        /// <summary>
        /// Called for each declared symbol in the compilation where the symbol's kind is an element of <see cref="SymbolKindsOfInterest"/>.
        /// </summary>
        /// <param name="symbol">The declared symbol</param>
        /// <param name="compilation">The compilation in which the symbol is declared</param>
        /// <param name="addDiagnostic">A delegate to be used to emit diagnostics</param>
        /// <param name="cancellationToken">A token for cancelling the computation</param>
        void AnalyzeSymbol(ISymbol symbol, Compilation compilation, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken);
    }
}
