// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.UseNullPropagation;

namespace Microsoft.CodeAnalysis.CSharp.UseNullPropagation
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseNullPropagationDiagnosticAnalyzer :
        AbstractUseNullPropagationDiagnosticAnalyzer<
            SyntaxKind,
            ExpressionSyntax,
            ConditionalExpressionSyntax,
            BinaryExpressionSyntax,
            InvocationExpressionSyntax,
            MemberAccessExpressionSyntax,
            ConditionalAccessExpressionSyntax,
            ElementAccessExpressionSyntax>
    {
        protected override bool ShouldAnalyze(Compilation compilation)
            => compilation.LanguageVersion() >= LanguageVersion.CSharp6;

        protected override ISyntaxFacts GetSyntaxFacts()
            => CSharpSyntaxFacts.Instance;

        protected override bool IsInExpressionTree(SemanticModel semanticModel, SyntaxNode node, INamedTypeSymbol? expressionTypeOpt, CancellationToken cancellationToken)
            => node.IsInExpressionTree(semanticModel, expressionTypeOpt, cancellationToken);

        protected override bool TryAnalyzePatternCondition(
            ISyntaxFacts syntaxFacts, SyntaxNode conditionNode,
            [NotNullWhen(true)] out SyntaxNode? conditionPartToCheck, out bool isEquals)
        {
            conditionPartToCheck = null;
            isEquals = true;

            if (conditionNode is not IsPatternExpressionSyntax patternExpression)
            {
                return false;
            }

            if (patternExpression.Pattern is not ConstantPatternSyntax constantPattern)
            {
                return false;
            }

            if (!syntaxFacts.IsNullLiteralExpression(constantPattern.Expression))
            {
                return false;
            }

            conditionPartToCheck = patternExpression.Expression;
            return true;
        }
    }
}
