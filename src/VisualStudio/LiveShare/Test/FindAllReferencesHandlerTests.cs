// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.UnitTests
{
    public class FindAllReferencesHandlerTests : AbstractLiveShareRequestHandlerTests
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

            var results = await RunFindAllReferencesAsync(solution, locations["caret"].First(), true);
            AssertLocationsEqual(locations["reference"], results);
        }

        [WpfFact]
        public async Task TestFindAllReferencesAsync_DoNotIncludeDeclarations()
        {
            var markup =
@"class A
{
    public int someInt = 1;
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

            var results = await RunFindAllReferencesAsync(solution, locations["caret"].First(), false);
            AssertLocationsEqual(locations["reference"], results);
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

            var results = await RunFindAllReferencesAsync(solution, locations["caret"].First(), true);
            AssertLocationsEqual(locations["reference"], results);
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

            var results = await RunFindAllReferencesAsync(solution, ranges["caret"].First(), true);
            Assert.Empty(results);
        }

        private static async Task<LSP.Location[]> RunFindAllReferencesAsync(Solution solution, LSP.Location caret, bool includeDeclaration)
        {
            var request = new LSP.ReferenceParams()
            {
                TextDocument = CreateTextDocumentIdentifier(caret.Uri),
                Position = caret.Range.Start,
                Context = new LSP.ReferenceContext()
                {
                    IncludeDeclaration = includeDeclaration
                }
            };

            var references = await TestHandleAsync<LSP.ReferenceParams, object[]>(solution, request);
            return references.Select(o => (LSP.Location)o).ToArray();
        }
    }
}
