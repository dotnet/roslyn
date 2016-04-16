// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
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
        private const int ConcurrentThreshold = 30000;

        private readonly ChainedFormattingRules _formattingRules;

        private readonly SyntaxNode _commonRoot;
        private readonly SyntaxToken _token1;
        private readonly SyntaxToken _token2;
        private readonly string _language;

        protected readonly TextSpan SpanToFormat;

        internal readonly TaskExecutor TaskExecutor;
        internal readonly OptionSet OptionSet;
        internal readonly TreeData TreeData;

        public AbstractFormatEngine(
            TreeData treeData,
            OptionSet optionSet,
            IEnumerable<IFormattingRule> formattingRules,
            SyntaxToken token1,
            SyntaxToken token2,
            TaskExecutor executor)
            : this(
                  treeData,
                  optionSet,
                  new ChainedFormattingRules(formattingRules, optionSet),
                  token1,
                  token2,
                  executor)
        {
        }

        internal AbstractFormatEngine(
            TreeData treeData,
            OptionSet optionSet,
            ChainedFormattingRules formattingRules,
            SyntaxToken token1,
            SyntaxToken token2,
            TaskExecutor executor)
        {
            Contract.ThrowIfNull(optionSet);
            Contract.ThrowIfNull(treeData);
            Contract.ThrowIfNull(formattingRules);
            Contract.ThrowIfNull(executor);

            Contract.ThrowIfTrue(treeData.Root.IsInvalidTokenRange(token1, token2));

            this.OptionSet = optionSet;
            this.TreeData = treeData;
            _formattingRules = formattingRules;

            _token1 = token1;
            _token2 = token2;

            // get span and common root
            this.SpanToFormat = GetSpanToFormat();
            _commonRoot = token1.GetCommonRoot(token2);
            if (token1 == default(SyntaxToken))
            {
                _language = token2.Language;
            }
            else
            {
                _language = token1.Language;
            }

            // set synchronous task executor if it is debug mode or if there is not many things to format
            this.TaskExecutor = optionSet.GetOption(FormattingOptions.DebugMode, _language) ? TaskExecutor.Synchronous :
                                    (SpanToFormat.Length < ConcurrentThreshold) ? TaskExecutor.Synchronous : executor;
        }

        protected abstract AbstractTriviaDataFactory CreateTriviaFactory();
        protected abstract AbstractFormattingResult CreateFormattingResult(TokenStream tokenStream);

        public async Task<AbstractFormattingResult> FormatAsync(CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Formatting_Format, FormatSummary, cancellationToken))
            {
                // setup environment
                var nodeOperations = CreateNodeOperationTasks(cancellationToken);

                var tokenStream = new TokenStream(this.TreeData, this.OptionSet, this.SpanToFormat, CreateTriviaFactory());
                var tokenOperationTask = CreateTokenOperationTask(tokenStream, cancellationToken);

                // initialize context
                var context = CreateFormattingContext(tokenStream, cancellationToken);

                // start anchor task that will be used later
                var anchorContextTask = TaskExecutor.ContinueWith(
                        nodeOperations.AnchorIndentationOperationsTask,
                        task => task.Result.Do(context.AddAnchorIndentationOperation),
                        cancellationToken);

                BuildContext(context, tokenStream, nodeOperations, cancellationToken);

                ApplyBeginningOfTreeTriviaOperation(context, tokenStream, cancellationToken);

                await ApplyTokenOperationsAsync(context, tokenStream, anchorContextTask, nodeOperations,
                    await tokenOperationTask.ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

                ApplyTriviaOperations(context, tokenStream, cancellationToken);

                ApplyEndOfTreeTriviaOperation(context, tokenStream, cancellationToken);

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

        protected virtual NodeOperations CreateNodeOperationTasks(CancellationToken cancellationToken)
        {
            // iterating tree is very expensive. do it once and cache it to list
            var nodeIteratorTask = this.TaskExecutor.StartNew(() =>
            {
                using (Logger.LogBlock(FunctionId.Formatting_IterateNodes, cancellationToken))
                {
                    const int magicLengthToNodesRatio = 5;
                    var result = new List<SyntaxNode>(Math.Max(this.SpanToFormat.Length / magicLengthToNodesRatio, 4));

                    foreach (var node in _commonRoot.DescendantNodesAndSelf(this.SpanToFormat))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        result.Add(node);
                    }

                    return result;
                }
            },
            cancellationToken);

            // iterate through each operation using index to not create any unnecessary object
            var indentBlockOperationTask = this.TaskExecutor.ContinueWith(nodeIteratorTask, task =>
            {
                using (Logger.LogBlock(FunctionId.Formatting_CollectIndentBlock, cancellationToken))
                {
                    return AddOperations<IndentBlockOperation>(task.Result, (l, n) => _formattingRules.AddIndentBlockOperations(l, n, _token2), cancellationToken);
                }
            },
            cancellationToken);

            var suppressOperationTask = this.TaskExecutor.ContinueWith(nodeIteratorTask, task =>
            {
                using (Logger.LogBlock(FunctionId.Formatting_CollectSuppressOperation, cancellationToken))
                {
                    return AddOperations<SuppressOperation>(task.Result, (l, n) => _formattingRules.AddSuppressOperations(l, n, _token2), cancellationToken);
                }
            },
            cancellationToken);

            var alignmentOperationTask = this.TaskExecutor.ContinueWith(nodeIteratorTask, task =>
            {
                using (Logger.LogBlock(FunctionId.Formatting_CollectAlignOperation, cancellationToken))
                {
                    var operations = AddOperations<AlignTokensOperation>(task.Result, (l, n) => _formattingRules.AddAlignTokensOperations(l, n, _token2), cancellationToken);

                    // make sure we order align operation from left to right
                    operations.Sort((o1, o2) => o1.BaseToken.Span.CompareTo(o2.BaseToken.Span));

                    return operations;
                }
            },
            cancellationToken);

            var anchorIndentationOperationsTask = this.TaskExecutor.ContinueWith(nodeIteratorTask, task =>
            {
                using (Logger.LogBlock(FunctionId.Formatting_CollectAnchorOperation, cancellationToken))
                {
                    return AddOperations<AnchorIndentationOperation>(task.Result, (l, n) => _formattingRules.AddAnchorIndentationOperations(l, n, _token2), cancellationToken);
                }
            },
            cancellationToken);

            return new NodeOperations(indentBlockOperationTask, suppressOperationTask, anchorIndentationOperationsTask, alignmentOperationTask);
        }

        private List<T> AddOperations<T>(List<SyntaxNode> nodes, Action<List<T>, SyntaxNode> addOperations, CancellationToken cancellationToken)
        {
            using (var localOperations = new ThreadLocal<List<T>>(() => new List<T>(), trackAllValues: true))
            using (var localList = new ThreadLocal<List<T>>(() => new List<T>(), trackAllValues: false))
            {
                // find out which executor we want to use.
                var taskExecutor = nodes.Count > (1000 * Environment.ProcessorCount) ? TaskExecutor.Concurrent : TaskExecutor.Synchronous;
                taskExecutor.ForEach(nodes, n =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var list = localList.Value;
                    addOperations(list, n);

                    foreach (var element in list)
                    {
                        if (element != null)
                        {
                            localOperations.Value.Add(element);
                        }
                    }

                    list.Clear();
                }, cancellationToken);

                var operations = new List<T>(localOperations.Values.Sum(v => v.Count));
                operations.AddRange(localOperations.Values.SelectMany(v => v));

                return operations;
            }
        }

        private Task<TokenPairWithOperations[]> CreateTokenOperationTask(
            TokenStream tokenStream,
            CancellationToken cancellationToken)
        {
            return this.TaskExecutor.StartNew(() =>
            {
                using (Logger.LogBlock(FunctionId.Formatting_CollectTokenOperation, cancellationToken))
                {
                    // pre-allocate list once. this is cheaper than re-adjusting list as items are added.
                    var list = new TokenPairWithOperations[tokenStream.TokenCount - 1];

                    this.TaskExecutor.ForEach(tokenStream.TokenIterator, pair =>
                    {
                        var spaceOperation = _formattingRules.GetAdjustSpacesOperation(pair.Item2, pair.Item3);
                        var lineOperation = _formattingRules.GetAdjustNewLinesOperation(pair.Item2, pair.Item3);

                        list[pair.Item1] = new TokenPairWithOperations(tokenStream, pair.Item1, spaceOperation, lineOperation);
                    }, cancellationToken);

                    return list;
                }
            },
            cancellationToken);
        }

        private async Task ApplyTokenOperationsAsync(
            FormattingContext context,
            TokenStream tokenStream,
            Task anchorContextTask,
            NodeOperations nodeOperations,
            TokenPairWithOperations[] tokenOperations,
            CancellationToken cancellationToken)
        {
            var applier = new OperationApplier(context, tokenStream, _formattingRules);
            ApplySpaceAndWrappingOperations(context, tokenStream, tokenOperations, applier, cancellationToken);

            // wait until anchor task to finish adding its information to context
            await anchorContextTask.ConfigureAwait(false);

            ApplyAnchorOperations(context, tokenStream, tokenOperations, applier, cancellationToken);

            await ApplySpecialOperationsAsync(context, tokenStream, nodeOperations, applier, cancellationToken).ConfigureAwait(false);
        }

        private void ApplyBeginningOfTreeTriviaOperation(
            FormattingContext context, TokenStream tokenStream, CancellationToken cancellationToken)
        {
            if (!tokenStream.FormatBeginningOfTree)
            {
                return;
            }

            Action<int, TriviaData> beginningOfTreeTriviaInfoApplier = (i, info) =>
            {
                tokenStream.ApplyBeginningOfTreeChange(info);
            };

            // remove all leading indentation
            var triviaInfo = tokenStream.GetTriviaDataAtBeginningOfTree().WithIndentation(0, context, _formattingRules, cancellationToken);

            triviaInfo.Format(context, _formattingRules, beginningOfTreeTriviaInfoApplier, cancellationToken);
        }

        private void ApplyEndOfTreeTriviaOperation(
            FormattingContext context, TokenStream tokenStream, CancellationToken cancellationToken)
        {
            if (!tokenStream.FormatEndOfTree)
            {
                return;
            }

            Action<int, TriviaData> endOfTreeTriviaInfoApplier = (i, info) =>
            {
                tokenStream.ApplyEndOfTreeChange(info);
            };

            // remove all trailing indentation
            var triviaInfo = tokenStream.GetTriviaDataAtEndOfTree().WithIndentation(0, context, _formattingRules, cancellationToken);

            triviaInfo.Format(context, _formattingRules, endOfTreeTriviaInfoApplier, cancellationToken);
        }

        private void ApplyTriviaOperations(FormattingContext context, TokenStream tokenStream, CancellationToken cancellationToken)
        {
            // trivia formatting result appliers
            Action<int, TriviaData> regularApplier = (tokenPairIndex, info) =>
            {
                tokenStream.ApplyChange(tokenPairIndex, info);
            };

            // trivia formatting applier
            Action<int> triviaFormatter = tokenPairIndex =>
            {
                var triviaInfo = tokenStream.GetTriviaData(tokenPairIndex);
                triviaInfo.Format(context, _formattingRules, regularApplier, cancellationToken, tokenPairIndex);
            };

            this.TaskExecutor.For(0, tokenStream.TokenCount - 1, triviaFormatter, cancellationToken);
        }

        private TextSpan GetSpanToFormat()
        {
            var startPosition = this.TreeData.IsFirstToken(_token1) ? this.TreeData.StartPosition : _token1.SpanStart;
            var endPosition = this.TreeData.IsLastToken(_token2) ? this.TreeData.EndPosition : _token2.Span.End;

            return TextSpan.FromBounds(startPosition, endPosition);
        }

        private async Task ApplySpecialOperationsAsync(
            FormattingContext context, TokenStream tokenStream, NodeOperations nodeOperationsCollector, OperationApplier applier, CancellationToken cancellationToken)
        {
            // apply alignment operation
            using (Logger.LogBlock(FunctionId.Formatting_CollectAlignOperation, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                // TODO : figure out a way to run alignment operations in parallel. probably find
                // unions and run each chunk in separate tasks
                var previousChangesMap = new Dictionary<SyntaxToken, int>();
                var alignmentOperations = await nodeOperationsCollector.AlignmentOperationTask.ConfigureAwait(false);

                alignmentOperations.Do(operation =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    applier.ApplyAlignment(operation, previousChangesMap, cancellationToken);
                });

                // go through all relative indent block operation, and see whether it is affected by previous operations
                context.GetAllRelativeIndentBlockOperations().Do(o =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    applier.ApplyBaseTokenIndentationChangesFromTo(FindCorrectBaseTokenOfRelativeIndentBlockOperation(o, tokenStream), o.StartToken, o.EndToken, previousChangesMap, cancellationToken);
                });
            }
        }

        private void ApplyAnchorOperations(
            FormattingContext context,
            TokenStream tokenStream,
            TokenPairWithOperations[] tokenOperations,
            OperationApplier applier,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Formatting_ApplyAnchorOperation, cancellationToken))
            {
                var tokenPairsToApplyAnchorOperations = this.TaskExecutor.Filter(
                                                            tokenOperations,
                                                            p => AnchorOperationCandidate(p),
                                                            p => p.PairIndex, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                // TODO: find out a way to apply anchor operation concurrently if possible
                var previousChangesMap = new Dictionary<SyntaxToken, int>();
                tokenPairsToApplyAnchorOperations.Do(pairIndex =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    applier.ApplyAnchorIndentation(pairIndex, previousChangesMap, cancellationToken);
                });

                // go through all relative indent block operation, and see whether it is affected by the anchor operation
                context.GetAllRelativeIndentBlockOperations().Do(o =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    applier.ApplyBaseTokenIndentationChangesFromTo(FindCorrectBaseTokenOfRelativeIndentBlockOperation(o, tokenStream), o.StartToken, o.EndToken, previousChangesMap, cancellationToken);
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
            TokenStream tokenStream,
            TokenPairWithOperations[] tokenOperations,
            OperationApplier applier,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Formatting_ApplySpaceAndLine, cancellationToken))
            {
                // go through each token pairs and apply operations. operations don't need to be applied in order
                var partitioner = new Partitioner(context, tokenStream, tokenOperations);

                // always create task 1 more than current processor count
                var partitions = partitioner.GetPartitions(this.TaskExecutor == TaskExecutor.Synchronous ? 1 : Environment.ProcessorCount + 1, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                var tasks = new Task[partitions.Count];
                for (int i = 0; i < partitions.Count; i++)
                {
                    var partition = partitions[i];
                    tasks[i] = this.TaskExecutor.StartNew(() =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        partition.Do(operationPair => ApplySpaceAndWrappingOperationsBody(context, tokenStream, operationPair, applier, cancellationToken));
                    },
                    cancellationToken);
                }

                Task.WaitAll(tasks, cancellationToken);
            }
        }

        private static void ApplySpaceAndWrappingOperationsBody(
            FormattingContext context,
            TokenStream tokenStream,
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

            var triviaInfo = tokenStream.GetTriviaData(operation.PairIndex);
            var spanBetweenTokens = TextSpan.FromBounds(token1.Span.End, token2.SpanStart);

            if (operation.LineOperation != null)
            {
                if (!context.IsWrappingSuppressed(spanBetweenTokens))
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
                if (!context.IsSpacingSuppressed(spanBetweenTokens))
                {
                    applier.Apply(operation.SpaceOperation, operation.PairIndex);
                }
            }
        }

        private void BuildContext(
            FormattingContext context,
            TokenStream tokenStream,
            NodeOperations nodeOperations,
            CancellationToken cancellationToken)
        {
            // add scope operation (run each kind sequentially)
            using (Logger.LogBlock(FunctionId.Formatting_BuildContext, cancellationToken))
            {
                var indentationScopeTask = this.TaskExecutor.ContinueWith(nodeOperations.IndentBlockOperationTask, task => context.AddIndentBlockOperations(task.Result, cancellationToken), cancellationToken);
                var suppressWrappingScopeTask = this.TaskExecutor.ContinueWith(nodeOperations.SuppressOperationTask, task => context.AddSuppressOperations(task.Result, cancellationToken), cancellationToken);

                Task.WaitAll(new[] { indentationScopeTask, suppressWrappingScopeTask }, cancellationToken);
            }
        }

        /// <summary>
        /// return summary for current formatting work
        /// </summary>
        private string FormatSummary()
        {
            return string.Format("({0}) ({1} - {2}) {3}",
                this.SpanToFormat,
                _token1.ToString().Replace("\r\n", "\\r\\n"),
                _token2.ToString().Replace("\r\n", "\\r\\n"),
                this.TaskExecutor.ToString());
        }
    }
}
