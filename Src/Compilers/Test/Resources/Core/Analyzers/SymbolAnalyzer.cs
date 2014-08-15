// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// Build with csc.exe /t:library /out:SymbolAnalyzer.dll /r:<binaries directory>\Microsoft.CodeAnalysis.dll /r:<binaries directory>\System.Collections.Immutable /r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\mscorlib.dll" SymbolAnalyzer.cs

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer]
class MyAnalyzer : ISymbolAnalyzer
{
    internal static readonly long loadTime = DateTime.Now.Ticks;
    internal static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor("MyAnalyzer01", string.Empty, "Analyzer loaded at: {0}", string.Empty, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get { return ImmutableArray.Create(descriptor); }
    }

    public ImmutableArray<SymbolKind> SymbolKindsOfInterest
    {
        get { return ImmutableArray.Create(SymbolKind.NamedType); }
    }

    public void AnalyzeSymbol(ISymbol symbol, Compilation compilation, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
    {
        addDiagnostic(Diagnostic.Create(descriptor, symbol.Locations.First(), loadTime));
    }
}