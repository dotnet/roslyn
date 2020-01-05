// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Roslyn.Utilities;
using INavigateToSearchService = Microsoft.CodeAnalysis.NavigateTo.INavigateToSearchService;
using INavigateToSearchService_RemoveInterfaceAboveAndRenameThisAfterInternalsVisibleToUsersUpdate = Microsoft.CodeAnalysis.NavigateTo.INavigateToSearchService_RemoveInterfaceAboveAndRenameThisAfterInternalsVisibleToUsersUpdate;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    internal partial class NavigateToItemProvider : INavigateToItemProvider2
    {
        private readonly Workspace _workspace;
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly INavigateToItemDisplayFactory _displayFactory;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public NavigateToItemProvider(
            Workspace workspace,
            IAsynchronousOperationListener asyncListener)
        {
            Contract.ThrowIfNull(workspace);
            Contract.ThrowIfNull(asyncListener);

            _workspace = workspace;
            _asyncListener = asyncListener;
            _displayFactory = new NavigateToItemDisplayFactory();
        }

        ISet<string> INavigateToItemProvider2.KindsProvided => KindsProvided;

        public ImmutableHashSet<string> KindsProvided
        {
            get
            {
                var result = ImmutableHashSet.Create<string>(StringComparer.Ordinal);
                foreach (var project in _workspace.CurrentSolution.Projects)
                {
                    var navigateToSearchService = TryGetNavigateToSearchService(project);
                    if (navigateToSearchService != null)
                    {
                        result = result.Union(navigateToSearchService.KindsProvided);
                    }
                }

                return result;
            }
        }

        public bool CanFilter
        {
            get
            {
                foreach (var project in _workspace.CurrentSolution.Projects)
                {
                    var navigateToSearchService = TryGetNavigateToSearchService(project);
                    if (navigateToSearchService is null)
                    {
                        // If we reach here, it means the current project does not support Navigate To, which is
                        // functionally equivalent to supporting filtering.
                        continue;
                    }

                    if (!navigateToSearchService.CanFilter)
                    {
                        return false;
                    }
                }

                // All projects either support filtering or do not support Navigate To at all
                return true;
            }
        }

        public void StopSearch()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void Dispose()
        {
            this.StopSearch();
            (_displayFactory as IDisposable)?.Dispose();
        }

        public void StartSearch(INavigateToCallback callback, string searchValue)
        {
            StartSearch(callback, searchValue, KindsProvided);
        }

        public void StartSearch(INavigateToCallback callback, string searchValue, INavigateToFilterParameters filter)
        {
            StartSearch(callback, searchValue, filter.Kinds.ToImmutableHashSet(StringComparer.Ordinal));
        }

        private void StartSearch(INavigateToCallback callback, string searchValue, IImmutableSet<string> kinds)
        {
            this.StopSearch();

            if (string.IsNullOrWhiteSpace(searchValue))
            {
                callback.Done();
                return;
            }

            if (kinds == null || kinds.Count == 0)
            {
                kinds = KindsProvided;
            }

            var searchCurrentDocument = (callback.Options as INavigateToOptions2)?.SearchCurrentDocument ?? false;
            var searcher = new Searcher(
                _workspace.CurrentSolution,
                _asyncListener,
                _displayFactory,
                callback,
                searchValue,
                searchCurrentDocument,
                kinds,
                _cancellationTokenSource.Token);

            _ = searcher.SearchAsync();
        }

        private static INavigateToSearchService_RemoveInterfaceAboveAndRenameThisAfterInternalsVisibleToUsersUpdate TryGetNavigateToSearchService(Project project)
        {
            var service = project.LanguageServices.GetService<INavigateToSearchService_RemoveInterfaceAboveAndRenameThisAfterInternalsVisibleToUsersUpdate>();
            if (service != null)
            {
                return service;
            }

#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0612 // Type or member is obsolete
            var legacyService = project.LanguageServices.GetService<INavigateToSearchService>();
            if (legacyService != null)
            {
                return new ShimNavigateToSearchService(legacyService);
            }
#pragma warning restore CS0612 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete

            return null;
        }

        [Obsolete("https://github.com/dotnet/roslyn/issues/28343")]
        private class ShimNavigateToSearchService : INavigateToSearchService_RemoveInterfaceAboveAndRenameThisAfterInternalsVisibleToUsersUpdate
        {
            private readonly INavigateToSearchService _navigateToSearchService;

            public ShimNavigateToSearchService(INavigateToSearchService navigateToSearchService)
            {
                _navigateToSearchService = navigateToSearchService;
            }

            public IImmutableSet<string> KindsProvided => ImmutableHashSet.Create<string>(StringComparer.Ordinal);

            public bool CanFilter => false;

            public Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentAsync(Document document, string searchPattern, IImmutableSet<string> kinds, CancellationToken cancellationToken)
                => _navigateToSearchService.SearchDocumentAsync(document, searchPattern, cancellationToken);

            public Task<ImmutableArray<INavigateToSearchResult>> SearchProjectAsync(Project project, ImmutableArray<Document> priorityDocuments, string searchPattern, IImmutableSet<string> kinds, CancellationToken cancellationToken)
                => _navigateToSearchService.SearchProjectAsync(project, searchPattern, cancellationToken);
        }
    }
}
