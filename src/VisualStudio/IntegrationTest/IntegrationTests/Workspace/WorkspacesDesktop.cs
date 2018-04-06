// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.Workspace
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class WorkspacesDesktop : WorkspaceBase
    {
        public WorkspacesDesktop(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, WellKnownProjectTemplates.ClassLibrary)
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public override void OpenCSharpThenVBSolution()
        {
            base.OpenCSharpThenVBSolution();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public override void MetadataReference()
        {
            base.MetadataReference();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public override void ProjectReference()
        {
            base.ProjectReference();
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/19914"), Trait(Traits.Feature, Traits.Features.Workspace)]
        public override void ProjectProperties()
        {
            VisualStudio.SolutionExplorer.CreateSolution(nameof(WorkspacesDesktop));
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddProject(project, WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic);
            base.ProjectProperties();
        }
    }
}
