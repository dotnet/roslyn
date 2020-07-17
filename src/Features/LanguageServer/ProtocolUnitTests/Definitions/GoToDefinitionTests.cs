// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Definitions
{
    public class GoToDefinitionTests : AbstractLanguageServerProtocolTests
    {
        [Fact]
        public async Task TestGotoDefinitionAsync()
        {
            var markup =
@"class A
{
    string {|definition:aString|} = 'hello';
    void M()
    {
        var len = {|caret:|}aString.Length;
    }
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);

            var results = await RunGotoDefinitionAsync(workspace.CurrentSolution, locations["caret"].Single());
            AssertLocationsEqual(locations["definition"], results);
        }

        [Fact]
        public async Task TestGotoDefinitionAsync_DifferentDocument()
        {
            var markups = new string[]
            {
@"namespace One
{
    class A
    {
        public static int {|definition:aInt|} = 1;
    }
}",
@"namespace One
{
    class B
    {
        int bInt = One.A.{|caret:|}aInt;
    }
}"
            };

            using var workspace = CreateTestWorkspace(markups, out var locations);

            var results = await RunGotoDefinitionAsync(workspace.CurrentSolution, locations["caret"].Single());
            AssertLocationsEqual(locations["definition"], results);
        }

        [Fact]
        public async Task TestGotoDefinitionAsync_MappedFile()
        {
            var markup =
@"class A
{
    string aString = 'hello';
    void M()
    {
        var len = aString.Length;
    }
}";
            using var workspace = CreateTestWorkspace(string.Empty, out var _);

            AddMappedDocument(workspace, markup);

            var position = new LSP.Position { Line = 5, Character = 18 };
            var results = await RunGotoDefinitionAsync(workspace.CurrentSolution, new LSP.Location
            {
                Uri = new Uri($"C:\\{TestSpanMapper.GeneratedFileName}"),
                Range = new LSP.Range { Start = position, End = position }
            });
            AssertLocationsEqual(ImmutableArray.Create(TestSpanMapper.MappedFileLocation), results);
        }

        [Fact]
        public async Task TestGotoDefinitionAsync_InvalidLocation()
        {
            var markup =
@"class A
{
    void M()
    {{|caret:|}
        var len = aString.Length;
    }
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);

            var results = await RunGotoDefinitionAsync(workspace.CurrentSolution, locations["caret"].Single());
            Assert.Empty(results);
        }

        private static async Task<LSP.Location[]> RunGotoDefinitionAsync(Solution solution, LSP.Location caret)
            => await GetLanguageServer(solution).ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.Location[]>(LSP.Methods.TextDocumentDefinitionName,
                CreateTextDocumentPositionParams(caret), new LSP.ClientCapabilities(), null, CancellationToken.None);
    }
}
