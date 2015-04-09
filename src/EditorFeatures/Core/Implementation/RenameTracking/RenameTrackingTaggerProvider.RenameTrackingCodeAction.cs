// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking
{
    internal sealed partial class RenameTrackingTaggerProvider
    {
        private class RenameTrackingCodeAction : CodeAction
        {
            private readonly string _title;
            private readonly Document _document;
            private readonly IEnumerable<IRefactorNotifyService> _refactorNotifyServices;
            private readonly bool _showPreview;
            private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;

            public RenameTrackingCodeAction(Document document, string title, IEnumerable<IRefactorNotifyService> refactorNotifyServices, ITextUndoHistoryRegistry undoHistoryRegistry, bool showPreview)
            {
                _document = document;
                _title = title;
                _refactorNotifyServices = refactorNotifyServices;
                _undoHistoryRegistry = undoHistoryRegistry;
                _showPreview = showPreview;
            }

            public override string Title { get { return _title; } }

            protected override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
            {
                SourceText text;
                StateMachine stateMachine;
                ITextBuffer textBuffer;

                if (_document.TryGetText(out text))
                {
                    textBuffer = text.Container.TryGetTextBuffer();
                    if (textBuffer == null)
                    {
                        Environment.FailFast(string.Format("document with name {0} is open but textBuffer is null. Textcontainer is of type {1}. SourceText is: {2}",
                                                            _document.Name, text.Container.GetType().FullName, text.ToString()));
                    }

                    if (textBuffer.Properties.TryGetProperty(typeof(StateMachine), out stateMachine))
                    {
                        TrackingSession trackingSession;
                        if (stateMachine.CanInvokeRename(out trackingSession, cancellationToken: cancellationToken))
                        {
                            var snapshotSpan = stateMachine.TrackingSession.TrackingSpan.GetSpan(stateMachine.Buffer.CurrentSnapshot);
                            var str = string.Format(EditorFeaturesResources.RenameTo, stateMachine.TrackingSession.OriginalName, snapshotSpan.GetText());
                            var committerOperation = new RenameTrackingCommitterOperation(new RenameTrackingCommitter(stateMachine, snapshotSpan, _refactorNotifyServices, _undoHistoryRegistry, str, showPreview: _showPreview));
                            return Task.FromResult(SpecializedCollections.SingletonEnumerable(committerOperation as CodeActionOperation));
                        }

                        // The rename tracking could be dismissed while a codefix is still cached
                        // in the lightbulb. If this happens, do not perform the rename requested
                        // and instead let the user know their fix will not be applied. 
                        _document.Project.Solution.Workspace.Services.GetService<INotificationService>()
                            ?.SendNotification(EditorFeaturesResources.TheRenameTrackingSessionWasCancelledAndIsNoLongerAvailable, severity: NotificationSeverity.Error);
                        return SpecializedTasks.EmptyEnumerable<CodeActionOperation>();
                    }
                }

                Debug.Assert(false, "RenameTracking codefix invoked on a document for which the text or StateMachine is not available.");
                return SpecializedTasks.EmptyEnumerable<CodeActionOperation>();
            }

            protected override Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
            {
                return SpecializedTasks.EmptyEnumerable<CodeActionOperation>();
            }

            private sealed class RenameTrackingCommitterOperation : CodeActionOperation
            {
                private readonly RenameTrackingCommitter _committer;

                public RenameTrackingCommitterOperation(RenameTrackingCommitter committer)
                {
                    _committer = committer;
                }

                public override void Apply(Workspace workspace, CancellationToken cancellationToken)
                {
                    _committer.Commit(cancellationToken);
                }
            }
        }
    }
}
