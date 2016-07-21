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

            private readonly object _gate = new object();
            private ImmutableList<TableEntry> _entries = ImmutableList<TableEntry>.Empty;

            private TableEntriesSnapshot _lastSnapshot;

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

            public int CurrentVersionNumber { get; private set; }

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
                var workspace = reference.Document.Project.Solution.Workspace as VisualStudioWorkspaceImpl;
                if (workspace == null)
                {
                    return;
                }

                var projectGuid = workspace.GetHostProject(reference.Document.Project.Id)?.Guid;
                if (projectGuid == null)
                {
                    return;
                }

                try
                {
                    // We're told about this reference synchronously, but we need to get the 
                    // SourceText for the reference's Document so that we can determine things
                    // like it's line/column/text.  We don't want to block this method getting
                    // that data, so instead we just fire off the async work to get the text
                    // and use it.  Because we're starting some async work, let the test harness
                    // know so that it doesn't verify results until this completes.
                    using (var token = _presenter._asyncListener.BeginAsyncOperation(nameof(OnReferenceFound)))
                    {
                        await OnReferenceFoundAsync(definition, reference, projectGuid.Value).ConfigureAwait(false);
                    }
                }
                catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
                {
                }
            }

            private async Task OnReferenceFoundAsync(
                INavigableItem definition, INavigableItem reference, Guid projectGuid)
            {
                var cancellationToken = _cancellationTokenSource.Token;
                cancellationToken.ThrowIfCancellationRequested();

                var document = reference.Document;
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                var referenceSpan = reference.SourceSpan;
                var sourceLine = sourceText.Lines.GetLineFromPosition(referenceSpan.Start);

                var firstNonWhitespacePosition = sourceLine.GetFirstNonWhitespacePosition().Value;
                var span = TextSpan.FromBounds(firstNonWhitespacePosition, sourceLine.End);

                // TODO: highlight the actual reference span in some way.
                var classifiedLineParts = await Classifier.GetClassifiedSymbolDisplayPartsAsync(
                    semanticModel, span, document.Project.Solution.Workspace, cancellationToken).ConfigureAwait(false);

                lock (_gate)
                {
                    // Once we can make the new entry, add it to our list.
                    var entry = new TableEntry(
                        _presenter,
                        definition, reference, projectGuid,
                        sourceText, classifiedLineParts);
                    _entries = _entries.Add(entry);
                    CurrentVersionNumber++;
                }

                // Let all our subscriptions know that we've updated.
                NotifySinksOfChangedVersion();
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
                        _lastSnapshot = new TableEntriesSnapshot(_entries, CurrentVersionNumber);
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

            public void Dispose()
            {
                CancelSearch();
            }

            #endregion
        }
    }
}