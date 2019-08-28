// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    // TODO : two alternative design possible for formatting engine
    //
    //        1. use AAL (TPL Dataflow) in .NET 4.5 to run things concurrently in sequential order
    //           * this has a problem of the new TPL lib being not released yet and possibility of not using all cores.
    //
    //        2. create dependency graph between operations, and format them in topological order and 
    //           run chunks that don't have dependency in parallel (kirill's idea)
    //           * this requires defining dependencies on each operations. can't use dependency between tokens since
    //             that would create too big graph. key for this approach is how to reduce size of graph.
    internal abstract partial class AbstractFormatEngine
    {
        private readonly ChainedFormattingRules _formattingRules;

        private readonly SyntaxNode _commonRoot;
        private readonly SyntaxToken _token1;
        private readonly SyntaxToken _token2;
        private readonly string _language;

        protected readonly TextSpan SpanToFormat;

        internal readonly OptionSet OptionSet;
        internal readonly TreeData TreeData;

        public AbstractFormatEngine(
            TreeData treeData,
            OptionSet optionSet,
            IEnumerable<AbstractFormattingRule> formattingRules,
            SyntaxToken token1,
            SyntaxToken token2)
            : this(
                  treeData,
                  optionSet,
                  new ChainedFormattingRules(formattingRules, optionSet),
                  token1,
                  token2)
        {
        }

        internal AbstractFormatEngine(
            TreeData treeData,
            OptionSet optionSet,
            ChainedFormattingRules formattingRules,
            SyntaxToken token1,
            SyntaxToken token2)
        {
            Contract.ThrowIfNull(optionSet);
            Contract.ThrowIfNull(treeData);
            Contract.ThrowIfNull(formattingRules);

            Contract.ThrowIfTrue(treeData.Root.IsInvalidTokenRange(token1, token2));

            this.OptionSet = optionSet;
            this.TreeData = treeData;
            _formattingRules = formattingRules;

            _token1 = token1;
            _token2 = token2;

            // get span and common root
            this.SpanToFormat = GetSpanToFormat();
            _commonRoot = token1.GetCommonRoot(token2);
            if (token1 == default)
            {
                _language = token2.Language;
            }
            else
            {
                _language = token1.Language;
            }
        }

        protected abstract AbstractTriviaDataFactory CreateTriviaFactory();
        protected abstract AbstractFormattingResult CreateFormattingResult(TokenStream tokenStream);

        public AbstractFormattingResult Format(CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Formatting_Format, FormatSummary, cancellationToken))
            {
                // setup environment
                var nodeOperations = CreateNodeOperations(cancellationToken);

                var tokenStream = new TokenStream(this.TreeData, this.OptionSet, this.SpanToFormat, CreateTriviaFactory());
                var tokenOperation = CreateTokenOperation(tokenStream, cancellationToken);

                // initialize context
                var context = CreateFormattingContext(tokenStream, cancellationToken);

                // start anchor task that will be used later
                cancellationToken.ThrowIfCancellationRequested();
                var anchorContext = nodeOperations.AnchorIndentationOperations.Do(context.AddAnchorIndentationOperation);

                BuildContext(context, nodeOperations, cancellationToken);

                ApplyBeginningOfTreeTriviaOperation(context, cancellationToken);

                ApplyTokenOperations(context, nodeOperations,
                    tokenOperation, cancellationToken);

                ApplyTriviaOperations(context, cancellationToken);

                ApplyEndOfTreeTriviaOperation(context, cancellationToken);

                return CreateFormattingResult(tokenStream);
            }
        }

        protected virtual FormattingContext CreateFormattingContext(TokenStream tokenStream, CancellationToken cancellationToken)
        {
            // initialize context
            var context = new FormattingContext(this, tokenStream, _language);
            context.Initialize(_formattingRules, _token1, _token2, cancellationToken);

            return context;
        }

        protected virtual NodeOperations CreateNodeOperations(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // iterating tree is very expensive. do it once and cache it to list
            List<SyntaxNode> nodeIterator;
            using (Logger.LogBlock(FunctionId.Formatting_IterateNodes, cancellationToken))
            {
                const int magicLengthToNodesRatio = 5;
                var result = new List<SyntaxNode>(Math.Max(this.SpanToFormat.Length / magicLengthToNodesRatio, 4));

                foreach (var node in _commonRoot.DescendantNodesAndSelf(this.SpanToFormat))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    result.Add(node);
                }

                nodeIterator = result;
            }

            // iterate through each operation using index to not create any unnecessary object
            cancellationToken.ThrowIfCancellationRequested();
            List<IndentBlockOperation> indentBlockOperation;
            using (Logger.LogBlock(FunctionId.Formatting_CollectIndentBlock, cancellationToken))
            {
                indentBlockOperation = AddOperations<IndentBlockOperation>(nodeIterator, (l, n) => _formattingRules.AddIndentBlockOperations(l, n), cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            List<SuppressOperation> suppressOperation;
            using (Logger.LogBlock(FunctionId.Formatting_CollectSuppressOperation, cancellationToken))
            {
                suppressOperation = AddOperations<SuppressOperation>(nodeIterator, (l, n) => _formattingRules.AddSuppressOperations(l, n), cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            List<AlignTokensOperation> alignmentOperation;
            using (Logger.LogBlock(FunctionId.Formatting_CollectAlignOperation, cancellationToken))
            {
                var operations = AddOperations<AlignTokensOperation>(nodeIterator, (l, n) => _formattingRules.AddAlignTokensOperations(l, n), cancellationToken);

                // make sure we order align operation from left to right
                operations.Sort((o1, o2) => o1.BaseToken.Span.CompareTo(o2.BaseToken.Span));

                alignmentOperation = operations;
            }

            cancellationToken.ThrowIfCancellationRequested();
            List<AnchorIndentationOperation> anchorIndentationOperations;
            using (Logger.LogBlock(FunctionId.Formatting_CollectAnchorOperation, cancellationToken))
            {
                anchorIndentationOperations = AddOperations<AnchorIndentationOperation>(nodeIterator, (l, n) => _formattingRules.AddAnchorIndentationOperations(l, n), cancellationToken);
            }

            return new NodeOperations(indentBlockOperation, suppressOperation, anchorIndentationOperations, alignmentOperation);
        }

        private List<T> AddOperations<T>(List<SyntaxNode> nodes, Action<List<T>, SyntaxNode> addOperations, CancellationToken cancellationToken)
        {
            var operations = new List<T>();
            var list = new List<T>();

            foreach (var n in nodes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                addOperations(list, n);

                list.RemoveAll(item => item == null);
                operations.AddRange(list);
                list.Clear();
            }

            return operations;
        }

        private TokenPairWithOperations[] CreateTokenOperation(
            TokenStream tokenStream,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (Logger.LogBlock(FunctionId.Formatting_CollectTokenOperation, cancellationToken))
            {
                // pre-allocate list once. this is cheaper than re-adjusting list as items are added.
                var list = new TokenPairWithOperations[tokenStream.TokenCount - 1];

                foreach (var pair in tokenStream.TokenIterator)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var spaceOperation = _formattingRules.GetAdjustSpacesOperation(pair.Item2, pair.Item3);
                    var lineOperation = _formattingRules.GetAdjustNewLinesOperation(pair.Item2, pair.Item3);

                    list[pair.Item1] = new TokenPairWithOperations(tokenStream, pair.Item1, spaceOperation, lineOperation);
                }

                return list;
            }
        }

        private void ApplyTokenOperations(
            FormattingContext context,
            NodeOperations nodeOperations,
            TokenPairWithOperations[] tokenOperations,
            CancellationToken cancellationToken)
        {
            var applier = new OperationApplier(context, _formattingRules);
            ApplySpaceAndWrappingOperations(context, tokenOperations, applier, cancellationToken);

            ApplyAnchorOperations(context, tokenOperations, applier, cancellationToken);

            ApplySpecialOperations(context, nodeOperations, applier, cancellationToken);
        }

        private void ApplyBeginningOfTreeTriviaOperation(
            FormattingContext context, CancellationToken cancellationToken)
        {
            if (!context.TokenStream.FormatBeginningOfTree)
            {
                return;
            }

            // remove all leading indentation
            var triviaInfo = context.TokenStream.GetTriviaDataAtBeginningOfTree().WithIndentation(0, context, _formattingRules, cancellationToken);

            triviaInfo.Format(context, _formattingRules, BeginningOfTreeTriviaInfoApplier, cancellationToken);

            return;

            // local functions
            static void BeginningOfTreeTriviaInfoApplier(int i, TokenStream ts, TriviaData info)
                => ts.ApplyBeginningOfTreeChange(info);
        }

        private void ApplyEndOfTreeTriviaOperation(
            FormattingContext context, CancellationToken cancellationToken)
        {
            if (!context.TokenStream.FormatEndOfTree)
            {
                return;
            }

            // remove all trailing indentation
            var triviaInfo = context.TokenStream.GetTriviaDataAtEndOfTree().WithIndentation(0, context, _formattingRules, cancellationToken);

            triviaInfo.Format(context, _formattingRules, EndOfTreeTriviaInfoApplier, cancellationToken);

            return;

            // local functions
            static void EndOfTreeTriviaInfoApplier(int i, TokenStream ts, TriviaData info)
                => ts.ApplyEndOfTreeChange(info);
        }

        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/30819", AllowCaptures = false)]
        private void ApplyTriviaOperations(FormattingContext context, CancellationToken cancellationToken)
        {
            for (var i = 0; i < context.TokenStream.TokenCount - 1; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                TriviaFormatter(i, context, _formattingRules, cancellationToken);
            }

            return;

            // local functions

            static void RegularApplier(int tokenPairIndex, TokenStream ts, TriviaData info)
                => ts.ApplyChange(tokenPairIndex, info);

            static void TriviaFormatter(int tokenPairIndex, FormattingContext ctx, ChainedFormattingRules formattingRules, CancellationToken ct)
            {
                var triviaInfo = ctx.TokenStream.GetTriviaData(tokenPairIndex);
                triviaInfo.Format(
                    ctx,
                    formattingRules,
                    (tokenPairIndex1, ts, info) => RegularApplier(tokenPairIndex1, ts, info),
                    ct,
                    tokenPairIndex);
            }
        }

        private TextSpan GetSpanToFormat()
        {
            var startPosition = this.TreeData.IsFirstToken(_token1) ? this.TreeData.StartPosition : _token1.SpanStart;
            var endPosition = this.TreeData.IsLastToken(_token2) ? this.TreeData.EndPosition : _token2.Span.End;

            return TextSpan.FromBounds(startPosition, endPosition);
        }

        private void ApplySpecialOperations(
            FormattingContext context, NodeOperations nodeOperationsCollector, OperationApplier applier, CancellationToken cancellationToken)
        {
            // apply alignment operation
            using (Logger.LogBlock(FunctionId.Formatting_CollectAlignOperation, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                // TODO : figure out a way to run alignment operations in parallel. probably find
                // unions and run each chunk in separate tasks
                var previousChangesMap = new Dictionary<SyntaxToken, int>();
                var alignmentOperations = nodeOperationsCollector.AlignmentOperation;

                alignmentOperations.Do(operation =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    applier.ApplyAlignment(operation, previousChangesMap, cancellationToken);
                });

                // go through all relative indent block operation, and see whether it is affected by previous operations
                context.GetAllRelativeIndentBlockOperations().Do(o =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    applier.ApplyBaseTokenIndentationChangesFromTo(FindCorrectBaseTokenOfRelativeIndentBlockOperation(o, context.TokenStream), o.StartToken, o.EndToken, previousChangesMap, cancellationToken);
                });
            }
        }

        private void ApplyAnchorOperations(
            FormattingContext context,
            TokenPairWithOperations[] tokenOperations,
            OperationApplier applier,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Formatting_ApplyAnchorOperation, cancellationToken))
            {
                // TODO: find out a way to apply anchor operation concurrently if possible
                var previousChangesMap = new Dictionary<SyntaxToken, int>();
                foreach (var p in tokenOperations)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!AnchorOperationCandidate(p))
                    {
                        continue;
                    }

                    var pairIndex = p.PairIndex;
                    applier.ApplyAnchorIndentation(pairIndex, previousChangesMap, cancellationToken);
                }

                // go through all relative indent block operation, and see whether it is affected by the anchor operation
                context.GetAllRelativeIndentBlockOperations().Do(o =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    applier.ApplyBaseTokenIndentationChangesFromTo(FindCorrectBaseTokenOfRelativeIndentBlockOperation(o, context.TokenStream), o.StartToken, o.EndToken, previousChangesMap, cancellationToken);
                });
            }
        }

        private static bool AnchorOperationCandidate(TokenPairWithOperations pair)
        {
            if (pair.LineOperation == null)
            {
                return pair.TokenStream.GetTriviaData(pair.PairIndex).SecondTokenIsFirstTokenOnLine;
            }

            if (pair.LineOperation.Option == AdjustNewLinesOption.ForceLinesIfOnSingleLine)
            {
                return !pair.TokenStream.TwoTokensOriginallyOnSameLine(pair.Token1, pair.Token2) &&
                        pair.TokenStream.GetTriviaData(pair.PairIndex).SecondTokenIsFirstTokenOnLine;
            }

            return false;
        }

        private SyntaxToken FindCorrectBaseTokenOfRelativeIndentBlockOperation(IndentBlockOperation operation, TokenStream tokenStream)
        {
            if (operation.Option.IsOn(IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine))
            {
                return tokenStream.FirstTokenOfBaseTokenLine(operation.BaseToken);
            }

            return operation.BaseToken;
        }

        private void ApplySpaceAndWrappingOperations(
            FormattingContext context,
            TokenPairWithOperations[] tokenOperations,
            OperationApplier applier,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Formatting_ApplySpaceAndLine, cancellationToken))
            {
                // go through each token pairs and apply operations. operations don't need to be applied in order
                var partitioner = new Partitioner(context, tokenOperations);

                // always create task 1 more than current processor count
                var partitions = partitioner.GetPartitions(partitionCount: 1, cancellationToken);

                foreach (var partition in partitions)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    partition.Do(operationPair => ApplySpaceAndWrappingOperationsBody(context, operationPair, applier, cancellationToken));
                }
            }
        }

        private static void ApplySpaceAndWrappingOperationsBody(
            FormattingContext context,
            TokenPairWithOperations operation,
            OperationApplier applier,
            CancellationToken cancellationToken)
        {
            var token1 = operation.Token1;
            var token2 = operation.Token2;

            // check whether one of tokens is missing (which means syntax error exist around two tokens)
            // in error case, we leave code as user wrote
            if (token1.IsMissing || token2.IsMissing)
            {
                return;
            }

            var triviaInfo = context.TokenStream.GetTriviaData(operation.PairIndex);
            var spanBetweenTokens = TextSpan.FromBounds(token1.Span.End, token2.SpanStart);

            if (operation.LineOperation != null)
            {
                if (!context.IsWrappingSuppressed(spanBetweenTokens, triviaInfo.TreatAsElastic))
                {
                    // TODO : need to revisit later for the case where line and space operations
                    // are conflicting each other by forcing new lines and removing new lines.
                    //
                    // if wrapping operation applied, no need to run any other operation
                    if (applier.Apply(operation.LineOperation, operation.PairIndex, cancellationToken))
                    {
                        return;
                    }
                }
            }

            if (operation.SpaceOperation != null)
            {
                if (!context.IsSpacingSuppressed(spanBetweenTokens, triviaInfo.TreatAsElastic))
                {
                    applier.Apply(operation.SpaceOperation, operation.PairIndex);
                }
            }
        }

        private void BuildContext(
            FormattingContext context,
            NodeOperations nodeOperations,
            CancellationToken cancellationToken)
        {
            // add scope operation (run each kind sequentially)
            using (Logger.LogBlock(FunctionId.Formatting_BuildContext, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                context.AddIndentBlockOperations(nodeOperations.IndentBlockOperation, cancellationToken);
                context.AddSuppressOperations(nodeOperations.SuppressOperation, cancellationToken);
            }
        }

        /// <summary>
        /// return summary for current formatting work
        /// </summary>
        private string FormatSummary()
        {
            return string.Format("({0}) ({1} - {2})",
                this.SpanToFormat,
                _token1.ToString().Replace("\r\n", "\\r\\n"),
                _token2.ToString().Replace("\r\n", "\\r\\n"));
        }
    }
}
