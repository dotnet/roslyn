// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using System.ComponentModel;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal abstract class ProjectItemSource : IAttachedCollectionSource, INotifyPropertyChanged
    {
        private readonly BaseItem _folder;
        private readonly Workspace _workspace;
        private readonly ProjectId _projectId;

        public event PropertyChangedEventHandler PropertyChanged;

        protected ProjectItemSource(BaseItem folder, Workspace workspace, ProjectId projectId)
        {
            _folder = folder;
            _workspace = workspace;
            _projectId = projectId;
            _workspace.WorkspaceChanged += Workspace_WorkspaceChanged;
        }

        private void Workspace_WorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            switch (e.Kind)
            {
                case WorkspaceChangeKind.SolutionAdded:
                case WorkspaceChangeKind.SolutionChanged:
                case WorkspaceChangeKind.SolutionReloaded:
                    Update();
                    break;

                case WorkspaceChangeKind.SolutionRemoved:
                case WorkspaceChangeKind.SolutionCleared:
                    _workspace.WorkspaceChanged -= Workspace_WorkspaceChanged;
                    break;

                case WorkspaceChangeKind.ProjectAdded:
                case WorkspaceChangeKind.ProjectReloaded:
                case WorkspaceChangeKind.ProjectChanged:
                    if (e.ProjectId == _projectId)
                    {
                        Update();
                    }

                    break;

                case WorkspaceChangeKind.ProjectRemoved:
                    if (e.ProjectId == _projectId)
                    {
                        _workspace.WorkspaceChanged -= Workspace_WorkspaceChanged;
                    }

                    break;
            }
        }

        protected abstract void Update();

        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public abstract bool HasItems { get; }

        public abstract IEnumerable Items { get; }

        public object SourceItem
        {
            get { return _folder; }
        }
    }
}
