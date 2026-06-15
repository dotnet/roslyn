// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public class CohostPrepareRenameEndpointTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public Task CSharp_Method()
        => VerifyPrepareRenameAsync(
            input: """
                @code
                {
                    void [|M$$ethod|]()
                    {
                    }
                }
                """);

    [Fact]
    public Task CSharp_StringLiteral_ReturnsNull()
        => VerifyPrepareRenameAsync(
            input: """
                @code
                {
                    void M()
                    {
                        var x = "he$$llo";
                    }
                }
                """);

    [Fact]
    public Task CSharp_ExplicitStatement_Local()
        => VerifyPrepareRenameAsync(
            input: """
                @{
                    var hello = 1;
                    [|he$$llo|]++;
                }
                """);

    [Fact]
    public Task CSharp_ImplicitExpression_Field()
        => VerifyPrepareRenameAsync(
            input: """
                <div>@[|Tit$$le|]</div>

                @code
                {
                    private string Title = "hello";
                }
                """);

    [Fact]
    public Task Component_SelfClosingTag()
        => VerifyPrepareRenameAsync(
            input: """
                <[|Com$$ponent|] />
                """,
            additionalFiles:
            [
                (FilePath("Component.razor"), "")
            ]);

    [Fact]
    public Task Component_StartTag()
        => VerifyPrepareRenameAsync(
            input: """
                <[|Com$$ponent|]></Component>
                """,
            additionalFiles:
            [
                (FilePath("Component.razor"), "")
            ]);

    [Fact]
    public Task Component_Attribute()
        => VerifyPrepareRenameAsync(
            input: """
                <Component [|Tit$$le|]="Hello" />
                """,
            additionalFiles: GetComponentFiles());

    [Fact]
    public Task Component_Attribute_Bind()
        => VerifyPrepareRenameAsync(
            input: """
                <Component @bind-[|Tit$$le|]="Hello" />
                """,
            additionalFiles: GetComponentFiles());

    [Fact]
    public Task Component_Attribute_BindWithParameter()
        => VerifyPrepareRenameAsync(
            input: """
                <Component @bind-[|Tit$$le|]:get="Hello" />
                """,
            additionalFiles: GetComponentFiles());

    [Fact]
    public Task Component_StartTag_FromMetadata()
        => VerifyPrepareRenameAsync(
            input: """
                <Inp$$utText></InputText>
                """);

    [Fact]
    public Task Component_Attribute_FromMetadata()
        => VerifyPrepareRenameAsync(
            input: """
            <InputText V$$alue="Hello" />
            """);

    [Fact]
    public Task Component_AttributeValue_ImplicitExpression()
        => VerifyPrepareRenameAsync(
            input: """
                <Component Title="@[|Inp$$utValue|]" />

                @code
                {
                    private string InputValue = "hello";
                }
                """,
            additionalFiles: GetComponentFiles());

    [Fact]
    public Task Component_AttributeValue_StringLiteral_ReturnsNull()
        => VerifyPrepareRenameAsync(
            input: """
                <Component Title="he$$llo" />
                """,
            additionalFiles: GetComponentFiles());

    [Fact]
    public Task Component_EndTag()
        => VerifyPrepareRenameAsync(
            input: """
                <Component></[|Com$$ponent|]>
                """,
            additionalFiles:
            [
                (FilePath("Component.razor"), "")
            ]);

    [Fact]
    public Task Component_FullyQualified_Name()
        => VerifyPrepareRenameAsync(
            input: """
                <My.Foo.[|Com$$ponent|] />
                """,
            additionalFiles: GetComponentFiles("My.Foo"));

    [Fact]
    public Task Component_FullyQualified_Namespace_UsesRoslyn()
        => VerifyPrepareRenameAsync(
            input: """
                <My.[|Fo$$o|].Component />
                """,
            additionalFiles: GetComponentFiles("My.Foo"));

    [Fact]
    public Task Component_FullyQualified_EndTagNamespace_UsesRoslyn()
        => VerifyPrepareRenameAsync(
            input: """
                <My.Foo.Component></My.[|Fo$$o|].Component>
                """,
            additionalFiles: GetComponentFiles("My.Foo"));

    [Fact]
    public Task Html_ReturnsNull()
        => VerifyPrepareRenameAsync(
            input: """
                <di$$v></div>
                """);

    [Fact]
    public Task Html_UsesHtmlLanguageServer()
        => VerifyPrepareRenameAsync(
            input: """
                <{|html:di$$v|}></div>
                """);

    [Fact]
    public Task Html_Attribute_ReturnsNull()
        => VerifyPrepareRenameAsync(
            input: """
                <div cla$$ss="hello"></div>
                """);

    [Fact]
    public Task Html_AttributeValue_ReturnsNull()
        => VerifyPrepareRenameAsync(
            input: """
                <div class="he$$llo"></div>
                """);

    [Fact]
    public Task Html_AttributeValue_ImplicitExpression()
        => VerifyPrepareRenameAsync(
            input: """
                <div class="@[|Cla$$ssName|]"></div>

                @code
                {
                    private string ClassName = "hello";
                }
                """);

    [Fact]
    public Task Legacy_CSharp_ExplicitStatement_Local()
        => VerifyPrepareRenameAsync(
            input: """
                @{
                    var hello = 1;
                    [|he$$llo|]++;
                }
                """,
                fileKind: RazorFileKind.Legacy);

    [Fact]
    public Task Legacy_CSharp_ImplicitExpression_Field()
        => VerifyPrepareRenameAsync(
            input: """
                <div>@[|Tit$$le|]</div>

                @functions
                {
                    private string Title = "hello";
                }
                """,
                fileKind: RazorFileKind.Legacy);

    [Fact]
    public Task Legacy_TagHelper_Element_UsesHtmlLanguageServer()
        => VerifyPrepareRenameAsync(
            input: """
                @addTagHelper *, SomeProject

                <{|html:dw:abou$$t-box|} />
                """,
            fileKind: RazorFileKind.Legacy,
            additionalFiles: GetTagHelperFiles());

    [Fact]
    public Task Legacy_TagHelper_Attribute_UsesHtmlLanguageServer()
        => VerifyPrepareRenameAsync(
            input: """
                @addTagHelper *, SomeProject

                <dw:about-box {|html:tit$$le|}="Dave" />
                """,
            fileKind: RazorFileKind.Legacy,
            additionalFiles: GetTagHelperFiles());

    [Fact]
    public Task Legacy_Html_Attribute_ReturnsNull()
        => VerifyPrepareRenameAsync(
            input: """
                <div cla$$ss="hello"></div>
                """,
                fileKind: RazorFileKind.Legacy);

    [Fact]
    public Task Legacy_Html_AttributeValue_ReturnsNull()
        => VerifyPrepareRenameAsync(
            input: """
                <div class="he$$llo"></div>
                """,
                fileKind: RazorFileKind.Legacy);

    [Fact]
    public Task Legacy_Html_AttributeValue_ImplicitExpression()
        => VerifyPrepareRenameAsync(
            input: """
                <div class="@[|Cla$$ssName|]"></div>

                @functions
                {
                    private string ClassName = "hello";
                }
                """,
                fileKind: RazorFileKind.Legacy);

    private static (string fileName, string contents)[] GetComponentFiles(string? @namespace = "SomeProject") =>
        [
            (FilePath("Component.razor"), $$"""
                @namespace {{@namespace}}

                <div></div>

                @code
                {
                    [Parameter]
                    public string Title { get; set; }
                }
                """)
        ];

    private static (string fileName, string contents)[] GetTagHelperFiles() =>
        [
            (FilePath("AboutBoxTagHelper.cs"), """
                using Microsoft.AspNetCore.Razor.TagHelpers;

                [HtmlTargetElement("dw:about-box")]
                public class AboutBoxTagHelper : TagHelper
                {
                    public string Title { get; set; }

                    public override void Process(TagHelperContext context, TagHelperOutput output)
                    {
                        output.TagName = "div";
                    }
                }
                """)
        ];

    private async Task VerifyPrepareRenameAsync(
        TestCode input,
        RazorFileKind? fileKind = null,
        (string fileName, string contents)[]? additionalFiles = null)
    {
        var document = CreateProjectAndRazorDocument(input.Text, fileKind, additionalFiles: additionalFiles);
        var sourceText = await document.GetTextAsync(DisposalToken);
        var htmlResponse = input.TryGetNamedSpans("html", out var htmlSpans)
            ? (object?)sourceText.GetRange(Assert.Single(htmlSpans))
            : null;
        var requestInvoker = new TestHtmlRequestInvoker([(Methods.TextDocumentPrepareRenameName, htmlResponse)]);

        var endpoint = new CohostPrepareRenameEndpoint(IncompatibleProjectService, RemoteServiceInvoker, requestInvoker);
        var result = await endpoint.GetTestAccessor().HandleRequestAsync(document, sourceText.GetPosition(input.Position), DisposalToken);

        if (!input.HasSpans && htmlResponse is null)
        {
            Assert.Null(result);
            return;
        }

        Assert.NotNull(result);
        var expectedRange = input.HasSpans
            ? sourceText.GetRange(input.Span)
            : sourceText.GetRange(Assert.Single(htmlSpans));
        Assert.Equal(expectedRange, result);
    }
}
