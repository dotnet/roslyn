// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.EditAndContinue;

[UseExportProvider]
public class RunningProjectOptionsTests
{
    private static IEnumerable<string> Inspect(ImmutableDictionary<ProjectId, RunningProjectOptions> options)
        => options.OrderBy(entry => entry.Key.DebugName).Select(entry => $"{entry.Key.DebugName}: {entry.Value.RestartWhenChangesHaveNoEffect}");

    [Fact]
    public void ToRunningProjectOptions()
    {
        using var workspace = new AdhocWorkspace();
        var projectAId = ProjectId.CreateNewId("ProjectA");
        var projectNoPathId = ProjectId.CreateNewId("NoPath");
        var projectB8Id = ProjectId.CreateNewId("ProjectB8");
        var projectB9Id = ProjectId.CreateNewId("ProjectB9");
        var projectCId = ProjectId.CreateNewId("ProjectC");

        var projectPathA = "ProjectA.csproj";
        var projectPathB = "ProjectB.csproj";
        var projectPathC = "ProjectC.csproj";

        var solution = workspace.CurrentSolution
            // no path
            .AddProject(ProjectInfo.Create(projectNoPathId, VersionStamp.Default, "ProjectNoPath", "ProjectNoPath", LanguageNames.CSharp, filePath: null))
            // single target
            .AddProject(ProjectInfo.Create(projectAId, VersionStamp.Default, "ProjectA", "ProjectA", LanguageNames.CSharp, filePath: projectPathA))
            // single target
            .AddProject(ProjectInfo.Create(projectCId, VersionStamp.Default, "ProjectC", "ProjectC", LanguageNames.CSharp, filePath: projectPathC))
            // multi-target
            .AddProject(ProjectInfo.Create(projectB8Id, VersionStamp.Default, "ProjectB (net8.0)", "ProjectB", LanguageNames.CSharp, filePath: projectPathB))
            .AddProject(ProjectInfo.Create(projectB9Id, VersionStamp.Default, "ProjectB (net9.0)", "ProjectB", LanguageNames.CSharp, filePath: projectPathB));

        var runningProjects = ImmutableArray.Create(
            (projectPathA, targetFramework: "", restartAutomatically: false),
            (projectPathB, targetFramework: "net8.0", restartAutomatically: false),
            (projectPathC, targetFramework: "net10.0", restartAutomatically: true),
            (projectPathB, targetFramework: "net9.0", restartAutomatically: true));

        var options = runningProjects.ToRunningProjectOptions(solution, static info => info);

        AssertEx.Equal(
        [
            "ProjectA: False",
            "ProjectB8: False",
            "ProjectB9: True",
            "ProjectC: True",
        ], Inspect(options));
    }

    [Fact]
    public void ToRunningProjectOptions_TargetFrameworkDoesNotMatch()
    {
        using var workspace = new AdhocWorkspace();
        var projectPath = "Project.csproj";
        var projectId = ProjectId.CreateNewId("Project8");
        var solution = workspace.CurrentSolution.AddProject(
            ProjectInfo.Create(projectId, VersionStamp.Default, "Project (net8.0)", "Project", LanguageNames.CSharp, filePath: projectPath));

        var runningProjects = ImmutableArray.Create((projectPath, targetFramework: "net9.0", restartAutomatically: true));
        var options = runningProjects.ToRunningProjectOptions(solution, static info => info);

        Assert.Empty(options);
    }
}
