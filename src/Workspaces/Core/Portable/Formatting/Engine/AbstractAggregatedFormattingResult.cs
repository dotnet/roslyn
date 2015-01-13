// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal abstract class AbstractAggregatedFormattingResult : IFormattingResult
    {
        protected readonly SyntaxNode Node;

        private readonly IList<AbstractFormattingResult> formattingResults;
        private readonly SimpleIntervalTree<TextSpan> formattingSpans;

        private readonly CancellableLazy<IList<TextChange>> lazyTextChanges;
        private readonly CancellableLazy<SyntaxNode> lazyNode;

        public AbstractAggregatedFormattingResult(
            SyntaxNode node,
            IList<AbstractFormattingResult> formattingResults,
            SimpleIntervalTree<TextSpan> formattingSpans)
        {
            Contract.ThrowIfNull(node);
            Contract.ThrowIfNull(formattingResults);

            this.Node = node;
            this.formattingResults = formattingResults;
            this.formattingSpans = formattingSpans;

            this.lazyTextChanges = new CancellableLazy<IList<TextChange>>(CreateTextChanges);
            this.lazyNode = new CancellableLazy<SyntaxNode>(CreateFormattedRoot);
        }

        /// <summary>
        /// rewrite the node with the given trivia information in the map
        /// </summary>
        protected abstract SyntaxNode Rewriter(Dictionary<ValueTuple<SyntaxToken, SyntaxToken>, TriviaData> changeMap, CancellationToken cancellationToken);

        protected SimpleIntervalTree<TextSpan> GetFormattingSpans()
        {
            return this.formattingSpans ?? SimpleIntervalTree.Create(TextSpanIntervalIntrospector.Instance, this.formattingResults.Select(r => r.FormattedSpan));
        }

        #region IFormattingResult implementation

        public bool ContainsChanges
        {
            get
            {
                return this.GetTextChanges(CancellationToken.None).Count > 0;
            }
        }

        public IList<TextChange> GetTextChanges(CancellationToken cancellationToken)
        {
            return this.lazyTextChanges.GetValue(cancellationToken);
        }

        public SyntaxNode GetFormattedRoot(CancellationToken cancellationToken)
        {
            return this.lazyNode.GetValue(cancellationToken);
        }

        private IList<TextChange> CreateTextChanges(CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Formatting_AggregateCreateTextChanges, cancellationToken))
            {
                // quick check
                var changes = CreateTextChangesWorker(cancellationToken);

                // formatted spans and formatting spans are different, filter returns to formatting span
                return this.formattingSpans == null ? changes : changes.Where(s => this.formattingSpans.IntersectsWith(s.Span)).ToList();
            }
        }

        private IList<TextChange> CreateTextChangesWorker(CancellationToken cancellationToken)
        {
            if (this.formattingResults.Count == 1)
            {
                return this.formattingResults[0].GetTextChanges(cancellationToken);
            }

            // pre-allocate list
            var count = this.formattingResults.Sum(r => r.GetTextChanges(cancellationToken).Count);
            var result = new List<TextChange>(count);
            foreach (var formattingResult in this.formattingResults)
            {
                result.AddRange(formattingResult.GetTextChanges(cancellationToken));
            }

            return result;
        }

        private SyntaxNode CreateFormattedRoot(CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Formatting_AggregateCreateFormattedRoot, cancellationToken))
            {
                // create a map
                var map = new Dictionary<ValueTuple<SyntaxToken, SyntaxToken>, TriviaData>();

                this.formattingResults.Do(result => result.GetChanges(cancellationToken).Do(change => map.Add(change.Item1, change.Item2)));

                return Rewriter(map, cancellationToken);
            }
        }

        #endregion
    }
}
