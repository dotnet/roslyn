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
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public Task CSharp_LocalTypeAlias_FromUsing()
        => VerifyPrepareRenameAsync(
            input: """
                @using [|Ali$$as|] = System.String
                @((Alias)null)
                """);

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public Task CSharp_LocalEscapedAlias_FromUsing()
        => VerifyPrepareRenameAsync(
            input: """
                @using [|@cl$$ass|] = System.String
                @((@class)null)
                """);

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public Task CSharp_LocalNamespaceAlias_FromInlineExpression()
        => VerifyPrepareRenameAsync(
            input: """
                @using Alias = System.Text
                @(([|Ali$$as|].StringBuilder)null)
                """);

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public Task CSharp_LocalTypeAlias_FromDeclaration()
        => VerifyPrepareRenameAsync(
            input: """
                @using Alias = System.String
                @code {
                    private [|Ali$$as|]? _value;
                }
                """);

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public Task Component_LocalTypeAlias()
        => VerifyPrepareRenameAsync(
            input: """
                @using Alias = My.Foo.Component
                <[|Ali$$as|]></Alias>
                """,
            additionalFiles: GetComponentFiles("My.Foo"));

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public Task Component_LocalNamespaceAlias_StartTag()
        => VerifyPrepareRenameAsync(
            input: """
                @using Alias = My.Foo
                <[|Ali$$as|].Component></Alias.Component>
                """,
            additionalFiles: GetComponentFiles("My.Foo"));

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public Task Component_LocalNamespaceAlias_EndTag()
        => VerifyPrepareRenameAsync(
            input: """
                @using Alias = My.Foo
                <Alias.Component></[|Ali$$as|].Component>
                """,
            additionalFiles: GetComponentFiles("My.Foo"));

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public Task Component_LocalNamespaceAlias_NestedNamespace()
        => VerifyPrepareRenameAsync(
            input: """
                @using /* alias */ Alias = My
                <[|Ali$$as|].Foo.Component />
                """,
            additionalFiles: GetComponentFiles("My.Foo"));

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public Task Component_LocalNamespaceAlias_ComponentName()
        => VerifyPrepareRenameAsync(
            input: """
                @using Alias = My.Foo
                <Alias.[|Com$$ponent|] />
                """,
            additionalFiles: GetComponentFiles("My.Foo"));

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public Task Component_LocalNamespaceAlias_NestedNamespaceSegmentDoesNotFallbackToHtml()
        => VerifyPrepareRenameAsync(
            input: """
                @using Alias = My.Foo
                <Alias.[|Con$$trols|].Component />
                """,
            additionalFiles: GetComponentFiles("My.Foo.Controls"),
            returnHtmlResponse: true,
            expectNoResult: true);

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public Task Component_LocalNamespaceAlias_EndBoundary()
        => VerifyPrepareRenameAsync(
            input: """
                @using UI = My.Foo
                <[|UI$$|].Component />
                """,
            additionalFiles: GetComponentFiles("My.Foo"));

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public Task Component_LocalEscapedNamespaceAlias()
        => VerifyPrepareRenameAsync(
            input: """
                @using @class = My.Foo
                <[|cl$$ass|].Component />
                """,
            additionalFiles: GetComponentFiles("My.Foo"));

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public Task Component_LocalNamespaceAlias_Attribute()
        => VerifyPrepareRenameAsync(
            input: """
                @using Alias = My.Foo
                <Alias.Component [|Tit$$le|]="Hello" />
                """,
            additionalFiles: GetComponentFiles("My.Foo"));

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public Task Component_ImportedNamespaceAlias_StartTag_ReturnsNull()
        => VerifyPrepareRenameAsync(
            input: """
                <Ali$$as.Component></Alias.Component>
                """,
            documentFilePath: FilePath("Aliases/File1.razor"),
            additionalFiles: [
                (FilePath("Aliases/_Imports.razor"), "@using Alias = My.Foo"),
                .. GetComponentFiles("My.Foo")
            ]);

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public Task Component_ImportedNamespaceAlias_EndTag_ReturnsNull()
        => VerifyPrepareRenameAsync(
            input: """
                <Alias.Component></Ali$$as.Component>
                """,
            documentFilePath: FilePath("Aliases/File1.razor"),
            additionalFiles: [
                (FilePath("Aliases/_Imports.razor"), "@using Alias = My.Foo"),
                .. GetComponentFiles("My.Foo")
            ]);

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public Task CSharp_ImportedTypeAlias_ReturnsNull()
        => VerifyPrepareRenameAsync(
            input: """
                @((Ali$$as)null)
                """,
            documentFilePath: FilePath("Aliases/File1.razor"),
            additionalFiles: [(FilePath("Aliases/_Imports.razor"), "@using Alias = System.String")]);

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public Task CSharp_ImportedNamespaceAlias_ReturnsNull()
        => VerifyPrepareRenameAsync(
            input: """
                @((Ali$$as.StringBuilder)null)
                """,
            documentFilePath: FilePath("Aliases/File1.razor"),
            additionalFiles: [(FilePath("Aliases/_Imports.razor"), "@using Alias = System.Text")]);

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public Task CSharp_ImportedAlias_FromDeclaration_ReturnsNull()
        => VerifyPrepareRenameAsync(
            input: """
                @code {
                    private Ali$$as? _value;
                }
                """,
            documentFilePath: FilePath("Aliases/File1.razor"),
            additionalFiles: [(FilePath("Aliases/_Imports.razor"), "@using Alias = System.String")]);

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public Task Component_ImportedTypeAlias_ReturnsNull()
        => VerifyPrepareRenameAsync(
            input: """
                <Ali$$as></Alias>
                """,
            documentFilePath: FilePath("Aliases/File1.razor"),
            additionalFiles: [
                (FilePath("Aliases/_Imports.razor"), "@using Alias = My.Foo.Component"),
                .. GetComponentFiles("My.Foo")
            ]);

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public Task CSharp_GlobalAlias_ReturnsNull()
        => VerifyPrepareRenameAsync(
            input: """
                @((Ali$$as)null)
                """,
            additionalFiles: [(FilePath("GlobalUsings.cs"), "global using Alias = System.String;")]);

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public Task Component_ImportedNamespaceAlias_ComponentName()
        => VerifyPrepareRenameAsync(
            input: """
                <Alias.[|Com$$ponent|] />
                """,
            documentFilePath: FilePath("Aliases/File1.razor"),
            additionalFiles: [
                (FilePath("Aliases/_Imports.razor"), "@using Alias = My.Foo"),
                .. GetComponentFiles("My.Foo")
            ]);

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
        (string fileName, string contents)[]? additionalFiles = null,
        string? documentFilePath = null,
        bool returnHtmlResponse = false,
        bool expectNoResult = false)
    {
        var document = CreateProjectAndRazorDocument(input.Text, fileKind, documentFilePath: documentFilePath, additionalFiles: additionalFiles);
        var sourceText = await document.GetTextAsync(DisposalToken);
        var hasHtmlSpan = input.TryGetNamedSpans("html", out var htmlSpans);
        var htmlResponse = returnHtmlResponse
            ? sourceText.GetRange(input.Span)
            : hasHtmlSpan
                ? (object?)sourceText.GetRange(Assert.Single(htmlSpans))
                : null;
        var requestInvoker = new TestHtmlRequestInvoker([(Methods.TextDocumentPrepareRenameName, htmlResponse)]);

        var endpoint = new CohostPrepareRenameEndpoint(IncompatibleProjectService, RemoteServiceInvoker, requestInvoker);
        var result = await endpoint.GetTestAccessor().HandleRequestAsync(document, sourceText.GetPosition(input.Position), DisposalToken);

        if (expectNoResult || (!input.HasSpans && htmlResponse is null))
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
