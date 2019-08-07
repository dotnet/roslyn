// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell.FindAllReferences;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Roslyn.Utilities;

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

            /// <summary>
            /// Map from custom column names to column states.
            /// </summary>
            private readonly Dictionary<string, ColumnState2> _customColumnTitleToStatesMap;

            #endregion

            protected AbstractTableDataSourceFindUsagesContext(
                 StreamingFindUsagesPresenter presenter,
                 IFindAllReferencesWindow findReferencesWindow,
                 ImmutableArray<AbstractCustomColumnDefinition> customColumns)
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

                // And add ourselves as the source of results for the window.
                // Additionally, add custom columns to display custom reference information.
                _findReferencesWindow.Manager.AddSource(this, customColumns.SelectAsArray(c => c.Name));

                // After adding us as the source, the manager should immediately call into us to
                // tell us what the data sink is.
                Debug.Assert(_tableDataSink != null);

                // Initialize custom column states at start of the FAR query.
                _customColumnTitleToStatesMap = GetInitialCustomColumnStates(findReferencesWindow.TableControl.ColumnStates, customColumns);

                // Now update the custom columns' state/visibility in the FAR window.
                // Note that the visibility of the custom column(s) can change only at two possible places:
                //  1. FAR query start, i.e. below invocation to SetColumnStates and/or
                //  2. First reference result which has a non-default custom column value
                //     (UpdateCustomColumnVisibility method below).
                // Also note that the TableControl.SetColumnStates is not dependent on order of the input column states.
                TableControl.SetColumnStates(_customColumnTitleToStatesMap.Values);
            }

            /// <summary>
            /// Gets the initial column states.
            /// Note that this method itself does not actually cause any UI/column updates,
            /// but just computes and returns the new states.
            /// </summary>
            private static Dictionary<string, ColumnState2> GetInitialCustomColumnStates(
                IReadOnlyList<ColumnState> allColumnStates,
                ImmutableArray<AbstractCustomColumnDefinition> customColumns)
            {
                var customColumnStatesMap = new Dictionary<string, ColumnState2>(customColumns.Length);
                var customColumnNames = new HashSet<string>(customColumns.Select(c => c.Name));

                // Compute the default visibility for each custom column.
                // If there is an existing column state for the custom column, flip it to be non-visible
                // by default at the start of FAR query.
                // We do so because the column will have empty values for all results for a FAR query for
                // certain cases such as types, literals, no references found case, etc.
                // It is preferable to dynamically hide an empty column for such queries, and dynamically
                // show the column if it has at least one non-default value.
                foreach (ColumnState2 columnState in allColumnStates.Where(c => customColumnNames.Contains(c.Name)))
                {
                    var newColumnState = new ColumnState2(columnState.Name, isVisible: false, columnState.Width,
                        columnState.SortPriority, columnState.DescendingSort, columnState.GroupingPriority);
                    customColumnStatesMap.Add(columnState.Name, newColumnState);
                }

                // For the remaining custom columns with no existing column state, use the default column state.
                foreach (var customColumn in customColumns)
                {
                    if (!customColumnStatesMap.ContainsKey(customColumn.Name))
                    {
                        customColumnStatesMap.Add(customColumn.Name, customColumn.DefaultColumnState);
                    }
                }

                return customColumnStatesMap;
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

            protected async Task<(Guid, string projectName, SourceText)> GetGuidAndProjectNameAndSourceTextAsync(Document document)
            {
                // The FAR system needs to know the guid for the project that a def/reference is 
                // from (to support features like filtering).  Normally that would mean we could
                // only support this from a VisualStudioWorkspace.  However, we want till work 
                // in cases like Any-Code (which does not use a VSWorkspace).  So we are tolerant
                // when we have another type of workspace.  This means we will show results, but
                // certain features (like filtering) may not work in that context.
                var vsWorkspace = document.Project.Solution.Workspace as VisualStudioWorkspace;

                var projectName = document.Project.Name;
                var guid = vsWorkspace?.GetProjectGuid(document.Project.Id) ?? Guid.Empty;

                var sourceText = await document.GetTextAsync(CancellationToken).ConfigureAwait(false);
                return (guid, projectName, sourceText);
            }

            protected async Task<Entry> TryCreateDocumentSpanEntryAsync(
                RoslynDefinitionBucket definitionBucket,
                DocumentSpan documentSpan,
                HighlightSpanKind spanKind,
                ImmutableDictionary<string, ImmutableArray<string>> referenceUsageInfo,
                string containingTypeInfo,
                string containingMemberInfo)
            {
                var document = documentSpan.Document;
                var (guid, projectName, sourceText) = await GetGuidAndProjectNameAndSourceTextAsync(document).ConfigureAwait(false);
                var (excerptResult, lineText) = await ExcerptAsync(sourceText, documentSpan).ConfigureAwait(false);

                var mappedDocumentSpan = await AbstractDocumentSpanEntry.TryMapAndGetFirstAsync(documentSpan, sourceText, CancellationToken).ConfigureAwait(false);
                if (mappedDocumentSpan == null)
                {
                    // this will be removed from the result
                    return null;
                }

                return new DocumentSpanEntry(
                    this, definitionBucket, spanKind, projectName,
                    guid, mappedDocumentSpan.Value, excerptResult, lineText, GetAggregatedCustomColumnsData(referenceUsageInfo, containingTypeInfo, containingMemberInfo));
            }

            private async Task<(ExcerptResult, SourceText)> ExcerptAsync(SourceText sourceText, DocumentSpan documentSpan)
            {
                var excerptService = documentSpan.Document.Services.GetService<IDocumentExcerptService>();
                if (excerptService != null)
                {
                    var result = await excerptService.TryExcerptAsync(documentSpan.Document, documentSpan.SourceSpan, ExcerptMode.SingleLine, CancellationToken).ConfigureAwait(false);
                    if (result != null)
                    {
                        return (result.Value, AbstractDocumentSpanEntry.GetLineContainingPosition(result.Value.Content, result.Value.MappedSpan.Start));
                    }
                }

                var classificationResult = await ClassifiedSpansAndHighlightSpanFactory.ClassifyAsync(documentSpan, CancellationToken).ConfigureAwait(false);

                // need to fix the span issue tracking here - https://github.com/dotnet/roslyn/issues/31001
                var excerptResult = new ExcerptResult(
                    sourceText,
                    classificationResult.HighlightSpan,
                    classificationResult.ClassifiedSpans,
                    documentSpan.Document,
                    documentSpan.SourceSpan);

                return (excerptResult, AbstractDocumentSpanEntry.GetLineContainingPosition(sourceText, documentSpan.SourceSpan.Start));
            }

            private ImmutableDictionary<string, string> GetAggregatedCustomColumnsData(IEnumerable<KeyValuePair<string, ImmutableArray<string>>> customColumnsDataOpt, string containingType, string containingMember)
            {
                var result = GetAggregatedUsageColumnData(customColumnsDataOpt);

                if (containingType != null)
                {
                    result = result.Add(nameof(ContainingTypeInfo), containingType);
                }

                if (containingMember != null)
                {
                    return result.Add(nameof(ContainingMemberInfo), containingMember);
                }

                return result;
            }

            private ImmutableDictionary<string, string> GetAggregatedUsageColumnData(IEnumerable<KeyValuePair<string, ImmutableArray<string>>> customColumnsDataOpt)
            {
                // Aggregate dictionary values to get column display values. For example, below input:
                //
                // {
                //   { "Column1", {"Value1", "Value2"} },
                //   { "Column2", {"Value3", "Value4"} }
                // }
                //
                // will transform to:
                //
                // {
                //   { "Column1", "Value1, Value2" },
                //   { "Column2", "Value3, Value4" }
                // }

                if (customColumnsDataOpt == null || !customColumnsDataOpt.Any())
                {
                    return ImmutableDictionary<string, string>.Empty;
                }

                return customColumnsDataOpt.Where(kvp => _customColumnTitleToStatesMap.ContainsKey(kvp.Key)).ToImmutableDictionary(
                    keySelector: kvp => kvp.Key,
                    elementSelector: kvp => GetCustomColumn(kvp.Key).GetDisplayStringForColumnValues(kvp.Value));

                // Local functions.
                AbstractCustomColumnDefinition GetCustomColumn(string columnName)
                    => (AbstractCustomColumnDefinition)TableControl.ColumnDefinitionManager.GetColumnDefinition(columnName);
            }

            private TextSpan GetRegionSpanForReference(SourceText sourceText, TextSpan referenceSpan)
            {
                const int AdditionalLineCountPerSide = 3;

                var lineNumber = sourceText.Lines.GetLineFromPosition(referenceSpan.Start).LineNumber;
                var firstLineNumber = Math.Max(0, lineNumber - AdditionalLineCountPerSide);
                var lastLineNumber = Math.Min(sourceText.Lines.Count - 1, lineNumber + AdditionalLineCountPerSide);

                return TextSpan.FromBounds(
                    sourceText.Lines[firstLineNumber].Start,
                    sourceText.Lines[lastLineNumber].End);
            }

            private void UpdateUsageColumnVisibility(ImmutableDictionary<string, ImmutableArray<string>> customUsageData)
            {
                // Check if we have any custom reference data to display.
                // columnDefinitionManager will be null under unit test
                var columnDefinitionManager = TableControl.ColumnDefinitionManager;
                if (customUsageData.Count == 0 || columnDefinitionManager == null)
                {
                    return;
                }

                // Get the new column states corresponding to the custom columns to display for custom data.
                var newColumnStates = ArrayBuilder<ColumnState2>.GetInstance();

                try
                {
                    lock (Gate)
                    {
                        foreach (var customColumnName in customUsageData.Keys)
                        {
                            // Get the matching custom column.
                            var customColumnDefinition = columnDefinitionManager.GetColumnDefinition(customColumnName) as AbstractCustomColumnDefinition;
                            if (customColumnDefinition == null)
                            {
                                Debug.Fail($"{nameof(SourceReferenceItem.ReferenceUsageInfo)} has a key '{customColumnName}', but there is no exported '{nameof(AbstractCustomColumnDefinition)}' with this name.");
                                continue;
                            }

                            // Ensure that we flip the visibility to true for the custom column.
                            // Note that the actual UI update happens outside the lock when we
                            // invoke "TableControl.SetColumnStates" below.
                            ColumnState2 newColumnStateOpt = null;
                            if (_customColumnTitleToStatesMap.TryGetValue(customColumnDefinition.Name, out var columnState))
                            {
                                if (!columnState.IsVisible)
                                {
                                    newColumnStateOpt = new ColumnState2(columnState.Name, isVisible: true, columnState.Width,
                                        columnState.SortPriority, columnState.DescendingSort, columnState.GroupingPriority);
                                }
                            }
                            else
                            {
                                newColumnStateOpt = customColumnDefinition.DefaultColumnState;
                            }

                            if (newColumnStateOpt != null)
                            {
                                _customColumnTitleToStatesMap[customColumnDefinition.Name] = newColumnStateOpt;

                                newColumnStates.Add(newColumnStateOpt);
                            }
                        }
                    }

                    // Update the column states if required.
                    if (newColumnStates.Count > 0)
                    {
                        // SetColumnStates API forces a switch to UI thread, so it should be safe to call
                        // from a background thread here.
                        // Also note that we will call it only once for each new custom column to add for
                        // each find references query - the lock above guarantees that newColumnStatesOpt is
                        // going to be non-null only for the first result that has a non-empty column value.
                        TableControl.SetColumnStates(newColumnStates.ToImmutable());
                    }
                }
                finally
                {
                    newColumnStates.Free();
                }
            }

            private void UpdateCustomColumnVisibility(string key)
            {
                // Check if we have any custom reference data to display.
                // columnDefinitionManager will be null under unit test
                var columnDefinitionManager = TableControl.ColumnDefinitionManager;
                if (columnDefinitionManager == null)
                {
                    return;
                }

                // Get the new column states corresponding to the custom columns to display for custom data.
                var newColumnStates = ArrayBuilder<ColumnState2>.GetInstance();

                try
                {
                    lock (Gate)
                    {
                        // Get the matching custom column.
                        var customColumnDefinition = columnDefinitionManager.GetColumnDefinition(key) as AbstractCustomColumnDefinition;
                        if (customColumnDefinition == null)
                        {
                            Debug.Fail($"{nameof(SourceReferenceItem.ReferenceUsageInfo)} has a key '{key}', but there is no exported '{nameof(AbstractCustomColumnDefinition)}' with this name.");
                        }

                        // Ensure that we flip the visibility to true for the custom column.
                        // Note that the actual UI update happens outside the lock when we
                        // invoke "TableControl.SetColumnStates" below.
                        ColumnState2 newColumnStateOpt = null;
                        if (_customColumnTitleToStatesMap.TryGetValue(customColumnDefinition.Name, out var columnState))
                        {
                            if (!columnState.IsVisible)
                            {
                                newColumnStateOpt = new ColumnState2(columnState.Name, isVisible: true, columnState.Width,
                                    columnState.SortPriority, columnState.DescendingSort, columnState.GroupingPriority);
                            }
                        }
                        else
                        {
                            newColumnStateOpt = customColumnDefinition.DefaultColumnState;
                        }

                        if (newColumnStateOpt != null)
                        {
                            _customColumnTitleToStatesMap[customColumnDefinition.Name] = newColumnStateOpt;

                            newColumnStates.Add(newColumnStateOpt);
                        }
                    }

                    // Update the column states if required.
                    if (newColumnStates.Count > 0)
                    {
                        // SetColumnStates API forces a switch to UI thread, so it should be safe to call
                        // from a background thread here.
                        // Also note that we will call it only once for each new custom column to add for
                        // each find references query - the lock above guarantees that newColumnStatesOpt is
                        // going to be non-null only for the first result that has a non-empty column value.
                        TableControl.SetColumnStates(newColumnStates.ToImmutable());
                    }
                }
                finally
                {
                    newColumnStates.Free();
                }
            }

            public sealed override Task OnReferenceFoundAsync(SourceReferenceItem reference)
            {
                UpdateUsageColumnVisibility(reference.ReferenceUsageInfo);
                UpdateCustomColumnVisibility(nameof(ContainingTypeInfo));
                UpdateCustomColumnVisibility(nameof(ContainingMemberInfo));
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
