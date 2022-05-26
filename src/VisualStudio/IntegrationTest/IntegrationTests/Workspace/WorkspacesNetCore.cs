// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
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
        public WorkspacesNetCore(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, WellKnownProjectTemplates.CSharpNetCoreClassLibrary)
        {
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.Workspace)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override void OpenCSharpThenVBSolution()
        {
            // The CSharpNetCoreClassLibrary template does not open a file automatically.
            VisualStudio.SolutionExplorer.OpenFile(new ProjectUtils.Project(ProjectName), WellKnownProjectTemplates.CSharpNetCoreClassLibraryClassFileName);
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
    <TargetFramework>net46</TargetFramework>
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

        [WpfFact]
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
