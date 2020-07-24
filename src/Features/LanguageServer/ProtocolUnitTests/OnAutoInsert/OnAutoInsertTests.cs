// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.OnAutoInsert
{
    public class OnAutoInsertTests : AbstractLanguageServerProtocolTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task OnAutoInsert_CommentCharacter()
        {
            var markup =
@"class A
{
    ///{|type:|}
    void M()
    {
    }
}";
            var expected =
@"class A
{
    /// <summary>
    /// $0
    /// </summary>
    void M()
    {
    }
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var characterTyped = "/";
            var locationTyped = locations["type"].Single();
            var documentText = await workspace.CurrentSolution.GetDocuments(locationTyped.Uri).Single().GetTextAsync();

            var result = await RunOnAutoInsertAsync(workspace.CurrentSolution, characterTyped, locationTyped);
            Assert.Equal(InsertTextFormat.Snippet, result.TextEditFormat);
            var actualText = ApplyTextEdits(new[] { result.TextEdit }, documentText);
            Assert.Equal(expected, actualText);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DocumentationComments)]
        public async Task OnAutoInsert_EnterKey()
        {
            var markup =
@"class A
{
    /// <summary>
    /// Foo
    /// </summary>
{|type:|}
    void M()
    {
    }
}";
            var expected =
@"class A
{
    /// <summary>
    /// Foo
    /// </summary>
    /// $0
    void M()
    {
    }
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var characterTyped = "\n";
            var locationTyped = locations["type"].Single();
            var documentText = await workspace.CurrentSolution.GetDocuments(locationTyped.Uri).Single().GetTextAsync();

            var result = await RunOnAutoInsertAsync(workspace.CurrentSolution, characterTyped, locationTyped);
            Assert.Equal(InsertTextFormat.Snippet, result.TextEditFormat);
            var actualText = ApplyTextEdits(new[] { result.TextEdit }, documentText);
            Assert.Equal(expected, actualText);
        }

        private static async Task<LSP.DocumentOnAutoInsertResponseItem> RunOnAutoInsertAsync(Solution solution, string characterTyped, LSP.Location locationTyped)
            => await GetLanguageServer(solution)
            .ExecuteRequestAsync<LSP.DocumentOnAutoInsertParams, LSP.DocumentOnAutoInsertResponseItem>(MSLSPMethods.OnAutoInsertName,
                CreateDocumentOnAutoInsertParams(characterTyped, locationTyped), new LSP.ClientCapabilities(), null, CancellationToken.None);

        private static LSP.DocumentOnAutoInsertParams CreateDocumentOnAutoInsertParams(string characterTyped, LSP.Location locationTyped)
            => new LSP.DocumentOnAutoInsertParams()
            {
                Position = locationTyped.Range.Start,
                Character = characterTyped,
                TextDocument = CreateTextDocumentIdentifier(locationTyped.Uri)
            };
    }
}
