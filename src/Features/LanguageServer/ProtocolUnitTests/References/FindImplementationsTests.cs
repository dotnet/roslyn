﻿// Licensed to the .NET Foundation under one or more agreements.
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

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.References
{
    public class FindImplementationsTests : AbstractLanguageServerProtocolTests
    {
        [Fact]
        public async Task TestFindImplementationAsync()
        {
            var markup =
@"interface IA
{
    void {|caret:|}M();
}
class A : IA
{
    void IA.{|implementation:M|}()
    {
    }
}";
            using var testLspServer = CreateTestLspServer(markup, out var locations);

            var results = await RunFindImplementationAsync(testLspServer, locations["caret"].Single());
            AssertLocationsEqual(locations["implementation"], results);
        }

        [Fact]
        public async Task TestFindImplementationAsync_DifferentDocument()
        {
            var markups = new string[]
            {
@"namespace One
{
    interface IA
    {
        void {|caret:|}M();
    }
}",
@"namespace One
{
    class A : IA
    {
        void IA.{|implementation:M|}()
        {
        }
    }
}"
            };

            using var testLspServer = CreateTestLspServer(markups, out var locations);

            var results = await RunFindImplementationAsync(testLspServer, locations["caret"].Single());
            AssertLocationsEqual(locations["implementation"], results);
        }

        [Fact]
        public async Task TestFindImplementationAsync_MappedFile()
        {
            var markup =
@"interface IA
{
    void M();
}
class A : IA
{
    void IA.M()
    {
    }
}";
            using var testLspServer = CreateTestLspServer(string.Empty, out var _);

            AddMappedDocument(testLspServer.TestWorkspace, markup);

            var position = new LSP.Position { Line = 2, Character = 9 };
            var results = await RunFindImplementationAsync(testLspServer, new LSP.Location
            {
                Uri = new Uri($"C:\\{TestSpanMapper.GeneratedFileName}"),
                Range = new LSP.Range { Start = position, End = position }
            });
            AssertLocationsEqual(ImmutableArray.Create(TestSpanMapper.MappedFileLocation), results);
        }

        [Fact]
        public async Task TestFindImplementationAsync_InvalidLocation()
        {
            var markup =
@"class A
{
    void M()
    {
        {|caret:|}
    }
}";
            using var testLspServer = CreateTestLspServer(markup, out var locations);

            var results = await RunFindImplementationAsync(testLspServer, locations["caret"].Single());
            Assert.Empty(results);
        }

        [Fact, WorkItem(44846, "https://github.com/dotnet/roslyn/issues/44846")]
        public async Task TestFindImplementationAsync_MultipleLocations()
        {
            var markup =
@"class {|caret:|}{|implementation:A|} { }

class {|implementation:B|} : A { }

class {|implementation:C|} : A { }";
            using var testLspServer = CreateTestLspServer(markup, out var locations);

            var results = await RunFindImplementationAsync(testLspServer, locations["caret"].Single());
            AssertLocationsEqual(locations["implementation"], results);
        }

        private static async Task<LSP.Location[]> RunFindImplementationAsync(TestLspServer testLspServer, LSP.Location caret)
        {
            return await testLspServer.ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.Location[]>(LSP.Methods.TextDocumentImplementationName,
                           CreateTextDocumentPositionParams(caret), new LSP.ClientCapabilities(), null, CancellationToken.None);
        }
    }
}
