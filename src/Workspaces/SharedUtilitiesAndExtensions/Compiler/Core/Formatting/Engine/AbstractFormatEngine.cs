// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting;

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
    // Intentionally do not trim the capacities of these collections down.  We will repeatedly try to format large
    // files as we edit them and this will produce a lot of garbage as we free the internal array backing the list
    // over and over again.
    private static readonly ObjectPool<SegmentedList<TokenPairWithOperations>> s_tokenPairListPool = new(() => [], trimOnFree: false);

    private readonly ChainedFormattingRules _formattingRules;

    private readonly SyntaxNode _commonRoot;
    private readonly SyntaxToken _startToken;
    private readonly SyntaxToken _endToken;

    protected readonly TextSpan SpanToFormat;

    internal readonly SyntaxFormattingOptions Options;
    internal readonly TreeData TreeData;

    /// <summary>
    /// It is very common to be formatting lots of documents at teh same time, with the same set of formatting rules and
    /// options. To help with that, cache the last set of ChainedFormattingRules that was produced, as it is not a cheap
    /// type to create.
    /// </summary>
    /// <remarks>
    /// Stored as a <see cref="Tuple{T1, T2, T3}"/> instead of a <see cref="ValueTuple{T1, T2, T3}"/> so we don't have
    /// to worry about torn write concerns.
    /// </remarks>
    private static Tuple<ImmutableArray<AbstractFormattingRule>, SyntaxFormattingOptions, ChainedFormattingRules>? s_lastRulesAndOptions;

    public AbstractFormatEngine(
        TreeData treeData,
        SyntaxFormattingOptions options,
        ImmutableArray<AbstractFormattingRule> formattingRules,
        SyntaxToken startToken,
        SyntaxToken endToken)
        : this(
              treeData,
              options,
              GetChainedFormattingRules(formattingRules, options),
              startToken,
              endToken)
    {
    }

    private static ChainedFormattingRules GetChainedFormattingRules(ImmutableArray<AbstractFormattingRule> formattingRules, SyntaxFormattingOptions options)
    {
        var lastRulesAndOptions = s_lastRulesAndOptions;
        if (formattingRules != lastRulesAndOptions?.Item1 || options != s_lastRulesAndOptions?.Item2)
        {
            lastRulesAndOptions = Tuple.Create(formattingRules, options, new ChainedFormattingRules(formattingRules, options));
            s_lastRulesAndOptions = lastRulesAndOptions;
        }

        return lastRulesAndOptions.Item3;
    }

    internal AbstractFormatEngine(
        TreeData treeData,
        SyntaxFormattingOptions options,
        ChainedFormattingRules formattingRules,
        SyntaxToken startToken,
        SyntaxToken endToken)
    {
        Contract.ThrowIfTrue(treeData.Root.IsInvalidTokenRange(startToken, endToken));

        this.Options = options;
        this.TreeData = treeData;
        _formattingRules = formattingRules;

        _startToken = startToken;
        _endToken = endToken;

        // get span and common root
        this.SpanToFormat = GetSpanToFormat();
        _commonRoot = startToken.GetCommonRoot(endToken) ?? throw ExceptionUtilities.Unreachable();
    }

    internal abstract IHeaderFacts HeaderFacts { get; }

    protected abstract AbstractTriviaDataFactory CreateTriviaFactory();
    protected abstract AbstractFormattingResult CreateFormattingResult(TokenStream tokenStream);

    public AbstractFormattingResult Format(CancellationToken cancellationToken)
    {
        using (Logger.LogBlock(FunctionId.Formatting_Format, FormatSummary, cancellationToken))
        {
            // setup environment
            using var nodeOperations = CreateNodeOperations(cancellationToken);

            var tokenStream = new TokenStream(this.TreeData, Options, this.SpanToFormat, CreateTriviaFactory());
            using var tokenOperations = s_tokenPairListPool.GetPooledObject();
            AddTokenOperations(tokenStream, tokenOperations.Object, cancellationToken);

            // initialize context
            var context = CreateFormattingContext(tokenStream, cancellationToken);

            // start anchor task that will be used later
            cancellationToken.ThrowIfCancellationRequested();
            var anchorContext = nodeOperations.AnchorIndentationOperations.Do(context.AddAnchorIndentationOperation);

            BuildContext(context, nodeOperations, cancellationToken);

            ApplyBeginningOfTreeTriviaOperation(context, cancellationToken);

            ApplyTokenOperations(context, nodeOperations, tokenOperations.Object, cancellationToken);

            ApplyTriviaOperations(context, cancellationToken);

            ApplyEndOfTreeTriviaOperation(context, cancellationToken);

            return CreateFormattingResult(tokenStream);
        }
    }

    protected virtual FormattingContext CreateFormattingContext(TokenStream tokenStream, CancellationToken cancellationToken)
    {
        // initialize context
        var context = new FormattingContext(this, tokenStream);
        context.Initialize(_formattingRules, _startToken, _endToken, cancellationToken);

        return context;
    }

    protected virtual NodeOperations CreateNodeOperations(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var nodeOperations = new NodeOperations();

        var indentBlockOperationScratch = new List<IndentBlockOperation>();
        var alignmentOperationScratch = new List<AlignTokensOperation>();
        var anchorIndentationOperationsScratch = new List<AnchorIndentationOperation>();
        using var _ = ArrayBuilder<SuppressOperation>.GetInstance(out var suppressOperationScratch);

        // Cache delegates out here to avoid allocation overhead.

        var addIndentBlockOperations = _formattingRules.AddIndentBlockOperations;
        var addSuppressOperation = _formattingRules.AddSuppressOperations;
        var addAlignTokensOperations = _formattingRules.AddAlignTokensOperations;
        var addAnchorIndentationOperations = _formattingRules.AddAnchorIndentationOperations;

        // iterating tree is very expensive. only do it once.
        foreach (var node in _commonRoot.DescendantNodesAndSelf(this.SpanToFormat))
        {
            cancellationToken.ThrowIfCancellationRequested();

            AddOperations(nodeOperations.IndentBlockOperation, indentBlockOperationScratch, node, addIndentBlockOperations);
            AddOperations(nodeOperations.SuppressOperation, suppressOperationScratch, node, addSuppressOperation);
            AddOperations(nodeOperations.AlignmentOperation, alignmentOperationScratch, node, addAlignTokensOperations);
            AddOperations(nodeOperations.AnchorIndentationOperations, anchorIndentationOperationsScratch, node, addAnchorIndentationOperations);
        }

        // make sure we order align operation from left to right
        nodeOperations.AlignmentOperation.Sort(static (o1, o2) => o1.BaseToken.Span.CompareTo(o2.BaseToken.Span));

        return nodeOperations;
    }

    private static void AddOperations<T>(SegmentedList<T> operations, List<T> scratch, SyntaxNode node, Action<List<T>, SyntaxNode> addOperations)
    {
        Debug.Assert(scratch.Count == 0);

        addOperations(scratch, node);
        foreach (var operation in scratch)
        {
            if (operation is not null)
                operations.Add(operation);
        }

        scratch.Clear();
    }

    private static void AddOperations<T>(SegmentedList<T> operations, ArrayBuilder<T> scratch, SyntaxNode node, Action<ArrayBuilder<T>, SyntaxNode> addOperations)
    {
        Debug.Assert(scratch.Count == 0);

        addOperations(scratch, node);
        foreach (var operation in scratch)
        {
            if (operation is not null)
                operations.Add(operation);
        }

        scratch.Clear();
    }

    private void AddTokenOperations(
        TokenStream tokenStream,
        SegmentedList<TokenPairWithOperations> list,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using (Logger.LogBlock(FunctionId.Formatting_CollectTokenOperation, cancellationToken))
        {
            foreach (var (index, currentToken, nextToken) in tokenStream.TokenIterator)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var spaceOperation = _formattingRules.GetAdjustSpacesOperation(currentToken, nextToken);
                var lineOperation = _formattingRules.GetAdjustNewLinesOperation(currentToken, nextToken);

                list.Add(new TokenPairWithOperations(tokenStream, index, spaceOperation, lineOperation));
            }
        }
    }

    private void ApplyTokenOperations(
        FormattingContext context,
        NodeOperations nodeOperations,
        SegmentedList<TokenPairWithOperations> tokenOperations,
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

        if (context.IsFormattingDisabled(new TextSpan(context.TokenStream.LastTokenInStream.Token.SpanStart, 0)))
        {
            // Formatting is suppressed in the document, and not restored before the end
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
            if (ctx.IsFormattingDisabled(tokenPairIndex))
            {
                return;
            }

            var triviaInfo = ctx.TokenStream.GetTriviaData(tokenPairIndex);
            triviaInfo.Format(
                ctx,
                formattingRules,
                RegularApplier,
                ct,
                tokenPairIndex);
        }
    }

    private TextSpan GetSpanToFormat()
    {
        var startPosition = this.TreeData.IsFirstToken(_startToken) ? this.TreeData.StartPosition : _startToken.SpanStart;
        var endPosition = this.TreeData.IsLastToken(_endToken) ? this.TreeData.EndPosition : _endToken.Span.End;

        return TextSpan.FromBounds(startPosition, endPosition);
    }

    private static void ApplySpecialOperations(
        FormattingContext context, NodeOperations nodeOperationsCollector, OperationApplier applier, CancellationToken cancellationToken)
    {
        // apply alignment operation
        using (Logger.LogBlock(FunctionId.Formatting_ApplyAlignOperation, cancellationToken))
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

    private static void ApplyAnchorOperations(
        FormattingContext context,
        SegmentedList<TokenPairWithOperations> tokenOperations,
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
                    continue;

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

    private static SyntaxToken FindCorrectBaseTokenOfRelativeIndentBlockOperation(IndentBlockOperation operation, TokenStream tokenStream)
    {
        if (operation.Option.IsOn(IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine))
        {
            return tokenStream.FirstTokenOfBaseTokenLine(operation.BaseToken);
        }

        return operation.BaseToken;
    }

    private static void ApplySpaceAndWrappingOperations(
        FormattingContext context,
        SegmentedList<TokenPairWithOperations> tokenOperations,
        OperationApplier applier,
        CancellationToken cancellationToken)
    {
        using (Logger.LogBlock(FunctionId.Formatting_ApplySpaceAndLine, cancellationToken))
        {
            // go through each token pairs and apply operations
            foreach (var operationPair in tokenOperations)
                ApplySpaceAndWrappingOperationsBody(context, operationPair, applier, cancellationToken);
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

    private static void BuildContext(
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
            _startToken.ToString().Replace("\r\n", "\\r\\n"),
            _endToken.ToString().Replace("\r\n", "\\r\\n"));
    }
}
