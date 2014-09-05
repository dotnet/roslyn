// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpCodeActionCreateAnalyzer : CodeActionCreateAnalyzer
    {
        protected override AbstractCodeBlockStartedAnalyzer GetCodeBlockStartedAnalyzer(ImmutableHashSet<ISymbol> symbols)
        {
            return new CodeBlockStartedAnalyzer(symbols);
        }

        private sealed class CodeBlockStartedAnalyzer : AbstractCodeBlockStartedAnalyzer
        {
            public CodeBlockStartedAnalyzer(ImmutableHashSet<ISymbol> symbols) : base(symbols)
            {
            }

            protected override AbstractSyntaxAnalyzer GetSyntaxAnalyzer(ImmutableHashSet<ISymbol> symbols)
            {
                return new SyntaxAnalyzer(symbols);
            }
        }

        private sealed class SyntaxAnalyzer : AbstractSyntaxAnalyzer, ISyntaxNodeAnalyzer<SyntaxKind>
        {
            public SyntaxAnalyzer(ImmutableHashSet<ISymbol> symbols) : base(symbols)
            {
            }

            public ImmutableArray<SyntaxKind> SyntaxKindsOfInterest
            {
                get { return ImmutableArray.Create(SyntaxKind.InvocationExpression); }
            }

            public void AnalyzeNode(SyntaxNode node, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
            {
                var invocation = node as InvocationExpressionSyntax;
                if (invocation == null)
                {
                    return;
                }

                AnalyzeInvocationExpression(invocation.Expression, semanticModel, addDiagnostic, cancellationToken);
            }
        }
    }
}
