// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.Workspace
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class WorkspacesNetCore : WorkspaceBase
    {
        public WorkspacesNetCore(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper, WellKnownProjectTemplates.CSharpNetCoreClassLibrary)
        {
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/39588")]
        [Trait(Traits.Feature, Traits.Features.Workspace)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override void OpenCSharpThenVBSolution()
        {
            base.OpenCSharpThenVBSolution();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Workspace)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        [WorkItem(34264, "https://github.com/dotnet/roslyn/issues/34264")]
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
            VisualStudio.SolutionExplorer.RestoreNuGetPackages(project);
            // 🐛 This should only need WaitForAsyncOperations for FeatureAttribute.Workspace
            // https://github.com/dotnet/roslyn/issues/34264
            VisualStudio.Workspace.WaitForAllAsyncOperations(Helper.HangMitigatingTimeout);
            VisualStudio.SolutionExplorer.OpenFile(project, "Class1.cs");
            base.MetadataReference();
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/39588")]
        [Trait(Traits.Feature, Traits.Features.Workspace)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]

        public override void ProjectReference()
        {
            base.ProjectReference();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Workspace)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override void ProjectProperties()
        {
            VisualStudio.SolutionExplorer.CreateSolution(nameof(WorkspacesDesktop));
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddProject(project, WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic);
            VisualStudio.SolutionExplorer.RestoreNuGetPackages(project);
            base.ProjectProperties();
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/30599")]
        [Trait(Traits.Feature, Traits.Features.Workspace)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override void RenamingOpenFilesViaDTE()
        {
            base.RenamingOpenFilesViaDTE();
        }
    }
}
