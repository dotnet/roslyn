// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace System.Runtime.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpCA1003DiagnosticAnalyzer : UseGenericEventHandler
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
