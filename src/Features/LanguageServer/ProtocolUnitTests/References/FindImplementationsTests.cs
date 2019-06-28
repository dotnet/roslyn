// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            var (solution, locations) = CreateTestSolution(markup);

            var results = await RunFindImplementationAsync(solution, locations["caret"].Single());
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
            var (solution, locations) = CreateTestSolution(markups);

            var results = await RunFindImplementationAsync(solution, locations["caret"].Single());
            AssertLocationsEqual(locations["implementation"], results);
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
            var (solution, locations) = CreateTestSolution(markup);

            var results = await RunFindImplementationAsync(solution, locations["caret"].Single());
            Assert.Empty(results);
        }

        private static async Task<LSP.Location[]> RunFindImplementationAsync(Solution solution, LSP.Location caret)
            => (LSP.Location[])await GetLanguageServer(solution).FindImplementationsAsync(solution, CreateTextDocumentPositionParams(caret), new LSP.ClientCapabilities(), CancellationToken.None);
    }
}
