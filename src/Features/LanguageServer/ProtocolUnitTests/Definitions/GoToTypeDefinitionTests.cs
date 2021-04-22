﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Definitions
{
    public class GoToTypeDefinitionTests : AbstractLanguageServerProtocolTests
    {
        [Fact]
        public async Task TestGotoTypeDefinitionAsync()
        {
            var markup =
@"class {|definition:A|}
{
}
class B
{
    {|caret:|}A classA;
}";
            using var testLspServer = CreateTestLspServer(markup, out var locations);

            var results = await RunGotoTypeDefinitionAsync(testLspServer, locations["caret"].Single());
            AssertLocationsEqual(locations["definition"], results);
        }

        [Fact]
        public async Task TestGotoTypeDefinitionAsync_DifferentDocument()
        {
            var markups = new string[]
            {
@"namespace One
{
    class {|definition:A|}
    {
    }
}",
@"namespace One
{
    class B
    {
        {|caret:|}A classA;
    }
}"
            };

            using var testLspServer = CreateTestLspServer(markups, out var locations);

            var results = await RunGotoTypeDefinitionAsync(testLspServer, locations["caret"].Single());
            AssertLocationsEqual(locations["definition"], results);
        }

        [Fact]
        public async Task TestGotoTypeDefinitionAsync_InvalidLocation()
        {
            var markup =
@"class {|definition:A|}
{
}
class B
{
    A classA;
    {|caret:|}
}";
            using var testLspServer = CreateTestLspServer(markup, out var locations);

            var results = await RunGotoTypeDefinitionAsync(testLspServer, locations["caret"].Single());
            Assert.Empty(results);
        }

        private static async Task<LSP.Location[]> RunGotoTypeDefinitionAsync(TestLspServer testLspServer, LSP.Location caret)
        {
            return await testLspServer.ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.Location[]>(LSP.Methods.TextDocumentTypeDefinitionName,
                           CreateTextDocumentPositionParams(caret), new LSP.ClientCapabilities(), null, CancellationToken.None);
        }
    }
}
