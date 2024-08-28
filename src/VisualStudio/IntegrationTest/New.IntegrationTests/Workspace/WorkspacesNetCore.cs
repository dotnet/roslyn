// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.Workspaces;

[Trait(Traits.Feature, Traits.Features.NetCore)]
[Trait(Traits.Feature, Traits.Features.Workspace)]
public class WorkspacesNetCore : WorkspaceBase
{
    public WorkspacesNetCore()
        : base(WellKnownProjectTemplates.CSharpNetCoreClassLibrary)
    {
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/72018")]
    [Trait(Traits.Feature, Traits.Features.Workspace)]
    [Trait(Traits.Feature, Traits.Features.NetCore)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/34264")]
    public override async Task MetadataReference()
    {
        await TestServices.SolutionExplorer.CreateSolutionAsync(nameof(WorkspacesNetCore), HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddCustomProjectAsync(ProjectName, ".csproj", @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
  </PropertyGroup>
</Project>", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "Class1.cs", contents: string.Empty, open: true, cancellationToken: HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.RestoreNuGetPackagesAsync(HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace], HangMitigatingCancellationToken);
        await TestServices.Workspace.SetFullSolutionAnalysisAsync(true, HangMitigatingCancellationToken);

        await base.MetadataReference();
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/30599")]
    [Trait(Traits.Feature, Traits.Features.Workspace)]
    [Trait(Traits.Feature, Traits.Features.NetCore)]
    public override async Task RenamingOpenFiles()
    {
        await InitializeWithDefaultSolution();
        await base.RenamingOpenFiles();
    }
}

