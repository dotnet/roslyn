// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            {
                workspace.WorkspaceChanged += OnWorkspaceChanged;
            }

            protected override void DisconnectFromWorkspace(Workspace workspace)
            {
                workspace.WorkspaceChanged -= OnWorkspaceChanged;
            }

            private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
            {
                if (e.Kind == WorkspaceChangeKind.ProjectChanged)
                {
                    var oldProject = e.OldSolution.GetProject(e.ProjectId);
                    var newProject = e.NewSolution.GetProject(e.ProjectId);

                    if (!object.Equals(oldProject.ParseOptions, newProject.ParseOptions))
                    {
                        var workspace = e.NewSolution.Workspace;
                        var documentIds = workspace.GetRelatedDocumentIds(SubjectBuffer.AsTextContainer());

                        if (documentIds.Any(d => d.ProjectId == e.ProjectId))
                        {
                            this.RaiseChanged();
                        }
                    }
                }
            }
        }
    }
}
