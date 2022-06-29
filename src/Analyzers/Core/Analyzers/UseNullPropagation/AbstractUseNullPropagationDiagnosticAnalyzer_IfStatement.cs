// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseNullPropagation;

internal abstract partial class AbstractUseNullPropagationDiagnosticAnalyzer<
    TSyntaxKind,
    TExpressionSyntax,
    TStatementSyntax,
    TConditionalExpressionSyntax,
    TBinaryExpressionSyntax,
    TInvocationExpressionSyntax,
    TMemberAccessExpressionSyntax,
    TConditionalAccessExpressionSyntax,
    TElementAccessExpressionSyntax,
    TIfStatementSyntax,
    TExpressionStatementSyntax>
{
    protected abstract bool TryGetSingleTrueStatementOfIfStatement(TIfStatementSyntax ifStatement, [NotNullWhen(true)] out TStatementSyntax? trueStatement);

    private void AnalyzeIfStatement(
        SyntaxNodeAnalysisContext context,
        IMethodSymbol? referenceEqualsMethod)
    {
        var option = context.GetAnalyzerOptions().PreferNullPropagation;
        if (!option.Value)
            return;

        var syntaxFacts = GetSyntaxFacts();
        var ifStatement = (TIfStatementSyntax)context.Node;

        var condition = (TExpressionSyntax)syntaxFacts.GetConditionOfIfStatement(ifStatement);

        // The true-statement if the if-statement has to be a statement of the form `<expr1>.Name(...)`;
        if (!TryGetSingleTrueStatementOfIfStatement(ifStatement, out var trueStatement))
            return;

        if (trueStatement is not TExpressionStatementSyntax expressionStatement)
            return;

        var trueExpression = (TExpressionSyntax)syntaxFacts.GetExpressionOfExpressionStatement(expressionStatement);

        //var invokedExpression = (TExpressionSyntax)syntaxFacts.GetExpressionOfInvocationExpression(trueInvocation);
        //if (!syntaxFacts.IsSimpleMemberAccessExpression(invokedExpression))
        //    return;

        //// this is the `<expr1>` portion of the invocation.
        //var accessedExpression = (TExpressionSyntax?)syntaxFacts.GetExpressionOfMemberAccessExpression(invokedExpression);
        //if (accessedExpression is null)
        //    return;

        // Now see if the `if (...)` looks like an appropriate null check.

        if (!TryAnalyzeCondition(context, syntaxFacts, referenceEqualsMethod, condition, out var conditionPartToCheck, out var isEquals))
            return;

        // Ok, we have `if (<expr2> == null)` or `if (<expr2> != null)` (or some similar form of that.  `conditionPartToCheck` will be `<expr2>` here.
        // We only support `if (<expr2> != null)`.  Fail out if we have the alternate form.
        if (isEquals)
            return;

        var semanticModel = context.SemanticModel;
        var whenPartMatch = GetWhenPartMatch(syntaxFacts, semanticModel, conditionPartToCheck, trueExpression);
        if (whenPartMatch == null)
            return;

        var whenPartIsNullable = semanticModel.GetTypeInfo(whenPartMatch).Type?.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
        var properties = whenPartIsNullable
            ? s_whenPartIsNullableProperties
            : ImmutableDictionary<string, string?>.Empty;

        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            ifStatement.GetFirstToken().GetLocation(),
            option.Notification.Severity,
            ImmutableArray.Create(
                ifStatement.GetLocation(),
                trueStatement.GetLocation(),
                whenPartMatch.GetLocation()),
            properties));
    }
}
