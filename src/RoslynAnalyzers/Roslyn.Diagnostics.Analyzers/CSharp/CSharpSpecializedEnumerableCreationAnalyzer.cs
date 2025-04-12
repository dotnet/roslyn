// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable warnings

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Diagnostics.Analyzers;

namespace Roslyn.Diagnostics.CSharp.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpSpecializedEnumerableCreationAnalyzer : SpecializedEnumerableCreationAnalyzer
    {
        protected override void GetCodeBlockStartedAnalyzer(CompilationStartAnalysisContext context, INamedTypeSymbol genericEnumerableSymbol, IMethodSymbol genericEmptyEnumerableSymbol)
        {
            context.RegisterCodeBlockStartAction<SyntaxKind>(new CodeBlockStartedAnalyzer(genericEnumerableSymbol, genericEmptyEnumerableSymbol).Initialize);
        }

        private sealed class CodeBlockStartedAnalyzer : AbstractCodeBlockStartedAnalyzer<SyntaxKind>
        {
            public CodeBlockStartedAnalyzer(INamedTypeSymbol genericEnumerableSymbol, IMethodSymbol genericEmptyEnumerableSymbol)
                : base(genericEnumerableSymbol, genericEmptyEnumerableSymbol)
            {
            }

            protected override void GetSyntaxAnalyzer(CodeBlockStartAnalysisContext<SyntaxKind> context, INamedTypeSymbol genericEnumerableSymbol, IMethodSymbol genericEmptyEnumerableSymbol)
            {
                context.RegisterSyntaxNodeAction(new SyntaxAnalyzer(genericEnumerableSymbol, genericEmptyEnumerableSymbol).AnalyzeNode, SyntaxKind.ReturnStatement);
            }
        }

        private sealed class SyntaxAnalyzer : AbstractSyntaxAnalyzer
        {
            public SyntaxAnalyzer(INamedTypeSymbol genericEnumerableSymbol, IMethodSymbol genericEmptyEnumerableSymbol)
                : base(genericEnumerableSymbol, genericEmptyEnumerableSymbol)
            {
            }

            public void AnalyzeNode(SyntaxNodeAnalysisContext context)
            {
                System.Collections.Generic.IEnumerable<SyntaxNode> expressionsToAnalyze = context.Node.DescendantNodes().Where(n => ShouldAnalyzeExpression(n, context.SemanticModel, context.CancellationToken));

                foreach (SyntaxNode expression in expressionsToAnalyze)
                {
                    switch (expression.Kind())
                    {
                        case SyntaxKind.ArrayCreationExpression:
                            AnalyzeArrayCreationExpression((ArrayCreationExpressionSyntax)expression, context.ReportDiagnostic);
                            break;
                        case SyntaxKind.ImplicitArrayCreationExpression:
                            AnalyzeInitializerExpression(((ImplicitArrayCreationExpressionSyntax)expression).Initializer, context.ReportDiagnostic);
                            break;
                        case SyntaxKind.SimpleMemberAccessExpression:
                            AnalyzeMemberAccessName(((MemberAccessExpressionSyntax)expression).Name, context.SemanticModel, context.ReportDiagnostic, context.CancellationToken);
                            break;
                    }
                }
            }

            private bool ShouldAnalyzeExpression(SyntaxNode expression, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                return expression.Kind() switch
                {
                    SyntaxKind.ArrayCreationExpression
                    or SyntaxKind.ImplicitArrayCreationExpression => ShouldAnalyzeArrayCreationExpression(expression, semanticModel, cancellationToken),
                    SyntaxKind.SimpleMemberAccessExpression => true,
                    _ => false,
                };
            }

            private static void AnalyzeArrayCreationExpression(ArrayCreationExpressionSyntax arrayCreationExpression, Action<Diagnostic> addDiagnostic)
            {
                ArrayTypeSyntax arrayType = arrayCreationExpression.Type;
                if (arrayType.RankSpecifiers.Count == 1)
                {
                    // Check for explicit specification of empty or singleton array

                    if (arrayType.RankSpecifiers[0].ChildNodes()
                        .FirstOrDefault(n => n.IsKind(SyntaxKind.NumericLiteralExpression)) is LiteralExpressionSyntax literalRankSpecifier)
                    {
                        AnalyzeArrayLength((int)literalRankSpecifier.Token.Value, arrayCreationExpression, addDiagnostic);
                        return;
                    }
                }

                AnalyzeInitializerExpression(arrayCreationExpression.Initializer, addDiagnostic);
            }

            private static void AnalyzeInitializerExpression(InitializerExpressionSyntax initializer, Action<Diagnostic> addDiagnostic)
            {
                // Check length of initializer list for empty or singleton array
                if (initializer != null)
                {
                    AnalyzeArrayLength(initializer.Expressions.Count, initializer.Parent, addDiagnostic);
                }
            }
        }
    }
}
