// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Implementation.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Classification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Tagger
{
    /// <summary>
    /// this is almost straight copy from typescript for syntatic LSP experiement.
    /// we won't attemp to change code to follow Roslyn style until we have result of the experiement
    /// </summary>
    internal sealed partial class SyntacticClassificationTaggerProvider
    {
        /// <summary>
        /// Backing implementation for all <see cref="Tagger"/> instances for a single <see cref="ITextBuffer"/>.
        /// </summary>
        internal partial class TagComputer
        {
            private static readonly IEnumerable<ITagSpan<IClassificationTag>> EmptyTagSpanEnumerable = Array.Empty<ITagSpan<IClassificationTag>>();

            private readonly ITextBuffer _textBuffer;
            private readonly ClassificationTypeMap _typeMap;
            private readonly WorkspaceRegistration _workspaceRegistration;
            private readonly AsynchronousSerialWorkQueue _workQueue;
            private readonly SyntacticClassificationTaggerProvider _taggerProvider;

            private Workspace _workspace;
            private TagSpanList<IClassificationTag> _lastProcessedTagList;
            private int _taggerReferenceCount;
            private int _isRequestPending; // int for Interlocked

            public TagComputer(
                ITextBuffer textBuffer,
                ClassificationTypeMap typeMap,
                IAsynchronousOperationListener asyncListener,
                SyntacticClassificationTaggerProvider taggerProvider)
            {
                _textBuffer = textBuffer;
                _typeMap = typeMap;
                _taggerProvider = taggerProvider;
                _workQueue = new AsynchronousSerialWorkQueue(taggerProvider._threadingContext, asyncListener);

                _workspaceRegistration = Workspace.GetWorkspaceRegistration(textBuffer.AsTextContainer());
                _workspaceRegistration.WorkspaceChanged += OnWorkspaceRegistrationChanged;

                if (this._workspaceRegistration.Workspace != null)
                {
                    ConnectToWorkspace(this._workspaceRegistration.Workspace);
                }
            }

            private void OnWorkspaceRegistrationChanged(object sender, EventArgs e)
            {
                // We both try to connect synchronously, and register for workspace registration events.
                // It's possible (particularly in tests), to connect in the startup path, but then get a
                // previously scheduled, but not yet delivered event.  Don't bother connecting to the
                // same workspace again in that case.
                var newWorkspace = this._workspaceRegistration.Workspace;
                if (newWorkspace == this._workspace)
                {
                    return;
                }

                DisconnectFromWorkspace();

                if (newWorkspace != null)
                {
                    ConnectToWorkspace(newWorkspace);
                }
            }

            internal void IncrementReferenceCount()
            {
                this._taggerReferenceCount++;
            }

            internal void DecrementReferenceCountAndDisposeIfNecessary()
            {
                this._taggerReferenceCount--;

                if (this._taggerReferenceCount == 0)
                {
                    DisconnectFromWorkspace();
                    this._workspaceRegistration.WorkspaceChanged -= OnWorkspaceRegistrationChanged;
                    this._taggerProvider.DisconnectTagComputer(this._textBuffer);
                }
            }

            private void ConnectToWorkspace(Workspace workspace)
            {
                workspace.WorkspaceChanged += this.OnWorkspaceChanged;
                workspace.DocumentOpened += this.OnDocumentOpened;
                workspace.DocumentActiveContextChanged += this.OnDocumentActiveContextChanged;
                this._workspace = workspace;

                EnqueueProcessSnapshotAsync();
            }

            public void DisconnectFromWorkspace()
            {
                var workspace = this._workspace;
                if (workspace != null)
                {
                    workspace.WorkspaceChanged -= this.OnWorkspaceChanged;
                    workspace.DocumentOpened -= this.OnDocumentOpened;
                    workspace.DocumentActiveContextChanged -= this.OnDocumentActiveContextChanged;

                    workspace = null;
                }
            }

            private void EnqueueProcessSnapshotAsync(DocumentId updatedDocumentId = null)
            {
                var workspace = this._workspace;
                if (workspace == null)
                {
                    return;
                }

                var documentId = workspace.GetDocumentIdInCurrentContext(this._textBuffer.AsTextContainer());
                if (documentId == null)
                {
                    return;
                }

                // If the caller specified a DocumentId and it's the one for this buffer (in the current context),
                // then there's nothing to do.
                if (updatedDocumentId != null && updatedDocumentId != documentId)
                {
                    // This is very common and not very interesting, so don't record it.
                    return;
                }

                if (Interlocked.CompareExchange(ref this._isRequestPending, 1, 0) != 0)
                {
                    return;
                }

                _workQueue.EnqueueBackgroundTask(c => this.EnqueueProcessSnapshotWorkerAsync(documentId, c), GetType() + "." + nameof(EnqueueProcessSnapshotAsync) + ".1", CancellationToken.None);
            }

            private async Task EnqueueProcessSnapshotWorkerAsync(DocumentId documentId, CancellationToken cancellationToken)
            {
                var workspace = this._workspace;
                if (workspace == null)
                {
                    return;
                }

                // This is an essentially arbitrary version of the document - we basically just want the path.
                var document = workspace.CurrentSolution.GetDocument(documentId);
                if (document == null)
                {
                    return;
                }

                // Changes after this point might not be incorporated into the server response, so
                // allow scheduling of additional requests.
                int wasRequestPending = Interlocked.Exchange(ref this._isRequestPending, 0);
                Debug.Assert(wasRequestPending == 1);

                var classificationService = document.Project.LanguageServices.GetService<IClassificationService>() as IRemoteClassificationService;
                if (classificationService == null)
                {
                    return;
                }

                var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var text = await tree.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var snapshot = text.FindCorrespondingEditorTextSnapshot();
                if (snapshot == null)
                {
                    return;
                }

                var classifiedSpans = ClassificationUtilities.GetOrCreateClassifiedSpanList();
                await classificationService.AddRemoteSyntacticClassificationsAsync(document, TextSpan.FromBounds(0, tree.Length), classifiedSpans, cancellationToken).ConfigureAwait(false);

                using var tagSpans = SharedPools.Default<List<ITagSpan<IClassificationTag>>>().GetPooledObject();
                ClassificationUtilities.Convert(_typeMap, snapshot, classifiedSpans, tagSpans.Object.Add);
                ClassificationUtilities.ReturnClassifiedSpanList(classifiedSpans);

                var tagList = new TagSpanList<IClassificationTag>(tagSpans.Object);
                Interlocked.Exchange(ref this._lastProcessedTagList, tagList);

                this.TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(snapshot.GetFullSpan()));
            }

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

            public IEnumerable<ITagSpan<IClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
            {
                // A background thread might be updating lastProcessedTagList.
                var tagList = Volatile.Read(ref this._lastProcessedTagList);
                return tagList?.GetTags(spans) ?? EmptyTagSpanEnumerable;
            }

            private void OnDocumentActiveContextChanged(object sender, DocumentActiveContextChangedEventArgs args)
            {
                var workspace = this._workspace;
                if (workspace != null && workspace == args.Solution.Workspace)
                {
                    EnqueueProcessSnapshotAsync(args.NewActiveContextDocumentId);
                }
            }

            private void OnDocumentOpened(object sender, DocumentEventArgs args)
            {
                EnqueueProcessSnapshotAsync(args.Document.Id);
            }

            private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs args)
            {
                // We're getting an event for a workspace we already disconnected from
                var workspace = this._workspace;
                if (args.NewSolution.Workspace != workspace)
                {
                    // we are async so we are getting events from previous workspace we were associated with
                    // just ignore them
                    return;
                }

                switch (args.Kind)
                {
                    case WorkspaceChangeKind.ProjectChanged:
                        {
                            var documentId = workspace.GetDocumentIdInCurrentContext(this._textBuffer.AsTextContainer());
                            if (documentId?.ProjectId == args.ProjectId)
                            {
                                EnqueueProcessSnapshotAsync();
                            }
                            break;
                        }
                    case WorkspaceChangeKind.DocumentChanged:
                        {
                            EnqueueProcessSnapshotAsync(args.DocumentId);
                            break;
                        }
                }
            }
        }
    }
}
