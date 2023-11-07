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
using Xunit.Abstractions;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.References
{
    public class FindImplementationsTests : AbstractLanguageServerProtocolTests
    {
        public FindImplementationsTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Theory, CombinatorialData]
        public async Task TestFindImplementationAsync(bool mutatingLspWorkspace)
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
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

            var results = await RunFindImplementationAsync(testLspServer, testLspServer.GetLocations("caret").Single());
            AssertLocationsEqual(testLspServer.GetLocations("implementation"), results);
        }

        [Theory, CombinatorialData]
        public async Task TestFindImplementationAsync_DifferentDocument(bool mutatingLspWorkspace)
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

            await using var testLspServer = await CreateTestLspServerAsync(markups, mutatingLspWorkspace);

            var results = await RunFindImplementationAsync(testLspServer, testLspServer.GetLocations("caret").Single());
            AssertLocationsEqual(testLspServer.GetLocations("implementation"), results);
        }

        [Theory, CombinatorialData]
        public async Task TestFindImplementationAsync_MappedFile(bool mutatingLspWorkspace)
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
            await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace);

            AddMappedDocument(testLspServer.TestWorkspace, markup);

            var position = new LSP.Position { Line = 2, Character = 9 };
            var results = await RunFindImplementationAsync(testLspServer, new LSP.Location
            {
                Uri = ProtocolConversions.CreateAbsoluteUri($"C:\\{TestSpanMapper.GeneratedFileName}"),
                Range = new LSP.Range { Start = position, End = position }
            });
            AssertLocationsEqual(ImmutableArray.Create(TestSpanMapper.MappedFileLocation), results);
        }

        [Theory, CombinatorialData]
        public async Task TestFindImplementationAsync_InvalidLocation(bool mutatingLspWorkspace)
        {
            var markup =
@"class A
{
    void M()
    {
        {|caret:|}
    }
}";
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

            var results = await RunFindImplementationAsync(testLspServer, testLspServer.GetLocations("caret").Single());
            Assert.Empty(results);
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/44846")]
        public async Task TestFindImplementationAsync_MultipleLocations(bool mutatingLspWorkspace)
        {
            var markup =
@"class {|caret:|}{|implementation:A|} { }

class {|implementation:B|} : A { }

class {|implementation:C|} : A { }";
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

            var results = await RunFindImplementationAsync(testLspServer, testLspServer.GetLocations("caret").Single());
            AssertLocationsEqual(testLspServer.GetLocations("implementation"), results);
        }

        [Theory, CombinatorialData]
        public async Task TestFindImplementationAsync_NoMetadataResults(bool mutatingLspWorkspace)
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
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

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
