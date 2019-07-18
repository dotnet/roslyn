// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
        private readonly AbstractFormatEngine _engine;
        private readonly TokenStream _tokenStream;

        // interval tree for inseparable regions (Span to indentation data)
        // due to dependencies, each region defined in the data can't be formatted independently.
        private readonly ContextIntervalTree<RelativeIndentationData> _relativeIndentationTree;

        // interval tree for each operations.
        // given a span in the tree, it returns data (indentation, anchor delta, etc) to be applied for the span
        private readonly ContextIntervalTree<IndentationData> _indentationTree;
        private readonly ContextIntervalTree<SuppressWrappingData> _suppressWrappingTree;
        private readonly ContextIntervalTree<SuppressSpacingData> _suppressSpacingTree;
        private readonly ContextIntervalTree<AnchorData> _anchorTree;

        // anchor token to anchor data map.
        // unlike anchorTree that would return anchor data for given span in the tree, it will return
        // anchorData based on key which is anchor token.
        private readonly Dictionary<SyntaxToken, AnchorData> _anchorBaseTokenMap;

        // hashset to prevent duplicate entries in the trees.
        private readonly HashSet<TextSpan> _indentationMap;
        private readonly HashSet<TextSpan> _suppressWrappingMap;
        private readonly HashSet<TextSpan> _suppressSpacingMap;
        private readonly HashSet<TextSpan> _anchorMap;

        // used for selection based formatting case. it contains operations that will define
        // what indentation to use as a starting indentation. (we always use 0 for formatting whole tree case)
        private List<IndentBlockOperation> _initialIndentBlockOperations;

        private readonly string _language;

        public FormattingContext(AbstractFormatEngine engine, TokenStream tokenStream, string language)
        {
            Contract.ThrowIfNull(engine);
            Contract.ThrowIfNull(tokenStream);

            _engine = engine;
            _tokenStream = tokenStream;
            _language = language;

            _relativeIndentationTree = new ContextIntervalTree<RelativeIndentationData>(this);

            _indentationTree = new ContextIntervalTree<IndentationData>(this);
            _suppressWrappingTree = new ContextIntervalTree<SuppressWrappingData>(SuppressIntervalIntrospector.Instance);
            _suppressSpacingTree = new ContextIntervalTree<SuppressSpacingData>(SuppressIntervalIntrospector.Instance);
            _anchorTree = new ContextIntervalTree<AnchorData>(this);

            _anchorBaseTokenMap = new Dictionary<SyntaxToken, AnchorData>();

            _indentationMap = new HashSet<TextSpan>();
            _suppressWrappingMap = new HashSet<TextSpan>();
            _suppressSpacingMap = new HashSet<TextSpan>();
            _anchorMap = new HashSet<TextSpan>();

            _initialIndentBlockOperations = new List<IndentBlockOperation>();
        }

        public void Initialize(
            ChainedFormattingRules formattingRules,
            SyntaxToken startToken,
            SyntaxToken endToken,
            CancellationToken cancellationToken)
        {
            var rootNode = this.TreeData.Root;
            if (_tokenStream.IsFormattingWholeDocument)
            {
                // if we are trying to format whole document, there is no reason to get initial context. just set
                // initial indentation.
                var data = new RootIndentationData(rootNode);
                _indentationTree.AddIntervalInPlace(data);
                _indentationMap.Add(data.TextSpan);
                return;
            }

            var initialContextFinder = new InitialContextFinder(_tokenStream, formattingRules, rootNode);
            var (indentOperations, suppressOperations) = initialContextFinder.Do(startToken, endToken);

            if (indentOperations != null)
            {
                var indentationOperations = indentOperations;

                var initialOperation = indentationOperations[0];
                var baseIndentationFinder = new BottomUpBaseIndentationFinder(
                                                formattingRules,
                                                this.OptionSet.GetOption(FormattingOptions.TabSize, _language),
                                                this.OptionSet.GetOption(FormattingOptions.IndentationSize, _language),
                                                _tokenStream);
                var initialIndentation = baseIndentationFinder.GetIndentationOfCurrentPosition(
                    rootNode,
                    initialOperation,
                    t => _tokenStream.GetCurrentColumn(t), cancellationToken);

                var data = new SimpleIndentationData(initialOperation.TextSpan, initialIndentation);
                _indentationTree.AddIntervalInPlace(data);
                _indentationMap.Add(data.TextSpan);

                // hold onto initial operations
                _initialIndentBlockOperations = indentationOperations;
            }

            suppressOperations?.Do(o => this.AddInitialSuppressOperation(o));
        }

        public void AddIndentBlockOperations(
            List<IndentBlockOperation> operations,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(operations);

            // if there is no initial block operations
            if (_initialIndentBlockOperations.Count <= 0)
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

            var baseSpan = _initialIndentBlockOperations[0].TextSpan;

            // indentation tree must build up from inputs that are in right order except initial indentation.
            // merge indentation operations from two places (initial operations for current selection, and nodes inside of selections)
            // sort it in right order and apply them to tree
            var count = _initialIndentBlockOperations.Count - 1 + operations.Count;
            var mergedList = new List<IndentBlockOperation>(count);

            // initial operations are already sorted, just add, no need to filter
            for (var i = 1; i < _initialIndentBlockOperations.Count; i++)
            {
                mergedList.Add(_initialIndentBlockOperations[i]);
            }

            for (var i = 0; i < operations.Count; i++)
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
                _indentationMap.Contains(intervalTreeSpan))
            {
                return;
            }

            // relative indentation case where indentation depends on other token
            if (operation.IsRelativeIndentation)
            {
                var inseparableRegionStartingPosition = operation.Option.IsOn(IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine) ? _tokenStream.FirstTokenOfBaseTokenLine(operation.BaseToken).FullSpan.Start : operation.BaseToken.FullSpan.Start;
                var relativeIndentationGetter = new Lazy<int>(() =>
                {
                    var indentationDelta = operation.IndentationDeltaOrPosition * this.OptionSet.GetOption(FormattingOptions.IndentationSize, _language);

                    // baseIndentation is calculated for the adjusted token if option is RelativeToFirstTokenOnBaseTokenLine
                    var baseIndentation = _tokenStream.GetCurrentColumn(operation.Option.IsOn(IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine) ?
                                                                            _tokenStream.FirstTokenOfBaseTokenLine(operation.BaseToken) :
                                                                            operation.BaseToken);

                    return baseIndentation + indentationDelta;
                }, isThreadSafe: true);

                // set new indentation
                var relativeIndentationData = new RelativeIndentationData(inseparableRegionStartingPosition, intervalTreeSpan, operation, relativeIndentationGetter);

                _indentationTree.AddIntervalInPlace(relativeIndentationData);
                _relativeIndentationTree.AddIntervalInPlace(relativeIndentationData);
                _indentationMap.Add(intervalTreeSpan);

                return;
            }

            // absolute position case
            if (operation.Option.IsOn(IndentBlockOption.AbsolutePosition))
            {
                _indentationTree.AddIntervalInPlace(new SimpleIndentationData(intervalTreeSpan, operation.IndentationDeltaOrPosition));
                _indentationMap.Add(intervalTreeSpan);
                return;
            }

            // regular indentation case where indentation is based on its previous indentation
            var indentationData = _indentationTree.GetSmallestContainingInterval(operation.TextSpan.Start, 0);
            if (indentationData == null)
            {
                // no previous indentation
                var indentation = operation.IndentationDeltaOrPosition * this.OptionSet.GetOption(FormattingOptions.IndentationSize, _language);
                _indentationTree.AddIntervalInPlace(new SimpleIndentationData(intervalTreeSpan, indentation));
                _indentationMap.Add(intervalTreeSpan);
                return;
            }

            // get indentation based on its previous indentation
            var indentationGetter = new Lazy<int>(() =>
            {
                var indentationDelta = operation.IndentationDeltaOrPosition * this.OptionSet.GetOption(FormattingOptions.IndentationSize, _language);

                return indentationData.Indentation + indentationDelta;
            }, isThreadSafe: true);

            // set new indentation
            _indentationTree.AddIntervalInPlace(new LazyIndentationData(intervalTreeSpan, indentationGetter));
            _indentationMap.Add(intervalTreeSpan);
        }

        public void AddInitialSuppressOperation(SuppressOperation operation)
        {
            // don't add stuff if it is empty
            if (operation == null || operation.TextSpan.IsEmpty)
            {
                return;
            }

            var onSameLine = _tokenStream.TwoTokensOriginallyOnSameLine(operation.StartToken, operation.EndToken);
            AddSuppressOperation(operation, onSameLine);
        }

        public void AddSuppressOperations(
            List<SuppressOperation> operations,
            CancellationToken cancellationToken)
        {
            var valuePairs = new (SuppressOperation operation, bool shouldSuppress, bool onSameLine)[operations.Count];

            // TODO: think about a way to figure out whether it is already suppressed and skip the expensive check below.
            for (var i = 0; i < operations.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var operation = operations[i];

                // if an operation contains elastic trivia itself and the operation is not marked to ignore the elastic trivia 
                // ignore the operation 
                if (operation.ContainsElasticTrivia(_tokenStream) && !operation.Option.IsOn(SuppressOption.IgnoreElasticWrapping))
                {
                    // don't bother to calculate line alignment between tokens 
                    valuePairs[i] = (operation, shouldSuppress: false, onSameLine: false);
                    continue;
                }

                var onSameLine = _tokenStream.TwoTokensOriginallyOnSameLine(operation.StartToken, operation.EndToken);
                valuePairs[i] = (operation, shouldSuppress: true, onSameLine);
            }

            valuePairs.Do(v =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (v.shouldSuppress)
                {
                    AddSuppressOperation(v.operation, v.onSameLine);
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
            if (!option.IsMaskOn(SuppressOption.NoSpacing) || _suppressSpacingMap.Contains(operation.TextSpan))
            {
                return;
            }

            if (!(option.IsOn(SuppressOption.NoSpacingIfOnSingleLine) && twoTokensOnSameLine) &&
                !(option.IsOn(SuppressOption.NoSpacingIfOnMultipleLine) && !twoTokensOnSameLine))
            {
                return;
            }

            var data = new SuppressSpacingData(operation.TextSpan);

            _suppressSpacingMap.Add(operation.TextSpan);
            _suppressSpacingTree.AddIntervalInPlace(data);
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
            if (!option.IsMaskOn(SuppressOption.NoWrapping) || _suppressWrappingMap.Contains(operation.TextSpan))
            {
                return;
            }

            if (!(option.IsOn(SuppressOption.NoWrappingIfOnSingleLine) && twoTokensOnSameLine) &&
                !(option.IsOn(SuppressOption.NoWrappingIfOnMultipleLine) && !twoTokensOnSameLine))
            {
                return;
            }

            var ignoreElastic = option.IsMaskOn(SuppressOption.IgnoreElasticWrapping) ||
                                !operation.ContainsElasticTrivia(_tokenStream);

            var data = new SuppressWrappingData(operation.TextSpan, ignoreElastic: ignoreElastic);

            _suppressWrappingMap.Add(operation.TextSpan);
            _suppressWrappingTree.AddIntervalInPlace(data);
        }

        public void AddAnchorIndentationOperation(AnchorIndentationOperation operation)
        {
            // don't add stuff if it is empty
            if (operation.TextSpan.IsEmpty ||
                _anchorMap.Contains(operation.TextSpan) ||
                _anchorBaseTokenMap.ContainsKey(operation.AnchorToken))
            {
                return;
            }

            var originalSpace = _tokenStream.GetOriginalColumn(operation.StartToken);
            var data = new AnchorData(operation, originalSpace);

            _anchorTree.AddIntervalInPlace(data);

            _anchorBaseTokenMap.Add(operation.AnchorToken, data);
            _anchorMap.Add(operation.TextSpan);
        }

        [Conditional("DEBUG")]
        private void DebugCheckEmpty<T>(ContextIntervalTree<T> tree, TextSpan textSpan)
        {
            var intervals = tree.GetIntervalsThatContain(textSpan.Start, textSpan.Length);
            Contract.ThrowIfFalse(intervals.Length == 0);
        }

        public int GetBaseIndentation(SyntaxToken token)
        {
            return GetBaseIndentation(token.SpanStart);
        }

        public int GetBaseIndentation(int position)
        {
            var indentationData = _indentationTree.GetSmallestContainingInterval(position, 0);
            if (indentationData == null)
            {
                DebugCheckEmpty(_indentationTree, new TextSpan(position, 0));
                return 0;
            }

            return indentationData.Indentation;
        }

        public IEnumerable<IndentBlockOperation> GetAllRelativeIndentBlockOperations()
        {
            return _relativeIndentationTree.GetIntervalsThatIntersectWith(this.TreeData.StartPosition, this.TreeData.EndPosition, this).Select(i => i.Operation);
        }

        public bool TryGetEndTokenForRelativeIndentationSpan(SyntaxToken token, int maxChainDepth, out SyntaxToken endToken, CancellationToken cancellationToken)
        {
            endToken = default;

            var depth = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (depth++ > maxChainDepth)
                {
                    return false;
                }

                var span = token.Span;
                var indentationData = _relativeIndentationTree.GetSmallestContainingInterval(span.Start, 0);
                if (indentationData == null)
                {
                    // this means the given token is not inside of inseparable regions
                    endToken = token;
                    return true;
                }

                // recursively find the end token outside of inseparable regions
                token = indentationData.EndToken.GetNextToken(includeZeroWidth: true);
                if (token.RawKind == 0)
                {
                    // reached end of tree
                    return true;
                }
            }
        }

        private AnchorData GetAnchorData(SyntaxToken token)
        {
            var span = token.Span;

            var anchorData = _anchorTree.GetSmallestContainingInterval(span.Start, 0);
            if (anchorData == null)
            {
                // no anchor
                DebugCheckEmpty(_anchorTree, new TextSpan(span.Start, 0));
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

            var currentColumn = _tokenStream.GetCurrentColumn(anchorData.AnchorToken);
            return currentColumn - anchorData.OriginalColumn;
        }

        public SyntaxToken GetAnchorToken(SyntaxToken token)
        {
            var anchorData = GetAnchorData(token);
            if (anchorData == null)
            {
                return default;
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

            var currentColumn = _tokenStream.GetCurrentColumn(token);
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
                return default;
            }

            // our anchor operation is very flexible so it not only let one anchor to contain others, it also
            // let anchors to overlap each other for whatever reasons
            // below, we will try to flat the overlapped anchor span, and find the last position (token) of that span

            // find other anchors overlapping with current anchor span
            var anchorData = _anchorTree.GetIntervalsThatOverlapWith(baseAnchorData.TextSpan.Start, baseAnchorData.TextSpan.Length);

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
                if (_anchorBaseTokenMap.TryGetValue(tokenData.Token, out var tempAnchorData))
                {
                    lastBaseAnchorData = tempAnchorData;
                }

                // tokenPairIndex is always 0 <= ... < TokenCount - 1
                var tokenPairIndex = tokenData.IndexInStream;
                if (_tokenStream.TokenCount - 1 <= tokenPairIndex ||
                    _tokenStream.GetTriviaData(tokenPairIndex).SecondTokenIsFirstTokenOnLine)
                {
                    return lastBaseAnchorData;
                }

                tokenData = tokenData.GetNextTokenData();
            }

            return lastBaseAnchorData;
        }

        public bool IsWrappingSuppressed(TextSpan textSpan, bool containsElasticTrivia)
        {
            // use edge exclusive version of GetSmallestContainingInterval
            var data = _suppressWrappingTree.GetSmallestEdgeExclusivelyContainingInterval(textSpan.Start, textSpan.Length);
            if (data == null)
            {
                return false;
            }

            if (containsElasticTrivia && !data.IgnoreElastic)
            {
                return false;
            }

            return true;
        }

        public bool IsSpacingSuppressed(TextSpan textSpan, bool containsElasticTrivia)
        {
            // For spaces, never ignore elastic trivia because that can 
            // generate incorrect code
            if (containsElasticTrivia)
            {
                return false;
            }

            // use edge exclusive version of GetSmallestContainingInterval
            var data = _suppressSpacingTree.GetSmallestEdgeExclusivelyContainingInterval(textSpan.Start, textSpan.Length);
            if (data == null)
            {
                return false;
            }

            return true;
        }

        public bool IsSpacingSuppressed(int pairIndex)
        {
            var token1 = _tokenStream.GetToken(pairIndex);
            var token2 = _tokenStream.GetToken(pairIndex + 1);

            var spanBetweenTwoTokens = TextSpan.FromBounds(token1.SpanStart, token2.Span.End);

            // this version of SpacingSuppressed will be called after all basic space operations are done. 
            // so no more elastic trivia should have left out
            return IsSpacingSuppressed(spanBetweenTwoTokens, containsElasticTrivia: false);
        }

        public OptionSet OptionSet => _engine.OptionSet;

        public TreeData TreeData => _engine.TreeData;

        public TokenStream TokenStream => _tokenStream;
    }
}
