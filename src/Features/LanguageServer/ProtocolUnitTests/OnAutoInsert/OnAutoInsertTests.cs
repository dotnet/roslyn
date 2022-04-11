// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
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
        public async Task OnAutoInsert_CommentCharacter_WithComment()
        {
            var markup =
@"class A
{
    ///{|type:|} This is an existing comment
    void M()
    {
    }
}";
            var expected =
@"class A
{
    /// <summary>
    /// $0This is an existing comment
    /// </summary>
    void M()
    {
    }
}";
            await VerifyMarkupAndExpected("/", markup, expected);
        }

        [Fact]
        public async Task OnAutoInsert_CommentCharacter_WithComment_NoSpace()
        {
            var markup =
@"class A
{
    ///{|type:|}This is an existing comment
    void M()
    {
    }
}";
            var expected =
@"class A
{
    /// <summary>
    /// $0This is an existing comment
    /// </summary>
    void M()
    {
    }
}";
            await VerifyMarkupAndExpected("/", markup, expected);
        }

        [Fact]
        public async Task OnAutoInsert_CommentCharacter_VB()
        {
            var markup =
@"Class A
    '''{|type:|}
    Sub M()
    End Sub
End Class";
            var expected =
@"Class A
    ''' <summary>
    ''' $0
    ''' </summary>
    Sub M()
    End Sub
End Class";
            await VerifyMarkupAndExpected("'", markup, expected, languageName: LanguageNames.VisualBasic);
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
            await VerifyMarkupAndExpected("\n", markup, expected, serverKind: WellKnownLspServerKinds.RazorLspServer);
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
            await VerifyMarkupAndExpected("\n", markup, expected, insertSpaces: false, tabSize: 4, serverKind: WellKnownLspServerKinds.RazorLspServer);
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
            await VerifyMarkupAndExpected("\n", markup, expected, serverKind: WellKnownLspServerKinds.RazorLspServer);
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

        private async Task VerifyMarkupAndExpected(
            string characterTyped,
            string markup,
            string expected,
            bool insertSpaces = true,
            int tabSize = 4,
            string languageName = LanguageNames.CSharp,
            WellKnownLspServerKinds serverKind = WellKnownLspServerKinds.AlwaysActiveVSLspServer)
        {
            Task<TestLspServer> testLspServerTask;
            if (languageName == LanguageNames.CSharp)
            {
                testLspServerTask = CreateTestLspServerAsync(markup, CapabilitiesWithVSExtensions, serverKind);
            }
            else if (languageName == LanguageNames.VisualBasic)
            {
                testLspServerTask = CreateVisualBasicTestLspServerAsync(markup, CapabilitiesWithVSExtensions, serverKind);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(languageName);
            }

            using var testLspServer = await testLspServerTask;
            var locationTyped = testLspServer.GetLocations("type").Single();

            var document = testLspServer.GetCurrentSolution().GetDocuments(locationTyped.Uri).Single();
            var documentText = await document.GetTextAsync();

            var result = await RunOnAutoInsertAsync(testLspServer, characterTyped, locationTyped, insertSpaces, tabSize);

            AssertEx.NotNull(result);
            Assert.Equal(InsertTextFormat.Snippet, result.TextEditFormat);
            var actualText = ApplyTextEdits(new[] { result.TextEdit }, documentText);
            Assert.Equal(expected, actualText);
        }

        private async Task VerifyNoResult(string characterTyped, string markup, bool insertSpaces = true, int tabSize = 4)
        {
            using var testLspServer = await CreateTestLspServerAsync(markup);
            var locationTyped = testLspServer.GetLocations("type").Single();
            var documentText = await testLspServer.GetCurrentSolution().GetDocuments(locationTyped.Uri).Single().GetTextAsync();

            var result = await RunOnAutoInsertAsync(testLspServer, characterTyped, locationTyped, insertSpaces, tabSize);

            Assert.Null(result);
        }

        private static async Task<LSP.VSInternalDocumentOnAutoInsertResponseItem?> RunOnAutoInsertAsync(
            TestLspServer testLspServer,
            string characterTyped,
            LSP.Location locationTyped,
            bool insertSpaces,
            int tabSize)
        {
            return await testLspServer.ExecuteRequestAsync<LSP.VSInternalDocumentOnAutoInsertParams, LSP.VSInternalDocumentOnAutoInsertResponseItem?>(VSInternalMethods.OnAutoInsertName,
                CreateDocumentOnAutoInsertParams(characterTyped, locationTyped, insertSpaces, tabSize), CancellationToken.None);
        }

        private static LSP.VSInternalDocumentOnAutoInsertParams CreateDocumentOnAutoInsertParams(
            string characterTyped,
            LSP.Location locationTyped,
            bool insertSpaces,
            int tabSize)
            => new LSP.VSInternalDocumentOnAutoInsertParams
            {
                Position = locationTyped.Range.Start,
                Character = characterTyped,
                TextDocument = CreateTextDocumentIdentifier(locationTyped.Uri),
                Options = new LSP.FormattingOptions
                {
                    InsertSpaces = insertSpaces,
                    TabSize = tabSize
                }
            };
    }
}
