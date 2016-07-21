using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.FindAllReferences;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal partial class AsyncFindReferencesPresenter
    {
        private class TableDataSourceFindReferencesContext :
            FindReferencesContext, ITableDataSource, ITableEntriesSnapshotFactory
        {
            private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

            private readonly ConcurrentBag<Subscription> _subscriptions = new ConcurrentBag<Subscription>();

            private readonly AsyncFindReferencesPresenter _presenter;
            private readonly IFindAllReferencesWindow _findReferencesWindow;

            // Lock which protects _definitionToBucketTask, _entries, _lastSnapshot and _currentVersionNumber
            private readonly object _gate = new object();

            /// <summary>
            /// We will hear about the same definition over and over again.  i.e. for each reference 
            /// to a definition, we will be told about the same definition.  However, we only want to
            /// create a single actual <see cref="DefinitionBucket"/> for the definition. To accomplish
            /// this we keep a map from the definition to the task that we're using to create the 
            /// bucket for it.  The first time we hear about a definition we'll make a single task
            /// and then always return that for all future references found.
            /// </summary>
            private readonly Dictionary<INavigableItem, Task<RoslynDefinitionBucket>> _definitionToBucketTask =
                new Dictionary<INavigableItem, Task<RoslynDefinitionBucket>>();

            private ImmutableList<ReferenceEntry> _referenceEntries = ImmutableList<ReferenceEntry>.Empty;
            private TableEntriesSnapshot _lastSnapshot;
            public int CurrentVersionNumber { get; private set; }

            public TableDataSourceFindReferencesContext(
                 AsyncFindReferencesPresenter presenter,
                 IFindAllReferencesWindow findReferencesWindow)
            {
                presenter.AssertIsForeground();

                _presenter = presenter;
                _findReferencesWindow = findReferencesWindow;

                // If the window is closed, cancel any work we're doing.
                _findReferencesWindow.Closed += (s, e) => CancelSearch();

                // Remove any existing sources in the window.  
                foreach (var source in findReferencesWindow.Manager.Sources.ToArray())
                {
                    findReferencesWindow.Manager.RemoveSource(source);
                }

                // And add ourselves as the source of results for the window.
                findReferencesWindow.Manager.AddSource(this);
            }

            private void CancelSearch()
            {
                _cancellationTokenSource.Cancel();
            }

            internal void OnSubscriptionDisposed()
            {
                CancelSearch();
            }

            public override CancellationToken CancellationToken => _cancellationTokenSource.Token;

            #region ITableDataSource

            public string DisplayName => "Roslyn Data Source";

            public string Identifier => "Roslyn Identifier";

            /// <summary>
            /// This value is expected by the "FindAllReferences" <see cref="ITableManager"/>.
            /// Do not change it.
            /// </summary>
            public string SourceTypeIdentifier => "FindAllReferencesProvider";

            public IDisposable Subscribe(ITableDataSink sink)
            {
                var subscription = new Subscription(this, sink);
                _subscriptions.Add(subscription);

                sink.AddFactory(this, removeAllFactories: true);

                return subscription;
            }

            #endregion

            #region EditorFindReferencesContext overrides.

            public override void OnStarted()
            {
                foreach (var subscription in _subscriptions)
                {
                    subscription.TableDataSink.IsStable = false;
                }
            }

            public override void OnCompleted()
            {
                foreach (var subscription in _subscriptions)
                {
                    subscription.TableDataSink.IsStable = true;
                }
            }

            public async override void OnReferenceFound(INavigableItem definition, INavigableItem reference)
            {
                try
                {
                    // We're told about this reference synchronously, but we need to get the 
                    // SourceText for the definition/reference's Document so that we can determine 
                    // things like it's line/column/text.  We don't want to block this method getting
                    // that data, so instead we just fire off the async work to get the text
                    // and use it.  Because we're starting some async work, let the test harness
                    // know so that it doesn't verify results until this completes.
                    using (var token = _presenter._asyncListener.BeginAsyncOperation(nameof(OnReferenceFound)))
                    {
                        await OnReferenceFoundAsync(definition, reference).ConfigureAwait(false);
                    }
                }
                catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
                {
                }
            }

            private async Task OnReferenceFoundAsync(
                INavigableItem definition, INavigableItem referenceItem)
            {
                var cancellationToken = _cancellationTokenSource.Token;
                cancellationToken.ThrowIfCancellationRequested();

                // First find the bucket corresponding to our definition. If we can't find/create 
                // one, then don't do anything for this reference.
                var definitionBucket = await GetOrCreateDefinitionBucketAsync(
                    definition, cancellationToken).ConfigureAwait(false);
                if (definitionBucket == null)
                {
                    return;
                }

                // Now make the underlying data object for the reference.
                var entryData = await this.TryCreateNavigableItemEntryData(
                    referenceItem, displayGlyph: false, cancellationToken: cancellationToken).ConfigureAwait(false);
                var referenceEntry = new ReferenceEntry(definitionBucket, referenceItem, entryData);

                lock (_gate)
                {
                    // Once we can make the new entry, add it to our list.
                    _referenceEntries = _referenceEntries.Add(referenceEntry);
                    CurrentVersionNumber++;
                }

                // Let all our subscriptions know that we've updated.
                NotifySinksOfChangedVersion();
            }

            private async Task<NavigableItemEntryData> TryCreateNavigableItemEntryData(
                INavigableItem item, bool displayGlyph, CancellationToken cancellationToken)
            {
                var document = item.Document;

                // The FAR system needs to know the guid for the project that a def/reference is 
                // from.  So we only support this for documents from a VSWorkspace.
                var workspace = document.Project.Solution.Workspace as VisualStudioWorkspaceImpl;
                if (workspace == null)
                {
                    return null;
                }

                var projectGuid = workspace.GetHostProject(document.Project.Id)?.Guid;
                if (projectGuid == null)
                {
                    return null;
                }

                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                var referenceSpan = item.SourceSpan;
                var sourceLine = sourceText.Lines.GetLineFromPosition(referenceSpan.Start);

                var firstNonWhitespacePosition = sourceLine.GetFirstNonWhitespacePosition().Value;
                var span = TextSpan.FromBounds(firstNonWhitespacePosition, sourceLine.End);

                // TODO: highlight the actual reference span in some way.
                var classifiedLineParts = await Classifier.GetClassifiedSymbolDisplayPartsAsync(
                    semanticModel, span, document.Project.Solution.Workspace, cancellationToken).ConfigureAwait(false);

                return new NavigableItemEntryData(
                    _presenter, item, projectGuid.Value, sourceText, classifiedLineParts, displayGlyph);
            }

            private Task<RoslynDefinitionBucket> GetOrCreateDefinitionBucketAsync(
                INavigableItem definition, CancellationToken cancellationToken)
            {
                lock (_gate)
                {
                    Task<RoslynDefinitionBucket> bucketTask;
                    if (!_definitionToBucketTask.TryGetValue(definition, out bucketTask))
                    {
                        bucketTask = CreateDefinitionBucketAsync(definition, cancellationToken);
                        _definitionToBucketTask.Add(definition, bucketTask);
                    }

                    return bucketTask;
                }
            }

            private async Task<RoslynDefinitionBucket> CreateDefinitionBucketAsync(
                INavigableItem definitionItem, CancellationToken cancellationToken)
            {
                var entryData = await TryCreateNavigableItemEntryData(
                    definitionItem, displayGlyph: true, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (entryData == null)
                {
                    return null;
                }

                return new RoslynDefinitionBucket(this, definitionItem, entryData);
            }

            private void NotifySinksOfChangedVersion()
            {
                foreach (var subscription in _subscriptions)
                {
                    subscription.TableDataSink.FactorySnapshotChanged(this);
                }
            }

            public override void ReportProgress(int current, int maximum)
            {
                var progress = maximum == 0 ? 0 : ((double)current / maximum);
                _findReferencesWindow.SetProgress(progress);
            }

            #endregion

            #region ITableEntriesSnapshotFactory

            public ITableEntriesSnapshot GetCurrentSnapshot()
            {
                lock (_gate)
                {
                    // If our last cached snapshot matches our current version number, then we
                    // can just return it.  Otherwise, we need to make a snapshot that matches
                    // our version.
                    if (_lastSnapshot?.VersionNumber != CurrentVersionNumber)
                    {
                        _lastSnapshot = new TableEntriesSnapshot(_referenceEntries, CurrentVersionNumber);
                    }

                    return _lastSnapshot;
                }
            }

            public ITableEntriesSnapshot GetSnapshot(int versionNumber)
            {
                lock (_gate)
                {
                    if (_lastSnapshot?.VersionNumber == versionNumber)
                    {
                        return _lastSnapshot;
                    }

                    if (versionNumber == CurrentVersionNumber)
                    {
                        return GetCurrentSnapshot();
                    }
                }

                // We didn't have this version.  Notify the sinks that something must have changed
                // so that they call back into us with the latest version.
                NotifySinksOfChangedVersion();
                return null;
            }

            void IDisposable.Dispose()
            {
                CancelSearch();
            }

            #endregion
        }
    }
}