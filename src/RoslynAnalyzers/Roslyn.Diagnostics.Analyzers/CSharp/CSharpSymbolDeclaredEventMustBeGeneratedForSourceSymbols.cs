// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable warnings

using System.Collections.Immutable;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Diagnostics.Analyzers;

namespace Roslyn.Diagnostics.CSharp.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpSymbolDeclaredEventAnalyzer : SymbolDeclaredEventAnalyzer<SyntaxKind>
    {
        private static readonly ImmutableHashSet<string> s_symbolTypesWithExpectedSymbolDeclaredEvent = ImmutableHashSet.Create(
            "SourceNamespaceSymbol",
            "SourceNamedTypeSymbol",
            "SourceEventSymbol",
            "SourceFieldSymbol",
            "SourceMethodSymbol",
            "SourcePropertySymbol");

        protected override CompilationAnalyzer? GetCompilationAnalyzer(Compilation compilation, INamedTypeSymbol symbolType)
        {
            INamedTypeSymbol? compilationType = compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisCSharpCSharpCompilation);
            if (compilationType == null)
            {
                return null;
            }

            return new CSharpCompilationAnalyzer(symbolType, compilationType);
        }

        protected override SyntaxKind InvocationExpressionSyntaxKind => SyntaxKind.InvocationExpression;

        private sealed class CSharpCompilationAnalyzer : CompilationAnalyzer
        {
            public CSharpCompilationAnalyzer(INamedTypeSymbol symbolType, INamedTypeSymbol compilationType)
                : base(symbolType, compilationType)
            { }

            protected override ImmutableHashSet<string> SymbolTypesWithExpectedSymbolDeclaredEvent => s_symbolTypesWithExpectedSymbolDeclaredEvent;

            protected override SyntaxNode? GetFirstArgumentOfInvocation(SyntaxNode invocation)
            {
                var invocationExpression = (InvocationExpressionSyntax)invocation;
                if (invocationExpression.ArgumentList != null)
                {
                    ArgumentSyntax argument = invocationExpression.ArgumentList.Arguments.FirstOrDefault();
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
