﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
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
                 IFindAllReferencesWindow findReferencesWindow)
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
                _findReferencesWindow.Manager.AddSource(this);

                // After adding us as the source, the manager should immediately call into us to
                // tell us what the data sink is.
                Debug.Assert(_tableDataSink != null);
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

            public sealed override void SetSearchTitle(string title)
                => _findReferencesWindow.Title = title;

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
                var workspace = document.Project.Solution.Workspace as VisualStudioWorkspaceImpl;
                var hostProject = workspace?.GetHostProject(document.Project.Id);

                var projectName = hostProject?.DisplayName ?? document.Project.Name;
                var guid = hostProject?.Guid ?? Guid.Empty;

                var sourceText = await document.GetTextAsync(CancellationToken).ConfigureAwait(false);
                return (guid, projectName, sourceText);
            }

            protected async Task<Entry> CreateDocumentSpanEntryAsync(
                RoslynDefinitionBucket definitionBucket,
                DocumentSpan documentSpan,
                HighlightSpanKind spanKind)
            {
                var document = documentSpan.Document;
                var (guid, projectName, sourceText) = await GetGuidAndProjectNameAndSourceTextAsync(document).ConfigureAwait(false);

                var narrowSpan = documentSpan.SourceSpan;
                var lineSpan = GetLineSpanForReference(sourceText, narrowSpan);

                var taggedLineParts = await GetTaggedTextForDocumentRegionAsync(document, narrowSpan, lineSpan).ConfigureAwait(false);

                return new DocumentSpanEntry(
                    this, definitionBucket, documentSpan, spanKind,
                    projectName, guid, sourceText, taggedLineParts);
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

            private async Task<ClassifiedSpansAndHighlightSpan> GetTaggedTextForDocumentRegionAsync(
                Document document, TextSpan narrowSpan, TextSpan widenedSpan)
            {
                var highlightSpan = new TextSpan(
                    start: narrowSpan.Start - widenedSpan.Start,
                    length: narrowSpan.Length);

                var classifiedSpans = await GetClassifiedSpansAsync(document, narrowSpan, widenedSpan).ConfigureAwait(false);
                return new ClassifiedSpansAndHighlightSpan(classifiedSpans, highlightSpan);
            }

            private async Task<ImmutableArray<ClassifiedSpan>> GetClassifiedSpansAsync(
                Document document, TextSpan narrowSpan, TextSpan widenedSpan)
            {
                var classificationService = document.GetLanguageService<IEditorClassificationService>();
                if (classificationService == null)
                {
                    // For languages that don't expose a classification service, we show the entire
                    // item as plain text. Break the text into three spans so that we can properly
                    // highlight the 'narrow-span' later on when we display the item.
                    return ImmutableArray.Create(
                        new ClassifiedSpan(ClassificationTypeNames.Text, TextSpan.FromBounds(widenedSpan.Start, narrowSpan.Start)),
                        new ClassifiedSpan(ClassificationTypeNames.Text, narrowSpan),
                        new ClassifiedSpan(ClassificationTypeNames.Text, TextSpan.FromBounds(narrowSpan.End, widenedSpan.End)));
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
                    return classifiedSpans;
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

            public sealed override Task OnReferenceFoundAsync(SourceReferenceItem reference)
                => OnReferenceFoundWorkerAsync(reference);

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

                return SpecializedTasks.EmptyTask;
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