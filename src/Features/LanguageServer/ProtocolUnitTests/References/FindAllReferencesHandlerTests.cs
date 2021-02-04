// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.ReferenceHighlighting;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.Text.Adornments;
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
            using var workspace = CreateTestWorkspace(markup, out var locations);

            var results = await RunFindAllReferencesAsync(workspace.CurrentSolution, locations["caret"].First());
            AssertLocationsEqual(locations["reference"], results.Select(result => result.Location));

            // Results are returned in a non-deterministic order, so we order them by location
            var orderedResults = results.OrderBy(r => r.Location, new OrderLocations()).ToArray();
            Assert.Equal("A", orderedResults[0].ContainingType);
            Assert.Equal("B", orderedResults[2].ContainingType);
            Assert.Equal("M", orderedResults[1].ContainingMember);
            Assert.Equal("M2", orderedResults[3].ContainingMember);

            AssertValidDefinitionProperties(results, 0, Glyph.FieldPublic);
            AssertHighlightCount(results, expectedDefinitionCount: 1, expectedWrittenReferenceCount: 0, expectedReferenceCount: 3);
        }

        [WpfFact]
        public async Task TestFindAllReferencesAsync_Streaming()
        {
            var markup =
@"class A
{
    public static int {|reference:someInt|} = 1;
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

            using var progress = BufferedProgress.Create<object>(null);

            var results = await RunFindAllReferencesAsync(workspace.CurrentSolution, locations["caret"].First(), progress);

            Assert.Null(results);

            // BufferedProgress wraps individual elements in an array, so when they are nested them like this,
            // with the test creating one, and the handler another, we have to unwrap.
            results = progress.GetValues().Cast<LSP.VSReferenceItem>().ToArray();

            Assert.NotNull(results);
            Assert.NotEmpty(results);

            AssertLocationsEqual(locations["reference"], results.Select(result => result.Location));

            // Results are returned in a non-deterministic order, so we order them by location
            var orderedResults = results.OrderBy(r => r.Location, new OrderLocations()).ToArray();
            Assert.Equal("A", orderedResults[0].ContainingType);
            Assert.Equal("B", orderedResults[2].ContainingType);
            Assert.Equal("M", orderedResults[1].ContainingMember);
            Assert.Equal("M2", orderedResults[3].ContainingMember);

            AssertValidDefinitionProperties(results, 0, Glyph.FieldPublic);
            AssertHighlightCount(results, expectedDefinitionCount: 1, expectedWrittenReferenceCount: 0, expectedReferenceCount: 3);
        }

        [WpfFact]
        public async Task TestFindAllReferencesAsync_Class()
        {
            var markup =
@"class {|reference:A|}
{
    public static int someInt = 1;
    void M()
    {
        var i = someInt + 1;
    }
}
class B
{
    int someInt = {|reference:A|}.someInt + 1;
    void M2()
    {
        var j = someInt + {|caret:|}{|reference:A|}.someInt;
    }
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);

            var results = await RunFindAllReferencesAsync(workspace.CurrentSolution, locations["caret"].First());
            AssertLocationsEqual(locations["reference"], results.Select(result => result.Location));

            var textElement = results[0].Text as ClassifiedTextElement;
            Assert.NotNull(textElement);
            var actualText = string.Concat(textElement.Runs.Select(r => r.Text));

            Assert.Equal("class A", actualText);

            // Results are returned in a non-deterministic order, so we order them by location
            var orderedResults = results.OrderBy(r => r.Location, new OrderLocations()).ToArray();
            Assert.Equal("B", orderedResults[1].ContainingType);
            Assert.Equal("B", orderedResults[2].ContainingType);
            Assert.Equal("M2", orderedResults[2].ContainingMember);

            AssertValidDefinitionProperties(results, 0, Glyph.ClassInternal);
            AssertHighlightCount(results, expectedDefinitionCount: 1, expectedWrittenReferenceCount: 0, expectedReferenceCount: 2);
        }

        [WpfFact]
        public async Task TestFindAllReferencesAsync_MultipleDocuments()
        {
            var markups = new string[] {
@"class A
{
    public static int {|reference:someInt|} = 1;
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

            // Results are returned in a non-deterministic order, so we order them by location
            var orderedResults = results.OrderBy(r => r.Location, new OrderLocations()).ToArray();
            Assert.Equal("A", orderedResults[0].ContainingType);
            Assert.Equal("B", orderedResults[2].ContainingType);
            Assert.Equal("M", orderedResults[1].ContainingMember);
            Assert.Equal("M2", orderedResults[3].ContainingMember);

            AssertValidDefinitionProperties(results, 0, Glyph.FieldPublic);
            AssertHighlightCount(results, expectedDefinitionCount: 1, expectedWrittenReferenceCount: 0, expectedReferenceCount: 3);
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

        [WpfFact]
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
            AssertHighlightCount(results, expectedDefinitionCount: 0, expectedWrittenReferenceCount: 0, expectedReferenceCount: 1);
        }

        [WpfFact, WorkItem(1240061, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1240061/")]
        public async Task TestFindAllReferencesAsync_Namespace()
        {
            var markup =
@"namespace {|caret:|}{|reference:N|}
{
    class C
    {
        void M()
        {
            var x = new {|reference:N|}.C();
        }
    }
}
";
            using var workspace = CreateTestWorkspace(markup, out var locations);

            var results = await RunFindAllReferencesAsync(workspace.CurrentSolution, locations["caret"].First());

            // Namespace definitions should not have a location
            Assert.True(results.Any(r => r.DefinitionText != null && r.Location == null));

            // Namespace references should have a location
            Assert.True(results.Any(r => r.DefinitionText == null && r.Location != null));

            AssertValidDefinitionProperties(results, 0, Glyph.Namespace);
            AssertHighlightCount(results, expectedDefinitionCount: 0, expectedWrittenReferenceCount: 0, expectedReferenceCount: 2);
        }

        [WpfFact, WorkItem(1245616, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1245616/")]
        public async Task TestFindAllReferencesAsync_Highlights()
        {
            var markup =
@"using System;

class C
{
    void M()
    {
        var {|caret:|}{|reference:x|} = 1;
        Console.WriteLine({|reference:x|});
        {|reference:x|} = 2;
    }
}
";
            using var workspace = CreateTestWorkspace(markup, out var locations);

            var results = await RunFindAllReferencesAsync(workspace.CurrentSolution, locations["caret"].First());
            AssertHighlightCount(results, expectedDefinitionCount: 1, expectedWrittenReferenceCount: 1, expectedReferenceCount: 1);
        }

        [WpfFact]
        public async Task TestFindAllReferencesAsync_StaticClassification()
        {
            var markup =
@"static class {|caret:|}{|reference:C|} { }
";
            using var workspace = CreateTestWorkspace(markup, out var locations);

            var results = await RunFindAllReferencesAsync(workspace.CurrentSolution, locations["caret"].First());

            // Ensure static definitions and references are only classified once
            var textRuns = ((ClassifiedTextElement)results.First().Text).Runs;
            Assert.Equal(9, textRuns.Count());
        }

        private static LSP.ReferenceParams CreateReferenceParams(LSP.Location caret, IProgress<object> progress) =>
            new LSP.ReferenceParams()
            {
                TextDocument = CreateTextDocumentIdentifier(caret.Uri),
                Position = caret.Range.Start,
                Context = new LSP.ReferenceContext(),
                PartialResultToken = progress
            };

        private static async Task<LSP.VSReferenceItem[]> RunFindAllReferencesAsync(Solution solution, LSP.Location caret, IProgress<object> progress = null)
        {
            var queue = CreateRequestQueue(solution);

            return await RunFindAllReferencesAsync(queue, solution, caret, progress);
        }

        internal static async Task<LSP.VSReferenceItem[]> RunFindAllReferencesAsync(Handler.RequestExecutionQueue queue, Solution solution, LSP.Location caret, IProgress<object> progress = null)
        {
            var vsClientCapabilities = new LSP.VSClientCapabilities
            {
                SupportsVisualStudioExtensions = true
            };

            return await GetLanguageServer(solution).ExecuteRequestAsync<LSP.ReferenceParams, LSP.VSReferenceItem[]>(queue, LSP.Methods.TextDocumentReferencesName,
                CreateReferenceParams(caret, progress), vsClientCapabilities, null, CancellationToken.None);
        }

        private static void AssertValidDefinitionProperties(LSP.VSReferenceItem[] referenceItems, int definitionIndex, Glyph definitionGlyph)
        {
            var definition = referenceItems[definitionIndex];
            var definitionId = definition.DefinitionId;
            Assert.NotNull(definition.DefinitionText);

            Assert.Equal(definitionGlyph.GetImageId(), definition.DefinitionIcon.ImageId);

            for (var i = 0; i < referenceItems.Length; i++)
            {
                if (i == definitionIndex)
                {
                    continue;
                }

                Assert.Null(referenceItems[i].DefinitionText);
                Assert.Equal(0, referenceItems[i].DefinitionIcon.ImageId.Id);
                Assert.Equal(definitionId, referenceItems[i].DefinitionId);
                Assert.NotEqual(definitionId, referenceItems[i].Id);
            }
        }

        private static void AssertHighlightCount(
            LSP.VSReferenceItem[] referenceItems,
            int expectedDefinitionCount,
            int expectedWrittenReferenceCount,
            int expectedReferenceCount)
        {
            var actualDefinitionCount = referenceItems.Select(
                item => ((ClassifiedTextElement)item.Text).Runs.Where(run => run.MarkerTagType == DefinitionHighlightTag.TagId)).Where(i => i.Any()).Count();
            var actualWrittenReferenceCount = referenceItems.Select(
                item => ((ClassifiedTextElement)item.Text).Runs.Where(run => run.MarkerTagType == WrittenReferenceHighlightTag.TagId)).Where(i => i.Any()).Count();
            var actualReferenceCount = referenceItems.Select(
                item => ((ClassifiedTextElement)item.Text).Runs.Where(run => run.MarkerTagType == ReferenceHighlightTag.TagId)).Where(i => i.Any()).Count();

            Assert.Equal(expectedDefinitionCount, actualDefinitionCount);
            Assert.Equal(expectedWrittenReferenceCount, actualWrittenReferenceCount);
            Assert.Equal(expectedReferenceCount, actualReferenceCount);
        }
    }
}
