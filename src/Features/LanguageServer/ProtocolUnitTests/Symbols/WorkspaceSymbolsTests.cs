// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.NavigateTo;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Symbols;

public class WorkspaceSymbolsTests(ITestOutputHelper testOutputHelper)
    : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    private static void AssertSetEquals(LSP.SymbolInformation[] expected, LSP.SymbolInformation[]? results)
        => Assert.True(expected.ToHashSet().SetEquals(results));

    private Task<TestLspServer> CreateTestLspServerAsync(string markup, bool mutatingLspWorkspace)
        => CreateTestLspServerAsync(markup, mutatingLspWorkspace, extraExportedTypes: [typeof(TestWorkspaceNavigateToSearchHostService)]);

    private Task<TestLspServer> CreateVisualBasicTestLspServerAsync(string markup, bool mutatingLspWorkspace)
        => CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace, extraExportedTypes: [typeof(TestWorkspaceNavigateToSearchHostService)]);

    private Task<TestLspServer> CreateTestLspServerAsync(string[] markups, bool mutatingLspWorkspace)
        => CreateTestLspServerAsync(markups, mutatingLspWorkspace, extraExportedTypes: [typeof(TestWorkspaceNavigateToSearchHostService)]);

    [Theory, CombinatorialData]
    public async Task TestGetWorkspaceSymbolsAsync_Class(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class {|class:A|}
            {
                void M()
                {
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var expected = new LSP.SymbolInformation[]
        {
            CreateSymbolInformation(LSP.SymbolKind.Class, "A", testLspServer.GetLocations("class").Single(), Glyph.ClassInternal, GetContainerName(testLspServer.GetCurrentSolution()))
        };

        var results = await RunGetWorkspaceSymbolsAsync(testLspServer, "A").ConfigureAwait(false);
        AssertSetEquals(expected, results);
    }

    [Theory, CombinatorialData]
    public async Task TestGetWorkspaceSymbolsAsync_Class_Streaming(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class {|class:A|}
            {
                void M()
                {
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var expected = new LSP.SymbolInformation[]
        {
            CreateSymbolInformation(LSP.SymbolKind.Class, "A", testLspServer.GetLocations("class").Single(), Glyph.ClassInternal, GetContainerName(testLspServer.GetCurrentSolution()))
        };

        using var progress = BufferedProgress.Create<LSP.SymbolInformation[]>(null);

        var results = await RunGetWorkspaceSymbolsAsync(testLspServer, "A", progress).ConfigureAwait(false);

        Assert.Null(results);

        results = progress.GetFlattenedValues();
        AssertSetEquals(expected, results);
    }

    [Theory, CombinatorialData]
    public async Task TestGetWorkspaceSymbolsAsync_Method(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
                void {|method:M|}()
                {
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var expected = new LSP.SymbolInformation[]
        {
            CreateSymbolInformation(LSP.SymbolKind.Method, "M", testLspServer.GetLocations("method").Single(), Glyph.MethodPrivate, GetContainerName(testLspServer.GetCurrentSolution(), "A"))
        };

        var results = await RunGetWorkspaceSymbolsAsync(testLspServer, "M").ConfigureAwait(false);
        AssertSetEquals(expected, results);
    }

    [Theory(Skip = "GetWorkspaceSymbolsAsync does not yet support locals."), CombinatorialData]
    // TODO - Remove skip & modify once GetWorkspaceSymbolsAsync is updated to support all symbols.
    // https://github.com/dotnet/roslyn/projects/45#card-20033822
    public async Task TestGetWorkspaceSymbolsAsync_Local(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
                void M()
                {
                    int {|local:i|} = 1;
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var expected = new LSP.SymbolInformation[]
        {
            CreateSymbolInformation(LSP.SymbolKind.Variable, "i", testLspServer.GetLocations("local").Single(), Glyph.Local, GetContainerName(testLspServer.GetCurrentSolution(), "A.M.i"))
        };

        var results = await RunGetWorkspaceSymbolsAsync(testLspServer, "i").ConfigureAwait(false);
        AssertSetEquals(expected, results);
    }

    [Theory, CombinatorialData]
    public async Task TestGetWorkspaceSymbolsAsync_MultipleKinds(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
                int {|field:F|};
                void M()
                {
                }
                class {|class:F|}
                {
                    int {|field:F|};
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var classAContainerName = GetContainerName(testLspServer.GetCurrentSolution(), "A");
        var expected = new LSP.SymbolInformation[]
        {
            CreateSymbolInformation(LSP.SymbolKind.Field, "F", testLspServer.GetLocations("field")[0], Glyph.FieldPrivate, classAContainerName),
            CreateSymbolInformation(LSP.SymbolKind.Class, "F", testLspServer.GetLocations("class").Single(), Glyph.ClassPrivate, classAContainerName),
            CreateSymbolInformation(LSP.SymbolKind.Field, "F", testLspServer.GetLocations("field")[1], Glyph.FieldPrivate, GetContainerName(testLspServer.GetCurrentSolution(), "A.F"))
        };

        var results = await RunGetWorkspaceSymbolsAsync(testLspServer, "F").ConfigureAwait(false);
        AssertSetEquals(expected, results);
    }

    [Theory, CombinatorialData]
    public async Task TestGetWorkspaceSymbolsAsync_MultipleDocuments(bool mutatingLspWorkspace)
    {
        var markups = new string[]
        {
            """
            class A
            {
                void {|method:M|}()
                {
                }
            }
            """,
            """
            class B
            {
                void {|method:M|}()
                {
                }
            }
            """
        };

        await using var testLspServer = await CreateTestLspServerAsync(markups, mutatingLspWorkspace);
        var expected = new LSP.SymbolInformation[]
        {
            CreateSymbolInformation(LSP.SymbolKind.Method, "M", testLspServer.GetLocations("method")[0], Glyph.MethodPrivate, GetContainerName(testLspServer.GetCurrentSolution(), "A")),
            CreateSymbolInformation(LSP.SymbolKind.Method, "M", testLspServer.GetLocations("method")[1], Glyph.MethodPrivate, GetContainerName(testLspServer.GetCurrentSolution(), "B"))
        };

        var results = await RunGetWorkspaceSymbolsAsync(testLspServer, "M").ConfigureAwait(false);
        AssertSetEquals(expected, results);
    }

    [Theory, CombinatorialData]
    public async Task TestGetWorkspaceSymbolsAsync_NoSymbols(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
                void M()
                {
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGetWorkspaceSymbolsAsync(testLspServer, "NonExistingSymbol").ConfigureAwait(false);
        Assert.Empty(results);
    }

    [Theory, CombinatorialData]
    public async Task TestGetWorkspaceSymbolsAsync_VisualBasic(bool mutatingLspWorkspace)
    {
        var markup = """
            Class {|class:A|}
                Sub Method()
                End Sub
            End Class
            """;

        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace);
        var expected = new LSP.SymbolInformation[]
        {
            CreateSymbolInformation(LSP.SymbolKind.Class, "A", testLspServer.GetLocations("class").Single(), Glyph.ClassInternal, GetContainerName(testLspServer.GetCurrentSolution()))
        };

        var results = await RunGetWorkspaceSymbolsAsync(testLspServer, "A").ConfigureAwait(false);
        AssertSetEquals(expected, results);
    }

    private static Task<LSP.SymbolInformation[]?> RunGetWorkspaceSymbolsAsync(TestLspServer testLspServer, string query, IProgress<LSP.SymbolInformation[]>? progress = null)
    {
        var request = new LSP.WorkspaceSymbolParams
        {
            Query = query,
            PartialResultToken = progress
        };

        return testLspServer.ExecuteRequestAsync<LSP.WorkspaceSymbolParams, LSP.SymbolInformation[]>(LSP.Methods.WorkspaceSymbolName,
            request, CancellationToken.None);
    }

    private static string GetContainerName(Solution solution, string? containingSymbolName = null)
    {
        if (containingSymbolName == null)
        {
            return string.Format(FeaturesResources.project_0, solution.Projects.Single().Name);
        }
        else
        {
            return string.Format(FeaturesResources.in_0_project_1, containingSymbolName, solution.Projects.Single().Name);
        }
    }

    [ExportWorkspaceService(typeof(IWorkspaceNavigateToSearcherHostService))]
    [PartNotDiscoverable, Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    protected sealed class TestWorkspaceNavigateToSearchHostService() : IWorkspaceNavigateToSearcherHostService
    {
        public ValueTask<bool> IsFullyLoadedAsync(CancellationToken cancellationToken)
            => new(true);
    }
}
