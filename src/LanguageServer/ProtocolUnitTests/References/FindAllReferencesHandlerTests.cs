// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.ReferenceHighlighting;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Roslyn.Test.Utilities;
using Roslyn.Text.Adornments;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.References
{
    public class FindAllReferencesHandlerTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerProtocolTests(testOutputHelper)
    {
        [Theory, CombinatorialData]
        public async Task TestFindAllReferencesAsync(bool mutatingLspWorkspace)
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
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);

            var results = await RunFindAllReferencesAsync(testLspServer, testLspServer.GetLocations("caret").First());
            AssertLocationsEqual(testLspServer.GetLocations("reference"), results.Select(result => result.Location));

            Assert.Equal("A", results[0].ContainingType);
            Assert.Equal("B", results[2].ContainingType);
            Assert.Equal("M", results[1].ContainingMember);
            Assert.Equal("M2", results[3].ContainingMember);

            AssertValidDefinitionProperties(results, 0, Glyph.FieldPublic);
            AssertHighlightCount(results, expectedDefinitionCount: 1, expectedWrittenReferenceCount: 0, expectedReferenceCount: 3);
        }

        [Theory, CombinatorialData]
        public async Task TestFindAllReferencesAsync_Streaming(bool mutatingLspWorkspace)
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
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);

            using var progress = BufferedProgress.Create<object>(null);

            var results = await RunFindAllReferencesAsync(testLspServer, testLspServer.GetLocations("caret").First(), progress);

            Assert.NotNull(results);
            Assert.NotEmpty(results);

            AssertLocationsEqual(testLspServer.GetLocations("reference"), results.Select(result => result.Location));

            Assert.Equal("A", results[0].ContainingType);
            Assert.Equal("B", results[2].ContainingType);
            Assert.Equal("M", results[1].ContainingMember);
            Assert.Equal("M2", results[3].ContainingMember);

            AssertValidDefinitionProperties(results, 0, Glyph.FieldPublic);
            AssertHighlightCount(results, expectedDefinitionCount: 1, expectedWrittenReferenceCount: 0, expectedReferenceCount: 3);
        }

        [Theory, CombinatorialData]
        public async Task TestFindAllReferencesAsync_Class(bool mutatingLspWorkspace)
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
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);

            var results = await RunFindAllReferencesAsync(testLspServer, testLspServer.GetLocations("caret").First());
            AssertLocationsEqual(testLspServer.GetLocations("reference"), results.Select(result => result.Location));

            var textElement = results[0].Text as ClassifiedTextElement;
            Assert.NotNull(textElement);
            var actualText = string.Concat(textElement.Runs.Select(r => r.Text));

            Assert.Equal("class A", actualText);

            Assert.Equal("B", results[1].ContainingType);
            Assert.Equal("B", results[2].ContainingType);
            Assert.Equal("M2", results[2].ContainingMember);

            AssertValidDefinitionProperties(results, 0, Glyph.ClassInternal);
            AssertHighlightCount(results, expectedDefinitionCount: 1, expectedWrittenReferenceCount: 0, expectedReferenceCount: 2);
        }

        [Theory, CombinatorialData]
        public async Task TestFindAllReferencesAsync_MultipleDocuments(bool mutatingLspWorkspace)
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

            await using var testLspServer = await CreateTestLspServerAsync(markups, mutatingLspWorkspace, new InitializationOptions { ClientCapabilities = CapabilitiesWithVSExtensions });

            var results = await RunFindAllReferencesAsync(testLspServer, testLspServer.GetLocations("caret").First());
            AssertLocationsEqual(testLspServer.GetLocations("reference"), results.Select(result => result.Location));

            Assert.Equal("A", results[0].ContainingType);
            Assert.Equal("B", results[2].ContainingType);
            Assert.Equal("M", results[1].ContainingMember);
            Assert.Equal("M2", results[3].ContainingMember);

            AssertValidDefinitionProperties(results, 0, Glyph.FieldPublic);
            AssertHighlightCount(results, expectedDefinitionCount: 1, expectedWrittenReferenceCount: 0, expectedReferenceCount: 3);
        }

        [Theory, CombinatorialData]
        public async Task TestFindAllReferencesAsync_InvalidLocation(bool mutatingLspWorkspace)
        {
            var markup =
@"class A
{
    {|caret:|}
}";
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);

            var results = await RunFindAllReferencesAsync(testLspServer, testLspServer.GetLocations("caret").First());
            Assert.Empty(results);
        }

        [Theory, CombinatorialData]
        public async Task TestFindAllReferencesMetadataDefinitionAsync(bool mutatingLspWorkspace)
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
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);

            var results = await RunFindAllReferencesAsync(testLspServer, testLspServer.GetLocations("caret").First());
            Assert.NotNull(results[0].Location.Uri);
            AssertHighlightCount(results, expectedDefinitionCount: 0, expectedWrittenReferenceCount: 0, expectedReferenceCount: 1);
        }

        [Theory, CombinatorialData, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1240061/")]
        public async Task TestFindAllReferencesAsync_Namespace(bool mutatingLspWorkspace)
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
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);

            var results = await RunFindAllReferencesAsync(testLspServer, testLspServer.GetLocations("caret").First());

            // Namespace source definitions and references should have locations:
            Assert.True(results.All(r => r.Location != null));

            AssertValidDefinitionProperties(results, 0, Glyph.Namespace);
            AssertHighlightCount(results, expectedDefinitionCount: 1, expectedWrittenReferenceCount: 0, expectedReferenceCount: 2);
        }

        [Theory, CombinatorialData, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1245616/")]
        public async Task TestFindAllReferencesAsync_Highlights(bool mutatingLspWorkspace)
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
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);

            var results = await RunFindAllReferencesAsync(testLspServer, testLspServer.GetLocations("caret").First());
            AssertHighlightCount(results, expectedDefinitionCount: 1, expectedWrittenReferenceCount: 1, expectedReferenceCount: 1);
        }

        [Theory, CombinatorialData]
        public async Task TestFindAllReferencesAsync_StaticClassification(bool mutatingLspWorkspace)
        {
            var markup =
@"static class {|caret:|}{|reference:C|} { }
";
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);

            var results = await RunFindAllReferencesAsync(testLspServer, testLspServer.GetLocations("caret").First());

            // Ensure static definitions and references are only classified once
            var textRuns = ((ClassifiedTextElement)results.First().Text).Runs;
            Assert.Equal(9, textRuns.Count());
        }

        private static LSP.ReferenceParams CreateReferenceParams(LSP.Location caret, IProgress<object> progress)
            => new LSP.ReferenceParams()
            {
                TextDocument = CreateTextDocumentIdentifier(caret.Uri),
                Position = caret.Range.Start,
                Context = new LSP.ReferenceContext(),
                PartialResultToken = progress
            };

        internal static async Task<LSP.VSInternalReferenceItem[]> RunFindAllReferencesAsync(TestLspServer testLspServer, LSP.Location caret, BufferedProgress<object>? progress = null)
        {
            var results = await testLspServer.ExecuteRequestAsync<LSP.ReferenceParams, LSP.VSInternalReferenceItem[]>(LSP.Methods.TextDocumentReferencesName,
                CreateReferenceParams(caret, progress), CancellationToken.None);
            // Results are returned in a non-deterministic order, so we order them by location
            var orderedResults = results?.OrderBy(r => r.Location, new OrderLocations()).ToArray();

            // If we're using progress, we need to return the results from the progress object.
            if (progress != null)
            {
                Assert.Null(orderedResults);
                // BufferedProgress wraps individual elements in an array, so when they are nested them like this,
                // with the test creating one, and the handler another, we have to unwrap.
                // Additionally, the VS LSP protocol specifies T from IProgress<T> as an object and not as the actual VSInternalReferenceItem
                // so we have to correctly convert the JObject into the expected type.
                orderedResults = progress.Value.GetValues()
                    .SelectMany(r => (List<object>)r).Select(r => JsonSerializer.Deserialize<LSP.VSInternalReferenceItem>((JsonElement)r, ProtocolConversions.LspJsonSerializerOptions))
                    .OrderBy(r => r.Location, new OrderLocations())
                    .ToArray();
            }

            return orderedResults;
        }

        internal static async Task<LSP.Location[]> RunFindAllReferencesNonVSAsync(TestLspServer testLspServer, LSP.Location caret, BufferedProgress<object>? progress = null)
        {
            var results = await testLspServer.ExecuteRequestAsync<LSP.ReferenceParams, LSP.Location[]>(LSP.Methods.TextDocumentReferencesName,
                CreateReferenceParams(caret, progress), CancellationToken.None);
            // Results are returned in a non-deterministic order, so we order them by location
            var orderedResults = results.OrderBy(r => r, new OrderLocations()).ToArray();

            // If we're using progress, we need to return the results from the progress object.
            if (progress != null)
            {
                Assert.Null(orderedResults);
                // BufferedProgress wraps individual elements in an array, so when they are nested them like this,
                // with the test creating one, and the handler another, we have to unwrap.
                // Additionally, the VS LSP protocol specifies T from IProgress<T> as an object and not as the actual LSP.Location
                // so we have to correctly convert the JObject into the expected type.
                orderedResults = progress.Value.GetValues()
                    .SelectMany(r => (List<object>)r).Select(r => JsonSerializer.Deserialize<LSP.Location>((JsonElement)r, ProtocolConversions.LspJsonSerializerOptions))
                    .OrderBy(r => r, new OrderLocations())
                    .ToArray();
            }

            return orderedResults;
        }

        private static void AssertValidDefinitionProperties(LSP.VSInternalReferenceItem[] referenceItems, int definitionIndex, Glyph definitionGlyph)
        {
            var definition = referenceItems[definitionIndex];
            var definitionId = definition.DefinitionId;
            Assert.NotNull(definition.DefinitionText);

            Assert.Equal(definitionGlyph.GetImageId().Guid, definition.DefinitionIcon.ImageId.Guid);
            Assert.Equal(definitionGlyph.GetImageId().Id, definition.DefinitionIcon.ImageId.Id);

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
            LSP.VSInternalReferenceItem[] referenceItems,
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
