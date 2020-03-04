﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal partial class TaggerEventSources
    {
        private class SemanticChangedEventSource : AbstractWorkspaceTrackingTaggerEventSource
        {
            private readonly ISemanticChangeNotificationService _notificationService;

            public SemanticChangedEventSource(ITextBuffer subjectBuffer, TaggerDelay delay, ISemanticChangeNotificationService notificationService)
                : base(subjectBuffer, delay)
            {
                _notificationService = notificationService;
            }

            public override void Connect()
            {
                base.Connect();
                this.SubjectBuffer.Changed += OnSubjectBufferChanged;
            }

            public override void Disconnect()
            {
                this.SubjectBuffer.Changed -= OnSubjectBufferChanged;
                base.Disconnect();
            }

            protected override void ConnectToWorkspace(Workspace workspace)
            {
                _notificationService.OpenedDocumentSemanticChanged += OnOpenedDocumentSemanticChanged;
                this.RaiseChanged();
            }

            protected override void DisconnectFromWorkspace(Workspace workspace)
            {
                _notificationService.OpenedDocumentSemanticChanged -= OnOpenedDocumentSemanticChanged;
                this.RaiseChanged();
            }

            private void OnSubjectBufferChanged(object sender, TextContentChangedEventArgs e)
            {
                // Whenever this subject buffer has changed, we always consider that to be a 
                // semantic change.
                if (e.Changes.Any())
                {
                    RaiseChanged();
                }
            }

            private void OnOpenedDocumentSemanticChanged(object sender, Document document)
            {
                var workspace = this.CurrentWorkspace;

                if (document.Project.Solution.Workspace != workspace)
                {
                    return;
                }

                var documentIds = workspace.GetRelatedDocumentIds(SubjectBuffer.AsTextContainer());

                if (!documentIds.Contains(document.Id))
                {
                    return;
                }

                // Semantics may change for a document for two reasons.  One is a top level change
                // outside of this document.  The other is that a change happened inside the document.
                // In the latter case we do *not* want to report a change because we'll already have
                // done so inside of OnSubjectBufferChanged.
                //
                // Note: although we're passing CancellationToken.None here, this should never actually
                // block.  This is because we would have only gotten this notification if this value
                // was already computed.  In which case retrieving it again should happen immediately.
                var documentVersion = document.GetTopLevelChangeTextVersionAsync(CancellationToken.None).WaitAndGetResult_CanCallOnBackground(CancellationToken.None);
                var projectVersion = document.Project.GetDependentSemanticVersionAsync(CancellationToken.None).WaitAndGetResult_CanCallOnBackground(CancellationToken.None);

                if (documentVersion == projectVersion)
                {
                    // The semantic version notification was caused by a change to this document.  
                    // In which case we want to *ignore* it as we will have already processed its
                    // buffer change event.
                    return;
                }

                // The semantic version notification was caused by something else (a sibling document
                // changing at the top level, or a dependent project changing), we want to report this
                // so that this document can be retagged.
                this.RaiseChanged();
            }
        }
    }
}
