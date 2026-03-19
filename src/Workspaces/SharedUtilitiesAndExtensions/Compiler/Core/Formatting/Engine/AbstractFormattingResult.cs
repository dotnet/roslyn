// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting;

internal abstract class AbstractFormattingResult : IFormattingResult
{
    protected readonly TreeData TreeInfo;
    protected readonly TokenStream TokenStream;

    private readonly CancellableLazy<IList<TextChange>> _lazyChanges;
    private readonly CancellableLazy<SyntaxNode> _lazyNode;

    /// <summary>
    /// span in the tree to format
    /// </summary>
    public readonly TextSpan FormattedSpan;

    internal AbstractFormattingResult(
        TreeData treeInfo,
        TokenStream tokenStream,
        TextSpan formattedSpan)
    {
        this.TreeInfo = treeInfo;
        this.TokenStream = tokenStream;
        this.FormattedSpan = formattedSpan;

        _lazyChanges = new CancellableLazy<IList<TextChange>>(CreateTextChanges);
        _lazyNode = new CancellableLazy<SyntaxNode>(CreateFormattedRoot);
    }

    /// <summary>
    /// rewrite the tree info root node with the trivia information in the map
    /// </summary>
    protected abstract SyntaxNode Rewriter(Dictionary<ValueTuple<SyntaxToken, SyntaxToken>, TriviaData> map, CancellationToken cancellationToken);

    #region IFormattingResult implementation

    public IList<TextChange> GetTextChanges(CancellationToken cancellationToken)
        => _lazyChanges.GetValue(cancellationToken);

    public SyntaxNode GetFormattedRoot(CancellationToken cancellationToken)
        => _lazyNode.GetValue(cancellationToken);

    private IList<TextChange> CreateTextChanges(CancellationToken cancellationToken)
    {
        using (Logger.LogBlock(FunctionId.Formatting_CreateTextChanges, cancellationToken))
        {
            var data = this.TokenStream.GetTriviaDataWithTokenPair(cancellationToken);

            var result = new List<TextChange>();
            foreach (var f in data)
            {
                if (!f.Item2.ContainsChanges)
                {
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();
                AddTextChanges(result, f.Item1.Item1, f.Item1.Item2, f.Item2);
            }

            return result;
        }
    }

    private void AddTextChanges(List<TextChange> list, SyntaxToken token1, SyntaxToken token2, TriviaData data)
    {
        var span = TextSpan.FromBounds(token1.RawKind == 0 ? this.TreeInfo.StartPosition : token1.Span.End, token2.RawKind == 0 ? this.TreeInfo.EndPosition : token2.SpanStart);
        var originalString = this.TreeInfo.GetTextBetween(token1, token2);

        foreach (var change in data.GetTextChanges(span))
        {
            var oldText = (change.Span == span) ? originalString : originalString.Substring(change.Span.Start - span.Start, change.Span.Length);
            list.Add(change.SimpleDiff(oldText));
        }
    }

    private SyntaxNode CreateFormattedRoot(CancellationToken cancellationToken)
    {
        using (Logger.LogBlock(FunctionId.Formatting_CreateFormattedRoot, cancellationToken))
        {
            var changes = GetChanges(cancellationToken);

            // create a map
            using var pooledObject = SharedPools.Default<Dictionary<ValueTuple<SyntaxToken, SyntaxToken>, TriviaData>>().GetPooledObject();

            var map = pooledObject.Object;
            changes.Do(change => map.Add(change.Item1, change.Item2));

            // no changes, return as it is.
            if (map.Count == 0)
            {
                return this.TreeInfo.Root;
            }

            return Rewriter(map, cancellationToken);
        }
    }

    internal IEnumerable<ValueTuple<ValueTuple<SyntaxToken, SyntaxToken>, TriviaData>> GetChanges(CancellationToken cancellationToken)
        => TokenStream.GetTriviaDataWithTokenPair(cancellationToken).Where(d => d.Item2.ContainsChanges);

    #endregion
}
