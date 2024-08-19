// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Language.Intellisense;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal sealed class SourceGeneratedFileItemSource(SourceGeneratorItem parentGeneratorItem, Workspace workspace, IAsynchronousOperationListener asyncListener, IThreadingContext threadingContext) : Shell.IAttachedCollectionSource, ISupportExpansionEvents
{
    private readonly SourceGeneratorItem _parentGeneratorItem = parentGeneratorItem;
    private readonly Workspace _workspace = workspace;
    private readonly IAsynchronousOperationListener _asyncListener = asyncListener;
    private readonly IThreadingContext _threadingContext = threadingContext;

    /// <summary>
    /// The returned collection of items. Can only be mutated on the UI thread, as other parts of WPF are subscribed to the change
    /// events and expect that.
    /// </summary>
    private readonly BulkObservableCollectionWithInit<BaseItem> _items = [];

    /// <summary>
    /// Gate to guard mutation of <see cref="_resettableDelay"/>.
    /// </summary>
    private readonly object _gate = new();

    private readonly CancellationSeries _cancellationSeries = new();
    private ResettableDelay? _resettableDelay;

    public object SourceItem => _parentGeneratorItem;

    // Since we are expensive to compute, always say we have items.
    public bool HasItems => true;

    public IEnumerable Items => _items;

    private async Task UpdateSourceGeneratedFileItemsAsync(Solution solution, CancellationToken cancellationToken)
    {
        var project = solution.GetProject(_parentGeneratorItem.ProjectId);

        if (project == null)
        {
            return;
        }

        var sourceGeneratedDocuments = await project.GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false);
        var sourceGeneratedDocumentsForGeneratorById =
            sourceGeneratedDocuments.Where(d => d.Identity.Generator == _parentGeneratorItem.Identity)
            .ToDictionary(d => d.Id);

        // We must update the list on the UI thread, since the WPF elements bound to our list expect that
        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        try
        {
            // We're going to incrementally update our items, ensuring we keep the object identity for things we didn't touch.
            // This is because the Solution Explorer itself will use identity to keep track of active items -- if you have an
            // item selected and we were to refresh in the background we don't want to lose that selection. If we just removed
            // and repopulated the list from scratch each time we'd lose the selection.
            _items.BeginBulkOperation();

            // Do we already have a "no files" placeholder item?
            if (_items is [NoSourceGeneratedFilesPlaceholderItem])
            {
                // We do -- if we have no items, we're done, since the placeholder is all that needs to be there;
                // otherwise remove it since we have real files now
                if (sourceGeneratedDocumentsForGeneratorById.Count == 0)
                {
                    return;
                }
                else
                {
                    _items.RemoveAt(0);
                }
            }

            for (var i = 0; i < _items.Count; i++)
            {
                // If this item that we already have is still a generated document, we'll remove it from our list; the list when we're
                // done is going to have the new items remaining. If it no longer exists, remove it from list.
                if (!sourceGeneratedDocumentsForGeneratorById.Remove(((SourceGeneratedFileItem)_items[i]).DocumentId))
                {
                    _items.RemoveAt(i);
                    i--;
                }
            }

            // Whatever is left in sourceGeneratedDocumentsForGeneratorById we should add; if we have nothing to add and nothing
            // in the list after removing anything, then we should add the placeholder.
            if (sourceGeneratedDocumentsForGeneratorById.Count == 0 && _items.Count == 0)
            {
                _items.Add(new NoSourceGeneratedFilesPlaceholderItem());
                return;
            }

            foreach (var document in sourceGeneratedDocumentsForGeneratorById.Values)
            {
                // Binary search to figure out where to insert
                var low = 0;
                var high = _items.Count;

                while (low < high)
                {
                    var mid = (low + high) / 2;

                    if (StringComparer.OrdinalIgnoreCase.Compare(document.HintName, ((SourceGeneratedFileItem)_items[mid]).HintName) < 0)
                    {
                        high = mid;
                    }
                    else
                    {
                        low = mid + 1;
                    }
                }

                _items.Insert(low, new SourceGeneratedFileItem(
                    _threadingContext, document.Id, document.HintName, document.Project.Language, _workspace));
            }
        }
        finally
        {
            _items.EndBulkOperation();
            _items.MarkAsInitialized();
        }
    }

    public void BeforeExpand()
    {
        lock (_gate)
        {
            // We should not have an existing computation active
            Contract.ThrowIfTrue(_cancellationSeries.HasActiveToken);

            var cancellationToken = _cancellationSeries.CreateNext();
            var asyncToken = _asyncListener.BeginAsyncOperation(nameof(SourceGeneratedFileItemSource) + "." + nameof(BeforeExpand));

            Task.Run(
                async () =>
                {
                    // Since the user just expanded this, we want to do a single population aggressively,
                    // where the only reason we'd cancel is if the user collapsed it again.
                    var solution = _workspace.CurrentSolution;
                    await UpdateSourceGeneratedFileItemsAsync(solution, cancellationToken).ConfigureAwait(false);

                    // Now that we've done it the first time, we'll subscribe for future changes
                    lock (_gate)
                    {
                        // It's important we check for cancellation inside our lock: if the user were to collapse
                        // right at this point, we don't want to have a case where we cancelled the work, unsubscribed
                        // in AfterCollapse, and _then_ subscribed here again.

                        cancellationToken.ThrowIfCancellationRequested();
                        _workspace.WorkspaceChanged += OnWorkpaceChanged;
                        if (_workspace.CurrentSolution != solution)
                        {
                            // The workspace changed while we were doing our initial population, so
                            // refresh it. We'll just call our OnWorkspaceChanged event handler
                            // so this looks like any other change.
                            OnWorkpaceChanged(this,
                                new WorkspaceChangeEventArgs(WorkspaceChangeKind.SolutionChanged, solution, _workspace.CurrentSolution));
                        }
                    }
                },
                cancellationToken).CompletesAsyncOperation(asyncToken);
        }
    }

    public void AfterCollapse()
    {
        StopUpdating();
    }

    private void StopUpdating()
    {
        lock (_gate)
        {
            _cancellationSeries.CreateNext(new CancellationToken(canceled: true));
            _workspace.WorkspaceChanged -= OnWorkpaceChanged;
            _resettableDelay = null;
        }
    }

    private void OnWorkpaceChanged(object sender, WorkspaceChangeEventArgs e)
    {
        if (!e.NewSolution.ContainsProject(_parentGeneratorItem.ProjectId))
        {
            StopUpdating();
        }

        lock (_gate)
        {
            // If we already have a ResettableDelay, just delay it further; otherwise we either have no delay
            // or the actual processing began, and we need to start over
            if (_resettableDelay != null)
            {
                _resettableDelay.Reset();
            }
            else
            {
                // Time to start the work all over again. We'll ensure any previous work is cancelled
                var cancellationToken = _cancellationSeries.CreateNext();
                var asyncToken = _asyncListener.BeginAsyncOperation(nameof(SourceGeneratedFileItemSource) + "." + nameof(OnWorkpaceChanged));

                // We're going to go with a really long delay: once the user expands this we will keep it updated, but it's fairly
                // unlikely to change in a lot of cases if a generator only produces a stable set of names.
                _resettableDelay = new ResettableDelay(delayInMilliseconds: 5000, _asyncListener, cancellationToken);
                _resettableDelay.Task.ContinueWith(_ =>
                {
                    lock (_gate)
                    {
                        // We've started off this work, so if another change comes in we need to start a delay all over again
                        _resettableDelay = null;
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    return UpdateSourceGeneratedFileItemsAsync(_workspace.CurrentSolution, cancellationToken);
                }, cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default).Unwrap().CompletesAsyncOperation(asyncToken);
            }
        }
    }

    /// <summary>
    /// This derivation of <see cref="ObservableCollection{T}"/> also supports raising an initialized event through
    /// <see cref="ISupportInitializeNotification"/>. This is used to show the spinning icon in the solution explorer
    /// the first time you expand it.
    /// </summary>
    private sealed class BulkObservableCollectionWithInit<T> : BulkObservableCollection<T>, ISupportInitializeNotification
    {
        public bool IsInitialized { get; private set; } = false;

        public event EventHandler? Initialized;

        void ISupportInitialize.BeginInit()
        {
        }

        void ISupportInitialize.EndInit()
        {
        }

        public void MarkAsInitialized()
        {
            if (!IsInitialized)
            {
                IsInitialized = true;
                Initialized?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
