// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpCodeActionCreateAnalyzer : CodeActionCreateAnalyzer<SyntaxKind>
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

            protected override void GetSyntaxAnalyzer(CodeBlockStartAnalysisContext<SyntaxKind> context, ImmutableHashSet<ISymbol> symbols)
            {
                var analyzer = new SyntaxAnalyzer(symbols);
                context.RegisterSyntaxNodeAction(analyzer.AnalyzeNode, analyzer.SyntaxKindsOfInterest.ToArray());
            }
        }

        private sealed class SyntaxAnalyzer : AbstractSyntaxAnalyzer
        {
            public SyntaxAnalyzer(ImmutableHashSet<ISymbol> symbols) : base(symbols)
            {
            }

            public ImmutableArray<SyntaxKind> SyntaxKindsOfInterest
            {
                get { return ImmutableArray.Create(SyntaxKind.InvocationExpression); }
            }

            public void AnalyzeNode(SyntaxNodeAnalysisContext context)
            {
                var invocation = context.Node as InvocationExpressionSyntax;
                if (invocation == null)
                {
                    return;
                }

                AnalyzeInvocationExpression(invocation.Expression, context.SemanticModel, context.ReportDiagnostic, context.CancellationToken);
            }
        }
    }
}
