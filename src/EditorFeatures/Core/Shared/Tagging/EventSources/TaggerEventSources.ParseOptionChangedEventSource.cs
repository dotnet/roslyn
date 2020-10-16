﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal partial class TaggerEventSources
    {
        private class ParseOptionChangedEventSource : AbstractWorkspaceTrackingTaggerEventSource
        {
            public ParseOptionChangedEventSource(ITextBuffer subjectBuffer, TaggerDelay delay)
                : base(subjectBuffer, delay)
            {
            }

            protected override void ConnectToWorkspace(Workspace workspace)
                => workspace.WorkspaceChanged += OnWorkspaceChanged;

            protected override void DisconnectFromWorkspace(Workspace workspace)
                => workspace.WorkspaceChanged -= OnWorkspaceChanged;

            private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
            {
                if (e.Kind == WorkspaceChangeKind.ProjectChanged)
                {
                    var oldProject = e.OldSolution.GetProject(e.ProjectId);
                    var newProject = e.NewSolution.GetProject(e.ProjectId);

                    if (!object.Equals(oldProject.ParseOptions, newProject.ParseOptions))
                    {
                        var workspace = e.NewSolution.Workspace;
                        var documentId = workspace.GetDocumentIdInCurrentContext(SubjectBuffer.AsTextContainer());
                        if (documentId != null)
                        {
                            var relatedDocumentIds = e.NewSolution.GetRelatedDocumentIds(documentId);

                            if (relatedDocumentIds.Any(d => d.ProjectId == e.ProjectId))
                            {
                                RaiseChanged();
                            }
                        }
                    }
                }
            }
        }
    }
}
