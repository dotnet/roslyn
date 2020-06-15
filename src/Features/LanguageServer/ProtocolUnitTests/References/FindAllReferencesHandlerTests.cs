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
        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/43063")]
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
            using var workspace = CreateTestWorkspace(markup, out var locations);

            var results = await RunFindAllReferencesAsync(workspace.CurrentSolution, locations["caret"].First());
            AssertLocationsEqual(locations["reference"], results.Select(result => result.Location));

            Assert.Equal("A", results[0].ContainingType);
            Assert.Equal("B", results[2].ContainingType);
            Assert.Equal("M", results[1].ContainingMember);
            Assert.Equal("M2", results[3].ContainingMember);

            AssertValidDefinitionProperties(results, 0);
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/43063")]
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

            using var workspace = CreateTestWorkspace(markups, out var locations);

            var results = await RunFindAllReferencesAsync(workspace.CurrentSolution, locations["caret"].First());
            AssertLocationsEqual(locations["reference"], results.Select(result => result.Location));

            Assert.Equal("A", results[0].ContainingType);
            Assert.Equal("B", results[2].ContainingType);
            Assert.Equal("M", results[1].ContainingMember);
            Assert.Equal("M2", results[3].ContainingMember);

            AssertValidDefinitionProperties(results, 0);
        }

        [WpfFact]
        public async Task TestFindAllReferencesAsync_InvalidLocation()
        {
            var markup =
@"class A
{
    {|caret:|}
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);

            var results = await RunFindAllReferencesAsync(workspace.CurrentSolution, locations["caret"].First());
            Assert.Empty(results);
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/43063")]
        public async Task TestFindAllReferencesMetadataDefinitionAsync()
        {
            var markup =
@"using System;

class A
{
    void M()
    {
        Console.{|caret:|}{|reference:WriteLine|}(""text"");
    }
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);

            var results = await RunFindAllReferencesAsync(workspace.CurrentSolution, locations["caret"].First());
            Assert.NotNull(results[0].Location.Uri);
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

            return await GetLanguageServer(solution).ExecuteRequestAsync<LSP.ReferenceParams, LSP.VSReferenceItem[]>(LSP.Methods.TextDocumentReferencesName,
                CreateReferenceParams(caret), vsClientCapabilities, null, CancellationToken.None);
        }

        private static void AssertValidDefinitionProperties(LSP.ReferenceItem[] referenceItems, int definitionIndex)
        {
            var definition = referenceItems[definitionIndex];
            var definitionId = definition.DefinitionId;
            Assert.NotNull(definition.DefinitionText);

            for (var i = 0; i < referenceItems.Length; i++)
            {
                if (i == definitionIndex)
                {
                    continue;
                }

                Assert.Null(referenceItems[i].DefinitionText);
                Assert.Equal(definitionId, referenceItems[i].DefinitionId);
                Assert.NotEqual(definitionId, referenceItems[i].Id);
            }
        }
    }
}
