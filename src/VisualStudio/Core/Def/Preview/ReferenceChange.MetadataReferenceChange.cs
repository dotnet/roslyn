// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Preview;

internal abstract partial class ReferenceChange : AbstractChange
{
    private sealed class MetadataReferenceChange : ReferenceChange
    {
        private readonly MetadataReference _reference;

        public MetadataReferenceChange(MetadataReference reference, ProjectId projectId, string projectName, bool isAdded, PreviewEngine engine)
            : base(projectId, projectName, isAdded, engine)
        {
            _reference = reference;
        }

        internal override Solution AddToSolution(Solution solution)
            => solution.AddMetadataReference(this.ProjectId, _reference);

        internal override Solution RemoveFromSolution(Solution solution)
            => solution.RemoveMetadataReference(this.ProjectId, _reference);

        protected override string GetDisplayText()
        {
            var display = _reference.Display ?? ServicesVSResources.Unknown1;
            return string.Format(ServicesVSResources.Reference_to_0_in_project_1, display, this.ProjectName);
        }
    }
}
