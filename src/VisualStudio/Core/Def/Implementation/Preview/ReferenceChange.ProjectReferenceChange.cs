// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Preview
{
    internal abstract partial class ReferenceChange : AbstractChange
    {
        private sealed class ProjectReferenceChange : ReferenceChange
        {
            private readonly ProjectReference _reference;
            private readonly string _projectReferenceName;

            public ProjectReferenceChange(ProjectReference reference, string projectReferenceName, ProjectId projectId, string projectName, bool isAdded, PreviewEngine engine)
                : base(projectId, projectName, isAdded, engine)
            {
                _reference = reference;
                _projectReferenceName = projectReferenceName;
            }

            internal override Solution AddToSolution(Solution solution)
                => solution.AddProjectReference(this.ProjectId, _reference);

            internal override Solution RemoveFromSolution(Solution solution)
                => solution.RemoveProjectReference(this.ProjectId, _reference);

            protected override string GetDisplayText()
                => string.Format(ServicesVSResources.Project_reference_to_0_in_project_1, _projectReferenceName, this.ProjectName);
        }
    }
}
