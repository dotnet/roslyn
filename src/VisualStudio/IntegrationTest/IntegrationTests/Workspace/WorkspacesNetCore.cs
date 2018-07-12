// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.Workspace
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class WorkspacesNetCore : WorkspaceBase
    {
        public WorkspacesNetCore()
            : base(WellKnownProjectTemplates.CSharpNetCoreClassLibrary)
        {
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Workspace)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override async Task OpenCSharpThenVBSolutionAsync()
        {
            await base.OpenCSharpThenVBSolutionAsync();
        }

        [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/28360")]
        [Trait(Traits.Feature, Traits.Features.Workspace)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override async Task MetadataReferenceAsync()
        {
            await VisualStudio.SolutionExplorer.EditProjectFileAsync(ProjectName);
            await VisualStudio.Editor.SetTextAsync(@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net461</TargetFramework>
  </PropertyGroup>
</Project>");
            await VisualStudio.SolutionExplorer.SaveAllAsync();
            await VisualStudio.SolutionExplorer.RestoreNuGetPackagesAsync(ProjectName);
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.cs");
            await base.MetadataReferenceAsync();
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public override async Task ProjectReferenceAsync()
        {
            await base.ProjectReferenceAsync();
        }

        [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/28361")]
        [Trait(Traits.Feature, Traits.Features.Workspace)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override async Task ProjectPropertiesAsync()
        {
            await VisualStudio.SolutionExplorer.CreateSolutionAsync(nameof(WorkspacesDesktop));
            await VisualStudio.SolutionExplorer.AddProjectAsync(ProjectName, WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic);
            await VisualStudio.SolutionExplorer.RestoreNuGetPackagesAsync(ProjectName);
            await base.ProjectPropertiesAsync();
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Workspace)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override async Task RenamingOpenFilesAsync()
        {
            await base.RenamingOpenFilesAsync();
        }
    }
}
