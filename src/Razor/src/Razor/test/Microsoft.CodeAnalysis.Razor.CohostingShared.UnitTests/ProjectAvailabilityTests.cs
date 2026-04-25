// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Remote.Razor;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.VisualStudio.Razor.LanguageClient.Cohost;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Tooltip;

public class ProjectAvailabilityTests(ITestOutputHelper testOutput) : CohostEndpointTestBase(testOutput)
{
    [Fact]
    public async Task GetProjectAvailabilityText_NoProjects_ReturnsNull()
    {
        var document = CreateProjectAndRazorDocument("");

        var availability = await GetAvailabilityAsync("MyTagHelper", document.Project.Solution);

        Assert.Null(availability);
    }

    [Fact]
    public async Task GetProjectAvailabilityText_OneProject_ReturnsNull()
    {
        var document = CreateProjectAndRazorDocument("",
            additionalFiles: [
                (FilePath("Component.razor"), "")
            ]);

        var availability = await GetAvailabilityAsync("SomeProject.Component", document.Project.Solution);

        Assert.Null(availability);
    }

    [Fact]
    public async Task GetProjectAvailabilityText_AvailableInAllProjects_ReturnsNull()
    {
        var solution = LocalWorkspace.CurrentSolution;
        var projectId = ProjectId.CreateNewId();
        solution = AddProjectAndRazorDocument(solution, TestProjectData.SomeProject.FilePath, projectId, DocumentId.CreateNewId(projectId), FilePath("Component.razor"), "@namespace SomeProject").Project.Solution;

        projectId = ProjectId.CreateNewId();
        solution = AddProjectAndRazorDocument(solution, TestProjectData.AnotherProject.FilePath, projectId, DocumentId.CreateNewId(projectId), FilePath("Component.razor"), "@namespace SomeProject").Project.Solution;

        var availability = await GetAvailabilityAsync("SomeProject.Component", solution);

        Assert.Null(availability);
    }

    [Fact]
    public async Task GetProjectAvailabilityText_NotAvailableInAllProjects_ReturnsText()
    {
        var solution = LocalWorkspace.CurrentSolution;
        var projectId = ProjectId.CreateNewId();
        solution = AddProjectAndRazorDocument(solution, TestProjectData.SomeProject.FilePath, projectId, DocumentId.CreateNewId(projectId), FilePath("Component.razor"), "").Project.Solution;

        projectId = ProjectId.CreateNewId();
        solution = AddProjectAndRazorDocument(solution, TestProjectData.AnotherProject.FilePath, projectId, DocumentId.CreateNewId(projectId), FilePath("Component.razor"), "",
            additionalFiles: [(FilePath("OtherComponent.razor"), "@namespace AnotherProject")]).Project.Solution;

        var availability = await GetAvailabilityAsync("AnotherProject.OtherComponent", solution);

        AssertEx.EqualOrDiff("""

            ⚠️ Not available in:
                SomeProject
            """, availability);
    }

    [Fact]
    public async Task GetProjectAvailabilityText_NotAvailableInAnyProject_ReturnsText()
    {
        var solution = LocalWorkspace.CurrentSolution;
        var projectId = ProjectId.CreateNewId();
        solution = AddProjectAndRazorDocument(solution, TestProjectData.SomeProject.FilePath, projectId, DocumentId.CreateNewId(projectId), FilePath("Component.razor"), "").Project.Solution;

        projectId = ProjectId.CreateNewId();
        solution = AddProjectAndRazorDocument(solution, TestProjectData.AnotherProject.FilePath, projectId, DocumentId.CreateNewId(projectId), FilePath("Component.razor"), "").Project.Solution;

        var availability = await GetAvailabilityAsync("SomeProject.OtherComponent", solution);

        AssertEx.EqualOrDiff("""

            ⚠️ Not available in:
                AnotherProject
                SomeProject
            """, availability);
    }

    private async Task<string?> GetAvailabilityAsync(string componentTypeName, Solution solution)
    {
        var snapshotManager = OOPExportProvider.GetExportedValue<RemoteSnapshotManager>();
        var solutionSnapshot = snapshotManager.GetSnapshot(solution);

        var componentAvailabilityService = new ComponentAvailabilityService(solutionSnapshot);

        return await componentAvailabilityService.GetProjectAvailabilityTextAsync(FilePath("Component.razor"), componentTypeName, DisposalToken);
    }
}
