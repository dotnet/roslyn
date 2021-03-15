// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
            using var testLspServer = CreateTestLspServer(markup, out var locations);

            var results = await RunGotoDefinitionAsync(testLspServer, locations["caret"].Single());
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

            using var testLspServer = CreateTestLspServer(markups, out var locations);

            var results = await RunGotoDefinitionAsync(testLspServer, locations["caret"].Single());
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
            using var testLspServer = CreateTestLspServer(string.Empty, out var _);

            AddMappedDocument(testLspServer.TestWorkspace, markup);

            var position = new LSP.Position { Line = 5, Character = 18 };
            var results = await RunGotoDefinitionAsync(testLspServer, new LSP.Location
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
            using var testLspServer = CreateTestLspServer(markup, out var locations);

            var results = await RunGotoDefinitionAsync(testLspServer, locations["caret"].Single());
            Assert.Empty(results);
        }

        [Fact, WorkItem(1264627, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1264627")]
        public async Task TestGotoDefinitionAsync_NoResultsOnNamespace()
        {
            var markup =
@"namespace {|caret:M|}
{
    class A
    {
    }
}";
            using var testLspServer = CreateTestLspServer(markup, out var locations);

            var results = await RunGotoDefinitionAsync(testLspServer, locations["caret"].Single());
            Assert.Empty(results);
        }

        private static async Task<LSP.Location[]> RunGotoDefinitionAsync(TestLspServer testLspServer, LSP.Location caret)
        {
            return await testLspServer.ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.Location[]>(LSP.Methods.TextDocumentDefinitionName,
                           CreateTextDocumentPositionParams(caret), new LSP.ClientCapabilities(), null, CancellationToken.None);
        }
    }
}
