// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.Workspace
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class WorkspacesNetCore : WorkspaceBase
    {
        public WorkspacesNetCore(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, WellKnownProjectTemplates.CSharpNetCoreClassLibrary)
        {
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn-project-system/issues/1825"), Trait(Traits.Feature, Traits.Features.Workspace)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override void OpenCSharpThenVBSolution()
        {
            base.OpenCSharpThenVBSolution();
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn-project-system/issues/1826"), Trait(Traits.Feature, Traits.Features.Workspace)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override void MetadataReference()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.EditProjectFile(project);
            VisualStudio.Editor.SetText(@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net461</TargetFramework>
  </PropertyGroup>
</Project>");
            VisualStudio.SolutionExplorer.SaveAll();
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudio.SolutionExplorer.OpenFile(project, "Class1.cs");
            base.MetadataReference();
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/19223"), Trait(Traits.Feature, Traits.Features.Workspace)]
        public override void ProjectReference()
        {
            base.ProjectReference();
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn-project-system/issues/1825"), Trait(Traits.Feature, Traits.Features.Workspace)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override void ProjectProperties()
        {
            VisualStudio.SolutionExplorer.CreateSolution(nameof(WorkspacesDesktop));
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddProject(project, WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic);
            base.ProjectProperties();
        }
    }
}
