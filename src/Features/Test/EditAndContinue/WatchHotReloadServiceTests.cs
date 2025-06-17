// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExternalAccess.Watch.Api;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

[UseExportProvider]
public sealed class WatchHotReloadServiceTests : EditAndContinueWorkspaceTestBase
{
    [Theory]
    [CombinatorialData]
    public async Task Test(bool requireCommit)
    {
        // See https://github.com/dotnet/sdk/blob/main/src/BuiltInTools/dotnet-watch/HotReload/CompilationHandler.cs#L125

        // Note that xUnit does not run test case of a theory in parallel, so we can set global state here:
        WatchHotReloadService.RequireCommit = requireCommit;

        var source1 = "class C { void M() { System.Console.WriteLine(1); } }";
        var source2 = "class C { void M() { System.Console.WriteLine(2); /*2*/} }";
        var source3 = "class C { void M() { System.Console.WriteLine(2); /*3*/} }";
        var source4 = "class C { void M<T>() { System.Console.WriteLine(2); } }";
        var source5 = "class C { void M() { System.Console.WriteLine(2)/* missing semicolon */ }";
        var source6 = "class C { void M() { Unknown(); } }";

        var dir = Temp.CreateDirectory();
        var sourceFileA = dir.CreateFile("A.cs").WriteAllText(source1, Encoding.UTF8);

        using var workspace = CreateWorkspace(out var solution, out var encService);

        var projectP = solution.
            AddTestProject("P", out var projectId);

        solution = projectP.Solution;

        var moduleId = EmitLibrary(projectP.Id, source1, sourceFileA.Path, assemblyName: "Proj");

        var documentIdA = DocumentId.CreateNewId(projectId, debugName: "A");
        solution = solution.AddDocument(DocumentInfo.Create(
            id: documentIdA,
            name: "A",
            loader: new WorkspaceFileTextLoader(solution.Services, sourceFileA.Path, Encoding.UTF8),
            filePath: sourceFileA.Path));

        var hotReload = new WatchHotReloadService(workspace.Services, ["Baseline", "AddDefinitionToExistingType", "NewTypeDefinition"]);

        await hotReload.StartSessionAsync(solution, CancellationToken.None);

        var sessionId = hotReload.GetTestAccessor().SessionId;
        var session = encService.GetTestAccessor().GetDebuggingSession(sessionId);
        var matchingDocuments = session.LastCommittedSolution.Test_GetDocumentStates();
        AssertEx.Equal(
        [
            "(A, MatchesBuildOutput)"
        ], matchingDocuments.Select(e => (solution.GetRequiredDocument(e.id).Name, e.state)).OrderBy(e => e.Name).Select(e => e.ToString()));

        // Valid update:
        solution = solution.WithDocumentText(documentIdA, CreateText(source2));

        var result = await hotReload.GetUpdatesAsync(solution, runningProjects: ImmutableDictionary<ProjectId, WatchHotReloadService.RunningProjectInfo>.Empty, CancellationToken.None);
        Assert.Empty(result.CompilationDiagnostics);
        Assert.Equal(1, result.ProjectUpdates.Length);
        AssertEx.Equal([0x02000002], result.ProjectUpdates[0].UpdatedTypes);

        if (requireCommit)
        {
            hotReload.CommitUpdate();
        }

        // Insignificant change:
        solution = solution.WithDocumentText(documentIdA, CreateText(source3));

        result = await hotReload.GetUpdatesAsync(solution, runningProjects: ImmutableDictionary<ProjectId, WatchHotReloadService.RunningProjectInfo>.Empty, CancellationToken.None);
        Assert.Empty(result.CompilationDiagnostics);
        Assert.Empty(result.CompilationDiagnostics);
        Assert.Empty(result.ProjectUpdates);
        Assert.Equal(WatchHotReloadService.Status.NoChangesToApply, result.Status);

        var updatedText = await ((EditAndContinueService)hotReload.GetTestAccessor().EncService)
            .GetTestAccessor()
            .GetActiveDebuggingSessions()
            .Single()
            .LastCommittedSolution
            .GetRequiredProject(documentIdA.ProjectId)
            .GetRequiredDocument(documentIdA)
            .GetTextAsync();

        Assert.Equal(source3, updatedText.ToString());

        // Rude edit:
        solution = solution.WithDocumentText(documentIdA, CreateText(source4));

        var runningProjects = ImmutableDictionary<ProjectId, WatchHotReloadService.RunningProjectInfo>.Empty
            .Add(projectId, new WatchHotReloadService.RunningProjectInfo() { RestartWhenChangesHaveNoEffect = false });

        result = await hotReload.GetUpdatesAsync(solution, runningProjects, CancellationToken.None);
        AssertEx.Equal(
            ["P: ENC0110: " + string.Format(FeaturesResources.Changing_the_signature_of_0_requires_restarting_the_application_because_it_is_not_supported_by_the_runtime, FeaturesResources.method)],
            result.RudeEdits.SelectMany(re => re.diagnostics.Select(d => $"{re.project.DebugName}: {d.Id}: {d.GetMessage()}")));
        Assert.Empty(result.ProjectUpdates);
        AssertEx.SetEqual(["P"], result.ProjectsToRestart.Select(p => solution.GetRequiredProject(p.Key).Name));
        AssertEx.SetEqual(["P"], result.ProjectsToRebuild.Select(p => solution.GetRequiredProject(p).Name));

        if (requireCommit)
        {
            // Emulate the user making choice to not restart.
            // dotnet-watch then waits until Ctrl+R forces restart.
            hotReload.DiscardUpdate();
        }

        // Syntax error:
        solution = solution.WithDocumentText(documentIdA, CreateText(source5));

        result = await hotReload.GetUpdatesAsync(solution, runningProjects, CancellationToken.None);
        AssertEx.Equal(
            ["CS1002: " + CSharpResources.ERR_SemicolonExpected],
            result.CompilationDiagnostics.Select(d => $"{d.Id}: {d.GetMessage()}"));
        Assert.Empty(result.ProjectUpdates);
        Assert.Empty(result.ProjectsToRestart);
        Assert.Empty(result.ProjectsToRebuild);

        // Semantic error:
        solution = solution.WithDocumentText(documentIdA, CreateText(source6));

        result = await hotReload.GetUpdatesAsync(solution, runningProjects, CancellationToken.None);
        AssertEx.Equal(
            ["CS0103: " + string.Format(CSharpResources.ERR_NameNotInContext, "Unknown")],
            result.CompilationDiagnostics.Select(d => $"{d.Id}: {d.GetMessage()}"));
        Assert.Empty(result.ProjectUpdates);
        Assert.Empty(result.ProjectsToRestart);
        Assert.Empty(result.ProjectsToRebuild);

        hotReload.EndSession();
    }

    [Fact]
    public async Task SourceGeneratorFailure()
    {
        using var workspace = CreateWorkspace(out var solution, out _);

        var generatorExecutionCount = 0;
        var generator = new TestSourceGenerator()
        {
            ExecuteImpl = context =>
            {
                generatorExecutionCount++;

                var additionalText = context.AdditionalFiles.Single().GetText()!.ToString();
                if (additionalText.Contains("updated"))
                {
                    throw new InvalidOperationException("Source generator failed");
                }

                context.AddSource("generated.cs", SourceText.From("generated: " + additionalText, Encoding.UTF8, SourceHashAlgorithm.Sha256));
            }
        };

        var project = solution
            .AddTestProject("A")
            .AddAdditionalDocument("A.txt", "text", filePath: Path.Combine(TempRoot.Root, "A.txt"))
            .Project;

        var projectId = project.Id;
        solution = project.Solution.AddAnalyzerReference(projectId, new TestGeneratorReference(generator));
        project = solution.GetRequiredProject(projectId);
        var aId = project.AdditionalDocumentIds.Single();

        var generatedDocuments = await project.Solution.CompilationState.GetSourceGeneratedDocumentStatesAsync(project.State, CancellationToken.None);

        var generatedText = generatedDocuments.States.Single().Value.GetTextSynchronously(CancellationToken.None).ToString();
        AssertEx.AreEqual("generated: text", generatedText);
        Assert.Equal(1, generatorExecutionCount);

        var generatorDiagnostics = await solution.CompilationState.GetSourceGeneratorDiagnosticsAsync(project.State, CancellationToken.None);
        Assert.Empty(generatorDiagnostics);

        var hotReload = new WatchHotReloadService(workspace.Services, ["Baseline", "AddDefinitionToExistingType", "NewTypeDefinition"]);

        await hotReload.StartSessionAsync(solution, CancellationToken.None);

        solution = solution.WithAdditionalDocumentText(aId, CreateText("updated text"));

        var runningProjects = ImmutableDictionary<ProjectId, WatchHotReloadService.RunningProjectInfo>.Empty
            .Add(projectId, new WatchHotReloadService.RunningProjectInfo() { RestartWhenChangesHaveNoEffect = false });

        var result = await hotReload.GetUpdatesAsync(solution, runningProjects, CancellationToken.None);
        var diagnostic = result.CompilationDiagnostics.Single();
        Assert.Equal("CS8785", diagnostic.Id);
        Assert.Contains("Source generator failed", diagnostic.GetMessage());
        hotReload.EndSession();
    }

    [Theory]
    [CombinatorialData]
    public async Task ManifestResourceUpdateOrDelete(bool isDelete)
    {
        using var workspace = CreateWorkspace(out var solution, out _);

        var dir = Temp.CreateDirectory();

        var source = "class C;";
        var sourceFile = dir.CreateFile("A.cs").WriteAllText(source, Encoding.UTF8);
        var resourceFile = dir.CreateFile("A.resources").WriteAllBytes(new byte[] { 1, 2, 3 });

        var resourceInfo = new MetadataResourceInfo("resource", resourceFile.Path, linkedResourceFileName: null, isPublic: true, contentVersion: 0);

        solution = solution
            .AddTestProject("A", out var projectId)
            .AddTestDocument(source, sourceFile.Path).Project
            .WithManifestResources(resourceInfo).Solution;

        EmitLibrary(solution.GetRequiredProject(projectId));

        var hotReload = new WatchHotReloadService(workspace.Services, ["Baseline", "AddDefinitionToExistingType", "NewTypeDefinition"]);

        await hotReload.StartSessionAsync(solution, CancellationToken.None);

        solution = WatchHotReloadService.WithManifestResourceChanged(solution, resourceFile.Path, isDelete);

        if (isDelete)
        {
            Assert.Empty(solution.GetRequiredProject(projectId).State.Attributes.ManifestResources);
        }
        else
        {
            Assert.Equal(1, solution.GetRequiredProject(projectId).State.Attributes.ManifestResources.Single().ContentVersion);
        }

        var runningProjects = ImmutableDictionary<ProjectId, WatchHotReloadService.RunningProjectInfo>.Empty
            .Add(projectId, new WatchHotReloadService.RunningProjectInfo() { RestartWhenChangesHaveNoEffect = true });

        var result = await hotReload.GetUpdatesAsync(solution, runningProjects, CancellationToken.None);
        Assert.Empty(result.CompilationDiagnostics);
        AssertEx.SequenceEqual([isDelete ? "ENC1009" : "ENC1010"], result.RudeEdits.Single().diagnostics.Select(d => d.Id));
        AssertEx.SequenceEqual(["A"], result.ProjectsToRestart.Select(p => p.Key.DebugName));
        hotReload.EndSession();
    }
}
#endif
