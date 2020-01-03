// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Shell.FindAllReferences;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    internal partial class StreamingFindUsagesPresenter
    {
        private abstract class AbstractTableDataSourceFindUsagesContext :
            FindUsagesContext, ITableDataSource, ITableEntriesSnapshotFactory
        {
            private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

            private ITableDataSink _tableDataSink;

            public readonly StreamingFindUsagesPresenter Presenter;
            private readonly IFindAllReferencesWindow _findReferencesWindow;
            protected readonly IWpfTableControl2 TableControl;

            protected readonly object Gate = new object();

            #region Fields that should be locked by _gate

            /// <summary>
            /// If we've been cleared or not.  If we're cleared we'll just return an empty
            /// list of results whenever queried for the current snapshot.
            /// </summary>
            private bool _cleared;

            /// <summary>
            /// The list of all definitions we've heard about.  This may be a superset of the
            /// keys in <see cref="_definitionToBucket"/> because we may encounter definitions
            /// we don't create definition buckets for.  For example, if the definition asks
            /// us to not display it if it has no references, and we don't run into any 
            /// references for it (common with implicitly declared symbols).
            /// </summary>
            protected readonly List<DefinitionItem> Definitions = new List<DefinitionItem>();

            /// <summary>
            /// We will hear about the same definition over and over again.  i.e. for each reference 
            /// to a definition, we will be told about the same definition.  However, we only want to
            /// create a single actual <see cref="DefinitionBucket"/> for the definition. To accomplish
            /// this we keep a map from the definition to the task that we're using to create the 
            /// bucket for it.  The first time we hear about a definition we'll make a single task
            /// and then always return that for all future references found.
            /// </summary>
            private readonly Dictionary<DefinitionItem, RoslynDefinitionBucket> _definitionToBucket =
                new Dictionary<DefinitionItem, RoslynDefinitionBucket>();

            /// <summary>
            /// We want to hide declarations of a symbol if the user is grouping by definition.
            /// With such grouping on, having both the definition group and the declaration item
            /// is just redundant.  To make life easier we keep around two groups of entries.
            /// One group for when we are grouping by definition, and one when we're not.
            /// </summary>
            private bool _currentlyGroupingByDefinition;

            protected ImmutableList<Entry> EntriesWhenNotGroupingByDefinition = ImmutableList<Entry>.Empty;
            protected ImmutableList<Entry> EntriesWhenGroupingByDefinition = ImmutableList<Entry>.Empty;

            private TableEntriesSnapshot _lastSnapshot;
            public int CurrentVersionNumber { get; protected set; }

            #endregion

            protected AbstractTableDataSourceFindUsagesContext(
                 StreamingFindUsagesPresenter presenter,
                 IFindAllReferencesWindow findReferencesWindow,
                 ImmutableArray<ITableColumnDefinition> customColumns,
                 bool includeContainingTypeAndMemberColumns,
                 bool includeKindColumn)
            {
                presenter.AssertIsForeground();

                Presenter = presenter;
                _findReferencesWindow = findReferencesWindow;
                TableControl = (IWpfTableControl2)findReferencesWindow.TableControl;
                TableControl.GroupingsChanged += OnTableControlGroupingsChanged;

                // If the window is closed, cancel any work we're doing.
                _findReferencesWindow.Closed += OnFindReferencesWindowClosed;

                DetermineCurrentGroupingByDefinitionState();

                Debug.Assert(_findReferencesWindow.Manager.Sources.Count == 0);

                // Add ourselves as the source of results for the window.
                // Additionally, add applicable custom columns to display custom reference information
                _findReferencesWindow.Manager.AddSource(
                    this,
                    SelectCustomColumnsToInclude(customColumns, includeContainingTypeAndMemberColumns, includeKindColumn));

                // After adding us as the source, the manager should immediately call into us to
                // tell us what the data sink is.
                Debug.Assert(_tableDataSink != null);
            }

            private static ImmutableArray<string> SelectCustomColumnsToInclude(ImmutableArray<ITableColumnDefinition> customColumns, bool includeContainingTypeAndMemberColumns, bool includeKindColumn)
            {
                var customColumnsToInclude = ArrayBuilder<string>.GetInstance();

                foreach (var column in customColumns)
                {
                    switch (column.Name)
                    {
                        case AbstractReferenceFinder.ContainingMemberInfoPropertyName:
                        case AbstractReferenceFinder.ContainingTypeInfoPropertyName:
                            if (includeContainingTypeAndMemberColumns)
                            {
                                customColumnsToInclude.Add(column.Name);
                            }

                            break;

                        case StandardTableColumnDefinitions2.SymbolKind:
                            if (includeKindColumn)
                            {
                                customColumnsToInclude.Add(column.Name);
                            }

                            break;
                    }
                }

                return customColumnsToInclude.ToImmutableAndFree();
            }

            protected void NotifyChange()
                => _tableDataSink.FactorySnapshotChanged(this);

            private void OnFindReferencesWindowClosed(object sender, EventArgs e)
            {
                Presenter.AssertIsForeground();
                CancelSearch();

                _findReferencesWindow.Closed -= OnFindReferencesWindowClosed;
                TableControl.GroupingsChanged -= OnTableControlGroupingsChanged;
            }

            private void OnTableControlGroupingsChanged(object sender, EventArgs e)
            {
                Presenter.AssertIsForeground();
                UpdateGroupingByDefinition();
            }

            private void UpdateGroupingByDefinition()
            {
                Presenter.AssertIsForeground();
                var changed = DetermineCurrentGroupingByDefinitionState();

                if (changed)
                {
                    // We changed from grouping-by-definition to not (or vice versa).
                    // Change which list we show the user.
                    lock (Gate)
                    {
                        CurrentVersionNumber++;
                    }

                    // Let all our subscriptions know that we've updated.  That way they'll refresh
                    // and we'll show/hide declarations as appropriate.
                    NotifyChange();
                }
            }

            private bool DetermineCurrentGroupingByDefinitionState()
            {
                Presenter.AssertIsForeground();

                var definitionColumn = _findReferencesWindow.GetDefinitionColumn();

                lock (Gate)
                {
                    var oldGroupingByDefinition = _currentlyGroupingByDefinition;
                    _currentlyGroupingByDefinition = definitionColumn?.GroupingPriority > 0;

                    return oldGroupingByDefinition != _currentlyGroupingByDefinition;
                }
            }

            private void CancelSearch()
            {
                Presenter.AssertIsForeground();
                _cancellationTokenSource.Cancel();
            }

            public sealed override CancellationToken CancellationToken => _cancellationTokenSource.Token;

            public void Clear()
            {
                this.Presenter.AssertIsForeground();

                // Stop all existing work.
                this.CancelSearch();

                // Clear the title of the window.  It will go back to the default editor title.
                this._findReferencesWindow.Title = null;

                lock (Gate)
                {
                    // Mark ourselves as clear so that no further changes are made.
                    // Note: we don't actually mutate any of our entry-lists.  Instead, 
                    // GetCurrentSnapshot will simply ignore them if it sees that _cleared
                    // is true.  This way we don't have to do anything complicated if we
                    // keep hearing about definitions/references on the background.
                    _cleared = true;
                    CurrentVersionNumber++;
                }

                // Let all our subscriptions know that we've updated.  That way they'll refresh
                // and remove all the data.
                NotifyChange();
            }

            #region ITableDataSource

            public string DisplayName => "Roslyn Data Source";

            public string Identifier
                => StreamingFindUsagesPresenter.RoslynFindUsagesTableDataSourceIdentifier;

            public string SourceTypeIdentifier
                => StreamingFindUsagesPresenter.RoslynFindUsagesTableDataSourceSourceTypeIdentifier;

            public IDisposable Subscribe(ITableDataSink sink)
            {
                Presenter.AssertIsForeground();

                Debug.Assert(_tableDataSink == null);
                _tableDataSink = sink;

                _tableDataSink.AddFactory(this, removeAllFactories: true);
                _tableDataSink.IsStable = false;

                return this;
            }

            #endregion

            #region FindUsagesContext overrides.

            public sealed override Task SetSearchTitleAsync(string title)
            {
                // Note: IFindAllReferenceWindow.Title is safe to set from any thread.
                _findReferencesWindow.Title = title;
                return Task.CompletedTask;
            }

            public sealed override async Task OnCompletedAsync()
            {
                await OnCompletedAsyncWorkerAsync().ConfigureAwait(false);

                _tableDataSink.IsStable = true;
            }

            protected abstract Task OnCompletedAsyncWorkerAsync();

            public sealed override Task OnDefinitionFoundAsync(DefinitionItem definition)
            {
                lock (Gate)
                {
                    Definitions.Add(definition);
                }

                return OnDefinitionFoundWorkerAsync(definition);
            }

            protected abstract Task OnDefinitionFoundWorkerAsync(DefinitionItem definition);

            protected async Task<Entry> TryCreateDocumentSpanEntryAsync(
                RoslynDefinitionBucket definitionBucket,
                DocumentSpan documentSpan,
                HighlightSpanKind spanKind,
                SymbolUsageInfo symbolUsageInfo,
                ImmutableDictionary<string, string> additionalProperties)
            {
                var document = documentSpan.Document;
                var (projectGuid, documentGuid, projectName, sourceText) = await FindUsagesUtilities.GetGuidAndProjectNameAndSourceTextAsync(document, CancellationToken).ConfigureAwait(false);
                var (excerptResult, lineText) = await FindUsagesUtilities.ExcerptAsync(sourceText, documentSpan, CancellationToken).ConfigureAwait(false);

                var mappedDocumentSpan = await FindUsagesUtilities.TryMapAndGetFirstAsync(documentSpan, sourceText, CancellationToken).ConfigureAwait(false);
                if (mappedDocumentSpan == null)
                {
                    // this will be removed from the result
                    return null;
                }

                return new DocumentSpanEntry(
                    this, definitionBucket, spanKind, projectName,
                    projectGuid, mappedDocumentSpan.Value, excerptResult, lineText, symbolUsageInfo, additionalProperties);
            }

            public sealed override Task OnReferenceFoundAsync(SourceReferenceItem reference)
            {
                return OnReferenceFoundWorkerAsync(reference);
            }

            protected abstract Task OnReferenceFoundWorkerAsync(SourceReferenceItem reference);

            protected RoslynDefinitionBucket GetOrCreateDefinitionBucket(DefinitionItem definition)
            {
                lock (Gate)
                {
                    if (!_definitionToBucket.TryGetValue(definition, out var bucket))
                    {
                        bucket = new RoslynDefinitionBucket(Presenter, this, definition);
                        _definitionToBucket.Add(definition, bucket);
                    }

                    return bucket;
                }
            }

            public sealed override Task ReportProgressAsync(int current, int maximum)
            {
                // https://devdiv.visualstudio.com/web/wi.aspx?pcguid=011b8bdf-6d56-4f87-be0d-0092136884d9&id=359162
                // Right now VS actually responds to each SetProgess call by enqueueing a UI task
                // to do the progress bar update.  This can made FindReferences feel extremely slow
                // when thousands of SetProgress calls are made.  So, for now, we're removing
                // the progress update until the FindRefs window fixes that perf issue.
#if false
                try
                {
                    // The original FAR window exposed a SetProgress(double). Ensure that we 
                    // don't crash if this code is running on a machine without the new API.
                    _findReferencesWindow.SetProgress(current, maximum);
                }
                catch
                {
                }
#endif

                return Task.CompletedTask;
            }

            #endregion

            #region ITableEntriesSnapshotFactory

            public ITableEntriesSnapshot GetCurrentSnapshot()
            {
                lock (Gate)
                {
                    // If our last cached snapshot matches our current version number, then we
                    // can just return it.  Otherwise, we need to make a snapshot that matches
                    // our version.
                    if (_lastSnapshot?.VersionNumber != CurrentVersionNumber)
                    {
                        // If we've been cleared, then just return an empty list of entries.
                        // Otherwise return the appropriate list based on how we're currently
                        // grouping.
                        var entries = _cleared
                            ? ImmutableList<Entry>.Empty
                            : _currentlyGroupingByDefinition
                                ? EntriesWhenGroupingByDefinition
                                : EntriesWhenNotGroupingByDefinition;

                        _lastSnapshot = new TableEntriesSnapshot(entries, CurrentVersionNumber);
                    }

                    return _lastSnapshot;
                }
            }

            public ITableEntriesSnapshot GetSnapshot(int versionNumber)
            {
                lock (Gate)
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
                NotifyChange();
                return null;
            }

            void IDisposable.Dispose()
            {
                this.Presenter.AssertIsForeground();

                // VS is letting go of us.  i.e. because a new FAR call is happening, or because
                // of some other event (like the solution being closed).  Remove us from the set
                // of sources for the window so that the existing data is cleared out.
                Debug.Assert(_findReferencesWindow.Manager.Sources.Count == 1);
                Debug.Assert(_findReferencesWindow.Manager.Sources[0] == this);

                _findReferencesWindow.Manager.RemoveSource(this);

                CancelSearch();

                // Remove ourselves from the list of contexts that are currently active.
                Presenter._currentContexts.Remove(this);
            }

            #endregion
        }
    }
}
