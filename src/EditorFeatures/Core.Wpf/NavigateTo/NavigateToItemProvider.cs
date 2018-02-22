// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
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

#pragma warning disable CA2213 // Disposable fields should be disposed - field is disposed in a helper method. Remove this suppression once https://github.com/dotnet/roslyn-analyzers/issues/1594 is fixed.
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
#pragma warning restore CA2213 // Disposable fields should be disposed

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

        public void StopSearch()
        {
            StopSearchCore(disposing: false);
        }

        private void StopSearchCore(bool disposing)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = !disposing ? new CancellationTokenSource() : null;
        }

        public void Dispose()
        {
            this.StopSearch();
            (_displayFactory as IDisposable)?.Dispose();
        }

        public void StartSearch(INavigateToCallback callback, string searchValue)
        {
            this.StopSearchCore(disposing: true);

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
            var options2 = callback.Options as INavigateToOptions2;
            return options2?.SearchCurrentDocument ?? false;
        }
    }
}
