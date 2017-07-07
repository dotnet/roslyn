﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Extensibility.Composition;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    internal partial class NavigateToItemProvider : INavigateToItemProvider
    {
        private readonly Workspace _workspace;
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly INavigateToItemDisplayFactory _displayFactory;
        private readonly ImmutableArray<Lazy<INavigateToHostVersionService, VisualStudioVersionMetadata>> _hostServices;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public NavigateToItemProvider(
            Workspace workspace,
            IGlyphService glyphService,
            IAsynchronousOperationListener asyncListener,
            IEnumerable<Lazy<INavigateToHostVersionService, VisualStudioVersionMetadata>> hostServices)
        {
            Contract.ThrowIfNull(workspace);
            Contract.ThrowIfNull(glyphService);
            Contract.ThrowIfNull(asyncListener);

            _workspace = workspace;
            _asyncListener = asyncListener;
            _hostServices = hostServices.ToImmutableArray();

            var hostService = _hostServices.Length > 0
                ? VersionSelector.SelectHighest(hostServices)
                : new Dev14NavigateToHostVersionService(glyphService);
            _displayFactory = hostService.CreateDisplayFactory();
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
            this.StopSearch();

            if (string.IsNullOrWhiteSpace(searchValue))
            {
                callback.Done();
                return;
            }

            var searchCurrentDocument = GetSearchCurrentDocumentOption(callback);
            var searcher = new Searcher(
                _workspace.CurrentSolution,
                _asyncListener,
                _displayFactory,
                callback,
                searchValue,
                searchCurrentDocument,
                _cancellationTokenSource.Token);

            searcher.Search();
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
            var hostService = _hostServices.Length > 0
                ? VersionSelector.SelectHighest(_hostServices)
                : null;
            var searchCurrentDocument = hostService?.GetSearchCurrentDocument(callback.Options) ?? false;
            return searchCurrentDocument;
        }
    }
}
