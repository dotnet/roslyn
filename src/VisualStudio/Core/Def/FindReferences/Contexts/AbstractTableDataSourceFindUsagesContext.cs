// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell.FindAllReferences;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages;

internal partial class StreamingFindUsagesPresenter
{
    private abstract class AbstractTableDataSourceFindUsagesContext :
        FindUsagesContext, ITableDataSource, ITableEntriesSnapshotFactory
    {
        /// <summary>
        /// Cancellation token we own that we will trigger if the presenter for this particular
        /// search is either closed, or repurposed to show results from another search.  Clients
        /// using the <see cref="IStreamingFindUsagesPresenter"/> should use this token if they 
        /// are populating the presenter in a fire-and-forget manner.  In other words if they kick
        /// off work to compute the results that they themselves are not waiting on.  If they are
        /// *not* kickign off work in a fire-and-forget manner, and are instead populating the 
        /// presenter on their own thread, they should have their own cancellation token (for example
        /// backed by a threaded-wait-dialog or CommandExecutionContext) that controls their scenario
        /// which a client can use to cancel that work.
        /// </summary>
        /// <remarks>
        /// Importantly, no code in this context or the presenter should actually examine this token
        /// to see if their work is cancelled.  Instead, any cancellable work should have a cancellation
        /// token passed in from the caller that should be used instead.
        /// </remarks>
        public readonly CancellationTokenSource CancellationTokenSource = new();

        private ITableDataSink _tableDataSink;

        public readonly StreamingFindUsagesPresenter Presenter;
        private readonly IFindAllReferencesWindow _findReferencesWindow;
        private readonly IGlobalOptionService _globalOptions;

        protected readonly IThreadingContext ThreadingContext;
        protected readonly IWpfTableControl2 TableControl;

        private readonly AsyncBatchingWorkQueue<(int current, int maximum)> _progressQueue;
        private readonly AsyncBatchingWorkQueue _notifyQueue;

        protected readonly object Gate = new();

        #region Fields that should be locked by _gate

        /// <summary>
        /// If we've been cleared or not.  If we're cleared we'll just return an empty
        /// list of results whenever queried for the current snapshot.
        /// </summary>
        private bool _cleared;

        /// <summary>
        /// Message we show if we find no definitions.  Consumers of the streaming presenter can set their own title.
        /// </summary>
        protected string NoDefinitionsFoundMessage = ServicesVSResources.Search_found_no_results;

        /// <summary>
        /// The list of all definitions we've heard about.  This may be a superset of the
        /// keys in <see cref="_definitionToBucket"/> because we may encounter definitions
        /// we don't create definition buckets for.  For example, if the definition asks
        /// us to not display it if it has no references, and we don't run into any 
        /// references for it (common with implicitly declared symbols).
        /// </summary>
        protected readonly List<DefinitionItem> Definitions = [];

        /// <summary>
        /// We will hear about the same definition over and over again.  i.e. for each reference 
        /// to a definition, we will be told about the same definition.  However, we only want to
        /// create a single actual <see cref="DefinitionBucket"/> for the definition. To accomplish
        /// this we keep a map from the definition to the task that we're using to create the 
        /// bucket for it.  The first time we hear about a definition we'll make a single task
        /// and then always return that for all future references found.
        /// </summary>
        private readonly Dictionary<DefinitionItem, RoslynDefinitionBucket> _definitionToBucket = [];

        /// <summary>
        /// We want to hide declarations of a symbol if the user is grouping by definition.
        /// With such grouping on, having both the definition group and the declaration item
        /// is just redundant.  To make life easier we keep around two groups of entries.
        /// One group for when we are grouping by definition, and one when we're not.
        /// </summary>
        private bool _currentlyGroupingByDefinition;

        protected readonly (List<Entry> primary, List<Entry> nonPrimary) EntriesWhenNotGroupingByDefinition = ([], []);
        protected readonly (List<Entry> primary, List<Entry> nonPrimary) EntriesWhenGroupingByDefinition = ([], []);

        private TableEntriesSnapshot? _lastSnapshot;
        public int CurrentVersionNumber { get; protected set; }

        #endregion

        protected AbstractTableDataSourceFindUsagesContext(
             StreamingFindUsagesPresenter presenter,
             IFindAllReferencesWindow findReferencesWindow,
             ImmutableArray<ITableColumnDefinition> customColumns,
             IGlobalOptionService globalOptions,
             bool includeContainingTypeAndMemberColumns,
             bool includeKindColumn,
             IThreadingContext threadingContext)
        {
            presenter.ThreadingContext.ThrowIfNotOnUIThread();

            Presenter = presenter;
            _findReferencesWindow = findReferencesWindow;
            _globalOptions = globalOptions;
            ThreadingContext = threadingContext;
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
            RoslynDebug.Assert(_tableDataSink != null);

            // https://devdiv.visualstudio.com/web/wi.aspx?pcguid=011b8bdf-6d56-4f87-be0d-0092136884d9&id=359162
            // VS actually responds to each SetProgess call by queuing a UI task to do the
            // progress bar update.  This can made FindReferences feel extremely slow when
            // thousands of SetProgress calls are made.
            //
            // To ensure a reasonable experience, we instead add the progress into a queue and
            // only update the UI a few times a second so as to not overload it.
            _progressQueue = new AsyncBatchingWorkQueue<(int current, int maximum)>(
                DelayTimeSpan.Short,
                this.UpdateTableProgressAsync,
                presenter._asyncListener,
                CancellationTokenSource.Token);

            // Similarly, updating the actual FAR window can be quite expensive (especially when there are thousands of
            // results).  To limit the amount of work we do, we'll only update the window every 250ms.
            _notifyQueue = new AsyncBatchingWorkQueue(
                DelayTimeSpan.Short,
                cancellationToken =>
                {
                    _tableDataSink.FactorySnapshotChanged(this);
                    return ValueTaskFactory.CompletedTask;
                },
                presenter._asyncListener,
                CancellationTokenSource.Token);
        }

        protected abstract Task OnCompletedAsyncWorkerAsync(CancellationToken cancellationToken);
        protected abstract ValueTask OnDefinitionFoundWorkerAsync(DefinitionItem definition, CancellationToken cancellationToken);
        protected abstract ValueTask OnReferenceFoundWorkerAsync(SourceReferenceItem reference, CancellationToken cancellationToken);

        protected static void Add<T>((List<T> primary, List<T> nonPrimary) entries, T item, bool isPrimary)
        {
            var list = isPrimary ? entries.primary : entries.nonPrimary;
            list.Add(item);
        }

        protected static void AddRange<T>((List<T> primary, List<T> nonPrimary) entries, ArrayBuilder<T> builder, bool isPrimary)
        {
            var list = isPrimary ? entries.primary : entries.nonPrimary;
            list.Capacity = list.Count + builder.Count;

            foreach (var item in builder)
                list.Add(item);
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

            customColumnsToInclude.Add(StandardTableKeyNames.Repository);
            customColumnsToInclude.Add(StandardTableKeyNames.ItemOrigin);

            return customColumnsToInclude.ToImmutableAndFree();
        }

        protected void NotifyChange()
            => _notifyQueue.AddWork();

        private void OnFindReferencesWindowClosed(object sender, EventArgs e)
        {
            this.Presenter.ThreadingContext.ThrowIfNotOnUIThread();
            CancelSearch();

            _findReferencesWindow.Closed -= OnFindReferencesWindowClosed;
            TableControl.GroupingsChanged -= OnTableControlGroupingsChanged;
        }

        private void OnTableControlGroupingsChanged(object sender, EventArgs e)
        {
            this.Presenter.ThreadingContext.ThrowIfNotOnUIThread();
            UpdateGroupingByDefinition();
        }

        private void UpdateGroupingByDefinition()
        {
            this.Presenter.ThreadingContext.ThrowIfNotOnUIThread();
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
            this.Presenter.ThreadingContext.ThrowIfNotOnUIThread();

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
            this.Presenter.ThreadingContext.ThrowIfNotOnUIThread();

            // Cancel any in flight find work that is going on. Note: disposal happens in our own
            // implementation of IDisposable.Dispose.
            CancellationTokenSource.Cancel();
        }

        public void Clear()
        {
            this.Presenter.ThreadingContext.ThrowIfNotOnUIThread();

            // Stop all existing work.
            this.CancelSearch();

            // Clear the title of the window.  It will go back to the default editor title.
            _findReferencesWindow.Title = null;

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
            this.Presenter.ThreadingContext.ThrowIfNotOnUIThread();

            Debug.Assert(_tableDataSink == null);
            _tableDataSink = sink;

            _tableDataSink.AddFactory(this, removeAllFactories: true);
            _tableDataSink.IsStable = false;

            return this;
        }

        #endregion

        #region FindUsagesContext overrides.

        public sealed override ValueTask SetSearchTitleAsync(string title, CancellationToken cancellationToken)
        {
            // Note: IFindAllReferenceWindow.Title is safe to set from any thread.
            _findReferencesWindow.Title = title;
            return default;
        }

        public sealed override async ValueTask OnCompletedAsync(CancellationToken cancellationToken)
        {
            await OnCompletedAsyncWorkerAsync(cancellationToken).ConfigureAwait(false);
            _tableDataSink.IsStable = true;
        }

        public sealed override ValueTask OnDefinitionFoundAsync(DefinitionItem definition, CancellationToken cancellationToken)
        {
            try
            {
                lock (Gate)
                {
                    Definitions.Add(definition);
                }

                return OnDefinitionFoundWorkerAsync(definition, cancellationToken);
            }
            catch (Exception ex) when (FatalError.ReportAndPropagateUnlessCanceled(ex, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        protected async Task AddDocumentSpanEntriesAsync(
            ArrayBuilder<Entry> entries,
            RoslynDefinitionBucket definitionBucket,
            DefinitionItem definition,
            CancellationToken cancellationToken)
        {
            for (int i = 0, n = definition.SourceSpans.Length; i < n; i++)
            {
                var sourceSpan = definition.SourceSpans[i];
                var classifiedSpan = definition.ClassifiedSpans.IsEmpty ? null : definition.ClassifiedSpans[i];

                var entry = await TryCreateDocumentSpanEntryAsync(
                    definitionBucket,
                    sourceSpan,
                    classifiedSpan,
                    HighlightSpanKind.Definition,
                    SymbolUsageInfo.None,
                    definition.DisplayableProperties,
                    cancellationToken).ConfigureAwait(false);

                entries.AddIfNotNull(entry);
            }
        }

        protected async Task<Entry?> TryCreateDefinitionEntryAsync(
            RoslynDefinitionBucket definitionBucket, DefinitionItem definition, CancellationToken cancellationToken)
        {
            var documentSpan = definition.SourceSpans[0];
            var sourceText = await documentSpan.Document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var lineText = AbstractDocumentSpanEntry.GetLineContainingPosition(sourceText, documentSpan.SourceSpan.Start);

            var mappedDocumentSpan = await AbstractDocumentSpanEntry.TryMapAndGetFirstAsync(documentSpan, sourceText, cancellationToken).ConfigureAwait(false);
            if (mappedDocumentSpan == null)
            {
                // this will be removed from the result
                return null;
            }

            var (guid, projectName) = GetGuidAndProjectName(documentSpan.Document);

            return new DefinitionItemEntry(
                this,
                definitionBucket,
                guid,
                projectName,
                lineText,
                mappedDocumentSpan.Value,
                documentSpan,
                ThreadingContext);
        }

        protected async Task<Entry?> TryCreateDocumentSpanEntryAsync(
            RoslynDefinitionBucket definitionBucket,
            DocumentSpan documentSpan,
            ClassifiedSpansAndHighlightSpan? classifiedSpans,
            HighlightSpanKind spanKind,
            SymbolUsageInfo symbolUsageInfo,
            ImmutableArray<(string key, string value)> additionalProperties,
            CancellationToken cancellationToken)
        {
            var document = documentSpan.Document;
            var sourceText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var (excerptResult, lineText) = await ExcerptAsync(sourceText, documentSpan, classifiedSpans, cancellationToken).ConfigureAwait(false);

            var mappedDocumentSpan = await AbstractDocumentSpanEntry.TryMapAndGetFirstAsync(documentSpan, sourceText, cancellationToken).ConfigureAwait(false);
            if (mappedDocumentSpan == null)
            {
                // this will be removed from the result
                return null;
            }

            var (guid, projectName) = GetGuidAndProjectName(document);

            return DocumentSpanEntry.TryCreate(
                this,
                definitionBucket,
                guid,
                projectName,
                document.FilePath,
                documentSpan.SourceSpan,
                spanKind,
                mappedDocumentSpan.Value,
                excerptResult,
                lineText,
                symbolUsageInfo,
                additionalProperties,
                ThreadingContext);
        }

        private async Task<(ExcerptResult, SourceText)> ExcerptAsync(
            SourceText sourceText, DocumentSpan documentSpan, ClassifiedSpansAndHighlightSpan? classifiedSpans, CancellationToken cancellationToken)
        {
            var document = documentSpan.Document;
            var sourceSpan = documentSpan.SourceSpan;

            var excerptService = document.DocumentServiceProvider.GetService<IDocumentExcerptService>();

            // Fetching options is expensive enough to try to avoid it if we can.  So only fetch this if absolutely necessary.
            ClassificationOptions? options = null;
            if (excerptService != null)
            {
                options ??= _globalOptions.GetClassificationOptions(document.Project.Language);

                var result = await excerptService.TryExcerptAsync(document, sourceSpan, ExcerptMode.SingleLine, options.Value, cancellationToken).ConfigureAwait(false);
                if (result != null)
                    return (result.Value, AbstractDocumentSpanEntry.GetLineContainingPosition(result.Value.Content, result.Value.MappedSpan.Start));
            }

            if (classifiedSpans is null)
            {
                options ??= _globalOptions.GetClassificationOptions(document.Project.Language);

                classifiedSpans = await ClassifiedSpansAndHighlightSpanFactory.ClassifyAsync(
                    documentSpan, classifiedSpans, options.Value, cancellationToken).ConfigureAwait(false);
            }

            // need to fix the span issue tracking here - https://github.com/dotnet/roslyn/issues/31001
            var excerptResult = new ExcerptResult(
                sourceText,
                classifiedSpans.Value.HighlightSpan,
                classifiedSpans.Value.ClassifiedSpans,
                document,
                sourceSpan);

            return (excerptResult, AbstractDocumentSpanEntry.GetLineContainingPosition(sourceText, sourceSpan.Start));
        }

        public sealed override async ValueTask OnReferencesFoundAsync(IAsyncEnumerable<SourceReferenceItem> references, CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var reference in references)
                    await OnReferenceFoundWorkerAsync(reference, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (FatalError.ReportAndPropagateUnlessCanceled(ex, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        protected RoslynDefinitionBucket GetOrCreateDefinitionBucket(DefinitionItem definition, bool expandedByDefault)
        {
            lock (Gate)
            {
                if (!_definitionToBucket.TryGetValue(definition, out var bucket))
                {
                    bucket = RoslynDefinitionBucket.Create(Presenter, this, definition, expandedByDefault, ThreadingContext);
                    _definitionToBucket.Add(definition, bucket);
                }

                return bucket;
            }
        }

        public sealed override ValueTask ReportNoResultsAsync(string message, CancellationToken cancellationToken)
        {
            lock (Gate)
            {
                NoDefinitionsFoundMessage = message;
            }

            return ValueTaskFactory.CompletedTask;
        }

        public sealed override async ValueTask ReportMessageAsync(string message, NotificationSeverity severity, CancellationToken cancellationToken)
        {
            await this.Presenter.ReportMessageAsync(message, severity, cancellationToken).ConfigureAwait(false);
        }

        protected sealed override ValueTask ReportProgressAsync(int current, int maximum, CancellationToken cancellationToken)
        {
            _progressQueue.AddWork((current, maximum));
            return default;
        }

        private ValueTask UpdateTableProgressAsync(ImmutableSegmentedList<(int current, int maximum)> nextBatch, CancellationToken _)
        {
            if (!nextBatch.IsEmpty)
            {
                var (current, maximum) = nextBatch.Last();

                // Do not update the UI if the current progress is zero.  It will switch us from the indeterminate
                // progress bar (which conveys to the user that we're working) to showing effectively nothing (which
                // makes it appear as if the search is complete).  So the user sees:
                //
                //      indeterminate->complete->progress
                //
                // instead of:
                //
                //      indeterminate->progress

                if (current > 0)
                    _findReferencesWindow.SetProgress(current, maximum);
            }

            return ValueTaskFactory.CompletedTask;
        }

        protected static DefinitionItem CreateNoResultsDefinitionItem(string message)
            => DefinitionItem.CreateNonNavigableItem(
                GlyphTags.GetTags(Glyph.StatusInformation),
                [new TaggedText(TextTags.Text, message)]);

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
                        ? []
                        : _currentlyGroupingByDefinition
                            ? ToImmutableArray(EntriesWhenGroupingByDefinition)
                            : ToImmutableArray(EntriesWhenNotGroupingByDefinition);

                    _lastSnapshot = new TableEntriesSnapshot(entries, CurrentVersionNumber);
                }

                return _lastSnapshot;
            }
        }

        private static ImmutableArray<Entry> ToImmutableArray((List<Entry> primary, List<Entry> nonPrimary) entries)
        {
            var result = new FixedSizeArrayBuilder<Entry>(entries.nonPrimary.Count + entries.primary.Count);
            foreach (var entry in entries.primary)
                result.Add(entry);

            foreach (var entry in entries.nonPrimary)
                result.Add(entry);

            return result.MoveToImmutable();
        }

        public ITableEntriesSnapshot? GetSnapshot(int versionNumber)
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
            this.Presenter.ThreadingContext.ThrowIfNotOnUIThread();

            // VS is letting go of us.  i.e. because a new FAR call is happening, or because
            // of some other event (like the solution being closed).  Remove us from the set
            // of sources for the window so that the existing data is cleared out.
            Debug.Assert(_findReferencesWindow.Manager.Sources.Count == 1);
            Debug.Assert(_findReferencesWindow.Manager.Sources[0] == this);

            _findReferencesWindow.Manager.RemoveSource(this);

            // Remove ourselves from the list of contexts that are currently active.
            Presenter._currentContexts.Remove(this);

            CancelSearch();
            CancellationTokenSource.Dispose();
        }

        #endregion
    }
}
