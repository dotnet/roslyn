// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    /// <summary>
    /// Responsible for updating data related to Document outline.
    /// It is expected that all operations this type do not need to be on the UI thread.
    /// </summary>
    internal partial class DocumentOutlineViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly ILanguageServiceBroker2 _languageServiceBroker;
        private readonly CompilationAvailableTaggerEventSource _textViewEventSource;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ITextBuffer _textBuffer;

        private CancellationToken CancellationToken => _cancellationTokenSource.Token;

        public DocumentOutlineViewModel(
            ILanguageServiceBroker2 languageServiceBroker,
            IAsynchronousOperationListener asyncListener,
            CompilationAvailableTaggerEventSource textViewEventSource,
            ITextBuffer textBuffer,
            IThreadingContext threadingContext)
        {
            _languageServiceBroker = languageServiceBroker;
            _textViewEventSource = textViewEventSource;
            _textBuffer = textBuffer;
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(threadingContext.DisposalToken);

            // work queue for refreshing LSP data
            _documentSymbolQueue = new AsyncBatchingResultQueue<DocumentSymbolDataModel>(
                DelayTimeSpan.Short,
                GetDocumentSymbolAsync,
                asyncListener,
                CancellationToken);

            // work queue for updating UI state
            _updateViewModelStateQueue = new AsyncBatchingWorkQueue<ViewModelStateDataChange>(
                DelayTimeSpan.Short,
                UpdateViewModelStateAsync,
                asyncListener,
                CancellationToken);

            _textViewEventSource.Changed += OnEventSourceChanged;
            _textViewEventSource.Connect();

            // queue initial model update
            _documentSymbolQueue.AddWork(cancelExistingWork: true);
        }

        private SortOption _sortOption = SortOption.Location;
        public SortOption SortOption
        {
            get => _sortOption;
            set => SetProperty(ref _sortOption, value);
        }

        private ImmutableArray<DocumentSymbolDataViewModel> _documentSymbolViewModelItems = ImmutableArray<DocumentSymbolDataViewModel>.Empty;
        public ImmutableArray<DocumentSymbolDataViewModel> DocumentSymbolViewModelItems
        {
            get => _documentSymbolViewModelItems;
            set
            {
                // Unselect any currently selected items or WPF will believe it needs to select the root node.
                DocumentOutlineHelper.UnselectAll(_documentSymbolViewModelItems);
                SetProperty(ref _documentSymbolViewModelItems, value);
            }
        }

        private void OnEventSourceChanged(object sender, TaggerEventArgs e)
            => _documentSymbolQueue.AddWork(cancelExistingWork: true);

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
