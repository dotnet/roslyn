// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    [UseExportProvider]
    public class EmitSolutionUpdateResultsTests
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

        private static EmitSolutionUpdateResults CreateMockResults(IEnumerable<ProjectId> updates, IEnumerable<ProjectId> rudeEdits)
        => new()
        {
            ModuleUpdates = new ModuleUpdates(ModuleUpdateStatus.Blocked, [.. updates.Select(CreateMockUpdate)]),
            RudeEdits = [.. rudeEdits.Select(id => (DocumentId.CreateNewId(id), ImmutableArray.Create(new RudeEditDiagnostic(RudeEditKind.InternalError, span: default))))],
            Diagnostics = [],
            SyntaxError = null,
        };

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

            var diagnosticData = ImmutableArray.Create(
                new DiagnosticData(
                    id: "CS0001",
                    category: "Test",
                    message: "warning",
                    severity: DiagnosticSeverity.Warning,
                    defaultSeverity: DiagnosticSeverity.Warning,
                    isEnabledByDefault: true,
                    warningLevel: 0,
                    customTags: ImmutableArray.Create("Test2"),
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
                    customTags: ImmutableArray.Create("Test2"),
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
                customTags: ImmutableArray.Create("Test3"),
                properties: ImmutableDictionary<string, string?>.Empty,
                document.Project.Id,
                new DiagnosticDataLocation(new(sourcePath, new(0, 1), new(0, 5)), document.Id),
                language: "C#",
                title: "title",
                description: "description",
                helpLink: "http://link");

            var rudeEdits = ImmutableArray.Create(
                (document.Id, ImmutableArray.Create(new RudeEditDiagnostic(RudeEditKind.Insert, TextSpan.FromBounds(1, 10), 123, ["a"]))),
                (document.Id, ImmutableArray.Create(new RudeEditDiagnostic(RudeEditKind.Delete, TextSpan.FromBounds(1, 10), 123, ["b"]))));

            var updateStatus = ModuleUpdateStatus.Blocked;
            var actual = await EmitSolutionUpdateResults.GetAllDiagnosticsAsync(solution, diagnosticData, rudeEdits, syntaxError, updateStatus, CancellationToken.None);

            AssertEx.Equal(
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

            var runningProjects = new[] { a, b };
            var results = CreateMockResults(updates: [c, d], rudeEdits: []);

            var projectsToRestart = new HashSet<Project>();
            var projectsToRebuild = new HashSet<Project>();

            results.GetProjectsToRebuildAndRestart(solution, p => runningProjects.Contains(p.Id), projectsToRestart, projectsToRebuild);

            Assert.Empty(projectsToRestart);
            Assert.Empty(projectsToRebuild);
        }

        [Fact]
        public void RunningProjects_RudeEdits()
        {
            using var _ = CreateWorkspace(out var solution);

            solution = solution
                .AddTestProject("C", out var c).Solution
                .AddTestProject("D", out var d).Solution
                .AddTestProject("A", out var a).AddProjectReferences([new(c)]).Solution
                .AddTestProject("B", out var b).AddProjectReferences([new(c), new(d)]).Solution;

            var runningProjects = new[] { a, b };
            var results = CreateMockResults(updates: [], rudeEdits: [d]);

            var projectsToRestart = new HashSet<Project>();
            var projectsToRebuild = new HashSet<Project>();

            results.GetProjectsToRebuildAndRestart(solution, p => runningProjects.Contains(p.Id), projectsToRestart, projectsToRebuild);

            // D has rude edit ==> B has to restart
            AssertEx.SetEqual([b], projectsToRestart.Select(p => p.Id));

            // D has rude edit:
            AssertEx.SetEqual([d], projectsToRebuild.Select(p => p.Id));
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

            var runningProjects = new[] { a };
            var results = CreateMockResults(updates: [], rudeEdits: [d]);

            var projectsToRestart = new HashSet<Project>();
            var projectsToRebuild = new HashSet<Project>();

            results.GetProjectsToRebuildAndRestart(solution, p => runningProjects.Contains(p.Id), projectsToRestart, projectsToRebuild);

            Assert.Empty(projectsToRestart);
            Assert.Empty(projectsToRebuild);
        }

        [Fact]
        public void RunningProjects_RudeEditAndUpdate_Dependent()
        {
            using var _ = CreateWorkspace(out var solution);

            solution = solution
                .AddTestProject("C", out var c).Solution
                .AddTestProject("D", out var d).Solution
                .AddTestProject("A", out var a).AddProjectReferences([new(c)]).Solution
                .AddTestProject("B", out var b).AddProjectReferences([new(c), new(d)]).Solution;

            var runningProjects = new[] { a, b };
            var results = CreateMockResults(updates: [c], rudeEdits: [d]);

            var projectsToRestart = new HashSet<Project>();
            var projectsToRebuild = new HashSet<Project>();

            results.GetProjectsToRebuildAndRestart(solution, p => runningProjects.Contains(p.Id), projectsToRestart, projectsToRebuild);

            // D has rude edit => B has to restart
            // C has update, B -> C and A -> C ==> A has to restart
            AssertEx.SetEqual([a, b], projectsToRestart.Select(p => p.Id));

            // D has rude edit, C has update that impacts restart set:
            AssertEx.SetEqual([c, d], projectsToRebuild.Select(p => p.Id));
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

            var runningProjects = new[] { a, b };
            var results = CreateMockResults(updates: [c], rudeEdits: [d]);

            var projectsToRestart = new HashSet<Project>();
            var projectsToRebuild = new HashSet<Project>();

            results.GetProjectsToRebuildAndRestart(solution, p => runningProjects.Contains(p.Id), projectsToRestart, projectsToRebuild);

            // D has rude edit => B has to restart
            AssertEx.SetEqual([b], projectsToRestart.Select(p => p.Id));

            // D has rude edit, C has update that does not impacts restart set:
            AssertEx.SetEqual([d], projectsToRebuild.Select(p => p.Id));
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

            var runningProjects = new[] { r1, r2, r3, r4 };
            var results = CreateMockResults(updates: reverse ? [p4, p3, p2] : [p2, p3, p4], rudeEdits: [p1]);

            var projectsToRestart = new HashSet<Project>();
            var projectsToRebuild = new HashSet<Project>();

            results.GetProjectsToRebuildAndRestart(solution, p => runningProjects.Contains(p.Id), projectsToRestart, projectsToRebuild);

            AssertEx.SetEqual([r1, r2, r3, r4], projectsToRestart.Select(p => p.Id));
            AssertEx.SetEqual([p1, p2, p3, p4], projectsToRebuild.Select(p => p.Id));
        }
    }
}
