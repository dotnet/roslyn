// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
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
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.FindReferences
{
    internal partial class StreamingFindReferencesPresenter
    {
        private class TableDataSourceFindReferencesContext :
            FindReferencesContext, ITableDataSource, ITableEntriesSnapshotFactory
        {
            private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

            private readonly ConcurrentBag<Subscription> _subscriptions = new ConcurrentBag<Subscription>();

            public readonly StreamingFindReferencesPresenter Presenter;
            private readonly IFindAllReferencesWindow _findReferencesWindow;

            private readonly Dictionary<Document, ValueTuple<ITextBuffer, PreviewWorkspace>> _documentToPreviewWorkspace = 
                new Dictionary<Document, ValueTuple<ITextBuffer, PreviewWorkspace>>();

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
            private readonly Dictionary<DefinitionItem, RoslynDefinitionBucket> _definitionToBucket =
                new Dictionary<DefinitionItem, RoslynDefinitionBucket>();

            private ImmutableList<ReferenceEntry> _referenceEntries = ImmutableList<ReferenceEntry>.Empty;
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
                _findReferencesWindow.Closed += (s, e) => CancelSearchAndCleanUpResources();

                // Remove any existing sources in the window.  
                foreach (var source in findReferencesWindow.Manager.Sources.ToArray())
                {
                    findReferencesWindow.Manager.RemoveSource(source);
                }

                // And add ourselves as the source of results for the window.
                findReferencesWindow.Manager.AddSource(this);
            }

            private void CancelSearchAndCleanUpResources()
            {
                Presenter.AssertIsForeground();

                _cancellationTokenSource.Cancel();

                foreach (var bufferAndWorkspace in _documentToPreviewWorkspace.Values)
                {
                    bufferAndWorkspace.Item2.Dispose();
                }

                _documentToPreviewWorkspace.Clear();
            }

            internal void OnSubscriptionDisposed()
            {
                CancelSearchAndCleanUpResources();
            }

            public override CancellationToken CancellationToken => _cancellationTokenSource.Token;

            #region ITableDataSource

            public string DisplayName => "Roslyn Data Source";

            public string Identifier => RoslynFindReferencesTableDataSourceIdentifier;

            public string SourceTypeIdentifier => RoslynFindReferencesTableDataSourceSourceTypeIdentifier;

            public IDisposable Subscribe(ITableDataSink sink)
            {
                var subscription = new Subscription(this, sink);
                _subscriptions.Add(subscription);

                sink.AddFactory(this, removeAllFactories: true);

                return subscription;
            }

            #endregion

            #region FindReferencesContext overrides.

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

            public async override void OnReferenceFound(SourceReferenceItem reference)
            {
                try
                {
                    // We're told about this reference synchronously, but we need to get the 
                    // SourceText for the definition/reference's Document so that we can determine 
                    // things like it's line/column/text.  We don't want to block this method getting
                    // that data, so instead we just fire off the async work to get the text
                    // and use it.  Because we're starting some async work, let the test harness
                    // know so that it doesn't verify results until this completes.
                    using (var token = Presenter._asyncListener.BeginAsyncOperation(nameof(OnReferenceFound)))
                    {
                        await OnReferenceFoundAsync(reference).ConfigureAwait(false);
                    }
                }
                catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
                {
                }
            }

            private async Task OnReferenceFoundAsync(SourceReferenceItem referenceItem)
            {
                var cancellationToken = _cancellationTokenSource.Token;
                cancellationToken.ThrowIfCancellationRequested();

                // First find the bucket corresponding to our definition. If we can't find/create 
                // one, then don't do anything for this reference.
                var definitionBucket = GetOrCreateDefinitionBucket(referenceItem.Definition);
                if (definitionBucket == null)
                {
                    return;
                }

                // Now make the underlying data object for the reference.
                //var entryData = await this.TryCreateNavigableItemEntryData(
                //    referenceItem, isDefinition: false, cancellationToken: cancellationToken).ConfigureAwait(false);
                var referenceEntry = await this.CreateReferenceEntryAsync(
                    definitionBucket, referenceItem, cancellationToken).ConfigureAwait(false);
                if (referenceEntry == null)
                {
                    return;
                }

                lock (_gate)
                {
                    // Once we can make the new entry, add it to our list.
                    _referenceEntries = _referenceEntries.Add(referenceEntry);
                    CurrentVersionNumber++;
                }

                // Let all our subscriptions know that we've updated.
                NotifySinksOfChangedVersion();
            }

            internal ITextBuffer GetTextBufferForPreview(
                Document document, SourceText sourceText)
            {
                Presenter.AssertIsForeground();

                ValueTuple<ITextBuffer, PreviewWorkspace> bufferAndWorkspace;
                if (!_documentToPreviewWorkspace.TryGetValue(document, out bufferAndWorkspace))
                {
                    var textBuffer = CreateNewBuffer(document, sourceText);

                    var newDocument = document.WithText(textBuffer.AsTextContainer().CurrentText);

                    var workspace = new PreviewWorkspace(newDocument.Project.Solution);
                    workspace.OpenDocument(newDocument.Id);

                    bufferAndWorkspace = ValueTuple.Create(textBuffer, workspace);
                    _documentToPreviewWorkspace.Add(document, bufferAndWorkspace);
                }

                return bufferAndWorkspace.Item1;
            }

            private ITextBuffer CreateNewBuffer(Document document, SourceText sourceText)
            {
                Presenter.AssertIsForeground();

                // is it okay to create buffer from threads other than UI thread?
                var contentTypeService = document.Project.LanguageServices.GetService<IContentTypeLanguageService>();
                var contentType = contentTypeService.GetDefaultContentType();

                return Presenter._textBufferFactoryService.CreateTextBuffer(
                    sourceText.ToString(), contentType);
            }


            private async Task<ReferenceEntry> CreateReferenceEntryAsync(
                RoslynDefinitionBucket definitionBucket, SourceReferenceItem referenceItem, CancellationToken cancellationToken)
            {
                var location = referenceItem.Location;
                var document = location.Document;

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

                var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                var referenceSpan = referenceItem.Location.SourceSpan;
                var lineSpan = GetLineSpanForReference(sourceText, referenceSpan);

                var taggedLineParts = await GetTaggedTextForReferenceAsync(document, referenceSpan, lineSpan, cancellationToken).ConfigureAwait(false);

                return new ReferenceEntry(
                    this, workspace, definitionBucket, referenceItem, 
                    projectGuid.Value, sourceText, taggedLineParts);
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

            private async Task<TaggedTextAndHighlightSpan> GetTaggedTextForReferenceAsync(
                Document document, TextSpan referenceSpan, TextSpan widenedSpan, CancellationToken cancellationToken)
            {
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                var classifiedLineParts = await Classifier.GetClassifiedSymbolDisplayPartsAsync(
                    semanticModel, widenedSpan, document.Project.Solution.Workspace,
                    insertSourceTextInGaps: true,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                var taggedText = classifiedLineParts.ToTaggedText();

                var highlightSpan = new TextSpan(
                    start: referenceSpan.Start - widenedSpan.Start, 
                    length: referenceSpan.Length);

                return new TaggedTextAndHighlightSpan(taggedText, highlightSpan);
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

            private void NotifySinksOfChangedVersion()
            {
                foreach (var subscription in _subscriptions)
                {
                    subscription.TableDataSink.FactorySnapshotChanged(this);
                }
            }

            public override void ReportProgress(int current, int maximum)
            {
                //var progress = maximum == 0 ? 0 : ((double)current / maximum);
                //_findReferencesWindow.SetProgress(progress);
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
                CancelSearchAndCleanUpResources();
            }

            #endregion
        }
    }
}