// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols;

internal sealed class FindLiteralsSearchEngine
{
    private enum SearchKind
    {
        None,
        StringLiterals,
        CharacterLiterals,
        NumericLiterals,
    }

    private readonly Solution _solution;
    private readonly IStreamingFindLiteralReferencesProgress _progress;
    private readonly IStreamingProgressTracker _progressTracker;

    private readonly object _value;
    private readonly string? _stringValue;
    private readonly long _longValue;
    private readonly SearchKind _searchKind;

    public FindLiteralsSearchEngine(
        Solution solution,
        IStreamingFindLiteralReferencesProgress progress, object value)
    {
        _solution = solution;
        _progress = progress;
        _progressTracker = progress.ProgressTracker;
        _value = value;

        switch (value)
        {
            case string s:
                _stringValue = s;
                _searchKind = SearchKind.StringLiterals;
                break;
            case double d:
                _longValue = BitConverter.DoubleToInt64Bits(d);
                _searchKind = SearchKind.NumericLiterals;
                break;
            case float f:
                _longValue = BitConverter.DoubleToInt64Bits(f);
                _searchKind = SearchKind.NumericLiterals;
                break;
            case decimal: // unsupported
                _searchKind = SearchKind.None;
                break;
            case char:
                _longValue = IntegerUtilities.ToInt64(value);
                _searchKind = SearchKind.CharacterLiterals;
                break;
            default:
                _longValue = IntegerUtilities.ToInt64(value);
                _searchKind = SearchKind.NumericLiterals;
                break;
        }
    }

    public async Task FindReferencesAsync(CancellationToken cancellationToken)
    {
        var disposable = await _progressTracker.AddSingleItemAsync(cancellationToken).ConfigureAwait(false);
        await using var _ = disposable.ConfigureAwait(false);

        if (_searchKind != SearchKind.None)
        {
            await FindReferencesWorkerAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task FindReferencesWorkerAsync(CancellationToken cancellationToken)
    {
        var count = _solution.Projects.SelectMany(p => p.DocumentIds).Count();
        await _progressTracker.AddItemsAsync(count, cancellationToken).ConfigureAwait(false);

        await RoslynParallel.ForEachAsync(
            source: SelectManyAsync(_solution.Projects, p => p.GetAllRegularAndSourceGeneratedDocumentsAsync(cancellationToken)),
            cancellationToken,
            ProcessDocumentAsync).ConfigureAwait(false);

        static async IAsyncEnumerable<TResult> SelectManyAsync<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, IAsyncEnumerable<TResult>> selector)
        {
            foreach (var item in source)
            {
                await foreach (var result in selector(item))
                    yield return result;
            }
        }
    }

    private async ValueTask ProcessDocumentAsync(Document document, CancellationToken cancellationToken)
    {
        try
        {
            await ProcessDocumentWorkerAsync(document, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await _progressTracker.ItemCompletedAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessDocumentWorkerAsync(Document document, CancellationToken cancellationToken)
    {
        var index = await SyntaxTreeIndex.GetIndexAsync(
            document, cancellationToken).ConfigureAwait(false);

        Contract.ThrowIfNull(index);
        if (_searchKind == SearchKind.StringLiterals)
        {
            Contract.ThrowIfNull(_stringValue);
            if (index.ProbablyContainsStringValue(_stringValue))
            {
                await SearchDocumentAsync(document, cancellationToken).ConfigureAwait(false);
            }
        }
        else if (index.ProbablyContainsInt64Value(_longValue))
        {
            await SearchDocumentAsync(document, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SearchDocumentAsync(Document document, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        using var _ = ArrayBuilder<SyntaxToken>.GetInstance(out var matches);
        ProcessNode(syntaxFacts, root, matches, cancellationToken);

        foreach (var token in matches)
            await _progress.OnReferenceFoundAsync(document, token.Span, cancellationToken).ConfigureAwait(false);
    }

    private void ProcessNode(
        ISyntaxFactsService syntaxFacts, SyntaxNode node,
        ArrayBuilder<SyntaxToken> matches, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var child in node.ChildNodesAndTokens())
        {
            if (child.AsNode(out var childNode))
            {
                ProcessNode(syntaxFacts, childNode, matches, cancellationToken);
            }
            else
            {
                ProcessToken(syntaxFacts, child.AsToken(), matches);
            }
        }
    }

    private void ProcessToken(
        ISyntaxFactsService syntaxFacts, SyntaxToken token,
        ArrayBuilder<SyntaxToken> matches)
    {
        if (_searchKind == SearchKind.StringLiterals &&
            syntaxFacts.IsStringLiteral(token))
        {
            CheckToken(token, matches);
        }
        else if (_searchKind == SearchKind.CharacterLiterals &&
                 syntaxFacts.IsCharacterLiteral(token))
        {
            CheckToken(token, matches);
        }
        else if (_searchKind == SearchKind.NumericLiterals &&
                 syntaxFacts.IsNumericLiteral(token))
        {
            CheckToken(token, matches);
        }
    }

    private void CheckToken(SyntaxToken token, ArrayBuilder<SyntaxToken> matches)
    {
        if (_value.Equals(token.Value))
        {
            matches.Add(token);
        }
    }
}
