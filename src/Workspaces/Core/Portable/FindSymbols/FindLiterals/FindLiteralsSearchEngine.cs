// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
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
        private readonly StreamingProgressTracker _progressTracker;
        private readonly CancellationToken _cancellationToken;

        private readonly object _value;
        private readonly string _stringValue;
        private readonly long _longValue;
        private readonly SearchKind _searchKind;

        public FindLiteralsSearchEngine(
            Solution solution,
            IStreamingFindLiteralReferencesProgress progress, object value,
            CancellationToken cancellationToken)
        {
            _solution = solution;
            _progress = progress;
            _progressTracker = new StreamingProgressTracker(_progress.ReportProgressAsync);
            _value = value;
            _cancellationToken = cancellationToken;

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
                case decimal d: // unsupported
                    _searchKind = SearchKind.None;
                    break;
                case char c:
                    _longValue = IntegerUtilities.ToInt64(value);
                    _searchKind = SearchKind.CharacterLiterals;
                    break;
                default:
                    _longValue = IntegerUtilities.ToInt64(value);
                    _searchKind = SearchKind.NumericLiterals;
                    break;
            }
        }

        public async Task FindReferencesAsync()
        {
            await _progressTracker.AddItemsAsync(1).ConfigureAwait(false);
            try
            {
                if (_searchKind != SearchKind.None)
                {
                    await FindReferencesWorkerAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                await _progressTracker.ItemCompletedAsync().ConfigureAwait(false);
            }
        }

        private async Task FindReferencesWorkerAsync()
        {
            var count = _solution.Projects.SelectMany(p => p.DocumentIds).Count();
            await _progressTracker.AddItemsAsync(count).ConfigureAwait(false);

            foreach (var project in _solution.Projects)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var documentTasks = new List<Task>();
                foreach (var document in project.Documents)
                {
                    documentTasks.Add(ProcessDocumentAsync(document));
                }

                await Task.WhenAll(documentTasks).ConfigureAwait(false);
            }
        }

        private async Task ProcessDocumentAsync(Document document)
        {
            try
            {
                await ProcessDocumentWorkerAsync(document).ConfigureAwait(false);
            }
            finally
            {
                await _progressTracker.ItemCompletedAsync().ConfigureAwait(false);
            }
        }

        private async Task ProcessDocumentWorkerAsync(Document document)
        {
            var index = await SyntaxTreeIndex.GetIndexAsync(
                document, _cancellationToken).ConfigureAwait(false);

            if (_searchKind == SearchKind.StringLiterals)
            {
                if (index.ProbablyContainsStringValue(_stringValue))
                {
                    await SearchDocumentAsync(document).ConfigureAwait(false);
                }
            }
            else if (index.ProbablyContainsInt64Value(_longValue))
            {
                await SearchDocumentAsync(document).ConfigureAwait(false);
            }
        }

        private async Task SearchDocumentAsync(Document document)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var root = await document.GetSyntaxRootAsync(_cancellationToken).ConfigureAwait(false);

            var matches = ArrayBuilder<SyntaxToken>.GetInstance();
            ProcessNode(syntaxFacts, root, matches);

            foreach (var token in matches)
            {
                await _progress.OnReferenceFoundAsync(document, token.Span).ConfigureAwait(false);
            }
        }

        private void ProcessNode(
            ISyntaxFactsService syntaxFacts, SyntaxNode node, ArrayBuilder<SyntaxToken> matches)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                {
                    ProcessNode(syntaxFacts, child.AsNode(), matches);
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
