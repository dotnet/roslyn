// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Settings;
using Xunit;
using Xunit.Abstractions;

#if COHOSTING
namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.Formatting;
#else
namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting;
#endif

public class DocumentFormattingTest(ITestOutputHelper testOutput) : DocumentFormattingTestBase(testOutput)
{
    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/9658#issuecomment-3943605712")]
    public async Task MultilineIfStatement()
    {
        await RunFormattingTestAsync(
            input: """
                <div>
                    @if (true ||
                        true ||
                        true ||
                        true)
                        {
                            // Hi
                        }
                    </div>
                """,
            htmlFormatted: """
                <div>
                    @if (true ||
                    true ||
                    true ||
                    true)
                    {
                    // Hi
                    }
                </div>
                """,
            expected: """
                <div>
                    @if (true ||
                        true ||
                        true ||
                        true)
                    {
                        // Hi
                    }
                </div>
                """);
    }

    [Fact]
    [WorkItem("https://developercommunity.visualstudio.com/t/Format-Document-in-a-blazor-documents-ad/11046727")]
    public async Task MultilineRawStringLiteral()
    {
        await RunFormattingTestAsync(
            input: """"
                <PageTitle>
                    <PageTitle>
                        @("""
                          <FluentButton IconStart="Icons.Create" @onclick="(() => _createDialogBs5?.Show())">Nieuw</FluentButton>

                          <FCBS5Modal @ref="_createDialogBs5" OnClose="() => _createDialogBs5?.Hide()">
                          <Title>Aanmaak scherm</Title>
                          <Body>
                                <label>Vul hier een tekst in</label>
                                <input @bind=_createItem />
                          </Body>
                          <Footer>
                                <FluentButton IconStart="Icons.Save" @onclick="SaveItem">Opslaan</FluentButton>
                                <FluentButton IconStart="Icons.Cancel" @onclick="() => _createDialogBs5?.Hide()">Annuleren</FluentButton>
                          </Footer>
                          </FCBS5Modal>
                          """)</PageTitle>
                </PageTitle>
                """",
            htmlFormatted: """"
                <PageTitle>
                    <PageTitle>
                        @("""
                        <FluentButton IconStart="Icons.Create" @onclick="(() => _createDialogBs5?.Show())">Nieuw</FluentButton>

                        <FCBS5Modal @ref="_createDialogBs5" OnClose="() => _createDialogBs5?.Hide()">
                        <Title>Aanmaak scherm</Title>
                        <Body>
                        <label>Vul hier een tekst in</label>
                        <input @bind=_createItem />
                        </Body>
                        <Footer>
                        <FluentButton IconStart="Icons.Save" @onclick="SaveItem">Opslaan</FluentButton>
                        <FluentButton IconStart="Icons.Cancel" @onclick="() => _createDialogBs5?.Hide()">Annuleren</FluentButton>
                        </Footer>
                        </FCBS5Modal>
                        """)
                    </PageTitle>
                </PageTitle>
                """",
            expected: """"
                <PageTitle>
                    <PageTitle>
                        @("""
                          <FluentButton IconStart="Icons.Create" @onclick="(() => _createDialogBs5?.Show())">Nieuw</FluentButton>

                          <FCBS5Modal @ref="_createDialogBs5" OnClose="() => _createDialogBs5?.Hide()">
                          <Title>Aanmaak scherm</Title>
                          <Body>
                                <label>Vul hier een tekst in</label>
                                <input @bind=_createItem />
                          </Body>
                          <Footer>
                                <FluentButton IconStart="Icons.Save" @onclick="SaveItem">Opslaan</FluentButton>
                                <FluentButton IconStart="Icons.Cancel" @onclick="() => _createDialogBs5?.Hide()">Annuleren</FluentButton>
                          </Footer>
                          </FCBS5Modal>
                          """)
                    </PageTitle>
                </PageTitle>
                """");
    }

    [Fact]
    [WorkItem("https://developercommunity.visualstudio.com/t/Razor-Formatting-Feature-internal-error/11041869")]
    public async Task TextAndTagOnSameLine()
    {
        await RunFormattingTestAsync(
            input: """
                <div>
                	@if (b)
                	{
                		<text>:</text> <InputFile OnChange="StateHasChanged" />
                	}
                </div>

                @code
                {
                	bool b;
                }
                
                """,
            htmlFormatted: """
                <div>
                	@if (b)
                	{
                	<text>:</text> <InputFile OnChange="StateHasChanged" />
                	}
                </div>
                
                @code
                {
                	bool b;
                }
                
                """,
            expected: """
                <div>
                	@if (b)
                	{
                		<text>:</text> <InputFile OnChange="StateHasChanged" />
                	}
                </div>
                
                @code
                {
                	bool b;
                }
                
                """,
            insertSpaces: false);
    }

    [Fact]
    [WorkItem("https://github.com/microsoft/vscode-dotnettools/issues/2766")]
    public async Task DifferentAttributeWrappingPoint1()
    {
        await RunFormattingTestAsync(
            input: """
                <div>
                    <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target=".navbar-collapse" aria-controls="navbarSupportedContent"
                            aria-expanded="false" aria-label="Toggle navigation"></button>
                </div>
                """,
            htmlFormatted: """
                <div>
                    <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target=".navbar-collapse"
                        aria-controls="navbarSupportedContent" aria-expanded="false" aria-label="Toggle navigation"></button>
                </div>
                """,
            expected: """
                <div>
                    <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target=".navbar-collapse"
                            aria-controls="navbarSupportedContent" aria-expanded="false" aria-label="Toggle navigation"></button>
                </div>
                """,
            validateHtmlFormattedMatchesWebTools: false);
    }

    [Fact]
    [WorkItem("https://github.com/microsoft/vscode-dotnettools/issues/2766")]
    public async Task DifferentAttributeWrappingPoint2()
    {
        await RunFormattingTestAsync(
            input: """
                <div>
                    <button class="navbar-toggler" type="button" data-bs-toggle="collapse"
                            data-bs-target=".navbar-collapse" aria-controls="navbarSupportedContent" aria-expanded="false" aria-label="Toggle navigation"></button>
                </div>
                """,
            htmlFormatted: """
                <div>
                    <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target=".navbar-collapse"
                        aria-controls="navbarSupportedContent" aria-expanded="false" aria-label="Toggle navigation"></button>
                </div>
                """,
            expected: """
                <div>
                    <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target=".navbar-collapse"
                            aria-controls="navbarSupportedContent" aria-expanded="false" aria-label="Toggle navigation"></button>
                </div>
                """,
            validateHtmlFormattedMatchesWebTools: false);
    }

    [Fact]
    [WorkItem("https://github.com/microsoft/vscode-dotnettools/issues/2766")]
    public async Task DifferentAttributeWrappingPoint3()
    {
        await RunFormattingTestAsync(
            input: """
                <div>
                    <button class="navbar-toggler"
                            type="button" data-bs-toggle="collapse"
                            data-bs-target=".navbar-collapse"
                            aria-controls="navbarSupportedContent"
                            aria-expanded="false"
                            aria-label="Toggle navigation"></button>
                </div>
                """,
            htmlFormatted: """
                <div>
                    <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target=".navbar-collapse"
                        aria-controls="navbarSupportedContent" aria-expanded="false" aria-label="Toggle navigation"></button>
                </div>
                """,
            expected: """
                <div>
                    <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target=".navbar-collapse"
                            aria-controls="navbarSupportedContent" aria-expanded="false" aria-label="Toggle navigation"></button>
                </div>
                """,
            validateHtmlFormattedMatchesWebTools: false);
    }

    [Fact]
    [WorkItem("https://github.com/microsoft/vscode-dotnettools/issues/2766")]
    public async Task NewBlankLines()
    {
        await RunFormattingTestAsync(
            input: """
                <html>
                <head>
                <title>Goo</title>
                </head>
                <body>
                <div>
                </div>
                </body>
                </html>
                """,
            htmlFormatted: """
                <html>

                <head>
                    <title>Goo</title>
                </head>

                <body>
                    <div>
                    </div>
                </body>

                </html>
                """,
            expected: """
                <html>
            
                <head>
                    <title>Goo</title>
                </head>
            
                <body>
                    <div>
                    </div>
                </body>
            
                </html>
                """,
            validateHtmlFormattedMatchesWebTools: false);
    }

    [Fact]
    public async Task EmptyDocument()
    {
        await RunFormattingTestAsync(
            input: "",
            htmlFormatted: """
                
                """,
            expected: "");
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/8333")]
    public async Task MultilineStringLiterals()
    {
        // The single line string doesn't fail in this test, because Web Tools' formatter doesn't produce
        // a bad edit. VS Code does, and tests in FormattingLogTest validate that scenario with VS Code edits.

        await RunFormattingTestAsync(
            input: """"
                <div>
                @{
                var s1 = "    test    test    ";

                var s2 = """
                            this is
                                async string
                              that shouldn't move
                          """;

                var s3 = @"
                            this is
                                async string
                              that shouldn't move
                          ";
                }
                </div>
                """",
            htmlFormatted: """"
                <div>
                    @{
                    var s1 = "    test    test    ";
                
                    var s2 = """
                    this is
                    async string
                    that shouldn't move
                    """;
                
                    var s3 = @"
                    this is
                    async string
                    that shouldn't move
                    ";
                    }
                </div>
                """",
            expected: """"
                <div>
                    @{
                        var s1 = "    test    test    ";

                        var s2 = """
                            this is
                                async string
                              that shouldn't move
                          """;

                        var s3 = @"
                            this is
                                async string
                              that shouldn't move
                          ";
                    }
                </div>
                """");
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12416")]
    public async Task MixedIndentation()
    {
        // This doesn't actually fail because the Html formatter in Web Tools doesn't produce "bad" edits
        // like VS Code does, but thought I'd put it here just in case. Tests in FormattingLogTest validate
        // the same scenario with VS Code edits.

        await RunFormattingTestAsync(
            input: """
                <div>
                @switch (true)
                {
                    case true:
                		@if (true)
                		{
                		}
                        break;
                }
                </div>
                """,
            htmlFormatted: """
                <div>
                    @switch (true)
                    {
                    case true:
                    @if (true)
                    {
                    }
                    break;
                    }
                </div>
                """,
            expected: """
                <div>
                    @switch (true)
                    {
                        case true:
                            @if (true)
                            {
                            }
                            break;
                    }
                </div>
                """);
    }

    [Fact]
    public async Task RangeFormatOpenBrace()
    {
        await RunFormattingTestAsync(
            input: """
                <div></div>

                @code {
                    private void M(string [|request|]) {
                    }
                }
                """,
            htmlFormatted: """
                <div></div>
                
                @code {
                    private void M(string request) {
                    }
                }
                """,
            expected: """
                <div></div>
                
                @code {
                    private void M(string request)
                    {
                    }
                }
                """);
    }

    [Fact]
    public async Task RangeFormatOpenBrace_WithContent()
    {
        await RunFormattingTestAsync(
            input: """
                <div></div>

                @code {
                    private void M(string [|request|]) {
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                    }
                }
                """,
            htmlFormatted: """
                <div></div>
                
                @code {
                    private void M(string request) {
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                    }
                }
                """,
            expected: """
                <div></div>
                
                @code {
                    private void M(string request)
                    {
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                        // This is quite a lot of content
                    }
                }
                """);
    }

    [Fact]
    public async Task RoslynFormatSpaceAfterDot()
    {
        await RunFormattingTestAsync(
            input: """
                <div>

                @DateTime.Now.ToString()

                </div>
                """,
            htmlFormatted: """
                <div>
                
                    @DateTime.Now.ToString()
                
                </div>
                """,
            expected: """
                <div>
                
                    @DateTime.Now.ToString()
                
                </div>
                """,
            csharpSyntaxFormattingOptions: RazorCSharpSyntaxFormattingOptions.Default with
            {
                Spacing = RazorSpacePlacement.AfterDot
            });
    }

    [Fact]
    public async Task RoslynFormatSpaceAfterMethodCall()
    {
        await RunFormattingTestAsync(
            input: """
                <h1>count is @counter</h1>

                @GetCount()

                @code {
                private int counter;

                class Goo
                {
                    public int GetCount()
                    {
                        return counter++;
                    }
                }
                }
                """,
            htmlFormatted: """
                <h1>count is @counter</h1>
                
                @GetCount()
                
                @code {
                private int counter;
                
                class Goo
                {
                    public int GetCount()
                    {
                        return counter++;
                    }
                }
                }
                """,
            expected: """
                <h1>count is @counter</h1>
                
                @GetCount()
                
                @code {
                    private int counter;
                
                    class Goo
                    {
                        public int GetCount()
                        {
                            return counter++;
                        }
                    }
                }
                """,
            csharpSyntaxFormattingOptions: RazorCSharpSyntaxFormattingOptions.Default with
            {
                Spacing = RazorSpacePlacement.AfterMethodCallName
            });
    }

    [Fact]
    public async Task RoslynFormatSpaceAfterMethodCallAndDecl()
    {
        await RunFormattingTestAsync(
            input: """
                <h1>count is @counter</h1>

                @GetCount()

                @code {
                private int counter;

                class Goo
                {
                    public int GetCount()
                    {
                        return counter++;
                    }
                }
                }
                """,
            htmlFormatted: """
                <h1>count is @counter</h1>
                
                @GetCount()
                
                @code {
                private int counter;
                
                class Goo
                {
                    public int GetCount()
                    {
                        return counter++;
                    }
                }
                }
                """,
            expected: """
                <h1>count is @counter</h1>
                
                @GetCount()
                
                @code {
                    private int counter;
                
                    class Goo
                    {
                        public int GetCount ()
                        {
                            return counter++;
                        }
                    }
                }
                """,
            csharpSyntaxFormattingOptions: RazorCSharpSyntaxFormattingOptions.Default with
            {
                Spacing = RazorSpacePlacement.AfterMethodCallName | RazorSpacePlacement.AfterMethodDeclarationName
            });
    }

    [Fact]
    public async Task RoslynFormatBracesAsKandR()
    {
        // To format code blocks we emit a class so that class members are parsed properly by Roslyn, and ignore
        // the open brace on the next line. This test validates that that system works when Roslyn is configured
        // to format braces in K&R style, with the brace on the same line as the declaration.

        await RunFormattingTestAsync(
            input: """
                <h1>count is @counter</h1>

                @code {
                private int counter;

                class Goo
                {
                    public void Bar()
                    {
                        counter++;
                    }
                }
                }
                """,
            htmlFormatted: """
                <h1>count is @counter</h1>
                
                @code {
                private int counter;
                
                class Goo
                {
                    public void Bar()
                    {
                        counter++;
                    }
                }
                }
                """,
            expected: """
                <h1>count is @counter</h1>

                @code {
                    private int counter;

                    class Goo {
                        public void Bar() {
                            counter++;
                        }
                    }
                }
                """,
            csharpSyntaxFormattingOptions: RazorCSharpSyntaxFormattingOptions.Default with
            {
                NewLines = RazorNewLinePlacement.None
            });
    }

    [Fact]
    public async Task RoslynFormatBracesAsKandR_CodeBlockBraceOnNextLine()
    {
        await RunFormattingTestAsync(
            input: """
                <h1>count is @counter</h1>

                @code
                {
                private int counter;

                class Goo
                {
                    public void Bar()
                    {
                        counter++;
                    }
                }
                }
                """,
            htmlFormatted: """
                <h1>count is @counter</h1>
                
                @code
                {
                private int counter;
                
                class Goo
                {
                    public void Bar()
                    {
                        counter++;
                    }
                }
                }
                """,
            expected: """
                <h1>count is @counter</h1>

                @code
                {
                    private int counter;

                    class Goo {
                        public void Bar() {
                            counter++;
                        }
                    }
                }
                """,
            csharpSyntaxFormattingOptions: RazorCSharpSyntaxFormattingOptions.Default with
            {
                NewLines = RazorNewLinePlacement.None
            });
    }

    [Fact]
    public async Task RoslynFormatBracesAsKandR_NoRazorOrHtml()
    {
        await RunFormattingTestAsync(
            input: """
                @code {
                private bool IconMenuActive { get; set; } = false;
                protected void ToggleIconMenu(bool iconMenuActive)
                {
                IconMenuActive = iconMenuActive;
                }
                }
                """,
            htmlFormatted: """
                @code {
                private bool IconMenuActive { get; set; } = false;
                protected void ToggleIconMenu(bool iconMenuActive)
                {
                IconMenuActive = iconMenuActive;
                }
                }
                """,
            expected: """
                @code {
                    private bool IconMenuActive { get; set; } = false;
                    protected void ToggleIconMenu(bool iconMenuActive)
                    {
                        IconMenuActive = iconMenuActive;
                    }
                }
                """,
            csharpSyntaxFormattingOptions: RazorCSharpSyntaxFormattingOptions.Default with
            {
                NewLines = RazorNewLinePlacement.BeforeOpenBraceInMethods
            });
    }

    [Fact]
    public async Task RoslynFormatBracesAsKandR_CodeBlockBraceOnNextLine_NoRazorOrHtml()
    {
        await RunFormattingTestAsync(
            input: """
                @code
                {
                private bool IconMenuActive { get; set; } = false;
                protected void ToggleIconMenu(bool iconMenuActive)
                {
                IconMenuActive = iconMenuActive;
                }
                }
                """,
            htmlFormatted: """
                @code
                {
                private bool IconMenuActive { get; set; } = false;
                protected void ToggleIconMenu(bool iconMenuActive)
                {
                IconMenuActive = iconMenuActive;
                }
                }
                """,
            expected: """
                @code
                {
                    private bool IconMenuActive { get; set; } = false;
                    protected void ToggleIconMenu(bool iconMenuActive)
                    {
                        IconMenuActive = iconMenuActive;
                    }
                }
                """,
            csharpSyntaxFormattingOptions: RazorCSharpSyntaxFormattingOptions.Default with
            {
                NewLines = RazorNewLinePlacement.BeforeOpenBraceInMethods
            });
    }

    [Fact]
    public async Task RoslynFormatBracesAsKandR_CodeBlockBraceIndented_NoRazorOrHtml()
    {
        await RunFormattingTestAsync(
            input: """
                @code
                    {
                private bool IconMenuActive { get; set; } = false;
                protected void ToggleIconMenu(bool iconMenuActive)
                {
                IconMenuActive = iconMenuActive;
                }
                }
                """,
            htmlFormatted: """
                @code
                    {
                private bool IconMenuActive { get; set; } = false;
                protected void ToggleIconMenu(bool iconMenuActive)
                {
                IconMenuActive = iconMenuActive;
                }
                }
                """,
            expected: """
                @code
                {
                    private bool IconMenuActive { get; set; } = false;
                    protected void ToggleIconMenu(bool iconMenuActive)
                    {
                        IconMenuActive = iconMenuActive;
                    }
                }
                """,
            csharpSyntaxFormattingOptions: RazorCSharpSyntaxFormattingOptions.Default with
            {
                NewLines = RazorNewLinePlacement.BeforeOpenBraceInMethods
            });
    }

    [Fact]
    public async Task RoslynFormatBracesAsKandR_CodeBlockBraceIndented_InsideHtml()
    {
        await RunFormattingTestAsync(
            input: """
                <div>
                @code
                            {
                private bool IconMenuActive { get; set; } = false;
                protected void ToggleIconMenu(bool iconMenuActive)
                {
                IconMenuActive = iconMenuActive;
                }
                }
                </div>
                """,
            htmlFormatted: """
                <div>
                    @code
                    {
                    private bool IconMenuActive { get; set; } = false;
                    protected void ToggleIconMenu(bool iconMenuActive)
                    {
                    IconMenuActive = iconMenuActive;
                    }
                    }
                </div>
                """,
            expected: """
                <div>
                @code
                {
                    private bool IconMenuActive { get; set; } = false;
                    protected void ToggleIconMenu(bool iconMenuActive)
                    {
                        IconMenuActive = iconMenuActive;
                    }
                }
                </div>
                """,
            csharpSyntaxFormattingOptions: RazorCSharpSyntaxFormattingOptions.Default with
            {
                NewLines = RazorNewLinePlacement.BeforeOpenBraceInMethods
            });
    }

    [Fact]
    public async Task PropertyShrunkToOneLine()
    {
        await RunFormattingTestAsync(
            input: """
                @code {
                    public string Name
                    {
                        get;
                        set;
                    }
                }
                """,
            htmlFormatted: """
                @code {
                    public string Name
                    {
                        get;
                        set;
                    }
                }
                """,
            expected: """
                @code {
                    public string Name {
                        get;
                        set;
                    }
                }
                """,
            csharpSyntaxFormattingOptions: RazorCSharpSyntaxFormattingOptions.Default with
            {
                NewLines = RazorNewLinePlacement.None
            });
    }

    [Fact]
    public async Task AllWhitespaceDocument()
    {
        // The Html formatter shrinks this down to one line
        await RunFormattingTestAsync(
            input: """

                    
                    

                """,
            htmlFormatted: """
                
                """,
            expected: """

                    
                    
                
                """);
    }

    [Fact]
    public async Task StartsWithWhitespace()
    {
        await RunFormattingTestAsync(
            input: """

                    

                <div></div>

                """,
            htmlFormatted: """
                
                
                
                <div></div>
                
                """,
            expected: """
                
                    
                
                <div></div>
                
                """);
    }

    [Fact]
    public async Task EndsWithWhitespace()
    {
        await RunFormattingTestAsync(
            input: """
                <div></div>

                

                """,
            htmlFormatted: """
                <div></div>
                
                
                
                """,
            expected: """
                <div></div>
                
                
                
                """);
    }

    [Fact]
    public async Task Section_BraceOnNextLine()
    {
        await RunFormattingTestAsync(
            input: """
                @section    Scripts
                    {
                <meta property="a" content="b">
                }
                """,
            htmlFormatted: """
                @section    Scripts
                    {
                <meta property="a" content="b">
                }
                """,
            expected: """
                @section Scripts
                {
                    <meta property="a" content="b">
                }
                """,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task Section_BraceOnSameLine()
    {
        await RunFormattingTestAsync(
            input: """
                @section        Scripts                         {
                <meta property="a" content="b">
                }
                """,
            htmlFormatted: """
                @section        Scripts                         {
                <meta property="a" content="b">
                }
                """,
            expected: """
                @section Scripts {
                    <meta property="a" content="b">
                }
                """,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/10796")]
    public async Task Section_BraceOnNextLine_AtColumnZero()
    {
        await RunFormattingTestAsync(
            input: """
                @section Controls
                {
                <label>
                <span>Office</span>
                </label>
                <label>
                <span>Department</span>
                </label>
                }
                """,
            htmlFormatted: """
                @section Controls
                {
                <label>
                    <span>Office</span>
                </label>
                <label>
                    <span>Department</span>
                </label>
                }
                """,
            expected: """
                @section Controls
                {
                    <label>
                        <span>Office</span>
                    </label>
                    <label>
                        <span>Department</span>
                    </label>
                }
                """,
            fileKind: RazorFileKind.Legacy);
    }

    [Theory, CombinatorialData]
    public async Task CodeBlock_SpansMultipleLines(bool inGlobalNamespace)
    {
        await RunFormattingTestAsync(
            input: """
                @code
                        {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            htmlFormatted: """
                @code
                        {
                    private int currentCount = 0;
                
                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            expected: """
                @code
                {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            inGlobalNamespace: inGlobalNamespace);
    }

    [Theory, CombinatorialData]
    public async Task CodeBlock_IndentedBlock_MaintainsIndent(bool inGlobalNamespace)
    {
        await RunFormattingTestAsync(
            input: """
                <boo>
                    @code
                            {
                        private int currentCount = 0;

                        private void IncrementCount()
                        {
                            currentCount++;
                        }
                    }
                </boo>
                """,
            htmlFormatted: """
                <boo>
                    @code
                    {
                    private int currentCount = 0;
                
                    private void IncrementCount()
                    {
                    currentCount++;
                    }
                    }
                </boo>
                """,
            expected: """
                <boo>
                @code
                {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                </boo>
                """,
            inGlobalNamespace: inGlobalNamespace);
    }

    [Fact]
    public async Task CodeBlock_IndentedBlock_FixCloseBrace()
    {
        await RunFormattingTestAsync(
            input: """
                <boo>
                    @code
                            {
                        private int currentCount = 0;

                        private void IncrementCount()
                        {
                            currentCount++;
                        }
                                        }
                </boo>
                """,
            htmlFormatted: """
                <boo>
                    @code
                    {
                    private int currentCount = 0;
                
                    private void IncrementCount()
                    {
                    currentCount++;
                    }
                    }
                </boo>
                """,
            expected: """
                <boo>
                @code
                {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                </boo>
                """);
    }

    [Fact]
    public async Task CodeBlock_IndentedBlock_FixCloseBrace2()
    {
        await RunFormattingTestAsync(
            input: """
                <boo>
                @code
                        {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                </boo>
                """,
            htmlFormatted: """
                <boo>
                    @code
                    {
                    private int currentCount = 0;
                
                    private void IncrementCount()
                    {
                    currentCount++;
                    }
                    }
                </boo>
                """,
            expected: """
                <boo>
                @code
                {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                </boo>
                """);
    }

    [Fact]
    public async Task CodeBlock_FixCloseBrace()
    {
        await RunFormattingTestAsync(
            input: """
                @code        {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                    }
                """,
            htmlFormatted: """
                @code        {
                    private int currentCount = 0;
                
                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                    }
                """,
            expected: """
                @code {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """);
    }

    [Fact]
    public async Task CodeBlock_FixCloseBrace2()
    {
        await RunFormattingTestAsync(
            input: """
                @code        {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }                        }
                """,
            htmlFormatted: """
                @code        {
                    private int currentCount = 0;
                
                    private void IncrementCount()
                    {
                        currentCount++;
                    }                        }
                """,
            expected: """
                @code {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """);
    }

    [Fact]
    public async Task CodeBlock_FixCloseBrace3()
    {
        await RunFormattingTestAsync(
            input: """
                @code        {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                    }
                """,
            htmlFormatted: """
                @code        {
                    private int currentCount = 0;
                
                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                    }
                """,
            expected: """
                @code
                {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            codeBlockBraceOnNextLine: true);
    }

    [Fact]
    public async Task CodeBlock_FixCloseBrace4()
    {
        await RunFormattingTestAsync(
            input: """
                @code        {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }                        }
                """,
            htmlFormatted: """
                @code        {
                    private int currentCount = 0;
                
                    private void IncrementCount()
                    {
                        currentCount++;
                    }                        }
                """,
            expected: """
                @code
                {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            codeBlockBraceOnNextLine: true);
    }

    [Fact]
    public async Task CodeBlock_TooMuchWhitespace()
    {
        await RunFormattingTestAsync(
            input: """
                @code        {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            htmlFormatted: """
                @code        {
                    private int currentCount = 0;
                
                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            expected: """
                @code {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """);
    }

    [Fact]
    public async Task CodeBlock_NonSpaceWhitespace()
    {
        await RunFormattingTestAsync(
            input: """
                @code	{
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            htmlFormatted: """
                @code	{
                    private int currentCount = 0;
                
                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            expected: """
                @code {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """);
    }

    [Fact]
    public async Task CodeBlock_NonSpaceWhitespace2()
    {
        await RunFormattingTestAsync(
            input: """
                @code	{
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            htmlFormatted: """
                @code	{
                    private int currentCount = 0;
                
                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            expected: """
                @code
                {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            codeBlockBraceOnNextLine: true);
    }

    [Fact]
    public async Task CodeBlock_NoWhitespace()
    {
        await RunFormattingTestAsync(
            input: """
                @code{
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            htmlFormatted: """
                @code{
                    private int currentCount = 0;
                
                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            expected: """
                @code {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """);
    }

    [Fact]
    public async Task CodeBlock_NoWhitespace2()
    {
        await RunFormattingTestAsync(
            input: """
                @code{
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            htmlFormatted: """
                @code{
                    private int currentCount = 0;
                
                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            expected: """
                @code
                {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            codeBlockBraceOnNextLine: true);
    }

    [Fact]
    public async Task FunctionsBlock_BraceOnNewLine()
    {
        await RunFormattingTestAsync(
            input: """
                @functions
                        {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            htmlFormatted: """
                @functions
                        {
                    private int currentCount = 0;
                
                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            expected: """
                @functions
                {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task FunctionsBlock_TooManySpaces()
    {
        await RunFormattingTestAsync(
            input: """
                @functions        {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            htmlFormatted: """
                @functions        {
                    private int currentCount = 0;
                
                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            expected: """
                @functions {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task FunctionsBlock_TooManySpaces2()
    {
        await RunFormattingTestAsync(
            input: """
                @functions        {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            htmlFormatted: """
                @functions        {
                    private int currentCount = 0;
                
                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            expected: """
                @functions
                {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            fileKind: RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: true);
    }

    [Fact]
    public async Task FunctionsBlock_FixCloseBrace()
    {
        await RunFormattingTestAsync(
            input: """
                @functions        {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                         }
                """,
            htmlFormatted: """
                @functions        {
                    private int currentCount = 0;
                
                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                         }
                """,
            expected: """
                @functions {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task FunctionsBlock_FixCloseBrace2()
    {
        await RunFormattingTestAsync(
            input: """
                @functions        {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }                             }
                """,
            htmlFormatted: """
                @functions        {
                    private int currentCount = 0;
                
                    private void IncrementCount()
                    {
                        currentCount++;
                    }                             }
                """,
            expected: """
                @functions {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task FunctionsBlock_Tabs_FixCloseBrace()
    {
        await RunFormattingTestAsync(
            input: """
                @functions        {
                	private int currentCount = 0;

                	private void IncrementCount()
                	{
                		currentCount++;
                	}
                				}
                """,
            expected: """
                @functions {
                	private int currentCount = 0;

                	private void IncrementCount()
                	{
                		currentCount++;
                	}
                }
                """,
            insertSpaces: false,
            tabSize: 8,
            fileKind: RazorFileKind.Legacy,
            htmlFormatted: """
                @functions        {
                	private int currentCount = 0;
                
                	private void IncrementCount()
                	{
                		currentCount++;
                	}
                				}
                """);
    }

    [Fact]
    public async Task Layout()
    {
        await RunFormattingTestAsync(
            input: """
                @layout    MyLayout
                """,
            htmlFormatted: """
                @layout    MyLayout
                """,
            expected: """
                @layout MyLayout
                """);
    }

    [Fact]
    public async Task Inherits()
    {
        await RunFormattingTestAsync(
            input: """
                @inherits    MyBaseClass
                """,
            htmlFormatted: """
                @inherits    MyBaseClass
                """,
            expected: """
                @inherits MyBaseClass
                """);
    }

    [Fact]
    public async Task Implements()
    {
        await RunFormattingTestAsync(
            input: """
                @implements    IDisposable
                """,
            htmlFormatted: """
                @implements    IDisposable
                """,
            expected: """
                @implements IDisposable
                """);
    }

    [Fact]
    public async Task PreserveWhitespace()
    {
        await RunFormattingTestAsync(
            input: """
                @preservewhitespace    true
                """,
            htmlFormatted: """
                @preservewhitespace    true
                """,
            expected: """
                @preservewhitespace true
                """);
    }

    [Fact]
    public async Task Inject()
    {
        await RunFormattingTestAsync(
            input: """
                @inject    MyClass     myClass
                """,
            htmlFormatted: """
                @inject    MyClass     myClass
                """,
            expected: """
                @inject MyClass myClass
                """);
    }

    [Fact]
    public async Task Inject_TrailingWhitespace()
    {
        await RunFormattingTestAsync(
            input: """
                @inject    MyClass     myClass
                """,
            htmlFormatted: """
                @inject    MyClass     myClass
                """,
            expected: """
                @inject MyClass myClass
                """);
    }

    [Fact]
    public async Task Attribute1()
    {
        await RunFormattingTestAsync(
            input: """
                @attribute     [Obsolete(   "asdf"   , error:    false)]
                """,
            htmlFormatted: """
                @attribute     [Obsolete(   "asdf"   , error:    false)]
                """,
            expected: """
                @attribute [Obsolete("asdf", error: false)]
                """);
    }

    [Fact]
    public async Task Attribute2()
    {
        await RunFormattingTestAsync(
            input: """
                @attribute     [Attr(   "asdf"   , error:    false)]
                @attribute   [Attribute(   "asdf"   , error:    false)]
                @attribute [ALongAttributeName(   "asdf"   , error:    false)]
                """,
            htmlFormatted: """
                @attribute     [Attr(   "asdf"   , error:    false)]
                @attribute   [Attribute(   "asdf"   , error:    false)]
                @attribute [ALongAttributeName(   "asdf"   , error:    false)]
                """,
            expected: """
                @attribute [Attr("asdf", error: false)]
                @attribute [Attribute("asdf", error: false)]
                @attribute [ALongAttributeName("asdf", error: false)]
                """);
    }

    [Fact]
    public async Task Attribute3()
    {
        await RunFormattingTestAsync(
            input: """
                <div></div>
                @attribute     [Obsolete(   "asdf"   , error:    false)]
                <div></div>
                @attribute     [Obsolete(   "asdf"   , error:    false)]
                <div></div>
                @attribute     [Obsolete(   "asdf"   , error:    false)]
                <div></div>
                """,
            htmlFormatted: """
                <div></div>
                @attribute     [Obsolete(   "asdf"   , error:    false)]
                <div></div>
                @attribute     [Obsolete(   "asdf"   , error:    false)]
                <div></div>
                @attribute     [Obsolete(   "asdf"   , error:    false)]
                <div></div>
                """,
            expected: """
                <div></div>
                @attribute [Obsolete("asdf", error: false)]
                <div></div>
                @attribute [Obsolete("asdf", error: false)]
                <div></div>
                @attribute [Obsolete("asdf", error: false)]
                <div></div>
                """);
    }

    [Fact]
    public async Task TypeParam_Unconstrained()
    {
        await RunFormattingTestAsync(
            input: """
                @typeparam     T
                """,
            htmlFormatted: """
                @typeparam     T
                """,
            expected: """
                @typeparam T
                """);
    }

    [Fact]
    public async Task TypeParam1()
    {
        await RunFormattingTestAsync(
            input: """
                @typeparam     T     where    T    :   IDisposable
                """,
            htmlFormatted: """
                @typeparam     T     where    T    :   IDisposable
                """,
            expected: """
                @typeparam T where T : IDisposable
                """);
    }

    [Fact]
    public async Task TypeParam2()
    {
        await RunFormattingTestAsync(
            input: """
                @typeparam     TItem     where    TItem    :   IDisposable
                """,
            htmlFormatted: """
                @typeparam     TItem     where    TItem    :   IDisposable
                """,
            expected: """
                @typeparam TItem where TItem : IDisposable
                """);
    }

    [Fact]
    public async Task TypeParam3()
    {
        await RunFormattingTestAsync(
            input: """
                @using System
                @typeparam     TItem     where    TItem    :   IDisposable

                <div>
                @{
                if (true)
                {
                // Hello
                }
                }
                </div>
                """,
            htmlFormatted: """
                @using System
                @typeparam     TItem     where    TItem    :   IDisposable
                
                <div>
                    @{
                    if (true)
                    {
                    // Hello
                    }
                    }
                </div>
                """,
            expected: """
                @using System
                @typeparam TItem where TItem : IDisposable
                
                <div>
                    @{
                        if (true)
                        {
                            // Hello
                        }
                    }
                </div>
                """);
    }

    [Fact]
    public async Task TypeParam4()
    {
        await RunFormattingTestAsync(
            input: """
                @using System
                @typeparam     TItem     where    TItem    :   IDisposable
                @typeparam TParent where TParent : string

                @if (true)
                {
                // Hello
                }
                """,
            htmlFormatted: """
                @using System
                @typeparam     TItem     where    TItem    :   IDisposable
                @typeparam TParent where TParent : string
                
                @if (true)
                {
                // Hello
                }
                """,
            expected: """
                @using System
                @typeparam TItem where TItem : IDisposable
                @typeparam TParent where TParent : string
                
                @if (true)
                {
                    // Hello
                }
                """);
    }

    [Fact]
    public async Task Model()
    {
        await RunFormattingTestAsync(
            input: """
                @model    MyModel
                """,
            htmlFormatted: """
                @model    MyModel
                """,
            expected: """
                @model MyModel
                """,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task Page()
    {
        await RunFormattingTestAsync(
            input: """
                @page    "MyPage"
                """,
            htmlFormatted: """
                @page    "MyPage"
                """,
            expected: """
                @page "MyPage"
                """,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task MultiLineComment_WithinHtml1()
    {
        await RunFormattingTestAsync(
            input: """
                <div>
                @* <div>
                This comment's opening at-star will be aligned, and the
                indentation of the rest of its lines will be preserved.
                        </div>
                    *@
                </div>
                """,
            htmlFormatted: """
                <div>
                    @* <div>
                    This comment's opening at-star will be aligned, and the
                    indentation of the rest of its lines will be preserved.
                    </div>
                    *@
                </div>
                """,
            expected: """
                <div>
                    @* <div>
                This comment's opening at-star will be aligned, and the
                indentation of the rest of its lines will be preserved.
                        </div>
                    *@
                </div>
                """,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task MultiLineComment_WithinHtml2()
    {
        await RunFormattingTestAsync(
            input: """
                <div>
                @* <div>
                This comment's opening at-star will be aligned, and the
                indentation of the rest of its lines will be preserved.
                        </div>                        *@
                </div>
                """,
            htmlFormatted: """
                <div>
                    @* <div>
                    This comment's opening at-star will be aligned, and the
                    indentation of the rest of its lines will be preserved.
                    </div>                        *@
                </div>
                """,
            expected: """
                <div>
                    @* <div>
                This comment's opening at-star will be aligned, and the
                indentation of the rest of its lines will be preserved.
                        </div>                        *@
                </div>
                """,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task MultiLineComment_WithinHtml3()
    {
        await RunFormattingTestAsync(
            input: """
                <div>
                @* <div>
                This comment's opening at-star will be aligned, and the
                indentation of the rest of its lines will be preserved.
                        </div>
                *@
                </div>
                """,
            htmlFormatted: """
                <div>
                    @* <div>
                    This comment's opening at-star will be aligned, and the
                    indentation of the rest of its lines will be preserved.
                    </div>
                    *@
                </div>
                """,
            expected: """
                <div>
                    @* <div>
                This comment's opening at-star will be aligned, and the
                indentation of the rest of its lines will be preserved.
                        </div>
                *@
                </div>
                """,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task Using()
    {
        await RunFormattingTestAsync(
            input: """
                @using   System;
                """,
            htmlFormatted: """
                @using   System;
                """,
            expected: """
                @using System;
                """);
    }

    [Fact]
    public async Task UsingStatic()
    {
        await RunFormattingTestAsync(
            input: """
                @using  static   System.Math;
                """,
            htmlFormatted: """
                @using  static   System.Math;
                """,
            expected: """
                @using static System.Math;
                """);
    }

    [Fact]
    public async Task UsingAlias()
    {
        await RunFormattingTestAsync(
            input: """
                @using  M   =    System.Math;
                """,
            htmlFormatted: """
                @using  M   =    System.Math;
                """,
            expected: """
                @using M = System.Math;
                """);
    }

    [Fact]
    public async Task TagHelpers()
    {
        await RunFormattingTestAsync(
            input: """
                @addTagHelper    *,    Microsoft.AspNetCore.Mvc.TagHelpers
                @removeTagHelper    *,     Microsoft.AspNetCore.Mvc.TagHelpers
                @addTagHelper    "*,  Microsoft.AspNetCore.Mvc.TagHelpers"
                @removeTagHelper    "*,  Microsoft.AspNetCore.Mvc.TagHelpers"
                @tagHelperPrefix    th:
                """,
            htmlFormatted: """
                @addTagHelper    *,    Microsoft.AspNetCore.Mvc.TagHelpers
                @removeTagHelper    *,     Microsoft.AspNetCore.Mvc.TagHelpers
                @addTagHelper    "*,  Microsoft.AspNetCore.Mvc.TagHelpers"
                @removeTagHelper    "*,  Microsoft.AspNetCore.Mvc.TagHelpers"
                @tagHelperPrefix    th:
                """,
            expected: """
                @addTagHelper    *,    Microsoft.AspNetCore.Mvc.TagHelpers
                @removeTagHelper    *,     Microsoft.AspNetCore.Mvc.TagHelpers
                @addTagHelper    "*,  Microsoft.AspNetCore.Mvc.TagHelpers"
                @removeTagHelper    "*,  Microsoft.AspNetCore.Mvc.TagHelpers"
                @tagHelperPrefix    th:
                """,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task LargeFile()
    {
        await RunFormattingTestAsync(
            input: RazorTestResources.GetResourceText("FormattingTest.razor"),
            htmlFormatted: RazorTestResources.GetResourceText("FormattingTest_HtmlFormatted.razor"),
            expected: RazorTestResources.GetResourceText("FormattingTest_Expected.razor"),
            allowDiagnostics: true);
    }

    [Fact]
    public async Task FormatsSimpleHtmlTag()
    {
        await RunFormattingTestAsync(
            input: """
                   <html>
                <head>
                   <title>Hello</title></head>
                <body><div>
                </div>
                        </body>
                 </html>
                """,
            htmlFormatted: """
                <html>
                <head>
                    <title>Hello</title>
                </head>
                <body>
                    <div>
                    </div>
                </body>
                </html>
                """,
            expected: """
                <html>
                <head>
                    <title>Hello</title>
                </head>
                <body>
                    <div>
                    </div>
                </body>
                </html>
                """);
    }

    [Fact]
    public async Task FormatsSimpleHtmlTag_Range()
    {
        await RunFormattingTestAsync(
            input: """
                <html>
                <head>
                    <title>Hello</title>
                </head>
                <body>
                        [|<div>
                        </div>|]
                </body>
                </html>
                """,
            htmlFormatted: """
                <html>
                <head>
                    <title>Hello</title>
                </head>
                <body>
                    <div>
                    </div>
                </body>
                </html>
                """,
            expected: """
                <html>
                <head>
                    <title>Hello</title>
                </head>
                <body>
                    <div>
                    </div>
                </body>
                </html>
                """);
    }

    [Fact]
    public async Task FormatsRazorHtmlBlock()
    {
        await RunFormattingTestAsync(
            input: """
                @page "/error"

                        <h1 class=
                "text-danger">Error.</h1>
                    <h2 class="text-danger">An error occurred while processing your request.</h2>

                            <h3>Development Mode</h3>
                <p>
                    Swapping to <strong>Development</strong> environment will display more detailed information about the error that occurred.</p>
                <p>
                    <strong>The Development environment shouldn't be enabled for deployed applications.
                </strong>
                            <div>
                 <div>
                    <div>
                <div>
                        This is heavily nested
                </div>
                 </div>
                    </div>
                        </div>
                </p>
                """,
            htmlFormatted: """
                @page "/error"
                
                <h1 class="text-danger">
                    Error.
                </h1>
                <h2 class="text-danger">An error occurred while processing your request.</h2>
                
                <h3>Development Mode</h3>
                <p>
                    Swapping to <strong>Development</strong> environment will display more detailed information about the error that occurred.
                </p>
                <p>
                    <strong>
                        The Development environment shouldn't be enabled for deployed applications.
                    </strong>
                    <div>
                        <div>
                            <div>
                                <div>
                                    This is heavily nested
                                </div>
                            </div>
                        </div>
                    </div>
                </p>
                """,
            expected: """
                @page "/error"

                <h1 class="text-danger">
                    Error.
                </h1>
                <h2 class="text-danger">An error occurred while processing your request.</h2>

                <h3>Development Mode</h3>
                <p>
                    Swapping to <strong>Development</strong> environment will display more detailed information about the error that occurred.
                </p>
                <p>
                    <strong>
                        The Development environment shouldn't be enabled for deployed applications.
                    </strong>
                    <div>
                        <div>
                            <div>
                                <div>
                                    This is heavily nested
                                </div>
                            </div>
                        </div>
                    </div>
                </p>
                """);
    }

    [Fact]
    public async Task FormatsMixedHtmlBlock()
    {
        await RunFormattingTestAsync(
            input: """
                @page "/test"
                @{
                <p>
                        @{
                                var t = 1;
                if (true)
                {

                            }
                        }
                        </p>
                <div>
                 @{
                    <div>
                <div>
                        This is heavily nested
                </div>
                 </div>
                    }
                        </div>
                }
                """,
            htmlFormatted: """
                @page "/test"
                @{
                <p>
                    @{
                    var t = 1;
                    if (true)
                    {
                
                    }
                    }
                </p>
                <div>
                    @{
                    <div>
                        <div>
                            This is heavily nested
                        </div>
                    </div>
                    }
                </div>
                }
                """,
            expected: """
                @page "/test"
                @{
                    <p>
                        @{
                            var t = 1;
                            if (true)
                            {

                            }
                        }
                    </p>
                    <div>
                        @{
                            <div>
                                <div>
                                    This is heavily nested
                                </div>
                            </div>
                        }
                    </div>
                }
                """);
    }

    [Fact]
    public async Task FormatAttributeStyles()
    {
        await RunFormattingTestAsync(
            input: """
                <div class=@className>Some Text</div>
                <div class=@className style=@style>Some Text</div>
                <div class=@className style="@style">Some Text</div>
                <div class='@className'>Some Text</div>
                <div class="@className">Some Text</div>
                
                <br class=@className/>
                <br class=@className style=@style/>
                <br class=@className style="@style"/>
                <br class='@className'/>
                <br class="@className"/>
                """,
            htmlFormatted: """
                <div class=@className>Some Text</div>
                <div class=@className style=@style>Some Text</div>
                <div class=@className style="@style">Some Text</div>
                <div class='@className'>Some Text</div>
                <div class="@className">Some Text</div>
                
                <br class=@className/>
                <br class=@className style=@style/>
                <br class=@className style="@style"/>
                <br class='@className'/>
                <br class="@className"/>
                """,
            expected: """
                <div class=@className>Some Text</div>
                <div class=@className style=@style>Some Text</div>
                <div class=@className style="@style">Some Text</div>
                <div class='@className'>Some Text</div>
                <div class="@className">Some Text</div>

                <br class=@className/>
                <br class=@className style=@style/>
                <br class=@className style="@style"/>
                <br class='@className'/>
                <br class="@className"/>
                """);
    }

    [Fact]
    public async Task FormatsMixedRazorBlock()
    {
        await RunFormattingTestAsync(
            input: """
                @page "/test"

                <div class=@className>Some Text</div>

                @{
                @: Hi!
                var x = 123;
                <p>
                        @if (true) {
                                var t = 1;
                if (true)
                {
                <div>@DateTime.Now</div>
                            }

                            @while(true){
                 }
                        }
                        </p>
                }
                """,
            htmlFormatted: """
                @page "/test"
                
                <div class=@className>Some Text</div>
                
                @{
                @: Hi!
                var x = 123;
                <p>
                    @if (true) {
                    var t = 1;
                    if (true)
                    {
                    <div>@DateTime.Now</div>
                    }
                
                    @while(true){
                    }
                    }
                </p>
                }
                """,
            expected: """
                @page "/test"

                <div class=@className>Some Text</div>

                @{
                    @: Hi!
                    var x = 123;
                    <p>
                        @if (true)
                        {
                            var t = 1;
                            if (true)
                            {
                                <div>@DateTime.Now</div>
                            }

                            @while (true)
                            {
                            }
                        }
                    </p>
                }
                """);
    }

    [Fact]
    public async Task FormatsMixedContentWithMultilineExpressions()
    {
        await RunFormattingTestAsync(
            input: """
                @page "/test"

                <div
                attr='val'
                class=@className>Some Text</div>

                @{
                @: Hi!
                var x = DateTime
                    .Now.ToString();
                <p>
                        @if (true) {
                                var t = 1;
                        }
                        </p>
                }

                @(DateTime
                    .Now
                .ToString())

                @(
                    Foo.Values.Select(f =>
                    {
                        return f.ToString();
                    })
                )
                """,
            htmlFormatted: """
                @page "/test"
                
                <div attr='val'
                     class=@className>
                    Some Text
                </div>
                
                @{
                @: Hi!
                var x = DateTime
                    .Now.ToString();
                <p>
                    @if (true) {
                    var t = 1;
                    }
                </p>
                }
                
                @(DateTime
                    .Now
                .ToString())
                
                @(
                    Foo.Values.Select(f =>
                    {
                        return f.ToString();
                    })
                )
                """,
            expected: """
                @page "/test"

                <div attr='val'
                     class=@className>
                    Some Text
                </div>

                @{
                    @: Hi!
                    var x = DateTime
                        .Now.ToString();
                    <p>
                        @if (true)
                        {
                            var t = 1;
                        }
                    </p>
                }

                @(DateTime
                    .Now
                .ToString())

                @(
                    Foo.Values.Select(f =>
                    {
                        return f.ToString();
                    })
                )
                """);
    }

    [Fact]
    public async Task FormatsComplexBlock()
    {
        await RunFormattingTestAsync(
            input: """
                @page "/"

                <h1>Hello, world!</h1>

                        Welcome to your new app.

                <PageTitle Title="How is Blazor working for you?" />

                <div class="FF"
                     id="ERT">
                     asdf
                    <div class="3"
                         id="3">
                             @if(true){<p></p>}
                         </div>
                </div>

                @{
                <div class="FF"
                    id="ERT">
                    asdf
                    <div class="3"
                        id="3">
                            @if(true){<p></p>}
                        </div>
                </div>
                }

                @{
                <div class="FF"
                    id="ERT">
                    @{
                <div class="FF"
                    id="ERT">
                    asdf
                    <div class="3"
                        id="3">
                            @if(true){<p></p>}
                        </div>
                </div>
                }
                </div>
                }

                @functions {
                        public class Foo
                    {
                        @* This is a Razor Comment *@
                        void Method() { }
                    }
                }
                """,
            htmlFormatted: """
                @page "/"
                
                <h1>Hello, world!</h1>
                
                        Welcome to your new app.
                
                <PageTitle Title="How is Blazor working for you?" />
                
                <div class="FF"
                     id="ERT">
                    asdf
                    <div class="3"
                         id="3">
                        @if(true){<p></p>}
                    </div>
                </div>
                
                @{
                <div class="FF"
                     id="ERT">
                    asdf
                    <div class="3"
                         id="3">
                        @if(true){<p></p>}
                    </div>
                </div>
                }
                
                @{
                <div class="FF"
                     id="ERT">
                    @{
                    <div class="FF"
                         id="ERT">
                        asdf
                        <div class="3"
                             id="3">
                            @if(true){<p></p>}
                        </div>
                    </div>
                    }
                </div>
                }
                
                @functions {
                        public class Foo
                    {
                        @* This is a Razor Comment *@
                        void Method() { }
                    }
                }
                """,
            expected: """
                @page "/"

                <h1>Hello, world!</h1>

                Welcome to your new app.

                <PageTitle Title="How is Blazor working for you?" />

                <div class="FF"
                     id="ERT">
                    asdf
                    <div class="3"
                         id="3">
                        @if (true)
                        {
                            <p></p>
                        }
                    </div>
                </div>

                @{
                    <div class="FF"
                         id="ERT">
                        asdf
                        <div class="3"
                             id="3">
                            @if (true)
                            {
                                <p></p>
                            }
                        </div>
                    </div>
                }

                @{
                    <div class="FF"
                         id="ERT">
                        @{
                            <div class="FF"
                                 id="ERT">
                                asdf
                                <div class="3"
                                     id="3">
                                    @if (true)
                                    {
                                        <p></p>
                                    }
                                </div>
                            </div>
                        }
                    </div>
                }

                @functions {
                    public class Foo
                    {
                        @* This is a Razor Comment *@
                        void Method() { }
                    }
                }
                """);
    }

    [Fact]
    public async Task FormatsShortBlock()
    {
        await RunFormattingTestAsync(
            input: """
                <div>
                </div>
                @{<p></p>}
                """,
            htmlFormatted: """
                <div>
                </div>
                @{<p></p>}
                """,
            expected: """
                <div>
                </div>
                @{
                    <p></p>
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/26836")]
    public async Task FormatNestedBlock()
    {
        await RunFormattingTestAsync(
            input: """
                @code {
                    public string DoSomething()
                    {
                        <strong>
                            @DateTime.Now.ToString()
                        </strong>

                        return String.Empty;
                    }
                }
                """,
            htmlFormatted: """
                @code {
                    public string DoSomething()
                    {
                <strong>
                    @DateTime.Now.ToString()
                </strong>
                
                        return String.Empty;
                    }
                }
                """,
            expected: """
                @code {
                    public string DoSomething()
                    {
                        <strong>
                            @DateTime.Now.ToString()
                        </strong>

                        return String.Empty;
                    }
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/26836")]
    public async Task FormatNestedBlock_Tabs()
    {
        await RunFormattingTestAsync(
            input: """
                @code {
                    public string DoSomething()
                    {
                        <strong>
                            @DateTime.Now.ToString()
                        </strong>

                        return String.Empty;
                    }
                }
                """,
            htmlFormatted: """
                @code {
                    public string DoSomething()
                    {
                <strong>
                	@DateTime.Now.ToString()
                </strong>
                
                        return String.Empty;
                    }
                }
                """,
            expected: """
                @code {
                	public string DoSomething()
                	{
                		<strong>
                			@DateTime.Now.ToString()
                		</strong>

                		return String.Empty;
                	}
                }
                """, // Due to a bug in the HTML formatter, this needs to be 4
            tabSize: 4,
            insertSpaces: false);
    }

    [Fact]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1273468/")]
    public async Task FormatHtmlWithTabs1()
    {
        await RunFormattingTestAsync(
            input: """
                @page "/"
                @{
                 ViewData["Title"] = "Create";
                 <hr />
                 <div class="row">
                  <div class="col-md-4">
                   <form method="post">
                    <div class="form-group">
                     <label asp-for="Movie.Title" class="control-label"></label>
                     <input asp-for="Movie.Title" class="form-control" />
                     <span asp-validation-for="Movie.Title" class="text-danger"></span>
                    </div>
                   </form>
                  </div>
                 </div>
                }
                """,
            htmlFormatted: """
                @page "/"
                @{
                 ViewData["Title"] = "Create";
                <hr />
                <div class="row">
                	<div class="col-md-4">
                		<form method="post">
                			<div class="form-group">
                				<label asp-for="Movie.Title" class="control-label"></label>
                				<input asp-for="Movie.Title" class="form-control" />
                				<span asp-validation-for="Movie.Title" class="text-danger"></span>
                			</div>
                		</form>
                	</div>
                </div>
                }
                """,
            expected: """
                @page "/"
                @{
                	ViewData["Title"] = "Create";
                	<hr />
                	<div class="row">
                		<div class="col-md-4">
                			<form method="post">
                				<div class="form-group">
                					<label asp-for="Movie.Title" class="control-label"></label>
                					<input asp-for="Movie.Title" class="form-control" />
                					<span asp-validation-for="Movie.Title" class="text-danger"></span>
                				</div>
                			</form>
                		</div>
                	</div>
                }
                """, // Due to a bug in the HTML formatter, this needs to be 4
            tabSize: 4,
            insertSpaces: false,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1273468/")]
    public async Task FormatHtmlWithTabs2()
    {
        await RunFormattingTestAsync(
            input: """
                @page "/"

                 <hr />
                 <div class="row">
                  <div class="col-md-4">
                   <form method="post">
                    <div class="form-group">
                     <label asp-for="Movie.Title" class="control-label"></label>
                     <input asp-for="Movie.Title" class="form-control" />
                     <span asp-validation-for="Movie.Title" class="text-danger"></span>
                    </div>
                   </form>
                  </div>
                 </div>
                """,
            htmlFormatted: """
                @page "/"
                
                <hr />
                <div class="row">
                	<div class="col-md-4">
                		<form method="post">
                			<div class="form-group">
                				<label asp-for="Movie.Title" class="control-label"></label>
                				<input asp-for="Movie.Title" class="form-control" />
                				<span asp-validation-for="Movie.Title" class="text-danger"></span>
                			</div>
                		</form>
                	</div>
                </div>
                """,
            expected: """
                @page "/"

                <hr />
                <div class="row">
                	<div class="col-md-4">
                		<form method="post">
                			<div class="form-group">
                				<label asp-for="Movie.Title" class="control-label"></label>
                				<input asp-for="Movie.Title" class="form-control" />
                				<span asp-validation-for="Movie.Title" class="text-danger"></span>
                			</div>
                		</form>
                	</div>
                </div>
                """, // Due to a bug in the HTML formatter, this needs to be 4
            tabSize: 4,
            insertSpaces: false,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1273468/")]
    public async Task FormatHtmlWithTabs3()
    {
        await RunFormattingTestAsync(
            input: """
                @page "/"

                 <hr />
                 <div class="row">
                  <div class="col-md-4"
                  label="label">
                   <form method="post">
                    <div class="form-group">
                     <label asp-for="Movie.Title"
                     class="control-label"></label>
                     <input asp-for="Movie.Title" class="form-control" />
                     <span asp-validation-for="Movie.Title" class="text-danger"></span>
                    </div>
                   </form>
                  </div>
                 </div>
                """,
            htmlFormatted: """
                @page "/"
                
                <hr />
                <div class="row">
                	<div class="col-md-4"
                		 label="label">
                		<form method="post">
                			<div class="form-group">
                				<label asp-for="Movie.Title"
                					   class="control-label"></label>
                				<input asp-for="Movie.Title" class="form-control" />
                				<span asp-validation-for="Movie.Title" class="text-danger"></span>
                			</div>
                		</form>
                	</div>
                </div>
                """,
            expected: """
                @page "/"

                <hr />
                <div class="row">
                	<div class="col-md-4"
                		 label="label">
                		<form method="post">
                			<div class="form-group">
                				<label asp-for="Movie.Title"
                					   class="control-label"></label>
                				<input asp-for="Movie.Title" class="form-control" />
                				<span asp-validation-for="Movie.Title" class="text-danger"></span>
                			</div>
                		</form>
                	</div>
                </div>
                """, // Due to a bug in the HTML formatter, this needs to be 4
            tabSize: 4,
            insertSpaces: false,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/30382")]
    public async Task FormatNestedComponents()
    {
        await RunFormattingTestAsync(
            input: """
                <CascadingAuthenticationState>
                <Router AppAssembly="@typeof(Program).Assembly">
                    <Found Context="routeData">
                        <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
                    </Found>
                    <NotFound>
                        <LayoutView Layout="@typeof(MainLayout)">
                            <p>Sorry, there's nothing at this address.</p>

                            @if (true)
                                    {
                                        <strong></strong>
                                }
                        </LayoutView>
                    </NotFound>
                </Router>
                </CascadingAuthenticationState>
                """,
            htmlFormatted: """
                <CascadingAuthenticationState>
                    <Router AppAssembly="@typeof(Program).Assembly">
                        <Found Context="routeData">
                            <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
                        </Found>
                        <NotFound>
                            <LayoutView Layout="@typeof(MainLayout)">
                                <p>Sorry, there's nothing at this address.</p>
                
                                @if (true)
                                {
                                <strong></strong>
                                }
                            </LayoutView>
                        </NotFound>
                    </Router>
                </CascadingAuthenticationState>
                """,
            expected: """
                <CascadingAuthenticationState>
                    <Router AppAssembly="@typeof(Program).Assembly">
                        <Found Context="routeData">
                            <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
                        </Found>
                        <NotFound>
                            <LayoutView Layout="@typeof(MainLayout)">
                                <p>Sorry, there's nothing at this address.</p>

                                @if (true)
                                {
                                    <strong></strong>
                                }
                            </LayoutView>
                        </NotFound>
                    </Router>
                </CascadingAuthenticationState>
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/29645")]
    public async Task FormatHtmlInIf()
    {
        await RunFormattingTestAsync(
            input: """
                @if (true)
                {
                    <p><em>Loading...</em></p>
                }
                else
                {
                    <table class="table">
                        <thead>
                            <tr>
                        <th>Date</th>
                        <th>Temp. (C)</th>
                        <th>Temp. (F)</th>
                        <th>Summary</th>
                            </tr>
                        </thead>
                    </table>
                }
                """,
            htmlFormatted: """
                @if (true)
                {
                <p><em>Loading...</em></p>
                }
                else
                {
                <table class="table">
                    <thead>
                        <tr>
                            <th>Date</th>
                            <th>Temp. (C)</th>
                            <th>Temp. (F)</th>
                            <th>Summary</th>
                        </tr>
                    </thead>
                </table>
                }
                """,
            expected: """
                @if (true)
                {
                    <p><em>Loading...</em></p>
                }
                else
                {
                    <table class="table">
                        <thead>
                            <tr>
                                <th>Date</th>
                                <th>Temp. (C)</th>
                                <th>Temp. (F)</th>
                                <th>Summary</th>
                            </tr>
                        </thead>
                    </table>
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12938")]
    public async Task FormatWrappedAttributesOnShortTag()
    {
        await RunFormattingTestAsync(
            input: $$"""
                <em href="#"
                            disabled
                        style="hello"
                  @onclick="foo()">
                </em>
                """,
            htmlFormatted: $$"""
                <em href="#"
                    disabled
                    style="hello"
                    @onclick="foo()">
                </em>
                """,
            expected: $$"""
                <em href="#"
                    disabled
                    style="hello"
                    @onclick="foo()">
                </em>
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12938")]
    public async Task FormatWrappedAttributesOnShortTag_WithContent()
    {
        await RunFormattingTestAsync(
            input: $$"""
                <em href="#"
                            disabled
                        style="hello"
                  @onclick="foo()">
                    Hello World
                </em>
                """,
            htmlFormatted: $$"""
                <em href="#"
                    disabled
                    style="hello"
                    @onclick="foo()">
                    Hello World
                </em>
                """,
            expected: $$"""
                <em href="#"
                    disabled
                    style="hello"
                    @onclick="foo()">
                    Hello World
                </em>
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12938")]
    public async Task FormatWrappedAttributesOnShortTag_NestedInHtml()
    {
        await RunFormattingTestAsync(
            input: $$"""
                <div>
                    <em href="#"
                                disabled
                            style="hello"
                      @onclick="foo()">
                    </em>
                </div>
                """,
            htmlFormatted: $$"""
                <div>
                    <em href="#"
                        disabled
                        style="hello"
                        @onclick="foo()">
                    </em>
                </div>
                """,
            expected: $$"""
                <div>
                    <em href="#"
                        disabled
                        style="hello"
                        @onclick="foo()">
                    </em>
                </div>
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12938")]
    public async Task FormatWrappedAttributesOnShortTag_InIfBlock()
    {
        await RunFormattingTestAsync(
            input: $$"""
                @if (true)
                {
                    <em href="#"
                                disabled
                            style="hello"
                      @onclick="foo()">
                    </em>
                }
                """,
            htmlFormatted: $$"""
                @if (true)
                {
                <em href="#"
                    disabled
                    style="hello"
                    @onclick="foo()">
                </em>
                }
                """,
            expected: $$"""
                @if (true)
                {
                    <em href="#"
                        disabled
                        style="hello"
                        @onclick="foo()">
                    </em>
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12938")]
    public async Task FormatWrappedAttributesOnShortestTag()
    {
        await RunFormattingTestAsync(
            input: $$"""
                <a href="#"
                            disabled
                        style="hello"
                  @onclick="foo()">
                </a>
                """,
            htmlFormatted: $$"""
                <a href="#"
                   disabled
                   style="hello"
                   @onclick="foo()">
                </a>
                """,
            expected: $$"""
                <a href="#"
                   disabled
                   style="hello"
                   @onclick="foo()">
                </a>
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12938")]
    public async Task FormatWrappedAttributesOnShortestTag_WithContent()
    {
        await RunFormattingTestAsync(
            input: $$"""
                <a href="#"
                            disabled
                        style="hello"
                  @onclick="foo()">
                    Hello World
                </a>
                """,
            htmlFormatted: $$"""
                <a href="#"
                   disabled
                   style="hello"
                   @onclick="foo()">
                    Hello World
                </a>
                """,
            expected: $$"""
                <a href="#"
                   disabled
                   style="hello"
                   @onclick="foo()">
                    Hello World
                </a>
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12938")]
    public async Task FormatWrappedAttributesOnShortestTag_NestedInHtml()
    {
        await RunFormattingTestAsync(
            input: $$"""
                <div>
                    <a href="#"
                                disabled
                            style="hello"
                      @onclick="foo()">
                    </a>
                </div>
                """,
            htmlFormatted: $$"""
                <div>
                    <a href="#"
                       disabled
                       style="hello"
                       @onclick="foo()">
                    </a>
                </div>
                """,
            expected: $$"""
                <div>
                    <a href="#"
                       disabled
                       style="hello"
                       @onclick="foo()">
                    </a>
                </div>
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12938")]
    public async Task FormatWrappedAttributesOnShortestTag_InIfBlock()
    {
        await RunFormattingTestAsync(
            input: $$"""
                @if (true)
                {
                    <a href="#"
                                disabled
                            style="hello"
                      @onclick="foo()">
                    </a>
                }
                """,
            htmlFormatted: $$"""
                @if (true)
                {
                <a href="#"
                   disabled
                   style="hello"
                   @onclick="foo()">
                </a>
                }
                """,
            expected: $$"""
                @if (true)
                {
                    <a href="#"
                       disabled
                       style="hello"
                       @onclick="foo()">
                    </a>
                }
                """);
    }

    [Fact]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2471065")]
    public Task MultipleHtmlElementsInCSharpCode()
        => RunFormattingTestAsync(
            input: """
                <div class="p-3 pb-0" style="min-height:24rem;">

                @if (productsForExport != null)
                {
                <label class="section-label">
                @(!showResult ? "some label" : "another label")
                </label>

                <div class="row">
                <div class="col">
                @if (!showResult)
                {
                <label>
                <input type="checkbox" class="me-2 mt-1" />
                <span>some label</span>
                </label>
                }
                </div>

                </div>
                }

                </div>

                @code {
                bool showResult = false;
                object? productsForExport = null;
                }
                """,
            htmlFormatted: """
                <div class="p-3 pb-0" style="min-height:24rem;">
                
                    @if (productsForExport != null)
                    {
                    <label class="section-label">
                        @(!showResult ? "some label" : "another label")
                    </label>
                
                    <div class="row">
                        <div class="col">
                            @if (!showResult)
                            {
                            <label>
                                <input type="checkbox" class="me-2 mt-1" />
                                <span>some label</span>
                            </label>
                            }
                        </div>
                
                    </div>
                    }
                
                </div>
                
                @code {
                bool showResult = false;
                object? productsForExport = null;
                }
                """,
            expected: """
                <div class="p-3 pb-0" style="min-height:24rem;">

                    @if (productsForExport != null)
                    {
                        <label class="section-label">
                            @(!showResult ? "some label" : "another label")
                        </label>

                        <div class="row">
                            <div class="col">
                                @if (!showResult)
                                {
                                    <label>
                                        <input type="checkbox" class="me-2 mt-1" />
                                        <span>some label</span>
                                    </label>
                                }
                            </div>

                        </div>
                    }

                </div>

                @code {
                    bool showResult = false;
                    object? productsForExport = null;
                }
                """);

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/29645")]
    public async Task FormatHtmlInIf_Range()
    {
        await RunFormattingTestAsync(
            input: """
                @if (true)
                {
                    <p><em>Loading...</em></p>
                }
                else
                {
                    <table class="table">
                        <thead>
                            <tr>
                [|      <th>Date</th>
                        <th>Temp. (C)</th>
                        <th>Temp. (F)</th>
                        <th>Summary</th>|]
                            </tr>
                        </thead>
                    </table>
                }
                """,
            htmlFormatted: """
                @if (true)
                {
                <p><em>Loading...</em></p>
                }
                else
                {
                <table class="table">
                    <thead>
                        <tr>
                            <th>Date</th>
                            <th>Temp. (C)</th>
                            <th>Temp. (F)</th>
                            <th>Summary</th>
                        </tr>
                    </thead>
                </table>
                }
                """,
            expected: """
                @if (true)
                {
                    <p><em>Loading...</em></p>
                }
                else
                {
                    <table class="table">
                        <thead>
                            <tr>
                                <th>Date</th>
                                <th>Temp. (C)</th>
                                <th>Temp. (F)</th>
                                <th>Summary</th>
                            </tr>
                        </thead>
                    </table>
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/5749")]
    public async Task FormatRenderFragmentInCSharpCodeBlock1()
    {
        // Sadly the first thing the HTML formatter does with this input
        // is put a newline after the @, which means <PageTitle /> won't be
        // seen as a component any more, so we have to turn off our validation,
        // or the test fails before we have a chance to fix the formatting.
        FormattingContext.SkipValidateComponents = true;

        await RunFormattingTestAsync(
            input: """
                @code
                {
                    public void DoStuff(RenderFragment renderFragment)
                    {
                        DoThings();
                        renderFragment(@<PageTitle Title="Foo" />);
                DoThings();
                renderFragment(@<PageTitle          Title="Foo"             />);

                        @* comment *@
                <div></div>

                        @* comment *@<div></div>
                    }
                }
                """,
            htmlFormatted: """
                @code
                {
                    public void DoStuff(RenderFragment renderFragment)
                    {
                        DoThings();
                        renderFragment(@
                <PageTitle Title="Foo" />);
                DoThings();
                renderFragment(@
                <PageTitle Title="Foo" />);
                
                        @* comment *@
                <div></div>
                
                        @* comment *@<div></div>
                    }
                }
                """,
            expected: """
                @code
                {
                    public void DoStuff(RenderFragment renderFragment)
                    {
                        DoThings();
                        renderFragment(@<PageTitle Title="Foo" />);
                        DoThings();
                        renderFragment(@<PageTitle Title="Foo" />);

                        @* comment *@
                        <div></div>

                        @* comment *@
                        <div></div>
                    }
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/5749")]
    public async Task FormatRenderFragmentInCSharpCodeBlock2()
    {
        // Sadly the first thing the HTML formatter does with this input
        // is put a newline after the @, which means <PageTitle /> won't be
        // seen as a component any more, so we have to turn off our validation,
        // or the test fails before we have a chance to fix the formatting.
        FormattingContext.SkipValidateComponents = true;

        await RunFormattingTestAsync(
            input: """
                <div>
                @{
                    renderFragment(@<PageTitle Title="Foo" />);

                        @* comment *@
                <div></div>

                        @* comment *@<div></div>
                    }
                </div>
                """,
            htmlFormatted: """
                <div>
                    @{
                    renderFragment(@<PageTitle Title="Foo" />);
                
                    @* comment *@
                    <div></div>
                
                    @* comment *@<div></div>
                    }
                </div>
                """,
            expected: """
                <div>
                    @{
                        renderFragment(@<PageTitle Title="Foo" />);

                        @* comment *@
                        <div></div>

                        @* comment *@
                        <div></div>
                    }
                </div>
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/5749")]
    public async Task FormatRenderFragmentInCSharpCodeBlock3()
    {
        // Sadly the first thing the HTML formatter does with this input
        // is put a newline after the @, which means <PageTitle /> won't be
        // seen as a component any more, so we have to turn off our validation,
        // or the test fails before we have a chance to fix the formatting.
        FormattingContext.SkipValidateComponents = true;

        await RunFormattingTestAsync(
            input: """
                <div>
                @{
                    renderFragment    (@<PageTitle      Title=  "Foo"     />);

                        @* comment *@
                <div></div>

                        @* comment *@<div></div>
                    }
                </div>
                """,
            htmlFormatted: """
                <div>
                    @{
                    renderFragment    (@<PageTitle Title="Foo" />);
                
                    @* comment *@
                    <div></div>
                
                    @* comment *@<div></div>
                    }
                </div>
                """,
            expected: """
                <div>
                    @{
                        renderFragment(@<PageTitle Title="Foo" />);

                        @* comment *@
                        <div></div>

                        @* comment *@
                        <div></div>
                    }
                </div>
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12635")]
    public Task TwoRenderFragmentsAfterEachOther()
     => RunFormattingTestAsync(
            input: """
                @{
                Func<(bool b1, bool b2), object> o1 = @<text>
                <div></div>
                </text>;
                Func<(bool b1, bool b2), object> o2 = @<text>
                <div></div>
                </text>;
                }
                """,
            htmlFormatted: """
                @{
                Func<(bool b1, bool b2), object> o1 = @<text>
                    <div></div>
                </text>;
                Func<(bool b1, bool b2), object> o2 = @<text>
                    <div></div>
                </text>;
                }
                """,
            expected: """
                @{
                    Func<(bool b1, bool b2), object> o1 = @<text>
                        <div></div>
                    </text>;
                    Func<(bool b1, bool b2), object> o2 = @<text>
                        <div></div>
                    </text>;
                }
                """);

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/6090")]
    public async Task FormatHtmlCommentsInsideCSharp1()
    {
        await RunFormattingTestAsync(
            input: """
                @foreach (var num in Enumerable.Range(1, 10))
                {
                    <span class="skill_result btn">
                        <!--asdfasd-->
                        <span style="margin-left:0px">
                            <svg>
                                <rect width="1" height="1" />
                            </svg>
                        </span>
                        <!--adfasfd-->
                    </span>
                }
                """,
            htmlFormatted: """
                @foreach (var num in Enumerable.Range(1, 10))
                {
                <span class="skill_result btn">
                    <!--asdfasd-->
                    <span style="margin-left:0px">
                        <svg>
                            <rect width="1" height="1" />
                        </svg>
                    </span>
                    <!--adfasfd-->
                </span>
                }
                """,
            expected: """
                @foreach (var num in Enumerable.Range(1, 10))
                {
                    <span class="skill_result btn">
                        <!--asdfasd-->
                        <span style="margin-left:0px">
                            <svg>
                                <rect width="1" height="1" />
                            </svg>
                        </span>
                        <!--adfasfd-->
                    </span>
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/6090")]
    public async Task FormatHtmlCommentsInsideCSharp2()
    {
        await RunFormattingTestAsync(
            input: """
                @foreach (var num in Enumerable.Range(1, 10))
                {
                    <span class="skill_result btn">
                        <!--asdfasd-->
                        <input type="text" />
                        <!--adfasfd-->
                    </span>
                }
                """,
            htmlFormatted: """
                @foreach (var num in Enumerable.Range(1, 10))
                {
                <span class="skill_result btn">
                    <!--asdfasd-->
                    <input type="text" />
                    <!--adfasfd-->
                </span>
                }
                """,
            expected: """
                @foreach (var num in Enumerable.Range(1, 10))
                {
                    <span class="skill_result btn">
                        <!--asdfasd-->
                        <input type="text" />
                        <!--adfasfd-->
                    </span>
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/6090")]
    public async Task FormatHtmlCommentsInsideCSharp3()
    {
        await RunFormattingTestAsync(
            input: """
                @foreach (var num in Enumerable.Range(1, 10))
                {
                    <span class="skill_result btn">
                            <!-- this is a
                            very long
                        comment in Html -->
                        <input type="text" />
                                <!-- this is a
                        very long
                        comment in Html
                            -->
                    </span>
                }
                """,
            htmlFormatted: """
                @foreach (var num in Enumerable.Range(1, 10))
                {
                <span class="skill_result btn">
                    <!-- this is a
                        very long
                    comment in Html -->
                    <input type="text" />
                    <!-- this is a
                    very long
                    comment in Html
                        -->
                </span>
                }
                """,
            expected: """
                @foreach (var num in Enumerable.Range(1, 10))
                {
                    <span class="skill_result btn">
                        <!-- this is a
                            very long
                        comment in Html -->
                        <input type="text" />
                        <!-- this is a
                        very long
                        comment in Html
                            -->
                    </span>
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/6001")]
    public async Task FormatNestedCascadingValue()
    {
        await RunFormattingTestAsync(
            input: """
                @using Microsoft.AspNetCore.Components.Forms;

                @if (Object1!= null)
                {
                    <CascadingValue Value="Variable1">
                        <CascadingValue Value="Variable2">
                            <PageTitle  />
                            @if (VarBool)
                        {
                            <div class="mb-16">
                                <PageTitle  />
                                <PageTitle  />
                            </div>
                        }
                    </CascadingValue>
                </CascadingValue>
                }

                @code
                {
                    public object Object1 {get;set;}
                    public object Variable1 {get;set;}
                public object Variable2 {get;set;}
                public bool VarBool {get;set;}
                }
                """,
            htmlFormatted: """
                @using Microsoft.AspNetCore.Components.Forms;
                
                @if (Object1!= null)
                {
                <CascadingValue Value="Variable1">
                    <CascadingValue Value="Variable2">
                        <PageTitle />
                        @if (VarBool)
                        {
                        <div class="mb-16">
                            <PageTitle />
                            <PageTitle />
                        </div>
                        }
                    </CascadingValue>
                </CascadingValue>
                }
                
                @code
                {
                    public object Object1 {get;set;}
                    public object Variable1 {get;set;}
                public object Variable2 {get;set;}
                public bool VarBool {get;set;}
                }
                """,
            expected: """
                @using Microsoft.AspNetCore.Components.Forms;

                @if (Object1 != null)
                {
                    <CascadingValue Value="Variable1">
                        <CascadingValue Value="Variable2">
                            <PageTitle />
                            @if (VarBool)
                            {
                                <div class="mb-16">
                                    <PageTitle />
                                    <PageTitle />
                                </div>
                            }
                        </CascadingValue>
                    </CascadingValue>
                }

                @code
                {
                    public object Object1 { get; set; }
                    public object Variable1 { get; set; }
                    public object Variable2 { get; set; }
                    public bool VarBool { get; set; }
                }
                """,
            fileKind: RazorFileKind.Component);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/6001")]
    public async Task FormatNestedCascadingValue2()
    {
        await RunFormattingTestAsync(
            input: """
                @using Microsoft.AspNetCore.Components.Forms;

                @if (Object1!= null)
                {
                    <CascadingValue Value="Variable1">
                            <PageTitle  />
                            @if (VarBool)
                        {
                            <div class="mb-16">
                                <PageTitle  />
                                <PageTitle  />
                            </div>
                        }
                </CascadingValue>
                }

                @code
                {
                    public object Object1 {get;set;}
                    public object Variable1 {get;set;}
                public object Variable2 {get;set;}
                public bool VarBool {get;set;}
                }
                """,
            htmlFormatted: """
                @using Microsoft.AspNetCore.Components.Forms;
                
                @if (Object1!= null)
                {
                <CascadingValue Value="Variable1">
                    <PageTitle />
                    @if (VarBool)
                    {
                    <div class="mb-16">
                        <PageTitle />
                        <PageTitle />
                    </div>
                    }
                </CascadingValue>
                }
                
                @code
                {
                    public object Object1 {get;set;}
                    public object Variable1 {get;set;}
                public object Variable2 {get;set;}
                public bool VarBool {get;set;}
                }
                """,
            expected: """
                @using Microsoft.AspNetCore.Components.Forms;

                @if (Object1 != null)
                {
                    <CascadingValue Value="Variable1">
                        <PageTitle />
                        @if (VarBool)
                        {
                            <div class="mb-16">
                                <PageTitle />
                                <PageTitle />
                            </div>
                        }
                    </CascadingValue>
                }

                @code
                {
                    public object Object1 { get; set; }
                    public object Variable1 { get; set; }
                    public object Variable2 { get; set; }
                    public bool VarBool { get; set; }
                }
                """,
            fileKind: RazorFileKind.Component); // tracked by https://github.com/dotnet/razor/issues/10836
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/6001")]
    public async Task FormatNestedCascadingValue3()
    {
        await RunFormattingTestAsync(
            input: """
                @using Microsoft.AspNetCore.Components.Forms;

                @if (Object1!= null)
                {
                    @if (VarBool)
                    {
                            <PageTitle  />
                            @if (VarBool)
                        {
                            <div class="mb-16">
                                <PageTitle  />
                                <PageTitle  />
                            </div>
                        }
                }
                }

                @code
                {
                    public object Object1 {get;set;}
                    public object Variable1 {get;set;}
                public object Variable2 {get;set;}
                public bool VarBool {get;set;}
                }
                """,
            htmlFormatted: """
                @using Microsoft.AspNetCore.Components.Forms;
                
                @if (Object1!= null)
                {
                    @if (VarBool)
                    {
                <PageTitle />
                            @if (VarBool)
                        {
                <div class="mb-16">
                    <PageTitle />
                    <PageTitle />
                </div>
                        }
                }
                }
                
                @code
                {
                    public object Object1 {get;set;}
                    public object Variable1 {get;set;}
                public object Variable2 {get;set;}
                public bool VarBool {get;set;}
                }
                """,
            expected: """
                @using Microsoft.AspNetCore.Components.Forms;

                @if (Object1 != null)
                {
                    @if (VarBool)
                    {
                        <PageTitle />
                        @if (VarBool)
                        {
                            <div class="mb-16">
                                <PageTitle />
                                <PageTitle />
                            </div>
                        }
                    }
                }

                @code
                {
                    public object Object1 { get; set; }
                    public object Variable1 { get; set; }
                    public object Variable2 { get; set; }
                    public bool VarBool { get; set; }
                }
                """,
            fileKind: RazorFileKind.Component);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/6001")]
    public async Task FormatNestedCascadingValue4()
    {
        await RunFormattingTestAsync(
            input: """
                @using Microsoft.AspNetCore.Components.Forms;

                    <CascadingValue Value="Variable1">
                            <PageTitle  />
                            @if (VarBool)
                        {
                            <div class="mb-16">
                                <PageTitle  />
                                <PageTitle  />
                            </div>
                        }
                </CascadingValue>

                @code
                {
                    public object Object1 {get;set;}
                    public object Variable1 {get;set;}
                public object Variable2 {get;set;}
                public bool VarBool {get;set;}
                }
                """,
            htmlFormatted: """
                @using Microsoft.AspNetCore.Components.Forms;
                
                <CascadingValue Value="Variable1">
                    <PageTitle />
                    @if (VarBool)
                    {
                    <div class="mb-16">
                        <PageTitle />
                        <PageTitle />
                    </div>
                    }
                </CascadingValue>
                
                @code
                {
                    public object Object1 {get;set;}
                    public object Variable1 {get;set;}
                public object Variable2 {get;set;}
                public bool VarBool {get;set;}
                }
                """,
            expected: """
                @using Microsoft.AspNetCore.Components.Forms;

                <CascadingValue Value="Variable1">
                    <PageTitle />
                    @if (VarBool)
                    {
                        <div class="mb-16">
                            <PageTitle />
                            <PageTitle />
                        </div>
                    }
                </CascadingValue>

                @code
                {
                    public object Object1 { get; set; }
                    public object Variable1 { get; set; }
                    public object Variable2 { get; set; }
                    public bool VarBool { get; set; }
                }
                """,
            fileKind: RazorFileKind.Component);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/6001")]
    public async Task FormatNestedCascadingValue5()
    {
        await RunFormattingTestAsync(
            input: """
                @using Microsoft.AspNetCore.Components.Forms;

                @if (Object1!= null)
                {
                    <PageTitle>
                            <PageTitle  />
                            @if (VarBool)
                        {
                            <div class="mb-16">
                                <PageTitle  />
                                <PageTitle  />
                            </div>
                        }
                </PageTitle>
                }

                @code
                {
                    public object Object1 {get;set;}
                    public object Variable1 {get;set;}
                public object Variable2 {get;set;}
                public bool VarBool {get;set;}
                }
                """,
            htmlFormatted: """
                @using Microsoft.AspNetCore.Components.Forms;
                
                @if (Object1!= null)
                {
                <PageTitle>
                    <PageTitle />
                    @if (VarBool)
                    {
                    <div class="mb-16">
                        <PageTitle />
                        <PageTitle />
                    </div>
                    }
                </PageTitle>
                }
                
                @code
                {
                    public object Object1 {get;set;}
                    public object Variable1 {get;set;}
                public object Variable2 {get;set;}
                public bool VarBool {get;set;}
                }
                """,
            expected: """
                @using Microsoft.AspNetCore.Components.Forms;

                @if (Object1 != null)
                {
                    <PageTitle>
                        <PageTitle />
                        @if (VarBool)
                        {
                            <div class="mb-16">
                                <PageTitle />
                                <PageTitle />
                            </div>
                        }
                    </PageTitle>
                }

                @code
                {
                    public object Object1 { get; set; }
                    public object Variable1 { get; set; }
                    public object Variable2 { get; set; }
                    public bool VarBool { get; set; }
                }
                """,
            fileKind: RazorFileKind.Component);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/6001")]
    public async Task FormatNestedCascadingValue6()
    {
        await RunFormattingTestAsync(
            input: """
                @using Microsoft.AspNetCore.Components.Forms;

                @if (Object1!= null)
                {
                    <CascadingValue Value="Variable1">
                    <div>
                            <PageTitle  />
                            @if (VarBool)
                        {
                            <div class="mb-16">
                                <PageTitle  />
                                <PageTitle  />
                            </div>
                        }
                        </div>
                </CascadingValue>
                }

                @code
                {
                    public object Object1 {get;set;}
                    public object Variable1 {get;set;}
                public object Variable2 {get;set;}
                public bool VarBool {get;set;}
                }
                """,
            htmlFormatted: """
                @using Microsoft.AspNetCore.Components.Forms;
                
                @if (Object1!= null)
                {
                <CascadingValue Value="Variable1">
                    <div>
                        <PageTitle />
                        @if (VarBool)
                        {
                        <div class="mb-16">
                            <PageTitle />
                            <PageTitle />
                        </div>
                        }
                    </div>
                </CascadingValue>
                }
                
                @code
                {
                    public object Object1 {get;set;}
                    public object Variable1 {get;set;}
                public object Variable2 {get;set;}
                public bool VarBool {get;set;}
                }
                """,
            expected: """
                @using Microsoft.AspNetCore.Components.Forms;

                @if (Object1 != null)
                {
                    <CascadingValue Value="Variable1">
                        <div>
                            <PageTitle />
                            @if (VarBool)
                            {
                                <div class="mb-16">
                                    <PageTitle />
                                    <PageTitle />
                                </div>
                            }
                        </div>
                    </CascadingValue>
                }

                @code
                {
                    public object Object1 { get; set; }
                    public object Variable1 { get; set; }
                    public object Variable2 { get; set; }
                    public bool VarBool { get; set; }
                }
                """,
            fileKind: RazorFileKind.Component);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/5676")]
    public async Task FormatInputSelect()
    {
        await RunFormattingTestAsync(
            input: """
                @using Microsoft.AspNetCore.Components.Forms;

                @code {
                    private string _id {get;set;}
                }

                <div>
                    @if (true)
                    {
                        <div>
                            <InputSelect @bind-Value="_id">
                                @if (true)
                                {
                                    <option>goo</option>
                                }
                            </InputSelect>
                        </div>
                    }
                </div>
                """,
            htmlFormatted: """
                @using Microsoft.AspNetCore.Components.Forms;
                
                @code {
                    private string _id {get;set;}
                }
                
                <div>
                    @if (true)
                    {
                    <div>
                        <InputSelect @bind-Value="_id">
                            @if (true)
                            {
                            <option>goo</option>
                            }
                        </InputSelect>
                    </div>
                    }
                </div>
                """,
            expected: """
                @using Microsoft.AspNetCore.Components.Forms;

                @code {
                    private string _id { get; set; }
                }

                <div>
                    @if (true)
                    {
                        <div>
                            <InputSelect @bind-Value="_id">
                                @if (true)
                                {
                                    <option>goo</option>
                                }
                            </InputSelect>
                        </div>
                    }
                </div>
                """,
            fileKind: RazorFileKind.Component);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/5676")]
    public async Task FormatInputSelect2()
    {
        await RunFormattingTestAsync(
            input: """
                @using Microsoft.AspNetCore.Components.Forms;

                @code {
                    private string _id {get;set;}
                }

                <div>
                        <div>
                            <InputSelect @bind-Value="_id">
                                @if (true)
                                {
                                    <option>goo</option>
                                }
                            </InputSelect>
                        </div>
                </div>
                """,
            htmlFormatted: """
                @using Microsoft.AspNetCore.Components.Forms;
                
                @code {
                    private string _id {get;set;}
                }
                
                <div>
                    <div>
                        <InputSelect @bind-Value="_id">
                            @if (true)
                            {
                            <option>goo</option>
                            }
                        </InputSelect>
                    </div>
                </div>
                """,
            expected: """
                @using Microsoft.AspNetCore.Components.Forms;

                @code {
                    private string _id { get; set; }
                }

                <div>
                    <div>
                        <InputSelect @bind-Value="_id">
                            @if (true)
                            {
                                <option>goo</option>
                            }
                        </InputSelect>
                    </div>
                </div>
                """,
            fileKind: RazorFileKind.Component);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/5676")]
    public async Task FormatInputSelect3()
    {
        await RunFormattingTestAsync(
            input: """
                @using Microsoft.AspNetCore.Components.Forms;

                @code {
                    private string _id {get;set;}
                }

                <div>
                        <div>
                            <InputSelect @bind-Value="_id">
                                    <option>goo</option>
                            </InputSelect>
                        </div>
                </div>
                """,
            htmlFormatted: """
                @using Microsoft.AspNetCore.Components.Forms;
                
                @code {
                    private string _id {get;set;}
                }
                
                <div>
                    <div>
                        <InputSelect @bind-Value="_id">
                            <option>goo</option>
                        </InputSelect>
                    </div>
                </div>
                """,
            expected: """
                @using Microsoft.AspNetCore.Components.Forms;

                @code {
                    private string _id { get; set; }
                }

                <div>
                    <div>
                        <InputSelect @bind-Value="_id">
                            <option>goo</option>
                        </InputSelect>
                    </div>
                </div>
                """,
            fileKind: RazorFileKind.Component);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/5676")]
    public async Task FormatInputSelect4()
    {
        await RunFormattingTestAsync(
            input: """
                @using Microsoft.AspNetCore.Components.Forms;

                @code {
                    private string _id {get;set;}
                }

                <div>
                    @if (true)
                    {
                        <div>
                            <InputSelect @bind-Value="_id">
                                    <option>goo</option>
                            </InputSelect>
                        </div>
                    }
                </div>
                """,
            htmlFormatted: """
                @using Microsoft.AspNetCore.Components.Forms;
                
                @code {
                    private string _id {get;set;}
                }
                
                <div>
                    @if (true)
                    {
                    <div>
                        <InputSelect @bind-Value="_id">
                            <option>goo</option>
                        </InputSelect>
                    </div>
                    }
                </div>
                """,
            expected: """
                @using Microsoft.AspNetCore.Components.Forms;

                @code {
                    private string _id { get; set; }
                }

                <div>
                    @if (true)
                    {
                        <div>
                            <InputSelect @bind-Value="_id">
                                <option>goo</option>
                            </InputSelect>
                        </div>
                    }
                </div>
                """,
            fileKind: RazorFileKind.Component);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/8606")]
    public async Task FormatAttributesWithTransition()
    {
        await RunFormattingTestAsync(
            input: """
                @using Microsoft.AspNetCore.Components.Forms;

                @code {
                    private string _id {get;set;}
                }

                <div>
                    @if (true)
                    {
                        <div>
                            <InputSelect CssClass="goo"
                                 @bind-Value="_id"
                               @ref="elem"
                                CurrentValue="boo">
                                    <option>goo</option>
                            </InputSelect>
                        </div>
                    }
                </div>
                """,
            htmlFormatted: """
                @using Microsoft.AspNetCore.Components.Forms;
                
                @code {
                    private string _id {get;set;}
                }
                
                <div>
                    @if (true)
                    {
                    <div>
                        <InputSelect CssClass="goo"
                                     @bind-Value="_id"
                                     @ref="elem"
                                     CurrentValue="boo">
                            <option>goo</option>
                        </InputSelect>
                    </div>
                    }
                </div>
                """,
            expected: """
                @using Microsoft.AspNetCore.Components.Forms;

                @code {
                    private string _id { get; set; }
                }

                <div>
                    @if (true)
                    {
                        <div>
                            <InputSelect CssClass="goo"
                                         @bind-Value="_id"
                                         @ref="elem"
                                         CurrentValue="boo">
                                <option>goo</option>
                            </InputSelect>
                        </div>
                    }
                </div>
                """,
            fileKind: RazorFileKind.Component);
    }

    [Fact]
    public async Task FormatEventHandlerAttributes()
    {
        await RunFormattingTestAsync(
            input: """
                <p>Current count: @currentCount</p>

                <button @onclick="IncrementCount">Increment</button>
                <button @onclick="@(e=>currentCount=4)">Update to 4</button>
                <button @onclick="e=>currentCount=5">Update to 5</button>

                @code {
                    private int currentCount=0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            htmlFormatted: """
                <p>Current count: @currentCount</p>
                
                <button @onclick="IncrementCount">Increment</button>
                <button @onclick="@(e=>currentCount=4)">Update to 4</button>
                <button @onclick="e=>currentCount=5">Update to 5</button>
                
                @code {
                    private int currentCount=0;
                
                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            expected: """
                <p>Current count: @currentCount</p>

                <button @onclick="IncrementCount">Increment</button>
                <button @onclick="@(e => currentCount = 4)">Update to 4</button>
                <button @onclick="e => currentCount = 5">Update to 5</button>

                @code {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            fileKind: RazorFileKind.Component);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12785")]
    public async Task FormatEventHandlerAttributes_BlockBodiedLambda()
    {
        await RunFormattingTestAsync(
            input: """
                <button @onclick="()=>{
                StateHasChanged();}">
                </button>
                """,
            htmlFormatted: """
                <button @onclick="()=>{
                StateHasChanged();}">
                </button>
                """,
            expected: """
                <button @onclick="() =>
                        {
                            StateHasChanged();
                        }">
                </button>
                """,
            csharpSyntaxFormattingOptions: GetNewLineBeforeBraceInLambdaExpressionOptions(newLineBeforeBraceInLambda: true),
            fileKind: RazorFileKind.Component);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12785")]
    public async Task FormatEventHandlerAttributes_BlockBodiedLambda_NoNewLineBeforeOpenBraceInLambdaExpressionBody()
    {
        await RunFormattingTestAsync(
            input: """
                <button @onclick="()=>{
                StateHasChanged();}">
                </button>
                """,
            htmlFormatted: """
                <button @onclick="()=>{
                StateHasChanged();}">
                </button>
                """,
            expected: """
                <button @onclick="() => {
                            StateHasChanged();
                        }">
                </button>
                """,
            csharpSyntaxFormattingOptions: GetNewLineBeforeBraceInLambdaExpressionOptions(newLineBeforeBraceInLambda: false),
            fileKind: RazorFileKind.Component);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12785")]
    public async Task FormatEventHandlerAttributes_BlockBodiedLambda2()
    {
        await RunFormattingTestAsync(
            input: """
                <button foo="bar"
                @onclick="()=>{
                StateHasChanged();}">
                </button>
                """,
            htmlFormatted: """
                <button foo="bar"
                        @onclick="()=>{
                StateHasChanged();}">
                </button>
                """,
            expected: """
                <button foo="bar"
                        @onclick="() =>
                        {
                            StateHasChanged();
                        }">
                </button>
                """,
            csharpSyntaxFormattingOptions: GetNewLineBeforeBraceInLambdaExpressionOptions(newLineBeforeBraceInLambda: true),
            fileKind: RazorFileKind.Component);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12785")]
    public async Task FormatEventHandlerAttributes_BlockBodiedLambda2_NoNewLineBeforeOpenBraceInLambdaExpressionBody()
    {
        await RunFormattingTestAsync(
            input: """
                <button foo="bar"
                @onclick="()=>{
                StateHasChanged();}">
                </button>
                """,
            htmlFormatted: """
                <button foo="bar"
                        @onclick="()=>{
                StateHasChanged();}">
                </button>
                """,
            expected: """
                <button foo="bar"
                        @onclick="() => {
                            StateHasChanged();
                        }">
                </button>
                """,
            csharpSyntaxFormattingOptions: GetNewLineBeforeBraceInLambdaExpressionOptions(newLineBeforeBraceInLambda: false),
            fileKind: RazorFileKind.Component);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12785")]
    public async Task FormatEventHandlerAttributes_BlockBodiedLambda3()
    {
        await RunFormattingTestAsync(
            input: """
                <div>
                <button foo="bar"
                @onclick="()=>{
                StateHasChanged();}">
                </button>
                </div>
                """,
            htmlFormatted: """
                <div>
                    <button foo="bar"
                            @onclick="()=>{
                StateHasChanged();}">
                    </button>
                </div>
                """,
            expected: """
                <div>
                    <button foo="bar"
                            @onclick="() =>
                            {
                                StateHasChanged();
                            }">
                    </button>
                </div>
                """,
            csharpSyntaxFormattingOptions: GetNewLineBeforeBraceInLambdaExpressionOptions(newLineBeforeBraceInLambda: true),
            fileKind: RazorFileKind.Component);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12785")]
    public async Task FormatEventHandlerAttributes_BlockBodiedLambda3_NoNewLineBeforeOpenBraceInLambdaExpressionBody()
    {
        await RunFormattingTestAsync(
            input: """
                <div>
                <button foo="bar"
                @onclick="()=>{
                StateHasChanged();}">
                </button>
                </div>
                """,
            htmlFormatted: """
                <div>
                    <button foo="bar"
                            @onclick="()=>{
                StateHasChanged();}">
                    </button>
                </div>
                """,
            expected: """
                <div>
                    <button foo="bar"
                            @onclick="() => {
                                StateHasChanged();
                            }">
                    </button>
                </div>
                """,
            csharpSyntaxFormattingOptions: GetNewLineBeforeBraceInLambdaExpressionOptions(newLineBeforeBraceInLambda: false),
            fileKind: RazorFileKind.Component);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12785")]
    public async Task FormatEventHandlerAttributes_BlockBodiedLambda4()
    {
        await RunFormattingTestAsync(
            input: """
                    <div>
                            <button foo="bar"
                                            @onclick="()=>{
                                                             StateHasChanged();}">
                            </button>
                    </div>
                """,
            htmlFormatted: """
                <div>
                    <button foo="bar"
                            @onclick="()=>{
                                                             StateHasChanged();}">
                    </button>
                </div>
                """,
            expected: """
                <div>
                    <button foo="bar"
                            @onclick="() =>
                            {
                                StateHasChanged();
                            }">
                    </button>
                </div>
                """,
            csharpSyntaxFormattingOptions: GetNewLineBeforeBraceInLambdaExpressionOptions(newLineBeforeBraceInLambda: true),
            fileKind: RazorFileKind.Component);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12785")]
    public async Task FormatEventHandlerAttributes_BlockBodiedLambda4_NoNewLineBeforeOpenBraceInLambdaExpressionBody()
    {
        await RunFormattingTestAsync(
            input: """
                    <div>
                            <button foo="bar"
                                            @onclick="()=>{
                                                             StateHasChanged();}">
                            </button>
                    </div>
                """,
            htmlFormatted: """
                <div>
                    <button foo="bar"
                            @onclick="()=>{
                                                             StateHasChanged();}">
                    </button>
                </div>
                """,
            expected: """
                <div>
                    <button foo="bar"
                            @onclick="() => {
                                StateHasChanged();
                            }">
                    </button>
                </div>
                """,
            csharpSyntaxFormattingOptions: GetNewLineBeforeBraceInLambdaExpressionOptions(newLineBeforeBraceInLambda: false),
            fileKind: RazorFileKind.Component);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12785")]
    public async Task FormatEventHandlerAttributes_BlockBodiedLambda_BraceOnNextLine()
    {
        await RunFormattingTestAsync(
            input: """
                <button @onclick="() =>
                {
                StateHasChanged();}">
                </button>
                """,
            htmlFormatted: """
                <button @onclick="() =>
                {
                StateHasChanged();}">
                </button>
                """,
            expected: """
                <button @onclick="() =>
                        {
                            StateHasChanged();
                        }">
                </button>
                """,
            csharpSyntaxFormattingOptions: GetNewLineBeforeBraceInLambdaExpressionOptions(newLineBeforeBraceInLambda: true),
            fileKind: RazorFileKind.Component);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12785")]
    public async Task FormatEventHandlerAttributes_BlockBodiedLambda_BraceOnNextLine_NoNewLineBeforeOpenBraceInLambdaExpressionBody()
    {
        await RunFormattingTestAsync(
            input: """
                <button @onclick="() =>
                {
                StateHasChanged();}">
                </button>
                """,
            htmlFormatted: """
                <button @onclick="() =>
                {
                StateHasChanged();}">
                </button>
                """,
            expected: """
                <button @onclick="() => {
                            StateHasChanged();
                        }">
                </button>
                """,
            csharpSyntaxFormattingOptions: GetNewLineBeforeBraceInLambdaExpressionOptions(newLineBeforeBraceInLambda: false),
            fileKind: RazorFileKind.Component);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12785")]
    public async Task FormatEventHandlerAttributes_BlockBodiedLambda_BraceOnNextLine_NestedAttribute()
    {
        await RunFormattingTestAsync(
            input: """
                <div>
                <button foo="bar"
                @onclick="() =>
                {
                StateHasChanged();}">
                </button>
                </div>
                """,
            htmlFormatted: """
                <div>
                    <button foo="bar"
                            @onclick="() =>
                {
                StateHasChanged();}">
                    </button>
                </div>
                """,
            expected: """
                <div>
                    <button foo="bar"
                            @onclick="() =>
                            {
                                StateHasChanged();
                            }">
                    </button>
                </div>
                """,
            csharpSyntaxFormattingOptions: GetNewLineBeforeBraceInLambdaExpressionOptions(newLineBeforeBraceInLambda: true),
            fileKind: RazorFileKind.Component);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12785")]
    public async Task FormatEventHandlerAttributes_BlockBodiedLambda_BraceOnNextLine_NestedAttribute_NoNewLineBeforeOpenBraceInLambdaExpressionBody()
    {
        await RunFormattingTestAsync(
            input: """
                <div>
                <button foo="bar"
                @onclick="() =>
                {
                StateHasChanged();}">
                </button>
                </div>
                """,
            htmlFormatted: """
                <div>
                    <button foo="bar"
                            @onclick="() =>
                {
                StateHasChanged();}">
                    </button>
                </div>
                """,
            expected: """
                <div>
                    <button foo="bar"
                            @onclick="() => {
                                StateHasChanged();
                            }">
                    </button>
                </div>
                """,
            csharpSyntaxFormattingOptions: GetNewLineBeforeBraceInLambdaExpressionOptions(newLineBeforeBraceInLambda: false),
            fileKind: RazorFileKind.Component);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12785")]
    public async Task FormatEventHandlerAttributes_BlockBodiedLambda_ContentAfterOpenBrace()
    {
        await RunFormattingTestAsync(
            input: """
                <button @onclick="() => { foo();
                bar(); }">
                </button>
                """,
            htmlFormatted: """
                <button @onclick="() => { foo();
                bar(); }">
                </button>
                """,
            expected: """
                <button @onclick="() =>
                        {
                            foo();
                            bar();
                        }">
                </button>
                """,
            csharpSyntaxFormattingOptions: GetNewLineBeforeBraceInLambdaExpressionOptions(newLineBeforeBraceInLambda: true),
            fileKind: RazorFileKind.Component);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12785")]
    public async Task FormatEventHandlerAttributes_BlockBodiedLambda_ContentAfterOpenBrace_NoNewLineBeforeOpenBraceInLambdaExpressionBody()
    {
        await RunFormattingTestAsync(
            input: """
                <button @onclick="() => { foo();
                bar(); }">
                </button>
                """,
            htmlFormatted: """
                <button @onclick="() => { foo();
                bar(); }">
                </button>
                """,
            expected: """
                <button @onclick="() => {
                            foo();
                            bar();
                        }">
                </button>
                """,
            csharpSyntaxFormattingOptions: GetNewLineBeforeBraceInLambdaExpressionOptions(newLineBeforeBraceInLambda: false),
            fileKind: RazorFileKind.Component);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12785")]
    public async Task FormatEventHandlerAttributes_BlockBodiedLambda_SingleLine()
    {
        await RunFormattingTestAsync(
            input: """
                <button @onclick="()=>{foo();}">
                </button>
                """,
            htmlFormatted: """
                <button @onclick="()=>{foo();}">
                </button>
                """,
            expected: """
                <button @onclick="() => { foo(); }">
                </button>
                """,
            csharpSyntaxFormattingOptions: GetNewLineBeforeBraceInLambdaExpressionOptions(newLineBeforeBraceInLambda: true),
            fileKind: RazorFileKind.Component);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12785")]
    public async Task FormatEventHandlerAttributes_BlockBodiedLambda_SingleLine_NoNewLineBeforeOpenBraceInLambdaExpressionBody()
    {
        await RunFormattingTestAsync(
            input: """
                <button @onclick="()=>{foo();}">
                </button>
                """,
            htmlFormatted: """
                <button @onclick="()=>{foo();}">
                </button>
                """,
            expected: """
                <button @onclick="() => { foo(); }">
                </button>
                """,
            csharpSyntaxFormattingOptions: GetNewLineBeforeBraceInLambdaExpressionOptions(newLineBeforeBraceInLambda: false),
            fileKind: RazorFileKind.Component);
    }

    [Fact]
    public async Task FormatEventCallbackAttributes()
    {
        await RunFormattingTestAsync(
            input: """
                @using Microsoft.AspNetCore.Components.Forms;

                <p>Current count: @currentCount</p>

                <InputText ValueChanged="IncrementCount">Increment</InputText>
                <InputText ValueChanged="@(e=>currentCount=4)">Update to 4</InputText>
                <InputText ValueChanged="e=>currentCount=5">Update to 5</InputText>

                @code {
                    private int currentCount=0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            htmlFormatted: """
                @using Microsoft.AspNetCore.Components.Forms;
                
                <p>Current count: @currentCount</p>
                
                <InputText ValueChanged="IncrementCount">Increment</InputText>
                <InputText ValueChanged="@(e=>currentCount=4)">Update to 4</InputText>
                <InputText ValueChanged="e=>currentCount=5">Update to 5</InputText>
                
                @code {
                    private int currentCount=0;
                
                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            expected: """
                @using Microsoft.AspNetCore.Components.Forms;

                <p>Current count: @currentCount</p>

                <InputText ValueChanged="IncrementCount">Increment</InputText>
                <InputText ValueChanged="@(e => currentCount = 4)">Update to 4</InputText>
                <InputText ValueChanged="e => currentCount = 5">Update to 5</InputText>

                @code {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            fileKind: RazorFileKind.Component);
    }

    [Fact]
    public async Task FormatBindAttributes()
    {
        await RunFormattingTestAsync(
            input: """
                @using Microsoft.AspNetCore.Components.Forms;

                <p>Current count: @currentCount</p>

                <InputText @bind-Value="currentCount" @bind-Value:after="IncrementCount">Increment</InputText>
                <InputText @bind-Value="currentCount" @bind-Value:after="e=>currentCount=5">Update to 5</InputText>

                @code {
                    private int currentCount=0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            htmlFormatted: """
                @using Microsoft.AspNetCore.Components.Forms;
                
                <p>Current count: @currentCount</p>
                
                <InputText @bind-Value="currentCount" @bind-Value:after="IncrementCount">Increment</InputText>
                <InputText @bind-Value="currentCount" @bind-Value:after="e=>currentCount=5">Update to 5</InputText>
                
                @code {
                    private int currentCount=0;
                
                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            expected: """
                @using Microsoft.AspNetCore.Components.Forms;

                <p>Current count: @currentCount</p>

                <InputText @bind-Value="currentCount" @bind-Value:after="IncrementCount">Increment</InputText>
                <InputText @bind-Value="currentCount" @bind-Value:after="e => currentCount = 5">Update to 5</InputText>

                @code {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            fileKind: RazorFileKind.Component);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/9337")]
    public async Task FormatMinimizedTagHelperAttributes()
    {
        await RunFormattingTestAsync(
            input: """
                @using Microsoft.AspNetCore.Components.Forms;

                @code {
                    private bool _id {get;set;}
                }

                <div>
                    @if (true)
                    {
                        <div>
                            <InputCheckbox CssClass="goo"
                               Value
                               accesskey="F" />
                        </div>
                    }
                </div>
                """,
            htmlFormatted: """
                @using Microsoft.AspNetCore.Components.Forms;
                
                @code {
                    private bool _id {get;set;}
                }
                
                <div>
                    @if (true)
                    {
                    <div>
                        <InputCheckbox CssClass="goo"
                                       Value
                                       accesskey="F" />
                    </div>
                    }
                </div>
                """,
            expected: """
                @using Microsoft.AspNetCore.Components.Forms;

                @code {
                    private bool _id { get; set; }
                }

                <div>
                    @if (true)
                    {
                        <div>
                            <InputCheckbox CssClass="goo"
                                           Value
                                           accesskey="F" />
                        </div>
                    }
                </div>
                """,
            fileKind: RazorFileKind.Component);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/6110")]
    public async Task FormatExplicitCSharpInsideHtml1()
    {
        await RunFormattingTestAsync(
            input: """
                @using System.Text;

                <div>
                    @(new C()
                            .M("Hello")
                        .M("World")
                        .M(source =>
                        {
                        if (source.Length > 0)
                        {
                        source.ToString();
                        }
                        }))

                    @(DateTime.Now)

                    @(DateTime
                .Now
                .ToString())

                                @(   Html.DisplayNameFor (@<text>
                        <p >
                        <h2 ></h2>
                        </p>
                        </text>)
                        .ToString())

                @{
                var x = @<p>Hi there!</p>;
                }
                @x()
                @(@x())
                </div>

                @functions {
                    class C
                    {
                        C M(string a) => this;
                        C M(Func<string, C> a) => this;
                    }
                }
                """,
            htmlFormatted: """
                @using System.Text;
                
                <div>
                    @(new C()
                    .M("Hello")
                    .M("World")
                    .M(source =>
                    {
                    if (source.Length > 0)
                    {
                    source.ToString();
                    }
                    }))
                
                    @(DateTime.Now)
                
                    @(DateTime
                    .Now
                    .ToString())
                
                    @(   Html.DisplayNameFor (@<text>
                        <p>
                            <h2></h2>
                        </p>
                    </text>)
                    .ToString())
                
                    @{
                    var x = @<p>Hi there!</p>;
                    }
                    @x()
                    @(@x())
                </div>
                
                @functions {
                    class C
                    {
                        C M(string a) => this;
                        C M(Func<string, C> a) => this;
                    }
                }
                """,
            expected: """
                @using System.Text;

                <div>
                    @(new C()
                            .M("Hello")
                        .M("World")
                        .M(source =>
                        {
                            if (source.Length > 0)
                            {
                                source.ToString();
                            }
                        }))

                    @(DateTime.Now)

                    @(DateTime
                .Now
                .ToString())

                    @(Html.DisplayNameFor(@<text>
                        <p>
                            <h2></h2>
                        </p>
                    </text>)
                .ToString())

                    @{
                        var x = @<p>Hi there!</p>;
                    }
                    @x()
                    @(@x())
                </div>

                @functions {
                    class C
                    {
                        C M(string a) => this;
                        C M(Func<string, C> a) => this;
                    }
                }
                """,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/6110")]
    public async Task FormatExplicitCSharpInsideHtml2()
    {
        await RunFormattingTestAsync(
            input: """
                <div>
                     @(   Html.DisplayNameFor (@<text>
                        <p >
                        <h2 ></h2>
                        </p>
                        </text>)
                        .ToString())

                     @(   Html.DisplayNameFor (@<div></div>,
                        1,   3,    4))

                     @(   Html.DisplayNameFor (@<div></div>,
                        1,   3, @<div></div>,
                        2, 4))

                     @(   Html.DisplayNameFor (
                        1,   3, @<div></div>,
                        2, 4))

                     @(   Html.DisplayNameFor (
                        1,   3,
                        2,  4))

                     @(   Html.DisplayNameFor (
                        2, 4,
                        1,   3, @<div></div>,
                        2, 4,
                        1,   3, @<div></div>,
                        4))
                </div>
                """,
            htmlFormatted: """
                <div>
                    @(   Html.DisplayNameFor (@<text>
                        <p>
                            <h2></h2>
                        </p>
                    </text>)
                    .ToString())
                
                    @(   Html.DisplayNameFor (@<div></div>,
                    1,   3,    4))
                
                    @(   Html.DisplayNameFor (@<div></div>,
                    1,   3, @<div></div>,
                    2, 4))
                
                    @(   Html.DisplayNameFor (
                    1,   3, @<div></div>,
                    2, 4))
                
                    @(   Html.DisplayNameFor (
                    1,   3,
                    2,  4))
                
                    @(   Html.DisplayNameFor (
                    2, 4,
                    1,   3, @<div></div>,
                    2, 4,
                    1,   3, @<div></div>,
                    4))
                </div>
                """,
            expected: """
                <div>
                    @(Html.DisplayNameFor(@<text>
                        <p>
                            <h2></h2>
                        </p>
                    </text>)
                       .ToString())

                    @(Html.DisplayNameFor(@<div></div>,
                       1, 3, 4))
                
                    @(Html.DisplayNameFor(@<div></div>,
                       1, 3, @<div></div>,
                       2, 4))

                    @(Html.DisplayNameFor(
                       1, 3, @<div></div>,
                       2, 4))

                    @(Html.DisplayNameFor(
                       1, 3,
                       2, 4))
                
                    @(Html.DisplayNameFor(
                       2, 4,
                       1, 3, @<div></div>,
                       2, 4,
                       1, 3, @<div></div>,
                       4))
                </div>
                """,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/6110")]
    public async Task FormatExplicitCSharpInsideHtml3()
    {
        await RunFormattingTestAsync(
            input: """
                @using System.Text;

                <div>
                    @(new C()
                        .M("Hello")
                        .M("World")
                        .M(source =>
                        {
                            if (source.Length > 0)
                            {
                                source.ToString();
                            }
                        }))

                    @(DateTime.Now)

                    @(DateTime
                        .Now
                        .ToString())
                </div>
                """,
            htmlFormatted: """
                @using System.Text;
                
                <div>
                    @(new C()
                    .M("Hello")
                    .M("World")
                    .M(source =>
                    {
                    if (source.Length > 0)
                    {
                    source.ToString();
                    }
                    }))
                
                    @(DateTime.Now)
                
                    @(DateTime
                    .Now
                    .ToString())
                </div>
                """,
            expected: """
                @using System.Text;

                <div>
                    @(new C()
                        .M("Hello")
                        .M("World")
                        .M(source =>
                        {
                            if (source.Length > 0)
                            {
                                source.ToString();
                            }
                        }))

                    @(DateTime.Now)

                    @(DateTime
                        .Now
                        .ToString())
                </div>
                """,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task RazorDiagnostics_SkipRangeFormatting()
    {
        await RunFormattingTestAsync(
            input: """
                @page "Goo"

                <div></div>

                [|<button|]
                @functions {
                 void M() { }
                }
                """,
            htmlFormatted: """
                @page "Goo"
                
                <div></div>
                
                <button @functions {
                        void M() { }
                        }
                """,
            expected: """
                @page "Goo"

                <div></div>

                <button
                @functions {
                 void M() { }
                }
                """,
            allowDiagnostics: true);
    }

    [Fact]
    public async Task RazorDiagnostics_DontSkipDocumentFormatting()
    {
        // Yes this format result looks wrong, but this is only done in direct response
        // to user action, and they can always undo it.
        await RunFormattingTestAsync(
            input: """
                <button
                @functions {
                 void M() { }
                }
                """,
            htmlFormatted: """
                <button @functions {
                        void M() { }
                        }
                """,
            expected: """
                <button @functions {
                    void M() { }
                    }
                """,
            allowDiagnostics: true);
    }

    [Fact]
    public async Task RazorDiagnostics_SkipRangeFormatting_WholeDocumentRange()
    {
        await RunFormattingTestAsync(
            input: """
                [|<button
                @functions {
                 void M() { }
                }|]
                """,
            htmlFormatted: """
                <button @functions {
                        void M() { }
                        }
                """,
            expected: """
                <button
                @functions {
                 void M() { }
                }
                """,
            allowDiagnostics: true);
    }

    [Fact]
    public async Task RazorDiagnostics_DontSkipWhenOutsideOfRange()
    {
        await RunFormattingTestAsync(
            input: """
                @page "Goo"

                [|      <div></div>|]

                <button
                @functions {
                 void M() { }
                }
                """,
            htmlFormatted: """
                @page "Goo"
                
                <div></div>
                
                <button @functions {
                        void M() { }
                        }
                """,
            expected: """
                @page "Goo"

                <div></div>

                <button
                @functions {
                 void M() { }
                }
                """,
            allowDiagnostics: true);
    }

    [Fact]
    public async Task FormatIndentedElementAttributes()
    {
        await RunFormattingTestAsync(
            input: """
                Welcome.

                <div class="goo"
                 align="center">
                </div>

                <PageTitle Title="How is Blazor working for you?"
                 Color="Red" />

                <PageTitle Title="How is Blazor working for you?"
                 Color="Red"></PageTitle>

                <PageTitle Title="How is Blazor working for you?"
                 Color="Red">
                 Hello
                 </PageTitle>

                @if (true)
                {
                <div class="goo"
                 align="center">
                </div>

                <PageTitle Title="How is Blazor working for you?"
                   Color="Red" />

                   <tag attr1="value1"
                   attr2="value2"
                   attr3="value3"
                   />

                 <tag attr1="value1"
                   attr2="value2"
                   attr3="value3"></tag>

                 <tag attr1="value1"
                   attr2="value2"
                   attr3="value3">
                Hello
                    </tag>

                   @if (true)
                   {
                   @if (true)
                   {
                   @if(true)
                   {
                   <table width="10"
                   height="10"
                   cols="3"
                   rows="3">
                   </table>
                   }
                   }
                   }
                }
                """,
            htmlFormatted: """
                Welcome.
                
                <div class="goo"
                     align="center">
                </div>
                
                <PageTitle Title="How is Blazor working for you?"
                           Color="Red" />
                
                <PageTitle Title="How is Blazor working for you?"
                           Color="Red"></PageTitle>
                
                <PageTitle Title="How is Blazor working for you?"
                           Color="Red">
                    Hello
                </PageTitle>
                
                @if (true)
                {
                <div class="goo"
                     align="center">
                </div>
                
                <PageTitle Title="How is Blazor working for you?"
                           Color="Red" />
                
                <tag attr1="value1"
                     attr2="value2"
                     attr3="value3" />
                
                <tag attr1="value1"
                     attr2="value2"
                     attr3="value3"></tag>
                
                <tag attr1="value1"
                     attr2="value2"
                     attr3="value3">
                    Hello
                </tag>
                
                   @if (true)
                   {
                   @if (true)
                   {
                   @if(true)
                   {
                <table width="10"
                       height="10"
                       cols="3"
                       rows="3">
                </table>
                   }
                   }
                   }
                }
                """,
            expected: """
                Welcome.

                <div class="goo"
                     align="center">
                </div>

                <PageTitle Title="How is Blazor working for you?"
                           Color="Red" />

                <PageTitle Title="How is Blazor working for you?"
                           Color="Red"></PageTitle>
                
                <PageTitle Title="How is Blazor working for you?"
                           Color="Red">
                    Hello
                </PageTitle>

                @if (true)
                {
                    <div class="goo"
                         align="center">
                    </div>

                    <PageTitle Title="How is Blazor working for you?"
                               Color="Red" />

                    <tag attr1="value1"
                         attr2="value2"
                         attr3="value3" />

                    <tag attr1="value1"
                         attr2="value2"
                         attr3="value3"></tag>
                
                    <tag attr1="value1"
                         attr2="value2"
                         attr3="value3">
                        Hello
                    </tag>

                    @if (true)
                    {
                        @if (true)
                        {
                            @if (true)
                            {
                                <table width="10"
                                       height="10"
                                       cols="3"
                                       rows="3">
                                </table>
                            }
                        }
                    }
                }
                """);
    }
    [Fact]
    public async Task FormatsCodeBlockDirective()
    {
        await RunFormattingTestAsync(
            input: """
                @code {
                 public class Foo{}
                        public interface Bar {
                }
                }
                """,
            htmlFormatted: """
                @code {
                 public class Foo{}
                        public interface Bar {
                }
                }
                """,
            expected: """
                @code {
                    public class Foo { }
                    public interface Bar
                    {
                    }
                }
                """);
    }

    [Fact]
    public async Task FormatCSharpInsideHtmlTag()
    {
        await RunFormattingTestAsync(
            input: """
                <html>
                <body>
                <div>
                @{
                <span>foo</span>
                <span>foo</span>
                }
                </div>
                </body>
                </html>
                """,
            htmlFormatted: """
                <html>
                <body>
                    <div>
                        @{
                        <span>foo</span>
                        <span>foo</span>
                        }
                    </div>
                </body>
                </html>
                """,
            expected: """
                <html>
                <body>
                    <div>
                        @{
                            <span>foo</span>
                            <span>foo</span>
                        }
                    </div>
                </body>
                </html>
                """);
    }

    [Fact]
    public async Task Format_DocumentWithDiagnostics()
    {
        // The malformed closing div in the foreach block causes confusing results in the formatter,
        // but the test validates that we don't crash at least.
        await RunFormattingTestAsync(
            input: """
                @page
                @model BlazorApp58.Pages.Index2Model
                @{
                }

                <section class="section">
                    <div class="container">
                        <h1 class="title">Managed pohotos</h1>
                        <p class="subtitle">@Model.ReferenceNumber</p>
                    </div>
                </section>
                <section class="section">
                    <div class="container">
                        @foreach       (var item in Model.Images)
                        {
                            <div><div>
                        }
                    </div>
                </section>
                """,
            htmlFormatted: """
                @page
                @model BlazorApp58.Pages.Index2Model
                @{
                }
                
                <section class="section">
                    <div class="container">
                        <h1 class="title">Managed pohotos</h1>
                        <p class="subtitle">@Model.ReferenceNumber</p>
                    </div>
                </section>
                <section class="section">
                    <div class="container">
                        @foreach       (var item in Model.Images)
                        {
                        <div>
                            <div>
                                }
                            </div>
                </section>
                """,
            expected: """
                @page
                @model BlazorApp58.Pages.Index2Model
                @{
                }

                <section class="section">
                    <div class="container">
                        <h1 class="title">Managed pohotos</h1>
                        <p class="subtitle">@Model.ReferenceNumber</p>
                    </div>
                </section>
                <section class="section">
                    <div class="container">
                        @foreach (var item in Model.Images)
                        {
                            <div>
                                <div>
                                    }
                                </div>
                            </section>
                """,
            fileKind: RazorFileKind.Legacy,
            allowDiagnostics: true);
    }

    [Fact]
    public async Task Formats_MultipleBlocksInADirective()
    {
        await RunFormattingTestAsync(
            input: """
                @{
                void Method(){
                var x = "foo";
                @(DateTime.Now)
                    <p></p>
                var y= "fooo";
                }
                }
                <div>
                        </div>
                """,
            htmlFormatted: """
                @{
                void Method(){
                var x = "foo";
                @(DateTime.Now)
                <p></p>
                var y= "fooo";
                }
                }
                <div>
                </div>
                """,
            expected: """
                @{
                    void Method()
                    {
                        var x = "foo";
                        @(DateTime.Now)
                        <p></p>
                        var y = "fooo";
                    }
                }
                <div>
                </div>
                """);
    }

    [Fact]
    public async Task Formats_NonCodeBlockDirectives()
    {
        await RunFormattingTestAsync(
            input: """
                @{
                var x = "foo";
                }
                <div>
                        </div>
                """,
            htmlFormatted: """
                @{
                var x = "foo";
                }
                <div>
                </div>
                """,
            expected: """
                @{
                    var x = "foo";
                }
                <div>
                </div>
                """);
    }

    [Fact]
    public async Task Formats_CodeBlockDirectiveWithMarkup_NonBraced()
    {
        await RunFormattingTestAsync(
            input: """
                @functions {
                 public class Foo{
                void Method() { var x = "t"; <div></div> var y = "t";}
                }
                }
                """,
            htmlFormatted: """
                @functions {
                 public class Foo{
                void Method() { var x = "t"; <div></div> var y = "t";}
                }
                }
                """,
            expected: """
                @functions {
                    public class Foo
                    {
                        void Method()
                        {
                            var x = "t";
                            <div></div>
                            var y = "t";
                        }
                    }
                }
                """);
    }

    [Fact]
    public async Task Formats_CodeBlockDirectiveWithMarkup()
    {
        await RunFormattingTestAsync(
            input: """
                @functions {
                 public class Foo{
                void Method() { <div></div> }
                }
                }
                """,
            htmlFormatted: """
                @functions {
                 public class Foo{
                void Method() { <div></div> }
                }
                }
                """,
            expected: """
                @functions {
                    public class Foo
                    {
                        void Method()
                        {
                            <div></div>
                        }
                    }
                }
                """);
    }

    [Fact]
    public async Task Formats_CodeBlockDirectiveWithImplicitExpressions()
    {
        await RunFormattingTestAsync(
            input: """
                @code {
                 public class Foo{
                void Method() { @DateTime.Now }
                    }
                }
                """,
            htmlFormatted: """
                @code {
                 public class Foo{
                void Method() { @DateTime.Now }
                    }
                }
                """,
            expected: """
                @code {
                    public class Foo
                    {
                        void Method()
                        {
                            @DateTime.Now
                        }
                    }
                }
                """);
    }

    [Fact]
    public async Task Formats_ImplicitExpressions()
    {
        await RunFormattingTestAsync(
            input: """
                <div>
                    It is @DateTime.Now.ToString(   "d MM yyy"   ). Or is it @DateTime.Now.ToString(   "d MM yyy"   ).

                    @DateTime.Now.ToString(   "d MM yyy"   ) it is.

                    @DateTime.Now.ToString(   "d MM yyy"   ). Is what it is today. Or is it @DateTime.Now.ToString(   "d MM yyy"   ).

                    @DateTime.Now.ToString(   "d MM yyy"   ) <span>Today!</span>
                </div>
                """,
            htmlFormatted: """
                <div>
                    It is @DateTime.Now.ToString(   "d MM yyy"   ). Or is it @DateTime.Now.ToString(   "d MM yyy"   ).
                
                    @DateTime.Now.ToString(   "d MM yyy"   ) it is.
                
                    @DateTime.Now.ToString(   "d MM yyy"   ). Is what it is today. Or is it @DateTime.Now.ToString(   "d MM yyy"   ).
                
                    @DateTime.Now.ToString(   "d MM yyy"   ) <span>Today!</span>
                </div>
                """,
            expected: """
                <div>
                    It is @DateTime.Now.ToString("d MM yyy"). Or is it @DateTime.Now.ToString("d MM yyy").
                
                    @DateTime.Now.ToString("d MM yyy") it is.
                
                    @DateTime.Now.ToString("d MM yyy"). Is what it is today. Or is it @DateTime.Now.ToString("d MM yyy").
                
                    @DateTime.Now.ToString("d MM yyy") <span>Today!</span>
                </div>
                """);
    }

    [Fact]
    public async Task Formats_ExplicitExpressions()
    {
        await RunFormattingTestAsync(
            input: """
                <div>
                    It is @(DateTime.    Now). Or is it @(DateTime.    Now).

                    @(DateTime.    Now) it is.

                    @(DateTime.    Now). Is what it is today. Or is it @(DateTime.    Now).

                    @(DateTime.    Now) <span>Today!</span>
                </div>
                """,
            htmlFormatted: """
                <div>
                    It is @(DateTime.    Now). Or is it @(DateTime.    Now).
                
                    @(DateTime.    Now) it is.
                
                    @(DateTime.    Now). Is what it is today. Or is it @(DateTime.    Now).
                
                    @(DateTime.    Now) <span>Today!</span>
                </div>
                """,
            expected: """
                <div>
                    It is @(DateTime.Now). Or is it @(DateTime.Now).
                
                    @(DateTime.Now) it is.
                
                    @(DateTime.Now). Is what it is today. Or is it @(DateTime.Now).
                
                    @(DateTime.Now) <span>Today!</span>
                </div>
                """);
    }

    [Fact]
    public async Task DoesNotFormat_CodeBlockDirectiveWithExplicitExpressions()
    {
        await RunFormattingTestAsync(
            input: """
                @functions {
                 public class Foo{
                void Method() { @(DateTime.Now) }
                    }
                }
                """,
            htmlFormatted: """
                @functions {
                 public class Foo{
                void Method() { @(DateTime.Now) }
                    }
                }
                """,
            expected: """
                @functions {
                    public class Foo
                    {
                        void Method()
                        {
                            @(DateTime.Now)
                        }
                    }
                }
                """,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task Format_SectionDirectiveBlock1()
    {
        await RunFormattingTestAsync(
            input: """
                @functions {
                 public class Foo{
                void Method() {  }
                    }
                }

                @section Scripts {
                <script></script>
                }
                """,
            htmlFormatted: """
                @functions {
                 public class Foo{
                void Method() {  }
                    }
                }
                
                @section Scripts {
                <script></script>
                }
                """,
            expected: """
                @functions {
                    public class Foo
                    {
                        void Method() { }
                    }
                }

                @section Scripts {
                    <script></script>
                }
                """,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task Format_SectionDirectiveBlock2()
    {
        await RunFormattingTestAsync(
            input: """
                @functions {
                 public class Foo{
                void Method() {  }
                    }
                }

                @section Scripts {
                <script>
                    function f() {
                    }
                </script>
                }
                """,
            htmlFormatted: """
                @functions {
                 public class Foo{
                void Method() {  }
                    }
                }
                
                @section Scripts {
                <script>
                    function f() {
                    }
                </script>
                }
                """,
            expected: """
                @functions {
                    public class Foo
                    {
                        void Method() { }
                    }
                }

                @section Scripts {
                    <script>
                        function f() {
                        }
                    </script>
                }
                """,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task Format_SectionDirectiveBlock3()
    {
        await RunFormattingTestAsync(
            input: """
                @functions {
                 public class Foo{
                void Method() {  }
                    }
                }

                @section Scripts {
                <p>this is a para</p>
                @if(true)
                {
                <p>and so is this</p>
                }
                }
                """,
            htmlFormatted: """
                @functions {
                 public class Foo{
                void Method() {  }
                    }
                }
                
                @section Scripts {
                <p>this is a para</p>
                @if(true)
                {
                <p>and so is this</p>
                }
                }
                """,
            expected: """
                @functions {
                    public class Foo
                    {
                        void Method() { }
                    }
                }

                @section Scripts {
                    <p>this is a para</p>
                    @if (true)
                    {
                        <p>and so is this</p>
                    }
                }
                """,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/6401")]
    public async Task Format_SectionDirectiveBlock4()
    {
        await RunFormattingTestAsync(
            input: """
                @functions {
                 public class Foo{
                void Method() {  }
                    }
                }

                @section Scripts {
                <script></script>
                }

                @if (true)
                {
                    <p></p>
                }
                """,
            htmlFormatted: """
                @functions {
                 public class Foo{
                void Method() {  }
                    }
                }
                
                @section Scripts {
                <script></script>
                }
                
                @if (true)
                {
                <p></p>
                }
                """,
            expected: """
                @functions {
                    public class Foo
                    {
                        void Method() { }
                    }
                }

                @section Scripts {
                    <script></script>
                }

                @if (true)
                {
                    <p></p>
                }
                """,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task Format_SectionDirectiveBlock5()
    {
        await RunFormattingTestAsync(
            input: """
                @functions {
                 public class Foo{
                void Method() {  }
                    }
                }

                @section Foo {
                    @{ var test = 1; }
                }

                <p></p>

                @section Scripts {
                <script></script>
                }

                <p></p>
                """,
            htmlFormatted: """
                @functions {
                 public class Foo{
                void Method() {  }
                    }
                }
                
                @section Foo {
                    @{ var test = 1; }
                }
                
                <p></p>
                
                @section Scripts {
                <script></script>
                }
                
                <p></p>
                """,
            expected: """
                @functions {
                    public class Foo
                    {
                        void Method() { }
                    }
                }

                @section Foo {
                    @{
                        var test = 1;
                    }
                }

                <p></p>

                @section Scripts {
                    <script></script>
                }

                <p></p>
                """,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task Format_SectionDirectiveBlock6()
    {
        await RunFormattingTestAsync(
            input: """
                @functions {
                 public class Foo{
                void Method() {  }
                    }
                }

                @section Scripts {
                <meta property="a" content="b">
                <meta property="a" content="b"/>
                <meta property="a" content="b">

                @if(true)
                {
                <p>this is a paragraph</p>
                }
                }
                """,
            htmlFormatted: """
                @functions {
                 public class Foo{
                void Method() {  }
                    }
                }
                
                @section Scripts {
                <meta property="a" content="b">
                <meta property="a" content="b" />
                <meta property="a" content="b">
                
                @if(true)
                {
                <p>this is a paragraph</p>
                }
                }
                """,
            expected: """
                @functions {
                    public class Foo
                    {
                        void Method() { }
                    }
                }

                @section Scripts {
                    <meta property="a" content="b">
                    <meta property="a" content="b" />
                    <meta property="a" content="b">

                    @if (true)
                    {
                        <p>this is a paragraph</p>
                    }
                }
                """,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task Format_SectionDirectiveBlock7()
    {
        await RunFormattingTestAsync(
            input: """
                @functions {
                 public class Foo{
                void Method() {  }
                    }
                }

                @section Scripts
                {
                <meta property="a" content="b">
                <meta property="a" content="b"/>
                <meta property="a" content="b">

                @if(true)
                {
                <p>this is a paragraph</p>
                }
                }
                """,
            htmlFormatted: """
                @functions {
                 public class Foo{
                void Method() {  }
                    }
                }
                
                @section Scripts
                {
                <meta property="a" content="b">
                <meta property="a" content="b" />
                <meta property="a" content="b">
                
                @if(true)
                {
                <p>this is a paragraph</p>
                }
                }
                """,
            expected: """
                @functions {
                    public class Foo
                    {
                        void Method() { }
                    }
                }

                @section Scripts
                {
                    <meta property="a" content="b">
                    <meta property="a" content="b" />
                    <meta property="a" content="b">

                    @if (true)
                    {
                        <p>this is a paragraph</p>
                    }
                }
                """,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task Format_SectionDirectiveBlock8()
    {
        await RunFormattingTestAsync(
            input: """
                @functions {
                 public class Foo{
                void Method() {  }
                    }
                }

                @section Scripts {
                <p>this is a para</p>
                @if(true)
                {
                <p>and so is this</p>
                }
                <p>and finally this</p>
                }
                """,
            htmlFormatted: """
                @functions {
                 public class Foo{
                void Method() {  }
                    }
                }
                
                @section Scripts {
                <p>this is a para</p>
                @if(true)
                {
                <p>and so is this</p>
                }
                <p>and finally this</p>
                }
                """,
            expected: """
                @functions {
                    public class Foo
                    {
                        void Method() { }
                    }
                }

                @section Scripts {
                    <p>this is a para</p>
                    @if (true)
                    {
                        <p>and so is this</p>
                    }
                    <p>and finally this</p>
                }
                """,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task Format_SectionDirectiveBlock9()
    {
        await RunFormattingTestAsync(
            input: """
                @functions {
                 public class Foo{
                void Method() {  }
                    }
                }

                @section Scripts {
                <p>this is a para</p>
                @if(true)
                {
                <p>and so is this</p>
                }
                <p>and finally this</p>
                }

                <p>I lied when I said finally</p>

                @functions {
                 public class Foo2{
                void Method() {  }
                    }
                }
                """,
            htmlFormatted: """
                @functions {
                 public class Foo{
                void Method() {  }
                    }
                }
                
                @section Scripts {
                <p>this is a para</p>
                @if(true)
                {
                <p>and so is this</p>
                }
                <p>and finally this</p>
                }
                
                <p>I lied when I said finally</p>
                
                @functions {
                 public class Foo2{
                void Method() {  }
                    }
                }
                """,
            expected: """
                @functions {
                    public class Foo
                    {
                        void Method() { }
                    }
                }

                @section Scripts {
                    <p>this is a para</p>
                    @if (true)
                    {
                        <p>and so is this</p>
                    }
                    <p>and finally this</p>
                }

                <p>I lied when I said finally</p>

                @functions {
                    public class Foo2
                    {
                        void Method() { }
                    }
                }
                """,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task Formats_CodeBlockDirectiveWithRazorComments()
    {
        await RunFormattingTestAsync(
            input: """
                @functions {
                 public class Foo{
                @* This is a Razor Comment *@
                void Method() {  }
                }
                }
                """,
            htmlFormatted: """
                @functions {
                 public class Foo{
                @* This is a Razor Comment *@
                void Method() {  }
                }
                }
                """,
            expected: """
                @functions {
                    public class Foo
                    {
                        @* This is a Razor Comment *@
                        void Method() { }
                    }
                }
                """);
    }

    [Fact]
    public async Task Formats_CodeBlockDirectiveWithRazorStatements()
    {
        await RunFormattingTestAsync(
            input: """
                @functions {
                 public class Foo{
                @* This is a Razor Comment *@
                    }
                }
                """,
            htmlFormatted: """
                @functions {
                 public class Foo{
                @* This is a Razor Comment *@
                    }
                }
                """,
            expected: """
                @functions {
                    public class Foo
                    {
                        @* This is a Razor Comment *@
                    }
                }
                """);
    }

    [Fact]
    public async Task Formats_ExplicitStatements1()
    {
        await RunFormattingTestAsync(
            input: """
                @{
                 <text>Hello</text>
                }

                @{ <text>Hello</text> }

                <div></div>

                @{ }

                <div></div>
                """,
            htmlFormatted: """
                @{
                <text>Hello</text>
                }
                
                @{ <text>Hello</text> }
                
                <div></div>
                
                @{ }
                
                <div></div>
                """,
            expected: """
                @{
                    <text>Hello</text>
                }
                
                @{
                    <text>Hello</text>
                }
                
                <div></div>
                
                @{ }
                
                <div></div>
                """);
    }

    [Fact]
    public async Task Formats_ExplicitStatements2()
    {
        await RunFormattingTestAsync(
            input: """
                <div>
                @{
                 <text>Hello</text>
                }

                @{ <text>Hello</text> }

                <div></div>

                @{ }

                <div></div>
                </div>
                """,
            htmlFormatted: """
                <div>
                    @{
                    <text>Hello</text>
                    }
                
                    @{ <text>Hello</text> }
                
                    <div></div>
                
                    @{ }
                
                    <div></div>
                </div>
                """,
            expected: """
                <div>
                    @{
                        <text>Hello</text>
                    }
                
                    @{
                        <text>Hello</text>
                    }
                
                    <div></div>
                
                    @{ }
                
                    <div></div>
                </div>
                """);
    }

    [Fact]
    public async Task DoesNotFormat_CodeBlockDirective_NotInSelectedRange()
    {
        await RunFormattingTestAsync(
            input: """
                [|<div>Foo</div>|]
                @functions {
                 public class Foo{}
                        public interface Bar {
                }
                }
                """,
            htmlFormatted: """
                <div>Foo</div>
                @functions {
                 public class Foo{}
                        public interface Bar {
                }
                }
                """,
            expected: """
                <div>Foo</div>
                @functions {
                 public class Foo{}
                        public interface Bar {
                }
                }
                """);
    }

    [Fact]
    public async Task OnlyFormatsWithinRange()
    {
        await RunFormattingTestAsync(
            input: """
                @functions {
                 public class Foo{}
                        [|public interface Bar {
                }|]
                }
                """,
            htmlFormatted: """
                @functions {
                 public class Foo{}
                        public interface Bar {
                }
                }
                """,
            expected: """
                @functions {
                 public class Foo{}
                    public interface Bar
                    {
                    }
                }
                """);
    }

    [Fact]
    public async Task MultipleCodeBlockDirectives()
    {
        await RunFormattingTestAsync(
            input: """
                @functions {
                 public class Foo{}
                        public interface Bar {
                }
                }
                Hello World
                @functions {
                      public class Baz    {
                          void Method ( )
                          { }
                          }
                }
                """,
            htmlFormatted: """
                @functions {
                 public class Foo{}
                        public interface Bar {
                }
                }
                Hello World
                @functions {
                      public class Baz    {
                          void Method ( )
                          { }
                          }
                }
                """,
            expected: """
                @functions {
                    public class Foo { }
                    public interface Bar
                    {
                    }
                }
                Hello World
                @functions {
                    public class Baz
                    {
                        void Method()
                        { }
                    }
                }
                """,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task MultipleCodeBlockDirectives2()
    {
        await RunFormattingTestAsync(
            input: """
                Hello World
                @code {
                public class HelloWorld
                {
                }
                }

                @functions{

                    public class Bar {}
                }
                """,
            htmlFormatted: """
                Hello World
                @code {
                public class HelloWorld
                {
                }
                }
                
                @functions{
                
                    public class Bar {}
                }
                """,
            expected: """
                Hello World
                @code {
                    public class HelloWorld
                    {
                    }
                }

                @functions {

                    public class Bar { }
                }
                """);
    }

    [Fact]
    public async Task CodeOnTheSameLineAsCodeBlockDirectiveStart()
    {
        await RunFormattingTestAsync(
            input: """
                @functions {public class Foo{
                }
                }
                """,
            htmlFormatted: """
                @functions {public class Foo{
                }
                }
                """,
            expected: """
                @functions {
                    public class Foo
                    {
                    }
                }
                """);
    }

    [Fact]
    public async Task CodeOnTheSameLineAsCodeBlockDirectiveEnd()
    {
        await RunFormattingTestAsync(
            input: """
                @functions {
                public class Foo{
                }}
                """,
            htmlFormatted: """
                @functions {
                public class Foo{
                }}
                """,
            expected: """
                @functions {
                    public class Foo
                    {
                    }
                }
                """);
    }

    [Fact]
    public async Task SingleLineCodeBlockDirective()
    {
        await RunFormattingTestAsync(
            input: """
                @functions {public class Foo{}
                }
                """,
            htmlFormatted: """
                @functions {public class Foo{}
                }
                """,
            expected: """
                @functions {
                    public class Foo { }
                }
                """);
    }

    [Fact]
    public async Task IndentsCodeBlockDirectiveStart()
    {
        await RunFormattingTestAsync(
            input: """
                Hello World
                     @functions {public class Foo{}
                }
                """,
            htmlFormatted: """
                Hello World
                     @functions {public class Foo{}
                }
                """,
            expected: """
                Hello World
                @functions {
                    public class Foo { }
                }
                """);
    }

    [Fact]
    public async Task IndentsCodeBlockDirectiveEnd()
    {
        await RunFormattingTestAsync(
            input: """
                @functions {
                public class Foo{}
                     }
                """,
            htmlFormatted: """
                @functions {
                public class Foo{}
                     }
                """,
            expected: """
                @functions {
                    public class Foo { }
                }
                """);
    }

    [Fact]
    public async Task ComplexCodeBlockDirective()
    {
        await RunFormattingTestAsync(
            input: """
                @using System.Buffers
                @functions{
                     public class Foo
                            {
                                public Foo()
                                {
                                    var arr = new string[ ] { "One", "two","three" };
                                    var str = @"
                This should
                not
                be indented.
                ";
                                }
                public int MyProperty { get
                {
                return 0 ;
                } set {} }

                void Method(){

                }
                                    }
                }
                """,
            htmlFormatted: """
                @using System.Buffers
                @functions{
                     public class Foo
                            {
                                public Foo()
                                {
                                    var arr = new string[ ] { "One", "two","three" };
                                    var str = @"
                This should
                not
                be indented.
                ";
                                }
                public int MyProperty { get
                {
                return 0 ;
                } set {} }
                
                void Method(){
                
                }
                                    }
                }
                """,
            expected: """
                @using System.Buffers
                @functions {
                    public class Foo
                    {
                        public Foo()
                        {
                            var arr = new string[] { "One", "two", "three" };
                            var str = @"
                This should
                not
                be indented.
                ";
                        }
                        public int MyProperty
                        {
                            get
                            {
                                return 0;
                            }
                            set { }
                        }

                        void Method()
                        {

                        }
                    }
                }
                """);
    }

    [Fact]
    public async Task Strings()
    {
        await RunFormattingTestAsync(
            input: """
                @functions{
                private string str1 = "hello world";
                private string str2 = $"hello world";
                private string str3 = @"hello world";
                private string str4 = $@"hello world";
                private string str5 = @"
                    One
                        Two
                            Three
                ";
                private string str6 = $@"
                    One
                        Two
                            Three
                ";
                // This looks wrong, but matches what the C# formatter does. Try it and see!
                private string str7 = "One" +
                    "Two" +
                        "Three" +
                "";
                }
                """,
            htmlFormatted: """
                @functions{
                private string str1 = "hello world";
                private string str2 = $"hello world";
                private string str3 = @"hello world";
                private string str4 = $@"hello world";
                private string str5 = @"
                    One
                        Two
                            Three
                ";
                private string str6 = $@"
                    One
                        Two
                            Three
                ";
                // This looks wrong, but matches what the C# formatter does. Try it and see!
                private string str7 = "One" +
                    "Two" +
                        "Three" +
                "";
                }
                """,
            expected: """
                @functions {
                    private string str1 = "hello world";
                    private string str2 = $"hello world";
                    private string str3 = @"hello world";
                    private string str4 = $@"hello world";
                    private string str5 = @"
                    One
                        Two
                            Three
                ";
                    private string str6 = $@"
                    One
                        Two
                            Three
                ";
                    // This looks wrong, but matches what the C# formatter does. Try it and see!
                    private string str7 = "One" +
                        "Two" +
                            "Three" +
                    "";
                }
                """);
    }

    [Fact]
    public async Task CodeBlockDirective_UseTabs()
    {
        await RunFormattingTestAsync(
            input: """
                @code {
                 public class Foo{}
                        void Method(  ) {
                }
                }
                """,
            htmlFormatted: """
                @code {
                 public class Foo{}
                        void Method(  ) {
                }
                }
                """,
            expected: """
                @code {
                	public class Foo { }
                	void Method()
                	{
                	}
                }
                """,
            insertSpaces: false);

    }
    [Fact]
    public async Task CodeBlockDirective_UseTabsWithTabSize8_HTML()
    {
        await RunFormattingTestAsync(
            input: """
                @code {
                 public class Foo{}
                        void Method(  ) {<div></div>
                }
                }
                """,
            htmlFormatted: """
                @code {
                 public class Foo{}
                        void Method(  ) {<div></div>
                }
                }
                """,
            expected: """
                @code {
                	public class Foo { }
                	void Method()
                	{
                		<div></div>
                	}
                }
                """,
            tabSize: 8,
            insertSpaces: false);
    }

    [Fact]
    public async Task CodeBlockDirective_UseTabsWithTabSize8()
    {
        await RunFormattingTestAsync(
            input: """
                @code {
                 public class Foo{}
                        void Method(  ) {
                }
                }
                """,
            htmlFormatted: """
                @code {
                 public class Foo{}
                        void Method(  ) {
                }
                }
                """,
            expected: """
                @code {
                	public class Foo { }
                	void Method()
                	{
                	}
                }
                """,
            tabSize: 8,
            insertSpaces: false);
    }

    [Fact]
    public async Task CodeBlockDirective_WithTabSize3()
    {
        await RunFormattingTestAsync(
            input: """
                @code {
                 public class Foo{}
                        void Method(  ) {
                }
                }
                """,
            htmlFormatted: """
                @code {
                 public class Foo{}
                        void Method(  ) {
                }
                }
                """,
            expected: """
                @code {
                   public class Foo { }
                   void Method()
                   {
                   }
                }
                """,
            tabSize: 3);
    }

    [Fact]
    public async Task CodeBlockDirective_WithTabSize8()
    {
        await RunFormattingTestAsync(
            input: """
                @code {
                 public class Foo{}
                        void Method(  ) {
                }
                }
                """,
            htmlFormatted: """
                @code {
                 public class Foo{}
                        void Method(  ) {
                }
                }
                """,
            expected: """
                @code {
                        public class Foo { }
                        void Method()
                        {
                        }
                }
                """,
            tabSize: 8);
    }

    [Fact]
    public async Task CodeBlockDirective_WithTabSize12()
    {
        await RunFormattingTestAsync(
            input: """
                @code {
                 public class Foo{}
                        void Method(  ) {
                }
                }
                """,
            htmlFormatted: """
                @code {
                 public class Foo{}
                        void Method(  ) {
                }
                }
                """,
            expected: """
                @code {
                            public class Foo { }
                            void Method()
                            {
                            }
                }
                """,
            tabSize: 12);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/27102")]
    public async Task CodeBlock_SemiColon_SingleLine()
    {
        await RunFormattingTestAsync(
            input: """
                <div></div>
                @{ Debugger.Launch()$$;}
                <div></div>
                """,
            htmlFormatted: """
                <div></div>
                @{ Debugger.Launch();}
                <div></div>
                """,
            expected: """
                <div></div>
                @{
                    Debugger.Launch();
                }
                <div></div>
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/29837")]
    public async Task CodeBlock_NestedComponents()
    {
        await RunFormattingTestAsync(
            input: """
                @code {
                    private WeatherForecast[] forecasts;

                    protected override async Task OnInitializedAsync()
                    {
                        <PageTitle>
                            @{
                                    var t = DateTime.Now;
                                    t.ToString();
                                }
                            </PageTitle>
                        forecasts = await ForecastService.GetForecastAsync(DateTime.Now);
                    }
                }
                """,
            htmlFormatted: """
                @code {
                    private WeatherForecast[] forecasts;
                
                    protected override async Task OnInitializedAsync()
                    {
                <PageTitle>
                    @{
                    var t = DateTime.Now;
                    t.ToString();
                    }
                </PageTitle>
                        forecasts = await ForecastService.GetForecastAsync(DateTime.Now);
                    }
                }
                """,
            expected: """
                @code {
                    private WeatherForecast[] forecasts;

                    protected override async Task OnInitializedAsync()
                    {
                        <PageTitle>
                            @{
                                var t = DateTime.Now;
                                t.ToString();
                            }
                        </PageTitle>
                        forecasts = await ForecastService.GetForecastAsync(DateTime.Now);
                    }
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/34320")]
    public async Task CodeBlock_ObjectCollectionArrayInitializers()
    {
        // The C# Formatter doesn't touch these types of initializers, so nor do we. This test
        // just verifies we don't regress things and start moving code around.
        await RunFormattingTestAsync(
            input: """
                @code {
                    public List<object> AList = new List<object>()
                    {
                        new
                        {
                            Name = "One",
                            Goo = new
                            {
                                First = 1,
                                Second = 2
                            },
                            Bar = new string[] {
                                "Hello",
                                "There"
                            },
                            Baz = new string[]
                            {
                                "Hello",
                                "There"
                            }
                        }
                    };
                }
                """,
            htmlFormatted: """
                @code {
                    public List<object> AList = new List<object>()
                    {
                        new
                        {
                            Name = "One",
                            Goo = new
                            {
                                First = 1,
                                Second = 2
                            },
                            Bar = new string[] {
                                "Hello",
                                "There"
                            },
                            Baz = new string[]
                            {
                                "Hello",
                                "There"
                            }
                        }
                    };
                }
                """,
            expected: """
                @code {
                    public List<object> AList = new List<object>()
                    {
                        new
                        {
                            Name = "One",
                            Goo = new
                            {
                                First = 1,
                                Second = 2
                            },
                            Bar = new string[] {
                                "Hello",
                                "There"
                            },
                            Baz = new string[]
                            {
                                "Hello",
                                "There"
                            }
                        }
                    };
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/6548")]
    public async Task CodeBlock_ImplicitObjectArrayInitializers()
    {
        // The C# Formatter doesn't touch these types of initializers, so nor do we. This test
        // just verifies we don't regress things and start moving code around.
        await RunFormattingTestAsync(
            input: """
                @code {
                    private object _x = new()
                        {
                            Name = "One",
                            Goo = new
                            {
                                First = 1,
                                Second = 2
                            },
                            Bar = new string[]
                            {
                                "Hello",
                                "There"
                            },
                        };
                }
                """,
            htmlFormatted: """
                @code {
                    private object _x = new()
                        {
                            Name = "One",
                            Goo = new
                            {
                                First = 1,
                                Second = 2
                            },
                            Bar = new string[]
                            {
                                "Hello",
                                "There"
                            },
                        };
                }
                """,
            expected: """
                @code {
                    private object _x = new()
                    {
                        Name = "One",
                        Goo = new
                        {
                            First = 1,
                            Second = 2
                        },
                        Bar = new string[]
                            {
                                "Hello",
                                "There"
                            },
                    };
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/7058")]
    public async Task CodeBlock_ImplicitArrayInitializers()
    {
        await RunFormattingTestAsync(
            input: """
                @code {
                    private void M()
                    {
                        var entries = new[]
                        {
                            "a",
                            "b",
                            "c"
                        };
                    }
                }
                """,
            htmlFormatted: """
                @code {
                    private void M()
                    {
                        var entries = new[]
                        {
                            "a",
                            "b",
                            "c"
                        };
                    }
                }
                """,
            expected: """
                @code {
                    private void M()
                    {
                        var entries = new[]
                        {
                            "a",
                            "b",
                            "c"
                        };
                    }
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/6092")]
    public async Task CodeBlock_ArrayInitializers()
    {
        // The C# Formatter doesn't touch these types of initializers, so nor do we. This test
        // just verifies we don't regress things and start moving code around.
        await RunFormattingTestAsync(
            input: """
                @code {
                    private void M()
                    {
                        var entries = new string[]
                        {
                            "a",
                            "b",
                            "c"
                        };
                    }
                }
                """,
            htmlFormatted: """
                @code {
                    private void M()
                    {
                        var entries = new string[]
                        {
                            "a",
                            "b",
                            "c"
                        };
                    }
                }
                """,
            expected: """
                @code {
                    private void M()
                    {
                        var entries = new string[]
                        {
                            "a",
                            "b",
                            "c"
                        };
                    }
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/6548")]
    public async Task CodeBlock_ArrayInitializers2()
    {
        // The C# Formatter doesn't touch these types of initializers, so nor do we. This test
        // just verifies we don't regress things and start moving code around.
        await RunFormattingTestAsync(
            input: """
                <p></p>

                @code {
                    private void M()
                    {
                        var entries = new string[]
                        {
                            "a",
                            "b",
                            "c"
                        };

                        object gridOptions = new()
                        {
                            Columns = new GridColumn<WorkOrderModel>[]
                            {
                                new TextColumn<WorkOrderModel>(e => e.Name) { Label = "Work Order #" },
                                new TextColumn<WorkOrderModel>(e => e.PartNumber) { Label = "Part #" },
                                new TextColumn<WorkOrderModel>(e => e.Lot) { Label = "Lot #" },
                                        new DateTimeColumn<WorkOrderModel>(e => e.TargetStartOn) { Label = "Target Start" },
                            },
                            Data = Model.WorkOrders,
                            Title = "Work Orders"
                        };
                    }
                }
                """,
            htmlFormatted: """
                <p></p>
                
                @code {
                    private void M()
                    {
                        var entries = new string[]
                        {
                            "a",
                            "b",
                            "c"
                        };
                
                        object gridOptions = new()
                        {
                            Columns = new GridColumn<WorkOrderModel>[]
                            {
                                new TextColumn<WorkOrderModel>(e => e.Name) { Label = "Work Order #" },
                                new TextColumn<WorkOrderModel>(e => e.PartNumber) { Label = "Part #" },
                                new TextColumn<WorkOrderModel>(e => e.Lot) { Label = "Lot #" },
                                        new DateTimeColumn<WorkOrderModel>(e => e.TargetStartOn) { Label = "Target Start" },
                            },
                            Data = Model.WorkOrders,
                            Title = "Work Orders"
                        };
                    }
                }
                """,
            expected: """
                <p></p>
                
                @code {
                    private void M()
                    {
                        var entries = new string[]
                        {
                            "a",
                            "b",
                            "c"
                        };
                
                        object gridOptions = new()
                        {
                            Columns = new GridColumn<WorkOrderModel>[]
                            {
                                new TextColumn<WorkOrderModel>(e => e.Name) { Label = "Work Order #" },
                                new TextColumn<WorkOrderModel>(e => e.PartNumber) { Label = "Part #" },
                                new TextColumn<WorkOrderModel>(e => e.Lot) { Label = "Lot #" },
                                        new DateTimeColumn<WorkOrderModel>(e => e.TargetStartOn) { Label = "Target Start" },
                            },
                            Data = Model.WorkOrders,
                            Title = "Work Orders"
                        };
                    }
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/9826")]
    public async Task CodeBlock_ArrayInitializers_InsideHtmlElement()
    {
        // The C# Formatter doesn't touch these types of initializers, so nor do we. This test
        // just verifies we don't regress things and start moving code around.
        await RunFormattingTestAsync(
            input: """
                <table>
                    @{
                        var minimum = "";
                        var maximum = "";
                        var dateOptions = new[,]
                        {
                            {$"Set to minimum ({minimum})", minimum},
                            {$"Set to maximum ({maximum})", maximum},
                        };
                    }
                </table>
                """,
            htmlFormatted: """
                <table>
                    @{
                    var minimum = "";
                    var maximum = "";
                    var dateOptions = new[,]
                    {
                    {$"Set to minimum ({minimum})", minimum},
                    {$"Set to maximum ({maximum})", maximum},
                    };
                    }
                </table>
                """,
            expected: """
                <table>
                    @{
                        var minimum = "";
                        var maximum = "";
                        var dateOptions = new[,]
                        {
                            {$"Set to minimum ({minimum})", minimum},
                            {$"Set to maximum ({maximum})", maximum},
                        };
                    }
                </table>
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/6092")]
    public async Task CodeBlock_CollectionArrayInitializers()
    {
        // The C# Formatter doesn't touch these types of initializers, so nor do we. This test
        // just verifies we don't regress things and start moving code around.
        await RunFormattingTestAsync(
            input: """
                @code {
                    private void M()
                    {
                        var entries = new List<string[]>()
                        {
                            new string[]
                            {
                                "Hello",
                                "There"
                            },
                            new string[] {
                                "Hello",
                                "There"
                            },
                            new string[]
                            {
                                "Hello",
                                "There"
                            }
                        };
                    }
                }
                """,
            htmlFormatted: """
                @code {
                    private void M()
                    {
                        var entries = new List<string[]>()
                        {
                            new string[]
                            {
                                "Hello",
                                "There"
                            },
                            new string[] {
                                "Hello",
                                "There"
                            },
                            new string[]
                            {
                                "Hello",
                                "There"
                            }
                        };
                    }
                }
                """,
            expected: """
                @code {
                    private void M()
                    {
                        var entries = new List<string[]>()
                        {
                            new string[]
                            {
                                "Hello",
                                "There"
                            },
                            new string[] {
                                "Hello",
                                "There"
                            },
                            new string[]
                            {
                                "Hello",
                                "There"
                            }
                        };
                    }
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/6092")]
    public async Task CodeBlock_ObjectInitializers()
    {
        // The C# Formatter doesn't touch these types of initializers, so nor do we. This test
        // just verifies we don't regress things and start moving code around.
        await RunFormattingTestAsync(
            input: """
                @code {
                    private void M()
                    {
                        var entries = new
                        {
                            First = 1,
                            Second = 2
                        };
                    }
                }
                """,
            htmlFormatted: """
                @code {
                    private void M()
                    {
                        var entries = new
                        {
                            First = 1,
                            Second = 2
                        };
                    }
                }
                """,
            expected: """
                @code {
                    private void M()
                    {
                        var entries = new
                        {
                            First = 1,
                            Second = 2
                        };
                    }
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/6092")]
    public async Task CodeBlock_ImplicitObjectInitializers()
    {
        // The C# Formatter doesn't touch these types of initializers, so nor do we. This test
        // just verifies we don't regress things and start moving code around.
        await RunFormattingTestAsync(
            input: """
                @code {
                    private void M()
                    {
                        object entries = new()
                        {
                            First = 1,
                            Second = 2
                        };
                    }
                }
                """,
            htmlFormatted: """
                @code {
                    private void M()
                    {
                        object entries = new()
                        {
                            First = 1,
                            Second = 2
                        };
                    }
                }
                """,
            expected: """
                @code {
                    private void M()
                    {
                        object entries = new()
                        {
                            First = 1,
                            Second = 2
                        };
                    }
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/6092")]
    public async Task CodeBlock_CollectionInitializers1()
    {
        // The C# Formatter doesn't touch these types of initializers, so nor do we. This test
        // just verifies we don't regress things and start moving code around.
        await RunFormattingTestAsync(
            input: """
                @code {
                    private void M()
                    {
                        var entries = new List<string>()
                        {
                            "a",
                            "b",
                            "c"
                        };
                    }
                }
                """,
            htmlFormatted: """
                @code {
                    private void M()
                    {
                        var entries = new List<string>()
                        {
                            "a",
                            "b",
                            "c"
                        };
                    }
                }
                """,
            expected: """
                @code {
                    private void M()
                    {
                        var entries = new List<string>()
                        {
                            "a",
                            "b",
                            "c"
                        };
                    }
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/6092")]
    public async Task CodeBlock_CollectionInitializers2()
    {
        // The C# Formatter doesn't touch these types of initializers, so nor do we. This test
        // just verifies we don't regress things and start moving code around.
        await RunFormattingTestAsync(
            input: """
                @code
                {
                    private void M()
                    {
                        var entries = new List<string>()
                        {
                            "a",
                            "b",
                            "c"
                        };
                    }
                }
                """,
            htmlFormatted: """
                @code
                {
                    private void M()
                    {
                        var entries = new List<string>()
                        {
                            "a",
                            "b",
                            "c"
                        };
                    }
                }
                """,
            expected: """
                @code
                {
                    private void M()
                    {
                        var entries = new List<string>()
                        {
                            "a",
                            "b",
                            "c"
                        };
                    }
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/11325")]
    public async Task CodeBlock_CollectionExpression1()
    {
        // The C# Formatter doesn't touch these types of initializers, so nor do we. This test
        // just verifies we don't regress things and start moving code around.
        await RunFormattingTestAsync(
            input: """
                @code {
                    private void M()
                    {
                        List<string> entries = [
                            "a",
                            "b",
                            "c"
                        ];
                    }
                }
                """,
            htmlFormatted: """
                @code {
                    private void M()
                    {
                        List<string> entries = [
                            "a",
                            "b",
                            "c"
                        ];
                    }
                }
                """,
            expected: """
                @code {
                    private void M()
                    {
                        List<string> entries = [
                            "a",
                            "b",
                            "c"
                        ];
                    }
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/11325")]
    public async Task CodeBlock_CollectionExpression2()
    {
        // The C# Formatter doesn't touch these types of initializers, so nor do we. This test
        // just verifies we don't regress things and start moving code around.
        await RunFormattingTestAsync(
            input: """
                @code {
                    private void M()
                    {
                        List<string> entries = [
                                "a",
                        "b",
                            "c"
                        ];
                    }
                }
                """,
            htmlFormatted: """
                @code {
                    private void M()
                    {
                        List<string> entries = [
                                "a",
                        "b",
                            "c"
                        ];
                    }
                }
                """,
            expected: """
                @code {
                    private void M()
                    {
                        List<string> entries = [
                                "a",
                        "b",
                            "c"
                        ];
                    }
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/11325")]
    public async Task CodeBlock_CollectionExpression3()
    {
        // The C# Formatter doesn't touch these types of initializers, so nor do we. This test
        // just verifies we don't regress things and start moving code around.
        await RunFormattingTestAsync(
            input: """
                @code {
                    private void M()
                    {
                        List<string> entries = [
                        ];
                    }
                }
                """,
            htmlFormatted: """
                @code {
                    private void M()
                    {
                        List<string> entries = [
                        ];
                    }
                }
                """,
            expected: """
                @code {
                    private void M()
                    {
                        List<string> entries = [
                        ];
                    }
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/11325")]
    public async Task CodeBlock_CollectionExpression4()
    {
        await RunFormattingTestAsync(
            input: """
                @code {
                    private void M(string[] strings)
                    {
                        List<string> entries = [  ..     strings,    "a",      "b",         "c"    ];
                    }
                }
                """,
            htmlFormatted: """
                @code {
                    private void M(string[] strings)
                    {
                        List<string> entries = [  ..     strings,    "a",      "b",         "c"    ];
                    }
                }
                """,
            expected: """
                @code {
                    private void M(string[] strings)
                    {
                        List<string> entries = [.. strings, "a", "b", "c"];
                    }
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/5618")]
    public async Task CodeBlock_EmptyObjectCollectionInitializers()
    {
        // The C# Formatter _does_ touch these types of initializers if they're empty. Who knew ¯\_(ツ)_/¯
        await RunFormattingTestAsync(
            input: """
                @code {
                    public void Foo()
                    {
                        SomeMethod(new List<string>()
                            {

                            });

                        SomeMethod(new Exception
                            {

                            });
                    }
                }
                """,
            htmlFormatted: """
                @code {
                    public void Foo()
                    {
                        SomeMethod(new List<string>()
                            {
                
                            });
                
                        SomeMethod(new Exception
                            {
                
                            });
                    }
                }
                """,
            expected: """
                @code {
                    public void Foo()
                    {
                        SomeMethod(new List<string>()
                        {

                        });

                        SomeMethod(new Exception
                        {

                        });
                    }
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/4498")]
    public async Task IfBlock_TopLevel()
    {
        await RunFormattingTestAsync(
            input: """
                        @if (true)
                {
                }
                """,
            htmlFormatted: """
                        @if (true)
                {
                }
                """,
            expected: """
                @if (true)
                {
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/4498")]
    public async Task IfBlock_TopLevel_WithOtherCode()
    {
        await RunFormattingTestAsync(
            input: """
                @{
                    // foo
                }

                        @if (true)
                {
                }
                """,
            htmlFormatted: """
                @{
                    // foo
                }
                
                        @if (true)
                {
                }
                """,
            expected: """
                @{
                    // foo
                }

                @if (true)
                {
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/4498")]
    public async Task IfBlock_TopLevel_WithOtherCode2()
    {
        await RunFormattingTestAsync(
            input: """
                @{

                    // foo

                        // foo

                }

                        @if (true)
                {
                }
                """,
            htmlFormatted: """
                @{
                
                    // foo
                
                        // foo
                
                }
                
                        @if (true)
                {
                }
                """,
            expected: """
                @{

                    // foo

                    // foo

                }

                @if (true)
                {
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/4498")]
    public async Task IfBlock_TopLevel_WithOtherCode3()
    {
        await RunFormattingTestAsync(
            input: """
                @{
                    var x = 3;

                    // foo
                }

                        @if (true)
                {
                }
                """,
            htmlFormatted: """
                @{
                    var x = 3;
                
                    // foo
                }
                
                        @if (true)
                {
                }
                """,
            expected: """
                @{
                    var x = 3;

                    // foo
                }

                @if (true)
                {
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/4498")]
    public async Task IfBlock_TopLevel_WithOtherCode4()
    {
        await RunFormattingTestAsync(
            input: """
                @{
                    var x = 3;
                }

                        @if (true)
                {
                }
                """,
            htmlFormatted: """
                @{
                    var x = 3;
                }
                
                        @if (true)
                {
                }
                """,
            expected: """
                @{
                    var x = 3;
                }

                @if (true)
                {
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/4498")]
    public async Task IfBlock_Nested()
    {
        await RunFormattingTestAsync(
            input: """
                <div>
                        @if (true)
                {
                }
                </div>
                """,
            htmlFormatted: """
                <div>
                    @if (true)
                    {
                    }
                </div>
                """,
            expected: """
                <div>
                    @if (true)
                    {
                    }
                </div>
                """);
    }

    [Fact]
    public async Task IfBlock_Nested_Contents()
    {
        await RunFormattingTestAsync(
            input: """
                <div>
                <div></div>
                        @if (true)
                {
                <div></div>
                }
                <div></div>
                </div>
                """,
            htmlFormatted: """
                <div>
                    <div></div>
                    @if (true)
                    {
                    <div></div>
                    }
                    <div></div>
                </div>
                """,
            expected: """
                <div>
                    <div></div>
                    @if (true)
                    {
                        <div></div>
                    }
                    <div></div>
                </div>
                """);
    }

    [Fact]
    public async Task IfBlock_SingleLine_Nested_Contents()
    {
        await RunFormattingTestAsync(
            input: """
                <div>
                <div></div>
                        @if (true) { <div></div> }
                <div></div>
                </div>
                """,
            htmlFormatted: """
                <div>
                    <div></div>
                    @if (true) { <div></div> }
                    <div></div>
                </div>
                """,
            expected: """
                <div>
                    <div></div>
                    @if (true)
                    {
                        <div></div>
                    }
                    <div></div>
                </div>
                """);
    }

    [Fact]
    public async Task Formats_MultilineExpressions()
    {
        await RunFormattingTestAsync(
            input: """
                @{
                    var icon = "/images/bootstrap-icons.svg#"
                        + GetIconName(login.ProviderDisplayName!);

                    var x = DateTime
                            .Now
                        .ToString();
                }

                @code
                {
                    public void M()
                    {
                        var icon2 = "/images/bootstrap-icons.svg#"
                            + GetIconName(login.ProviderDisplayName!);
                
                        var x2 = DateTime
                                .Now
                            .ToString();
                    }
                }
                """,
            htmlFormatted: """
                @{
                    var icon = "/images/bootstrap-icons.svg#"
                        + GetIconName(login.ProviderDisplayName!);
                
                    var x = DateTime
                            .Now
                        .ToString();
                }
                
                @code
                {
                    public void M()
                    {
                        var icon2 = "/images/bootstrap-icons.svg#"
                            + GetIconName(login.ProviderDisplayName!);
                
                        var x2 = DateTime
                                .Now
                            .ToString();
                    }
                }
                """,
            expected: """
                @{
                    var icon = "/images/bootstrap-icons.svg#"
                        + GetIconName(login.ProviderDisplayName!);

                    var x = DateTime
                            .Now
                        .ToString();
                }
                
                @code
                {
                    public void M()
                    {
                        var icon2 = "/images/bootstrap-icons.svg#"
                            + GetIconName(login.ProviderDisplayName!);
                
                        var x2 = DateTime
                                .Now
                            .ToString();
                    }
                }
                """);
    }

    [Fact]
    public async Task Formats_MultilineExpressionAtStartOfBlock()
    {
        await RunFormattingTestAsync(
            input: """
                @{
                    var x = DateTime
                        .Now
                        .ToString();
                }
                """,
            htmlFormatted: """
                @{
                    var x = DateTime
                        .Now
                        .ToString();
                }
                """,
            expected: """
                @{
                    var x = DateTime
                        .Now
                        .ToString();
                }
                """);
    }

    [Fact]
    public async Task Formats_MultilineExpressionAfterWhitespaceAtStartOfBlock()
    {
        await RunFormattingTestAsync(
            input: """
                @{



                    var x = DateTime
                        .Now
                        .ToString();
                }
                """,
            htmlFormatted: """
                @{
                
                
                
                    var x = DateTime
                        .Now
                        .ToString();
                }
                """,
            expected: """
                @{



                    var x = DateTime
                        .Now
                        .ToString();
                }
                """);
    }

    [Fact]
    public async Task Formats_MultilineExpressionNotAtStartOfBlock()
    {
        await RunFormattingTestAsync(
            input: """
                @{
                    //
                    var x = DateTime
                        .Now
                        .ToString();
                }
                """,
            htmlFormatted: """
                @{
                    //
                    var x = DateTime
                        .Now
                        .ToString();
                }
                """,
            expected: """
                @{
                    //
                    var x = DateTime
                        .Now
                        .ToString();
                }
                """);
    }

    [Fact]
    public async Task Formats_MultilineRazorComment()
    {
        await RunFormattingTestAsync(
            input: """
                <div></div>
                    @*
                line 1
                  line 2
                    line 3
                            *@
                @code
                {
                    void M()
                    {
                    @*
                line 1
                  line 2
                    line 3
                                *@
                    }
                }
                """,
            htmlFormatted: """
                <div></div>
                    @*
                line 1
                  line 2
                    line 3
                            *@
                @code
                {
                    void M()
                    {
                    @*
                line 1
                  line 2
                    line 3
                                *@
                    }
                }
                """,
            expected: """
                <div></div>
                @*
                line 1
                  line 2
                    line 3
                            *@
                @code
                {
                    void M()
                    {
                        @*
                line 1
                  line 2
                    line 3
                                *@
                    }
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/6192")]
    public async Task Formats_NoEditsForNoChanges()
    {
        var input = """
                @code {
                    public void M()
                    {
                        Console.WriteLine("Hello");
                        Console.WriteLine("World"); // <-- type/replace semicolon here
                    }
                }

                """;

        await RunFormattingTestAsync(input, htmlFormatted: """
                @code {
                    public void M()
                    {
                        Console.WriteLine("Hello");
                        Console.WriteLine("World"); // <-- type/replace semicolon here
                    }
                }
                
                """, expected: input,
            fileKind: RazorFileKind.Component);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/6158")]
    public async Task Format_NestedLambdas()
    {
        await RunFormattingTestAsync(
            input: """
                @code {

                    protected Action Goo(string input)
                    {
                        return async () =>
                        {
                        foreach (var x in input)
                        {
                        if (true)
                        {
                        await Task.Delay(1);

                        if (true)
                        {
                        // do some stufff
                        if (true)
                        {
                        }
                        }
                        }
                        }
                        };
                    }
                }
                """,
            htmlFormatted: """
                @code {
                
                    protected Action Goo(string input)
                    {
                        return async () =>
                        {
                        foreach (var x in input)
                        {
                        if (true)
                        {
                        await Task.Delay(1);
                
                        if (true)
                        {
                        // do some stufff
                        if (true)
                        {
                        }
                        }
                        }
                        }
                        };
                    }
                }
                """,
            expected: """
                @code {

                    protected Action Goo(string input)
                    {
                        return async () =>
                        {
                            foreach (var x in input)
                            {
                                if (true)
                                {
                                    await Task.Delay(1);

                                    if (true)
                                    {
                                        // do some stufff
                                        if (true)
                                        {
                                        }
                                    }
                                }
                            }
                        };
                    }
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/5693")]
    public async Task Format_NestedLambdasWithAtIf()
    {
        await RunFormattingTestAsync(
            input: """
                @code {

                    public RenderFragment RenderFoo()
                    {
                        return (__builder) =>
                        {
                            @if (true) { }
                        };
                    }
                }
                """,
            htmlFormatted: """
                @code {
                
                    public RenderFragment RenderFoo()
                    {
                        return (__builder) =>
                        {
                            @if (true) { }
                        };
                    }
                }
                """,
            expected: """
                @code {

                    public RenderFragment RenderFoo()
                    {
                        return (__builder) =>
                        {
                            @if (true) { }
                        };
                    }
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/9254")]
    public async Task RenderFragmentPresent()
    {
        await RunFormattingTestAsync(
            input: """
                @page "/"
                @code
                {
                    void T()
                    {
                        S("first"
                            + "second"
                            + "third");
                    }

                string[] S(string s) =>
                        s.Split(',')
                        . Select(s => s.Trim())
                        . ToArray();

                RenderFragment R => @<div></div>;
                }
                """,
            htmlFormatted: """
                @page "/"
                @code
                {
                    void T()
                    {
                        S("first"
                            + "second"
                            + "third");
                    }
                
                string[] S(string s) =>
                        s.Split(',')
                        . Select(s => s.Trim())
                        . ToArray();
                
                RenderFragment R => @<div></div>;
                }
                """,
            expected: """
                @page "/"
                @code
                {
                    void T()
                    {
                        S("first"
                            + "second"
                            + "third");
                    }

                    string[] S(string s) =>
                            s.Split(',')
                            .Select(s => s.Trim())
                            .ToArray();

                    RenderFragment R => @<div></div>;
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/9254")]
    public async Task RenderFragmentPresent2()
    {
        await RunFormattingTestAsync(
            input: """
                @page "/"
                @code
                {
                    void T()
                    {
                        S("first"
                            + "second"
                            + "third");
                    }

                string[] S(string s) =>
                        s.Split(',')
                        . Select(s => s.Trim())
                        . ToArray();

                RenderFragment     R      =>      @<div></div>;
                }
                """,
            htmlFormatted: """
                @page "/"
                @code
                {
                    void T()
                    {
                        S("first"
                            + "second"
                            + "third");
                    }
                
                string[] S(string s) =>
                        s.Split(',')
                        . Select(s => s.Trim())
                        . ToArray();
                
                RenderFragment     R      =>      @<div></div>;
                }
                """,
            expected: """
                @page "/"
                @code
                {
                    void T()
                    {
                        S("first"
                            + "second"
                            + "third");
                    }

                    string[] S(string s) =>
                            s.Split(',')
                            .Select(s => s.Trim())
                            .ToArray();

                    RenderFragment R => @<div></div>;
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/9254")]
    public async Task RenderFragmentPresent3()
    {
        await RunFormattingTestAsync(
            input: """
                @page "/"
                @code
                {
                    void T()
                    {
                        S("first"
                            + "second"
                            + "third");
                    }

                string[] S(string s) =>
                        s.Split(',')
                        . Select(s => s.Trim())
                        . ToArray();

                RenderFragment R=>@<div></div>;
                }
                """,
            htmlFormatted: """
                @page "/"
                @code
                {
                    void T()
                    {
                        S("first"
                            + "second"
                            + "third");
                    }
                
                string[] S(string s) =>
                        s.Split(',')
                        . Select(s => s.Trim())
                        . ToArray();
                
                RenderFragment R=>@<div></div>;
                }
                """,
            expected: """
                @page "/"
                @code
                {
                    void T()
                    {
                        S("first"
                            + "second"
                            + "third");
                    }

                    string[] S(string s) =>
                            s.Split(',')
                            .Select(s => s.Trim())
                            .ToArray();

                    RenderFragment R => @<div></div>;
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/9254")]
    public async Task RenderFragmentPresent4()
    {
        await RunFormattingTestAsync(
            input: """
                @page "/"
                @code
                {
                    void T()
                    {
                        S("first"
                            + "second"
                            + "third");
                    }

                string[] S(string s) =>
                        s.Split(',')
                        . Select(s => s.Trim())
                        . ToArray();

                RenderFragment R =>
                    @<div></div>;
                }
                """,
            htmlFormatted: """
                @page "/"
                @code
                {
                    void T()
                    {
                        S("first"
                            + "second"
                            + "third");
                    }
                
                string[] S(string s) =>
                        s.Split(',')
                        . Select(s => s.Trim())
                        . ToArray();
                
                RenderFragment R =>
                    @<div></div>;
                }
                """,
            expected: """
                @page "/"
                @code
                {
                    void T()
                    {
                        S("first"
                            + "second"
                            + "third");
                    }

                    string[] S(string s) =>
                            s.Split(',')
                            .Select(s => s.Trim())
                            .ToArray();

                    RenderFragment R =>
                        @<div></div>;
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/6150")]
    public async Task RenderFragment_InLambda()
    {
        // Formatting result here is not necessarily perfect, but in the new engine is stable
        await RunFormattingTestAsync(
            input: """
                @page "/"
                @using RazorClassLibrary2.Models

                @code{
                    private DateTime? date1;

                    Gopt<int> gopt = new Gopt<int>()
                    {
                        Name = "hi"
                    }
                    .Editor(m =>
                    {
                    return
                    @<text>hi</text>
                    ; }
                    );    
                }
                """,
            htmlFormatted: """
                @page "/"
                @using RazorClassLibrary2.Models
                
                @code{
                    private DateTime? date1;
                
                    Gopt<int> gopt = new Gopt<int>()
                    {
                        Name = "hi"
                    }
                    .Editor(m =>
                    {
                    return
                    @<text>hi</text>
                    ; }
                    );
                }
                """,
            expected: """
                @page "/"
                @using RazorClassLibrary2.Models

                @code {
                    private DateTime? date1;

                    Gopt<int> gopt = new Gopt<int>()
                    {
                        Name = "hi"
                    }
                    .Editor(m =>
                    {
                        return
                        @<text>hi</text>
                        ;
                    }
                    );
                }
                """);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/razor/issues/12310")]
    public async Task RenderFragment_Multiline(bool newLineBeforeBraceInLambda)
    {
        await RunFormattingTestAsync(
            input: """
                @page "/"

                @code{
                    protected RenderFragment RootFragment() =>
                        @<text>
                        @if (true)
                        {
                            <div class="test"
                                accesskey="k">
                                Hello
                                @if (true)
                                {
                                    <span>World</span>
                                }
                                else
                                {
                                    <span>Not World</span>
                                }
                            </div>
                        }
                        </text>;
                }
                """,
            htmlFormatted: """
                @page "/"
                
                @code{
                    protected RenderFragment RootFragment() =>
                        @<text>
                    @if (true)
                    {
                    <div class="test"
                         accesskey="k">
                        Hello
                        @if (true)
                        {
                        <span>World</span>
                        }
                        else
                        {
                        <span>Not World</span>
                        }
                    </div>
                    }
                </text>;
                }
                """,
            expected: """
                @page "/"

                @code {
                    protected RenderFragment RootFragment() =>
                        @<text>
                            @if (true)
                            {
                                <div class="test"
                                     accesskey="k">
                                    Hello
                                    @if (true)
                                    {
                                        <span>World</span>
                                    }
                                    else
                                    {
                                        <span>Not World</span>
                                    }
                                </div>
                            }
                        </text>;
                }
                """,
            csharpSyntaxFormattingOptions: RazorCSharpSyntaxFormattingOptions.Default with
            {
                NewLines = newLineBeforeBraceInLambda
                    ? RazorCSharpSyntaxFormattingOptions.Default.NewLines | RazorNewLinePlacement.BeforeOpenBraceInLambdaExpressionBody
                    : RazorCSharpSyntaxFormattingOptions.Default.NewLines & ~RazorNewLinePlacement.BeforeOpenBraceInLambdaExpressionBody
            });
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/razor/issues/12310")]
    public async Task RenderFragment_Multiline2(bool newLineBeforeBraceInLambda)
    {
        await RunFormattingTestAsync(
            input: """
                @page "/"

                @code{
                    protected RenderFragment RootFragment() =>
                        @<PageTitle>
                        @if (true)
                        {
                            <div class="test"
                                accesskey="k">
                                Hello
                                @if (true)
                                {
                                    <span>World</span>
                                }
                                else
                                {
                                    <span>Not World</span>
                                }
                            </div>
                        }
                        </PageTitle>;
                }
                """,
            htmlFormatted: """
                @page "/"
                
                @code{
                    protected RenderFragment RootFragment() =>
                        @<PageTitle>
                    @if (true)
                    {
                    <div class="test"
                         accesskey="k">
                        Hello
                        @if (true)
                        {
                        <span>World</span>
                        }
                        else
                        {
                        <span>Not World</span>
                        }
                    </div>
                    }
                </PageTitle>;
                }
                """,
            expected: """
                @page "/"

                @code {
                    protected RenderFragment RootFragment() =>
                        @<PageTitle>
                            @if (true)
                            {
                                <div class="test"
                                     accesskey="k">
                                    Hello
                                    @if (true)
                                    {
                                        <span>World</span>
                                    }
                                    else
                                    {
                                        <span>Not World</span>
                                    }
                                </div>
                            }
                        </PageTitle>;
                }
                """,
            csharpSyntaxFormattingOptions: RazorCSharpSyntaxFormattingOptions.Default with
            {
                NewLines = newLineBeforeBraceInLambda
                    ? RazorCSharpSyntaxFormattingOptions.Default.NewLines | RazorNewLinePlacement.BeforeOpenBraceInLambdaExpressionBody
                    : RazorCSharpSyntaxFormattingOptions.Default.NewLines & ~RazorNewLinePlacement.BeforeOpenBraceInLambdaExpressionBody
            });
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/razor/issues/12310")]
    public async Task RenderFragment_Multiline3(bool newLineBeforeBraceInLambda)
    {
        await RunFormattingTestAsync(
            input: """
                @page "/"

                @code{
                    protected RenderFragment RootFragment() => @<text>
                        @if (true)
                        {
                            <div class="test"
                                accesskey="k">
                                Hello
                                @if (true)
                                {
                                    <span>World</span>
                                }
                                else
                                {
                                    <span>Not World</span>
                                }
                            </div>
                        }
                        </text>;
                }
                """,
            htmlFormatted: """
                @page "/"
                
                @code{
                    protected RenderFragment RootFragment() => @<text>
                    @if (true)
                    {
                    <div class="test"
                         accesskey="k">
                        Hello
                        @if (true)
                        {
                        <span>World</span>
                        }
                        else
                        {
                        <span>Not World</span>
                        }
                    </div>
                    }
                </text>;
                }
                """,
            expected: """
                @page "/"

                @code {
                    protected RenderFragment RootFragment() => @<text>
                        @if (true)
                        {
                            <div class="test"
                                 accesskey="k">
                                Hello
                                @if (true)
                                {
                                    <span>World</span>
                                }
                                else
                                {
                                    <span>Not World</span>
                                }
                            </div>
                        }
                    </text>;
                }
                """,
            csharpSyntaxFormattingOptions: RazorCSharpSyntaxFormattingOptions.Default with
            {
                NewLines = newLineBeforeBraceInLambda
                    ? RazorCSharpSyntaxFormattingOptions.Default.NewLines | RazorNewLinePlacement.BeforeOpenBraceInLambdaExpressionBody
                    : RazorCSharpSyntaxFormattingOptions.Default.NewLines & ~RazorNewLinePlacement.BeforeOpenBraceInLambdaExpressionBody
            });
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/razor/issues/12310")]
    public async Task RenderFragment_Multiline4(bool newLineBeforeBraceInLambda)
    {
        await RunFormattingTestAsync(
            input: """
                @page "/"

                @code{
                    protected RenderFragment RootFragment()=>@<text>
                        @if (true)
                        {
                            <div class="test"
                                accesskey="k">
                                Hello
                                @if (true)
                                {
                                    <span>World</span>
                                }
                                else
                                {
                                    <span>Not World</span>
                                }
                            </div>
                        }
                        </text>;
                }
                """,
            htmlFormatted: """
                @page "/"
                
                @code{
                    protected RenderFragment RootFragment()=>@<text>
                    @if (true)
                    {
                    <div class="test"
                         accesskey="k">
                        Hello
                        @if (true)
                        {
                        <span>World</span>
                        }
                        else
                        {
                        <span>Not World</span>
                        }
                    </div>
                    }
                </text>;
                }
                """,
            expected: """
                @page "/"

                @code {
                    protected RenderFragment RootFragment() => @<text>
                        @if (true)
                        {
                            <div class="test"
                                 accesskey="k">
                                Hello
                                @if (true)
                                {
                                    <span>World</span>
                                }
                                else
                                {
                                    <span>Not World</span>
                                }
                            </div>
                        }
                    </text>;
                }
                """,
            csharpSyntaxFormattingOptions: RazorCSharpSyntaxFormattingOptions.Default with
            {
                NewLines = newLineBeforeBraceInLambda
                    ? RazorCSharpSyntaxFormattingOptions.Default.NewLines | RazorNewLinePlacement.BeforeOpenBraceInLambdaExpressionBody
                    : RazorCSharpSyntaxFormattingOptions.Default.NewLines & ~RazorNewLinePlacement.BeforeOpenBraceInLambdaExpressionBody
            });
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/razor/issues/12310")]
    public async Task RenderFragment_Multiline5(bool newLineBeforeBraceInLambda)
    {
        await RunFormattingTestAsync(
            input: """
                @page "/"

                @code{
                    protected     RenderFragment     RootFragment()      =>     @<text>
                        @if (true)
                        {
                            <div class="test"
                                accesskey="k">
                                Hello
                                @if (true)
                                {
                                    <span>World</span>
                                }
                                else
                                {
                                    <span>Not World</span>
                                }
                            </div>
                        }
                        </text>;
                }
                """,
            htmlFormatted: """
                @page "/"
                
                @code{
                    protected     RenderFragment     RootFragment()      =>     @<text>
                    @if (true)
                    {
                    <div class="test"
                         accesskey="k">
                        Hello
                        @if (true)
                        {
                        <span>World</span>
                        }
                        else
                        {
                        <span>Not World</span>
                        }
                    </div>
                    }
                </text>;
                }
                """,
            expected: """
                @page "/"

                @code {
                    protected RenderFragment RootFragment() => @<text>
                        @if (true)
                        {
                            <div class="test"
                                 accesskey="k">
                                Hello
                                @if (true)
                                {
                                    <span>World</span>
                                }
                                else
                                {
                                    <span>Not World</span>
                                }
                            </div>
                        }
                    </text>;
                }
                """,
            csharpSyntaxFormattingOptions: RazorCSharpSyntaxFormattingOptions.Default with
            {
                NewLines = newLineBeforeBraceInLambda
                    ? RazorCSharpSyntaxFormattingOptions.Default.NewLines | RazorNewLinePlacement.BeforeOpenBraceInLambdaExpressionBody
                    : RazorCSharpSyntaxFormattingOptions.Default.NewLines & ~RazorNewLinePlacement.BeforeOpenBraceInLambdaExpressionBody
            });
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/razor/issues/12310")]
    public async Task RenderFragment_Multiline6(bool newLineBeforeBraceInLambda)
    {
        await RunFormattingTestAsync(
            input: """
                @page "/"

                @code{
                    protected RenderFragment RootFragment() =>
                        @<text>
                        @if (true)
                        {
                            <div class="test"
                                accesskey="k">
                                Hello
                                @if (true)
                                {
                                    <span>World</span>
                                }
                                else
                                {
                                    <span>Not World</span>
                                }
                            </div>
                        }
                        </text>
                        ;
                }
                """,
            htmlFormatted: """
                @page "/"
                
                @code{
                    protected RenderFragment RootFragment() =>
                        @<text>
                    @if (true)
                    {
                    <div class="test"
                         accesskey="k">
                        Hello
                        @if (true)
                        {
                        <span>World</span>
                        }
                        else
                        {
                        <span>Not World</span>
                        }
                    </div>
                    }
                </text>
                        ;
                }
                """,
            expected: """
                @page "/"

                @code {
                    protected RenderFragment RootFragment() =>
                        @<text>
                            @if (true)
                            {
                                <div class="test"
                                     accesskey="k">
                                    Hello
                                    @if (true)
                                    {
                                        <span>World</span>
                                    }
                                    else
                                    {
                                        <span>Not World</span>
                                    }
                                </div>
                            }
                        </text>
                ;
                }
                """,
            csharpSyntaxFormattingOptions: RazorCSharpSyntaxFormattingOptions.Default with
            {
                NewLines = newLineBeforeBraceInLambda
                    ? RazorCSharpSyntaxFormattingOptions.Default.NewLines | RazorNewLinePlacement.BeforeOpenBraceInLambdaExpressionBody
                    : RazorCSharpSyntaxFormattingOptions.Default.NewLines & ~RazorNewLinePlacement.BeforeOpenBraceInLambdaExpressionBody
            });
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/razor/issues/12310")]
    public async Task RenderFragment_Multiline7(bool newLineBeforeBraceInLambda)
    {
        await RunFormattingTestAsync(
            input: """
                @page "/"

                @code{
                    protected RenderFragment RootFragment() =>
                        @<div>
                            <div class="test"
                                accesskey="k">
                                Hello
                                <div>
                                    <span>World</span>
                                </div>
                            </div>
                        </div>
                        ;
                }
                """,
            htmlFormatted: """
                @page "/"
                
                @code{
                    protected RenderFragment RootFragment() =>
                        @<div>
                    <div class="test"
                         accesskey="k">
                        Hello
                        <div>
                            <span>World</span>
                        </div>
                    </div>
                </div>
                        ;
                }
                """,
            expected: """
                @page "/"

                @code {
                    protected RenderFragment RootFragment() =>
                        @<div>
                            <div class="test"
                                 accesskey="k">
                                Hello
                                <div>
                                    <span>World</span>
                                </div>
                            </div>
                        </div>
                ;
                }
                """,
            csharpSyntaxFormattingOptions: RazorCSharpSyntaxFormattingOptions.Default with
            {
                NewLines = newLineBeforeBraceInLambda
                    ? RazorCSharpSyntaxFormattingOptions.Default.NewLines | RazorNewLinePlacement.BeforeOpenBraceInLambdaExpressionBody
                    : RazorCSharpSyntaxFormattingOptions.Default.NewLines & ~RazorNewLinePlacement.BeforeOpenBraceInLambdaExpressionBody
            });
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/13064")]
    public async Task RenderFragment_Multiline_ComponentAttributesWithExplicitExpression()
    {
        await RunFormattingTestAsync(
            input: """
                @code {
                    [Fact]
                    private void RenderFragment_First()
                    {
                        Render(@<FluentAutocomplete Id="my-list"
                                        TOption="string"
                                TValue="string"
                                              Multiple="false"
                                   Items="@Digits"
                                              SelectedItem="@("Three")" />);
                    }

                    [Fact]
                    private void RenderFragment_Second()
                    {
                    }
                }
                """,
            htmlFormatted: """
                @code {
                    [Fact]
                    private void RenderFragment_First()
                    {
                        Render(@
                <FluentAutocomplete Id="my-list"
                                    TOption="string"
                                    TValue="string"
                                    Multiple="false"
                                    Items="@Digits"
                                    SelectedItem="@("Three")" />);
                    }

                    [Fact]
                    private void RenderFragment_Second()
                    {
                    }
                }
                """,
            expected: """
                @code {
                    [Fact]
                    private void RenderFragment_First()
                    {
                        Render(@<FluentAutocomplete Id="my-list"
                                                    TOption="string"
                                                    TValue="string"
                                                    Multiple="false"
                                                    Items="@Digits"
                                                    SelectedItem="@("Three")" />);
                    }

                    [Fact]
                    private void RenderFragment_Second()
                    {
                    }
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/13064")]
    public async Task RenderFragment_Multiline_ComponentAttributesWithExplicitExpression_IndentByOne()
    {
        await RunFormattingTestAsync(
            input: """
                @code {
                    [Fact]
                    private void RenderFragment_First()
                    {
                        Render(@<FluentAutocomplete Id="my-list"
                                        TOption="string"
                                TValue="string"
                                              Multiple="false"
                                   Items="@Digits"
                                              SelectedItem="@("Three")" />);
                    }

                    [Fact]
                    private void RenderFragment_Second()
                    {
                    }
                }
                """,
            htmlFormatted: """
                @code {
                    [Fact]
                    private void RenderFragment_First()
                    {
                        Render(@
                <FluentAutocomplete Id="my-list"
                                    TOption="string"
                                    TValue="string"
                                    Multiple="false"
                                    Items="@Digits"
                                    SelectedItem="@("Three")" />);
                    }

                    [Fact]
                    private void RenderFragment_Second()
                    {
                    }
                }
                """,
            expected: """
                @code {
                    [Fact]
                    private void RenderFragment_First()
                    {
                        Render(@<FluentAutocomplete Id="my-list"
                            TOption="string"
                            TValue="string"
                            Multiple="false"
                            Items="@Digits"
                            SelectedItem="@("Three")" />);
                    }

                    [Fact]
                    private void RenderFragment_Second()
                    {
                    }
                }
                """,
            attributeIndentStyle: AttributeIndentStyle.IndentByOne);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/13064")]
    public async Task RenderFragment_Multiline_ComponentAttributesWithExplicitExpression_IndentByTwo()
    {
        await RunFormattingTestAsync(
            input: """
                @code {
                    [Fact]
                    private void RenderFragment_First()
                    {
                        Render(@<FluentAutocomplete Id="my-list"
                                        TOption="string"
                                TValue="string"
                                              Multiple="false"
                                   Items="@Digits"
                                              SelectedItem="@("Three")" />);
                    }

                    [Fact]
                    private void RenderFragment_Second()
                    {
                    }
                }
                """,
            htmlFormatted: """
                @code {
                    [Fact]
                    private void RenderFragment_First()
                    {
                        Render(@
                <FluentAutocomplete Id="my-list"
                                    TOption="string"
                                    TValue="string"
                                    Multiple="false"
                                    Items="@Digits"
                                    SelectedItem="@("Three")" />);
                    }

                    [Fact]
                    private void RenderFragment_Second()
                    {
                    }
                }
                """,
            expected: """
                @code {
                    [Fact]
                    private void RenderFragment_First()
                    {
                        Render(@<FluentAutocomplete Id="my-list"
                                TOption="string"
                                TValue="string"
                                Multiple="false"
                                Items="@Digits"
                                SelectedItem="@("Three")" />);
                    }

                    [Fact]
                    private void RenderFragment_Second()
                    {
                    }
                }
                """,
            attributeIndentStyle: AttributeIndentStyle.IndentByTwo);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/13064")]
    public async Task RenderFragment_Multiline_NestedComponentAttributesWithExplicitExpression()
    {
        await RunFormattingTestAsync(
            input: """
                @code {
                    [Fact]
                    private void RenderFragment_First()
                    {
                        Render(@<div>
                                <FluentAutocomplete Id="my-list"
                                                TOption="string"
                                        TValue="string"
                                                      Multiple="false"
                                           Items="@Digits"
                                                      SelectedItem="@("Three")" />
                        </div>);
                    }

                    [Fact]
                    private void RenderFragment_Second()
                    {
                    }
                }
                """,
            htmlFormatted: """
                @code {
                    [Fact]
                    private void RenderFragment_First()
                    {
                        Render(@<div>
                    <FluentAutocomplete Id="my-list"
                                        TOption="string"
                                        TValue="string"
                                        Multiple="false"
                                        Items="@Digits"
                                        SelectedItem="@("Three")" />
                </div>);
                    }

                    [Fact]
                    private void RenderFragment_Second()
                    {
                    }
                }
                """,
            expected: """
                @code {
                    [Fact]
                    private void RenderFragment_First()
                    {
                        Render(@<div>
                            <FluentAutocomplete Id="my-list"
                                                TOption="string"
                                                TValue="string"
                                                Multiple="false"
                                                Items="@Digits"
                                                SelectedItem="@("Three")" />
                        </div>);
                    }

                    [Fact]
                    private void RenderFragment_Second()
                    {
                    }
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/9119")]
    public async Task CollectionInitializers()
    {
        await RunFormattingTestAsync(
            input: """
                @{
                    // Stable
                    var formatMe = new string[] {
                        "One",
                        "Two",
                        "Three",
                    };

                    // Closing brace advances to the right
                    var formatMeTwo = new string[]
                    {
                        "One",
                        "Two",
                        "Three",
                    };

                    // Stable
                    var formatMeThree = new List<string> {
                        "One",
                        "Two",
                        "Three",
                    };
                
                    // Opening brace advances to the right
                    var formatMeFour = new List<string>
                    {
                        "One",
                        "Two",
                        "Three",
                    };
                }
                """,
            htmlFormatted: """
                @{
                    // Stable
                    var formatMe = new string[] {
                        "One",
                        "Two",
                        "Three",
                    };
                
                    // Closing brace advances to the right
                    var formatMeTwo = new string[]
                    {
                        "One",
                        "Two",
                        "Three",
                    };
                
                    // Stable
                    var formatMeThree = new List<string> {
                        "One",
                        "Two",
                        "Three",
                    };
                
                    // Opening brace advances to the right
                    var formatMeFour = new List<string>
                    {
                        "One",
                        "Two",
                        "Three",
                    };
                }
                """,
            expected: """
                @{
                    // Stable
                    var formatMe = new string[] {
                        "One",
                        "Two",
                        "Three",
                    };
                
                    // Closing brace advances to the right
                    var formatMeTwo = new string[]
                    {
                        "One",
                        "Two",
                        "Three",
                    };
                
                    // Stable
                    var formatMeThree = new List<string> {
                        "One",
                        "Two",
                        "Three",
                    };
                
                    // Opening brace advances to the right
                    var formatMeFour = new List<string>
                    {
                        "One",
                        "Two",
                        "Three",
                    };
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/9711")]
    public async Task Directives()
    {
        await RunFormattingTestAsync(
            input: """
                        @page "/"

                        @using System
                        @inject object Foo


                """,
            htmlFormatted: """
                        @page "/"
                
                        @using System
                        @inject object Foo
                
                
                """,
            expected: """
                @page "/"
                
                @using System
                @inject object Foo
                
                
                """);
    }

    [Fact]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2347107")]
    public async Task ImplicitExpressionAtEndOfCodeBlock()
    {
        await RunFormattingTestAsync(
            input: """
                @page "/"
                @model IndexModel

                <div>
                </div>

                @functions {void Foo() { }}@Foo()
                """,
            htmlFormatted: """
                @page "/"
                @model IndexModel
                
                <div>
                </div>
                
                @functions {void Foo() { }}@Foo()
                """,
            expected: """
                @page "/"
                @model IndexModel
                
                <div>
                </div>
                
                @functions {
                    void Foo() { }
                }
                @Foo()
                """,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task LineBreakAtTheEndOfBlocks()
    {
        await RunFormattingTestAsync(
            input: """
                @page "/"
                @model IndexModel

                <div>
                </div>

                @code {void Foo() { }}@Foo.ToString(   1  )
                """,
            htmlFormatted: """
                @page "/"
                @model IndexModel
                
                <div>
                </div>
                
                @code {void Foo() { }}@Foo.ToString(   1  )
                """,
            expected: """
                @page "/"
                @model IndexModel
                
                <div>
                </div>
                
                @code {
                    void Foo() { }
                }
                @Foo.ToString(1)
                """);
    }

    [Fact]
    public async Task EscapedAtSignsInCSS()
    {
        await RunFormattingTestAsync(
            input: """
                @page "/"
                @model IndexModel

                <style>
                    @@media only screen and (max-width: 600px) {
                        body {
                            background-color: lightblue;
                        }
                    }
                </style>

                <style>
                    @@font-face {
                        src: url();
                    }
                </style>

                @if (RendererInfo.IsInteractive)
                {
                <button />
                }
                """,
            htmlFormatted: """
                @page "/"
                @model IndexModel
                
                <style>
                    @@media only screen and (max-width: 600px) {
                        body {
                            background-color: lightblue;
                        }
                    }
                </style>
                
                <style>
                    @@font-face {
                        src: url();
                    }
                </style>
                
                @if (RendererInfo.IsInteractive)
                {
                <button />
                }
                """,
            expected: """
                @page "/"
                @model IndexModel
                
                <style>
                    @@media only screen and (max-width: 600px) {
                        body {
                            background-color: lightblue;
                        }
                    }
                </style>

                <style>
                    @@font-face {
                        src: url();
                    }
                </style>

                @if (RendererInfo.IsInteractive)
                {
                    <button />
                }
                """);
    }

    [Fact]
    public async Task PartialTagHelper()
    {
        await RunFormattingTestAsync(
            input: """
                @page "/"
                
                <div>
                    <partial name="~/Views/Shared/_TestimonialRow.cshtml"
                    model="new DefaultTitleContentAreaViewModel
                    {
                    Title = Model.CurrentPage.TestimonialsTitle,
                    ContentArea = Model.CurrentPage.TestimonialsContentArea,
                    ChildCssClass = string.Empty
                    }" />
                </div>
                """,
            htmlFormatted: """
                @page "/"
                
                <div>
                    <partial name="~/Views/Shared/_TestimonialRow.cshtml"
                             model="new DefaultTitleContentAreaViewModel
                    {
                    Title = Model.CurrentPage.TestimonialsTitle,
                    ContentArea = Model.CurrentPage.TestimonialsContentArea,
                    ChildCssClass = string.Empty
                    }" />
                </div>
                """,
            expected: """
                @page "/"
                
                <div>
                    <partial name="~/Views/Shared/_TestimonialRow.cshtml"
                             model="new DefaultTitleContentAreaViewModel
                             {
                                 Title = Model.CurrentPage.TestimonialsTitle,
                                 ContentArea = Model.CurrentPage.TestimonialsContentArea,
                                 ChildCssClass = string.Empty
                             }" />
                </div>
                """,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task MultilineExplicitExpression()
    {
        await RunFormattingTestAsync(
            input: """
                @page "/"

                <partial name="~/Views/Shared/_TestimonialRow.cshtml"
                    model="@(new DefaultTitleContentAreaViewModel
                    {
                        Title = Model.CurrentPage.TestimonialsTitle,
                        ContentArea = Model.CurrentPage.TestimonialsContentArea,
                        ChildCssClass = string.Empty
                    })" />

                <partial model="@(new DefaultTitleContentAreaViewModel
                    {
                        Title = Model.CurrentPage.TestimonialsTitle,
                        ContentArea = Model.CurrentPage.TestimonialsContentArea,
                        ChildCssClass = string.Empty
                    })" />
                
                <div>
                    <partial name="~/Views/Shared/_TestimonialRow.cshtml"
                    model="@(new DefaultTitleContentAreaViewModel
                    {
                    Title = Model.CurrentPage.TestimonialsTitle,
                    ContentArea = Model.CurrentPage.TestimonialsContentArea,
                    ChildCssClass = string.Empty
                    })" />
                </div>
                """,
            htmlFormatted: """
                @page "/"
                
                <partial name="~/Views/Shared/_TestimonialRow.cshtml"
                         model="@(new DefaultTitleContentAreaViewModel
                    {
                        Title = Model.CurrentPage.TestimonialsTitle,
                        ContentArea = Model.CurrentPage.TestimonialsContentArea,
                        ChildCssClass = string.Empty
                    })" />
                
                <partial model="@(new DefaultTitleContentAreaViewModel
                    {
                        Title = Model.CurrentPage.TestimonialsTitle,
                        ContentArea = Model.CurrentPage.TestimonialsContentArea,
                        ChildCssClass = string.Empty
                    })" />
                
                <div>
                    <partial name="~/Views/Shared/_TestimonialRow.cshtml"
                             model="@(new DefaultTitleContentAreaViewModel
                    {
                    Title = Model.CurrentPage.TestimonialsTitle,
                    ContentArea = Model.CurrentPage.TestimonialsContentArea,
                    ChildCssClass = string.Empty
                    })" />
                </div>
                """,
            expected: """
                @page "/"

                <partial name="~/Views/Shared/_TestimonialRow.cshtml"
                         model="@(new DefaultTitleContentAreaViewModel
                         {
                             Title = Model.CurrentPage.TestimonialsTitle,
                             ContentArea = Model.CurrentPage.TestimonialsContentArea,
                             ChildCssClass = string.Empty
                         })" />

                <partial model="@(new DefaultTitleContentAreaViewModel
                         {
                             Title = Model.CurrentPage.TestimonialsTitle,
                             ContentArea = Model.CurrentPage.TestimonialsContentArea,
                             ChildCssClass = string.Empty
                         })" />

                <div>
                    <partial name="~/Views/Shared/_TestimonialRow.cshtml"
                             model="@(new DefaultTitleContentAreaViewModel
                             {
                                 Title = Model.CurrentPage.TestimonialsTitle,
                                 ContentArea = Model.CurrentPage.TestimonialsContentArea,
                                 ChildCssClass = string.Empty
                             })" />
                </div>
                """);
    }

    [Fact]
    public async Task MultilineExplicitExpression_IsStable()
    {
        // This test explicitly validates that the expected output from the above test results in stable formatting.
        var code = """
                @page "/"

                <partial name="~/Views/Shared/_TestimonialRow.cshtml"
                         model="@(new DefaultTitleContentAreaViewModel
                         {
                             Title = Model.CurrentPage.TestimonialsTitle,
                             ContentArea = Model.CurrentPage.TestimonialsContentArea,
                             ChildCssClass = string.Empty
                         })" />

                <partial model="@(new DefaultTitleContentAreaViewModel
                         {
                             Title = Model.CurrentPage.TestimonialsTitle,
                             ContentArea = Model.CurrentPage.TestimonialsContentArea,
                             ChildCssClass = string.Empty
                         })" />

                <div>
                    <partial name="~/Views/Shared/_TestimonialRow.cshtml"
                             model="@(new DefaultTitleContentAreaViewModel
                             {
                                 Title = Model.CurrentPage.TestimonialsTitle,
                                 ContentArea = Model.CurrentPage.TestimonialsContentArea,
                                 ChildCssClass = string.Empty
                             })" />
                </div>
                """;
        await RunFormattingTestAsync(
            input: code,
            htmlFormatted: """
                @page "/"
                
                <partial name="~/Views/Shared/_TestimonialRow.cshtml"
                         model="@(new DefaultTitleContentAreaViewModel
                         {
                             Title = Model.CurrentPage.TestimonialsTitle,
                             ContentArea = Model.CurrentPage.TestimonialsContentArea,
                             ChildCssClass = string.Empty
                         })" />
                
                <partial model="@(new DefaultTitleContentAreaViewModel
                         {
                             Title = Model.CurrentPage.TestimonialsTitle,
                             ContentArea = Model.CurrentPage.TestimonialsContentArea,
                             ChildCssClass = string.Empty
                         })" />
                
                <div>
                    <partial name="~/Views/Shared/_TestimonialRow.cshtml"
                             model="@(new DefaultTitleContentAreaViewModel
                             {
                                 Title = Model.CurrentPage.TestimonialsTitle,
                                 ContentArea = Model.CurrentPage.TestimonialsContentArea,
                                 ChildCssClass = string.Empty
                             })" />
                </div>
                """,
            expected: code);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/11622")]
    public async Task TextArea()
    {
        var code = """
                @page "/"
                
                @if (true)
                {
                    <textarea id="textarea1">
                    a
                        @if (true)
                        {
                        b
                            }
                            c
                    </textarea>
                }
                
                <textarea id="textarea2">
                    a
                        @if (true)
                        {
                        b
                            }
                            c
                    </textarea>
                
                <div>
                    <textarea id="textarea3">
                            a
                                @if (true)
                                {
                                b
                                    }
                                    c
                        </textarea>
                </div>
                """;
        await RunFormattingTestAsync(
            input: code,
            htmlFormatted: """
                @page "/"
                
                @if (true)
                {
                <textarea id="textarea1">
                    a
                        @if (true)
                        {
                        b
                            }
                            c
                    </textarea>
                }
                
                <textarea id="textarea2">
                    a
                        @if (true)
                        {
                        b
                            }
                            c
                    </textarea>
                
                <div>
                    <textarea id="textarea3">
                            a
                                @if (true)
                                {
                                b
                                    }
                                    c
                        </textarea>
                </div>
                """,
            expected: code);
    }

    [Fact]
    internal Task TextArea_WithAttributes()
        => RunFormattingTestAsync(
            input: """
                <textarea name="foo"
                                    id="foo">@("Foo")
                     test</textarea>
                """,
            htmlFormatted: """
                <textarea name="foo"
                          id="foo">@("Foo")
                     test</textarea>
                """,
            expected: """
                <textarea name="foo"
                          id="foo">@("Foo")
                     test</textarea>
                """);

    [Fact]
    internal Task TextArea_WithAttributes_IndentByOne()
        => RunFormattingTestAsync(
            input: """
                <textarea name="foo"
                                    id="foo">@("Foo")
                     test</textarea>
                """,
            htmlFormatted: """
                <textarea name="foo"
                          id="foo">@("Foo")
                     test</textarea>
                """,
            expected: """
                <textarea name="foo"
                    id="foo">@("Foo")
                     test</textarea>
                """,
            attributeIndentStyle: AttributeIndentStyle.IndentByOne);

    [Fact]
    internal Task TextArea_WithAttributes_IndentByTwo()
        => RunFormattingTestAsync(
            input: """
                <textarea name="foo"
                                    id="foo">@("Foo")
                     test</textarea>
                """,
            htmlFormatted: """
                <textarea name="foo"
                          id="foo">@("Foo")
                     test</textarea>
                """,
            expected: """
                <textarea name="foo"
                        id="foo">@("Foo")
                     test</textarea>
                """,
            attributeIndentStyle: AttributeIndentStyle.IndentByTwo);

    [Fact]
    internal Task PreTag_InIf()
        => RunFormattingTestAsync(
            input: """
                @if (true)
                {
                    <pre>
                    a
                        @if (true)
                        {
                        b
                            }
                            c
                    </pre>
                }
                """,
            htmlFormatted: """
                @if (true)
                {
                <pre>
                    a
                        @if (true)
                        {
                        b
                            }
                            c
                    </pre>
                }
                """,
            expected: """
                @if (true)
                {
                    <pre>
                    a
                        @if (true)
                        {
                        b
                            }
                            c
                    </pre>
                }
                """);

    [Fact]
    internal Task PreTag()
        => RunFormattingTestAsync(
            input: """
                <pre>
                    a
                        @if (true)
                        {
                        b
                            }
                            c
                    </pre>
                """,
            htmlFormatted: """
                <pre>
                    a
                        @if (true)
                        {
                        b
                            }
                            c
                    </pre>
                """,
            expected: """
                <pre>
                    a
                        @if (true)
                        {
                        b
                            }
                            c
                    </pre>
                """);

    [Fact]
    internal Task PreTag_Nested()
        => RunFormattingTestAsync(
            input: """
                <div>
                    <pre>
                            a
                                @if (true)
                                {
                                b
                                    }
                                    c
                        </pre>
                </div>
                """,
            htmlFormatted: """
                <div>
                    <pre>
                            a
                                @if (true)
                                {
                                b
                                    }
                                    c
                        </pre>
                </div>
                """,
            expected: """
                <div>
                    <pre>
                            a
                                @if (true)
                                {
                                b
                                    }
                                    c
                        </pre>
                </div>
                """);

    [Fact]
    internal Task PreTag_WithAttributes()
        => RunFormattingTestAsync(
            input: """
                <pre class="code"
                                    id="foo">some content
                           more content</pre>
                """,
            htmlFormatted: """
                <pre class="code"
                     id="foo">some content
                           more content</pre>
                """,
            expected: """
                <pre class="code"
                     id="foo">some content
                           more content</pre>
                """);

    [Fact]
    public async Task PreTag_IndentStartTag()
    {
        await RunFormattingTestAsync(
            input: """
                <div>
                        <pre>
                    content here
                        </pre>
                </div>
                """,
            htmlFormatted: """
                <div>
                    <pre>
                    content here
                        </pre>
                </div>
                """,
            expected: """
                <div>
                    <pre>
                    content here
                        </pre>
                </div>
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/11777")]
    public Task RangeFormat_AfterProperty()
        => RunFormattingTestAsync(
            input: """
                @code
                {
                    public string S
                    {
                        get => _s;
                        set
                        {
                            _s = value;
                        }
                    } [|private string _s = "";|]
                }
                """,
            htmlFormatted: """
                @code
                {
                    public string S
                    {
                        get => _s;
                        set
                        {
                            _s = value;
                        }
                    } private string _s = "";
                }
                """,
            expected: """
                @code
                {
                    public string S
                    {
                        get => _s;
                        set
                        {
                            _s = value;
                        }
                    }
                    private string _s = "";
                }
                """,
            debugAssertsEnabled: false
);

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/11873")]
    public Task NestedExplicitExpression1()
        => RunFormattingTestAsync(
            input: """
                @if (true)
                {
                    <div class="d-flex">
                        <div class="d-flex flex-column" style="text-align: end;">
                            @if (true)
                            {
                                <span>
                                    @((((true) ? 123d : 0d) +
                                        (true ? 123d : 0d)
                                        ).ToString("F2", CultureInfo.InvariantCulture)) €
                                </span>
                                <hr class="my-1" />
                                <span>
                                    @((123d +
                                        ((true) ? 123d : 0d) +
                                        (true ? 123d : 0d)
                                        ).ToString("F2", CultureInfo.InvariantCulture)) €
                                </span>
                            }
                        </div>
                    </div>
                }
                """,
            htmlFormatted: """
                @if (true)
                {
                <div class="d-flex">
                    <div class="d-flex flex-column" style="text-align: end;">
                        @if (true)
                        {
                        <span>
                            @((((true) ? 123d : 0d) +
                            (true ? 123d : 0d)
                            ).ToString("F2", CultureInfo.InvariantCulture)) €
                        </span>
                        <hr class="my-1" />
                        <span>
                            @((123d +
                            ((true) ? 123d : 0d) +
                            (true ? 123d : 0d)
                            ).ToString("F2", CultureInfo.InvariantCulture)) €
                        </span>
                        }
                    </div>
                </div>
                }
                """,
            expected: """
                @if (true)
                {
                    <div class="d-flex">
                        <div class="d-flex flex-column" style="text-align: end;">
                            @if (true)
                            {
                                <span>
                                    @((((true) ? 123d : 0d) +
                                        (true ? 123d : 0d)
                                        ).ToString("F2", CultureInfo.InvariantCulture)) €
                                </span>
                                <hr class="my-1" />
                                <span>
                                    @((123d +
                                        ((true) ? 123d : 0d) +
                                        (true ? 123d : 0d)
                                        ).ToString("F2", CultureInfo.InvariantCulture)) €
                                </span>
                            }
                        </div>
                    </div>
                }
                """);

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/11873")]
    public Task NestedExplicitExpression1_Stable()
    {
        var code = """
            @if (true)
            {
                <div class="d-flex">
                    <div class="d-flex flex-column" style="text-align: end;">
                        @if (true)
                        {
                            <span>
                                @((((true) ? 123d : 0d) +
                                            (true ? 123d : 0d)
                                            ).ToString("F2", CultureInfo.InvariantCulture)) €
                            </span>
                            <hr class="my-1" />
                            <span>
                                @((123d +
                                            ((true) ? 123d : 0d) +
                                            (true ? 123d : 0d)
                                            ).ToString("F2", CultureInfo.InvariantCulture)) €
                            </span>
                        }
                    </div>
                </div>
            }
            """;

        return RunFormattingTestAsync(input: code, htmlFormatted: """
                @if (true)
                {
                <div class="d-flex">
                    <div class="d-flex flex-column" style="text-align: end;">
                        @if (true)
                        {
                        <span>
                            @((((true) ? 123d : 0d) +
                            (true ? 123d : 0d)
                            ).ToString("F2", CultureInfo.InvariantCulture)) €
                        </span>
                        <hr class="my-1" />
                        <span>
                            @((123d +
                            ((true) ? 123d : 0d) +
                            (true ? 123d : 0d)
                            ).ToString("F2", CultureInfo.InvariantCulture)) €
                        </span>
                        }
                    </div>
                </div>
                }
                """,
            expected: code);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/11873")]
    public Task NestedExplicitExpression2()
        => RunFormattingTestAsync(
            input: """
                @if (true)
                {
                    <span>
                        @((((true) ? 123d : 0d) +
                            (true ? 123d : 0d)
                            ).ToString("F2", CultureInfo.InvariantCulture)) €
                    </span>
                    <hr class="my-1" />
                    <span>
                        @((123d +
                            ((true) ? 123d : 0d) +
                            (true ? 123d : 0d)
                            ).ToString("F2", CultureInfo.InvariantCulture)) €
                    </span>
                }
                """,
            htmlFormatted: """
                @if (true)
                {
                <span>
                    @((((true) ? 123d : 0d) +
                    (true ? 123d : 0d)
                    ).ToString("F2", CultureInfo.InvariantCulture)) €
                </span>
                <hr class="my-1" />
                <span>
                    @((123d +
                    ((true) ? 123d : 0d) +
                    (true ? 123d : 0d)
                    ).ToString("F2", CultureInfo.InvariantCulture)) €
                </span>
                }
                """,
            expected: """
                @if (true)
                {
                    <span>
                        @((((true) ? 123d : 0d) +
                            (true ? 123d : 0d)
                            ).ToString("F2", CultureInfo.InvariantCulture)) €
                    </span>
                    <hr class="my-1" />
                    <span>
                        @((123d +
                            ((true) ? 123d : 0d) +
                            (true ? 123d : 0d)
                            ).ToString("F2", CultureInfo.InvariantCulture)) €
                    </span>
                }
                """);

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/11873")]
    public Task NestedExplicitExpression3()
        => RunFormattingTestAsync(
            input: """
                @if (true)
                {
                    <span>
                        @((((true) ? 123d : 0d) +
                            (true ? 123d : 0d)
                            ).ToString("F2", CultureInfo.InvariantCulture)
                        ) €
                    </span>
                    <hr class="my-1" />
                    <span>
                        @((123d +
                            ((true) ? 123d : 0d) +
                            (true ? 123d : 0d)
                            ).ToString("F2", CultureInfo.InvariantCulture)
                        ) €
                    </span>
                }
                """,
            htmlFormatted: """
                @if (true)
                {
                <span>
                    @((((true) ? 123d : 0d) +
                    (true ? 123d : 0d)
                    ).ToString("F2", CultureInfo.InvariantCulture)
                    ) €
                </span>
                <hr class="my-1" />
                <span>
                    @((123d +
                    ((true) ? 123d : 0d) +
                    (true ? 123d : 0d)
                    ).ToString("F2", CultureInfo.InvariantCulture)
                    ) €
                </span>
                }
                """,
            expected: """
                @if (true)
                {
                    <span>
                        @((((true) ? 123d : 0d) +
                            (true ? 123d : 0d)
                            ).ToString("F2", CultureInfo.InvariantCulture)
                        ) €
                    </span>
                    <hr class="my-1" />
                    <span>
                        @((123d +
                            ((true) ? 123d : 0d) +
                            (true ? 123d : 0d)
                            ).ToString("F2", CultureInfo.InvariantCulture)
                        ) €
                    </span>
                }
                """);

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/11873")]
    public Task NestedExplicitExpression4()
        => RunFormattingTestAsync(
            input: """
                @if (true)
                {
                    <span>
                        @((((true) ? 123d : 0d) +
                            (true ? 123d : 0d)
                            ).ToString("F2", CultureInfo.InvariantCulture)
                ) €
                    </span>
                    <hr class="my-1" />
                    <span>
                        @((123d +
                            ((true) ? 123d : 0d) +
                            (true ? 123d : 0d)
                            ).ToString("F2", CultureInfo.InvariantCulture)
                ) €
                    </span>
                }
                """,
            htmlFormatted: """
                @if (true)
                {
                <span>
                    @((((true) ? 123d : 0d) +
                    (true ? 123d : 0d)
                    ).ToString("F2", CultureInfo.InvariantCulture)
                    ) €
                </span>
                <hr class="my-1" />
                <span>
                    @((123d +
                    ((true) ? 123d : 0d) +
                    (true ? 123d : 0d)
                    ).ToString("F2", CultureInfo.InvariantCulture)
                    ) €
                </span>
                }
                """,
            expected: """
                @if (true)
                {
                    <span>
                        @((((true) ? 123d : 0d) +
                            (true ? 123d : 0d)
                            ).ToString("F2", CultureInfo.InvariantCulture)
                ) €
                    </span>
                    <hr class="my-1" />
                    <span>
                        @((123d +
                            ((true) ? 123d : 0d) +
                            (true ? 123d : 0d)
                            ).ToString("F2", CultureInfo.InvariantCulture)
                ) €
                    </span>
                }
                """);

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12445")]
    public Task TypeParameterAttribute()
     => RunFormattingTestAsync(
            input: """
                <div>
                <InputSelect TValue="Guid?">
                </InputSelect>
                </div>
                """,
            htmlFormatted: """
                <div>
                    <InputSelect TValue="Guid?">
                    </InputSelect>
                </div>
                """,
            expected: """
                <div>
                    <InputSelect TValue="Guid?">
                    </InputSelect>
                </div>
                """);

    [Fact]
    public Task HtmlAttributes()
        => RunFormattingTestAsync(
            input: """
                <div class="foo"
                            disabled
                        style="hello"
                  @onclick="foo()">
                <InputSelect @onclick="foo()"
                TValue="Guid?"
                 disabled
                 style="hello">
                 <p></p><a href="#"
                 disabled
                 style="hello"
                @onclick="foo()"/>
                 <br class="a"
                 style="b"
                 disabled>
                 <br />
                </InputSelect>
                </div>
                """,
            htmlFormatted: """
                <div class="foo"
                     disabled
                     style="hello"
                     @onclick="foo()">
                    <InputSelect @onclick="foo()"
                                 TValue="Guid?"
                                 disabled
                                 style="hello">
                        <p></p><a href="#"
                                  disabled
                                  style="hello"
                                  @onclick="foo()" />
                        <br class="a"
                            style="b"
                            disabled>
                        <br />
                    </InputSelect>
                </div>
                """,
            expected: """
                <div class="foo"
                     disabled
                     style="hello"
                     @onclick="foo()">
                    <InputSelect @onclick="foo()"
                                 TValue="Guid?"
                                 disabled
                                 style="hello">
                        <p></p><a href="#"
                                  disabled
                                  style="hello"
                                  @onclick="foo()" />
                        <br class="a"
                            style="b"
                            disabled>
                        <br />
                    </InputSelect>
                </div>
                """);

    [Fact]
    public Task HtmlAttributes_FirstAttributeOnNextLine()
        => RunFormattingTestAsync(
            input: """
                <div
                  class="foo"
                  disabled
                  style="hello"
                  @onclick="foo()">
                </div>
                """,
            htmlFormatted: """
                <div class="foo"
                     disabled
                     style="hello"
                     @onclick="foo()">
                </div>
                """,
            expected: """
                <div class="foo"
                     disabled
                     style="hello"
                     @onclick="foo()">
                </div>
                """);

    [Fact]
    public Task HtmlAttributes_IndentByOne()
        => RunFormattingTestAsync(
            input: """
                <div class="foo"
                            disabled
                        style="hello"
                  @onclick="foo()">
                <InputSelect @onclick="foo()"
                TValue="Guid?"
                 disabled
                 style="hello">
                 <p></p><a href="#"
                 disabled
                 style="hello"
                @onclick="foo()"/>
                 <br class="a"
                 style="b"
                 disabled>
                 <br />
                </InputSelect>
                </div>
                """,
            htmlFormatted: """
                <div class="foo"
                     disabled
                     style="hello"
                     @onclick="foo()">
                    <InputSelect @onclick="foo()"
                                 TValue="Guid?"
                                 disabled
                                 style="hello">
                        <p></p><a href="#"
                                  disabled
                                  style="hello"
                                  @onclick="foo()" />
                        <br class="a"
                            style="b"
                            disabled>
                        <br />
                    </InputSelect>
                </div>
                """,
            expected: """
                <div class="foo"
                    disabled
                    style="hello"
                    @onclick="foo()">
                    <InputSelect @onclick="foo()"
                        TValue="Guid?"
                        disabled
                        style="hello">
                        <p></p><a href="#"
                            disabled
                            style="hello"
                            @onclick="foo()" />
                        <br class="a"
                            style="b"
                            disabled>
                        <br />
                    </InputSelect>
                </div>
                """,
            attributeIndentStyle: AttributeIndentStyle.IndentByOne);

    [Fact]
    public Task HtmlAttributes_IndentByTwo()
        => RunFormattingTestAsync(
            input: """
                <div class="foo"
                            disabled
                        style="hello"
                  @onclick="foo()">
                <InputSelect @onclick="foo()"
                TValue="Guid?"
                 disabled
                 style="hello">
                 <p></p><a href="#"
                 disabled
                 style="hello"
                @onclick="foo()"/>
                 <br class="a"
                 style="b"
                 disabled>
                 <br />
                </InputSelect>
                </div>
                """,
            htmlFormatted: """
                <div class="foo"
                     disabled
                     style="hello"
                     @onclick="foo()">
                    <InputSelect @onclick="foo()"
                                 TValue="Guid?"
                                 disabled
                                 style="hello">
                        <p></p><a href="#"
                                  disabled
                                  style="hello"
                                  @onclick="foo()" />
                        <br class="a"
                            style="b"
                            disabled>
                        <br />
                    </InputSelect>
                </div>
                """,
            expected: """
                <div class="foo"
                        disabled
                        style="hello"
                        @onclick="foo()">
                    <InputSelect @onclick="foo()"
                            TValue="Guid?"
                            disabled
                            style="hello">
                        <p></p><a href="#"
                                disabled
                                style="hello"
                                @onclick="foo()" />
                        <br class="a"
                                style="b"
                                disabled>
                        <br />
                    </InputSelect>
                </div>
                """,
            attributeIndentStyle: AttributeIndentStyle.IndentByTwo);

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12223")]
    public Task ExplicitExpression_InIf()
        => RunFormattingTestAsync(
            input: """
                @if (true)
                {
                    @(Html.Grid()
                        .Render())
                }
                """,
            htmlFormatted: """
                @if (true)
                {
                    @(Html.Grid()
                        .Render())
                }
                """,
            expected: """
                @if (true)
                {
                    @(Html.Grid()
                        .Render())
                }
                """);

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12554")]
    public Task ObjectInitializers1()
        => RunFormattingTestAsync(
            input: """
                @{
                    Func<Test, IHtmlContent> RenderTest = @<div>Test X: @item.X, Y: @item.Y</div>;
                }

                @RenderTest(new Test()
                {
                    X = 10,
                    Y = 20,
                })

                <div>
                    @RenderTest(new Test()
                    {
                        X = 1,
                        Y = 2,
                    })
                </div>
                """,
            htmlFormatted: """
                @{
                    Func<Test, IHtmlContent> RenderTest = @<div>Test X: @item.X, Y: @item.Y</div>;
                }
                
                @RenderTest(new Test()
                {
                    X = 10,
                    Y = 20,
                })
                
                <div>
                    @RenderTest(new Test()
                    {
                    X = 1,
                    Y = 2,
                    })
                </div>
                """,
            expected: """
                @{
                    Func<Test, IHtmlContent> RenderTest = @<div>Test X: @item.X, Y: @item.Y</div>;
                }

                @RenderTest(new Test()
                {
                    X = 10,
                    Y = 20,
                })

                <div>
                    @RenderTest(new Test()
                    {
                        X = 1,
                        Y = 2,
                    })
                </div>
                """);

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12554")]
    public Task ObjectInitializers2()
        => RunFormattingTestAsync(
            input: """
                <div>
                    @if (true)
                    {
                        @Html.TextBox(new Test()
                        {
                            test = 5
                        })
                    }
                </div>
                """,
            htmlFormatted: """
                <div>
                    @if (true)
                    {
                    @Html.TextBox(new Test()
                    {
                    test = 5
                    })
                    }
                </div>
                """,
            expected: """
                <div>
                    @if (true)
                    {
                        @Html.TextBox(new Test()
                        {
                            test = 5
                        })
                    }
                </div>
                """);

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12554")]
    public Task ObjectInitializers3()
        => RunFormattingTestAsync(
            input: """
                <div>
                    @{
                        var a = new int[] { 1, 2, 3 }
                            .Where(i => i % 2 == 0)
                            .ToArray();
                    }
                </div>
                """,
            htmlFormatted: """
                <div>
                    @{
                    var a = new int[] { 1, 2, 3 }
                    .Where(i => i % 2 == 0)
                    .ToArray();
                    }
                </div>
                """,
            expected: """
                <div>
                    @{
                        var a = new int[] { 1, 2, 3 }
                            .Where(i => i % 2 == 0)
                            .ToArray();
                    }
                </div>
                """);

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12622")]
    public Task ObjectInitializers4()
        => RunFormattingTestAsync(
            input: """
                <div>
                    @if (true)
                    {
                        @Html.TextBox(new Test()
                        {
                            test = 5
                        })
                        <div></div>
                    }
                </div>
                """,
            htmlFormatted: """
                <div>
                    @if (true)
                    {
                    @Html.TextBox(new Test()
                    {
                    test = 5
                    })
                    <div></div>
                    }
                </div>
                """,
            expected: """
                <div>
                    @if (true)
                    {
                        @Html.TextBox(new Test()
                        {
                            test = 5
                        })
                        <div></div>
                    }
                </div>
                """);

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12622")]
    public Task ObjectInitializers5()
        => RunFormattingTestAsync(
            input: """
                <div>
                    @if (true)
                    {
                        @Html.TextBox(new Test() { test = 5 })
                        <div></div>
                    }
                </div>
                """,
            htmlFormatted: """
                <div>
                    @if (true)
                    {
                    @Html.TextBox(new Test() { test = 5 })
                    <div></div>
                    }
                </div>
                """,
            expected: """
                <div>
                    @if (true)
                    {
                        @Html.TextBox(new Test() { test = 5 })
                        <div></div>
                    }
                </div>
                """);

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12622")]
    public Task ObjectInitializers6()
        => RunFormattingTestAsync(
            input: """
                @if (true)
                {
                    @Html.TextBox(new Test()
                    {
                        test = 5
                    })
                    <div></div>
                }
                """,
            htmlFormatted: """
                @if (true)
                {
                    @Html.TextBox(new Test()
                    {
                        test = 5
                    })
                <div></div>
                }
                """,
            expected: """
                @if (true)
                {
                    @Html.TextBox(new Test()
                    {
                        test = 5
                    })
                    <div></div>
                }
                """);

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12622")]
    public Task ObjectInitializers7()
        => RunFormattingTestAsync(
            input: """
                <div>
                    <div>
                        @Html.TextBox(new 
                        {
                            test = 5,
                        })
                    </div>
                    <div>
                        @Html.TextBox(new 
                        {
                            test = 5,
                        })
                    </div>
                </div>
                """,
            htmlFormatted: """
                <div>
                    <div>
                        @Html.TextBox(new
                        {
                        test = 5,
                        })
                    </div>
                    <div>
                        @Html.TextBox(new
                        {
                        test = 5,
                        })
                    </div>
                </div>
                """,
            expected: """
                <div>
                    <div>
                        @Html.TextBox(new
                        {
                            test = 5,
                        })
                    </div>
                    <div>
                        @Html.TextBox(new
                        {
                            test = 5,
                        })
                    </div>
                </div>
                """);

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12622")]
    public Task ObjectInitializers8()
        => RunFormattingTestAsync(
            input: """
                @if (true)
                {
                    @Html.TextBox(new Test() {
                        test = 5
                    })
                    <div></div>
                }
                """,
            htmlFormatted: """
                @if (true)
                {
                    @Html.TextBox(new Test() {
                        test = 5
                    })
                <div></div>
                }
                """,
            expected: """
                @if (true)
                {
                    @Html.TextBox(new Test()
                    {
                        test = 5
                    })
                    <div></div>
                }
                """);

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12622")]
    public Task ObjectInitializers9()
        => RunFormattingTestAsync(
            input: """
                @if (true)
                {
                    @Html.TextBox(new Test() {
                        test = 5
                    })
                    <div></div>
                }
                """,
            htmlFormatted: """
                @if (true)
                {
                    @Html.TextBox(new Test() {
                        test = 5
                    })
                <div></div>
                }
                """,
            expected: """
                @if (true)
                {
                    @Html.TextBox(new Test() {
                        test = 5
                    })
                    <div></div>
                }
                """,
            csharpSyntaxFormattingOptions: RazorCSharpSyntaxFormattingOptions.Default with
            {
                NewLines = RazorCSharpSyntaxFormattingOptions.Default.NewLines & ~RazorNewLinePlacement.BeforeOpenBraceInObjectCollectionArrayInitializers
            });

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12622")]
    public Task ObjectInitializers10()
        => RunFormattingTestAsync(
            input: """
                    @if (true)
                {
                    @Html.TextBox(new Test()
                    {
                        test = 5
                    })
                    <div></div>
                }
                """,
            htmlFormatted: """
                    @if (true)
                {
                    @Html.TextBox(new Test()
                    {
                        test = 5
                    })
                <div></div>
                }
                """,
            expected: """
                @if (true)
                {
                    @Html.TextBox(new Test() {
                        test = 5
                    })
                    <div></div>
                }
                """,
            csharpSyntaxFormattingOptions: RazorCSharpSyntaxFormattingOptions.Default with
            {
                NewLines = RazorCSharpSyntaxFormattingOptions.Default.NewLines & ~RazorNewLinePlacement.BeforeOpenBraceInObjectCollectionArrayInitializers
            });

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12622")]
    public Task ObjectInitializers11()
        => RunFormattingTestAsync(
            input: """
                <div>
                    <div>
                        <div>
                            @if (true)
                            {
                                <div>
                                    @Html.TextBox(new
                                    {
                                        test = 6
                                    })
                                </div>

                                <div></div>
                            }
                        </div>
                    </div>
                </div>
                """,
            htmlFormatted: """
                <div>
                    <div>
                        <div>
                            @if (true)
                            {
                            <div>
                                @Html.TextBox(new
                                {
                                test = 6
                                })
                            </div>
                
                            <div></div>
                            }
                        </div>
                    </div>
                </div>
                """,
            expected: """
                <div>
                    <div>
                        <div>
                            @if (true)
                            {
                                <div>
                                    @Html.TextBox(new
                                    {
                                        test = 6
                                    })
                                </div>

                                <div></div>
                            }
                        </div>
                    </div>
                </div>
                """);

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12631")]
    public Task ObjectInitializers12()
        => RunFormattingTestAsync(
            input: """
                @await Component.InvokeAsync("ReviewAndPublishModal", 
                    new { 
                        id = "ReviewPublishModal", 
                        title = "Review and publish",
                        text = Model.ReviewNotes, 
                        state = Model.State, 
                        allowSave = allowSaveReview, 
                        allowPublish = allowPublish, 
                        isPublished =isCurrentPublished     
                    }
                )
                """,
            htmlFormatted: """
                @await Component.InvokeAsync("ReviewAndPublishModal",
                    new {
                        id = "ReviewPublishModal",
                        title = "Review and publish",
                        text = Model.ReviewNotes,
                        state = Model.State,
                        allowSave = allowSaveReview,
                        allowPublish = allowPublish,
                        isPublished =isCurrentPublished
                    }
                )
                """,
            expected: """
                @await Component.InvokeAsync("ReviewAndPublishModal",
                    new
                    {
                        id = "ReviewPublishModal",
                        title = "Review and publish",
                        text = Model.ReviewNotes,
                        state = Model.State,
                        allowSave = allowSaveReview,
                        allowPublish = allowPublish,
                        isPublished = isCurrentPublished
                    }
                )
                """);

    [Fact]
    public Task PartialDocument()
        => RunFormattingTestAsync(
            input: """
                <table>
                <tr>
                <td>
                """,
            htmlFormatted: """
                <table>
                    <tr>
                        <td>

                """,
            expected: """
                <table>
                    <tr>
                        <td>
                """,
            allowDiagnostics: true);

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12807")]
    public Task TernaryInAttribute()
        => RunFormattingTestAsync(
            input: """
                <Icon Name="@(expanded?ParentDataGrid.SelfReferenceCollapseIcon:ParentDataGrid.SelfReferenceExpandIcon)"/>
                """,
            htmlFormatted: """
                <Icon Name="@(expanded?ParentDataGrid.SelfReferenceCollapseIcon:ParentDataGrid.SelfReferenceExpandIcon)" />
                """,
            expected: """
                <Icon Name="@(expanded ? ParentDataGrid.SelfReferenceCollapseIcon : ParentDataGrid.SelfReferenceExpandIcon)" />
                """,
            allowDiagnostics: true);

    [Fact]
    public Task CSSWrappedToMultipleLines()
        => RunFormattingTestAsync(
            input: """
                @using System

                <style>
                    /* Card header row */
                    .ipam-card-header-row { display: flex; align-items: center; justify-content: space-between; width: 100%; }
                    .ipam-card-header-row h3 { margin: 0; }
                </style>
                """,
            htmlFormatted: """
                @using System

                <style>
                    /* Card header row */
                    .ipam-card-header-row {
                        display: flex;
                        align-items: center;
                        justify-content: space-between;
                        width: 100%;
                    }

                        .ipam-card-header-row h3 {
                            margin: 0;
                        }
                </style>
                """,
            expected: """
                @using System

                <style>
                    /* Card header row */
                    .ipam-card-header-row {
                        display: flex;
                        align-items: center;
                        justify-content: space-between;
                        width: 100%;
                    }
                
                        .ipam-card-header-row h3 {
                            margin: 0;
                        }
                </style>
                """,
            validateHtmlFormattedMatchesWebTools: false);

    [Fact]
    public Task CSSWrappedToMultipleLines_WithBlankLines()
        => RunFormattingTestAsync(
            input: """
                @using System

                <style>
                    /* Card header row */
                    .ipam-card-header-row { display: flex; align-items: center; justify-content: space-between; width: 100%; }

                    .ipam-card-header-row h3 { margin: 0; }
                </style>
                """,
            htmlFormatted: """
                @using System

                <style>
                    /* Card header row */
                    .ipam-card-header-row {
                        display: flex;
                        align-items: center;
                        justify-content: space-between;
                        width: 100%;
                    }

                        .ipam-card-header-row h3 {
                            margin: 0;
                        }
                </style>
                """,
            expected: """
                @using System

                <style>
                    /* Card header row */
                    .ipam-card-header-row {
                        display: flex;
                        align-items: center;
                        justify-content: space-between;
                        width: 100%;
                    }
                
                        .ipam-card-header-row h3 {
                            margin: 0;
                        }
                </style>
                """,
            validateHtmlFormattedMatchesWebTools: false);

    [Theory]
    [CombinatorialData]
    public Task ScriptTagTagHelper(bool scriptTagHelper)
    {
        var directive = scriptTagHelper
            ? "@addTagHelper *, SomeProject"
            : "";
        return RunFormattingTestAsync(
                input: $$"""
                    {{directive}}

                    <script type="text/javascript">
                        $(document).ready(function () {
                            $('.dropdown-item').click(function (e) {
                                e.preventDefault();
                                var url = $(this).attr('href');
                                $.ajax({
                                    url: url,
                                                method: 'POST',
                                    success: function (response) {
                                        alert('Content published successfully!');
                                    },
                                    error: function (xhr, status, error) {
                                        alert('Error publishing content: ' + error);
                                    }
                                });
                            });
                        });
                    </script>
                    """,
                htmlFormatted: $$"""
                    {{directive}}

                    <script type="text/javascript">
                        $(document).ready(function () {
                            $('.dropdown-item').click(function (e) {
                                e.preventDefault();
                                var url = $(this).attr('href');
                                $.ajax({
                                    url: url,
                                    method: 'POST',
                                    success: function (response) {
                                        alert('Content published successfully!');
                                    },
                                    error: function (xhr, status, error) {
                                        alert('Error publishing content: ' + error);
                                    }
                                });
                            });
                        });
                    </script>
                    """,
                expected: $$"""
                    {{directive}}

                    <script type="text/javascript">
                        $(document).ready(function () {
                            $('.dropdown-item').click(function (e) {
                                e.preventDefault();
                                var url = $(this).attr('href');
                                $.ajax({
                                    url: url,
                                    method: 'POST',
                                    success: function (response) {
                                        alert('Content published successfully!');
                                    },
                                    error: function (xhr, status, error) {
                                        alert('Error publishing content: ' + error);
                                    }
                                });
                            });
                        });
                    </script>
                    """,
                validateHtmlFormattedMatchesWebTools: false, // We don't have JS formatting in tests, so the method param wouldn't really move
                fileKind: RazorFileKind.Legacy,
                additionalFiles:
                [
                    (FilePath("ScriptTagHelper.cs"), """
                        using Microsoft.AspNetCore.Razor.TagHelpers;

                        [HtmlTargetElement("script")]
                        public class ScriptTagHelper : TagHelper
                        {
                        }
                        """)
                ]);
    }

    [Theory]
    [CombinatorialData]
    public Task VoidTagTagHelper(bool useTagHelper)
    {
        var directive = useTagHelper
            ? "@addTagHelper *, SomeProject"
            : "";
        return RunFormattingTestAsync(
                input: $$"""
                    {{directive}}

                    <div>
                        <input type="text">
                        This shouldn't be indented.

                        <input type="text" @bind="Value">
                        Neither should this
                    </div>
                    """,
                htmlFormatted: $$"""
                    {{directive}}

                    <div>
                        <input type="text">
                        This shouldn't be indented.
                    
                        <input type="text" @bind="Value">
                        Neither should this
                    </div>
                    """,
                expected: $$"""
                    {{directive}}

                    <div>
                        <input type="text">
                        This shouldn't be indented.
                    
                        <input type="text" @bind="Value">
                        Neither should this
                    </div>
                    """,
                validateHtmlFormattedMatchesWebTools: false,
                fileKind: RazorFileKind.Legacy,
                additionalFiles:
                [
                    (FilePath("InputTagHelper.cs"), """
                        using Microsoft.AspNetCore.Razor.TagHelpers;

                        [HtmlTargetElement("input")]
                        public class InputTagHelper : TagHelper
                        {
                        }
                        """)
                ]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12952")]
    public async Task RazorCommentClosingWithHtmlOnSameLine()
    {
        await RunFormattingTestAsync(
            input: """
                <div class="table-container">
                    <table>
                        <thead>
                @*
                            <tr class="row-1">
                                <th>Group A</th>
                            </tr>
                 *@         <tr>
                                <th>ID</th>
                            </tr>
                        </thead>
                    </table>
                </div>
                """,
            htmlFormatted: """
                <div class="table-container">
                    <table>
                        <thead>
                            @*
                            <tr class="row-1">
                            <th>Group A</th>
                            </tr>
                            *@
                            <tr>
                                <th>ID</th>
                            </tr>
                        </thead>
                    </table>
                </div>
                """,
            expected: """
                <div class="table-container">
                    <table>
                        <thead>
                            @*
                            <tr class="row-1">
                                <th>Group A</th>
                            </tr>
                 *@
                            <tr>
                                <th>ID</th>
                            </tr>
                        </thead>
                    </table>
                </div>
                """);
    }

    private static RazorCSharpSyntaxFormattingOptions GetNewLineBeforeBraceInLambdaExpressionOptions(bool newLineBeforeBraceInLambda)
        => RazorCSharpSyntaxFormattingOptions.Default with
        {
            NewLines = newLineBeforeBraceInLambda
                ? RazorCSharpSyntaxFormattingOptions.Default.NewLines | RazorNewLinePlacement.BeforeOpenBraceInLambdaExpressionBody
                : RazorCSharpSyntaxFormattingOptions.Default.NewLines & ~RazorNewLinePlacement.BeforeOpenBraceInLambdaExpressionBody
        };
}
