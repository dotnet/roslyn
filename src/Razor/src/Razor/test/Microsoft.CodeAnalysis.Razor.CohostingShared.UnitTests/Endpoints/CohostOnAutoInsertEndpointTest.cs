// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Razor.Settings;
using Microsoft.CodeAnalysis.Remote.Razor;
using Microsoft.CodeAnalysis.Remote.Razor.AutoInsert;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Razor.LanguageClient.Cohost;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using WorkItemAttribute = Roslyn.Test.Utilities.WorkItemAttribute;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public class CohostOnAutoInsertEndpointTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    private static readonly string[] s_newLineBeforeOpenBracePlacements =
    [
        "accessors",
        "types",
        "methods",
        "properties",
        "anonymous_methods",
        "control_blocks",
        "anonymous_types",
        "object_collection_array_initializers",
        "lambdas",
    ];

    [Fact]
    public void RazorTriggerCharactersMatchOOPAutoInsertProviders()
    {
        var expectedTriggerCharacters = OOPExportProvider.GetExportedValues<IOnAutoInsertProvider>()
            .Select(provider => provider.TriggerCharacter)
            .Distinct()
            .OrderBy(triggerCharacter => triggerCharacter)
            .ToArray();
        var actualTriggerCharacters = CohostOnAutoInsertEndpoint.TestAccessor.GetRazorOnAutoInsertTriggerCharacters()
            .OrderBy(triggerCharacter => triggerCharacter)
            .ToArray();

        Assert.Equal(expectedTriggerCharacters, actualTriggerCharacters);
    }

    [Fact]
    public void CSharpTriggerCharactersMatchRemoteAutoInsertService()
    {
        var expectedTriggerCharacters = RemoteAutoInsertService.TestAccessor.GetCSharpAllowedAutoInsertTriggerCharacters()
            .OrderBy(triggerCharacter => triggerCharacter)
            .ToArray();
        var actualTriggerCharacters = CohostOnAutoInsertEndpoint.TestAccessor.GetCSharpAllowedAutoInsertTriggerCharacters()
            .OrderBy(triggerCharacter => triggerCharacter)
            .ToArray();

        Assert.Equal(expectedTriggerCharacters, actualTriggerCharacters);
    }

    [Fact]
    public void HtmlTriggerCharactersMatchRemoteAutoInsertService()
    {
        var expectedTriggerCharacters = RemoteAutoInsertService.TestAccessor.GetHtmlAllowedAutoInsertTriggerCharacters()
            .OrderBy(triggerCharacter => triggerCharacter)
            .ToArray();
        var actualTriggerCharacters = CohostOnAutoInsertEndpoint.TestAccessor.GetHtmlAllowedAutoInsertTriggerCharacters()
            .OrderBy(triggerCharacter => triggerCharacter)
            .ToArray();

        Assert.Equal(expectedTriggerCharacters, actualTriggerCharacters);
    }

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

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/13203")]
    public async Task EndTag_EmptyTagName()
    {
        await VerifyOnAutoInsertAsync(
            input: "<>$$",
            output: "<>$0</>",
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
    [WorkItem("https://github.com/dotnet/razor/issues/12703")]
    public async Task CSharp_OnEnter_KAndRBraces_ControlBlock()
    {
        await VerifyCSharpOnEnterKAndRBracesAsync(
            input: """
                @{
                    if (true) {
                $$}
                }
                """,
            output: """
                @{
                    if (true) {
                        $0
                    }
                }
                """,
            excludedBracePlacement: "control_blocks");
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12703")]
    public async Task CSharp_OnEnter_KAndRBraces_NestedType()
    {
        await VerifyOnAutoInsertAsync(
            input: """
                @code {
                    private class C {
                $$}
                }
                """,
            output: """
                @code {
                    private class C {
                        $0
                    }
                }
                """,
            triggerCharacter: "\n",
            additionalFiles:
            [
                (".editorconfig", """
                    root = true

                    [*.razor]
                    csharp_new_line_before_open_brace = none
                    """)
            ]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12703")]
    public async Task CSharp_OnEnter_KAndRBraces_Method()
    {
        await VerifyCSharpOnEnterKAndRBracesAsync(
            input: """
                @code {
                    private void M() {
                $$}
                }
                """,
            output: """
                @code {
                    private void M() {
                        $0
                    }
                }
                """,
            excludedBracePlacement: "methods");
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/5607")]
    public async Task CSharp_OnEnter_UsesEditorConfig_Razor()
    {
        await VerifyOnAutoInsertAsync(
            input: """
                @code {
                    private void M() {
                $$}
                }
                """,
            output: """
                @code {
                    private void M() {
                        $0
                    }
                }
                """,
            triggerCharacter: "\n",
            additionalFiles:
            [
                (".editorconfig", """
                    root = true

                    [*.razor]
                    csharp_new_line_before_open_brace = none
                    """)
            ]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/5607")]
    public async Task CSharp_OnEnter_UsesEditorConfig_Cshtml()
    {
        await VerifyOnAutoInsertAsync(
            input: """
                @functions {
                    private void M() {
                $$}
                }
                """,
            output: """
                @functions {
                    private void M() {
                        $0
                    }
                }
                """,
            triggerCharacter: "\n",
            fileKind: RazorFileKind.Legacy,
            additionalFiles:
            [
                (".editorconfig", """
                    root = true

                    [*.cshtml]
                    csharp_new_line_before_open_brace = none
                    """)
            ]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12703")]
    public async Task CSharp_OnEnter_KAndRBraces_Property()
    {
        await VerifyCSharpOnEnterKAndRBracesAsync(
            input: """
                @code {
                    private int P {
                $$}
                }
                """,
            output: """
                @code {
                    private int P {
                        $0
                    }
                }
                """,
            excludedBracePlacement: "properties");
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12703")]
    public async Task CSharp_OnEnter_KAndRBraces_Accessor()
    {
        await VerifyCSharpOnEnterKAndRBracesAsync(
            input: """
                @code {
                    private int P
                    {
                        get {
                $$}
                    }
                }
                """,
            output: """
                @code {
                    private int P
                    {
                        get {
                            $0
                        }
                    }
                }
                """,
            excludedBracePlacement: "accessors");
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12703")]
    public async Task CSharp_OnEnter_KAndRBraces_Lambda()
    {
        await VerifyCSharpOnEnterKAndRBracesAsync(
            input: """
                @code {
                    private System.Action A = () => {
                $$};
                }
                """,
            output: """
                @code {
                    private System.Action A = () => {
                        $0
                    };
                }
                """,
            excludedBracePlacement: "lambdas");
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12703")]
    public async Task CSharp_OnEnter_KAndRBraces_AnonymousMethod()
    {
        await VerifyCSharpOnEnterKAndRBracesAsync(
            input: """
                @code {
                    private System.Action A = delegate {
                $$};
                }
                """,
            output: """
                @code {
                    private System.Action A = delegate {
                        $0
                    };
                }
                """,
            excludedBracePlacement: "anonymous_methods");
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12703")]
    public async Task CSharp_OnEnter_KAndRBraces_ObjectInitializer()
    {
        await VerifyCSharpOnEnterKAndRBracesAsync(
            input: """
                @code {
                    private object O = new object {
                $$};
                }
                """,
            output: """
                @code {
                    private object O = new object {
                        $0
                    };
                }
                """,
            excludedBracePlacement: "object_collection_array_initializers");
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12703")]
    public async Task CSharp_OnEnter_KAndRBraces_AnonymousType()
    {
        await VerifyCSharpOnEnterKAndRBracesAsync(
            input: """
                @code {
                    private object O = new {
                $$};
                }
                """,
            output: """
                @code {
                    private object O = new {
                        $0
                    };
                }
                """,
            excludedBracePlacement: "anonymous_types");
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

    private Task VerifyCSharpOnEnterKAndRBracesAsync(
        TestCode input,
        string output,
        string excludedBracePlacement)
        => VerifyOnAutoInsertAsync(
            input,
            output,
            triggerCharacter: "\n",
            additionalFiles:
            [
                (".editorconfig", $$"""
                    root = true

                    [*.razor]
                    csharp_new_line_before_open_brace = {{string.Join(", ", s_newLineBeforeOpenBracePlacements.Where(placement => placement != excludedBracePlacement))}}
                    """)
            ]);

    private async Task VerifyOnAutoInsertAsync(
        TestCode input,
        string? output,
        string triggerCharacter,
        string? delegatedResponseText = null,
        bool insertSpaces = true,
        int tabSize = 4,
        bool formatOnType = true,
        bool autoClosingTags = true,
        RazorFileKind? fileKind = null,
        (string fileName, string contents)[]? additionalFiles = null)
    {
        fileKind ??= RazorFileKind.Component;
        var document = CreateProjectAndRazorDocument(
            input.Text,
            fileKind: fileKind,
            additionalFiles: additionalFiles);
        var sourceText = await document.GetTextAsync(DisposalToken);

        ClientSettingsManager.Update(ClientAdvancedSettings.Default with { FormatOnType = formatOnType, AutoClosingTags = autoClosingTags });

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
                DocumentUri = document.GetURI()
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
