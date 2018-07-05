// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
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

        public ISet<string> KindsProvided
        {
            get
            {
                var result = new HashSet<string>(StringComparer.Ordinal);
                foreach (var project in _workspace.CurrentSolution.Projects)
                {
                    var navigateToSearchService = project.LanguageServices.GetService<INavigateToSearchService_RemoveInterfaceAboveAndRenameThisAfterInternalsVisibleToUsersUpdate>();
                    if (navigateToSearchService != null)
                    {
                        result.UnionWith(navigateToSearchService.KindsProvided);
                        continue;
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
                    var navigateToSearchService = project.LanguageServices.GetService<INavigateToSearchService_RemoveInterfaceAboveAndRenameThisAfterInternalsVisibleToUsersUpdate>();
                    if (navigateToSearchService != null)
                    {
                        if (!navigateToSearchService.CanFilter)
                        {
                            return false;
                        }

                        continue;
                    }

#pragma warning disable CS0618 // Type or member is obsolete
                    var legacyNavigateToSearchService = project.LanguageServices.GetService<INavigateToSearchService>();
                    if (legacyNavigateToSearchService != null)
                    {
                        return false;
                    }
#pragma warning restore CS0618 // Type or member is obsolete

                    // If we reach here, it means the current project does not support Navigate To, which is
                    // functionally equivalent to supporting filtering.
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

        private bool GetSearchCurrentDocumentOption(INavigateToCallback callback)
        {
            try
            {
                return GetSearchCurrentDocumentOptionWorker(callback);
            }
            catch (TypeLoadException)
            {
                // The version of the APIs we call in VS may not match what the 
                // user currently has on the box (as the APIs have changed during
                // the VS15 timeframe.  Be resilient to this happening and just
                // default to searching all documents.
                return false;
            }
        }

        private bool GetSearchCurrentDocumentOptionWorker(INavigateToCallback callback)
        {
            var options2 = callback.Options as INavigateToOptions2;
            return options2?.SearchCurrentDocument ?? false;
        }

        public void StartSearch(INavigateToCallback callback, string searchValue, INavigateToFilterParameters filter)
        {
            StartSearch(callback, searchValue, filter.Kinds);
        }

        private void StartSearch(INavigateToCallback callback, string searchValue, ISet<string> kinds)
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

            var searchCurrentDocument = GetSearchCurrentDocumentOption(callback);
            var searcher = new Searcher(
                _workspace.CurrentSolution,
                _asyncListener,
                _displayFactory,
                callback,
                searchValue,
                searchCurrentDocument,
                kinds,
                _cancellationTokenSource.Token);

            searcher.Search();
        }
    }
}
