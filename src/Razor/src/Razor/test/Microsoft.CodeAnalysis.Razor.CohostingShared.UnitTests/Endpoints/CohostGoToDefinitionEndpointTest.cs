// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Protocol;
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
        var input = """
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

        await VerifyGoToDefinitionAsync(input);
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
        var input = """
            <div></div>
            @functions
            {
                private stri$$ng _name;
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
        var line = File.ReadLines(location.DocumentUri.GetRequiredParsedUri().LocalPath).ElementAt(location.Range.Start.Line);
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

        Assert.Equal(FileUri("SurveyPrompt.razor"), location.DocumentUri.GetRequiredParsedUri());
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

        Assert.Equal(FileUri("SurveyPrompt.razor"), location.DocumentUri.GetRequiredParsedUri());
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

        Assert.Equal(FileUri("SurveyPrompt.razor"), location.DocumentUri.GetRequiredParsedUri());
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

        Assert.Equal(FileUri("SurveyPrompt.razor"), location.DocumentUri.GetRequiredParsedUri());
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

        Assert.Equal(FileUri("File1.razor"), location.DocumentUri.GetRequiredParsedUri());
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

        Assert.Equal(FileUri("SurveyPrompt.cs"), location.DocumentUri.GetRequiredParsedUri());
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

        Assert.Equal(FileUri("SurveyPrompt.cs"), location.DocumentUri.GetRequiredParsedUri());
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

        var htmlResponse = new SumType<LspLocation, LspLocation[], DocumentLink[]>?(new LspLocation[]
        {
            new() {
                DocumentUri = new(new Uri(document.CreateUri(), document.Name + LanguageServerConstants.HtmlVirtualDocumentSuffix)),
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

        Assert.Equal(FileUri("AuthorViewComponent.cs"), location.DocumentUri.GetRequiredParsedUri());
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

        Assert.Equal(FileUri("AboutBoxTagHelper.cs"), location.DocumentUri.GetRequiredParsedUri());
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

        Assert.Equal(FileUri("AboutBoxTagHelper_1.cs"), location.DocumentUri.GetRequiredParsedUri());
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

        Assert.Equal(FileUri("AboutBoxTagHelper.cs"), location.DocumentUri.GetRequiredParsedUri());
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

        Assert.Equal(FileUri("AboutBoxTagHelper_2.cs"), location.DocumentUri.GetRequiredParsedUri());
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

        Assert.Equal(FileUri("FooTagHelper.cs"), location.DocumentUri.GetRequiredParsedUri());
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

        Assert.Equal(FileUri("FooTagHelper.cs"), location.DocumentUri.GetRequiredParsedUri());
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
        Assert.Equal(FileUri("TagHelpers.cs"), location.DocumentUri.GetRequiredParsedUri());
        Assert.Equal(expected.Spans[0], SourceText.From(expected.Text).GetTextSpan(location.Range));

        location = locations[1];
        Assert.Equal(FileUri("TagHelpers.cs"), location.DocumentUri.GetRequiredParsedUri());
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

        Assert.Equal(FileUri("Views/Shared/_Partial.cshtml"), location.DocumentUri.GetRequiredParsedUri());
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

        Assert.Equal(FileUri("_Partial.cshtml"), location.DocumentUri.GetRequiredParsedUri());
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

        Assert.Equal(FileUri("Pages/Counter.razor"), location.DocumentUri.GetRequiredParsedUri());
    }

    [Theory, WorkItem("https://github.com/dotnet/razor/issues/4325")]
    [InlineData("~/Pages/Counter")]
    [InlineData("Not a file")]
    [InlineData("~/Program.cs")]
    [InlineData("File.razor is cool")]
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
        var line = File.ReadLines(location.DocumentUri.GetRequiredParsedUri().LocalPath).ElementAt(location.Range.Start.Line);
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

        Assert.Equal(surveyPromptDocument.CreateUri(), location.DocumentUri.GetRequiredParsedUri());
        var text = SourceText.From(surveyPrompt.Text);
        var range = text.GetRange(surveyPrompt.Span);
        Assert.Equal(range, location.Range);
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

        Assert.Equal(document.CreateUri(), location.DocumentUri.GetRequiredParsedUri());
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

        var endpoint = new CohostGoToDefinitionEndpoint(IncompatibleProjectService, RemoteServiceInvoker, requestInvoker, FilePathService);

        var textDocumentPositionParams = new TextDocumentPositionParams
        {
            Position = position,
            TextDocument = new TextDocumentIdentifier { DocumentUri = document.CreateDocumentUri() },
        };

        return await endpoint.GetTestAccessor().HandleRequestAsync(textDocumentPositionParams, document, DisposalToken);
    }
}
