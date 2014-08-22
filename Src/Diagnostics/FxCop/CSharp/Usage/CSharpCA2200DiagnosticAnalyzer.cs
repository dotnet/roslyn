// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpCA2200DiagnosticAnalyzer : CA2200DiagnosticAnalyzer, ISyntaxNodeAnalyzer<SyntaxKind>
    {
        private static readonly ImmutableArray<SyntaxKind> kindsOfInterest = ImmutableArray.Create(SyntaxKind.ThrowStatement);

        public ImmutableArray<SyntaxKind> SyntaxKindsOfInterest
        {
            get
            {
                return kindsOfInterest;
            }
        }

        public void AnalyzeNode(SyntaxNode node, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            var throwStatement = (ThrowStatementSyntax)node;
            var expr = throwStatement.Expression;
            if (expr == null)
            {
                return;
            }

            for (SyntaxNode syntax = throwStatement; syntax != null; syntax = syntax.Parent)
            {
                switch (syntax.CSharpKind())
                {
                    case SyntaxKind.CatchClause:
                        {
                            var local = semanticModel.GetSymbolInfo(expr).Symbol as ILocalSymbol;
                            if (local == null || local.Locations.Length == 0)
                            {
                                return;
                            }

                            // if (local.LocalKind != LocalKind.Catch) return; // TODO: expose LocalKind in the symbol model?

                            var catchClause = syntax as CatchClauseSyntax;
                            if (catchClause != null && catchClause.Declaration.Span.Contains(local.Locations[0].SourceSpan))
                            {
                                addDiagnostic(CreateDiagnostic(throwStatement));
                                return;
                            }
                        }

                        break;

                    case SyntaxKind.ParenthesizedLambdaExpression:
                    case SyntaxKind.SimpleLambdaExpression:
                    case SyntaxKind.AnonymousMethodExpression:
                    case SyntaxKind.ClassDeclaration:
                    case SyntaxKind.StructDeclaration:
                        return;
                }
            }
        }
    }
}
