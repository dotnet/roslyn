// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.AnalyzerPowerPack.Usage;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AnalyzerPowerPack.CSharp.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpCA2200DiagnosticAnalyzer : CA2200DiagnosticAnalyzer
    {
        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.RegisterSyntaxNodeAction<SyntaxKind>(AnalyzeNode, SyntaxKind.ThrowStatement);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var throwStatement = (ThrowStatementSyntax)context.Node;
            var expr = throwStatement.Expression;
            if (expr == null)
            {
                return;
            }

            for (SyntaxNode syntax = throwStatement; syntax != null; syntax = syntax.Parent)
            {
                switch (syntax.Kind())
                {
                    case SyntaxKind.CatchClause:
                        {
                            var local = context.SemanticModel.GetSymbolInfo(expr).Symbol as ILocalSymbol;
                            if (local == null || local.Locations.Length == 0)
                            {
                                return;
                            }

                            // if (local.LocalKind != LocalKind.Catch) return; // TODO: expose LocalKind in the symbol model?

                            var catchClause = syntax as CatchClauseSyntax;
                            if (catchClause != null && catchClause.Declaration.Span.Contains(local.Locations[0].SourceSpan))
                            {
                                context.ReportDiagnostic(CreateDiagnostic(throwStatement));
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
