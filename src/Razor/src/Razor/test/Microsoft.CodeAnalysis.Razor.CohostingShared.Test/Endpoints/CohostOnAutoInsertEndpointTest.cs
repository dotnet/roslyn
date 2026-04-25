// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.AutoInsert;
using Microsoft.CodeAnalysis.Razor.Settings;
using Microsoft.CodeAnalysis.Remote.Razor.AutoInsert;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Razor.LanguageClient.Cohost;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public class CohostOnAutoInsertEndpointTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Theory]
    [InlineData("PageTitle")]
    [InlineData("div")]
    [InlineData("text")]
    public async Task EndTag(string startTag)
    {
        await VerifyOnAutoInsertAsync(
            input: $"""
                This is a Razor document.

                <{startTag}>$$

                The end.
                """,
            output: $"""
                This is a Razor document.

                <{startTag}>$0</{startTag}>

                The end.
                """,
            triggerCharacter: ">");
    }

    [Theory]
    [InlineData("PageTitle")]
    [InlineData("div")]
    [InlineData("text")]
    public async Task EndTag_InCSharp(string startTag)
    {
        await VerifyOnAutoInsertAsync(
            input: $$"""
                <div>
                    @if (true)
                    {
                        <{{startTag}}>$$
                    }
                </div>
                """,
            output: $$"""
                <div>
                    @if (true)
                    {
                        <{{startTag}}>$0</{{startTag}}>
                    }
                </div>
                """,
            triggerCharacter: ">");
    }

    [Fact]
    public async Task EndTag_AlreadyExists()
    {
        await VerifyOnAutoInsertAsync(
            input: """
                This is a Razor document.

                <test>$$<test></test></test>

                The end.
                """,
            output: null,
            triggerCharacter: ">");
    }

    [Fact]
    public async Task EndTag_TagStructure_WithoutEndTag()
    {
        await VerifyOnAutoInsertAsync(
            input: """
                This is a Razor document.

                <area href="~/foo">$$

                The end.
                """,
            output: """
                This is a Razor document.

                <area href="~/foo" />

                The end.
                """,
            triggerCharacter: ">",
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task EndTag_TagStructure_WithoutEndTag_AlreadyExists()
    {
        await VerifyOnAutoInsertAsync(
            input: """
                This is a Razor document.

                <area href="~/foo">$$</area>

                The end.
                """,
            output: """
                This is a Razor document.

                <area href="~/foo" /></area>

                The end.
                """,
            triggerCharacter: ">",
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task EndTag_CloseOutOfScope()
    {
        await VerifyOnAutoInsertAsync(
            input: """
                <div>
                    @if (true)
                    {
                        <div>$$</div>
                    }
                """,
            output: """
                <div>
                    @if (true)
                    {
                        <div>$0</div></div>
                    }
                """,
            triggerCharacter: ">");
    }

    [Fact]
    public async Task EndTag_VoidElement()
    {
        await VerifyOnAutoInsertAsync(
            input: """
                This is a Razor document.

                <input>$$

                The end.
                """,
            output: """
                This is a Razor document.

                <input />

                The end.
                """,
            triggerCharacter: ">");
    }

    [Fact]
    public async Task EndTag_VoidElement_CaseInsensitive()
    {
        await VerifyOnAutoInsertAsync(
            input: """
                This is a Razor document.

                <Input>$$

                The end.
                """,
            output: """
                This is a Razor document.

                <Input />

                The end.
                """,
            triggerCharacter: ">");
    }

    [Fact]
    public async Task EndTag_Nested()
    {
        await VerifyOnAutoInsertAsync(
            input: """
                This is a Razor document.

                <div><test>$$</div>

                The end.
                """,
            output: """
                This is a Razor document.

                <div><test>$0</test></div>

                The end.
                """,
            triggerCharacter: ">");
    }

    [Fact]
    public async Task EndTag_Nested_WithAttribute()
    {
        await VerifyOnAutoInsertAsync(
            input: """
                This is a Razor document.

                <div><a target="_blank">$$</div>

                The end.
                """,
            output: """
                This is a Razor document.

                <div><a target="_blank">$0</a></div>

                The end.
                """,
            triggerCharacter: ">");
    }

    [Fact]
    public async Task EndTag_Nested_WithAttribute_WithSpace()
    {
        await VerifyOnAutoInsertAsync(
            input: """
                This is a Razor document.

                <div><a target="_blank" >$$</div>

                The end.
                """,
            output: """
                This is a Razor document.

                <div><a target="_blank" >$0</a></div>

                The end.
                """,
            triggerCharacter: ">");
    }

    [Fact]
    public async Task EndTag_Nested_WithMinimizedAttribute()
    {
        await VerifyOnAutoInsertAsync(
            input: """
                This is a Razor document.

                <div><form novalidate>$$</div>

                The end.
                """,
            output: """
                This is a Razor document.

                <div><form novalidate>$0</form></div>

                The end.
                """,
            triggerCharacter: ">");
    }

    [Fact]
    public async Task EndTag_Nested_WithMinimizedAttribute_WithSpace()
    {
        await VerifyOnAutoInsertAsync(
            input: """
                This is a Razor document.

                <div><form novalidate >$$</div>

                The end.
                """,
            output: """
                This is a Razor document.

                <div><form novalidate >$0</form></div>

                The end.
                """,
            triggerCharacter: ">");
    }

    [Fact]
    public async Task EndTag_Nested_VoidElement()
    {
        await VerifyOnAutoInsertAsync(
            input: """
                This is a Razor document.

                <test><input>$$</test>

                The end.
                """,
            output: """
                This is a Razor document.

                <test><input /></test>

                The end.
                """,
            triggerCharacter: ">");
    }

    [Fact]
    public async Task EndTag_VoidElement_AlreadyClosed()
    {
        await VerifyOnAutoInsertAsync(
            input: """
                This is a Razor document.

                <input />$$

                The end.
                """,
            output: null,
            triggerCharacter: ">");
    }

    [Theory]
    [InlineData("PageTitle")]
    [InlineData("div")]
    [InlineData("text")]
    public async Task DoNotAutoInsertEndTag_DisabledAutoClosingTags(string startTag)
    {
        await VerifyOnAutoInsertAsync(
            input: $"""
                This is a Razor document.

                <{startTag}>$$

                The end.
                """,
            output: null,
            triggerCharacter: ">",
            autoClosingTags: false);
    }

    [Fact]
    public async Task AttributeQuotes()
    {
        await VerifyOnAutoInsertAsync(
            input: $"""
                This is a Razor document.

                <PageTitle style=$$></PageTitle>

                The end.
                """,
            output: $"""
                This is a Razor document.

                <PageTitle style="$0"></PageTitle>

                The end.
                """,
            triggerCharacter: "=",
            delegatedResponseText: "\"$0\"");
    }

    [Fact]
    public async Task CSharp_RawStringLiteral()
    {
        await VerifyOnAutoInsertAsync(
            input: """"
                @code {
                    void TestMethod() {
                        var x = """$$
                    }
                }
                """",
            output: """""""
                @code {
                    void TestMethod() {
                        var x = """$0"""
                    }
                }
                """"""",
            triggerCharacter: "\"");
    }

    [Fact]
    public async Task CSharp_OnForwardSlash()
    {
        await VerifyOnAutoInsertAsync(
            input: """
                @code {
                    ///$$
                    void TestMethod() {}
                }
                """,
            output: """
                @code {
                    /// <summary>
                    /// $0
                    /// </summary>
                    void TestMethod() {}
                }
                """,
            triggerCharacter: "/");
    }

    [Fact]
    public async Task CSharp_DocComment_OnEnter()
    {
        await VerifyOnAutoInsertAsync(
            input: """
                @code {
                    /// <summary>
                    /// This is some text
                    $$
                    /// </summary>
                    void TestMethod() {}
                }
                """,
            output: """
                @code {
                    /// <summary>
                    /// This is some text
                    /// $0
                    /// </summary>
                    void TestMethod() {}
                }
                """,
            triggerCharacter: "\n");
    }

    [Fact]
    public async Task DoNotAutoInsertCSharp_OnForwardSlashWithFormatOnTypeDisabled()
    {
        await VerifyOnAutoInsertAsync(
            input: """
                @code {
                    ///$$
                    void TestMethod() {}
                }
                """,
            output: null,
            triggerCharacter: "/",
            formatOnType: false);
    }

    [Fact]
    public async Task CSharp_OnEnter()
    {
        await VerifyOnAutoInsertAsync(
            input: """
                Hello
                <div>
                    Hello
                    <p>Hello</p>
                    <p class="@DateTime.Now.DayOfWeek">Hello</p>
                </div>

                Hello

                @code {
                    void TestMethod() {
                $$}
                }
                """,
            output: """
                Hello
                <div>
                    Hello
                    <p>Hello</p>
                    <p class="@DateTime.Now.DayOfWeek">Hello</p>
                </div>

                Hello
                
                @code {
                    void TestMethod()
                    {
                        $0
                    }
                }
                """,
            triggerCharacter: "\n");
    }

    [Fact]
    public async Task CSharp_OnEnter_TwoSpaceIndent()
    {
        await VerifyOnAutoInsertAsync(
            input: """
                @code {
                    void TestMethod() {
                $$}
                }
                """,
            output: """
                @code {
                  void TestMethod()
                  {
                    $0
                  }
                }
                """,
            triggerCharacter: "\n",
            tabSize: 2);
    }

    [Fact]
    public async Task CSharp_OnEnter_UseTabs()
    {
        const char tab = '\t';
        await VerifyOnAutoInsertAsync(
            input: """
                @code {
                    void TestMethod() {
                $$}
                }
                """,
            output: $$"""
                @code {
                {{tab}}void TestMethod()
                {{tab}}{
                {{tab}}{{tab}}$0
                {{tab}}}
                }
                """,
            triggerCharacter: "\n",
            insertSpaces: false);
    }

    private async Task VerifyOnAutoInsertAsync(
        TestCode input,
        string? output,
        string triggerCharacter,
        string? delegatedResponseText = null,
        bool insertSpaces = true,
        int tabSize = 4,
        bool formatOnType = true,
        bool autoClosingTags = true,
        RazorFileKind? fileKind = null)
    {
        fileKind ??= RazorFileKind.Component;
        var document = CreateProjectAndRazorDocument(input.Text, fileKind: fileKind);
        var sourceText = await document.GetTextAsync(DisposalToken);

        ClientSettingsManager.Update(ClientAdvancedSettings.Default with { FormatOnType = formatOnType, AutoClosingTags = autoClosingTags });

        IOnAutoInsertTriggerCharacterProvider[] onAutoInsertTriggerCharacterProviders = [
            new RemoteAutoClosingTagOnAutoInsertProvider(),
            new RemoteCloseTextTagOnAutoInsertProvider()];

        VSInternalDocumentOnAutoInsertResponseItem? response = null;
        if (delegatedResponseText is not null)
        {
            var start = sourceText.GetPosition(input.Position);
            var end = start;
            response = new VSInternalDocumentOnAutoInsertResponseItem()
            {
                TextEdit = new TextEdit() { NewText = delegatedResponseText, Range = new() { Start = start, End = end } },
                TextEditFormat = InsertTextFormat.Snippet
            };
        }

        var requestInvoker = new TestHtmlRequestInvoker([(VSInternalMethods.OnAutoInsertName, response)]);

        var endpoint = new CohostOnAutoInsertEndpoint(
            IncompatibleProjectService,
            RemoteServiceInvoker,
            ClientSettingsManager,
            onAutoInsertTriggerCharacterProviders,
            requestInvoker,
            LoggerFactory);

        var formattingOptions = new FormattingOptions()
        {
            InsertSpaces = insertSpaces,
            TabSize = tabSize
        };

        var request = new VSInternalDocumentOnAutoInsertParams()
        {
            TextDocument = new TextDocumentIdentifier()
            {
                DocumentUri = document.CreateDocumentUri()
            },
            Position = sourceText.GetPosition(input.Position),
            Character = triggerCharacter,
            Options = formattingOptions
        };

        var result = await endpoint.GetTestAccessor().HandleRequestAsync(request, document, DisposalToken);

        if (output is not null)
        {
            Assert.NotNull(result);
        }
        else
        {
            Assert.Null(result);
            return;
        }

        if (result is not null)
        {
            var change = sourceText.GetTextChange(result.TextEdit);
            sourceText = sourceText.WithChanges(change);
        }

        AssertEx.EqualOrDiff(output, sourceText.ToString());
    }
}
