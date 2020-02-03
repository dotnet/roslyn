// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.UseCoalesceExpression;

namespace Microsoft.CodeAnalysis.CSharp.UseCoalesceExpression
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseCoalesceExpressionDiagnosticAnalyzer :
        AbstractUseCoalesceExpressionDiagnosticAnalyzer<
            SyntaxKind,
            ExpressionSyntax,
            ConditionalExpressionSyntax,
            BinaryExpressionSyntax>
    {
        protected override ISyntaxFactsService GetSyntaxFactsService()
            => CSharpSyntaxFactsService.Instance;

        protected override SyntaxKind GetSyntaxKindToAnalyze()
            => SyntaxKind.ConditionalExpression;

        protected override bool IsEquals(BinaryExpressionSyntax condition)
            => condition.Kind() == SyntaxKind.EqualsExpression;

        protected override bool IsNotEquals(BinaryExpressionSyntax condition)
            => condition.Kind() == SyntaxKind.NotEqualsExpression;
    }
}
