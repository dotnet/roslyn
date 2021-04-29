﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal class FindLiteralsSearchEngine
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
        private readonly string _stringValue;
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
                case decimal _: // unsupported
                    _searchKind = SearchKind.None;
                    break;
                case char _:
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
            await using var _ = await _progressTracker.AddSingleItemAsync(cancellationToken).ConfigureAwait(false);

            if (_searchKind != SearchKind.None)
            {
                await FindReferencesWorkerAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task FindReferencesWorkerAsync(CancellationToken cancellationToken)
        {
            var count = _solution.Projects.SelectMany(p => p.DocumentIds).Count();
            await _progressTracker.AddItemsAsync(count, cancellationToken).ConfigureAwait(false);

            foreach (var project in _solution.Projects)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var documentTasks = new List<Task>();
                foreach (var document in await project.GetAllRegularAndSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false))
                {
                    documentTasks.Add(ProcessDocumentAsync(document, cancellationToken));
                }

                await Task.WhenAll(documentTasks).ConfigureAwait(false);
            }
        }

        private async Task ProcessDocumentAsync(Document document, CancellationToken cancellationToken)
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

            if (_searchKind == SearchKind.StringLiterals)
            {
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
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var matches = ArrayBuilder<SyntaxToken>.GetInstance();
            ProcessNode(syntaxFacts, root, matches, cancellationToken);

            foreach (var token in matches)
            {
                await _progress.OnReferenceFoundAsync(document, token.Span, cancellationToken).ConfigureAwait(false);
            }
        }

        private void ProcessNode(
            ISyntaxFactsService syntaxFacts, SyntaxNode node,
            ArrayBuilder<SyntaxToken> matches, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                {
                    ProcessNode(syntaxFacts, child.AsNode(), matches, cancellationToken);
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
}
