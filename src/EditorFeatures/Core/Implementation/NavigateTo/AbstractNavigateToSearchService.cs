// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    internal abstract partial class AbstractNavigateToSearchService : INavigateToSearchService
    {
        private readonly ImmutableArray<INavigateToSearchResultProvider> _resultProviders;

        protected AbstractNavigateToSearchService(IEnumerable<INavigateToSearchResultProvider> resultProviders)
        {
            _resultProviders = resultProviders.ToImmutableArray();
        }

        public async Task<IEnumerable<INavigateToSearchResult>> SearchProjectAsync(Project project, string searchPattern, CancellationToken cancellationToken)
        {
            if (_resultProviders.Length == 0)
            {
                return SpecializedCollections.EmptyEnumerable<INavigateToSearchResult>();
            }
            else if (_resultProviders.Length == 1)
            {
                return await _resultProviders[0].SearchProjectAsync(project, searchPattern, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var tasks = new Task<IEnumerable<INavigateToSearchResult>>[_resultProviders.Length];
                for (int i = 0; i < _resultProviders.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var resultProvider = _resultProviders[i];
                    tasks[i] = Task.Run(() => resultProvider.SearchProjectAsync(project, searchPattern, cancellationToken), cancellationToken);
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
        }
    }
}
