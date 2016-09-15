// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    internal abstract partial class AbstractNavigateToSearchService : INavigateToSearchService
    {
        private readonly ImmutableArray<INavigateToSearchResultProvider> _resultProviders;

        protected AbstractNavigateToSearchService(IEnumerable<INavigateToSearchResultProvider> resultProviders)
        {
            _resultProviders = resultProviders.ToImmutableArray();
        }

        private async Task<ImmutableArray<INavigateToSearchResult>> SearchAsync(
             Func<INavigateToSearchResultProvider, Task<ImmutableArray<INavigateToSearchResult>>> searchAsync,
             CancellationToken cancellationToken)
        {
            var tasks = new Task<ImmutableArray<INavigateToSearchResult>>[_resultProviders.Length];
            for (int i = 0; i < _resultProviders.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var resultProvider = _resultProviders[i];
                tasks[i] = searchAsync(resultProvider);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            var results = ArrayBuilder<INavigateToSearchResult>.GetInstance();
            foreach (var task in tasks)
            {
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    results.AddRange(task.Result);
                }
            }

            return results.ToImmutableAndFree();
        }
    
        public Task<ImmutableArray<INavigateToSearchResult>> SearchProjectAsync(
            Project project, string searchPattern, CancellationToken cancellationToken)
        {
            return SearchAsync(
                provider => provider.SearchProjectAsync(project, searchPattern, cancellationToken),
                cancellationToken);
        }

        public Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentAsync(
            Document document, string searchPattern, CancellationToken cancellationToken)
        {
            return SearchAsync(
                provider => provider.SearchDocumentAsync(document, searchPattern, cancellationToken),
                cancellationToken);
        }
    }
}