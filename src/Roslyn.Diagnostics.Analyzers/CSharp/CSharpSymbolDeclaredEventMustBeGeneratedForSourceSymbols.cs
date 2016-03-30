// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpSymbolDeclaredEventAnalyzer : SymbolDeclaredEventAnalyzer<SyntaxKind>
    {
        private static readonly HashSet<string> s_symbolTypesWithExpectedSymbolDeclaredEvent = new HashSet<string>(
            new[] { "SourceNamespaceSymbol", "SourceNamedTypeSymbol", "SourceEventSymbol", "SourceFieldSymbol", "SourceMethodSymbol", "SourcePropertySymbol" });

        protected override CompilationAnalyzer GetCompilationAnalyzer(Compilation compilation, INamedTypeSymbol symbolType)
        {
            INamedTypeSymbol compilationType = compilation.GetTypeByMetadataName(typeof(CSharpCompilation).FullName);
            if (compilationType == null)
            {
                return null;
            }

            return new CSharpCompilationAnalyzer(symbolType, compilationType);
        }

        protected override SyntaxKind InvocationExpressionSyntaxKind
        {
            get { return SyntaxKind.InvocationExpression; }
        }

        private sealed class CSharpCompilationAnalyzer : CompilationAnalyzer
        {
            public CSharpCompilationAnalyzer(INamedTypeSymbol symbolType, INamedTypeSymbol compilationType)
                : base(symbolType, compilationType)
            { }

            protected override HashSet<string> SymbolTypesWithExpectedSymbolDeclaredEvent
            {
                get
                {
                    return s_symbolTypesWithExpectedSymbolDeclaredEvent;
                }
            }

            protected override SyntaxNode GetFirstArgumentOfInvocation(SyntaxNode node)
            {
                var invocation = (InvocationExpressionSyntax)node;
                if (invocation.ArgumentList != null)
                {
                    ArgumentSyntax argument = invocation.ArgumentList.Arguments.FirstOrDefault();
                    if (argument != null)
                    {
                        return argument.Expression;
                    }
                }

                return null;
            }
        }
    }
}
