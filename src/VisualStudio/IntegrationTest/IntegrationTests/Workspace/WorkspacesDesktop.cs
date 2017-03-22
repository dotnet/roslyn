// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.Workspace
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class WorkspacesDesktop : WorkspaceBase
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public WorkspacesDesktop(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
            VisualStudio.Instance.SolutionExplorer.CreateSolution(nameof(WorkspacesDesktop));
            VisualStudio.Instance.SolutionExplorer.AddProject(ProjectName, WellKnownProjectTemplates.ClassLibrary, LanguageName);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void OpenCSharpThenVBSolution()
        {
            OpenCSharpThenVBSolutionCommon();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void MetadataReference()
        {
            MetadataReferenceCommon();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void ProjectReference()
        {
            ProjectReferenceCommon(WellKnownProjectTemplates.ClassLibrary);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void ProjectProperties()
        {
            VisualStudio.Instance.SolutionExplorer.CreateSolution(nameof(WorkspacesDesktop));
            VisualStudio.Instance.SolutionExplorer.AddProject(ProjectName, WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic);
            ProjectPropertiesCommon();
        }
    }
}
