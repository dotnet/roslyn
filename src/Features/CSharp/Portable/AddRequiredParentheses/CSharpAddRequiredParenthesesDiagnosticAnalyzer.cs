// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.AddRequiredParentheses;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryParentheses;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses;

namespace Microsoft.CodeAnalysis.CSharp.AddRequiredParentheses
{
    [DiagnosticAnalyzer(LanguageNames.CSharp), Shared]
    internal class CSharpAddRequiredParenthesesDiagnosticAnalyzer :
        AbstractAddRequiredParenthesesDiagnosticAnalyzer<SyntaxKind, BinaryExpressionSyntax>
    {
        private static readonly ImmutableArray<SyntaxKind> s_kinds = ImmutableArray.Create(
            SyntaxKind.AddExpression,
            SyntaxKind.SubtractExpression,
            SyntaxKind.MultiplyExpression,
            SyntaxKind.DivideExpression,
            SyntaxKind.ModuloExpression,
            SyntaxKind.LeftShiftExpression,
            SyntaxKind.RightShiftExpression,
            SyntaxKind.LogicalOrExpression,
            SyntaxKind.LogicalAndExpression,
            SyntaxKind.BitwiseOrExpression,
            SyntaxKind.BitwiseAndExpression,
            SyntaxKind.ExclusiveOrExpression,
            SyntaxKind.EqualsExpression,
            SyntaxKind.NotEqualsExpression,
            SyntaxKind.LessThanExpression,
            SyntaxKind.LessThanOrEqualExpression,
            SyntaxKind.GreaterThanExpression,
            SyntaxKind.GreaterThanOrEqualExpression,
            SyntaxKind.IsExpression,
            SyntaxKind.AsExpression,
            SyntaxKind.CoalesceExpression);

        protected override ImmutableArray<SyntaxKind> GetSyntaxNodeKinds()
            => s_kinds;

        protected override ISyntaxFactsService GetSyntaxFactsService()
            => CSharpSyntaxFactsService.Instance;

        protected override int GetPrecedence(BinaryExpressionSyntax binaryExpression)
            => (int)binaryExpression.GetOperatorPrecedence();

        protected override PrecedenceKind GetPrecedenceKind(BinaryExpressionSyntax binaryExpression, SemanticModel semanticModel)
            => CSharpRemoveUnnecessaryParenthesesDiagnosticAnalyzer.GetPrecedenceKind(binaryExpression, semanticModel);
    }
}
