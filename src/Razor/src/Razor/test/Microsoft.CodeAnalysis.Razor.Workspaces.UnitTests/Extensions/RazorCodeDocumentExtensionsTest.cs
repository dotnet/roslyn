// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Workspaces.Test.Extensions;

public class RazorCodeDocumentExtensionsTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [Fact]
    public void GetLanguageKind_TagHelperElementOwnsName()
    {
        // Arrange
        var descriptor = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "TestAssembly");
        descriptor.TypeName = "TestTagHelper";
        descriptor.TagMatchingRule(rule => rule.TagName = "test");

        TestCode code = """
            @addTagHelper *, TestAssembly
            <te$$st>@Name</test>
            """;

        var codeDocument = CreateCodeDocument(code, descriptor.Build());

        // Act
        var languageKind = codeDocument.GetLanguageKind(code.Position, rightAssociative: false);

        // Assert
        Assert.Equal(RazorLanguageKind.Html, languageKind);
    }

    [Fact]
    public void GetLanguageKind_TagHelpersDoNotOwnTrailingEdge()
    {
        // Arrange
        var descriptor = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "TestAssembly");
        descriptor.TypeName = "TestTagHelper";
        descriptor.TagMatchingRule(rule => rule.TagName = "test");

        TestCode code = """
            @addTagHelper *, TestAssembly
            <test></test>$$@DateTime.Now
            """;

        var codeDocument = CreateCodeDocument(code, descriptor.Build());

        // Act
        var languageKind = codeDocument.GetLanguageKind(code.Position, rightAssociative: false);

        // Assert
        Assert.Equal(RazorLanguageKind.Razor, languageKind);
    }

    [Fact]
    public void GetLanguageKind_TagHelperNestedCSharpAttribute()
    {
        // Arrange
        var descriptor = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "TestAssembly");
        descriptor.TypeName = "TestTagHelper";
        descriptor.TagMatchingRule(rule => rule.TagName = "test");
        descriptor.BindAttribute(builder =>
        {
            builder.Name = "asp-int";
            builder.TypeName = typeof(int).FullName;
            builder.PropertyName = "AspInt";
        });

        TestCode code = """
            @addTagHelper *, TestAssembly
            <test asp-int='12$$3'></test>
            """;

        var codeDocument = CreateCodeDocument(code, descriptor.Build());

        // Act
        var languageKind = codeDocument.GetLanguageKind(code.Position, rightAssociative: false);

        // Assert
        Assert.Equal(RazorLanguageKind.CSharp, languageKind);
    }

    [Fact]
    public void GetLanguageKind_CSharp()
    {
        // Arrange
        TestCode code = "<p>@N$$ame</p>";
        var codeDocument = CreateCodeDocument(code);

        // Act
        var languageKind = codeDocument.GetLanguageKind(code.Position, rightAssociative: false);

        // Assert
        Assert.Equal(RazorLanguageKind.CSharp, languageKind);
    }

    [Fact]
    public void GetLanguageKind_Html()
    {
        // Arrange
        TestCode code = "<p>He$$llo World</p>";
        var codeDocument = CreateCodeDocument(code);

        // Act
        var languageKind = codeDocument.GetLanguageKind(code.Position, rightAssociative: false);

        // Assert
        Assert.Equal(RazorLanguageKind.Html, languageKind);
    }

    [Fact]
    public void GetLanguageKind_DefaultsToRazorLanguageIfCannotLocateOwner()
    {
        // Arrange
        TestCode code = "<p>Hello World</p>$$";
        var codeDocument = CreateCodeDocument(code);

        // Act
        var languageKind = codeDocument.GetLanguageKind(code.Position + 1, rightAssociative: false);

        // Assert
        Assert.Equal(RazorLanguageKind.Razor, languageKind);
    }

    [Fact]
    public void GetLanguageKind_GetsLastClassifiedSpanLanguageIfAtEndOfDocument()
    {
        // Arrange
        TestCode code = """
            <strong>Something</strong>
            <App>$$
            """;

        var codeDocument = CreateCodeDocument(code);

        // Act
        var languageKind = codeDocument.GetLanguageKind(code.Position, rightAssociative: false);

        // Assert
        Assert.Equal(RazorLanguageKind.Html, languageKind);
    }

    [Fact]
    public void GetLanguageKind_HtmlEdgeEnd()
    {
        // Arrange
        TestCode code = "Hello World$$";
        var codeDocument = CreateCodeDocument(code);

        // Act
        var languageKind = codeDocument.GetLanguageKind(code.Position, rightAssociative: false);

        // Assert
        Assert.Equal(RazorLanguageKind.Html, languageKind);
    }

    [Fact]
    public void GetLanguageKind_CSharpEdgeEnd()
    {
        // Arrange
        TestCode code = "@Name$$";
        var codeDocument = CreateCodeDocument(code);

        // Act
        var languageKind = codeDocument.GetLanguageKind(code.Position, rightAssociative: false);

        // Assert
        Assert.Equal(RazorLanguageKind.CSharp, languageKind);
    }

    [Fact]
    public void GetLanguageKind_RazorEdgeWithCSharp()
    {
        // Arrange
        TestCode code = "@{$$}";
        var codeDocument = CreateCodeDocument(code);

        // Act
        var languageKind = codeDocument.GetLanguageKind(code.Position, rightAssociative: false);

        // Assert
        Assert.Equal(RazorLanguageKind.CSharp, languageKind);
    }

    [Fact]
    public void GetLanguageKind_CSharpEdgeWithCSharpMarker()
    {
        // Arrange
        TestCode code = "@{var x = 1;$$}";
        var codeDocument = CreateCodeDocument(code);

        // Act
        var languageKind = codeDocument.GetLanguageKind(code.Position, rightAssociative: false);

        // Assert
        Assert.Equal(RazorLanguageKind.CSharp, languageKind);
    }

    [Fact]
    public void GetLanguageKind_ExplicitExpressionStartCSharp()
    {
        // Arrange
        TestCode code = "@($$)";
        var codeDocument = CreateCodeDocument(code);

        // Act
        var languageKind = codeDocument.GetLanguageKind(code.Position, rightAssociative: false);

        // Assert
        Assert.Equal(RazorLanguageKind.CSharp, languageKind);
    }

    [Fact]
    public void GetLanguageKind_ExplicitExpressionInProgressCSharp()
    {
        // Arrange
        TestCode code = "@(Da$$)";
        var codeDocument = CreateCodeDocument(code);

        // Act
        var languageKind = codeDocument.GetLanguageKind(code.Position, rightAssociative: false);

        // Assert
        Assert.Equal(RazorLanguageKind.CSharp, languageKind);
    }

    [Fact]
    public void GetLanguageKind_ImplicitExpressionStartCSharp()
    {
        // Arrange
        TestCode code = "@$$";
        var codeDocument = CreateCodeDocument(code);

        // Act
        var languageKind = codeDocument.GetLanguageKind(code.Position, rightAssociative: false);

        // Assert
        Assert.Equal(RazorLanguageKind.CSharp, languageKind);
    }

    [Fact]
    public void GetLanguageKind_ImplicitExpressionInProgressCSharp()
    {
        // Arrange
        TestCode code = "@Da$$";
        var codeDocument = CreateCodeDocument(code);

        // Act
        var languageKind = codeDocument.GetLanguageKind(code.Position, rightAssociative: false);

        // Assert
        Assert.Equal(RazorLanguageKind.CSharp, languageKind);
    }

    [Fact]
    public void GetLanguageKind_RazorEdgeWithHtml()
    {
        // Arrange
        TestCode code = "@{$$<br />}";
        var codeDocument = CreateCodeDocument(code);

        // Act
        var languageKind = codeDocument.GetLanguageKind(code.Position, rightAssociative: false);

        // Assert
        Assert.Equal(RazorLanguageKind.Html, languageKind);
    }

    [Fact]
    public void GetLanguageKind_HtmlAtCSharpBraceLeftAssociative()
    {
        // Arrange
        TestCode code = "@if (true) {$$ <br /> }";
        var codeDocument = CreateCodeDocument(code);

        // Act
        var languageKind = codeDocument.GetLanguageKind(code.Position, rightAssociative: false);

        // Assert
        Assert.Equal(RazorLanguageKind.CSharp, languageKind);
    }

    [Fact]
    public void GetLanguageKind_HtmlAtCSharpBraceRightAssociative()
    {
        // Arrange
        TestCode code = "@if (true) {$$ <br /> }";
        var codeDocument = CreateCodeDocument(code);

        // Act
        var languageKind = codeDocument.GetLanguageKind(code.Position, rightAssociative: true);

        // Assert
        Assert.Equal(RazorLanguageKind.Html, languageKind);
    }

    [Fact]
    public void GetLanguageKind_HtmlInCSharpLeftAssociative()
    {
        // Arrange
        TestCode code = "@if (true) { $$<br /> }";
        var codeDocument = CreateCodeDocument(code);

        // Act
        var languageKind = codeDocument.GetLanguageKind(code.Position, rightAssociative: false);

        // Assert
        Assert.Equal(RazorLanguageKind.Html, languageKind);
    }

    [Fact]
    public void GetLanguageKind_HtmlInCSharpRightAssociative()
    {
        // Arrange
        TestCode code = "@if (true) { $$<br /> }";
        var codeDocument = CreateCodeDocument(code);

        // Act
        var languageKind = codeDocument.GetLanguageKind(code.Position, rightAssociative: true);

        // Assert
        Assert.Equal(RazorLanguageKind.Html, languageKind);
    }

    [Fact]
    public void GetLanguageKind_TagHelperInCSharpRightAssociative()
    {
        // Arrange
        var descriptor = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "TestAssembly");
        descriptor.TypeName = "TestTagHelper";
        descriptor.TagMatchingRule(rule => rule.TagName = "test");

        TestCode code = """
            @addTagHelper *, TestAssembly
            @if {
                $$<test>@Name</test>
            }
            """;

        var codeDocument = CreateCodeDocument(code, descriptor.Build());

        // Act
        var languageKind = codeDocument.GetLanguageKind(code.Position, rightAssociative: true);

        // Assert
        Assert.Equal(RazorLanguageKind.Html, languageKind);
    }

    private static RazorCodeDocument CreateCodeDocument(TestCode code, params TagHelperCollection tagHelpers)
    {
        tagHelpers ??= [];

        var sourceDocument = TestRazorSourceDocument.Create(code.Text);
        var projectEngine = RazorProjectEngine.Create(builder =>
        {
            builder.ConfigureParserOptions(builder =>
            {
                builder.UseRoslynTokenizer = true;
            });
        });

        return projectEngine.Process(sourceDocument, RazorFileKind.Legacy, importSources: default, tagHelpers);
    }
}
