// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
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

        public ISet<string> KindsProvided { get; } = ImmutableHashSet.Create(
            NavigateToItemKind.Class,
            NavigateToItemKind.Constant,
            NavigateToItemKind.Delegate,
            NavigateToItemKind.Enum,
            NavigateToItemKind.EnumItem,
            NavigateToItemKind.Event,
            NavigateToItemKind.Field,
            NavigateToItemKind.Interface,
            NavigateToItemKind.Method,
            NavigateToItemKind.Module,
            NavigateToItemKind.Property,
            NavigateToItemKind.Structure);

        public bool CanFilter => true;

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
