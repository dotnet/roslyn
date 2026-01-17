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
using Microsoft.CodeAnalysis.ExternalAccess.HotReload.Api;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

[UseExportProvider]
public sealed class HotReloadServiceTests : EditAndContinueWorkspaceTestBase
{
    private static Task<SourceText> GetCommittedDocumentTextAsync(HotReloadService service, DocumentId documentId)
        => ((EditAndContinueService)service.GetTestAccessor().EncService)
           .GetTestAccessor()
           .GetActiveDebuggingSessions()
           .Single()
           .LastCommittedSolution
           .GetRequiredProject(documentId.ProjectId)
           .GetRequiredDocument(documentId)
           .GetTextAsync();

    [Fact]
    public async Task Test()
    {
        var source1 = "class C { void M() { System.Console.WriteLine(1); } }";
        var source2 = "class C { void M() { System.Console.WriteLine(2); /*2*/} }";
        var source3 = "class C { void M() { System.Console.WriteLine(2); /*3*/} }";
        var dir = Temp.CreateDirectory();
        var sourceFileA = dir.CreateFile("A.cs").WriteAllText(source1);

        using var workspace = CreateWorkspace(out var solution, out var encService);

        solution = solution.
            AddTestProject("P", out var projectId).
            AddTestDocument(source: null, sourceFileA.Path, out var documentIdA).Project.Solution;

        EmitLibrary(solution.GetRequiredProject(projectId));

        var hotReload = new HotReloadService(workspace.Services, ["Baseline", "AddDefinitionToExistingType", "NewTypeDefinition"]);

        await hotReload.StartSessionAsync(solution, CancellationToken.None);

        var sessionId = hotReload.GetTestAccessor().SessionId;
        var session = encService.GetTestAccessor().GetDebuggingSession(sessionId);
        Assert.Empty(session.LastCommittedSolution.Test_GetDocumentStates());

        // Valid update:
        solution = solution.WithDocumentText(documentIdA, CreateText(source2));

        var result = await hotReload.GetUpdatesAsync(solution, runningProjects: ImmutableDictionary<ProjectId, HotReloadService.RunningProjectInfo>.Empty, CancellationToken.None);
        Assert.Empty(result.PersistentDiagnostics);
        Assert.Empty(result.TransientDiagnostics);
        Assert.Equal(1, result.ProjectUpdates.Length);
        AssertEx.Equal([0x02000002], result.ProjectUpdates[0].UpdatedTypes);

        hotReload.CommitUpdate();

        var updatedText = await GetCommittedDocumentTextAsync(hotReload, documentIdA);
        Assert.Equal(source2, updatedText.ToString());

        // Insignificant change:
        solution = solution.WithDocumentText(documentIdA, CreateText(source3));

        result = await hotReload.GetUpdatesAsync(solution, runningProjects: ImmutableDictionary<ProjectId, HotReloadService.RunningProjectInfo>.Empty, CancellationToken.None);
        Assert.Empty(result.PersistentDiagnostics);
        Assert.Empty(result.TransientDiagnostics);
        Assert.Empty(result.ProjectUpdates);
        Assert.Equal(HotReloadService.Status.NoChangesToApply, result.Status);

        updatedText = await GetCommittedDocumentTextAsync(hotReload, documentIdA);
        Assert.Equal(source3, updatedText.ToString());

        // Rude edit:
        solution = solution.WithDocumentText(documentIdA, CreateText("class C { void M<T>() { System.Console.WriteLine(2); } }"));

        var runningProjects = ImmutableDictionary<ProjectId, HotReloadService.RunningProjectInfo>.Empty
            .Add(projectId, new HotReloadService.RunningProjectInfo() { RestartWhenChangesHaveNoEffect = true });

        result = await hotReload.GetUpdatesAsync(solution, runningProjects, CancellationToken.None);
        Assert.Empty(result.PersistentDiagnostics);
        AssertEx.Equal(
            [$"P: {sourceFileA.Path}: (0,17)-(0,18): Error ENC0110: {string.Format(FeaturesResources.Changing_the_signature_of_0_requires_restarting_the_application_because_it_is_not_supported_by_the_runtime, FeaturesResources.method)}"],
            InspectDiagnostics(result.TransientDiagnostics));
        Assert.Empty(result.ProjectUpdates);
        AssertEx.SetEqual(["P"], result.ProjectsToRestart.Select(p => solution.GetRequiredProject(p.Key).Name));
        AssertEx.SetEqual(["P"], result.ProjectsToRebuild.Select(p => solution.GetRequiredProject(p).Name));

        // Emulate the user making choice to not restart.
        // dotnet-watch then waits until Ctrl+R forces restart.
        hotReload.DiscardUpdate();

        updatedText = await GetCommittedDocumentTextAsync(hotReload, documentIdA);
        Assert.Equal(source3, updatedText.ToString());

        // Syntax error:
        solution = solution.WithDocumentText(documentIdA, CreateText("class C { void M() { System.Console.WriteLine(2)/* missing semicolon */ }"));

        result = await hotReload.GetUpdatesAsync(solution, runningProjects, CancellationToken.None);
        AssertEx.Equal(
            [$"{sourceFileA.Path}: (0,72)-(0,73): Error CS1002: {CSharpResources.ERR_SemicolonExpected}"],
            InspectDiagnostics(result.PersistentDiagnostics));
        Assert.Empty(result.ProjectUpdates);
        Assert.Empty(result.ProjectsToRestart);
        Assert.Empty(result.ProjectsToRebuild);

        updatedText = await GetCommittedDocumentTextAsync(hotReload, documentIdA);
        Assert.Equal(source3, updatedText.ToString());

        // Semantic diagnostics and no-effect edit:
        solution = solution.WithDocumentText(documentIdA, CreateText("class C { void M() { Unknown(); } static C() { int x = 1; } }"));

        result = await hotReload.GetUpdatesAsync(solution, runningProjects, CancellationToken.None);
        AssertEx.Equal(
        [
            $"{sourceFileA.Path}: (0,21)-(0,28): Error CS0103: {string.Format(CSharpResources.ERR_NameNotInContext, "Unknown")}",
            $"{sourceFileA.Path}: (0,51)-(0,52): Warning CS0219: {string.Format(CSharpResources.WRN_UnreferencedVarAssg, "x")}",
        ], InspectDiagnostics(result.PersistentDiagnostics));

        // TODO: https://github.com/dotnet/roslyn/issues/79017
        //AssertEx.Equal(
        //[
        //    $"P: {sourceFileA.Path}: (0,34)-(0,44): Warning ENC0118: {string.Format(FeaturesResources.Changing_0_might_not_have_any_effect_until_the_application_is_restarted, FeaturesResources.static_constructor)}",
        //], InspectDiagnostics(result.TransientDiagnostics));
        AssertEx.Empty(result.TransientDiagnostics);

        Assert.Empty(result.ProjectUpdates);
        Assert.Empty(result.ProjectsToRestart);
        Assert.Empty(result.ProjectsToRebuild);

        hotReload.EndSession();
    }

    [Fact]
    public async Task SourceGeneratorFailure()
    {
        using var workspace = CreateWorkspace(out var solution, out var encService);

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

                context.AddSource("generated.cs", CreateText("generated: " + additionalText));
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

        var hotReload = new HotReloadService(workspace.Services, ["Baseline", "AddDefinitionToExistingType", "NewTypeDefinition"]);

        await hotReload.StartSessionAsync(solution, CancellationToken.None);

        solution = solution.WithAdditionalDocumentText(aId, CreateText("updated text"));

        var runningProjects = ImmutableDictionary<ProjectId, HotReloadService.RunningProjectInfo>.Empty
            .Add(projectId, new HotReloadService.RunningProjectInfo() { RestartWhenChangesHaveNoEffect = false });

        var result = await hotReload.GetUpdatesAsync(solution, runningProjects, CancellationToken.None);
        var diagnostic = result.PersistentDiagnostics.Single();
        Assert.Equal("CS8785", diagnostic.Id);
        Assert.Contains("Source generator failed", diagnostic.GetMessage());
        hotReload.EndSession();
    }

    [Fact]
    public async Task AdditionalFile()
    {
        using var workspace = CreateWorkspace(out var solution, out _);

        var source1 = "class C;";
        var source2 = "class C { virtual void M() { } }";

        var dir = Temp.CreateDirectory();
        var additionalFileA = dir.CreateFile("A.txt").WriteAllText(source1);

        var generator = new TestSourceGenerator()
        {
            ExecuteImpl = context =>
            {
                var additionalText = context.AdditionalFiles.Single().GetText()!.ToString();
                context.AddSource("generated.cs", CreateText("/* generated */ " + additionalText));
            }
        };

        solution = solution
            .AddTestProject("A", out var projectId)
            .AddAdditionalTestDocument(source: null, additionalFileA.Path, out var documentIdA)
            .Project.Solution
            .AddAnalyzerReference(projectId, new TestGeneratorReference(generator));

        EmitLibrary(solution.GetRequiredProject(projectId));

        var hotReload = new HotReloadService(workspace.Services, ["Baseline", "AddDefinitionToExistingType"]);

        await hotReload.StartSessionAsync(solution, CancellationToken.None);

        // rude edit in the generated code:
        solution = solution.WithAdditionalDocumentText(documentIdA, CreateText(source2));

        var result = await hotReload.GetUpdatesAsync(solution, ImmutableDictionary<ProjectId, HotReloadService.RunningProjectInfo>.Empty, CancellationToken.None);

        var generatedDoc = (await solution.GetRequiredProject(projectId).GetSourceGeneratedDocumentsAsync()).Single();

        AssertEx.Equal(
            [$"A: {generatedDoc.FilePath}: (0,26)-(0,42): Error ENC0023: {string.Format(FeaturesResources.Adding_an_abstract_0_or_overriding_an_inherited_0_requires_restarting_the_application, FeaturesResources.method)}"],
            InspectDiagnostics(result.TransientDiagnostics));
    }

    [Fact]
    public async Task AnalyzerConfigFile()
    {
        using var workspace = CreateWorkspace(out var solution, out _);

        var source1 = "class C;";
        var source2 = "class C { virtual void M() { } }";

        var dir = Temp.CreateDirectory();
        var sourceFile = dir.CreateFile("empty.cs").WriteAllText("");
        var configFile = dir.CreateFile(".editorconfig").WriteAllText(source1);

        var generator = new TestSourceGenerator()
        {
            ExecuteImpl = context =>
            {
                var syntaxTree = context.Compilation.SyntaxTrees.Single(t => t.FilePath == sourceFile.Path);
                var content = context.AnalyzerConfigOptions.GetOptions(syntaxTree).TryGetValue("content", out var optionValue) ? optionValue.ToString() : "none";
                context.AddSource("generated.cs", CreateText("/* generated */ " + content));
            }
        };

        solution = solution
            .AddTestProject("A", out var projectId)
            .AddTestDocument(source: null, sourceFile.Path).Project
            .AddAnalyzerConfigTestDocument([("content", source1)], configFile.Path, out var configId).Project.Solution
            .AddAnalyzerReference(projectId, new TestGeneratorReference(generator));

        EmitLibrary(solution.GetRequiredProject(projectId));

        var hotReload = new HotReloadService(workspace.Services, ["Baseline", "AddDefinitionToExistingType"]);

        await hotReload.StartSessionAsync(solution, CancellationToken.None);

        // rude edit in the generated code:
        solution = solution.WithAnalyzerConfigDocumentText(configId, CreateText(Extensions.GetAnalyzerConfigSource([("content", source2)])));

        var result = await hotReload.GetUpdatesAsync(solution, ImmutableDictionary<ProjectId, HotReloadService.RunningProjectInfo>.Empty, CancellationToken.None);

        var generatedDoc = (await solution.GetRequiredProject(projectId).GetSourceGeneratedDocumentsAsync()).Single();

        AssertEx.Equal(
            [$"A: {generatedDoc.FilePath}: (0,26)-(0,42): Error ENC0023: {string.Format(FeaturesResources.Adding_an_abstract_0_or_overriding_an_inherited_0_requires_restarting_the_application, FeaturesResources.method)}"],
            InspectDiagnostics(result.TransientDiagnostics));
    }

    [Fact]
    public async Task StaleSource()
    {
        var source1 = "class C { void M() { System.Console.WriteLine(1); } }";
        var source2 = "class C { void M() { System.Console.WriteLine(2); } }";
        var source3 = "class C { void M() { System.Console.WriteLine(3); } }";
        var dir = Temp.CreateDirectory();
        var sourceFileA = dir.CreateFile("A.cs");

        using var workspace = CreateWorkspace(out var solution, out _);

        solution = solution.
            AddTestProject("P", out var projectId).
            AddTestDocument(source: null, sourceFileA.Path, out var documentIdA).Project.Solution;

        EmitLibrary(projectId, source1, sourceFileA.Path, assemblyName: "Proj");

        sourceFileA.WriteAllText(source2, Encoding.UTF8);

        var hotReload = new HotReloadService(workspace.Services, ["Baseline", "AddDefinitionToExistingType", "NewTypeDefinition"]);

        // loads source text V2 from disk:
        await hotReload.StartSessionAsync(solution, CancellationToken.None);

        // Out of sync:
        solution = solution.WithDocumentText(documentIdA, CreateText(source3));

        var result = await hotReload.GetUpdatesAsync(solution, runningProjects: ImmutableDictionary<ProjectId, HotReloadService.RunningProjectInfo>.Empty, CancellationToken.None);

        Assert.Empty(result.ProjectUpdates);
        Assert.Empty(result.ProjectsToRestart);
        Assert.Empty(result.ProjectsToRebuild);

        var message = string.Format(
            FeaturesResources.Changing_source_file_0_in_a_stale_project_1_has_no_effect_until_the_project_is_rebuilt_2,
            sourceFileA.Path,
            "P",
            FeaturesResources.the_content_of_the_document_is_stale);

        AssertEx.Equal(
        [
            $"P: {sourceFileA.Path}: (0,0)-(0,0): Warning ENC1008: {message}",
        ], InspectDiagnostics(result.TransientDiagnostics));
    }

    [Fact]
    public async Task StaleSource_AdditionalFile()
    {
        using var workspace = CreateWorkspace(out var solution, out _);

        var source1 = "class C { virtual void M() { } }";
        var source2 = "class C {}";
        var source3 = "class C { virtual void M() { } }";

        var dir = Temp.CreateDirectory();
        var additionalFileA = dir.CreateFile("A.txt").WriteAllText(source1);

        var generator = new TestSourceGenerator()
        {
            ExecuteImpl = context =>
            {
                var additionalText = context.AdditionalFiles.Single().GetText()!.ToString();
                context.AddSource("generated.cs", CreateText("/* generated */ " + additionalText));
            }
        };

        solution = solution
            .AddTestProject("A", out var projectId)
            .AddAdditionalTestDocument(source: null, additionalFileA.Path, out var documentIdA)
            .Project.Solution
            .AddAnalyzerReference(projectId, new TestGeneratorReference(generator));

        EmitLibrary(solution.GetRequiredProject(projectId));

        additionalFileA.WriteAllText(source2);

        var hotReload = new HotReloadService(workspace.Services, ["Baseline", "AddDefinitionToExistingType"]);

        // V2 of the text is loaded from disk:
        await hotReload.StartSessionAsync(solution, CancellationToken.None);

        // rude edit in the generated code:
        solution = solution.WithAdditionalDocumentText(documentIdA, CreateText(source3));

        // The generated file in the old compilation is regenerated based on V2.
        // The generated file in the new compilation is generated based on V3.
        var result = await hotReload.GetUpdatesAsync(solution, ImmutableDictionary<ProjectId, HotReloadService.RunningProjectInfo>.Empty, CancellationToken.None);

        var generatedDoc = (await solution.GetRequiredProject(projectId).GetSourceGeneratedDocumentsAsync()).Single();

        AssertEx.Equal(
            [$"A: {generatedDoc.FilePath}: (0,26)-(0,42): Error ENC0023: {string.Format(FeaturesResources.Adding_an_abstract_0_or_overriding_an_inherited_0_requires_restarting_the_application, FeaturesResources.method)}"],
            InspectDiagnostics(result.TransientDiagnostics));
    }
}
#endif
