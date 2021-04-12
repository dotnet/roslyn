// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.NavigateTo;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.NavigateTo;

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

        public async Task<NavigateToSearchLocation> SearchDocumentAsync(
            Document document, string searchPattern, IImmutableSet<string> kinds,
            Func<INavigateToSearchResult, Task> onResultFound,
            bool isFullyLoaded, CancellationToken cancellationToken)
        {
            var results = await _service.SearchDocumentAsync(document, searchPattern, kinds, cancellationToken).ConfigureAwait(false);
            foreach (var result in results)
                await onResultFound(new InternalFSharpNavigateToSearchResult(result)).ConfigureAwait(false);

            return NavigateToSearchLocation.Latest;
        }

        public async Task<NavigateToSearchLocation> SearchProjectAsync(
            Project project, ImmutableArray<Document> priorityDocuments, string searchPattern,
            IImmutableSet<string> kinds, Func<INavigateToSearchResult, Task> onResultFound,
            bool isFullyLoaded, CancellationToken cancellationToken)
        {
            var results = await _service.SearchProjectAsync(project, priorityDocuments, searchPattern, kinds, cancellationToken).ConfigureAwait(false);
            foreach (var result in results)
                await onResultFound(new InternalFSharpNavigateToSearchResult(result)).ConfigureAwait(false);

            return NavigateToSearchLocation.Latest;
        }
    }
}
