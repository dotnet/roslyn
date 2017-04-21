// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            {
                return solution.AddProjectReference(this.ProjectId, _reference);
            }

            internal override Solution RemoveFromSolution(Solution solution)
            {
                return solution.RemoveProjectReference(this.ProjectId, _reference);
            }

            protected override string GetDisplayText()
            {
                return string.Format(ServicesVSResources.Project_reference_to_0_in_project_1, _projectReferenceName, this.ProjectName);
            }
        }
    }
}
