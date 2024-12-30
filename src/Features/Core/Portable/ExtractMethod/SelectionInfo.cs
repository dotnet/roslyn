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

        public InitialSelectionInfo(
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

        public SyntaxNode CommonRoot => this.FirstTokenInOriginalSpan.GetCommonRoot(this.LastTokenInOriginalSpan);
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
