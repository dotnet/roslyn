// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Design;

namespace Microsoft.CodeAnalysis.CSharp.FxCopAnalyzers.Design
{
    [DiagnosticAnalyzer]
    [ExportDiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpCA1003DiagnosticAnalyzer : CA1003DiagnosticAnalyzer
    {
        protected override AnalyzerBase GetAnalyzer(
            Compilation compilation,
            INamedTypeSymbol eventHandler,
            INamedTypeSymbol genericEventHandler,
            INamedTypeSymbol eventArgs,
            INamedTypeSymbol comSourceInterfacesAttribute)
        {
            return new Analyzer(compilation, eventHandler, genericEventHandler, eventArgs, comSourceInterfacesAttribute);
        }

        private sealed class Analyzer : AnalyzerBase
        {
            public Analyzer(
                Compilation compilation,
                INamedTypeSymbol eventHandler,
                INamedTypeSymbol genericEventHandler,
                INamedTypeSymbol eventArgs,
                INamedTypeSymbol comSourceInterfacesAttribute)
                : base(compilation, eventHandler, genericEventHandler, eventArgs, comSourceInterfacesAttribute)
            {
            }

            protected override bool IsViolatingEventHandler(INamedTypeSymbol type)
            {
                return !IsValidLibraryEventHandlerInstance(type);
            }

            protected override bool IsAssignableTo(Compilation compilation, ITypeSymbol fromSymbol, ITypeSymbol toSymbol)
            {
                return
                    fromSymbol != null &&
                    toSymbol != null &&
                    ((CSharpCompilation)compilation).ClassifyConversion(fromSymbol, toSymbol).IsImplicit;
            }
        }
    }
}