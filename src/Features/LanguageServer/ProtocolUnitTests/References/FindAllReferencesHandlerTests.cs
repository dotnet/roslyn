// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Text.Adornments;
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

            AssertValidDefinitionProperties(results, 0, Glyph.FieldPublic);
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/43063")]
        public async Task TestFindAllReferencesAsync_Class()
        {
            var markup =
@"class {|reference:A|}
{
    public int someInt = 1;
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
            Assert.Equal("B", results[1].ContainingType);
            Assert.Equal("B", results[2].ContainingType);
            Assert.Equal("M2", results[2].ContainingMember);

            AssertValidDefinitionProperties(results, 0, Glyph.ClassInternal);
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

            AssertValidDefinitionProperties(results, 0, Glyph.FieldPublic);
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

        private static LSP.ReferenceParams CreateReferenceParams(LSP.Location caret, IProgress<object> progress) =>
            new LSP.ReferenceParams()
            {
                TextDocument = CreateTextDocumentIdentifier(caret.Uri),
                Position = caret.Range.Start,
                Context = new LSP.ReferenceContext(),
                PartialResultToken = progress
            };

        private static async Task<LSP.VSReferenceItem[]> RunFindAllReferencesAsync(Solution solution, LSP.Location caret)
        {
            var vsClientCapabilities = new LSP.VSClientCapabilities
            {
                SupportsVisualStudioExtensions = true
            };

            var progress = new ProgressCollector<LSP.VSReferenceItem>();

            var queue = CreateRequestQueue(solution);
            await GetLanguageServer(solution).ExecuteRequestAsync<LSP.ReferenceParams, LSP.VSReferenceItem[]>(queue, LSP.Methods.TextDocumentReferencesName,
                CreateReferenceParams(caret, progress), vsClientCapabilities, null, CancellationToken.None);

            return progress.GetItems();
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

        private sealed class ProgressCollector<T> : IProgress<object>
        {
            private readonly List<T> _items = new List<T>();

            public T[] GetItems() => _items.ToArray();

            public void Report(object value)
            {
                _items.AddRange((T[])value);
            }
        }
    }
}
