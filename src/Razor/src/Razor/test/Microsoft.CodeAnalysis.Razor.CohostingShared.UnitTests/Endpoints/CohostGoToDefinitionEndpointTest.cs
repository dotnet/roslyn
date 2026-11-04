// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Remote.Razor;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;
using TextDocument = Microsoft.CodeAnalysis.TextDocument;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public class CohostGoToDefinitionEndpointTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task CSharp_Method()
    {
        TestCode input = """
            <div></div>
            @{
                var x = Ge$$tX();
            }
            @functions
            {
                int [|GetX|]()
                {
                    return 4;
                }
            }
            """;

        var document = CreateProjectAndRazorDocument(input.Text);
        var response = await GetRemoteGoToDefinitionResponseAsync(document, input);

        Assert.False(response.StopHandling);
        Assumes.NotNull(response.Result);
        Assert.NotNull(response.Result.Locations);
        Assert.Null(response.Result.CSharpRequest);

        await VerifyGoToDefinitionAsync(input, razorDocument: document);
    }

    [Fact]
    public async Task CSharp_Local()
    {
        var input = """
            <div></div>
            @{
                var x = GetX();
            }
            @functions
            {
                private string [|_name|];
                string GetX()
                {
                    return _na$$me;
                }
            }
            """;

        await VerifyGoToDefinitionAsync(input);
    }

    [Fact]
    public async Task CSharp_MetadataReference()
    {
        TestCode input = """
            <div></div>
            @functions
            {
                private stri$$ng _name;
            }
            """;

        var document = CreateProjectAndRazorDocument(input.Text);
        var response = await GetRemoteGoToDefinitionResponseAsync(document, input);

        Assert.False(response.StopHandling);
        Assumes.NotNull(response.Result);
        Assert.Null(response.Result.Locations);
        Assert.NotNull(response.Result.CSharpRequest);

        var result = await GetGoToDefinitionResultCoreAsync(document, input, htmlResponse: null);

        Assumes.NotNull(result);
        Assert.NotNull(result.Value.Second);
        var locations = result.Value.Second;
        var location = Assert.Single(locations);
        Assert.EndsWith("String.cs", location.DocumentUri.UriString);

        // Note: The location is in a generated C# "metadata-as-source" file, which has a different
        // number of using directives in .NET Framework vs. .NET Core, so rather than relying on line
        // numbers we do some vague notion of actual navigation and test the actual source line that
        // the user would see.
        var line = File.ReadLines(location.DocumentUri.GetRequiredSystemUri().LocalPath).ElementAt(location.Range.Start.Line);
        Assert.Contains("public sealed class String", line);
    }

    [Theory]
    [InlineData("$$IncrementCount")]
    [InlineData("In$$crementCount")]
    [InlineData("IncrementCount$$")]
    public async Task Attribute_SameFile(string method)
    {
        var input = $$"""
            <button @onclick="{{method}}"></div>

            @code
            {
                void [|IncrementCount|]()
                {
                }
            }
            """;

        await VerifyGoToDefinitionAsync(input, RazorFileKind.Component);
    }

    [Fact]
    public async Task AttributeValue_BindAfter()
    {
        var input = """
            <input type="text" @bind="InputValue" @bind:after="() => Af$$ter()">

            @code
            {
                public string InputValue { get; set; }

                public void [|After|]()
                {
                }
            }
            """;

        await VerifyGoToDefinitionAsync(input, RazorFileKind.Component);
    }

    [Fact]
    public async Task Component()
    {
        TestCode input = """
            <Surv$$eyPrompt Title="InputValue" />
            """;

        TestCode surveyPrompt = """
            [||]@namespace SomeProject

            <div></div>

            @code
            {
                [Parameter]
                public string Title { get; set; }
            }
            """;

        var result = await GetGoToDefinitionResultAsync(input, RazorFileKind.Component,
            additionalFiles: (FilePath("SurveyPrompt.razor"), surveyPrompt.Text));

        Assumes.NotNull(result);
        Assert.NotNull(result.Value.Second);
        var locations = result.Value.Second;
        var location = Assert.Single(locations);

        Assert.Equal(FileUri("SurveyPrompt.razor"), location.DocumentUri);
        var text = SourceText.From(surveyPrompt.Text);
        var range = text.GetRange(surveyPrompt.Span);
        Assert.Equal(range, location.Range);
    }

    [Fact]
    public async Task Component_FromCSharp()
    {
        TestCode input = """
            <SurveyPrompt Title="InputValue" />

            @typeof(Surv$$eyPrompt).ToString()
            """;

        TestCode surveyPrompt = """
            [||]@namespace SomeProject

            <div></div>

            @code
            {
                [Parameter]
                public string Title { get; set; }
            }
            """;

        var result = await GetGoToDefinitionResultAsync(input, RazorFileKind.Component,
            additionalFiles: (FilePath("SurveyPrompt.razor"), surveyPrompt.Text));

        Assumes.NotNull(result);
        Assert.NotNull(result.Value.Second);
        var locations = result.Value.Second;
        var location = Assert.Single(locations);

        Assert.Equal(FileUri("SurveyPrompt.razor"), location.DocumentUri);
        var text = SourceText.From(surveyPrompt.Text);
        var range = text.GetRange(surveyPrompt.Span);
        Assert.Equal(range, location.Range);
    }

    [Fact]
    public async Task ComponentEndTag()
    {
        TestCode input = """
            <SurveyPrompt Title="InputValue"></Surv$$eyPrompt>
            """;

        TestCode surveyPrompt = """
            [||]@namespace SomeProject

            <div></div>

            @code
            {
                [Parameter]
                public string Title { get; set; }
            }
            """;

        var result = await GetGoToDefinitionResultAsync(input, RazorFileKind.Component,
            additionalFiles: (FilePath("SurveyPrompt.razor"), surveyPrompt.Text));

        Assumes.NotNull(result);
        Assert.NotNull(result.Value.Second);
        var locations = result.Value.Second;
        var location = Assert.Single(locations);

        Assert.Equal(FileUri("SurveyPrompt.razor"), location.DocumentUri);
        var text = SourceText.From(surveyPrompt.Text);
        var range = text.GetRange(surveyPrompt.Span);
        Assert.Equal(range, location.Range);
    }

    [Fact]
    public async Task ComponentAttribute()
    {
        TestCode input = """
            <SurveyPrompt Ti$$tle="InputValue" />
            """;

        TestCode surveyPrompt = """
            @namespace SomeProject

            <div></div>

            @code
            {
                [Parameter]
                public string [|Title|] { get; set; }
            }
            """;

        var result = await GetGoToDefinitionResultAsync(input, RazorFileKind.Component,
            additionalFiles: (FilePath("SurveyPrompt.razor"), surveyPrompt.Text));

        Assumes.NotNull(result);
        Assert.NotNull(result.Value.Second);
        var locations = result.Value.Second;
        var location = Assert.Single(locations);

        Assert.Equal(FileUri("SurveyPrompt.razor"), location.DocumentUri);
        var text = SourceText.From(surveyPrompt.Text);
        var range = text.GetRange(surveyPrompt.Span);
        Assert.Equal(range, location.Range);
    }

    [Fact]
    public async Task ComponentAttributeValue()
    {
        TestCode input = """
            <SurveyPrompt Title="@Inp$$utValue" />

            @code
            {
                private string? [|InputValue|] { get; set; }
            }
            """;

        TestCode surveyPrompt = """
            @namespace SomeProject

            <div></div>

            @code
            {
                [Parameter]
                public string Title { get; set; }
            }
            """;

        var result = await GetGoToDefinitionResultAsync(input, RazorFileKind.Component,
            additionalFiles: (FilePath("SurveyPrompt.razor"), surveyPrompt.Text));

        Assumes.NotNull(result);
        Assert.NotNull(result.Value.Second);
        var locations = result.Value.Second;
        var location = Assert.Single(locations);

        Assert.Equal(FileUri("File1.razor"), location.DocumentUri);
        var text = SourceText.From(input.Text);
        var range = text.GetRange(input.Span);
        Assert.Equal(range, location.Range);
    }

    [Fact]
    public async Task Component_DefinedInCSharp()
    {
        TestCode input = """
            <Surv$$eyPrompt Title="InputValue" />
            """;

        // lang=c#-test
        TestCode surveyPrompt = """
            using Microsoft.AspNetCore.Components;
            using Microsoft.AspNetCore.Components.Rendering;

            namespace SomeProject;

            public class [|SurveyPrompt|] : ComponentBase
            {
                [Parameter]
                public string Title { get; set; } = "Hello";

                protected override void BuildRenderTree(RenderTreeBuilder builder)
                {
                    builder.OpenElement(0, "div");
                    builder.AddContent(1, Title + " from a C#-defined component!");
                    builder.CloseElement();
                }
            }
            """;

        var result = await GetGoToDefinitionResultAsync(input, RazorFileKind.Component,
            additionalFiles: (FilePath("SurveyPrompt.cs"), surveyPrompt.Text));

        Assumes.NotNull(result);
        Assert.NotNull(result.Value.Second);
        var locations = result.Value.Second;
        var location = Assert.Single(locations);

        Assert.Equal(FileUri("SurveyPrompt.cs"), location.DocumentUri);
        var text = SourceText.From(surveyPrompt.Text);
        var range = text.GetRange(surveyPrompt.Span);
        Assert.Equal(range, location.Range);
    }

    [Fact]
    public async Task ComponentAttribute_DefinedInCSharp()
    {
        TestCode input = """
            <SurveyPrompt Ti$$tle="InputValue" />
            """;

        // lang=c#-test
        TestCode surveyPrompt = """
            using Microsoft.AspNetCore.Components;
            using Microsoft.AspNetCore.Components.Rendering;

            namespace SomeProject;

            public class SurveyPrompt : ComponentBase
            {
                [Parameter]
                public string [|Title|] { get; set; } = "Hello";

                protected override void BuildRenderTree(RenderTreeBuilder builder)
                {
                    builder.OpenElement(0, "div");
                    builder.AddContent(1, Title + " from a C#-defined component!");
                    builder.CloseElement();
                }
            }
            """;

        var result = await GetGoToDefinitionResultAsync(input, RazorFileKind.Component,
            additionalFiles: (FilePath("SurveyPrompt.cs"), surveyPrompt.Text));

        Assumes.NotNull(result);
        Assert.NotNull(result.Value.Second);
        var locations = result.Value.Second;
        var location = Assert.Single(locations);

        Assert.Equal(FileUri("SurveyPrompt.cs"), location.DocumentUri);
        var text = SourceText.From(surveyPrompt.Text);
        var range = text.GetRange(surveyPrompt.Span);
        Assert.Equal(range, location.Range);
    }

    [Theory]
    [InlineData("Ti$$tle")]
    [InlineData("$$@bind-Title")]
    [InlineData("@$$bind-Title")]
    [InlineData("@bi$$nd-Title")]
    [InlineData("@bind$$-Title")]
    [InlineData("@bind-Ti$$tle")]
    public async Task OtherRazorFile(string attribute)
    {
        TestCode input = $$"""
            <SurveyPrompt {{attribute}}="InputValue" />

            @code
            {
                private string? InputValue { get; set; }

                private void BindAfter()
                {
                }
            }
            """;

        TestCode surveyPrompt = """
            @namespace SomeProject

            <div></div>

            @code
            {
                [Parameter]
                public string [|Title|] { get; set; }
            }
            """;

        var result = await GetGoToDefinitionResultAsync(input, RazorFileKind.Component,
            additionalFiles: (FilePath("SurveyPrompt.razor"), surveyPrompt.Text));

        Assumes.NotNull(result);
        Assert.NotNull(result.Value.Second);
        var locations = result.Value.Second;
        var location = Assert.Single(locations);

        var text = SourceText.From(surveyPrompt.Text);
        var range = text.GetRange(surveyPrompt.Span);
        Assert.Equal(range, location.Range);
    }

    [Fact]
    public async Task Html()
    {
        // This really just validates Uri remapping, the actual response is largely arbitrary

        TestCode input = """
            <div></div>

            <script>
                function [|foo|]() {
                    f$$oo();
                }
            </script>
            """;

        var document = CreateProjectAndRazorDocument(input.Text);
        var inputText = await document.GetTextAsync(DisposalToken);

        var uri = new DocumentUri($"{document.CreateSystemUri()}{LanguageServerConstants.HtmlVirtualDocumentSuffix}");
        var htmlResponse = new SumType<LspLocation, LspLocation[], DocumentLink[]>?(new LspLocation[]
        {
            new() {
                DocumentUri = uri,
                Range = inputText.GetRange(input.Span),
            },
        });

        await VerifyGoToDefinitionAsync(input, htmlResponse: htmlResponse, razorDocument: document);
    }

    [Fact]
    public async Task ViewComponent()
    {
        TestCode expected;
        var result = await GetGoToDefinitionResultAsync("""
            @addTagHelper *, SomeProject

            <vc:aut$$hor-view author-id="1234"></vc:author-view>
            """,
            fileKind: RazorFileKind.Legacy,
            additionalFiles:
                (FilePath("AuthorViewComponent.cs"),
                    (expected = """
                        using Microsoft.AspNetCore.Mvc;

                        public class [|AuthorViewViewComponent|] : ViewComponent
                        {
                            public string Invoke(int authorId)
                            {
                                return "Steve";
                            }
                        }
                        """).Text));

        Assumes.NotNull(result);
        Assert.NotNull(result.Value.Second);
        var locations = result.Value.Second;
        var location = Assert.Single(locations);

        Assert.Equal(FileUri("AuthorViewComponent.cs"), location.DocumentUri);
        Assert.Equal(expected.Span, SourceText.From(expected.Text).GetTextSpan(location.Range));
    }

    [Fact]
    public async Task TagHelper()
    {
        TestCode expected;
        var result = await GetGoToDefinitionResultAsync("""
            @addTagHelper *, SomeProject

            <dw:abou$$t-box />
            """,
            fileKind: RazorFileKind.Legacy,
            additionalFiles:
                (FilePath("AboutBoxTagHelper.cs"),
                    (expected = """
                        using Microsoft.AspNetCore.Razor.TagHelpers;

                        [HtmlTargetElement("dw:about-box")]
                        public class [|AboutBoxTagHelper|] : TagHelper
                        {
                            public override void Process(TagHelperContext context, TagHelperOutput output)
                            {
                                output.TagName = "div";
                                output.Attributes.SetAttribute("class", "about-box");
                                output.Content.SetHtmlContent("<strong>About</strong>");
                            }
                        }
                        """).Text));

        Assumes.NotNull(result);
        Assert.NotNull(result.Value.Second);
        var locations = result.Value.Second;
        var location = Assert.Single(locations);

        Assert.Equal(FileUri("AboutBoxTagHelper.cs"), location.DocumentUri);
        Assert.Equal(expected.Span, SourceText.From(expected.Text).GetTextSpan(location.Range));
    }

    [Fact]
    public async Task TagHelper_Partial()
    {
        TestCode expected;
        var result = await GetGoToDefinitionResultAsync("""
            @addTagHelper *, SomeProject

            <dw:abou$$t-box />
            """,
            fileKind: RazorFileKind.Legacy,
            additionalFiles:
                [(FilePath("AboutBoxTagHelper_1.cs"),
                    (expected = """
                        using Microsoft.AspNetCore.Razor.TagHelpers;

                        [HtmlTargetElement("dw:about-box")]
                        public partial class [|AboutBoxTagHelper|] : TagHelper
                        {
                            public override void Process(TagHelperContext context, TagHelperOutput output)
                            {
                                output.TagName = "div";
                                output.Attributes.SetAttribute("class", "about-box");
                                output.Content.SetHtmlContent("<strong>About</strong>");
                            }
                        }
                        """).Text),
                (FilePath("AboutBoxTagHelper_2.cs"),
                    """
                    using Microsoft.AspNetCore.Razor.TagHelpers;

                    public partial class AboutBoxTagHelper
                    {
                    }
                    """)]);

        Assumes.NotNull(result);
        Assert.NotNull(result.Value.Second);
        var locations = result.Value.Second;
        var location = Assert.Single(locations);

        Assert.Equal(FileUri("AboutBoxTagHelper_1.cs"), location.DocumentUri);
        Assert.Equal(expected.Span, SourceText.From(expected.Text).GetTextSpan(location.Range));
    }

    [Fact]
    public async Task TagHelper_Attribute()
    {
        TestCode expected;
        var result = await GetGoToDefinitionResultAsync("""
            @addTagHelper *, SomeProject

            <dw:about-box tit$$le="Dave" />
            """,
            fileKind: RazorFileKind.Legacy,
            additionalFiles:
                (FilePath("AboutBoxTagHelper.cs"),
                    (expected = """
                        using Microsoft.AspNetCore.Razor.TagHelpers;

                        [HtmlTargetElement("dw:about-box")]
                        public class AboutBoxTagHelper : TagHelper
                        {
                            public string [|Title|] { get; set; }

                            public override void Process(TagHelperContext context, TagHelperOutput output)
                            {
                                output.TagName = "div";
                                output.Attributes.SetAttribute("class", "about-box");
                                output.Content.SetHtmlContent("<strong>About</strong>");
                            }
                        }
                        """).Text));

        Assumes.NotNull(result);
        Assert.NotNull(result.Value.Second);
        var locations = result.Value.Second;
        var location = Assert.Single(locations);

        Assert.Equal(FileUri("AboutBoxTagHelper.cs"), location.DocumentUri);
        Assert.Equal(expected.Span, SourceText.From(expected.Text).GetTextSpan(location.Range));
    }

    [Fact]
    public async Task TagHelper_Attribute_Partial()
    {
        TestCode expected;
        var result = await GetGoToDefinitionResultAsync("""
            @addTagHelper *, SomeProject

            <dw:about-box tit$$le="Dave" />
            """,
            fileKind: RazorFileKind.Legacy,
            additionalFiles:
                [(FilePath("AboutBoxTagHelper_1.cs"),
                    """
                    using Microsoft.AspNetCore.Razor.TagHelpers;

                    [HtmlTargetElement("dw:about-box")]
                    public partial class AboutBoxTagHelper : TagHelper
                    {
                        public override void Process(TagHelperContext context, TagHelperOutput output)
                        {
                            output.TagName = "div";
                            output.Attributes.SetAttribute("class", "about-box");
                            output.Content.SetHtmlContent("<strong>About</strong>");
                        }
                    }
                    """),
                (FilePath("AboutBoxTagHelper_2.cs"),
                    (expected = """
                        using Microsoft.AspNetCore.Razor.TagHelpers;

                        public partial class AboutBoxTagHelper
                        {
                            public string [|Title|] { get; set; }
                        }
                        """).Text)]);

        Assumes.NotNull(result);
        Assert.NotNull(result.Value.Second);
        var locations = result.Value.Second;
        var location = Assert.Single(locations);

        Assert.Equal(FileUri("AboutBoxTagHelper_2.cs"), location.DocumentUri);
        Assert.Equal(expected.Span, SourceText.From(expected.Text).GetTextSpan(location.Range));
    }

    [Fact]
    public async Task TagHelper_Attribute_Wildcard()
    {
        var result = await GetGoToDefinitionResultAsync("""
            @addTagHelper *, SomeProject

            <label fo$$o="Dave" />
            """,
            fileKind: RazorFileKind.Legacy,
            additionalFiles:
                (FilePath("FooTagHelper.cs"), """
                    using Microsoft.AspNetCore.Razor.TagHelpers;

                    [HtmlTargetElement("*", Attributes = FooAttributeName)]
                    public class FooTaghelper : TagHelper
                    {
                        private const string FooAttributeName = "foo";

                        public override void Process(TagHelperContext context, TagHelperOutput output)
                        {
                            output.Attributes.Add("foo", "bar");
                        }
                    }
                    """));

        Assert.Null(result);
    }

    [Fact]
    public async Task TagHelper_Element_Wildcard()
    {
        TestCode expected;
        var result = await GetGoToDefinitionResultAsync("""
            @addTagHelper *, SomeProject

            <lab$$el foo="Dave" />
            """,
            fileKind: RazorFileKind.Legacy,
            additionalFiles:
                (FilePath("FooTagHelper.cs"),
                    (expected = """
                        using Microsoft.AspNetCore.Razor.TagHelpers;

                        [HtmlTargetElement("*", Attributes = FooAttributeName)]
                        public class [|FooTaghelper|] : TagHelper
                        {
                            private const string FooAttributeName = "foo";

                            public override void Process(TagHelperContext context, TagHelperOutput output)
                            {
                                output.Attributes.Add("foo", "bar");
                            }
                        }
                        """).Text));

        Assumes.NotNull(result);
        Assert.NotNull(result.Value.Second);
        var locations = result.Value.Second;
        var location = Assert.Single(locations);

        Assert.Equal(FileUri("FooTagHelper.cs"), location.DocumentUri);
        Assert.Equal(expected.Span, SourceText.From(expected.Text).GetTextSpan(location.Range));
    }

    [Fact]
    public async Task TagHelper_Attribute_Wildcard_WithOtherTagHelper()
    {
        var result = await GetGoToDefinitionResultAsync("""
            @addTagHelper *, SomeProject

            <label asp-for="fish" fo$$o="Dave" />
            """,
            fileKind: RazorFileKind.Legacy,
            additionalFiles:
                (FilePath("FooTagHelper.cs"), """
                    using Microsoft.AspNetCore.Razor.TagHelpers;

                    [HtmlTargetElement("*", Attributes = FooAttributeName)]
                    public class FooTaghelper : TagHelper
                    {
                        private const string FooAttributeName = "foo";

                        public override void Process(TagHelperContext context, TagHelperOutput output)
                        {
                            output.Attributes.Add("foo", "bar");
                        }
                    }
                    """));

        Assert.Null(result);
    }

    [Fact]
    public async Task TagHelper_Element_Wildcard_WithOtherTagHelper()
    {
        TestCode expected;
        var result = await GetGoToDefinitionResultAsync("""
            @addTagHelper *, SomeProject

            <lab$$el asp-for="fish" foo="Dave" />
            """,
            fileKind: RazorFileKind.Legacy,
            additionalFiles:
                (FilePath("FooTagHelper.cs"),
                    (expected = """
                        using Microsoft.AspNetCore.Razor.TagHelpers;

                        [HtmlTargetElement("*", Attributes = FooAttributeName)]
                        public class [|FooTaghelper|] : TagHelper
                        {
                            private const string FooAttributeName = "foo";

                            public override void Process(TagHelperContext context, TagHelperOutput output)
                            {
                                output.Attributes.Add("foo", "bar");
                            }
                        }
                        """).Text));

        Assumes.NotNull(result);
        Assert.NotNull(result.Value.Second);
        var locations = result.Value.Second;
        var location = Assert.Single(locations);

        Assert.Equal(FileUri("FooTagHelper.cs"), location.DocumentUri);
        Assert.Equal(expected.Span, SourceText.From(expected.Text).GetTextSpan(location.Range));
    }

    [Fact]
    public async Task TagHelper_Attribute_Multiple()
    {
        var result = await GetGoToDefinitionResultAsync("""
            @addTagHelper *, SomeProject

            <label b$$ar="Paul" foo="Dave" />
            """,
            fileKind: RazorFileKind.Legacy,
            additionalFiles:
                (FilePath("TagHelpers.cs"), """
                    using Microsoft.AspNetCore.Razor.TagHelpers;

                    [HtmlTargetElement("label", Attributes = FooAttributeName)]
                    public class FooTaghelper: TagHelper
                    {
                        private const string FooAttributeName = "foo";

                        public override void Process(TagHelperContext context, TagHelperOutput output)
                        {
                            output.Attributes.Add("foo", "bar");
                        }
                    }

                    [HtmlTargetElement("label", Attributes = BarAttributeName)]
                    public class BarTaghelper : TagHelper
                    {
                        private const string BarAttributeName = "bar";

                        public override void Process(TagHelperContext context, TagHelperOutput output)
                        {
                            output.Attributes.Add("bar", "hello");
                        }
                    }
                    """));

        Assert.Null(result);
    }

    [Fact]
    public async Task TagHelper_Element_MultipleWildcard()
    {
        TestCode expected;
        var result = await GetGoToDefinitionResultAsync("""
            @addTagHelper *, SomeProject

            <la$$bel bar="Paul" foo="Dave" />
            """,
            fileKind: RazorFileKind.Legacy,
            additionalFiles:
                (FilePath("TagHelpers.cs"),
                    (expected = """
                        using Microsoft.AspNetCore.Razor.TagHelpers;

                        [HtmlTargetElement("label", Attributes = FooAttributeName)]
                        public class [|FooTaghelper|]: TagHelper
                        {
                            private const string FooAttributeName = "foo";

                            public override void Process(TagHelperContext context, TagHelperOutput output)
                            {
                                output.Attributes.Add("foo", "bar");
                            }
                        }

                        [HtmlTargetElement("label", Attributes = BarAttributeName)]
                        public class [|BarTaghelper|] : TagHelper
                        {
                            private const string BarAttributeName = "bar";

                            public override void Process(TagHelperContext context, TagHelperOutput output)
                            {
                                output.Attributes.Add("bar", "hello");
                            }
                        }
                        """).Text));

        Assumes.NotNull(result);
        Assert.NotNull(result.Value.Second);
        var locations = result.Value.Second;
        Assert.Equal(expected.Spans.Length, locations.Length);

        var location = locations[0];
        Assert.Equal(FileUri("TagHelpers.cs"), location.DocumentUri);
        Assert.Equal(expected.Spans[0], SourceText.From(expected.Text).GetTextSpan(location.Range));

        location = locations[1];
        Assert.Equal(FileUri("TagHelpers.cs"), location.DocumentUri);
        Assert.Equal(expected.Spans[1], SourceText.From(expected.Text).GetTextSpan(location.Range));
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/4325")]
    public async Task StringLiteral_TildePath()
    {
        var input = """
            @{
                Html.Partial("~/Views/Shared/_Pa$$rtial.cshtml");
            }
            """;

        var partialFileContent = """
            <div>This is a partial view</div>
            """;

        var result = await GetGoToDefinitionResultAsync(input, RazorFileKind.Legacy,
            additionalFiles: (FilePath("Views/Shared/_Partial.cshtml"), partialFileContent));

        Assumes.NotNull(result);
        Assert.NotNull(result.Value.Second);
        var locations = result.Value.Second;
        var location = Assert.Single(locations);

        Assert.Equal(FileUri("Views/Shared/_Partial.cshtml"), location.DocumentUri);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/4325")]
    public async Task StringLiteral_RelativePath()
    {
        var input = """
            @{
                Html.Partial("_Pa$$rtial.cshtml");
            }
            """;

        var partialFileContent = """
            <div>This is a partial view</div>
            """;

        var result = await GetGoToDefinitionResultAsync(input, RazorFileKind.Legacy,
            additionalFiles: (FilePath("_Partial.cshtml"), partialFileContent));

        Assumes.NotNull(result);
        Assert.NotNull(result.Value.Second);
        var locations = result.Value.Second;
        var location = Assert.Single(locations);

        Assert.Equal(FileUri("_Partial.cshtml"), location.DocumentUri);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/4325")]
    public async Task StringLiteral_RazorComponent()
    {
        var input = """
            @{
                var path = "~/Pages/Cou$$nter.razor";
            }
            """;

        var counterFileContent = """
            @page "/counter"

            <h1>Counter</h1>

            @code {
                private int currentCount = 0;
            }
            """;

        var result = await GetGoToDefinitionResultAsync(input, RazorFileKind.Component,
            additionalFiles: (FilePath("Pages/Counter.razor"), counterFileContent));

        Assumes.NotNull(result);
        Assert.NotNull(result.Value.Second);
        var locations = result.Value.Second;
        var location = Assert.Single(locations);

        Assert.Equal(FileUri("Pages/Counter.razor"), location.DocumentUri);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/84945")]
    [InlineData(RazorFileKind.Legacy, "css/site.css")]
    [InlineData(RazorFileKind.Legacy, "js/site.js")]
    [InlineData(RazorFileKind.Legacy, "images/logo.png")]
    [InlineData(RazorFileKind.Legacy, "LICENSE")]
    [InlineData(RazorFileKind.Component, "css/site.css")]
    [InlineData(RazorFileKind.Component, "js/site.js")]
    [InlineData(RazorFileKind.Component, "images/logo.png")]
    [InlineData(RazorFileKind.Component, "LICENSE")]
    public async Task StringLiteral_TildePath_AnyFileKind(RazorFileKind fileKind, string fileName)
    {
        TestCode input = $$"""
            @{
                var path = "~/{{fileName}}$$";
            }
            """;

        await VerifyFilePathDefinitionAsync(
            input,
            fileKind,
            expectedFileName: fileName,
            additionalFiles: [(FilePath(fileName), "")]);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/84945")]
    [InlineData(RazorFileKind.Legacy)]
    [InlineData(RazorFileKind.Component)]
    public async Task StringLiteral_TildePath_PrefersWwwroot(RazorFileKind fileKind)
    {
        TestCode input = """
            @{
                var path = "~/css/si$$te.css";
            }
            """;

        await VerifyFilePathDefinitionAsync(
            input,
            fileKind,
            expectedFileName: "wwwroot/css/site.css",
            additionalFiles: [
                (FilePath("wwwroot/css/site.css"), ""),
                (FilePath("css/site.css"), "")]);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/84945")]
    [InlineData(RazorFileKind.Legacy)]
    [InlineData(RazorFileKind.Component)]
    public async Task StringLiteral_TildePath_FallsBackToProjectRoot(RazorFileKind fileKind)
    {
        TestCode input = """
            @{
                var path = "~/js/si$$te.js";
            }
            """;

        await VerifyFilePathDefinitionAsync(
            input,
            fileKind,
            expectedFileName: "js/site.js",
            additionalFiles: [
                (FilePath("wwwroot/js/other.js"), ""),
                (FilePath("js/site.js"), "")]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/84945")]
    public async Task StringLiteral_RelativePath_AnyFileKind_Legacy()
    {
        TestCode input = """
            @{
                var path = "../images/lo$$go.png";
            }
            """;

        await VerifyFilePathDefinitionAsync(
            input,
            RazorFileKind.Legacy,
            expectedFileName: "Views/images/logo.png",
            additionalFiles: [
                (FilePath("Views/images/logo.png"), ""),
                (FilePath("wwwroot/images/logo.png"), "")],
            documentFilePath: FilePath("Views/Home/Index.cshtml"));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/84945")]
    public async Task StringLiteral_RelativePath_AnyFileKind_Component()
    {
        TestCode input = """
            @{
                var path = "../images/lo$$go.png";
            }
            """;

        await VerifyFilePathDefinitionAsync(
            input,
            RazorFileKind.Component,
            expectedFileName: "Views/images/logo.png",
            additionalFiles: [
                (FilePath("Views/images/logo.png"), ""),
                (FilePath("wwwroot/images/logo.png"), "")],
            documentFilePath: FilePath("Views/Home/Index.razor"));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/84945")]
    public async Task StringLiteral_TildePath_Backslashes()
    {
        TestCode input = """
            @{
                var path = @"~\images\lo$$go.png";
            }
            """;

        await VerifyFilePathDefinitionAsync(
            input,
            RazorFileKind.Legacy,
            expectedFileName: "wwwroot/images/logo.png",
            additionalFiles: [(FilePath("wwwroot/images/logo.png"), "")]);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/pull/86072")]
    [InlineData(RazorFileKind.Legacy)]
    [InlineData(RazorFileKind.Component)]
    public async Task StringLiteral_ProtocolRelativeUrl(RazorFileKind fileKind)
    {
        TestCode input = """
            @{
                var path = "//cdn.example.com/css/si$$te.css";
            }
            """;

        var document = CreateProjectAndRazorDocument(input.Text, fileKind);
        var fileSystem = SetFileSystem([]);

        var result = await GetGoToDefinitionResultCoreAsync(document, input, htmlResponse: null);

        Assert.Empty(fileSystem.FileExistsCalls);
        Assumes.NotNull(result);
        var location = Assert.Single(result.Value.Second);
        Assert.EndsWith("String.cs", location.DocumentUri.UriString);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/pull/86072")]
    [InlineData(RazorFileKind.Legacy, """@{ var path = "~/css/Rate%20Ca$$rd%25.css"; }""")]
    [InlineData(RazorFileKind.Component, """@{ var path = "~/css/Rate%20Ca$$rd%25.css"; }""")]
    [InlineData(RazorFileKind.Legacy, """<link href="~/css/Rate%20Ca$$rd%25.css" />""")]
    [InlineData(RazorFileKind.Component, """<link href="~/css/Rate%20Ca$$rd%25.css" />""")]
    public async Task FilePath_PercentEscapes_AreLiteral(RazorFileKind fileKind, string input)
    {
        TestCode testCode = input;
        var document = CreateProjectAndRazorDocument(testCode.Text, fileKind);
        var expectedPath = FilePath("wwwroot/css/Rate%20Card%25.css");
        var fileSystem = SetFileSystem([(expectedPath, ""), (FilePath("wwwroot/css/Rate Card%.css"), "")]);

        var result = await GetGoToDefinitionResultCoreAsync(document, testCode, htmlResponse: null);

        // The fake URL-decodes paths when comparing them, so also check the exact path it received.
        Assert.Equal(expectedPath, Assert.Single(fileSystem.FileExistsCalls));
        Assumes.NotNull(result);
        var location = Assert.Single(result.Value.Second);
        Assert.Equal(FileUri("wwwroot/css/Rate%20Card%25.css"), location.DocumentUri);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/84945")]
    [InlineData(RazorFileKind.Legacy, """<img src="~/images/lo$$go.png" />""")]
    [InlineData(RazorFileKind.Component, """<img src="~/images/lo$$go.png" />""")]
    [InlineData(RazorFileKind.Legacy, """<img src='~/images/lo$$go.png' />""")]
    [InlineData(RazorFileKind.Component, """<img src='~/images/lo$$go.png' />""")]
    [InlineData(RazorFileKind.Legacy, """<img src="~/$$images/logo.png" />""")]
    [InlineData(RazorFileKind.Component, """<img src="~/images/logo.png$$" />""")]
    public async Task LiteralAttribute_TildePath(RazorFileKind fileKind, string input)
    {
        await VerifyFilePathDefinitionAsync(
            input,
            fileKind,
            expectedFileName: "wwwroot/images/logo.png",
            additionalFiles: [(FilePath("wwwroot/images/logo.png"), "")]);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/84945")]
    [InlineData("site.css")]
    [InlineData("site styles.css")]
    public async Task LiteralAttribute_RelativePath_Legacy(string fileName)
    {
        TestCode input = $$"""<link href="css/$${{fileName}}" />""";

        await VerifyFilePathDefinitionAsync(
            input,
            RazorFileKind.Legacy,
            expectedFileName: $"Views/css/{fileName}",
            additionalFiles: [(FilePath($"Views/css/{fileName}"), "")],
            documentFilePath: FilePath("Views/Index.cshtml"));
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/84945")]
    [InlineData("site.css")]
    [InlineData("site styles.css")]
    public async Task LiteralAttribute_RelativePath_Component(string fileName)
    {
        TestCode input = $$"""<link href="css/$${{fileName}}" />""";

        await VerifyFilePathDefinitionAsync(
            input,
            RazorFileKind.Component,
            expectedFileName: $"Views/css/{fileName}",
            additionalFiles: [(FilePath($"Views/css/{fileName}"), "")],
            documentFilePath: FilePath("Views/Index.razor"));
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/84945")]
    [InlineData("logo.png")]
    [InlineData("site logo.png")]
    public async Task LiteralAttribute_UnmappedComponentParameter(string fileName)
    {
        TestCode input = $$"""<Image Source="~/images/$${{fileName}}" />""";

        var document = CreateProjectAndRazorDocument(input.Text, additionalFiles:
            [(FilePath("Image.cs"), """
                namespace Microsoft.AspNetCore.Components
                {
                    [System.AttributeUsage(System.AttributeTargets.Property)]
                    public sealed class AssetPathAttribute : System.Attribute { }
                }

                namespace SomeProject
                {
                    public class Image : Microsoft.AspNetCore.Components.ComponentBase
                    {
                        [Microsoft.AspNetCore.Components.Parameter]
                        [Microsoft.AspNetCore.Components.AssetPath]
                        public string Source { get; set; }
                    }
                }
                """)]);
        SetFileSystem([(FilePath($"wwwroot/images/{fileName}"), "")]);

        // Verify the ~/ rewrite is active so navigation must work without a C# source mapping.
        var snapshotManager = OOPExportProvider.GetExportedValue<RemoteSnapshotManager>();
        var codeDocument = await snapshotManager.GetSnapshot(document).GetGeneratedOutputAsync(DisposalToken);
        Assert.Contains($$"""Assets[@"images/{{fileName}}"]""", codeDocument.GetRequiredImplCSharpDocument().Text.ToString());

        var result = await GetGoToDefinitionResultCoreAsync(document, input, htmlResponse: null);

        Assumes.NotNull(result);
        var location = Assert.Single(result.Value.Second);
        Assert.Equal(FileUri($"wwwroot/images/{fileName}"), location.DocumentUri);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/84945")]
    [InlineData("""<img src="~/images/mi$$ssing.png" />""")]
    [InlineData("""<img src="~/images/@name/lo$$go.png" />""")]
    [InlineData("""<img sr$$c="~/images/logo.png" />""")]
    [InlineData("""<img src="https://example.com/lo$$go.png" />""")]
    public async Task LiteralAttribute_NotFileReference_CallsHtml(string input)
    {
        TestCode testCode = input;
        var document = CreateProjectAndRazorDocument(testCode.Text, RazorFileKind.Legacy);
        SetFileSystem([(FilePath("wwwroot/images/logo.png"), ""), (FilePath("wwwroot/images/@name/logo.png"), "")]);

        var response = await GetRemoteGoToDefinitionResponseAsync(document, testCode);

        Assert.Equal(RemoteResponse<GoToDefinitionResponse?>.CallHtml, response);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/pull/86072")]
    [InlineData(RazorFileKind.Legacy)]
    [InlineData(RazorFileKind.Component)]
    public async Task LiteralAttribute_ProtocolRelativeUrl_CallsHtml(RazorFileKind fileKind)
    {
        TestCode input = """<script src="//cdn.example.com/js/si$$te.js"></script>""";
        var document = CreateProjectAndRazorDocument(input.Text, fileKind);
        var fileSystem = SetFileSystem([]);

        var response = await GetRemoteGoToDefinitionResponseAsync(document, input);

        Assert.Empty(fileSystem.FileExistsCalls);
        Assert.Equal(RemoteResponse<GoToDefinitionResponse?>.CallHtml, response);
    }

    [Theory, WorkItem("https://github.com/dotnet/razor/issues/4325")]
    [InlineData("~/Pages/Counter")]
    [InlineData("Not a file")]
    [InlineData("~/Program.cs")]
    [InlineData("File.razor is cool")]
    [InlineData("~/images/missing.png")]
    [InlineData("https://example.com/site.css")]
    [InlineData("invalid\\0path")]
    public async Task StringLiteral_NotFileReference(string literalContents)
    {
        var input = $$"""
            @{
                var path = "$${{literalContents}}";
            }
            """;

        var result = await GetGoToDefinitionResultAsync(input);

        Assumes.NotNull(result);
        Assert.NotNull(result.Value.Second);
        var locations = result.Value.Second;
        var location = Assert.Single(locations);
        Assert.EndsWith("String.cs", location.DocumentUri.UriString);

        // Note: The location is in a generated C# "metadata-as-source" file, which has a different
        // number of using directives in .NET Framework vs. .NET Core, so rather than relying on line
        // numbers we do some vague notion of actual navigation and test the actual source line that
        // the user would see.
        var line = File.ReadLines(location.DocumentUri.GetRequiredSystemUri().LocalPath).ElementAt(location.Range.Start.Line);
        Assert.Contains("public sealed class String", line);
    }

    [Fact]
    public async Task ComponentAttribute_CrossProject()
    {
        // Note: This test doesn't simulate syncing solutions to the remote workspace, so strictly speaking is running in the "wrong" MEF composition
        // but thats not an important aspect of this scenario.

        var someProjectId = ProjectId.CreateNewId();
        var surveyPromptId = DocumentId.CreateNewId(someProjectId);
        TestCode surveyPrompt = """
            @namespace SomeProject

            <div></div>

            @code
            {
                [Parameter]
                public string [|Title|] { get; set; }
            }
            """;

        var anotherProjectId = ProjectId.CreateNewId();
        var componentId = DocumentId.CreateNewId(anotherProjectId);
        TestCode component = """
            @using SomeProject

            <File1 Ti$$tle="InputValue" />
            """;

        var solution = LocalWorkspace.CurrentSolution;
        var project1 = AddProjectAndRazorDocument(solution, TestProjectData.SomeProject.FilePath, someProjectId, surveyPromptId, TestProjectData.SomeProjectComponentFile1.FilePath, surveyPrompt.Text).Project;
        var project2 = AddProjectAndRazorDocument(project1.Solution, TestProjectData.AnotherProject.FilePath, anotherProjectId, componentId, TestProjectData.AnotherProjectComponentFile2.FilePath, component.Text).Project;
        project2 = project2.AddProjectReference(new ProjectReference(project1.Id));
        project1 = project2.Solution.GetRequiredProject(project1.Id);

        var surveyPromptDocument = project1.GetAdditionalDocument(surveyPromptId);
        Assert.NotNull(surveyPromptDocument);
        var componentDocument = project2.GetAdditionalDocument(componentId);
        Assert.NotNull(componentDocument);

        var result = await GetGoToDefinitionResultCoreAsync(componentDocument, component, htmlResponse: null);

        Assumes.NotNull(result);
        Assert.NotNull(result.Value.Second);
        var locations = result.Value.Second;
        var location = Assert.Single(locations);

        Assert.Equal(surveyPromptDocument.GetURI(), location.DocumentUri);
        var text = SourceText.From(surveyPrompt.Text);
        var range = text.GetRange(surveyPrompt.Span);
        Assert.Equal(range, location.Range);
    }

    private async Task VerifyFilePathDefinitionAsync(
        TestCode input,
        RazorFileKind fileKind,
        string expectedFileName,
        (string filePath, string contents)[] additionalFiles,
        string? documentFilePath = null)
    {
        var document = CreateProjectAndRazorDocument(input.Text, fileKind, documentFilePath: documentFilePath);
        SetFileSystem(additionalFiles);

        var result = await GetGoToDefinitionResultCoreAsync(document, input, htmlResponse: null);

        Assumes.NotNull(result);
        var location = Assert.Single(result.Value.Second);
        Assert.Equal(FileUri(expectedFileName), location.DocumentUri);
        Assert.Equal(LspFactory.DefaultRange, location.Range);
    }

    private TestFileSystem SetFileSystem((string filePath, string contents)[] files)
    {
        var testFileSystem = new TestFileSystem(files);
        var fileSystem = (RemoteFileSystem)OOPExportProvider.GetExportedValue<IFileSystem>();
        fileSystem.GetTestAccessor().SetFileSystem(testFileSystem);
        return testFileSystem;
    }

    private async Task VerifyGoToDefinitionAsync(
        TestCode input,
        RazorFileKind? fileKind = null,
        SumType<LspLocation, LspLocation[], DocumentLink[]>? htmlResponse = null,
        TextDocument? razorDocument = null)
    {
        var document = razorDocument ?? CreateProjectAndRazorDocument(input.Text, fileKind);
        var result = await GetGoToDefinitionResultCoreAsync(document, input, htmlResponse);

        Assumes.NotNull(result);

        Assert.NotNull(result.Value.Second);
        var locations = result.Value.Second;
        var location = Assert.Single(locations);

        var text = SourceText.From(input.Text);
        var range = text.GetRange(input.Span);
        Assert.Equal(range, location.Range);

        Assert.Equal(document.GetURI(), location.DocumentUri);
    }

    private async Task<SumType<LspLocation, LspLocation[], DocumentLink[]>?> GetGoToDefinitionResultAsync(
        TestCode input,
        RazorFileKind? fileKind = null,
        SumType<LspLocation, LspLocation[], DocumentLink[]>? htmlResponse = null,
        params (string fileName, string contents)[]? additionalFiles)
    {
        var document = CreateProjectAndRazorDocument(input.Text, fileKind, additionalFiles: additionalFiles);
        return await GetGoToDefinitionResultCoreAsync(document, input, htmlResponse);
    }

    private async Task<SumType<LspLocation, LspLocation[], DocumentLink[]>?> GetGoToDefinitionResultCoreAsync(
        TextDocument document, TestCode input, SumType<LspLocation, LspLocation[], DocumentLink[]>? htmlResponse)
    {
        var inputText = await document.GetTextAsync(DisposalToken);
        var position = inputText.GetPosition(input.Position);

        var requestInvoker = new TestHtmlRequestInvoker(
            htmlResponse is null
                ? []
                : [(Methods.TextDocumentDefinitionName, htmlResponse)]);

        var endpoint = new CohostGoToDefinitionEndpoint(IncompatibleProjectService, RemoteServiceInvoker, requestInvoker);

        var textDocumentPositionParams = new TextDocumentPositionParams
        {
            Position = position,
            TextDocument = new TextDocumentIdentifier { DocumentUri = document.GetURI() },
        };

        return await endpoint.GetTestAccessor().HandleRequestAsync(textDocumentPositionParams, document, DisposalToken);
    }

    private async Task<RemoteResponse<GoToDefinitionResponse?>> GetRemoteGoToDefinitionResponseAsync(TextDocument document, TestCode input)
    {
        var inputText = await document.GetTextAsync(DisposalToken);
        var position = inputText.GetPosition(input.Position);

        return await RemoteServiceInvoker.TryInvokeAsync<IRemoteGoToDefinitionService, RemoteResponse<GoToDefinitionResponse?>>(
            document.Project.Solution,
            (service, solutionInfo, cancellationToken) =>
                service.GetDefinitionsAsync(solutionInfo, document.Id, position, cancellationToken),
            DisposalToken);
    }
}
