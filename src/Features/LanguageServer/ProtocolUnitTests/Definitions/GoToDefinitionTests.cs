// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            var (solution, locations) = CreateTestSolution(markup);

            var results = await RunGotoDefinitionAsync(solution, locations["caret"].Single());
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
            var (solution, locations) = CreateTestSolution(markups);

            var results = await RunGotoDefinitionAsync(solution, locations["caret"].Single());
            AssertLocationsEqual(locations["definition"], results);
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
            var (solution, locations) = CreateTestSolution(markup);

            var results = await RunGotoDefinitionAsync(solution, locations["caret"].Single());
            Assert.Empty(results);
        }

        private static async Task<LSP.Location[]> RunGotoDefinitionAsync(Solution solution, LSP.Location caret)
            => (LSP.Location[])await GetLanguageServer(solution).GoToDefinitionAsync(solution, CreateTextDocumentPositionParams(caret), new LSP.ClientCapabilities(), CancellationToken.None);
    }
}
