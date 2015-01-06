// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    public abstract partial class DiagnosticAnalyzerCorrectnessAnalyzer : DiagnosticAnalyzer
    {
        protected abstract class InvocationCompilationAnalyzer<TClassDeclarationSyntax, TInvocationExpressionSyntax> : CompilationAnalyzer
            where TClassDeclarationSyntax : SyntaxNode
            where TInvocationExpressionSyntax : SyntaxNode
        {
            protected InvocationCompilationAnalyzer(INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute)
                : base(diagnosticAnalyzer, diagnosticAnalyzerAttribute)
            {
            }

            internal IEnumerable<TClassDeclarationSyntax> GetClassDeclarationNodes(INamedTypeSymbol namedType, CancellationToken cancellationToken)
            {
                foreach (var syntax in namedType.DeclaringSyntaxReferences.Select(s => s.GetSyntax(cancellationToken)))
                {
                    if (syntax != null)
                    {
                        var classDecl = syntax.FirstAncestorOrSelf<TClassDeclarationSyntax>(ascendOutOfTrivia: false);
                        if (classDecl != null)
                        {
                            yield return classDecl;
                        }
                    }
                }
            }

            protected override void AnalyzeDiagnosticAnalyzer(SymbolAnalysisContext symbolContext)
            {
                var namedType = (INamedTypeSymbol)symbolContext.Symbol;
                var classDecls = GetClassDeclarationNodes(namedType, symbolContext.CancellationToken);
                foreach (var classDecl in classDecls)
                {
                    var invocations = classDecl.DescendantNodes().OfType<TInvocationExpressionSyntax>();
                    if (invocations.Any())
                    {
                        var semanticModel = symbolContext.Compilation.GetSemanticModel(classDecl.SyntaxTree);
                        foreach (var invocation in invocations)
                        {
                            var symbol = semanticModel.GetSymbolInfo(invocation, symbolContext.CancellationToken).Symbol;
                            if (symbol != null)
                            {
                                AnalyzeInvocation(symbolContext, invocation, symbol, semanticModel);
                            }
                        }
                    }
                }
            }

            protected abstract void AnalyzeInvocation(SymbolAnalysisContext symbolContext, TInvocationExpressionSyntax invocation, ISymbol invocationSymbol, SemanticModel semanticModel);
        }
    }
}
