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
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    internal partial class DocumentOutlineViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly ILanguageServiceBroker2 _languageServiceBroker;
        private readonly VisualStudioCodeWindowInfoService _visualStudioCodeWindowInfoService;
        private readonly CompilationAvailableTaggerEventSource _textViewEventSource;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly IDocumentNavigationService _navigationService;
        private readonly Workspace _workspace;
        private CancellationToken CancellationToken => _cancellationTokenSource.Token;

        public DocumentOutlineViewModel(
            ILanguageServiceBroker2 languageServiceBroker,
            IAsynchronousOperationListener asyncListener,
            VisualStudioCodeWindowInfoService visualStudioCodeWindowInfoService,
            CompilationAvailableTaggerEventSource textViewEventSource,
            Workspace workspace,
            IDocumentNavigationService documentNavigationService)
        {
            _languageServiceBroker = languageServiceBroker;
            _visualStudioCodeWindowInfoService = visualStudioCodeWindowInfoService;
            _textViewEventSource = textViewEventSource;
            _workspace = workspace;
            _navigationService = documentNavigationService;
            _cancellationTokenSource = new CancellationTokenSource();

            // initialize public properties
            _sortOption = SortOption.Location;
            _documentSymbolViewModelItems = new ObservableCollection<DocumentSymbolItemViewModel>();

            // event queues for updating view model state
            _expandCollapseQueue = new AsyncBatchingWorkQueue<ExpansionOption>(
                DelayTimeSpan.NearImmediate,
                ExpandCollapseItemsAsync,
                asyncListener,
                CancellationToken);

            _selectTreeNodeQueue = new AsyncBatchingWorkQueue<CaretPosition>(
                DelayTimeSpan.Short,
                SelectTreeNodeAsync,
                asyncListener,
                CancellationToken);

            _navigationQueue = new AsyncBatchingWorkQueue<TextSpan>(
                DelayTimeSpan.NearImmediate,
                NavigateToTextSpanAsync,
                asyncListener,
                CancellationToken);

            // work queues for refreshing LSP data
            _visualStudioCodeWindowInfoQueue = new AsyncBatchingResultQueue<DocumentSymbolsRequestInfo?>(
                DelayTimeSpan.Short,
                GetVisualStudioCodeWindowInfoAsync,
                asyncListener,
                CancellationToken);

            _documentSymbolQueue = new AsyncBatchingWorkQueue<DocumentSymbolsRequestInfo, DocumentSymbolDataModel?>(
                DelayTimeSpan.Short,
                GetDocumentSymbolAsync,
                EqualityComparer<DocumentSymbolsRequestInfo>.Default,
                asyncListener,
                CancellationToken);

            _filterQueue = new AsyncBatchingWorkQueue<string>(
                DelayTimeSpan.Short,
                FilterTreeAsync,
                asyncListener,
                CancellationToken);

            _textViewEventSource.Changed += OnEventSourceChanged;
            _textViewEventSource.Connect();

            // queue initial model update
            var service = _visualStudioCodeWindowInfoService.GetServiceAndThrowIfNotOnUIThread();
            var info = service.GetDocumentSymbolsRequestInfo();
            Assumes.NotNull(info);
            _documentSymbolQueue.AddWork(info, cancelExistingWork: true);
        }

        private SortOption _sortOption;
        public SortOption SortOption
        {
            get => _sortOption;
            set => SetProperty(ref _sortOption, value);
        }

        private readonly SemaphoreSlim _guard = new(1);
        private ObservableCollection<DocumentSymbolItemViewModel> _documentSymbolViewModelItems;
        public ObservableCollection<DocumentSymbolItemViewModel> DocumentSymbolViewModelItems
        {
            get => _documentSymbolViewModelItems;
            set => SetProperty(ref _documentSymbolViewModelItems, value);
        }

        private void OnEventSourceChanged(object sender, TaggerEventArgs e)
            => _visualStudioCodeWindowInfoQueue.AddWork(cancelExistingWork: true);

        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            field = value;
            NotifyPropertyChanged(propertyName);
        }

        public void Dispose()
        {
            _textViewEventSource.Changed -= OnEventSourceChanged;
            _textViewEventSource.Disconnect();
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }
    }
}
