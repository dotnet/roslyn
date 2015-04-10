// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace System.Runtime.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpUseOrdinalStringComparisonAnalyzer : UseOrdinalStringComparisonAnalyzer
    {
        protected override void GetAnalyzer(CompilationStartAnalysisContext context, INamedTypeSymbol stringComparisonType)
        {
            context.RegisterSyntaxNodeAction(new Analyzer(stringComparisonType).AnalyzeNode, SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression, SyntaxKind.InvocationExpression);
        }

        private sealed class Analyzer : AbstractCodeBlockAnalyzer
        {
            public Analyzer(INamedTypeSymbol stringComparisonType)
                : base(stringComparisonType)
            {
            }

            public void AnalyzeNode(SyntaxNodeAnalysisContext context)
            {
                var kind = context.Node.Kind();
                if (kind == SyntaxKind.InvocationExpression)
                {
                    AnalyzeInvocationExpression((InvocationExpressionSyntax)context.Node, context.SemanticModel, context.ReportDiagnostic);
                }
                else if (kind == SyntaxKind.EqualsExpression || kind == SyntaxKind.NotEqualsExpression)
                {
                    AnalyzeBinaryExpression((BinaryExpressionSyntax)context.Node, context.SemanticModel, context.ReportDiagnostic);
                }
            }

            private void AnalyzeInvocationExpression(InvocationExpressionSyntax node, SemanticModel model, Action<Diagnostic> reportDiagnostic)
            {
                if (node.Expression.Kind() == SyntaxKind.SimpleMemberAccessExpression)
                {
                    var memberAccess = (MemberAccessExpressionSyntax)node.Expression;
                    if (memberAccess.Name != null && IsEqualsOrCompare(memberAccess.Name.Identifier.ValueText))
                    {
                        var methodSymbol = model.GetSymbolInfo(memberAccess.Name).Symbol as IMethodSymbol;
                        if (methodSymbol != null && methodSymbol.ContainingType.SpecialType == SpecialType.System_String)
                        {
                            Debug.Assert(IsEqualsOrCompare(methodSymbol.Name));

                            if (!IsAcceptableOverload(methodSymbol, model))
                            {
                                // wrong overload
                                reportDiagnostic(memberAccess.Name.GetLocation().CreateDiagnostic(Rule));
                            }
                            else
                            {
                                var lastArgument = node.ArgumentList.Arguments.Last();
                                var lastArgSymbol = model.GetSymbolInfo(lastArgument.Expression).Symbol;
                                if (lastArgSymbol != null && lastArgSymbol.ContainingType != null &&
                                    lastArgSymbol.ContainingType.Equals(StringComparisonType) &&
                                    !IsOrdinalOrOrdinalIgnoreCase(lastArgument, model))
                                {
                                    // right overload, wrong value
                                    reportDiagnostic(lastArgument.GetLocation().CreateDiagnostic(Rule));
                                }
                            }
                        }
                    }
                }
            }

            private static void AnalyzeBinaryExpression(BinaryExpressionSyntax node, SemanticModel model, Action<Diagnostic> addDiagnostic)
            {
                var leftType = model.GetTypeInfo(node.Left).Type;
                var rightType = model.GetTypeInfo(node.Right).Type;
                if (leftType != null && rightType != null && leftType.SpecialType == SpecialType.System_String && rightType.SpecialType == SpecialType.System_String)
                {
                    addDiagnostic(node.OperatorToken.GetLocation().CreateDiagnostic(Rule));
                }
            }

            private static bool IsOrdinalOrOrdinalIgnoreCase(ArgumentSyntax argumentSyntax, SemanticModel model)
            {
                var argumentSymbol = model.GetSymbolInfo(argumentSyntax.Expression).Symbol;
                if (argumentSymbol != null)
                {
                    return IsOrdinalOrOrdinalIgnoreCase(argumentSymbol.Name);
                }

                return false;
            }
        }
    }
}
