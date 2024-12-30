// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal abstract partial class AbstractExtractMethodService<
    TStatementSyntax,
    TExecutableStatementSyntax,
    TExpressionSyntax>
{
    public abstract partial class SelectionValidator(
        SemanticDocument document,
        TextSpan textSpan)
    {
        protected readonly SemanticDocument SemanticDocument = document;
        protected readonly TextSpan OriginalSpan = textSpan;

        public bool ContainsValidSelection => !OriginalSpan.IsEmpty;

        protected abstract TextSpan GetAdjustedSpan(TextSpan textSpan);

        protected abstract InitialSelectionInfo GetInitialSelectionInfo(CancellationToken cancellationToken);

        protected abstract FinalSelectionInfo UpdateSelectionInfo(
            InitialSelectionInfo selectionInfo, CancellationToken cancellationToken);
        protected abstract Task<SelectionResult> CreateSelectionResultAsync(
            InitialSelectionInfo initialSelectionInfo, FinalSelectionInfo selectionInfo, CancellationToken cancellationToken);

        public async Task<(SelectionResult?, OperationStatus)> GetValidSelectionAsync(CancellationToken cancellationToken)
        {
            if (!this.ContainsValidSelection)
                return (null, OperationStatus.FailedWithUnknownReason);

            var initialSelectionInfo = this.GetInitialSelectionInfo(cancellationToken);
            if (initialSelectionInfo.Status.Failed)
                return (null, initialSelectionInfo.Status);

            var selectionInfo = UpdateSelectionInfo(initialSelectionInfo, cancellationToken);
            if (selectionInfo.Status.Failed)
                return (null, selectionInfo.Status);

            if (!selectionInfo.SelectionInExpression &&
                !IsValidStatementRange(SemanticDocument.Root, selectionInfo.FinalSpan, cancellationToken))
            {
                return (null, selectionInfo.Status.With(succeeded: false, FeaturesResources.Cannot_determine_valid_range_of_statements_to_extract));
            }

            var selectionResult = await CreateSelectionResultAsync(initialSelectionInfo, selectionInfo, cancellationToken).ConfigureAwait(false);

            var status = selectionInfo.Status.With(
                selectionResult.ValidateSelectionResult(cancellationToken));

            return (selectionResult, status);
        }

        private static bool IsValidStatementRange(
            SyntaxNode root, TextSpan textSpan, CancellationToken cancellationToken)
        {
            // use top-down approach to find largest statement range contained in the given span
            // this method is a bit more expensive than bottom-up approach, but way more simpler than the other approach.
            var token1 = root.FindToken(textSpan.Start);
            var token2 = root.FindTokenFromEnd(textSpan.End);

            var commonRoot = token1.GetCommonRoot(token2).GetAncestorOrThis<TStatementSyntax>() ?? root;

            TStatementSyntax? firstStatement = null;
            TStatementSyntax? lastStatement = null;

            foreach (var statement in commonRoot.DescendantNodesAndSelf().OfType<TStatementSyntax>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (firstStatement == null && statement.SpanStart >= textSpan.Start)
                    firstStatement = statement;

                if (firstStatement != null && statement.Span.End <= textSpan.End && statement.Parent == firstStatement.Parent)
                    lastStatement = statement;
            }

            return firstStatement != null && lastStatement != null;
        }

        protected FinalSelectionInfo AssignFinalSpan(
             InitialSelectionInfo initialSelectionInfo, FinalSelectionInfo finalSelectionInfo)
        {
            if (finalSelectionInfo.Status.Failed)
                return finalSelectionInfo;

            var adjustedSpan = GetAdjustedSpan(OriginalSpan);

            // set final span
            var start = initialSelectionInfo.FirstTokenInOriginalSpan == finalSelectionInfo.FirstTokenInFinalSpan
                ? Math.Min(initialSelectionInfo.FirstTokenInOriginalSpan.SpanStart, adjustedSpan.Start)
                : finalSelectionInfo.FirstTokenInFinalSpan.FullSpan.Start;

            var end = initialSelectionInfo.LastTokenInOriginalSpan == finalSelectionInfo.LastTokenInFinalSpan
                ? Math.Max(initialSelectionInfo.LastTokenInOriginalSpan.Span.End, adjustedSpan.End)
                : finalSelectionInfo.LastTokenInFinalSpan.Span.End;

            return finalSelectionInfo with
            {
                FinalSpan = GetAdjustedSpan(TextSpan.FromBounds(start, end)),
            };
        }
    }
}
