// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

internal sealed class TriviaRewriter : CSharpSyntaxRewriter
{
    private readonly SyntaxNode _node;
    private readonly TextSpanMutableIntervalTree _spans;
    private readonly SyntaxFormattingOptions _options;
    private readonly CancellationToken _cancellationToken;

    private readonly Dictionary<SyntaxToken, SyntaxTriviaList> _trailingTriviaMap = [];
    private readonly Dictionary<SyntaxToken, SyntaxTriviaList> _leadingTriviaMap = [];

    public TriviaRewriter(
        SyntaxNode node,
        TextSpanMutableIntervalTree spanToFormat,
        Dictionary<ValueTuple<SyntaxToken, SyntaxToken>, TriviaData> map,
        SyntaxFormattingOptions options,
        CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(node);
        Contract.ThrowIfNull(map);

        _node = node;
        _spans = spanToFormat;
        _options = options;
        _cancellationToken = cancellationToken;

        PreprocessTriviaListMap(map, cancellationToken);
    }

    public SyntaxNode Transform()
        => Visit(_node);

    private void PreprocessTriviaListMap(
        Dictionary<ValueTuple<SyntaxToken, SyntaxToken>, TriviaData> map,
        CancellationToken cancellationToken)
    {
        foreach (var pair in map)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (trailingTrivia, leadingTrivia) = GetTrailingAndLeadingTrivia(pair, cancellationToken);

            if (pair.Key.Item1.RawKind != 0)
            {
                _trailingTriviaMap.Add(pair.Key.Item1, trailingTrivia);
            }

            if (pair.Key.Item2.RawKind != 0)
            {
                _leadingTriviaMap.Add(pair.Key.Item2, leadingTrivia);
            }
        }
    }

    private (SyntaxTriviaList trailingTrivia, SyntaxTriviaList leadingTrivia) GetTrailingAndLeadingTrivia(
        KeyValuePair<ValueTuple<SyntaxToken, SyntaxToken>,
        TriviaData> pair,
        CancellationToken cancellationToken)
    {
        if (pair.Key.Item1.RawKind == 0)
        {
            return (default(SyntaxTriviaList), GetLeadingTriviaAtBeginningOfTree(pair.Key, pair.Value, cancellationToken));
        }

        if (pair.Value is TriviaDataWithList csharpTriviaData)
        {
            var triviaList = csharpTriviaData.GetTriviaList(cancellationToken);
            var index = GetFirstEndOfLineIndexOrRightBeforeComment(triviaList);

            return (TriviaHelpers.CreateTriviaListFromTo(triviaList, 0, index),
                    TriviaHelpers.CreateTriviaListFromTo(triviaList, index + 1, triviaList.Count - 1));
        }

        // whitespace trivia case such as spaces/tabs/new lines
        // these will always have a single text change
        var text = pair.Value.GetTextChanges(GetTextSpan(pair.Key)).Single().NewText ?? "";
        var trailingTrivia = SyntaxFactory.ParseTrailingTrivia(text);

        var width = trailingTrivia.GetFullWidth();
        var leadingTrivia = SyntaxFactory.ParseLeadingTrivia(text[width..]);

        return (trailingTrivia, leadingTrivia);
    }

    private TextSpan GetTextSpan(ValueTuple<SyntaxToken, SyntaxToken> pair)
    {
        if (pair.Item1.RawKind == 0)
        {
            return TextSpan.FromBounds(_node.FullSpan.Start, pair.Item2.SpanStart);
        }

        if (pair.Item2.RawKind == 0)
        {
            return TextSpan.FromBounds(pair.Item1.Span.End, _node.FullSpan.End);
        }

        return TextSpan.FromBounds(pair.Item1.Span.End, pair.Item2.SpanStart);
    }

    private static int GetFirstEndOfLineIndexOrRightBeforeComment(SyntaxTriviaList triviaList)
    {
        for (var i = 0; i < triviaList.Count; i++)
        {
            var trivia = triviaList[i];

            if (trivia.Kind() == SyntaxKind.EndOfLineTrivia)
            {
                return i;
            }

            if (trivia.IsDocComment())
            {
                return i - 1;
            }
        }

        return triviaList.Count - 1;
    }

    private SyntaxTriviaList GetLeadingTriviaAtBeginningOfTree(
        ValueTuple<SyntaxToken, SyntaxToken> pair,
        TriviaData triviaData,
        CancellationToken cancellationToken)
    {
        if (triviaData is TriviaDataWithList csharpTriviaData)
        {
            return csharpTriviaData.GetTriviaList(cancellationToken);
        }

        // whitespace trivia case such as spaces/tabs/new lines
        // these will always have single text changes
        var text = triviaData.GetTextChanges(GetTextSpan(pair)).Single().NewText ?? "";
        return SyntaxFactory.ParseLeadingTrivia(text);
    }

    [return: NotNullIfNotNull(nameof(node))]
    public override SyntaxNode? Visit(SyntaxNode? node)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        if (node == null || !_spans.HasIntervalThatIntersectsWith(node.FullSpan))
        {
            return node;
        }

        return base.Visit(node);
    }

    public override SyntaxToken VisitToken(SyntaxToken token)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        if (!_spans.HasIntervalThatIntersectsWith(token.FullSpan))
        {
            return token;
        }

        var hasChanges = false;

        // get token span

        // check whether we have trivia info belongs to this token
        if (_trailingTriviaMap.TryGetValue(token, out var trailingTrivia))
        {
            // okay, we have this situation
            // token|trivia
            hasChanges = true;
        }
        else
        {
            (var hasTrailingChanges, trailingTrivia) = NormalizeElasticNewLines(token.TrailingTrivia, _options.LineFormatting);
            hasChanges |= hasTrailingChanges;
        }

        if (_leadingTriviaMap.TryGetValue(token, out var leadingTrivia))
        {
            // okay, we have this situation
            // trivia|token
            hasChanges = true;
        }
        else
        {
            (var hasLeadingChanges, leadingTrivia) = NormalizeElasticNewLines(token.LeadingTrivia, _options.LineFormatting);
            hasChanges |= hasLeadingChanges;
        }

        if (hasChanges)
        {
            return CreateNewToken(leadingTrivia, token, trailingTrivia);
        }

        // we have no trivia belongs to this one
        return token;
    }

    private static SyntaxToken CreateNewToken(SyntaxTriviaList leadingTrivia, SyntaxToken token, SyntaxTriviaList trailingTrivia)
        => token.With(leadingTrivia, trailingTrivia);

    private static (bool hasChanges, SyntaxTriviaList triviaList) NormalizeElasticNewLines(SyntaxTriviaList triviaList, LineFormattingOptions options)
    {
        if (triviaList.Count == 0)
            return (false, triviaList);

        var hasElasticNewLine = triviaList.Any(t => t.IsElastic() && t.IsEndOfLine());
        if (!hasElasticNewLine)
            return (false, triviaList);

        var hasChanges = false;
        using var _ = ArrayBuilder<SyntaxTrivia>.GetInstance(triviaList.Count, out var builder);
        foreach (var trivia in triviaList)
        {
            if (trivia.IsEndOfLine() && trivia.IsElastic())
            {
                builder.Add(SyntaxFactory.EndOfLine(options.NewLine));
                hasChanges = true;
            }
            else
            {
                builder.Add(trivia);
            }
        }

        return (hasChanges, builder.ToSyntaxTriviaList());
    }
}
