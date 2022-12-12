// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Evaluation.IL;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
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
            _documentSymbolUIItems = new ObservableCollection<DocumentSymbolItemViewModel>();

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
            _visualStudioCodeWindowInfoQueue = new AsyncBatchingResultQueue<VisualStudioCodeWindowInfo?>(
                DelayTimeSpan.Short,
                GetVisualStudioCodeWindowInfoAsync,
                asyncListener,
                CancellationToken);

            _documentSymbolQueue = new AsyncBatchingWorkQueue<VisualStudioCodeWindowInfo, DocumentSymbolDataModel?>(
                DelayTimeSpan.Short,
                GetDocumentSymbolAsync,
                EqualityComparer<VisualStudioCodeWindowInfo>.Default,
                asyncListener,
                CancellationToken);

            _textViewEventSource.Changed += OnEventSourceChanged;
            _textViewEventSource.Connect();

            // queue initial model update
            var service = _visualStudioCodeWindowInfoService.GetServiceAndThrowIfNotOnUIThread();
            var info = service.GetVisualStudioCodeWindowInfo();
            Assumes.NotNull(info);
            _documentSymbolQueue.AddWork(info, cancelExistingWork: true);
        }

        private string? _searchText;
        public string? SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        private SortOption _sortOption;
        public SortOption SortOption
        {
            get => _sortOption;
            set => SetProperty(ref _sortOption, value);
        }

        private readonly SemaphoreSlim _guard = new SemaphoreSlim(1);
        private ObservableCollection<DocumentSymbolItemViewModel> _documentSymbolUIItems;
        public ObservableCollection<DocumentSymbolItemViewModel> DocumentSymbolUIItems
        {
            get => _documentSymbolUIItems;
            set => SetProperty(ref _documentSymbolUIItems, value);
        }

        private void OnEventSourceChanged(object sender, TaggerEventArgs e)
            => _visualStudioCodeWindowInfoQueue.AddWork(cancelExistingWork: true);

        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            // Note: we do not lock here. Worst case is that we fire multiple
            //       NotifyPropertyChanged events which WPF can handle.
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
