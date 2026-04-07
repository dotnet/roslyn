// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
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
        TIfStatementSyntax ifStatement,
        [NotNullWhen(true)] out TExpressionSyntax? condition,
        out ImmutableArray<TStatementSyntax> trueStatements);

    private void AnalyzeIfStatementAndReportDiagnostic(
        SyntaxNodeAnalysisContext context,
        IMethodSymbol? referenceEqualsMethod)
    {
        var cancellationToken = context.CancellationToken;
        var option = context.GetAnalyzerOptions().PreferNullPropagation;
        if (!option.Value || ShouldSkipAnalysis(context, option.Notification))
            return;

        var ifStatement = (TIfStatementSyntax)context.Node;
        var analysisResultOpt = AnalyzeIfStatement(
            context.SemanticModel, referenceEqualsMethod, ifStatement, cancellationToken);
        if (analysisResultOpt is not IfStatementAnalysisResult analysisResult)
            return;

        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            ifStatement.GetFirstToken().GetLocation(),
            option.Notification,
            context.Options,
            additionalLocations: [ifStatement.GetLocation()],
            properties: analysisResult.Properties));

    }

    public virtual IfStatementAnalysisResult? AnalyzeIfStatement(
        SemanticModel semanticModel,
        IMethodSymbol? referenceEqualsMethod,
        TIfStatementSyntax ifStatement,
        CancellationToken cancellationToken)
    {
        var syntaxFacts = this.SyntaxFacts;

        // The true-statement if the if-statement has to be a statement of the form `<expr1>.Name(...)`;
        if (!TryGetPartsOfIfStatement(ifStatement, out var condition, out var trueStatement, out var nullAssignmentOpt))
            return null;

        if (trueStatement is not TExpressionStatementSyntax expressionStatement)
            return null;

        if (nullAssignmentOpt is not null and not TExpressionStatementSyntax)
            return null;

        // Now see if the `if (<condition>)` looks like an appropriate null check.
        if (!TryAnalyzeCondition(
                semanticModel, referenceEqualsMethod, condition,
                out var conditionPartToCheck, out var isEquals,
                cancellationToken))
        {
            return null;
        }

        // Ok, we have `if (<expr2> == null)` or `if (<expr2> != null)` (or some similar form of that.  `conditionPartToCheck` will be `<expr2>` here.
        // We only support `if (<expr2> != null)`.  Fail out if we have the alternate form.
        if (isEquals)
            return null;

        if (nullAssignmentOpt != null)
        {
            // If we have a second statement in the if-statement, it must be `<expr> = null;`.
            // This is fine to convert to a null-conditional access.  Here's why:  say we started with:
            //
            // `if (<expr> != null) { <expr>.Method(); <expr> = null; }`
            //
            // If 'expr' is not null, then we execute the body and then end up with expr being null.  So `expr?.Method(); expr = null;`
            // preserves those semantics.  Simialarly, if is expr is null, then `expr?.Method();` does nothing, and `expr = null` keeps it
            // the same as well.  So this is a valid conversion in all cases.
            if (!syntaxFacts.IsSimpleAssignmentStatement(nullAssignmentOpt))
                return null;

            syntaxFacts.GetPartsOfAssignmentStatement(nullAssignmentOpt, out var assignLeft, out _, out var assignRight);
            if (!syntaxFacts.AreEquivalent(assignLeft, conditionPartToCheck) ||
                !syntaxFacts.IsNullLiteralExpression(assignRight))
            {
                return null;
            }

            // Looks good.  we can convert this block.
        }

        var whenPartMatch = GetWhenPartMatch(
            syntaxFacts, semanticModel, conditionPartToCheck,
            (TExpressionSyntax)syntaxFacts.GetExpressionOfExpressionStatement(expressionStatement),
            cancellationToken);
        if (whenPartMatch == null)
            return null;

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
                return null;
        }

        // can't use ?. on a pointer
        var whenPartType = semanticModel.GetTypeInfo(whenPartMatch, cancellationToken).Type;
        if (whenPartType is IPointerTypeSymbol or IFunctionPointerTypeSymbol)
            return null;

        var whenPartIsNullable = semanticModel.GetTypeInfo(whenPartMatch, cancellationToken).Type?.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
        var properties = whenPartIsNullable
            ? s_whenPartIsNullableProperties
            : ImmutableDictionary<string, string?>.Empty;

        return new(trueStatement, whenPartMatch, nullAssignmentOpt, properties);
    }

    private bool TryGetPartsOfIfStatement(
        TIfStatementSyntax ifStatement,
        [NotNullWhen(true)] out TExpressionSyntax? condition,
        [NotNullWhen(true)] out TStatementSyntax? trueStatement,
        out TStatementSyntax? nullAssignmentOpt)
    {
        trueStatement = null;
        nullAssignmentOpt = null;

        if (!this.TryGetPartsOfIfStatement(ifStatement, out condition, out var trueStatements))
            return false;

        if (trueStatements.Length is < 1 or > 2)
            return false;

        trueStatement = trueStatements[0];
        if (trueStatements.Length == 2)
            nullAssignmentOpt = trueStatements[1];
        return true;
    }
}
