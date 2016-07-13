﻿using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.FindReferences
{
    internal partial class AsyncFindReferencesPresenter
    {
        private class DataSource : FindReferencesContext, ITableDataSource, ITableEntriesSnapshotFactory
        {
            private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
            private readonly ConcurrentBag<Subscription> _subscriptions = new ConcurrentBag<Subscription>();

            private readonly ClassificationTypeMap _typeMap;
            private readonly IAsynchronousOperationListener _asyncListener;

            private readonly object _gate = new object();
            private ImmutableList<TableEntry> _entries = ImmutableList<TableEntry>.Empty;

            private TableEntriesSnapshot _lastSnapshot;

            public DataSource(ClassificationTypeMap typeMap, IAsynchronousOperationListener asyncListener)
            {
                _typeMap = typeMap;
                _asyncListener = asyncListener;
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
                var subscription = new Subscription(_cancellationTokenSource, sink);
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
                    using (var token = _asyncListener.BeginAsyncOperation(nameof(OnReferenceFound)))
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

                var document = reference.Document;
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                var referenceSpan = reference.SourceSpan;
                var sourceLine = sourceText.Lines.GetLineFromPosition(referenceSpan.Start);

                var firstNonWhitespacePosition = sourceLine.GetFirstNonWhitespacePosition().Value;
                var span = TextSpan.FromBounds(firstNonWhitespacePosition, sourceLine.End);

                var classifiedLineParts = await Classifier.GetClassifiedSymbolDisplayPartsAsync(
                    semanticModel, span, document.Project.Solution.Workspace, cancellationToken).ConfigureAwait(false);

                lock (_gate)
                {
                    // Once we can make the new entry, add it to our list.
                    var entry = new TableEntry(
                        definition, reference, projectGuid, 
                        sourceText, classifiedLineParts, _typeMap);
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
                _cancellationTokenSource.Cancel();
            }

            #endregion
        }
    }
}
