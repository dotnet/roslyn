// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
#if !VSCODE
using System.Linq;
#endif
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

using static HoverAssertions;

public class CohostHoverEndpointTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task Razor()
    {
        TestCode code = """
            <[|PageTi$$tle|]></PageTitle>
            <div></div>
            
            @{
                var myVariable = "Hello";
            
                var length = myVariable.Length;
            }
            """;

        await VerifyHoverAsync(code, async (hover, document) =>
        {
            await VerifyRangeAsync(hover, code.Span, document);

            hover.VerifyContents(
                Container(
                    Container(
                        Image,
                        ClassifiedText( // class Microsoft.AspNetCore.Components.Web.PageTitle
                            Keyword("class"),
                            WhiteSpace(" "),
                            Namespace("Microsoft"),
                            Punctuation("."),
                            Namespace("AspNetCore"),
                            Punctuation("."),
                            Namespace("Components"),
                            Punctuation("."),
                            Namespace("Web"),
                            Punctuation("."),
                            ClassName("PageTitle")))));
        });
    }

    [Fact]
    public async Task Html()
    {
        TestCode code = """
            <PageTitle></PageTitle>
            <div$$></div>
            
            @{
                var myVariable = "Hello";
            
                var length = myVariable.Length;
            }
            """;

        // This simply verifies that Hover will call into HTML.
        var htmlResponse = new VSInternalHover();

        await VerifyHoverAsync(code, htmlResponse, h => Assert.Same(htmlResponse, h));
    }

    [Fact]
    public async Task Html_TagHelper()
    {
        TestCode code = """
            <[|bo$$dy|]></body>
            """;

        // This verifies Hover calls into both razor and HTML, aggregating their results
        const string BodyDescription = "body description";
        var htmlResponse = new VSInternalHover
        {
            Range = new LspRange()
            {
                Start = new Position(0, 1),
                End = new Position(0, "<body".Length),
            },
            Contents = new MarkupContent()
            {
                Kind = MarkupKind.Markdown,
                Value = BodyDescription,
            }
        };

        await VerifyHoverAsync(code, RazorFileKind.Legacy, htmlResponse, async (hover, document) =>
        {
            await VerifyRangeAsync(hover, code.Span, document);

            hover.VerifyContents(
                Container(
                    Container(
                        ClassifiedText(
                            Text(BodyDescription)),
                        HorizontalRule),
                    Container(
                        Image,
                        ClassifiedText(
                            Text("Microsoft"),
                            Punctuation("."),
                            Text("AspNetCore"),
                            Punctuation("."),
                            Text("Mvc"),
                            Punctuation("."),
                            Text("Razor"),
                            Punctuation("."),
                            Text("TagHelpers"),
                            Punctuation("."),
                            Type("BodyTagHelper")))));
        });
    }

    [Fact]
    public async Task Html_EndTag()
    {
        TestCode code = """
            <PageTitle></PageTitle>
            <div></d$$iv>
            
            @{
                var myVariable = "Hello";
            
                var length = myVariable.Length;
            }
            """;

        // This simply verifies that Hover will call into HTML.
        var htmlResponse = new VSInternalHover();

        await VerifyHoverAsync(code, htmlResponse, h => Assert.Same(htmlResponse, h));
    }

    [ConditionalFact(typeof(IsEnglishLocal))]
    public async Task CSharp_Impl()
    {
        TestCode code = """
            <PageTitle></PageTitle>
            <div></div>

            @{
                var $$[|myVariable|] = "Hello";

                var length = myVariable.Length;
            }
            """;

        await VerifyHoverAsync(code, async (hover, document) =>
        {
            await VerifyRangeAsync(hover, code.Span, document);

            hover.VerifyContents(
                Container(
                    Container(
                        Image,
                        ClassifiedText( // (local variable) string myVariable
                            Punctuation("("),
                            Text("local variable"),
                            Punctuation(")"),
                            WhiteSpace(" "),
                            Keyword("string"),
                            WhiteSpace(" "),
                            LocalName("myVariable")))));
        });
    }

    [ConditionalFact(typeof(IsEnglishLocal))]
    public async Task CSharp_Decl()
    {
        TestCode code = """
            <PageTitle></PageTitle>
            <div></div>

            @code
            {
                public void M()
                {
                    var $$[|myVariable|] = "Hello";
    
                    var length = myVariable.Length;
                }
            }
            """;

        await VerifyHoverAsync(code, async (hover, document) =>
        {
            await VerifyRangeAsync(hover, code.Span, document);

            hover.VerifyContents(
                Container(
                    Container(
                        Image,
                        ClassifiedText( // (local variable) string myVariable
                            Punctuation("("),
                            Text("local variable"),
                            Punctuation(")"),
                            WhiteSpace(" "),
                            Keyword("string"),
                            WhiteSpace(" "),
                            LocalName("myVariable")))));
        });
    }

    [ConditionalFact(typeof(IsEnglishLocal))]
    public async Task CSharp_Legacy()
    {
        TestCode code = """
            <PageTitle></PageTitle>
            <div></div>

            @functions
            {
                public void M()
                {
                    var $$[|myVariable|] = "Hello";
    
                    var length = myVariable.Length;
                }
            }
            """;

        await VerifyHoverAsync(code, fileKind: RazorFileKind.Legacy, htmlResponse: null, async (hover, document) =>
        {
            await VerifyRangeAsync(hover, code.Span, document);

            hover.VerifyContents(
                Container(
                    Container(
                        Image,
                        ClassifiedText( // (local variable) string myVariable
                            Punctuation("("),
                            Text("local variable"),
                            Punctuation(")"),
                            WhiteSpace(" "),
                            Keyword("string"),
                            WhiteSpace(" "),
                            LocalName("myVariable")))));
        });
    }

    [Fact]
    public async Task ComponentAttribute()
    {
        // Component attributes are within HTML but actually map to C#.
        // In this situation, Hover prefers treating the position as C# and calls
        // Roslyn rather than HTML.

        TestCode code = """
            <EditForm [|Form$$Name|]="Hello" />
            """;

        await VerifyHoverAsync(code, async (hover, document) =>
        {
            await VerifyRangeAsync(hover, code.Span, document);

            hover.VerifyContents(
                Container(
                    Container(
                        Image,
                        ClassifiedText( // string? EditForm.FormName { get; set; }
                            Keyword("string"),
                            Punctuation("?"),
                            WhiteSpace(" "),
                            ClassName("EditForm"),
                            Punctuation("."),
                            PropertyName("FormName"),
                            WhiteSpace(" "),
                            Punctuation("{"),
                            WhiteSpace(" "),
                            Keyword("get"),
                            Punctuation(";"),
                            WhiteSpace(" "),
                            Keyword("set"),
                            Punctuation(";"),
                            WhiteSpace(" "),
                            Punctuation("}")))));
        });
    }

    [Fact]
    public async Task Component_WithCallbacks()
    {
        TestCode code = """
            <[|Inpu$$tText|] ValueChanged="Foo"
                       DisplayName="Foo"
                       @onchange="Foo"
                       @onfocus="Foo"
                       @onblur="Foo" />

            @code {
                private void Foo()
                {
                }
            }
            """;

        await VerifyHoverAsync(code, async (hover, document) =>
        {
            await VerifyRangeAsync(hover, code.Span, document);

            hover.VerifyContents(
                Container(
                    Container(
                        Image,
                        ClassifiedText( // class Microsoft.ApsNetCore.Components.Forms.InputText
                            Keyword("class"),
                            WhiteSpace(" "),
                            Namespace("Microsoft"),
                            Punctuation("."),
                            Namespace("AspNetCore"),
                            Punctuation("."),
                            Namespace("Components"),
                            Punctuation("."),
                            Namespace("Forms"),
                            Punctuation("."),
                            ClassName("InputText")))));
        });
    }

    [Fact]
    public async Task ComponentEndTag()
    {
        TestCode code = """
            <PageTitle></[|Pa$$geTitle|]>
            <div></div>
            
            @{
                var myVariable = "Hello";
            
                var length = myVariable.Length;
            }
            """;

        await VerifyHoverAsync(code, async (hover, document) =>
        {
            await VerifyRangeAsync(hover, code.Span, document);

            hover.VerifyContents(
                Container(
                    Container(
                        Image,
                        ClassifiedText( // class Microsoft.AspNetCore.Components.Web.PageTitle
                            Keyword("class"),
                            WhiteSpace(" "),
                            Namespace("Microsoft"),
                            Punctuation("."),
                            Namespace("AspNetCore"),
                            Punctuation("."),
                            Namespace("Components"),
                            Punctuation("."),
                            Namespace("Web"),
                            Punctuation("."),
                            ClassName("PageTitle")))));
        });
    }

    [Fact]
    public async Task ComponentEndTag_FullyQualified()
    {
        TestCode code = """
            <Microsoft.AspNetCore.Components.Web.PageTitle></Microsoft.AspNetCore.Components.Web.[|Pa$$geTitle|]>
            <div></div>
            
            @{
                var myVariable = "Hello";
            
                var length = myVariable.Length;
            }
            """;

        await VerifyHoverAsync(code, async (hover, document) =>
        {
            await VerifyRangeAsync(hover, code.Span, document);

            hover.VerifyContents(
                Container(
                    Container(
                        Image,
                        ClassifiedText( // class Microsoft.AspNetCore.Components.Web.PageTitle
                            Keyword("class"),
                            WhiteSpace(" "),
                            Namespace("Microsoft"),
                            Punctuation("."),
                            Namespace("AspNetCore"),
                            Punctuation("."),
                            Namespace("Components"),
                            Punctuation("."),
                            Namespace("Web"),
                            Punctuation("."),
                            ClassName("PageTitle")))));
        });
    }

    [Fact]
    public async Task ComponentEndTag_FullyQualified_Namespace()
    {
        TestCode code = """
            <Microsoft.AspNetCore.Components.Web.PageTitle></Microsoft.[|AspNe$$tCore|].Components.Web.PageTitle>
            <div></div>
            
            @{
                var myVariable = "Hello";
            
                var length = myVariable.Length;
            }
            """;

        await VerifyHoverAsync(code, async (hover, document) =>
        {
            await VerifyRangeAsync(hover, code.Span, document);

            hover.VerifyContents(
                Container(
                    Container(
                        Image,
                        ClassifiedText( // namespace Microsoft.AspNetCore
                            Keyword("namespace"),
                            WhiteSpace(" "),
                            Namespace("Microsoft"),
                            Punctuation("."),
                            Namespace("AspNetCore")))));
        });
    }

    [Fact]
    public async Task BindFallbackDocumentation()
    {
        TestCode code = """
            <my-element [|@bind-fo$$o|]="value" @bind-foo:event="onchange"/>

            @code {
                private string value = "initial";
            }
            """;

        await VerifyHoverAsync(code, async (hover, document) =>
        {
            await VerifyRangeAsync(hover, code.Span, document);

#if VSCODE
            hover.VerifyContents(
                """
                object Bind.Bind

                Binds the provided expression to an attribute and a change event, based on the naming of the bind attribute. For example:

                @bind-value="..."

                and

                @bind-value:event="onchange"

                will assign the current value of the expression to the 'value' attribute, and assign a delegate that attempts to set the value to the 'onchange' attribute.
                """);
#else
            hover.VerifyContents(
                Container(
                    Container(
                        Image,
                        ClassifiedText(
                            Keyword("object"),
                            WhiteSpace(" "),
                            Text("Microsoft"),
                            Punctuation("."),
                            Text("AspNetCore"),
                            Punctuation("."),
                            Text("Components"),
                            Punctuation("."),
                            Type("Bind"),
                            Punctuation("."),
                            Run("Bind", Microsoft.CodeAnalysis.Classification.ClassificationTypeNames.Identifier))),
                    Container(o =>
                    {
                        var classifiedText = Assert.IsType<Roslyn.Text.Adornments.ClassifiedTextElement>(o);
                        var summaryText = string.Concat(classifiedText.Runs.Select(static run => run.Text));

                        Assert.Contains("For example:", summaryText);
                        Assert.Contains("@bind-value=\"...\"", summaryText);
                        Assert.Contains("@bind-value:event=\"onchange\"", summaryText);
                        Assert.Contains("will assign the current value of the expression", summaryText);
                        Assert.Contains(classifiedText.Runs, static run =>
                            run.Text == "@bind-value=\"...\"" &&
                            run.Style.HasFlag(Roslyn.Text.Adornments.ClassifiedTextRunStyle.UseClassificationFont));
                        Assert.Contains(classifiedText.Runs, static run =>
                            run.Text == "@bind-value:event=\"onchange\"" &&
                            run.Style.HasFlag(Roslyn.Text.Adornments.ClassifiedTextRunStyle.UseClassificationFont));
                    })));
#endif
        });
    }

#if VSCODE
    [Fact]
    public async Task BindFallbackDocumentation_Markdown()
    {
        UpdateClientLSPInitializationOptions(options =>
        {
            options.ClientCapabilities.TextDocument!.Hover = new HoverSetting
            {
                ContentFormat = [MarkupKind.Markdown],
            };
            return options;
        });

        TestCode code = """
            <my-element [|@bind-fo$$o|]="value" @bind-foo:event="onchange"/>

            @code {
                private string value = "initial";
            }
            """;

        await VerifyHoverAsync(code, async (hover, document) =>
        {
            await VerifyRangeAsync(hover, code.Span, document);

            var markup = hover.Contents.Fourth;
            Assert.Equal(MarkupKind.Markdown, markup.Kind);
            Assert.Contains("**object** Bind.**Bind**", markup.Value);
            Assert.Contains("Binds the provided expression to an attribute", markup.Value);
            Assert.Contains($"```text{Environment.NewLine}@bind-value=\"...\"{Environment.NewLine}```", markup.Value);
            Assert.Contains($"```text{Environment.NewLine}@bind-value:event=\"onchange\"{Environment.NewLine}```", markup.Value);
        });
    }
#endif

    private Task VerifyHoverAsync(TestCode input, Func<Hover, TextDocument, Task> verifyHover)
        => VerifyHoverAsync(input, fileKind: null, htmlResponse: null, verifyHover);

    private async Task VerifyHoverAsync(TestCode input, RazorFileKind? fileKind, Hover? htmlResponse, Func<Hover, TextDocument, Task> verifyHover)
    {
        var document = CreateProjectAndRazorDocument(input.Text, fileKind);
        var result = await GetHoverResultAsync(document, input, htmlResponse);

        Assert.NotNull(result);
        await verifyHover(result, document);
    }

    private async Task VerifyHoverAsync(TestCode input, Hover htmlResponse, Action<Hover?> verifyHover)
    {
        var document = CreateProjectAndRazorDocument(input.Text);
        var result = await GetHoverResultAsync(document, input, htmlResponse);

        Assert.NotNull(result);
        verifyHover(result);
    }

    private async Task<Hover?> GetHoverResultAsync(TextDocument document, TestCode input, Hover? htmlResponse = null)
    {
        var inputText = await document.GetTextAsync(DisposalToken);
        var linePosition = inputText.GetLinePosition(input.Position);

        var requestInvoker = new TestHtmlRequestInvoker([(Methods.TextDocumentHoverName, htmlResponse)]);
        var endpoint = new CohostHoverEndpoint(IncompatibleProjectService, RemoteServiceInvoker, requestInvoker);

        var textDocumentPositionParams = new TextDocumentPositionParams
        {
            Position = LspFactory.CreatePosition(linePosition),
            TextDocument = new TextDocumentIdentifier { DocumentUri = document.GetURI() },
        };

        return await endpoint.GetTestAccessor().HandleRequestAsync(textDocumentPositionParams, document, DisposalToken);
    }

    private static async Task VerifyRangeAsync(Hover hover, TextSpan expected, TextDocument document)
    {
        var text = await document.GetTextAsync();
        Assert.NotNull(hover.Range);
        Assert.Equal(text.GetLinePositionSpan(expected), hover.Range.ToLinePositionSpan());
    }
}
