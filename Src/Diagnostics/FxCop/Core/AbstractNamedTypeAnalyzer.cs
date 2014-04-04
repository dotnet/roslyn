// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers
{
    public abstract class AbstractNamedTypeAnalyzer : ISymbolAnalyzer
    {
        public ImmutableArray<SymbolKind> SymbolKindsOfInterest
        {
            get
            {
                return ImmutableArray.Create(SymbolKind.NamedType);
            }
        }

        public abstract ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

        public abstract void AnalyzeSymbol(INamedTypeSymbol symbol, Compilation compilation, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken);

        void ISymbolAnalyzer.AnalyzeSymbol(ISymbol symbol, Compilation compilation, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
        {
            AnalyzeSymbol((INamedTypeSymbol)symbol, compilation, addDiagnostic, cancellationToken);
        }
    }
}
