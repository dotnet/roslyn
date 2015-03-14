// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics
{
    internal abstract partial class AbstractAggregatedDiagnosticsTagSource<TTag>
    {
        private class InteractiveMode : Mode
        {
            private readonly WorkspaceRegistration _workspaceRegistration;
            private readonly ConcurrentDictionary<object, DiagnosticsTagSource> _tagSources;

            private DocumentId _lastDocumentId;
            private bool _called;

            public InteractiveMode(AbstractAggregatedDiagnosticsTagSource<TTag> owner) : base(owner)
            {
                _lastDocumentId = null;
                _called = false;

                _tagSources = new ConcurrentDictionary<object, DiagnosticsTagSource>(concurrencyLevel: 2, capacity: 10);
                this.DiagnosticService.DiagnosticsUpdated += OnDiagnosticsUpdated;

                _workspaceRegistration = Workspace.GetWorkspaceRegistration(this.SubjectBuffer.AsTextContainer());
                _workspaceRegistration.WorkspaceChanged += OnWorkspaceChanged;
            }

            public override void Disconnect()
            {
                this.DiagnosticService.DiagnosticsUpdated -= OnDiagnosticsUpdated;
                _workspaceRegistration.WorkspaceChanged -= OnWorkspaceChanged;

                ShutdownTagSources();
            }

            public override IList<ITagSpan<TTag>> GetIntersectingSpans(SnapshotSpan snapshotSpan)
            {
                if (snapshotSpan.Snapshot.TextBuffer != this.SubjectBuffer)
                {
                    // in venus case, buffer comes and goes and tag source might hold onto diagnostics that belong to
                    // old/new buffers which are different than current subject buffer. 
                    return null;
                }

                var introspector = new IntervalIntrospector(snapshotSpan.Snapshot);

                var result = new List<ITagSpan<TTag>>();
                foreach (var tagSource in _tagSources.Values)
                {
                    tagSource.AppendIntersectingSpans(snapshotSpan.Start, snapshotSpan.Length, introspector, result);
                }

                return result;
            }

            private void OnWorkspaceChanged(object sender, EventArgs e)
            {
                ProcessTheFirstCall();
            }

            private void OnDiagnosticsUpdated(object sender, DiagnosticsUpdatedArgs e)
            {
                using (Logger.LogBlock(FunctionId.Tagger_Diagnostics_Updated, CancellationToken.None))
                using (this.Listener.BeginAsyncOperation("OnDiagnosticsUpdated"))
                {
                    // check whether new diagnostics is for this document.
                    if (!CheckDocumentContext(e.Workspace, e.DocumentId))
                    {
                        return;
                    }

                    ProcessProjectContextChange(e);

                    if (ProcessRemovedDocument(e))
                    {
                        // document has been removed
                        return;
                    }

                    SourceText text;
                    ITextSnapshot snapshot;
                    if (!ProcessRoslynToEditorSnapshotMapping(e.Id, e.Solution.GetDocument(e.DocumentId), out text, out snapshot))
                    {
                        // we don't have roslyn snapshot to editor snapshot mapping
                        return;
                    }

                    var tagSource = GetOrAddTagSource(e.Id);
                    tagSource.OnDiagnosticsUpdated(e, text, snapshot, TaggerDelay.Medium.ComputeTimeDelay(this.SubjectBuffer));
                }
            }

            private bool CheckDocumentContext(Workspace workspace, DocumentId id)
            {
                var document = this.SubjectBuffer.AsTextContainer().GetOpenDocumentInCurrentContext();
                if (document == null || document.Project.Solution.Workspace != workspace || !document.Id.Equals(id))
                {
                    return false;
                }

                return true;
            }

            private bool ProcessRoslynToEditorSnapshotMapping(object id, Document document, out SourceText text, out ITextSnapshot snapshot)
            {
                if (TryGetOldSnapshot(document, out text, out snapshot))
                {
                    // succeeded
                    return true;
                }

                ShutdownTagSource(id);
                return false;
            }

            private DiagnosticsTagSource GetOrAddTagSource(object id)
            {
                return _tagSources.GetOrAdd(id, key => new DiagnosticsTagSource(this.Owner, key));
            }

            private bool ProcessRemovedDocument(DiagnosticsUpdatedArgs e)
            {
                if (e.Solution != null &&
                    e.Solution.GetDocument(e.DocumentId) != null &&
                    e.Diagnostics.Any(this.Owner.ShouldInclude))
                {
                    return false;
                }

                // the document has been removed
                ShutdownTagSource(e.Id);
                return true;
            }

            private void ShutdownTagSource(object id)
            {
                var tagSource = default(DiagnosticsTagSource);
                if (!_tagSources.TryRemove(id, out tagSource))
                {
                    // tag source not exist
                    return;
                }

                // shutdown this tag source
                tagSource.Shutdown();
            }

            private void ProcessProjectContextChange(DiagnosticsUpdatedArgs e)
            {
                if (e.DocumentId == _lastDocumentId)
                {
                    return;
                }

                // linked file context has changed. re-initialize state
                _lastDocumentId = e.DocumentId;

                // clear all state
                ShutdownTagSources();
                _tagSources.Clear();

                if (e.Solution == null)
                {
                    // document is removed
                    return;
                }

                // setup initial diagnostics
                PopulateInitialDiagnostics(e.Solution.GetDocument(e.DocumentId));

                // ask to refresh entire buffer
                RefreshEntireBuffer();
            }

            private void ShutdownTagSources()
            {
                foreach (var tagSource in _tagSources.Values)
                {
                    tagSource.Shutdown();
                }
            }

            private void PopulateInitialDiagnostics(Document document)
            {
                SourceText text;
                ITextSnapshot snapshot;
                if (!TryGetOldSnapshot(document, out text, out snapshot))
                {
                    return;
                }

                var delay = TaggerDelay.Medium.ComputeTimeDelay(this.SubjectBuffer);
                var solution = document.Project.Solution;

                var map = this.DiagnosticService.GetEngineCachedDiagnostics(document.Id);
                foreach (var kv in map)
                {
                    if (kv.Value.IsEmpty)
                    {
                        ShutdownTagSource(kv.Key);
                        continue;
                    }

                    var tagSource = GetOrAddTagSource(kv.Key);

                    var args = new DiagnosticsUpdatedArgs(kv.Key, solution.Workspace, solution, document.Project.Id, document.Id, kv.Value);
                    tagSource.OnDiagnosticsUpdated(args, text, snapshot, delay);
                }
            }

            private void ProcessTheFirstCall()
            {
                using (this.Listener.BeginAsyncOperation("ProcessTheFirstCall"))
                {
                    if (_called)
                    {
                        return;
                    }

                    _called = true;

                    var document = this.SubjectBuffer.AsTextContainer().GetOpenDocumentInCurrentContext();
                    if (document == null)
                    {
                        return;
                    }

                    PopulateInitialDiagnostics(document);
                }
            }

            private bool TryGetOldSnapshot(Document document, out SourceText text, out ITextSnapshot oldSnapshot)
            {
                if (!document.TryGetText(out text))
                {
                    oldSnapshot = null;
                    return false;
                }

                oldSnapshot = text.FindCorrespondingEditorTextSnapshot();
                if (oldSnapshot == null)
                {
                    return false;
                }

                // two should be same
                Contract.ThrowIfFalse(text.Length == oldSnapshot.Length);
                return true;
            }
        }
    }
}
