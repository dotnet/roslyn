// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers.CSharp
{
    [DiagnosticAnalyzer]
    [ExportDiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpSpecializedEnumerableCreationAnalyzer : SpecializedEnumerableCreationAnalyzer
    {
        protected override AbstractCodeBlockStartedAnalyzer GetCodeBlockStartedAnalyzer(INamedTypeSymbol genericEnumerableSymbol, IMethodSymbol genericEmptyEnumerableSymbol)
        {
            return new CodeBlockStartedAnalyzer(genericEnumerableSymbol, genericEmptyEnumerableSymbol);
        }

        private sealed class CodeBlockStartedAnalyzer : AbstractCodeBlockStartedAnalyzer
        {
            public CodeBlockStartedAnalyzer(INamedTypeSymbol genericEnumerableSymbol, IMethodSymbol genericEmptyEnumerableSymbol) :
                base(genericEnumerableSymbol, genericEmptyEnumerableSymbol)
            {
            }

            protected override AbstractSyntaxAnalyzer GetSyntaxAnalyzer(INamedTypeSymbol genericEnumerableSymbol, IMethodSymbol genericEmptyEnumerableSymbol)
            {
                return new SyntaxAnalyzer(genericEnumerableSymbol, genericEmptyEnumerableSymbol);
            }
        }

        private sealed class SyntaxAnalyzer : AbstractSyntaxAnalyzer, ISyntaxNodeAnalyzer<SyntaxKind>
        {
            public SyntaxAnalyzer(INamedTypeSymbol genericEnumerableSymbol, IMethodSymbol genericEmptyEnumerableSymbol) :
                base(genericEnumerableSymbol, genericEmptyEnumerableSymbol)
            {
            }

            public ImmutableArray<SyntaxKind> SyntaxKindsOfInterest
            {
                get { return ImmutableArray.Create(SyntaxKind.ReturnStatement); }
            }

            public void AnalyzeNode(SyntaxNode node, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
            {
                var expressionsToAnalyze = node.DescendantNodes().Where(n => ShouldAnalyzeExpression(n, semanticModel));

                foreach (var expression in expressionsToAnalyze)
                {
                    switch (expression.CSharpKind())
                    {
                        case SyntaxKind.ArrayCreationExpression:
                            AnalyzeArrayCreationExpression((ArrayCreationExpressionSyntax)expression, addDiagnostic);
                            break;
                        case SyntaxKind.ImplicitArrayCreationExpression:
                            AnalyzeInitializerExpression(((ImplicitArrayCreationExpressionSyntax)expression).Initializer, addDiagnostic);
                            break;
                        case SyntaxKind.SimpleMemberAccessExpression:
                            AnalyzeMemberAccessName(((MemberAccessExpressionSyntax)expression).Name, semanticModel, addDiagnostic);
                            break;
                    }
                }
            }

            private bool ShouldAnalyzeExpression(SyntaxNode expression, SemanticModel semanticModel)
            {
                switch (expression.CSharpKind())
                {
                    case SyntaxKind.ArrayCreationExpression:
                    case SyntaxKind.ImplicitArrayCreationExpression:
                        return ShouldAnalyzeArrayCreationExpression(expression, semanticModel);
                    case SyntaxKind.SimpleMemberAccessExpression:
                        return true;
                    default:
                        return false;
                }
            }

            private static void AnalyzeArrayCreationExpression(ArrayCreationExpressionSyntax arrayCreationExpression, Action<Diagnostic> addDiagnostic)
            {
                var arrayType = arrayCreationExpression.Type;
                if (arrayType.RankSpecifiers.Count == 1)
                {
                    // Check for explicit specification of empty or singleton array
                    var literalRankSpecifier = arrayType.RankSpecifiers[0].ChildNodes()
                        .SingleOrDefault(n => n.CSharpKind() == SyntaxKind.NumericLiteralExpression)
                        as LiteralExpressionSyntax;

                    if (literalRankSpecifier != null)
                    {
                        Debug.Assert(literalRankSpecifier.Token.Value != null);
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
