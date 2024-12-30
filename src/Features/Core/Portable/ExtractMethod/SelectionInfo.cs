// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal abstract partial class AbstractExtractMethodService<
    TStatementSyntax,
    TExecutableStatementSyntax,
    TExpressionSyntax>
{
    internal sealed class InitialSelectionInfo
    {
        public readonly OperationStatus Status;

        public readonly bool SelectionInExpression;

        public readonly SyntaxToken FirstTokenInOriginalSpan;
        public readonly SyntaxToken LastTokenInOriginalSpan;

        public readonly TStatementSyntax? FirstStatement;
        public readonly TStatementSyntax? LastStatement;

        private InitialSelectionInfo(OperationStatus status)
            => Status = status;


        private InitialSelectionInfo(
            OperationStatus status,
            SyntaxToken firstTokenInOriginalSpan,
            SyntaxToken lastTokenInOriginalSpan,
            TStatementSyntax? firstStatement,
            TStatementSyntax? lastStatement,
            bool selectionInExpression)
            : this(status)
        {
            FirstTokenInOriginalSpan = firstTokenInOriginalSpan;
            LastTokenInOriginalSpan = lastTokenInOriginalSpan;
            FirstStatement = firstStatement;
            LastStatement = lastStatement;
            SelectionInExpression = selectionInExpression;
        }

        public static InitialSelectionInfo Failure(string reason)
            => new(new(succeeded: false, reason));

        public static InitialSelectionInfo Expression(
            SyntaxToken firstTokenInOriginalSpan,
            SyntaxToken lastTokenInOriginalSpan)
        {
            return new(OperationStatus.SucceededStatus, firstTokenInOriginalSpan, lastTokenInOriginalSpan, firstStatement: null, lastStatement: null, selectionInExpression: true);
        }

        public static InitialSelectionInfo Statement(
            SemanticDocument document,
            SyntaxToken firstTokenInOriginalSpan,
            SyntaxToken lastTokenInOriginalSpan,
            CancellationToken cancellationToken)
        {
            var statements = GetStatementRangeContainingSpan(document, firstTokenInOriginalSpan, lastTokenInOriginalSpan, cancellationToken);
            if (statements is not var (firstStatement, lastStatement))
                return Failure(FeaturesResources.No_valid_statement_range_to_extract);

            return new(OperationStatus.SucceededStatus, firstTokenInOriginalSpan, lastTokenInOriginalSpan, firstStatement, lastStatement, selectionInExpression: false);
        }

        public SyntaxNode CommonRoot => this.FirstTokenInOriginalSpan.GetCommonRoot(this.LastTokenInOriginalSpan);

        private static (TStatementSyntax firstStatement, TStatementSyntax lastStatement)? GetStatementRangeContainingSpan(
            SemanticDocument document,
            SyntaxToken firstTokenInOriginalSpan,
            SyntaxToken lastTokenInOriginalSpan,
            CancellationToken cancellationToken)
        {
            var blockFacts = document.GetRequiredLanguageService<IBlockFactsService>();

            // use top-down approach to find smallest statement range that contains given span. this approach is more
            // expansive than bottom-up approach I used before but way simpler and easy to understand
            var textSpan = TextSpan.FromBounds(firstTokenInOriginalSpan.SpanStart, lastTokenInOriginalSpan.Span.End);

            var root = document.Root;
            var token1 = root.FindToken(textSpan.Start);
            var token2 = root.FindTokenFromEnd(textSpan.End);

            var commonRoot = token1.GetCommonRoot(token2).GetAncestorOrThis<TStatementSyntax>() ?? root;

            TStatementSyntax? firstStatement = null;
            TStatementSyntax? lastStatement = null;

            var spine = new List<TStatementSyntax>();

            foreach (var statement in commonRoot.DescendantNodesAndSelf().OfType<TStatementSyntax>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                // quick skip check.
                // - not containing at all
                if (statement.Span.End < textSpan.Start)
                    continue;

                // quick exit check
                // - passed candidate statements
                if (textSpan.End < statement.SpanStart)
                    break;

                if (statement.SpanStart <= textSpan.Start)
                {
                    // keep track spine
                    spine.Add(statement);
                }

                if (textSpan.End <= statement.Span.End && spine.Any(s => AreStatementsInSameContainer(s, statement)))
                {
                    // malformed code or selection can make spine to have more than an elements
                    firstStatement = spine.First(s => AreStatementsInSameContainer(s, statement));
                    lastStatement = statement;

                    spine.Clear();
                }
            }

            if (firstStatement == null || lastStatement == null)
                return null;

            return (firstStatement, lastStatement);

            bool AreStatementsInSameContainer(TStatementSyntax statement1, TStatementSyntax statement2)
            {
                var parent1 = blockFacts.GetImmediateParentExecutableBlockForStatement(statement1) ?? statement1.Parent;
                var parent2 = blockFacts.GetImmediateParentExecutableBlockForStatement(statement2) ?? statement2.Parent;

                return parent1 == parent2;
            }
        }
    }

    internal sealed record FinalSelectionInfo
    {
        public required OperationStatus Status { get; init; }

        public TextSpan FinalSpan { get; init; }

        public SyntaxToken FirstTokenInFinalSpan { get; init; }
        public SyntaxToken LastTokenInFinalSpan { get; init; }

        public bool SelectionInExpression { get; init; }
        public bool SelectionInSingleStatement { get; init; }

        /// <summary>
        /// For VB.  C# should just use standard <c>with</c> operator.
        /// </summary>
        public FinalSelectionInfo With(
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

            var firstStatement = this.FirstTokenInFinalSpan.GetRequiredAncestor<TExecutableStatementSyntax>();
            var lastStatement = this.LastTokenInFinalSpan.GetRequiredAncestor<TExecutableStatementSyntax>();
            if (firstStatement == lastStatement || firstStatement.Span.Contains(lastStatement.Span))
                return SelectionType.SingleStatement;

            return SelectionType.MultipleStatements;
        }
    }
}
