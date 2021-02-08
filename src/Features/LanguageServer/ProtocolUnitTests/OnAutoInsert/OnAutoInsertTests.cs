// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
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

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task OnAutoInsert_BraceFormatting()
        {
            // The test starts with the closing brace already on a new line.
            // In LSP, hitting enter will first trigger a didChange event for the new line character
            // (bringing the server text to the form below) and then trigger OnAutoInsert
            // for the new line character.
            var markup =
@"class A
{
    void M() {{|type:|}
    }
}";
            var expected =
@"class A
{
    void M()
    {
        $0
    }
}";
            await VerifyMarkupAndExpected("\n", markup, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task OnAutoInsert_BraceFormattingWithTabs()
        {
            var markup =
@"class A
{
    void M() {{|type:|}
    }
}";
            // Use show whitespace when modifying the expected value.
            // The method braces and caret location should be indented with tabs.
            var expected =
@"class A
{
    void M()
	{
		$0
	}
}";
            await VerifyMarkupAndExpected("\n", markup, expected, useTabs: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task OnAutoInsert_BraceFormattingInsideMethod()
        {
            var markup =
@"class A
{
    void M()
    {
        if (true) {{|type:|}
        }
    }
}";
            var expected =
@"class A
{
    void M()
    {
        if (true)
        {
            $0
        }
    }
}";
            await VerifyMarkupAndExpected("\n", markup, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task OnAutoInsert_BraceFormattingNoResultInInterpolation()
        {
            var markup =
@"class A
{
    void M()
    {
        var s = $""Hello {{|type:|}
        }
}";
            await VerifyNoResult("\n", markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        [WorkItem(1260219, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1260219")]
        public async Task OnAutoInsert_BraceFormattingDoesNotInsertExtraEmptyLines()
        {
            // The test starts with the closing brace already on a new line.
            // In LSP, hitting enter will first trigger a didChange event for the new line character
            // (bringing the server text to the form below) and then trigger OnAutoInsert
            // for the new line character.
            var markup =
@"class A
{
    void M()
    {
        
        {|type:|}
    }
}";
            await VerifyNoResult("\n", markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        [WorkItem(1260219, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1260219")]
        public async Task OnAutoInsert_BraceFormattingDoesNotMoveCaretOnEnterInsideBraces()
        {
            // The test starts with the closing brace already on a new line.
            // In LSP, hitting enter will first trigger a didChange event for the new line character
            // (bringing the server text to the form below) and then trigger OnAutoInsert
            // for the new line character.
            var markup =
@"class A
{
    void M()
    {{|type:|}


    }
}";
            await VerifyNoResult("\n", markup);
        }

        private async Task VerifyMarkupAndExpected(string characterTyped, string markup, string expected, bool useTabs = false)
        {
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var locationTyped = locations["type"].Single();

            if (useTabs)
            {
                var newSolution = workspace.CurrentSolution.WithOptions(
                    workspace.CurrentSolution.Options.WithChangedOption(CodeAnalysis.Formatting.FormattingOptions.UseTabs, LanguageNames.CSharp, useTabs));
                workspace.TryApplyChanges(newSolution);
            }

            var document = workspace.CurrentSolution.GetDocuments(locationTyped.Uri).Single();
            var documentText = await document.GetTextAsync();

            var result = await RunOnAutoInsertAsync(workspace.CurrentSolution, characterTyped, locationTyped);

            AssertEx.NotNull(result);
            Assert.Equal(InsertTextFormat.Snippet, result.TextEditFormat);
            var actualText = ApplyTextEdits(new[] { result.TextEdit }, documentText);
            Assert.Equal(expected, actualText);
        }

        private async Task VerifyNoResult(string characterTyped, string markup)
        {
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var locationTyped = locations["type"].Single();
            var documentText = await workspace.CurrentSolution.GetDocuments(locationTyped.Uri).Single().GetTextAsync();

            var result = await RunOnAutoInsertAsync(workspace.CurrentSolution, characterTyped, locationTyped);

            Assert.Null(result);
        }

        private static async Task<LSP.DocumentOnAutoInsertResponseItem?> RunOnAutoInsertAsync(Solution solution, string characterTyped, LSP.Location locationTyped)
        {
            var queue = CreateRequestQueue(solution);
            return await GetLanguageServer(solution).ExecuteRequestAsync<LSP.DocumentOnAutoInsertParams, LSP.DocumentOnAutoInsertResponseItem?>(queue, MSLSPMethods.OnAutoInsertName,
                           CreateDocumentOnAutoInsertParams(characterTyped, locationTyped), new LSP.ClientCapabilities(), null, CancellationToken.None);
        }

        private static LSP.DocumentOnAutoInsertParams CreateDocumentOnAutoInsertParams(string characterTyped, LSP.Location locationTyped)
            => new LSP.DocumentOnAutoInsertParams()
            {
                Position = locationTyped.Range.Start,
                Character = characterTyped,
                TextDocument = CreateTextDocumentIdentifier(locationTyped.Uri)
            };
    }
}
