﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Roslyn.Utilities;

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
                    var navigateToSearchService = project.GetLanguageService<INavigateToSearchService>();
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
                    var navigateToSearchService = project.GetLanguageService<INavigateToSearchService>();
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
            => StartSearch(callback, searchValue, KindsProvided);

        public void StartSearch(INavigateToCallback callback, string searchValue, INavigateToFilterParameters filter)
            => StartSearch(callback, searchValue, filter.Kinds.ToImmutableHashSet(StringComparer.Ordinal));

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

            var roslynCallback = new NavigateToItemProviderCallback(_displayFactory, callback);
            var searcher = new NavigateToSearcher(
                _workspace.CurrentSolution,
                _asyncListener,
                roslynCallback,
                searchValue,
                searchCurrentDocument,
                kinds,
                _cancellationTokenSource.Token);

            _ = searcher.SearchAsync();
        }
    }
}
