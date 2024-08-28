// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Simplification.Simplifiers;

internal abstract class AbstractMemberAccessExpressionSimplifier<
    TExpressionSyntax,
    TMemberAccessExpressionSyntax,
    TThisExpressionSyntax>
    where TExpressionSyntax : SyntaxNode
    where TMemberAccessExpressionSyntax : TExpressionSyntax
    where TThisExpressionSyntax : TExpressionSyntax
{
    protected abstract ISyntaxFacts SyntaxFacts { get; }
    protected abstract ISpeculationAnalyzer GetSpeculationAnalyzer(
        SemanticModel semanticModel, TMemberAccessExpressionSyntax memberAccessExpression, CancellationToken cancellationToken);

    protected abstract bool MayCauseParseDifference(TMemberAccessExpressionSyntax memberAccessExpression);

    /// <summary>
    /// Checks a member access expression <c>expr.Name</c> and, if it is of the form <c>this.Name</c> or
    /// <c>Me.Name</c> determines if it is safe to replace with just <c>Name</c> alone.
    /// </summary>
    public bool ShouldSimplifyThisMemberAccessExpression(
        TMemberAccessExpressionSyntax? memberAccessExpression,
        SemanticModel semanticModel,
        SimplifierOptions simplifierOptions,
        [NotNullWhen(true)] out TThisExpressionSyntax? thisExpression,
        out NotificationOption2 notificationOption,
        CancellationToken cancellationToken)
    {
        notificationOption = NotificationOption2.Silent;
        thisExpression = null;

        if (memberAccessExpression is null)
            return false;

        var syntaxFacts = this.SyntaxFacts;
        if (!syntaxFacts.IsSimpleMemberAccessExpression(memberAccessExpression))
            return false;

        thisExpression = syntaxFacts.GetExpressionOfMemberAccessExpression(memberAccessExpression) as TThisExpressionSyntax;
        if (!syntaxFacts.IsThisExpression(thisExpression))
            return false;

        var symbolInfo = semanticModel.GetSymbolInfo(memberAccessExpression, cancellationToken);
        if (symbolInfo.Symbol == null)
            return false;

        if (!simplifierOptions.TryGetQualifyMemberAccessOption(symbolInfo.Symbol.Kind, out var optionValue))
            return false;

        // We always simplify a static accesses off of this/me.  Otherwise, we fall back to whatever the user's option is.
        if (!symbolInfo.Symbol.IsStatic && optionValue.Value)
            return false;

        var speculationAnalyzer = GetSpeculationAnalyzer(semanticModel, memberAccessExpression, cancellationToken);
        var newSymbolInfo = speculationAnalyzer.SpeculativeSemanticModel.GetSymbolInfo(speculationAnalyzer.ReplacedExpression, cancellationToken);
        if (!symbolInfo.Symbol.Equals(newSymbolInfo.Symbol, SymbolEqualityComparer.IncludeNullability))
            return false;

        notificationOption = optionValue.Notification;
        return !semanticModel.SyntaxTree.OverlapsHiddenPosition(memberAccessExpression.Span, cancellationToken) &&
               !MayCauseParseDifference(memberAccessExpression);
    }
}
