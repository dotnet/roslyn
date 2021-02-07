// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Symbols
{
    public class WorkspaceSymbolsTests : AbstractLanguageServerProtocolTests
    {
        [Fact]
        public async Task TestGetWorkspaceSymbolsAsync_Class()
        {
            var markup =
@"class {|class:A|}
{
    void M()
    {
    }
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var expected = new LSP.SymbolInformation[]
            {
                CreateSymbolInformation(LSP.SymbolKind.Class, "A", locations["class"].Single(), Glyph.ClassInternal, GetContainerName(workspace))
            };

            var results = await RunGetWorkspaceSymbolsAsync(workspace.CurrentSolution, "A").ConfigureAwait(false);
            AssertJsonEquals(expected, results);
        }

        [Fact]
        public async Task TestGetWorkspaceSymbolsAsync_Class_Streaming()
        {
            var markup =
@"class {|class:A|}
{
    void M()
    {
    }
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var expected = new LSP.SymbolInformation[]
            {
                CreateSymbolInformation(LSP.SymbolKind.Class, "A", locations["class"].Single(), Glyph.ClassInternal, GetContainerName(workspace))
            };

            using var progress = BufferedProgress.Create<LSP.SymbolInformation>(null);

            var results = await RunGetWorkspaceSymbolsAsync(workspace.CurrentSolution, "A", progress).ConfigureAwait(false);

            Assert.Null(results);

            results = progress.GetValues().ToArray();

            AssertJsonEquals(expected, results);
        }

        [Fact]
        public async Task TestGetWorkspaceSymbolsAsync_Method()
        {
            var markup =
@"class A
{
    void {|method:M|}()
    {
    }
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var expected = new LSP.SymbolInformation[]
            {
                CreateSymbolInformation(LSP.SymbolKind.Method, "M", locations["method"].Single(), Glyph.MethodPrivate, GetContainerName(workspace, "A"))
            };

            var results = await RunGetWorkspaceSymbolsAsync(workspace.CurrentSolution, "M").ConfigureAwait(false);
            AssertJsonEquals(expected, results);
        }

        [Fact(Skip = "GetWorkspaceSymbolsAsync does not yet support locals.")]
        // TODO - Remove skip & modify once GetWorkspaceSymbolsAsync is updated to support all symbols.
        // https://github.com/dotnet/roslyn/projects/45#card-20033822
        public async Task TestGetWorkspaceSymbolsAsync_Local()
        {
            var markup =
@"class A
{
    void M()
    {
        int {|local:i|} = 1;
    }
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var expected = new LSP.SymbolInformation[]
            {
                CreateSymbolInformation(LSP.SymbolKind.Variable, "i", locations["local"].Single(), Glyph.Local, GetContainerName(workspace, "A.M.i"))
            };

            var results = await RunGetWorkspaceSymbolsAsync(workspace.CurrentSolution, "i").ConfigureAwait(false);
            AssertJsonEquals(expected, results);
        }

        [Fact]
        public async Task TestGetWorkspaceSymbolsAsync_MultipleKinds()
        {
            var markup =
@"class A
{
    int {|field:F|};
    void M()
    {
    }
    class {|class:F|}
    {
        int {|field:F|};
    }
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var classAContainerName = GetContainerName(workspace, "A");
            var expected = new LSP.SymbolInformation[]
            {
                CreateSymbolInformation(LSP.SymbolKind.Field, "F", locations["field"][0], Glyph.FieldPrivate, classAContainerName),
                CreateSymbolInformation(LSP.SymbolKind.Class, "F", locations["class"].Single(), Glyph.ClassPrivate, classAContainerName),
                CreateSymbolInformation(LSP.SymbolKind.Field, "F", locations["field"][1], Glyph.FieldPrivate, GetContainerName(workspace, "A.F"))
            };

            var results = await RunGetWorkspaceSymbolsAsync(workspace.CurrentSolution, "F").ConfigureAwait(false);
            AssertJsonEquals(expected, results);
        }

        [Fact]
        public async Task TestGetWorkspaceSymbolsAsync_MultipleDocuments()
        {
            var markups = new string[]
            {
@"class A
{
    void {|method:M|}()
    {
    }
}",
@"class B
{
    void {|method:M|}()
    {
    }
}"
            };

            using var workspace = CreateTestWorkspace(markups, out var locations);
            var expected = new LSP.SymbolInformation[]
            {
                CreateSymbolInformation(LSP.SymbolKind.Method, "M", locations["method"][0], Glyph.MethodPrivate, GetContainerName(workspace, "A")),
                CreateSymbolInformation(LSP.SymbolKind.Method, "M", locations["method"][1], Glyph.MethodPrivate, GetContainerName(workspace, "B"))
            };

            var results = await RunGetWorkspaceSymbolsAsync(workspace.CurrentSolution, "M").ConfigureAwait(false);
            AssertJsonEquals(expected, results);
        }

        [Fact]
        public async Task TestGetWorkspaceSymbolsAsync_NoSymbols()
        {
            var markup =
@"class A
{
    void M()
    {
    }
}";
            using var workspace = CreateTestWorkspace(markup, out var _);

            var results = await RunGetWorkspaceSymbolsAsync(workspace.CurrentSolution, "NonExistingSymbol").ConfigureAwait(false);
            Assert.Empty(results);
        }

        [Fact]
        public async Task TestGetWorkspaceSymbolsAsync_VisualBasic()
        {
            var markup = @"Class {|class:A|}
    Sub Method()
    End Sub
End Class";

            using var workspace = CreateVisualBasicTestWorkspace(markup, out var locations);
            var expected = new LSP.SymbolInformation[]
            {
                CreateSymbolInformation(LSP.SymbolKind.Class, "A", locations["class"].Single(), Glyph.ClassInternal, GetContainerName(workspace))
            };

            var results = await RunGetWorkspaceSymbolsAsync(workspace.CurrentSolution, "A").ConfigureAwait(false);
            AssertJsonEquals(expected, results);
        }

        private static async Task<LSP.SymbolInformation[]> RunGetWorkspaceSymbolsAsync(Solution solution, string query, IProgress<LSP.SymbolInformation[]>? progress = null)
        {
            var request = new LSP.WorkspaceSymbolParams
            {
                Query = query,
                PartialResultToken = progress
            };

            var queue = CreateRequestQueue(solution);
            return await GetLanguageServer(solution).ExecuteRequestAsync<LSP.WorkspaceSymbolParams, LSP.SymbolInformation[]>(queue, LSP.Methods.WorkspaceSymbolName,
                request, new LSP.ClientCapabilities(), null, CancellationToken.None);
        }

        private static string GetContainerName(TestWorkspace testWorkspace, string? containingSymbolName = null)
        {
            if (containingSymbolName == null)
            {
                return string.Format(FeaturesResources.project_0, testWorkspace.Projects.Single().Name);
            }
            else
            {
                return string.Format(FeaturesResources.in_0_project_1, containingSymbolName, testWorkspace.Projects.Single().Name);
            }
        }
    }
}
