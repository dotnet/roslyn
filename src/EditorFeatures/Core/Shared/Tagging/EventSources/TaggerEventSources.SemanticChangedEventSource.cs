// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal partial class TaggerEventSources
    {
        private class SemanticChangedEventSource : AbstractWorkspaceTrackingTaggerEventSource
        {
            private readonly ISemanticChangeNotificationService _notificationService;

            public SemanticChangedEventSource(ITextBuffer subjectBuffer, TaggerDelay delay, ISemanticChangeNotificationService notificationService) :
                base(subjectBuffer, delay)
            {
                _notificationService = notificationService;
            }

            public override string EventKind
            {
                get
                {
                    return PredefinedChangedEventKinds.SemanticsChanged;
                }
            }

            protected override void ConnectToWorkspace(Workspace workspace)
            {
                _notificationService.OpenedDocumentSemanticChanged += OnSemanticChanged;
            }

            protected override void DisconnectFromWorkspace(Workspace workspace)
            {
                _notificationService.OpenedDocumentSemanticChanged -= OnSemanticChanged;
            }

            private void OnSemanticChanged(object sender, Document d)
            {
                var workspace = this.CurrentWorkspace;

                if (d.Project.Solution.Workspace != workspace)
                {
                    return;
                }

                var documentIds = workspace.GetRelatedDocumentIds(SubjectBuffer.AsTextContainer());

                if (documentIds.Contains(d.Id))
                {
                    this.RaiseChanged();
                }
            }
        }
    }
}
