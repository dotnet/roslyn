// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.NavigateTo;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.NavigateTo;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.NavigateTo;

[Shared]
[ExportLanguageService(typeof(INavigateToSearchService), LanguageNames.FSharp)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class FSharpNavigateToSearchService([Import(AllowDefault = true)] IFSharpNavigateToSearchService? service) : INavigateToSearchService
{
    private readonly IFSharpNavigateToSearchService? _service = service;

    public IImmutableSet<string> KindsProvided => _service?.KindsProvided ?? ImmutableHashSet<string>.Empty;

    public bool CanFilter => _service?.CanFilter ?? false;

    public async Task SearchDocumentAsync(
        Document document,
        string searchPattern,
        IImmutableSet<string> kinds,
        Func<ImmutableArray<INavigateToSearchResult>, Task> onResultsFound,
        CancellationToken cancellationToken)
    {
        if (_service == null)
            return;

        var results = await _service.SearchDocumentAsync(document, searchPattern, kinds, cancellationToken).ConfigureAwait(false);
        if (results.Length > 0)
            await onResultsFound(results.SelectAsArray(result => (INavigateToSearchResult)new InternalFSharpNavigateToSearchResult(result))).ConfigureAwait(false);
    }

    public async Task SearchProjectsAsync(
        Solution solution,
        ImmutableArray<Project> projects,
        ImmutableArray<Document> priorityDocuments,
        string searchPattern,
        IImmutableSet<string> kinds,
        Document? activeDocument,
        Func<ImmutableArray<INavigateToSearchResult>, Task> onResultsFound,
        Func<Task> onProjectCompleted,
        CancellationToken cancellationToken)
    {
        if (_service == null)
            return;

        Contract.ThrowIfTrue(projects.IsEmpty);
        Contract.ThrowIfTrue(projects.Select(p => p.Language).Distinct().Count() != 1);

        foreach (var project in projects)
        {
            var results = await _service.SearchProjectAsync(project, priorityDocuments, searchPattern, kinds, cancellationToken).ConfigureAwait(false);
            if (results.Length > 0)
                await onResultsFound(results.SelectAsArray(result => (INavigateToSearchResult)new InternalFSharpNavigateToSearchResult(result))).ConfigureAwait(false);

            await onProjectCompleted().ConfigureAwait(false);
        }
    }
}
