// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;

namespace MSBuildWorkspaceTester.ViewModels
{
    internal class ProjectReferenceViewModel : HierarchyItemViewModel
    {
        private readonly ProjectReference _projectReference;

        public ProjectReferenceViewModel(Workspace workspace, ProjectReference projectReference)
            : base(workspace)
        {
            _projectReference = projectReference;
        }

        protected override string GetDisplayName()
            => Workspace.CurrentSolution.GetProject(_projectReference.ProjectId).Name;

        public string Language
            => Workspace.CurrentSolution.GetProject(_projectReference.ProjectId).Language;
    }
}
