// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace System.Runtime.Analyzers
{
    /// <summary>
    /// CA1820: Test for empty strings using string length.
    /// <para>
    /// Comparing strings using the <see cref="string.Length"/> property or the <see cref="string.IsNullOrEmpty"/> method is significantly faster than using <see cref="string.Equals(string)"/>.
    /// This is because Equals executes significantly more MSIL instructions than either IsNullOrEmpty or the number of instructions executed to retrieve the Length property value and compare it to zero.
    /// </para>
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CSharpTestForEmptyStringsUsingStringLengthAnalyzer : TestForEmptyStringsUsingStringLengthAnalyzer<SyntaxKind>
    {
        protected override ImmutableArray<SyntaxKind> SyntaxKindsOfInterest => ImmutableArray.Create(SyntaxKind.InvocationExpression, SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression);

        protected override void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            switch (context.Node.Kind())
            {
                case SyntaxKind.InvocationExpression:
                    AnalyzeInvocationExpression(context);
                    break;

                default:
                    AnalyzeBinaryExpression(context);
                    break;
            }
        }

        private void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            var node = (InvocationExpressionSyntax)context.Node;
            if (node.Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression) &&
                node.ArgumentList?.Arguments.Count > 0)
            {
                var memberAccess = (MemberAccessExpressionSyntax)node.Expression;
                if (memberAccess.Name != null && IsEqualsMethod(memberAccess.Name.Identifier.ValueText))
                {
                    var methodSymbol = context.SemanticModel.GetSymbolInfo(memberAccess.Name).Symbol as IMethodSymbol;
                    if (methodSymbol != null &&
                        IsEqualsMethod(methodSymbol.Name) &&
                        methodSymbol.ContainingType.SpecialType == SpecialType.System_String &&
                        HasAnEmptyStringArgument(node, context.SemanticModel, context.CancellationToken))
                    {
                        ReportDiagnostic(context, node.Expression);
                    }
                }
            }
        }

        private void AnalyzeBinaryExpression(SyntaxNodeAnalysisContext context)
        {
            var node = (BinaryExpressionSyntax)context.Node;
            var methodSymbol = context.SemanticModel.GetSymbolInfo(node, context.CancellationToken).Symbol as IMethodSymbol;
            if (methodSymbol != null &&
                methodSymbol.ContainingType.SpecialType == SpecialType.System_String &&
                IsEqualityOrInequalityOperator(methodSymbol) &&
                (IsEmptyString(node.Left, context.SemanticModel, context.CancellationToken) ||
                 IsEmptyString(node.Right, context.SemanticModel, context.CancellationToken)))
            {
                ReportDiagnostic(context, node);
            }
        }

        private static bool HasAnEmptyStringArgument(InvocationExpressionSyntax invocation, SemanticModel model, CancellationToken cancellationToken)
        {
            foreach (var argument in invocation.ArgumentList.Arguments)
            {
                if (IsEmptyString(argument.Expression, model, cancellationToken))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
