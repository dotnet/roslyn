// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Preview
{
    internal abstract partial class ReferenceChange : AbstractChange
    {
        private sealed class AnalyzerReferenceChange : ReferenceChange
        {
            private readonly AnalyzerReference _reference;

            public AnalyzerReferenceChange(AnalyzerReference reference, ProjectId projectId, string projectName, bool isAdded, PreviewEngine engine)
                : base(projectId, projectName, isAdded, engine)
            {
                _reference = reference;
            }

            internal override Solution AddToSolution(Solution solution)
            {
                return solution.AddAnalyzerReference(this.ProjectId, _reference);
            }

            internal override Solution RemoveFromSolution(Solution solution)
            {
                return solution.RemoveAnalyzerReference(this.ProjectId, _reference);
            }

            protected override string GetDisplayText()
            {
                var display = _reference.Display ?? ServicesVSResources.Unknown1;
                return string.Format(ServicesVSResources.Analyzer_reference_to_0_in_project_1, display, this.ProjectName);
            }
        }
    }
}
