// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

[UseExportProvider]
public sealed class UnitTestingHotReloadServiceTests : EditAndContinueWorkspaceTestBase
{
    [Fact]
    public async Task Test()
    {
        var source1 = "class C { void M() { System.Console.WriteLine(1); } }";
        var source2 = "class C { void M() { System.Console.WriteLine(2); } }";
        var source3 = "class C { void M<T>() { System.Console.WriteLine(2); } }";
        var source4 = "class C { void M() { System.Console.WriteLine(2)/* missing semicolon */ }";

        var dir = Temp.CreateDirectory();
        var sourceFileA = dir.CreateFile("A.cs").WriteAllText(source1, Encoding.UTF8);
        var moduleId = EmitLibrary(source1, sourceFileA.Path, assemblyName: "Proj");

        using var workspace = CreateWorkspace(out var solution, out var encService);

        var projectP = solution.
            AddTestProject("P").
            WithMetadataReferences(TargetFrameworkUtil.GetReferences(DefaultTargetFramework));

        solution = projectP.Solution;

        var documentIdA = DocumentId.CreateNewId(projectP.Id, debugName: "A");
        solution = solution.AddDocument(DocumentInfo.Create(
            id: documentIdA,
            name: "A",
            loader: new WorkspaceFileTextLoader(solution.Services, sourceFileA.Path, Encoding.UTF8),
            filePath: sourceFileA.Path));

        var hotReload = new UnitTestingHotReloadService(workspace.Services);

        await hotReload.StartSessionAsync(solution, ["Baseline", "AddDefinitionToExistingType", "NewTypeDefinition"], CancellationToken.None);

        var sessionId = hotReload.GetTestAccessor().SessionId;
        var session = encService.GetTestAccessor().GetDebuggingSession(sessionId);
        var matchingDocuments = session.LastCommittedSolution.Test_GetDocumentStates();
        AssertEx.Equal(
        [
            "(A, MatchesBuildOutput)"
        ], matchingDocuments.Select(e => (solution.GetDocument(e.id).Name, e.state)).OrderBy(e => e.Name).Select(e => e.ToString()));

        // Valid change
        solution = solution.WithDocumentText(documentIdA, CreateText(source2));

        var result = await hotReload.EmitSolutionUpdateAsync(solution, commitUpdates: true, CancellationToken.None);
        Assert.Empty(result.diagnostics);
        Assert.Equal(1, result.updates.Length);

        solution = solution.WithDocumentText(documentIdA, CreateText(source3));

        // Rude edit
        result = await hotReload.EmitSolutionUpdateAsync(solution, commitUpdates: true, CancellationToken.None);
        AssertEx.Equal(
            new[] { "ENC0110: " + string.Format(FeaturesResources.Changing_the_signature_of_0_requires_restarting_the_application_because_it_is_not_supported_by_the_runtime, FeaturesResources.method) },
            result.diagnostics.Select(d => $"{d.Id}: {d.GetMessage()}"));

        Assert.Empty(result.updates);

        // Syntax error is reported in the diagnostics:
        solution = solution.WithDocumentText(documentIdA, CreateText(source4));

        result = await hotReload.EmitSolutionUpdateAsync(solution, commitUpdates: true, CancellationToken.None);
        Assert.Equal(1, result.diagnostics.Length);
        Assert.Empty(result.updates);

        hotReload.EndSession();
    }
}
