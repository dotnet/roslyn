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
            using var testLspServer = await CreateTestLspServerAsync(markup);

            var results = await RunFindImplementationAsync(testLspServer, testLspServer.GetLocations("caret").Single());
            AssertLocationsEqual(testLspServer.GetLocations("implementation"), results);
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

            using var testLspServer = await CreateTestLspServerAsync(markups);

            var results = await RunFindImplementationAsync(testLspServer, testLspServer.GetLocations("caret").Single());
            AssertLocationsEqual(testLspServer.GetLocations("implementation"), results);
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
            using var testLspServer = await CreateTestLspServerAsync(string.Empty);

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
            using var testLspServer = await CreateTestLspServerAsync(markup);

            var results = await RunFindImplementationAsync(testLspServer, testLspServer.GetLocations("caret").Single());
            Assert.Empty(results);
        }

        [Fact, WorkItem(44846, "https://github.com/dotnet/roslyn/issues/44846")]
        public async Task TestFindImplementationAsync_MultipleLocations()
        {
            var markup =
@"class {|caret:|}{|implementation:A|} { }

class {|implementation:B|} : A { }

class {|implementation:C|} : A { }";
            using var testLspServer = await CreateTestLspServerAsync(markup);

            var results = await RunFindImplementationAsync(testLspServer, testLspServer.GetLocations("caret").Single());
            AssertLocationsEqual(testLspServer.GetLocations("implementation"), results);
        }

        [Fact]
        public async Task TestFindImplementationAsync_NoMetadataResults()
        {
            var markup = @"
using System;
class C : IDisposable
{
    public void {|implementation:Dispose|}()
    {
        IDisposable d;
        d.{|caret:|}Dispose();
    }
}";
            using var testLspServer = await CreateTestLspServerAsync(markup);

            var results = await RunFindImplementationAsync(testLspServer, testLspServer.GetLocations("caret").Single());

            // At the moment we only support source results here, so verify we haven't accidentally
            // broken that without work to make sure they display nicely
            AssertLocationsEqual(testLspServer.GetLocations("implementation"), results);
        }

        private static async Task<LSP.Location[]> RunFindImplementationAsync(TestLspServer testLspServer, LSP.Location caret)
        {
            return await testLspServer.ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.Location[]>(LSP.Methods.TextDocumentImplementationName,
                           CreateTextDocumentPositionParams(caret), CancellationToken.None);
        }
    }
}
