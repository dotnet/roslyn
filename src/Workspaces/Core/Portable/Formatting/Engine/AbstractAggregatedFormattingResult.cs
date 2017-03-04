﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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

        private readonly IList<AbstractFormattingResult> _formattingResults;
        private readonly SimpleIntervalTree<TextSpan> _formattingSpans;

        private readonly CancellableLazy<IList<TextChange>> _lazyTextChanges;
        private readonly CancellableLazy<SyntaxNode> _lazyNode;

        public AbstractAggregatedFormattingResult(
            SyntaxNode node,
            IList<AbstractFormattingResult> formattingResults,
            SimpleIntervalTree<TextSpan> formattingSpans)
        {
            Contract.ThrowIfNull(node);
            Contract.ThrowIfNull(formattingResults);

            this.Node = node;
            _formattingResults = formattingResults;
            _formattingSpans = formattingSpans;

            _lazyTextChanges = new CancellableLazy<IList<TextChange>>(CreateTextChanges);
            _lazyNode = new CancellableLazy<SyntaxNode>(CreateFormattedRoot);
        }

        /// <summary>
        /// rewrite the node with the given trivia information in the map
        /// </summary>
        protected abstract SyntaxNode Rewriter(Dictionary<ValueTuple<SyntaxToken, SyntaxToken>, TriviaData> changeMap, CancellationToken cancellationToken);

        protected SimpleIntervalTree<TextSpan> GetFormattingSpans()
        {
            return _formattingSpans ?? SimpleIntervalTree.Create(TextSpanIntervalIntrospector.Instance, _formattingResults.Select(r => r.FormattedSpan));
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
            return _lazyTextChanges.GetValue(cancellationToken);
        }

        public SyntaxNode GetFormattedRoot(CancellationToken cancellationToken)
        {
            return _lazyNode.GetValue(cancellationToken);
        }

        private IList<TextChange> CreateTextChanges(CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Formatting_AggregateCreateTextChanges, cancellationToken))
            {
                // quick check
                var changes = CreateTextChangesWorker(cancellationToken);

                // formatted spans and formatting spans are different, filter returns to formatting span
                return _formattingSpans == null 
                    ? changes
                    : changes.Where(s => _formattingSpans.HasIntervalThatIntersectsWith(s.Span)).ToList();
            }
        }

        private IList<TextChange> CreateTextChangesWorker(CancellationToken cancellationToken)
        {
            if (_formattingResults.Count == 1)
            {
                return _formattingResults[0].GetTextChanges(cancellationToken);
            }

            // pre-allocate list
            var count = _formattingResults.Sum(r => r.GetTextChanges(cancellationToken).Count);
            var result = new List<TextChange>(count);
            foreach (var formattingResult in _formattingResults)
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

                _formattingResults.Do(result => result.GetChanges(cancellationToken).Do(change => map.Add(change.Item1, change.Item2)));

                return Rewriter(map, cancellationToken);
            }
        }

        #endregion
    }
}
