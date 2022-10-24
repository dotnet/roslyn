// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.NavigateTo;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.NavigateTo
{
    [Shared]
    [ExportLanguageService(typeof(INavigateToSearchService), LanguageNames.FSharp)]
    internal class FSharpNavigateToSearchService : INavigateToSearchService
    {
        private readonly IFSharpNavigateToSearchService _service;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FSharpNavigateToSearchService(IFSharpNavigateToSearchService service)
        {
            _service = service;
        }

        public IImmutableSet<string> KindsProvided => _service.KindsProvided;

        public bool CanFilter => _service.CanFilter;

        public async IAsyncEnumerable<INavigateToSearchResult> SearchDocumentAsync(
            Document document,
            string searchPattern,
            IImmutableSet<string> kinds,
            Document? activeDocument,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var results = await _service.SearchDocumentAsync(document, searchPattern, kinds, cancellationToken).ConfigureAwait(false);
            foreach (var result in results)
                yield return new InternalFSharpNavigateToSearchResult(result);
        }

        public async IAsyncEnumerable<INavigateToSearchResult> SearchProjectAsync(
            Project project,
            ImmutableArray<Document> priorityDocuments,
            string searchPattern,
            IImmutableSet<string> kinds,
            Document? activeDocument,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var results = await _service.SearchProjectAsync(project, priorityDocuments, searchPattern, kinds, cancellationToken).ConfigureAwait(false);
            foreach (var result in results)
                yield return new InternalFSharpNavigateToSearchResult(result);
        }

        public IAsyncEnumerable<INavigateToSearchResult> SearchCachedDocumentsAsync(
            Project project,
            ImmutableArray<Document> priorityDocuments,
            string searchPattern,
            IImmutableSet<string> kinds,
            Document? activeDocument,
            CancellationToken cancellationToken)
        {
            // we don't support searching cached documents.
            return AsyncEnumerable<INavigateToSearchResult>.Empty;
        }

        public IAsyncEnumerable<INavigateToSearchResult> SearchGeneratedDocumentsAsync(
            Project project,
            string searchPattern,
            IImmutableSet<string> kinds,
            Document? activeDocument,
            CancellationToken cancellationToken)
        {
            // we don't support searching generated documents.
            return AsyncEnumerable<INavigateToSearchResult>.Empty;
        }
    }
}
