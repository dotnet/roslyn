// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.UseCoalesceExpression;

namespace Microsoft.CodeAnalysis.CSharp.UseCoalesceExpression
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseCoalesceExpressionForNullableTernaryConditionalCheckDiagnosticAnalyzer :
        AbstractUseCoalesceExpressionForNullableTernaryConditionalCheckDiagnosticAnalyzer<
            SyntaxKind,
            ExpressionSyntax,
            ConditionalExpressionSyntax,
            BinaryExpressionSyntax,
            MemberAccessExpressionSyntax,
            PrefixUnaryExpressionSyntax>
    {
        protected override ISyntaxFacts GetSyntaxFacts()
            => CSharpSyntaxFacts.Instance;

        protected override bool IsTargetTyped(SemanticModel semanticModel, ConditionalExpressionSyntax conditional, CancellationToken cancellationToken)
            => UseCoalesceExpressionHelpers.IsTargetTyped(semanticModel, conditional, cancellationToken);
    }
}
