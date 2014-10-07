// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    /// <summary>
    /// this class maintain contextual information such as 
    /// indentation of current position, based token to follow in current position and etc.
    /// </summary>
    internal partial class FormattingContext
    {
        private readonly AbstractFormatEngine engine;
        private readonly TokenStream tokenStream;

        // interval tree for inseparable regions (Span to indentation data)
        // due to dependencies, each region defined in the data can't be formatted independently.
        private readonly ContextIntervalTree<RelativeIndentationData> relativeIndentationTree;

        // interval tree for each operations.
        // given a span in the tree, it returns data (indentation, anchor delta, etc) to be applied for the span
        private readonly ContextIntervalTree<IndentationData> indentationTree;
        private readonly ContextIntervalTree<SuppressWrappingData> suppressWrappingTree;
        private readonly ContextIntervalTree<SuppressSpacingData> suppressSpacingTree;
        private readonly ContextIntervalTree<AnchorData> anchorTree;

        // anchor token to anchor data map.
        // unlike anchorTree that would return anchor data for given span in the tree, it will return
        // anchorData based on key which is anchor token.
        private readonly Dictionary<SyntaxToken, AnchorData> anchorBaseTokenMap;

        // hashset to prevent duplicate entries in the trees.
        private readonly HashSet<TextSpan> indentationMap;
        private readonly HashSet<TextSpan> suppressWrappingMap;
        private readonly HashSet<TextSpan> suppressSpacingMap;
        private readonly HashSet<TextSpan> anchorMap;

        // used for selection based formatting case. it contains operations that will define
        // what indentation to use as a starting indentation. (we always use 0 for formatting whole tree case)
        private List<IndentBlockOperation> initialIndentBlockOperations;

        private readonly string language;

        public FormattingContext(AbstractFormatEngine engine, TokenStream tokenStream, string language)
        {
            Contract.ThrowIfNull(engine);
            Contract.ThrowIfNull(tokenStream);

            this.engine = engine;
            this.tokenStream = tokenStream;
            this.language = language;

            this.relativeIndentationTree = new ContextIntervalTree<RelativeIndentationData>(this);

            this.indentationTree = new ContextIntervalTree<IndentationData>(this);
            this.suppressWrappingTree = new ContextIntervalTree<SuppressWrappingData>(SuppressIntervalIntrospector.Instance);
            this.suppressSpacingTree = new ContextIntervalTree<SuppressSpacingData>(SuppressIntervalIntrospector.Instance);
            this.anchorTree = new ContextIntervalTree<AnchorData>(this);

            this.anchorBaseTokenMap = new Dictionary<SyntaxToken, AnchorData>();

            this.indentationMap = new HashSet<TextSpan>();
            this.suppressWrappingMap = new HashSet<TextSpan>();
            this.suppressSpacingMap = new HashSet<TextSpan>();
            this.anchorMap = new HashSet<TextSpan>();

            this.initialIndentBlockOperations = new List<IndentBlockOperation>();
        }

        public void Initialize(
            ChainedFormattingRules formattingRules,
            SyntaxToken startToken,
            SyntaxToken endToken,
            CancellationToken cancellationToken)
        {
            var rootNode = this.TreeData.Root;
            if (this.tokenStream.IsFormattingWholeDocument)
            {
                // if we are trying to format whole document, there is no reason to get initial context. just set
                // initial indentation.
                var data = new RootIndentationData(rootNode);
                this.indentationTree.AddIntervalInPlace(data);
                this.indentationMap.Add(data.TextSpan);
                return;
            }

            var initialContextFinder = new InitialContextFinder(tokenStream, formattingRules, rootNode);
            var results = initialContextFinder.Do(startToken, endToken);

            if (results.Item1 != null)
            {
                var indentationOperations = results.Item1;

                var initialOperation = indentationOperations[0];
                var baseIndentationFinder = new BottomUpBaseIndentationFinder(
                                                formattingRules,
                                                this.OptionSet.GetOption(FormattingOptions.TabSize, this.language),
                                                this.OptionSet.GetOption(FormattingOptions.IndentationSize, this.language),
                                                this.tokenStream);
                var initialIndentation = baseIndentationFinder.GetIndentationOfCurrentPosition(
                    rootNode,
                    initialOperation,
                    t => this.tokenStream.GetCurrentColumn(t), cancellationToken);

                var data = new SimpleIndentationData(initialOperation.TextSpan, initialIndentation);
                this.indentationTree.AddIntervalInPlace(data);
                this.indentationMap.Add(data.TextSpan);

                // hold onto initial operations
                this.initialIndentBlockOperations = indentationOperations;
            }

            if (results.Item2 != null)
            {
                results.Item2.Do(o => this.AddInitialSuppressOperation(o));
            }
        }

        public void AddIndentBlockOperations(
            List<IndentBlockOperation> operations,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(operations);

            // if there is no initial block operations
            if (this.initialIndentBlockOperations.Count <= 0)
            {
                // sort operations and add them to interval tree
                operations.Sort(CommonFormattingHelpers.IndentBlockOperationComparer);
                operations.Do(o =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    this.AddIndentBlockOperation(o);
                });

                return;
            }

            var baseSpan = this.initialIndentBlockOperations[0].TextSpan;

            // indentation tree must build up from inputs that are in right order except initial indentation.
            // merge indentation operations from two places (initial operations for current selection, and nodes inside of selections)
            // sort it in right order and apply them to tree
            var count = this.initialIndentBlockOperations.Count - 1 + operations.Count;
            var mergedList = new List<IndentBlockOperation>(count);

            // initial operations are already sorted, just add, no need to filter
            for (int i = 1; i < this.initialIndentBlockOperations.Count; i++)
            {
                mergedList.Add(this.initialIndentBlockOperations[i]);
            }

            for (int i = 0; i < operations.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // filter out operations whose position is before the base indentation
                var operationSpan = operations[i].TextSpan;

                if (operationSpan.Start < baseSpan.Start ||
                    operationSpan.Contains(baseSpan))
                {
                    continue;
                }

                mergedList.Add(operations[i]);
            }

            mergedList.Sort(CommonFormattingHelpers.IndentBlockOperationComparer);
            mergedList.Do(o =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                this.AddIndentBlockOperation(o);
            });
        }

        public void AddIndentBlockOperation(IndentBlockOperation operation)
        {
            var intervalTreeSpan = operation.TextSpan;

            // don't add stuff if it is empty
            if (intervalTreeSpan.IsEmpty ||
                this.indentationMap.Contains(intervalTreeSpan))
            {
                return;
            }

            // relative indentation case where indentation depends on other token
            if (operation.IsRelativeIndentation)
            {
                var inseparableRegionStartingPosition = operation.Option.IsOn(IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine) ? 0 : operation.BaseToken.FullSpan.Start;
                var relativeIndentationGetter = new Lazy<int>(() =>
                {
                    var indentationDelta = operation.IndentationDeltaOrPosition * this.OptionSet.GetOption(FormattingOptions.IndentationSize, this.language);

                    // baseIndentation is calculated for the adjusted token if option is RelativeToFirstTokenOnBaseTokenLine
                    var baseIndentation = this.tokenStream.GetCurrentColumn(operation.Option.IsOn(IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine) ?
                                                                            tokenStream.FirstTokenOfBaseTokenLine(operation.BaseToken) :
                                                                            operation.BaseToken);

                    return baseIndentation + indentationDelta;
                }, isThreadSafe: true);

                // set new indentation
                var relativeIndentationData = new RelativeIndentationData(inseparableRegionStartingPosition, intervalTreeSpan, operation, relativeIndentationGetter);

                this.indentationTree.AddIntervalInPlace(relativeIndentationData);
                this.relativeIndentationTree.AddIntervalInPlace(relativeIndentationData);
                this.indentationMap.Add(intervalTreeSpan);

                return;
            }

            // absolute position case
            if (operation.Option.IsOn(IndentBlockOption.AbsolutePosition))
            {
                this.indentationTree.AddIntervalInPlace(new SimpleIndentationData(intervalTreeSpan, operation.IndentationDeltaOrPosition));
                this.indentationMap.Add(intervalTreeSpan);
                return;
            }

            // regular indentation case where indentation is based on its previous indentation
            var indentationData = this.indentationTree.GetSmallestContainingInterval(operation.TextSpan.Start, 0);
            if (indentationData == null)
            {
                // no previous indentation
                var indentation = operation.IndentationDeltaOrPosition * this.OptionSet.GetOption(FormattingOptions.IndentationSize, this.language);
                this.indentationTree.AddIntervalInPlace(new SimpleIndentationData(intervalTreeSpan, indentation));
                this.indentationMap.Add(intervalTreeSpan);
                return;
            }

            // get indentation based on its previous indentation
            var indentationGetter = new Lazy<int>(() =>
            {
                var indentationDelta = operation.IndentationDeltaOrPosition * this.OptionSet.GetOption(FormattingOptions.IndentationSize, this.language);

                return indentationData.Indentation + indentationDelta;
            }, isThreadSafe: true);

            // set new indentation
            this.indentationTree.AddIntervalInPlace(new LazyIndentationData(intervalTreeSpan, indentationGetter));
            this.indentationMap.Add(intervalTreeSpan);
        }

        public void AddInitialSuppressOperation(SuppressOperation operation)
        {
            // don't add stuff if it is empty
            if (operation == null || operation.TextSpan.IsEmpty)
            {
                return;
            }

            var onSameLine = this.tokenStream.TwoTokensOriginallyOnSameLine(operation.StartToken, operation.EndToken);
            AddSuppressOperation(operation, onSameLine);
        }

        public void AddSuppressOperations(
            List<SuppressOperation> operations,
            CancellationToken cancellationToken)
        {
            var valuePairs = new ValueTuple<SuppressOperation, bool, bool>[operations.Count];

            // TODO: think about a way to figure out whether it is already suppressed and skip the expensive check below.
            this.engine.TaskExecutor.For(0, operations.Count, i =>
            {
                var operation = operations[i];

                // if an operation contains elastic trivia itself and the operation is not marked to ignore the elastic triva
                // ignore the operation
                if (operation.ContainsElasticTrivia(tokenStream) && !operation.Option.IsOn(SuppressOption.IgnoreElastic))
                {
                    // don't bother to calculate line alignment between tokens
                    valuePairs[i] = ValueTuple.Create(operation, false, false);
                    return;
                }

                var onSameLine = tokenStream.TwoTokensOriginallyOnSameLine(operation.StartToken, operation.EndToken);
                valuePairs[i] = ValueTuple.Create(operation, true, onSameLine);
            }, cancellationToken);

            valuePairs.Do(v =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (v.Item2)
                {
                    AddSuppressOperation(v.Item1, v.Item3);
                }
            });
        }

        private void AddSuppressOperation(SuppressOperation operation, bool onSameLine)
        {
            AddSpacingSuppressOperation(operation, onSameLine);
            AddWrappingSuppressOperation(operation, onSameLine);
        }

        private void AddSpacingSuppressOperation(SuppressOperation operation, bool twoTokensOnSameLine)
        {
            // don't add stuff if it is empty
            if (operation == null ||
                operation.TextSpan.IsEmpty)
            {
                return;
            }

            // we might need to merge bits with enclosing suppress flag
            var option = operation.Option;
            if (!option.IsMaskOn(SuppressOption.NoSpacing) || this.suppressSpacingMap.Contains(operation.TextSpan))
            {
                return;
            }

            if (!(option.IsOn(SuppressOption.NoSpacingIfOnSingleLine) && twoTokensOnSameLine) &&
                !(option.IsOn(SuppressOption.NoSpacingIfOnMultipleLine) && !twoTokensOnSameLine))
            {
                return;
            }

            var data = new SuppressSpacingData(operation.TextSpan, noSpacing: true);

            this.suppressSpacingMap.Add(operation.TextSpan);
            this.suppressSpacingTree.AddIntervalInPlace(data);
        }

        private void AddWrappingSuppressOperation(SuppressOperation operation, bool twoTokensOnSameLine)
        {
            // don't add stuff if it is empty
            if (operation == null ||
                operation.TextSpan.IsEmpty)
            {
                return;
            }

            var option = operation.Option;
            if (!option.IsMaskOn(SuppressOption.NoWrapping) || this.suppressWrappingMap.Contains(operation.TextSpan))
            {
                return;
            }

            if (!(option.IsOn(SuppressOption.NoWrappingIfOnSingleLine) && twoTokensOnSameLine) &&
                !(option.IsOn(SuppressOption.NoWrappingIfOnMultipleLine) && !twoTokensOnSameLine))
            {
                return;
            }

            var data = new SuppressWrappingData(operation.TextSpan, noWrapping: true);

            this.suppressWrappingMap.Add(operation.TextSpan);
            this.suppressWrappingTree.AddIntervalInPlace(data);
        }

        public void AddAnchorIndentationOperation(AnchorIndentationOperation operation)
        {
            // don't add stuff if it is empty
            if (operation.TextSpan.IsEmpty ||
                this.anchorMap.Contains(operation.TextSpan) ||
                this.anchorBaseTokenMap.ContainsKey(operation.AnchorToken))
            {
                return;
            }

            var originalSpace = this.tokenStream.GetOriginalColumn(operation.StartToken);
            var data = new AnchorData(operation, originalSpace);

            this.anchorTree.AddIntervalInPlace(data);

            this.anchorBaseTokenMap.Add(operation.AnchorToken, data);
            this.anchorMap.Add(operation.TextSpan);
        }

        [Conditional("DEBUG")]
        private void DebugCheckEmpty<T>(ContextIntervalTree<T> tree, TextSpan textSpan)
        {
            var intervals = tree.GetContainingIntervals(textSpan.Start, textSpan.Length);
            Contract.ThrowIfFalse(intervals.IsEmpty());
        }

        public int GetBaseIndentation(SyntaxToken token)
        {
            return GetBaseIndentation(token.SpanStart);
        }

        public int GetBaseIndentation(int position)
        {
            var indentationData = this.indentationTree.GetSmallestContainingInterval(position, 0);
            if (indentationData == null)
            {
                DebugCheckEmpty(this.indentationTree, new TextSpan(position, 0));
                return 0;
            }

            return indentationData.Indentation;
        }

        public IEnumerable<IndentBlockOperation> GetAllRelativeIndentBlockOperations()
        {
            return this.relativeIndentationTree.GetIntersectingInOrderIntervals(this.TreeData.StartPosition, this.TreeData.EndPosition, this).Select(i => i.Operation);
        }

        public SyntaxToken GetEndTokenForRelativeIndentationSpan(SyntaxToken token)
        {
            var span = token.Span;
            var indentationData = this.relativeIndentationTree.GetSmallestContainingInterval(span.Start, 0);
            if (indentationData == null)
            {
                // this means the given token is not inside of inseparable regions
                return token;
            }

            // recursively find the end token outside of inseparable regions
            var nextToken = indentationData.EndToken.GetNextToken(includeZeroWidth: true);
            if (nextToken.RawKind == 0)
            {
                // reached end of tree
                return default(SyntaxToken);
            }

            return GetEndTokenForRelativeIndentationSpan(nextToken);
        }

        private AnchorData GetAnchorData(SyntaxToken token)
        {
            var span = token.Span;

            var anchorData = this.anchorTree.GetSmallestContainingInterval(span.Start, 0);
            if (anchorData == null)
            {
                // no anchor
                DebugCheckEmpty(this.anchorTree, new TextSpan(span.Start, 0));
                return null;
            }

            return anchorData;
        }

        public int GetAnchorDeltaFromOriginalColumn(SyntaxToken token)
        {
            var anchorData = GetAnchorData(token);
            if (anchorData == null)
            {
                return 0;
            }

            var currentColumn = this.tokenStream.GetCurrentColumn(anchorData.AnchorToken);
            return currentColumn - anchorData.OriginalColumn;
        }

        public SyntaxToken GetAnchorToken(SyntaxToken token)
        {
            var anchorData = GetAnchorData(token);
            if (anchorData == null)
            {
                return default(SyntaxToken);
            }

            return anchorData.AnchorToken;
        }

        public int GetDeltaFromPreviousChangesMap(SyntaxToken token, Dictionary<SyntaxToken, int> previousChangesMap)
        {
            // no changes
            if (!previousChangesMap.ContainsKey(token))
            {
                return 0;
            }

            var currentColumn = this.tokenStream.GetCurrentColumn(token);
            return currentColumn - previousChangesMap[token];
        }

        public SyntaxToken GetEndTokenForAnchorSpan(TokenData tokenData)
        {
            // consider situation like below
            //
            // var q = from c in cs
            //              where c > 1
            //                          + 
            //                            2;
            //
            // if alignment operation moves "where" to align with "from"
            // we want to move "+" and "2" along with it (anchor operation)
            // 
            // below we are trying to figure out up to which token ("2" in the above example)
            // we should apply the anchor operation
            var baseAnchorData = FindAnchorSpanOnSameLineAfterToken(tokenData);
            if (baseAnchorData == null)
            {
                return default(SyntaxToken);
            }

            // our anchor operation is very flexible so it not only let one anchor to contain others, it also
            // let anchors to overlap each other for whatever reasons
            // below, we will try to flat the overlaped anchor span, and find the last position (token) of that span

            // find other anchors overlapping with current anchor span
            var anchorData = this.anchorTree.GetOverlappingIntervals(baseAnchorData.TextSpan.Start, baseAnchorData.TextSpan.Length);

            // among those anchors find the biggest end token
            var lastEndToken = baseAnchorData.EndToken;
            foreach (var interval in anchorData)
            {
                // anchor token is not in scope, move to next
                if (!baseAnchorData.TextSpan.IntersectsWith(interval.AnchorToken.Span))
                {
                    continue;
                }

                if (interval.EndToken.Span.End < lastEndToken.Span.End)
                {
                    continue;
                }

                lastEndToken = interval.EndToken;
            }

            return lastEndToken;
        }

        private AnchorData FindAnchorSpanOnSameLineAfterToken(TokenData tokenData)
        {
            // every token after given token on same line is implicitly dependent to the token.
            // check whether one of them is an anchor token.

            AnchorData lastBaseAnchorData = null;
            while (tokenData.IndexInStream >= 0)
            {
                AnchorData tempAnchorData;
                if (this.anchorBaseTokenMap.TryGetValue(tokenData.Token, out tempAnchorData))
                {
                    lastBaseAnchorData = tempAnchorData;
                }

                // tokenPairIndex is always 0 <= ... < TokenCount - 1
                var tokenPairIndex = tokenData.IndexInStream;
                if (this.tokenStream.TokenCount - 1 <= tokenPairIndex ||
                    this.tokenStream.GetTriviaData(tokenPairIndex).SecondTokenIsFirstTokenOnLine)
                {
                    return lastBaseAnchorData;
                }

                tokenData = tokenData.GetNextTokenData();
            }

            return lastBaseAnchorData;
        }

        public bool IsWrappingSuppressed(TextSpan textSpan)
        {
            // use edge exclusive version of GetSmallestContainingInterval
            var data = this.suppressWrappingTree.GetSmallestEdgeExclusivelyContainingInterval(textSpan.Start, textSpan.Length);
            if (data == null)
            {
                return false;
            }

            return data.NoWrapping;
        }

        public bool IsSpacingSuppressed(TextSpan textSpan)
        {
            // use edge exclusive version of GetSmallestCointainingInterval
            var data = this.suppressSpacingTree.GetSmallestEdgeExclusivelyContainingInterval(textSpan.Start, textSpan.Length);
            if (data == null)
            {
                return false;
            }

            return data.NoSpacing;
        }

        public bool IsSpacingSuppressed(int pairIndex)
        {
            var token1 = this.tokenStream.GetToken(pairIndex);
            var token2 = this.tokenStream.GetToken(pairIndex + 1);

            var spanBetweenTwoTokens = TextSpan.FromBounds(token1.SpanStart, token2.Span.End);

            // this version of SpacingSuppressed will be called after all basic space operations are done. 
            // so no more elastic trivia should have left out
            return IsSpacingSuppressed(spanBetweenTwoTokens);
        }

        public OptionSet OptionSet
        {
            get { return this.engine.OptionSet; }
        }

        public TreeData TreeData
        {
            get { return this.engine.TreeData; }
        }

        public TokenStream TokenStream
        {
            get { return this.tokenStream; }
        }
    }
}
