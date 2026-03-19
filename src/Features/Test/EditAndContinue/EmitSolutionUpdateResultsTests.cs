// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

[UseExportProvider]
public sealed class EmitSolutionUpdateResultsTests
{
    private static TestWorkspace CreateWorkspace(out Solution solution)
    {
        var workspace = new TestWorkspace(composition: FeaturesTestCompositions.Features);
        solution = workspace.CurrentSolution;
        return workspace;
    }

    private static ManagedHotReloadUpdate CreateMockUpdate(ProjectId projectId)
        => new(
            projectId.Id,
            projectId.ToString(),
            projectId,
            ilDelta: [],
            metadataDelta: [],
            pdbDelta: [],
            updatedTypes: [],
            requiredCapabilities: [],
            updatedMethods: [],
            sequencePoints: [],
            activeStatements: [],
            exceptionRegions: []);

    private static ImmutableArray<ManagedHotReloadUpdate> CreateValidUpdates(params IEnumerable<ProjectId> projectIds)
        => [.. projectIds.Select(CreateMockUpdate)];

    private static ImmutableArray<ProjectDiagnostics> CreateProjectRudeEdits(IEnumerable<ProjectId> blocking, IEnumerable<ProjectId> noEffect)
        => [.. blocking.Select(id => (id, kind: RudeEditKind.InternalError)).Concat(noEffect.Select(id => (id, kind: RudeEditKind.UpdateMightNotHaveAnyEffect)))
            .GroupBy(e => e.id)
            .OrderBy(g => g.Key)
            .Select(g => new ProjectDiagnostics(g.Key, [.. g.Select(e => Diagnostic.Create(EditAndContinueDiagnosticDescriptors.GetDescriptor(e.kind), Location.None))]))];

    private static ImmutableDictionary<ProjectId, RunningProjectOptions> CreateRunningProjects(IEnumerable<(ProjectId id, bool noEffectRestarts)> projectIds)
        => projectIds.ToImmutableDictionary(keySelector: e => e.id, elementSelector: e => new RunningProjectOptions() { RestartWhenChangesHaveNoEffect = e.noEffectRestarts });

    private static IEnumerable<string> Inspect(ImmutableDictionary<ProjectId, ImmutableArray<ProjectId>> projectsToRestart)
        => projectsToRestart
            .OrderBy(kvp => kvp.Key.DebugName)
            .Select(kvp => $"{kvp.Key.DebugName}: [{string.Join(",", kvp.Value.Select(id => id.DebugName).Order())}]");

    [Fact]
    public async Task GetHotReloadDiagnostics()
    {
        using var workspace = new TestWorkspace(composition: FeaturesTestCompositions.Features);

        var sourcePath = Path.Combine(TempRoot.Root, "x", "a.cs");
        var razorPath1 = Path.Combine(TempRoot.Root, "x", "a.razor");
        var razorPath2 = Path.Combine(TempRoot.Root, "a.razor");

        var document = workspace.CurrentSolution.
            AddProject("proj", "proj", LanguageNames.CSharp).
            WithMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Standard)).
            AddDocument(sourcePath, SourceText.From("class C {}", Encoding.UTF8), filePath: Path.Combine(TempRoot.Root, sourcePath));

        var solution = document.Project.Solution;
        var tree = await document.GetRequiredSyntaxTreeAsync(CancellationToken.None);

        var diagnostics = ImmutableArray.Create(
            new DiagnosticData(
                id: "CS0001",
                category: "Test",
                message: "warning",
                severity: DiagnosticSeverity.Warning,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                warningLevel: 0,
                customTags: ["Test2"],
                properties: ImmutableDictionary<string, string?>.Empty,
                document.Project.Id,
                DiagnosticDataLocation.TestAccessor.Create(new(sourcePath, new(0, 0), new(0, 5)), document.Id, new("a.razor", new(10, 10), new(10, 15)), forceMappedPath: true),
                language: "C#",
                title: "title",
                description: "description",
                helpLink: "http://link"),
            new DiagnosticData(
                id: "CS0012",
                category: "Test",
                message: "error",
                severity: DiagnosticSeverity.Error,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                warningLevel: 0,
                customTags: ["Test2"],
                properties: ImmutableDictionary<string, string?>.Empty,
                document.Project.Id,
                DiagnosticDataLocation.TestAccessor.Create(new(sourcePath, new(0, 0), new(0, 5)), document.Id, new(@"..\a.razor", new(10, 10), new(10, 15)), forceMappedPath: true),
                language: "C#",
                title: "title",
                description: "description",
                helpLink: "http://link"));

        var syntaxError = new DiagnosticData(
            id: "CS0002",
            category: "Test",
            message: "syntax error",
            severity: DiagnosticSeverity.Error,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            warningLevel: 0,
            customTags: ["Test3"],
            properties: ImmutableDictionary<string, string?>.Empty,
            document.Project.Id,
            new DiagnosticDataLocation(new(sourcePath, new(0, 1), new(0, 5)), document.Id),
            language: "C#",
            title: "title",
            description: "description",
            helpLink: "http://link");

        var rudeEdits = ImmutableArray.Create(new ProjectDiagnostics(document.Project.Id,
        [
            new RudeEditDiagnostic(RudeEditKind.Insert, TextSpan.FromBounds(1, 10), 123, ["a"]).ToDiagnostic(tree),
            new RudeEditDiagnostic(RudeEditKind.Delete, TextSpan.FromBounds(1, 10), 123, ["b"]).ToDiagnostic(tree)
        ])).ToDiagnosticData(solution);

        var data = new EmitSolutionUpdateResults.Data()
        {
            Diagnostics = [.. diagnostics, .. rudeEdits],
            SyntaxError = syntaxError,
            ModuleUpdates = new ModuleUpdates(ModuleUpdateStatus.Blocked, Updates: []),
            ProjectsToRebuild = [],
            ProjectsToRestart = ImmutableDictionary<ProjectId, ImmutableArray<ProjectId>>.Empty,
            ProjectsToRedeploy = [],
        };

        var actual = data.GetAllDiagnostics();

        AssertEx.SetEqual(
        [
            $@"Warning CS0001: {razorPath1} (10,10)-(10,15): warning",
            $@"Error CS0012: {razorPath2} (10,10)-(10,15): error",
            $@"Error CS0002: {sourcePath} (0,1)-(0,5): syntax error",
            $@"RestartRequired ENC0021: {sourcePath} (0,1)-(0,10): {string.Format(FeaturesResources.Adding_0_requires_restarting_the_application, "a")}",
            $@"RestartRequired ENC0033: {sourcePath} (0,1)-(0,10): {string.Format(FeaturesResources.Deleting_0_requires_restarting_the_application, "b")}",
        ], actual.Select(d => $"{d.Severity} {d.Id}: {d.FilePath} {d.Span.GetDebuggerDisplay()}: {d.Message}"));
    }

    [Fact]
    public void RunningProjects_Updates()
    {
        using var _ = CreateWorkspace(out var solution);

        solution = solution
            .AddTestProject("C", out var c).Solution
            .AddTestProject("D", out var d).Solution
            .AddTestProject("A", out var a).AddProjectReferences([new(c)]).Solution
            .AddTestProject("B", out var b).AddProjectReferences([new(c), new(d)]).Solution;

        EmitSolutionUpdateResults.GetProjectsToRebuildAndRestart(
            solution,
            CreateValidUpdates(c, d),
            CreateProjectRudeEdits(blocking: [], noEffect: []),
            addedUnbuiltProjects: [],
            CreateRunningProjects([(a, noEffectRestarts: false), (b, noEffectRestarts: false)]),
            out var projectsToRestart,
            out var projectsToRebuild);

        Assert.Empty(projectsToRestart);
        Assert.Empty(projectsToRebuild);
    }

    [Fact]
    public void RunningProjects_RudeEdits_SingleImpactedRunningProject()
    {
        using var _ = CreateWorkspace(out var solution);

        solution = solution
            .AddTestProject("C", out var c).Solution
            .AddTestProject("D", out var d).Solution
            .AddTestProject("A", out var a).AddProjectReferences([new(c)]).Solution
            .AddTestProject("B", out var b).AddProjectReferences([new(c), new(d)]).Solution;

        EmitSolutionUpdateResults.GetProjectsToRebuildAndRestart(
            solution,
            CreateValidUpdates(),
            CreateProjectRudeEdits(blocking: [d], noEffect: []),
            addedUnbuiltProjects: [],
            CreateRunningProjects([(a, noEffectRestarts: false), (b, noEffectRestarts: false)]),
            out var projectsToRestart,
            out var projectsToRebuild);

        // D has rude edit ==> B has to restart
        AssertEx.Equal(["B: [D]"], Inspect(projectsToRestart));

        AssertEx.SetEqual([b], projectsToRebuild);
    }

    [Fact]
    public void RunningProjects_RudeEdits_MultipleImpactedRunningProjects()
    {
        using var _ = CreateWorkspace(out var solution);

        solution = solution
            .AddTestProject("C", out var c).Solution
            .AddTestProject("D", out var d).Solution
            .AddTestProject("A", out var a).AddProjectReferences([new(c)]).Solution
            .AddTestProject("B", out var b).AddProjectReferences([new(c), new(d)]).Solution;

        EmitSolutionUpdateResults.GetProjectsToRebuildAndRestart(
            solution,
            CreateValidUpdates(),
            CreateProjectRudeEdits(blocking: [c], noEffect: []),
            addedUnbuiltProjects: [],
            CreateRunningProjects([(a, noEffectRestarts: true), (b, noEffectRestarts: false)]),
            out var projectsToRestart,
            out var projectsToRebuild);

        // C has rude edit
        // ==> A, B have to restart:
        AssertEx.Equal(
        [
            "A: [C]",
            "B: [C]",
        ], Inspect(projectsToRestart));

        AssertEx.SetEqual([a, b], projectsToRebuild);
    }

    [Fact]
    public void RunningProjects_RudeEdits_NotImpactingRunningProjects()
    {
        using var _ = CreateWorkspace(out var solution);

        solution = solution
            .AddTestProject("C", out var c).Solution
            .AddTestProject("D", out var d).Solution
            .AddTestProject("A", out var a).AddProjectReferences([new(c)]).Solution
            .AddTestProject("B", out var b).AddProjectReferences([new(c), new(d)]).Solution;

        EmitSolutionUpdateResults.GetProjectsToRebuildAndRestart(
            solution,
            CreateValidUpdates(),
            CreateProjectRudeEdits(blocking: [d], noEffect: []),
            addedUnbuiltProjects: [],
            CreateRunningProjects([(a, noEffectRestarts: false)]),
            out var projectsToRestart,
            out var projectsToRebuild);

        Assert.Empty(projectsToRestart);

        // Rude edit in projects that doesn't affect running project still causes the updated project to be rebuilt,
        // so that the change takes effect if it is loaded in future.
        AssertEx.SetEqual([d], projectsToRebuild);
    }

    [Fact]
    public void RunningProjects_NoEffectEdits_NoEffectRestarts()
    {
        using var _ = CreateWorkspace(out var solution);

        solution = solution
            .AddTestProject("C", out var c).Solution
            .AddTestProject("D", out var d).Solution
            .AddTestProject("A", out var a).AddProjectReferences([new(c)]).Solution
            .AddTestProject("B", out var b).AddProjectReferences([new(c), new(d)]).Solution;

        EmitSolutionUpdateResults.GetProjectsToRebuildAndRestart(
            solution,
            CreateValidUpdates(c),
            CreateProjectRudeEdits(blocking: [], noEffect: [c]),
            addedUnbuiltProjects: [],
            CreateRunningProjects([(a, noEffectRestarts: false), (b, noEffectRestarts: true)]),
            out var projectsToRestart,
            out var projectsToRebuild);

        // C has no-effect edit
        // B restarts on no effect changes
        // A restarts on blocking changes
        // ==> B has to restart
        // ==> A has to restart as well since B is restarting and C has an update
        AssertEx.Equal(
        [
            "A: [C]",
            "B: [C]",
        ], Inspect(projectsToRestart));

        AssertEx.SetEqual([a, b], projectsToRebuild);
    }

    [Fact]
    public void RunningProjects_NoEffectEdits_BlockingRestartsOnly()
    {
        using var _ = CreateWorkspace(out var solution);

        solution = solution
            .AddTestProject("C", out var c).Solution
            .AddTestProject("D", out var d).Solution
            .AddTestProject("A", out var a).AddProjectReferences([new(c)]).Solution
            .AddTestProject("B", out var b).AddProjectReferences([new(c), new(d)]).Solution;

        EmitSolutionUpdateResults.GetProjectsToRebuildAndRestart(
            solution,
            CreateValidUpdates(c),
            CreateProjectRudeEdits(blocking: [], noEffect: [c]),
            addedUnbuiltProjects: [],
            CreateRunningProjects([(a, noEffectRestarts: false), (b, noEffectRestarts: false)]),
            out var projectsToRestart,
            out var projectsToRebuild);

        // C has no-effect edit
        // B restarts on blocking changes
        // A restarts on blocking changes
        // ==> no restarts/rebuild
        Assert.Empty(projectsToRestart);
        Assert.Empty(projectsToRebuild);
    }

    [Fact]
    public void RunningProjects_NoEffectEdits_NoImpactedRunningProject()
    {
        using var _ = CreateWorkspace(out var solution);

        solution = solution
            .AddTestProject("C", out var c).Solution
            .AddTestProject("D", out var d).Solution
            .AddTestProject("A", out var a).AddProjectReferences([new(c)]).Solution
            .AddTestProject("B", out var b).AddProjectReferences([new(c), new(d)]).Solution;

        EmitSolutionUpdateResults.GetProjectsToRebuildAndRestart(
            solution,
            CreateValidUpdates(d),
            CreateProjectRudeEdits(blocking: [], noEffect: [d]),
            addedUnbuiltProjects: [],
            CreateRunningProjects([(a, noEffectRestarts: false)]),
            out var projectsToRestart,
            out var projectsToRebuild);

        Assert.Empty(projectsToRestart);
        Assert.Empty(projectsToRebuild);
    }

    [Fact]
    public void RunningProjects_NoEffectEditAndRudeEdit_SameProject()
    {
        using var _ = CreateWorkspace(out var solution);

        solution = solution
            .AddTestProject("C", out var c).Solution
            .AddTestProject("D", out var d).Solution
            .AddTestProject("A", out var a).AddProjectReferences([new(c)]).Solution
            .AddTestProject("B", out var b).AddProjectReferences([new(c), new(d)]).Solution;

        EmitSolutionUpdateResults.GetProjectsToRebuildAndRestart(
            solution,
            CreateValidUpdates(),
            CreateProjectRudeEdits(blocking: [c], noEffect: [c]),
            addedUnbuiltProjects: [],
            CreateRunningProjects([(a, noEffectRestarts: false), (b, noEffectRestarts: false)]),
            out var projectsToRestart,
            out var projectsToRebuild);

        // C has rude edit
        // ==> A, B have to restart
        AssertEx.Equal(
        [
            "A: [C]",
            "B: [C]",
        ], Inspect(projectsToRestart));

        AssertEx.SetEqual([a, b], projectsToRebuild);
    }

    [Fact]
    public void RunningProjects_NoEffectEditAndRudeEdit_DifferentProjects()
    {
        using var _ = CreateWorkspace(out var solution);

        solution = solution
            .AddTestProject("Q", out var q).Solution
            .AddTestProject("P0", out var p0).AddProjectReferences([new(q)]).Solution
            .AddTestProject("P1", out var p1).AddProjectReferences([new(q)]).Solution
            .AddTestProject("P2", out var p2).Solution
            .AddTestProject("R0", out var r0).AddProjectReferences([new(p0)]).Solution
            .AddTestProject("R1", out var r1).AddProjectReferences([new(p1), new(p0)]).Solution
            .AddTestProject("R2", out var r2).AddProjectReferences([new(p2), new(p0)]).Solution;

        EmitSolutionUpdateResults.GetProjectsToRebuildAndRestart(
            solution,
            CreateValidUpdates(p0, q),
            CreateProjectRudeEdits(blocking: [p1, p2], noEffect: [q]),
            addedUnbuiltProjects: [],
            CreateRunningProjects([(r0, noEffectRestarts: false), (r1, noEffectRestarts: false), (r2, noEffectRestarts: false)]),
            out var projectsToRestart,
            out var projectsToRebuild);

        // P1, P2 have rude edits
        // ==> R1, R2 have to restart
        // P0 has no-effect edit, but R0, R1, R2 do not restart on no-effect edits
        // P0 has update, R1 -> P0, R2 -> P0, R1 and R2 are restarting due to rude edits in P1 and P2
        // ==> R0 has to restart due to rude edits in P1 and P2
        // Q has update
        // ==> R0 has to restart due to rude edits in P1 and P2
        AssertEx.Equal(
        [
            "R0: [P1,P2]",
            "R1: [P1]",
            "R2: [P2]",
        ], Inspect(projectsToRestart));

        AssertEx.SetEqual([r0, r1, r2], projectsToRebuild);
    }

    [Fact]
    public void RunningProjects_RudeEditAndUpdate_DependentOnRunningProject()
    {
        using var _ = CreateWorkspace(out var solution);

        solution = solution
            .AddTestProject("C", out var c).Solution
            .AddTestProject("D", out var d).Solution
            .AddTestProject("A", out var a).AddProjectReferences([new(c)]).Solution
            .AddTestProject("B", out var b).AddProjectReferences([new(c), new(d)]).Solution;

        EmitSolutionUpdateResults.GetProjectsToRebuildAndRestart(
            solution,
            CreateValidUpdates(c),
            CreateProjectRudeEdits(blocking: [d], noEffect: []),
            addedUnbuiltProjects: [],
            CreateRunningProjects([(a, noEffectRestarts: false), (b, noEffectRestarts: false)]),
            out var projectsToRestart,
            out var projectsToRebuild);

        // D has rude edit => B has to restart
        // C has update, B -> C and A -> C ==> A has to restart
        AssertEx.Equal(
        [
            "A: [D]",
            "B: [D]",
        ], Inspect(projectsToRestart));

        AssertEx.SetEqual([a, b], projectsToRebuild);
    }

    [Fact]
    public void RunningProjects_RudeEditAndUpdate_DependentOnRebuiltProject()
    {
        using var _ = CreateWorkspace(out var solution);

        solution = solution
            .AddTestProject("C", out var c).Solution
            .AddTestProject("D", out var d).Solution
            .AddTestProject("A", out var a).AddProjectReferences([new(c)]).Solution
            .AddTestProject("B", out var b).AddProjectReferences([new(c), new(d)]).Solution;

        EmitSolutionUpdateResults.GetProjectsToRebuildAndRestart(
            solution,
            CreateValidUpdates(c),
            CreateProjectRudeEdits(blocking: [b], noEffect: []),
            addedUnbuiltProjects: [],
            CreateRunningProjects([(a, noEffectRestarts: false)]),
            out var projectsToRestart,
            out var projectsToRebuild);

        // B has rude edit ==> B has to rebuild
        // B -> C and C has change ==> C has to rebuild
        // A -> C and A is running ==> A has to restart
        AssertEx.Equal(
        [
            "A: [B]",
        ], Inspect(projectsToRestart));

        AssertEx.SetEqual([a, b], projectsToRebuild);
    }

    [Fact]
    public void RunningProjects_AddedProject_NotImpactingRunningProject()
    {
        using var _ = CreateWorkspace(out var solution);

        solution = solution
            .AddTestProject("C", out var c).Solution
            .AddTestProject("D", out var d).Solution
            .AddTestProject("A", out var a).AddProjectReferences([new(c)]).Solution
            .AddTestProject("B", out var b).AddProjectReferences([new(c), new(d)]).Solution;

        EmitSolutionUpdateResults.GetProjectsToRebuildAndRestart(
            solution,
            CreateValidUpdates(c),
            CreateProjectRudeEdits(blocking: [], noEffect: []),
            addedUnbuiltProjects: [b],
            CreateRunningProjects([(a, noEffectRestarts: false)]),
            out var projectsToRestart,
            out var projectsToRebuild);

        // B isn't built, but doesn't impact a running project ==> does not need to be rebuilt
        // B will be considered stale until rebuilt.
        Assert.Empty(projectsToRestart);
        Assert.Empty(projectsToRebuild);
    }

    [Fact]
    public void RunningProjects_AddedProject_ImpactingRunningProject()
    {
        using var _ = CreateWorkspace(out var solution);

        solution = solution
            .AddTestProject("C", out var c).Solution
            .AddTestProject("D", out var d).Solution
            .AddTestProject("A", out var a).AddProjectReferences([new(c)]).Solution
            .AddTestProject("B", out var b).AddProjectReferences([new(c), new(d)]).Solution
            .AddTestProject("E", out var e).AddProjectReferences([new(b)]).Solution;

        EmitSolutionUpdateResults.GetProjectsToRebuildAndRestart(
            solution,
            CreateValidUpdates(c),
            CreateProjectRudeEdits(blocking: [], noEffect: []),
            addedUnbuiltProjects: [b],
            CreateRunningProjects([(a, noEffectRestarts: false), (e, noEffectRestarts: false)]),
            out var projectsToRestart,
            out var projectsToRebuild);

        // B isn't built ==> B has to rebuild
        // B -> C and C has change ==> C has to rebuild
        // A -> C and A is running ==> A has to restart
        AssertEx.Equal(
        [
            "A: [B]",
            "E: [B]",
        ], Inspect(projectsToRestart));

        AssertEx.SetEqual([a, b, e], projectsToRebuild);
    }

    [Fact]
    public void RunningProjects_RudeEditAndUpdate_Independent()
    {
        using var _ = CreateWorkspace(out var solution);

        solution = solution
            .AddTestProject("C", out var c).Solution
            .AddTestProject("D", out var d).Solution
            .AddTestProject("A", out var a).AddProjectReferences([new(c)]).Solution
            .AddTestProject("B", out var b).AddProjectReferences([new(d)]).Solution;

        EmitSolutionUpdateResults.GetProjectsToRebuildAndRestart(
            solution,
            CreateValidUpdates(c),
            CreateProjectRudeEdits(blocking: [d], noEffect: []),
            addedUnbuiltProjects: [],
            CreateRunningProjects([(a, noEffectRestarts: false), (b, noEffectRestarts: false)]),
            out var projectsToRestart,
            out var projectsToRebuild);

        // D has rude edit => B has to restart
        AssertEx.Equal(["B: [D]"], Inspect(projectsToRestart));
        AssertEx.SetEqual([b], projectsToRebuild);
    }

    [Fact]
    public void RunningProjects_NoEffectEditAndUpdate()
    {
        using var _ = CreateWorkspace(out var solution);

        solution = solution
            .AddTestProject("C", out var c).Solution
            .AddTestProject("D", out var d).Solution
            .AddTestProject("A", out var a).AddProjectReferences([new(c)]).Solution
            .AddTestProject("B", out var b).AddProjectReferences([new(c), new(d)]).Solution;

        EmitSolutionUpdateResults.GetProjectsToRebuildAndRestart(
            solution,
            CreateValidUpdates(c, d),
            CreateProjectRudeEdits(blocking: [], noEffect: [d]),
            addedUnbuiltProjects: [],
            CreateRunningProjects([(a, noEffectRestarts: false), (b, noEffectRestarts: true)]),
            out var projectsToRestart,
            out var projectsToRebuild);

        // D has no-effect edit
        // ==> B has to restart
        // C has update, A -> C, B -> C, B restarting
        // ==> A has to restart even though it does not restart on no-effect edits
        AssertEx.Equal(
        [
            "A: [D]",
            "B: [D]",
        ], Inspect(projectsToRestart));

        AssertEx.SetEqual([a, b], projectsToRebuild);
    }

    [Theory]
    [CombinatorialData]
    public void RunningProjects_RudeEditAndUpdate_Chain(bool reverse)
    {
        using var _ = CreateWorkspace(out var solution);

        solution = solution
            .AddTestProject("P1", out var p1).Solution
            .AddTestProject("P2", out var p2).Solution
            .AddTestProject("P3", out var p3).Solution
            .AddTestProject("P4", out var p4).Solution
            .AddTestProject("R1", out var r1).AddProjectReferences([new(p1), new(p2)]).Solution
            .AddTestProject("R2", out var r2).AddProjectReferences([new(p2), new(p3)]).Solution
            .AddTestProject("R3", out var r3).AddProjectReferences([new(p3), new(p4)]).Solution
            .AddTestProject("R4", out var r4).AddProjectReferences([new(p4)]).Solution;

        EmitSolutionUpdateResults.GetProjectsToRebuildAndRestart(
            solution,
            CreateValidUpdates(reverse ? [p4, p3, p2] : [p2, p3, p4]),
            CreateProjectRudeEdits(blocking: [p1], noEffect: []),
            addedUnbuiltProjects: [],
            CreateRunningProjects([(r1, noEffectRestarts: false), (r2, noEffectRestarts: false), (r3, noEffectRestarts: false), (r4, noEffectRestarts: false)]),
            out var projectsToRestart,
            out var projectsToRebuild);

        AssertEx.Equal(
        [
            "R1: [P1]",
            "R2: [P1]",
            "R3: [P1]",
            "R4: [P1]",
        ], Inspect(projectsToRestart));

        AssertEx.SetEqual([r1, r2, r3, r4], projectsToRebuild);
    }
}
