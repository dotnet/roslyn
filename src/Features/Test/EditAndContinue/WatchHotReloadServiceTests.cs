// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExternalAccess.Watch.Api;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

[UseExportProvider]
public sealed class WatchHotReloadServiceTests : EditAndContinueWorkspaceTestBase
{
    [Fact]
    public async Task Test()
    {
        // See https://github.com/dotnet/sdk/blob/main/src/BuiltInTools/dotnet-watch/HotReload/CompilationHandler.cs#L125

        var source1 = "class C { void M() { System.Console.WriteLine(1); } }";
        var source2 = "class C { void M() { System.Console.WriteLine(2); } }";
        var source3 = "class C { void M<T>() { System.Console.WriteLine(2); } }";
        var source4 = "class C { void M() { System.Console.WriteLine(2)/* missing semicolon */ }";

        var dir = Temp.CreateDirectory();
        var sourceFileA = dir.CreateFile("A.cs").WriteAllText(source1, Encoding.UTF8);
        var moduleId = EmitLibrary(source1, sourceFileA.Path, assemblyName: "Proj");

        using var workspace = CreateWorkspace(out var solution, out var encService);

        var projectId = ProjectId.CreateNewId();
        var projectP = solution.
            AddProject(ProjectInfo.Create(projectId, VersionStamp.Create(), "P", "P", LanguageNames.CSharp, parseOptions: CSharpParseOptions.Default.WithNoRefSafetyRulesAttribute())).GetProject(projectId).
            WithMetadataReferences(TargetFrameworkUtil.GetReferences(DefaultTargetFramework));

        solution = projectP.Solution;

        var documentIdA = DocumentId.CreateNewId(projectP.Id, debugName: "A");
        solution = solution.AddDocument(DocumentInfo.Create(
            id: documentIdA,
            name: "A",
            loader: new WorkspaceFileTextLoader(solution.Services, sourceFileA.Path, Encoding.UTF8),
            filePath: sourceFileA.Path));

        var hotReload = new WatchHotReloadService(workspace.Services, ImmutableArray.Create("Baseline", "AddDefinitionToExistingType", "NewTypeDefinition"));

        await hotReload.StartSessionAsync(solution, CancellationToken.None);

        var sessionId = hotReload.GetTestAccessor().SessionId;
        var session = encService.GetTestAccessor().GetDebuggingSession(sessionId);
        var matchingDocuments = session.LastCommittedSolution.Test_GetDocumentStates();
        AssertEx.Equal(new[]
        {
            "(A, MatchesBuildOutput)"
        }, matchingDocuments.Select(e => (solution.GetDocument(e.id).Name, e.state)).OrderBy(e => e.Name).Select(e => e.ToString()));

        // Valid update:
        solution = solution.WithDocumentText(documentIdA, CreateText(source2));

        var result = await hotReload.EmitSolutionUpdateAsync(solution, CancellationToken.None);
        Assert.Empty(result.diagnostics);
        Assert.Equal(1, result.updates.Length);
        AssertEx.Equal(new[] { 0x02000002 }, result.updates[0].UpdatedTypes);

        // Rude edit:
        solution = solution.WithDocumentText(documentIdA, CreateText(source3));

        result = await hotReload.EmitSolutionUpdateAsync(solution, CancellationToken.None);
        AssertEx.Equal(
            new[] { "ENC0110: " + string.Format(FeaturesResources.Changing_the_signature_of_0_requires_restarting_the_application_because_it_is_not_supported_by_the_runtime, FeaturesResources.method) },
            result.diagnostics.Select(d => $"{d.Id}: {d.GetMessage()}"));

        Assert.Empty(result.updates);

        // Syntax error (not reported in diagnostics):
        solution = solution.WithDocumentText(documentIdA, CreateText(source4));

        result = await hotReload.EmitSolutionUpdateAsync(solution, CancellationToken.None);
        Assert.Empty(result.diagnostics);
        Assert.Empty(result.updates);

        hotReload.EndSession();
    }
}
