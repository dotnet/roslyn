// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Simplification.Simplifiers
{
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

        public bool ShouldSimplifyThisMemberAccessExpression(
            TMemberAccessExpressionSyntax? memberAccessExpression,
            SemanticModel semanticModel,
            SimplifierOptions simplifierOptions,
            [NotNullWhen(true)] out TThisExpressionSyntax? thisExpression,
            out ReportDiagnostic severity,
            CancellationToken cancellationToken)
        {
            severity = default;
            thisExpression = null;

            if (memberAccessExpression is null)
                return false;

            var syntaxFacts = this.SyntaxFacts;
            thisExpression = syntaxFacts.GetExpressionOfMemberAccessExpression(memberAccessExpression) as TThisExpressionSyntax;
            if (!syntaxFacts.IsThisExpression(thisExpression))
                return false;

            var symbolInfo = semanticModel.GetSymbolInfo(memberAccessExpression, cancellationToken);
            if (symbolInfo.Symbol == null)
                return false;

            if (!simplifierOptions.TryGetQualifyMemberAccessOption(symbolInfo.Symbol.Kind, out var optionValue))
                return false;

            if (optionValue.Value)
                return false;

            var speculationAnalyzer = GetSpeculationAnalyzer(semanticModel, memberAccessExpression, cancellationToken);
            var newSymbolInfo = speculationAnalyzer.SpeculativeSemanticModel.GetSymbolInfo(speculationAnalyzer.ReplacedExpression, cancellationToken);
            if (!symbolInfo.Symbol.Equals(newSymbolInfo.Symbol, SymbolEqualityComparer.IncludeNullability))
                return false;

            severity = optionValue.Notification.Severity;
            return !semanticModel.SyntaxTree.OverlapsHiddenPosition(memberAccessExpression.Span, cancellationToken);
        }
    }
}
