// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.FindAllReferences;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    internal partial class StreamingFindUsagesPresenter
    {
        private class TableDataSourceFindUsagesContext :
            FindUsagesContext, ITableDataSource, ITableEntriesSnapshotFactory
        {
            private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

            private ITableDataSink _tableDataSink;

            public readonly StreamingFindUsagesPresenter Presenter;
            private readonly IFindAllReferencesWindow _findReferencesWindow;
            private readonly IWpfTableControl2 _tableControl;
            private readonly bool _alwaysIncludeDeclarations;

            private readonly object _gate = new object();

            #region Fields that should be locked by _gate

            /// <summary>
            /// The list of all definitions we've heard about.  This may be a superset of the
            /// keys in <see cref="_definitionToBucket"/> becaue we may encounter definitions
            /// we don't create definition buckets for.  For example, if the definition asks
            /// us to not display it if it has no references, and we don't run into any 
            /// references for it (common with implicitly declared symbols).
            /// </summary>
            private readonly List<DefinitionItem> _definitions = new List<DefinitionItem>();

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

            private ImmutableList<Entry> _entriesWithDeclarations = ImmutableList<Entry>.Empty;
            private ImmutableList<Entry> _entriesWithoutDeclarations = ImmutableList<Entry>.Empty;

            private TableEntriesSnapshot _lastSnapshot;
            public int CurrentVersionNumber { get; private set; }

            #endregion

            public TableDataSourceFindUsagesContext(
                 StreamingFindUsagesPresenter presenter,
                 IFindAllReferencesWindow findReferencesWindow,
                 bool alwaysIncludeDeclarations)
            {
                presenter.AssertIsForeground();

                Presenter = presenter;
                _alwaysIncludeDeclarations = alwaysIncludeDeclarations;
                _findReferencesWindow = findReferencesWindow;
                _tableControl = (IWpfTableControl2)findReferencesWindow.TableControl;
                _tableControl.GroupingsChanged += OnTableControlGroupingsChanged;

                // If the window is closed, cancel any work we're doing.
                _findReferencesWindow.Closed += OnFindReferencesWindowClosed;

                DetermineCurrentGroupingByDefinitionState();

                // Remove any existing sources in the window.  
                foreach (var source in findReferencesWindow.Manager.Sources.ToArray())
                {
                    findReferencesWindow.Manager.RemoveSource(source);
                }

                // And add ourselves as the source of results for the window.
                findReferencesWindow.Manager.AddSource(this);

                // After adding us as the source, the manager should immediately call into us to
                // tell us what the data sink is.
                Debug.Assert(_tableDataSink != null);
            }

            private void OnFindReferencesWindowClosed(object sender, EventArgs e)
            {
                Presenter.AssertIsForeground();
                CancelSearch();

                _findReferencesWindow.Closed -= OnFindReferencesWindowClosed;
                _tableControl.GroupingsChanged -= OnTableControlGroupingsChanged;
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
                    lock (_gate)
                    {
                        CurrentVersionNumber++;
                    }

                    // Let all our subscriptions know that we've updated.  That way they'll refresh
                    // and we'll show/hide declarations as appropriate.
                    _tableDataSink.FactorySnapshotChanged(this);
                }
            }

            private bool DetermineCurrentGroupingByDefinitionState()
            {
                Presenter.AssertIsForeground();

                var definitionColumn = _tableControl.ColumnStates.FirstOrDefault(
                    s => s.Name == StandardTableColumnDefinitions2.Definition) as ColumnState2;

                lock (_gate)
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

            internal void OnSubscriptionDisposed()
            {
                CancelSearch();
            }

            public override CancellationToken CancellationToken => _cancellationTokenSource.Token;

            #region ITableDataSource

            public string DisplayName => "Roslyn Data Source";

            public string Identifier => RoslynFindUsagesTableDataSourceIdentifier;

            public string SourceTypeIdentifier => RoslynFindUsagesTableDataSourceSourceTypeIdentifier;

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

            public override void SetSearchTitle(string title)
            {
                try
                {
                    // Editor renamed their property from Label to Title, and made it public.
                    // However, we don't have access to that property yet until they publish
                    // their next SDK.  In the meantime, use reflection to get at the right
                    // property.
                    var titleProperty = _findReferencesWindow.GetType().GetProperty(
                        "Title", BindingFlags.Public | BindingFlags.Instance);
                    if (titleProperty != null)
                    {
                        titleProperty.SetValue(_findReferencesWindow, title);
                    }
                }
                catch (Exception)
                {
                }
            }

            public override async Task OnCompletedAsync()
            {
                // Now that we know the search is over, create and display any error messages
                // for definitions that were not found.
                await CreateMissingReferenceEntriesIfNecessaryAsync().ConfigureAwait(false);
                await CreateNoResultsFoundEntryIfNecessaryAsync().ConfigureAwait(false);

                _tableDataSink.IsStable = true;
            }

            private async Task CreateNoResultsFoundEntryIfNecessaryAsync()
            {
                bool noDefinitions;
                lock(_gate)
                {
                    noDefinitions = this._definitions.Count == 0;
                }

                if (noDefinitions)
                {
                    // Create a fake definition/reference called "search found no results"
                    await OnEntryFoundAsync(NoResultsDefinitionItem,
                        bucket => SimpleMessageEntry.CreateAsync(
                            bucket, ServicesVisualStudioNextResources.Search_found_no_results),
                        addToEntriesWithDeclarations: true,
                        addToEntriesWithoutDeclarations: true).ConfigureAwait(false);
                }
            }

            private static readonly DefinitionItem NoResultsDefinitionItem =
                DefinitionItem.CreateNonNavigableItem(
                    GlyphTags.GetTags(Glyph.StatusInformation),
                    ImmutableArray.Create(new TaggedText(
                        TextTags.Text,
                        ServicesVisualStudioNextResources.Search_found_no_results)));

            private async Task CreateMissingReferenceEntriesIfNecessaryAsync()
            {
                await CreateMissingReferenceEntriesIfNecessaryAsync(withDeclarations: true).ConfigureAwait(false);
                await CreateMissingReferenceEntriesIfNecessaryAsync(withDeclarations: false).ConfigureAwait(false);
            }

            private async Task CreateMissingReferenceEntriesIfNecessaryAsync(
                bool withDeclarations)
            {
                // Go through and add dummy entries for any definitions that 
                // that we didn't find any references for.

                var definitions = GetDefinitionsToCreateMissingReferenceItemsFor(withDeclarations);
                foreach (var definition in definitions)
                {
                    // Create a fake reference to this definition that says 
                    // "no references found to <symbolname>".
                    await OnEntryFoundAsync(definition,
                        bucket => SimpleMessageEntry.CreateAsync(
                            bucket, GetMessage(bucket.DefinitionItem)),
                        addToEntriesWithDeclarations: withDeclarations,
                        addToEntriesWithoutDeclarations: !withDeclarations).ConfigureAwait(false);
                }
            }

            private static string GetMessage(DefinitionItem definition)
            {
                if (definition.IsExternal)
                {
                    return ServicesVisualStudioNextResources.External_reference_found;
                }

                return string.Format(
                    ServicesVisualStudioNextResources.No_references_found_to_0,
                    definition.NameDisplayParts.JoinText());
            }

            private ImmutableArray<DefinitionItem> GetDefinitionsToCreateMissingReferenceItemsFor(
                bool withDeclarations)
            {
                lock (_gate)
                {
                    var entries = withDeclarations
                        ? _entriesWithDeclarations
                        : _entriesWithoutDeclarations;

                    // Find any definitions that we didn't have any references to. But only show 
                    // them if they want to be displayed without any references.  This will 
                    // ensure that we still see things like overrides and whatnot, but we
                    // won't show property-accessors.
                    var seenDefinitions = entries.Select(r => r.DefinitionBucket.DefinitionItem).ToSet();
                    var q = from definition in _definitions
                            where !seenDefinitions.Contains(definition) &&
                                  definition.DisplayIfNoReferences
                            select definition;

                    // If we find at least one of these types of definitions, then just return those.
                    var result = ImmutableArray.CreateRange(q);
                    if (result.Length > 0)
                    {
                        return result;
                    }

                    // We found no definitions that *want* to be displayed.  However, we still 
                    // want to show something.  So, if necessary, show at lest the first definition
                    // even if we found no references and even if it would prefer to not be seen.
                    if (entries.Count == 0 && _definitions.Count > 0)
                    {
                        return ImmutableArray.Create(_definitions.First());
                    }

                    return ImmutableArray<DefinitionItem>.Empty;
                }
            }

            public override async Task OnDefinitionFoundAsync(DefinitionItem definition)
            {
                lock (_gate)
                {
                    _definitions.Add(definition);
                }

                // If this is a definition we always want to show, then create entries
                // for all the declaration locations immediately.  Otherwise, we'll 
                // create them on demand when we hear about references for this definition.
                if (definition.DisplayIfNoReferences)
                {
                    await AddDeclarationEntriesAsync(definition).ConfigureAwait(false);
                }
            }

            private bool HasDeclarationEntries(DefinitionItem definition)
            {
                lock (_gate)
                {
                    return _entriesWithDeclarations.Any(e => e.DefinitionBucket.DefinitionItem == definition);
                }
            }

            private async Task AddDeclarationEntriesAsync(DefinitionItem definition)
            {
                CancellationToken.ThrowIfCancellationRequested();

                // Don't do anything if we already have declaration entries for this definition 
                // (i.e. another thread beat us to this).
                if (HasDeclarationEntries(definition))
                {
                    return;
                }

                var definitionBucket = GetOrCreateDefinitionBucket(definition);

                // We could do this inside the lock.  but that would mean async activity in a 
                // lock, and i'd like to avoid that.  That does mean that we might do extra
                // work if multiple threads end up down htis path.  But only one of them will
                // win when we access the lock below.
                var declarations = ArrayBuilder<Entry>.GetInstance();
                foreach (var declarationLocation in definition.SourceSpans)
                {
                    var definitionEntry = await CreateDocumentLocationEntryAsync(
                        definitionBucket, declarationLocation, isDefinitionLocation: true).ConfigureAwait(false);
                    if (definitionEntry != null)
                    {
                        declarations.Add(definitionEntry);
                    }
                }

                lock (_gate)
                {
                    // Do one final check to ensure that no other thread beat us here.
                    if (!HasDeclarationEntries(definition))
                    {
                        // We only include declaration entries in the entries we show when 
                        // not grouping by definition.
                        _entriesWithDeclarations = _entriesWithDeclarations.AddRange(declarations);
                        CurrentVersionNumber++;
                    }
                }

                declarations.Free();

                // Let all our subscriptions know that we've updated.
                _tableDataSink.FactorySnapshotChanged(this);
            }

            public override Task OnReferenceFoundAsync(SourceReferenceItem reference)
            {
                // Normal references go into both sets of entries.
                return OnEntryFoundAsync(
                    reference.Definition,
                    bucket => CreateDocumentLocationEntryAsync(
                        bucket, reference.SourceSpan, isDefinitionLocation: false),
                    addToEntriesWithDeclarations: true,
                    addToEntriesWithoutDeclarations: true);
            }

            private async Task OnEntryFoundAsync(
                DefinitionItem definition,
                Func<RoslynDefinitionBucket, Task<Entry>> createEntryAsync,
                bool addToEntriesWithDeclarations,
                bool addToEntriesWithoutDeclarations)
            {
                Debug.Assert(addToEntriesWithDeclarations || addToEntriesWithoutDeclarations);
                CancellationToken.ThrowIfCancellationRequested();

                // First find the bucket corresponding to our definition.
                var definitionBucket = GetOrCreateDefinitionBucket(definition);

                var entry = await createEntryAsync(definitionBucket).ConfigureAwait(false);

                // Ok, we got a *reference* to some definition item.  This may have been
                // a reference for some definition that we haven't created any declaration
                // entries for (i.e. becuase it had DisplayIfNoReferences = false).  Because
                // we've now found a reference, we want to make sure all its declaration
                // entries are added.
                await AddDeclarationEntriesAsync(definition).ConfigureAwait(false);

                lock (_gate)
                {
                    // Once we can make the new entry, add it to the appropriate list.
                    if (addToEntriesWithDeclarations)
                    {
                        _entriesWithDeclarations = _entriesWithDeclarations.Add(entry);
                    }

                    if (addToEntriesWithoutDeclarations)
                    {
                        _entriesWithoutDeclarations = _entriesWithoutDeclarations.Add(entry);
                    }

                    CurrentVersionNumber++;
                }

                // Let all our subscriptions know that we've updated.
                _tableDataSink.FactorySnapshotChanged(this);
            }

            private async Task<Entry> CreateDocumentLocationEntryAsync(
                RoslynDefinitionBucket definitionBucket,
                DocumentSpan documentSpan,
                bool isDefinitionLocation)
            {
                var document = documentSpan.Document;

                // The FAR system needs to know the guid for the project that a def/reference is 
                // from (to support features like filtering).  Normally that would mean we could
                // only support this from a VisualStudioWorkspace.  However, we want till work 
                // in cases lke Any-Code (which does not use a VSWorkspace).  So we are tolerant
                // when we have another type of workspace.  This means we will show results, but
                // certain features (like filtering) may not work in that context.
                var workspace = document.Project.Solution.Workspace as VisualStudioWorkspaceImpl;
                var guid = workspace?.GetHostProject(document.Project.Id)?.Guid ?? Guid.Empty;

                var sourceText = await document.GetTextAsync(CancellationToken).ConfigureAwait(false);

                var referenceSpan = documentSpan.SourceSpan;
                var lineSpan = GetLineSpanForReference(sourceText, referenceSpan);

                var taggedLineParts = await GetTaggedTextForReferenceAsync(document, referenceSpan, lineSpan).ConfigureAwait(false);

                return new DocumentSpanEntry(
                    this, definitionBucket, documentSpan, isDefinitionLocation,
                    guid, sourceText, taggedLineParts);
            }

            private TextSpan GetLineSpanForReference(SourceText sourceText, TextSpan referenceSpan)
            {
                var sourceLine = sourceText.Lines.GetLineFromPosition(referenceSpan.Start);
                var firstNonWhitespacePosition = sourceLine.GetFirstNonWhitespacePosition().Value;

                return TextSpan.FromBounds(firstNonWhitespacePosition, sourceLine.End);
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

            private async Task<ClassifiedSpansAndHighlightSpan> GetTaggedTextForReferenceAsync(
                Document document, TextSpan referenceSpan, TextSpan widenedSpan)
            {
                var classificationService = document.GetLanguageService<IEditorClassificationService>();
                if (classificationService == null)
                {
                    return new ClassifiedSpansAndHighlightSpan(ImmutableArray<ClassifiedSpan>.Empty, new TextSpan());
                }

                // Call out to the individual language to classify the chunk of text around the
                // reference. We'll get both the syntactic and semantic spans for this region.
                // Because the semantic tags may override the semantic ones (for example, 
                // "DateTime" might be syntactically an identifier, but semantically a struct
                // name), we'll do a later merging step to get the final correct list of 
                // classifications.  For tagging, normally the editor handles this.  But as
                // we're producing the list of Inlines ourselves, we have to handles this here.
                var syntaxSpans = ListPool<ClassifiedSpan>.Allocate();
                var semanticSpans = ListPool<ClassifiedSpan>.Allocate();
                try
                {
                    var sourceText = await document.GetTextAsync(CancellationToken).ConfigureAwait(false);

                    await classificationService.AddSyntacticClassificationsAsync(
                        document, widenedSpan, syntaxSpans, CancellationToken).ConfigureAwait(false);
                    await classificationService.AddSemanticClassificationsAsync(
                        document, widenedSpan, semanticSpans, CancellationToken).ConfigureAwait(false);

                    var classifiedSpans = MergeClassifiedSpans(
                        syntaxSpans, semanticSpans, widenedSpan, sourceText);

                    var highlightSpan = new TextSpan(
                        start: referenceSpan.Start - widenedSpan.Start,
                        length: referenceSpan.Length);

                    return new ClassifiedSpansAndHighlightSpan(classifiedSpans, highlightSpan);
                }
                finally
                {
                    ListPool<ClassifiedSpan>.Free(syntaxSpans);
                    ListPool<ClassifiedSpan>.Free(semanticSpans);
                }
            }

            private ImmutableArray<ClassifiedSpan> MergeClassifiedSpans(
                List<ClassifiedSpan> syntaxSpans, List<ClassifiedSpan> semanticSpans,
                TextSpan widenedSpan, SourceText sourceText)
            {
                // The spans produced by the language services may not be ordered
                // (indeed, this happens with semantic classification as different
                // providers produce different results in an arbitrary order).  Order
                // them first before proceeding.
                Order(syntaxSpans);
                Order(semanticSpans);

                // It's possible for us to get classified spans that occur *before*
                // or after the span we want to present. This happens because the calls to
                // AddSyntacticClassificationsAsync and AddSemanticClassificationsAsync 
                // may return more spans than the range asked for.  While bad form,
                // it's never been a requirement that implementation not do that.
                // For example, the span may be the non-full-span of a node, but the
                // classifiers may still return classifications for leading/trailing
                // trivia even if it's out of the bounds of that span.
                // 
                // To deal with that, we adjust all spans so that they don't go outside
                // of the range we care about.
                AdjustSpans(syntaxSpans, widenedSpan);
                AdjustSpans(semanticSpans, widenedSpan);

                // The classification service will only produce classifications for
                // things it knows about.  i.e. there will be gaps in what it produces.
                // Fill in those gaps so we have *all* parts of the span 
                // classified properly.
                var filledInSyntaxSpans = ArrayBuilder<ClassifiedSpan>.GetInstance();
                var filledInSemanticSpans = ArrayBuilder<ClassifiedSpan>.GetInstance();

                try
                {
                    FillInClassifiedSpanGaps(sourceText, widenedSpan.Start, syntaxSpans, filledInSyntaxSpans);
                    FillInClassifiedSpanGaps(sourceText, widenedSpan.Start, semanticSpans, filledInSemanticSpans);

                    // Now merge the lists together, taking all the results from syntaxParts
                    // unless they were overridden by results in semanticParts.
                    return MergeParts(filledInSyntaxSpans, filledInSemanticSpans);
                }
                finally
                {
                    filledInSyntaxSpans.Free();
                    filledInSemanticSpans.Free();
                }
            }

            private void AdjustSpans(List<ClassifiedSpan> spans, TextSpan widenedSpan)
            {
                for (var i = 0; i < spans.Count; i++)
                {
                    var span = spans[i];

                    // Make sure the span actually intersects 'widenedSpan'.  If it 
                    // does not, just put in an empty length span.  It will get ignored later
                    // when we walk through this list.
                    var intersection = span.TextSpan.Intersection(widenedSpan);

                    var newSpan = new ClassifiedSpan(span.ClassificationType,
                        intersection ?? new TextSpan());
                    spans[i] = newSpan;
                }
            }

            private static void FillInClassifiedSpanGaps(
                SourceText sourceText, int startPosition,
                List<ClassifiedSpan> classifiedSpans, ArrayBuilder<ClassifiedSpan> result)
            {
                foreach (var span in classifiedSpans)
                {
                    // Ignore empty spans.  We can get those when the classification service
                    // returns spans outside of the range of the span we asked to classify.
                    if (span.TextSpan.Length == 0)
                    {
                        continue;
                    }

                    // If there is space between this span and the last one, then add a space.
                    if (startPosition != span.TextSpan.Start)
                    {
                        result.Add(new ClassifiedSpan(ClassificationTypeNames.Text,
                            TextSpan.FromBounds(
                                startPosition, span.TextSpan.Start)));
                    }

                    result.Add(span);
                    startPosition = span.TextSpan.End;
                }
            }

            private void Order(List<ClassifiedSpan> syntaxSpans)
            {
                syntaxSpans.Sort((s1, s2) => s1.TextSpan.Start - s2.TextSpan.Start);
            }

            private ImmutableArray<ClassifiedSpan> MergeParts(
                ArrayBuilder<ClassifiedSpan> syntaxParts,
                ArrayBuilder<ClassifiedSpan> semanticParts)
            {
                // Take all the syntax parts.  However, if any have been overridden by a 
                // semantic part, then choose that one.

                var finalParts = ArrayBuilder<ClassifiedSpan>.GetInstance();
                var lastReplacementIndex = 0;
                for (int i = 0, n = syntaxParts.Count; i < n; i++)
                {
                    var syntaxPartAndSpan = syntaxParts[i];

                    // See if we can find a semantic part to replace this syntax part.
                    var replacementIndex = semanticParts.FindIndex(
                        lastReplacementIndex, t => t.TextSpan == syntaxPartAndSpan.TextSpan);

                    // Take the semantic part if it's just 'text'.  We want to keep it if
                    // the semantic classifier actually produced an interesting result 
                    // (as opposed to it just being a 'gap' classification).
                    var part = replacementIndex >= 0 && !IsClassifiedAsText(semanticParts[replacementIndex])
                        ? semanticParts[replacementIndex]
                        : syntaxPartAndSpan;
                    finalParts.Add(part);

                    if (replacementIndex >= 0)
                    {
                        // If we found a semantic replacement, update the lastIndex.
                        // That way we can start searching from that point instead 
                        // of checking all the elements each time.
                        lastReplacementIndex = replacementIndex + 1;
                    }
                }

                return finalParts.ToImmutableAndFree();
            }

            private bool IsClassifiedAsText(ClassifiedSpan partAndSpan)
            {
                // Don't take 'text' from the semantic parts.  We'll get those for the 
                // spaces between the actual interesting semantic spans, and we don't 
                // want them to override actual good syntax spans.
                return partAndSpan.ClassificationType == ClassificationTypeNames.Text;
            }

            private RoslynDefinitionBucket GetOrCreateDefinitionBucket(DefinitionItem definition)
            {
                lock (_gate)
                {
                    if (!_definitionToBucket.TryGetValue(definition, out var bucket))
                    {
                        bucket = new RoslynDefinitionBucket(Presenter, this, definition);
                        _definitionToBucket.Add(definition, bucket);
                    }

                    return bucket;
                }
            }

            public override Task ReportProgressAsync(int current, int maximum)
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

                return SpecializedTasks.EmptyTask;
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
                        var entries = _currentlyGroupingByDefinition && !_alwaysIncludeDeclarations
                            ? _entriesWithoutDeclarations
                            : _entriesWithDeclarations;

                        _lastSnapshot = new TableEntriesSnapshot(entries, CurrentVersionNumber);
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
                _tableDataSink.FactorySnapshotChanged(this);
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