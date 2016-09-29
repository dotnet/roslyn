// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindReferences;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.FindAllReferences;
using Microsoft.VisualStudio.Shell.TableManager;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Completion;
using System.Diagnostics;

namespace Microsoft.VisualStudio.LanguageServices.FindReferences
{
    internal partial class StreamingFindReferencesPresenter
    {
        private class TableDataSourceFindReferencesContext :
            FindReferencesContext, ITableDataSource, ITableEntriesSnapshotFactory
        {
            private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

            private ITableDataSink _tableDataSink;

            public readonly StreamingFindReferencesPresenter Presenter;
            private readonly IFindAllReferencesWindow _findReferencesWindow;

            // Lock which protects _definitionToShoudlShowWithoutReferences, 
            // _definitionToBucket, _entries, _lastSnapshot and CurrentVersionNumber
            private readonly object _gate = new object();

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

            private ImmutableList<Entry> _entries = ImmutableList<Entry>.Empty;
            private TableEntriesSnapshot _lastSnapshot;
            public int CurrentVersionNumber { get; private set; }

            public TableDataSourceFindReferencesContext(
                 StreamingFindReferencesPresenter presenter,
                 IFindAllReferencesWindow findReferencesWindow)
            {
                presenter.AssertIsForeground();

                Presenter = presenter;
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

                // After adding us as the source, the manager should immediately call into us to
                // tell us what the data sink is.
                Debug.Assert(_tableDataSink != null);
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

            public string Identifier => RoslynFindReferencesTableDataSourceIdentifier;

            public string SourceTypeIdentifier => RoslynFindReferencesTableDataSourceSourceTypeIdentifier;

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

            #region FindReferencesContext overrides.

            public override void SetSearchLabel(string displayName)
            {
                var labelProperty = _findReferencesWindow.GetType().GetProperty("Label");
                if (labelProperty != null)
                {
                    labelProperty.SetValue(_findReferencesWindow, displayName);
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
                            bucket, ServicesVisualStudioNextResources.Search_found_no_results)).ConfigureAwait(false);
                }
            }

            private static DefinitionItem NoResultsDefinitionItem =
                DefinitionItem.CreateNonNavigableItem(
                    GlyphTags.GetTags(Glyph.StatusInformation),
                    ImmutableArray.Create(new TaggedText(
                        TextTags.Text,
                        ServicesVisualStudioNextResources.Search_found_no_results)));

            private async Task CreateMissingReferenceEntriesIfNecessaryAsync()
            {
                // Go through and add dummy entries for any definitions that 
                // that we didn't find any references for.

                var definitions = GetDefinitionsToCreateMissingReferenceItemsFor();
                foreach (var definition in definitions)
                {
                    // Create a fake reference to this definition that says 
                    // "no references found to <symbolname>".
                    await OnEntryFoundAsync(definition,
                        bucket => SimpleMessageEntry.CreateAsync(
                            bucket, GetMessage(bucket.DefinitionItem))).ConfigureAwait(false);
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
                    definition.DisplayParts.JoinText());
            }

            private ImmutableArray<DefinitionItem> GetDefinitionsToCreateMissingReferenceItemsFor()
            {
                lock (_gate)
                {
                    // Find any definitions that we didn't have any references to. But only show 
                    // them if they want to be displayed without any references.  This will 
                    // ensure that we still see things like overrides and whatnot, but we
                    // won't show property-accessors.
                    var seenDefinitions = this._entries.Select(r => r.DefinitionBucket.DefinitionItem).ToSet();
                    var q = from definition in _definitions
                            where !seenDefinitions.Contains(definition) &&
                                  definition.DisplayIfNoReferences
                            select definition;

                    // If we find at least one of these tyeps of definitions, then just return those.
                    var result = ImmutableArray.CreateRange(q);
                    if (result.Length > 0)
                    {
                        return result;
                    }

                    // We found no definitions that *want* to be displayed.  However, we still 
                    // want to show something.  So, if necessary, show at lest the first definition
                    // even if we found no references and even if it would prefer to not be seen.
                    if (_entries.Count == 0 && _definitions.Count > 0)
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
                // for all the definition locations immediately.
                if (definition.DisplayIfNoReferences)
                {
                    await AddDefinitionEntriesAsync(definition).ConfigureAwait(false);
                }
            }

            private async Task AddDefinitionEntriesAsync(DefinitionItem definition)
            {
                CancellationToken.ThrowIfCancellationRequested();

                // Don't do anything if we already have entries for this definition 
                // (i.e. another thread beat us to this).
                if (HasEntriesForDefinition(definition))
                {
                    return;
                }

                // First find the bucket corresponding to our definition. If we can't find/create 
                // one, then don't do anything for this reference.
                var definitionBucket = GetOrCreateDefinitionBucket(definition);
                if (definitionBucket == null)
                {
                    return;
                }

                // We could do this inside the lock.  but that would mean async activity in a 
                // lock, and i'd like to avoid that.  That does mean that we might do extra
                // work if multiple threads end up down htis path.  But only one of them will
                // win when we access the lock below.
                var builder = ImmutableArray.CreateBuilder<Entry>();
                foreach (var definitionLocation in definition.SourceSpans)
                {
                    var definitionEntry = await CreateDocumentLocationEntryAsync(
                        definitionBucket, definitionLocation, isDefinitionLocation: true).ConfigureAwait(false);
                    if (definitionEntry != null)
                    {
                        builder.Add(definitionEntry);
                    }
                }

                lock (_gate)
                {
                    // Do one final check to ensure that no other thread beat us here.
                    if (!HasEntriesForDefinition(definition))
                    {
                        _entries = _entries.AddRange(builder);
                        CurrentVersionNumber++;
                    }
                }

                // Let all our subscriptions know that we've updated.
                _tableDataSink.FactorySnapshotChanged(this);
            }

            public override Task OnReferenceFoundAsync(SourceReferenceItem reference)
            {
                return OnEntryFoundAsync(reference.Definition,
                    bucket => CreateDocumentLocationEntryAsync(
                        bucket, reference.SourceSpan, isDefinitionLocation: false));
            }

            private async Task OnEntryFoundAsync(
                DefinitionItem definition,
                Func<RoslynDefinitionBucket, Task<Entry>> createEntryAsync)
            {
                CancellationToken.ThrowIfCancellationRequested();

                // First find the bucket corresponding to our definition. If we can't find/create 
                // one, then don't do anything for this reference.
                var definitionBucket = GetOrCreateDefinitionBucket(definition);
                if (definitionBucket == null)
                {
                    return;
                }

                var entry = await createEntryAsync(definitionBucket).ConfigureAwait(false);
                if (entry == null)
                {
                    return;
                }

                // Ok, we got a *reference* to some definition item.  This may have been
                // a reference for some definition that we haven't created any definition
                // entries for (i.e. becuase it had DisplayIfNoReferences = false).  Because
                // we've now found a reference, we want to make sure all tis definition
                // entries are added.
                await AddDefinitionEntriesAsync(definition).ConfigureAwait(false);

                lock (_gate)
                {
                    // Once we can make the new entry, add it to our list.
                    _entries = _entries.Add(entry);
                    CurrentVersionNumber++;
                }

                // Let all our subscriptions know that we've updated.
                _tableDataSink.FactorySnapshotChanged(this);
            }

            private bool HasEntriesForDefinition(DefinitionItem definition)
            {
                lock (_gate)
                {
                    return _entries.Any(e => e.DefinitionBucket.DefinitionItem == definition);
                }
            }

            private async Task<Entry> CreateDocumentLocationEntryAsync(
                RoslynDefinitionBucket definitionBucket, 
                DocumentSpan documentSpan,
                bool isDefinitionLocation)
            {
                var document = documentSpan.Document;

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

                var sourceText = await document.GetTextAsync(CancellationToken).ConfigureAwait(false);

                var referenceSpan = documentSpan.SourceSpan;
                var lineSpan = GetLineSpanForReference(sourceText, referenceSpan);

                var taggedLineParts = await GetTaggedTextForReferenceAsync(document, referenceSpan, lineSpan).ConfigureAwait(false);

                return new DocumentSpanEntry(
                    this, workspace, definitionBucket, documentSpan, 
                    isDefinitionLocation, projectGuid.Value, sourceText, taggedLineParts);
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
                var syntaxSpans = new List<ClassifiedSpan>();
                var semanticSpans = new List<ClassifiedSpan>();

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

                // The classification service will only produce classifications for
                // things it knows about.  i.e. there will be gaps in what it produces.
                // Fill in those gaps so we have *all* parts of the span 
                // classified properly.
                var syntaxParts = FillInClassifiedSpanGaps(sourceText, widenedSpan.Start, syntaxSpans);
                var semanticParts = FillInClassifiedSpanGaps(sourceText, widenedSpan.Start, semanticSpans);

                // Now merge the lists together, taking all the results from syntaxParts
                // unless they were overridden by results in semanticParts.
                return MergeParts(syntaxParts, semanticParts);
            }

            private static List<ClassifiedSpan> FillInClassifiedSpanGaps(
                SourceText sourceText, int startPosition, IEnumerable<ClassifiedSpan> classifiedSpans)
            {
                var result = new List<ClassifiedSpan>();

                foreach (var span in classifiedSpans)
                {
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

                return result;
            }

            private void Order(List<ClassifiedSpan> syntaxSpans)
            {
                syntaxSpans.Sort((s1, s2) => s1.TextSpan.Start - s2.TextSpan.Start);
            }

            private ImmutableArray<ClassifiedSpan> MergeParts(
                List<ClassifiedSpan> syntaxParts,
                List<ClassifiedSpan> semanticParts)
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
                    RoslynDefinitionBucket bucket;
                    if (!_definitionToBucket.TryGetValue(definition, out bucket))
                    {
                        bucket = new RoslynDefinitionBucket(Presenter, this, definition);
                        _definitionToBucket.Add(definition, bucket);
                    }

                    return bucket;
                }
            }

            public override Task ReportProgressAsync(int current, int maximum)
            {
                //var progress = maximum == 0 ? 0 : ((double)current / maximum);
                // _findReferencesWindow.SetProgress(current, maximum);
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