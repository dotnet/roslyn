// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseNullPropagation;

internal abstract partial class AbstractUseNullPropagationDiagnosticAnalyzer<
    TSyntaxKind,
    TExpressionSyntax,
    TStatementSyntax,
    TConditionalExpressionSyntax,
    TBinaryExpressionSyntax,
    TInvocationExpressionSyntax,
    TConditionalAccessExpressionSyntax,
    TElementAccessExpressionSyntax,
    TMemberAccessExpressionSyntax,
    TIfStatementSyntax,
    TExpressionStatementSyntax>
{
    protected abstract bool TryGetPartsOfIfStatement(
        TIfStatementSyntax ifStatement, [NotNullWhen(true)] out TExpressionSyntax? condition, [NotNullWhen(true)] out TStatementSyntax? trueStatement);

    private void AnalyzeIfStatement(
        SyntaxNodeAnalysisContext context,
        IMethodSymbol? referenceEqualsMethod)
    {
        var cancellationToken = context.CancellationToken;
        var option = context.GetAnalyzerOptions().PreferNullPropagation;
        if (!option.Value || ShouldSkipAnalysis(context, option.Notification))
            return;

        var syntaxFacts = this.SyntaxFacts;
        var ifStatement = (TIfStatementSyntax)context.Node;

        // The true-statement if the if-statement has to be a statement of the form `<expr1>.Name(...)`;
        if (!TryGetPartsOfIfStatement(ifStatement, out var condition, out var trueStatement))
            return;

        if (trueStatement is not TExpressionStatementSyntax expressionStatement)
            return;

        // Now see if the `if (<condition>)` looks like an appropriate null check.
        if (!TryAnalyzeCondition(context, syntaxFacts, referenceEqualsMethod, condition, out var conditionPartToCheck, out var isEquals))
            return;

        // Ok, we have `if (<expr2> == null)` or `if (<expr2> != null)` (or some similar form of that.  `conditionPartToCheck` will be `<expr2>` here.
        // We only support `if (<expr2> != null)`.  Fail out if we have the alternate form.
        if (isEquals)
            return;

        var semanticModel = context.SemanticModel;
        var whenPartMatch = GetWhenPartMatch(
            syntaxFacts, semanticModel, conditionPartToCheck,
            (TExpressionSyntax)syntaxFacts.GetExpressionOfExpressionStatement(expressionStatement),
            cancellationToken);
        if (whenPartMatch == null)
            return;

        // If we have:
        //
        // D D { get; }
        // 
        // public void Test()
        // {
        //     if (D != null)
        //     {
        //         D.Method(D);
        //     }
        // }
        //
        // Then `D.Method` is actually an access of a static member, and cannot be converted to `D?.Method`.
        if (whenPartMatch.Parent is TMemberAccessExpressionSyntax memberAccess)
        {
            var memberSymbol = semanticModel.GetSymbolInfo(memberAccess, cancellationToken).GetAnySymbol();
            if (memberSymbol?.IsStatic is true)
                return;
        }

        // can't use ?. on a pointer
        var whenPartType = semanticModel.GetTypeInfo(whenPartMatch, cancellationToken).Type;
        if (whenPartType is IPointerTypeSymbol or IFunctionPointerTypeSymbol)
            return;

        var whenPartIsNullable = semanticModel.GetTypeInfo(whenPartMatch).Type?.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
        var properties = whenPartIsNullable
            ? s_whenPartIsNullableProperties
            : ImmutableDictionary<string, string?>.Empty;

        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            ifStatement.GetFirstToken().GetLocation(),
            option.Notification,
            ImmutableArray.Create(
                ifStatement.GetLocation(),
                trueStatement.GetLocation(),
                whenPartMatch.GetLocation()),
            properties));
    }
}
