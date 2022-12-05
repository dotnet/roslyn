// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    internal partial class DocumentOutlineViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly ILanguageServiceBroker2 _languageServiceBroker;
        private readonly VisualStudioCodeWindowInfoService _visualStudioCodeWindowInfoService;
        private readonly CompilationAvailableTaggerEventSource _textViewEventSource;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private CancellationToken CancellationToken => _cancellationTokenSource.Token;

        public DocumentOutlineViewModel(
            ILanguageServiceBroker2 languageServiceBroker,
            IAsynchronousOperationListener asyncListener,
            VisualStudioCodeWindowInfoService visualStudioCodeWindowInfoService,
            CompilationAvailableTaggerEventSource textViewEventSource)
        {
            _languageServiceBroker = languageServiceBroker;
            _visualStudioCodeWindowInfoService = visualStudioCodeWindowInfoService;
            _textViewEventSource = textViewEventSource;
            _cancellationTokenSource = new CancellationTokenSource();

            // initialize public properties
            _sortOption = SortOption.Location;
            _documentSymbolUIItems = new ObservableCollection<DocumentSymbolUIItem>();

            // setup work queues
            _documentSymbolQueue = new AsyncBatchingWorkQueue<VisualStudioCodeWindowInfo, DocumentSymbolDataModel?>(
                DelayTimeSpan.Short,
                GetDocumentSymbolAsync,
                EqualityComparer<VisualStudioCodeWindowInfo>.Default,
                asyncListener,
                CancellationToken);

            _filterAndSortQueue = new AsyncBatchingWorkQueue<FilterAndSortOptions, DocumentSymbolDataModel?>(
                DelayTimeSpan.NearImmediate,
                FilterAndSortDataModelAsync,
                EqualityComparer<FilterAndSortOptions>.Default,
                asyncListener,
                CancellationToken);

            _updateUIQueue = new AsyncBatchingWorkQueue<UIData>(
                DelayTimeSpan.NearImmediate,
                UpdateUIAsync,
                asyncListener,
                CancellationToken);

            _textViewEventSource.Changed += OnEventSourceChanged;
            _textViewEventSource.Connect();

            // queue initial model update
            EnqueueModelUpdateAsync().Forget();
        }

        private string? _searchText;
        public string? SearchText
        {
            get
            {
                return _searchText;
            }
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private SortOption _sortOption;
        public SortOption SortOption
        {
            get
            {
                return _sortOption;
            }
            set
            {
                if (_sortOption != value)
                {
                    _sortOption = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private ObservableCollection<DocumentSymbolUIItem> _documentSymbolUIItems;
        public ObservableCollection<DocumentSymbolUIItem> DocumentSymbolUIItems
        {
            get => _documentSymbolUIItems;
            set
            {
                _documentSymbolUIItems = value;
                NotifyPropertyChanged();
            }
        }

        private void OnEventSourceChanged(object sender, TaggerEventArgs e)
            => EnqueueModelUpdateAsync().Forget();

        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public void Dispose()
        {
            _textViewEventSource.Changed -= OnEventSourceChanged;
            _textViewEventSource.Disconnect();
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }
    }
}
