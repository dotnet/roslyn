// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.References
{
    public class FindAllReferencesHandlerTests : AbstractLanguageServerProtocolTests
    {
        [WpfFact]
        public async Task TestFindAllReferencesAsync()
        {
            var markup =
@"class A
{
    public int {|reference:someInt|} = 1;
    void M()
    {
        var i = {|reference:someInt|} + 1;
    }
}
class B
{
    int someInt = A.{|reference:someInt|} + 1;
    void M2()
    {
        var j = someInt + A.{|caret:|}{|reference:someInt|};
    }
}";
            var (solution, locations) = CreateTestSolution(markup);

            var results = await RunFindAllReferencesAsync(solution, locations["caret"].First());
            AssertLocationsEqual(locations["reference"], results.Select(result => result.Location));
        }

        [WpfFact]
        public async Task TestFindAllReferencesAsync_MultipleDocuments()
        {
            var markups = new string[] {
@"class A
{
    public int {|reference:someInt|} = 1;
    void M()
    {
        var i = {|reference:someInt|} + 1;
    }
}",
@"class B
{
    int someInt = A.{|reference:someInt|} + 1;
    void M2()
    {
        var j = someInt + A.{|caret:|}{|reference:someInt|};
    }
}"
            };

            var (solution, locations) = CreateTestSolution(markups);

            var results = await RunFindAllReferencesAsync(solution, locations["caret"].First());
            AssertLocationsEqual(locations["reference"], results.Select(result => result.Location));
        }

        [WpfFact]
        public async Task TestFindAllReferencesAsync_InvalidLocation()
        {
            var markup =
@"class A
{
    {|caret:|}
}";
            var (solution, ranges) = CreateTestSolution(markup);

            var results = await RunFindAllReferencesAsync(solution, ranges["caret"].First());
            Assert.Empty(results);
        }

        private static LSP.ReferenceParams CreateReferenceParams(LSP.Location caret) =>
            new LSP.ReferenceParams()
            {
                TextDocument = CreateTextDocumentIdentifier(caret.Uri),
                Position = caret.Range.Start,
                Context = new LSP.ReferenceContext(),
            };

        private static async Task<LSP.VSReferenceItem[]> RunFindAllReferencesAsync(Solution solution, LSP.Location caret)
        {
            var vsClientCapabilities = new LSP.VSClientCapabilities
            {
                SupportsVisualStudioExtensions = true
            };

            return await GetLanguageServer(solution).GetDocumentReferencesAsync(solution, CreateReferenceParams(caret), vsClientCapabilities, CancellationToken.None);
        }
    }
}
