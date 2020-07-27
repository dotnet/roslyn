// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.OnAutoInsert
{
    public class OnAutoInsertTests : AbstractLanguageServerProtocolTests
    {
        [Fact]
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
            await VerifyMarkupAndExpected("/", markup, expected);
        }

        [Fact]
        public async Task OnAutoInsert_ParametersAndReturns()
        {
            var markup =
@"class A
{
    ///{|type:|}
    string M(int foo, bool bar)
    {
    }
}";
            var expected =
@"class A
{
    /// <summary>
    /// $0
    /// </summary>
    /// <param name=""foo""></param>
    /// <param name=""bar""></param>
    /// <returns></returns>
    string M(int foo, bool bar)
    {
    }
}";
            await VerifyMarkupAndExpected("/", markup, expected);
        }

        [Fact]
        public async Task OnAutoInsert_CommentCharacterInsideMethod_Ignored()
        {
            var markup =
@"class A
{
    void M()
    {
        ///{|type:|}
    }
}";
            await VerifyNoResult("/", markup);
        }

        [Fact]
        public async Task OnAutoInsert_VisualBasicCommentCharacter_Ignored()
        {
            var markup =
@"class A
{
    '''{|type:|}
    void M()
    {
    }
}";
            await VerifyNoResult("'", markup);
        }

        [Fact]
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
            await VerifyMarkupAndExpected("\n", markup, expected);
        }

        [Fact]
        public async Task OnAutoInsert_EnterKey2()
        {
            var markup =
@"class A
{
    /// <summary>
    /// Foo
{|type:|}
    /// </summary>
    void M()
    {
    }
}";
            var expected =
@"class A
{
    /// <summary>
    /// Foo
    /// $0
    /// </summary>
    void M()
    {
    }
}";
            await VerifyMarkupAndExpected("\n", markup, expected);
        }

        [Fact]
        public async Task OnAutoInsert_EnterKey3()
        {
            var markup =
@"class A
{
    ///
{|type:|}
    string M(int foo, bool bar)
    {
    }
}";
            var expected =
@"class A
{
    /// <summary>
    /// $0
    /// </summary>
    /// <param name=""foo""></param>
    /// <param name=""bar""></param>
    /// <returns></returns>
    string M(int foo, bool bar)
    {
    }
}";
            await VerifyMarkupAndExpected("\n", markup, expected);
        }

        private async Task VerifyMarkupAndExpected(string characterTyped, string markup, string expected)
        {
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var locationTyped = locations["type"].Single();
            var documentText = await workspace.CurrentSolution.GetDocuments(locationTyped.Uri).Single().GetTextAsync();

            var results = await RunOnAutoInsertAsync(workspace.CurrentSolution, characterTyped, locationTyped);

            Assert.Single(results);
            var result = results[0];
            Assert.Equal(InsertTextFormat.Snippet, result.TextEditFormat);
            var actualText = ApplyTextEdits(new[] { result.TextEdit }, documentText);
            Assert.Equal(expected, actualText);
        }

        private async Task VerifyNoResult(string characterTyped, string markup)
        {
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var locationTyped = locations["type"].Single();
            var documentText = await workspace.CurrentSolution.GetDocuments(locationTyped.Uri).Single().GetTextAsync();

            var results = await RunOnAutoInsertAsync(workspace.CurrentSolution, characterTyped, locationTyped);

            Assert.Empty(results);
        }

        private static async Task<LSP.DocumentOnAutoInsertResponseItem[]> RunOnAutoInsertAsync(Solution solution, string characterTyped, LSP.Location locationTyped)
            => await GetLanguageServer(solution)
            .ExecuteRequestAsync<LSP.DocumentOnAutoInsertParams, LSP.DocumentOnAutoInsertResponseItem[]>(MSLSPMethods.OnAutoInsertName,
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
