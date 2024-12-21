// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal abstract partial class AbstractExtractMethodService<
    TValidator,
    TExtractor,
    TSelectionResult,
    TStatementSyntax,
    TExpressionSyntax>
{
    internal sealed record SelectionInfo
    {
        public OperationStatus Status { get; init; }

        public TextSpan OriginalSpan { get; init; }
        public TextSpan FinalSpan { get; init; }

        public SyntaxNode CommonRootFromOriginalSpan { get; init; }

        public SyntaxToken FirstTokenInOriginalSpan { get; init; }
        public SyntaxToken LastTokenInOriginalSpan { get; init; }

        public SyntaxToken FirstTokenInFinalSpan { get; init; }
        public SyntaxToken LastTokenInFinalSpan { get; init; }

        public bool SelectionInExpression { get; init; }
        public bool SelectionInSingleStatement { get; init; }

        /// <summary>
        /// For VB.  C# should just use standard <c>with</c> operator.
        /// </summary>
        public SelectionInfo With(
            Optional<OperationStatus> status = default,
            Optional<TextSpan> finalSpan = default,
            Optional<SyntaxToken> firstTokenInFinalSpan = default,
            Optional<SyntaxToken> lastTokenInFinalSpan = default,
            Optional<bool> selectionInExpression = default,
            Optional<bool> selectionInSingleStatement = default)
        {
            var resultStatus = status.HasValue ? status.Value : this.Status;
            var resultFinalSpan = finalSpan.HasValue ? finalSpan.Value : this.FinalSpan;
            var resultFirstTokenInFinalSpan = firstTokenInFinalSpan.HasValue ? firstTokenInFinalSpan.Value : this.FirstTokenInFinalSpan;
            var resultLastTokenInFinalSpan = lastTokenInFinalSpan.HasValue ? lastTokenInFinalSpan.Value : this.LastTokenInFinalSpan;
            var resultSelectionInExpression = selectionInExpression.HasValue ? selectionInExpression.Value : this.SelectionInExpression;
            var resultSelectionInSingleStatement = selectionInSingleStatement.HasValue ? selectionInSingleStatement.Value : this.SelectionInSingleStatement;

            return this with
            {
                Status = resultStatus,
                FinalSpan = resultFinalSpan,
                FirstTokenInFinalSpan = resultFirstTokenInFinalSpan,
                LastTokenInFinalSpan = resultLastTokenInFinalSpan,
                SelectionInExpression = resultSelectionInExpression,
                SelectionInSingleStatement = resultSelectionInSingleStatement,
            };
        }

        public SelectionType GetSelectionType()
        {
            if (this.SelectionInExpression)
                return SelectionType.Expression;

            var firstStatement = this.FirstTokenInFinalSpan.GetRequiredAncestor<TStatementSyntax>();
            var lastStatement = this.LastTokenInFinalSpan.GetRequiredAncestor<TStatementSyntax>();
            if (firstStatement == lastStatement || firstStatement.Span.Contains(lastStatement.Span))
                return SelectionType.SingleStatement;

            return SelectionType.MultipleStatements;
        }

        public TextSpan GetControlFlowSpan()
            => TextSpan.FromBounds(this.FirstTokenInFinalSpan.SpanStart, this.LastTokenInFinalSpan.Span.End);
    }
}
