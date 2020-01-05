// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    [ExportLanguageService(typeof(INavigateToSearchService_RemoveInterfaceAboveAndRenameThisAfterInternalsVisibleToUsersUpdate), LanguageNames.FSharp)]
    internal class FSharpNavigateToSearchService : INavigateToSearchService_RemoveInterfaceAboveAndRenameThisAfterInternalsVisibleToUsersUpdate
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

        public async Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentAsync(Document document, string searchPattern, IImmutableSet<string> kinds, CancellationToken cancellationToken)
        {
            var results = await _service.SearchDocumentAsync(document, searchPattern, kinds, cancellationToken).ConfigureAwait(false);
            return results.SelectAsArray(x => (INavigateToSearchResult)new InternalFSharpNavigateToSearchResult(x));
        }

        public async Task<ImmutableArray<INavigateToSearchResult>> SearchProjectAsync(Project project, ImmutableArray<Document> priorityDocuments, string searchPattern, IImmutableSet<string> kinds, CancellationToken cancellationToken)
        {
            var results = await _service.SearchProjectAsync(project, priorityDocuments, searchPattern, kinds, cancellationToken).ConfigureAwait(false);
            return results.SelectAsArray(x => (INavigateToSearchResult)new InternalFSharpNavigateToSearchResult(x));
        }
    }
}
