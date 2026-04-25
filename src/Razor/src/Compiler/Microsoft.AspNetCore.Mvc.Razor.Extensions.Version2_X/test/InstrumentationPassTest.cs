// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;
using static Microsoft.AspNetCore.Razor.Language.Intermediate.IntermediateNodeAssert;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X;

public class InstrumentationPassTest : RazorProjectEngineTestBase
{
    protected override RazorLanguageVersion Version => RazorLanguageVersion.Version_2_1;

    [Fact]
    public void InstrumentationPass_NoOps_ForDesignTime()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateEmptyDesignTimeCodeDocument();
        var documentNode = new DocumentIntermediateNode() { Options = codeDocument.CodeGenerationOptions };

        var builder = IntermediateNodeBuilder.Create(documentNode);

        builder.Push(new HtmlContentIntermediateNode());

        builder.Add(IntermediateNodeFactory.HtmlToken("Hi"));

        builder.Pop();

        // Act
        ProjectEngine.ExecutePass<InstrumentationPass>(codeDocument, documentNode);

        // Assert
        Children(
            documentNode,
            n => IntermediateNodeAssert.Html("Hi", n));
    }

    [Fact]
    public void InstrumentationPass_InstrumentsHtml()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateEmptyCodeDocument();
        var documentNode = new DocumentIntermediateNode() { Options = codeDocument.CodeGenerationOptions };

        var builder = IntermediateNodeBuilder.Create(documentNode);

        builder.Push(new HtmlContentIntermediateNode()
        {
            Source = CreateSource(1),
        });

        builder.Add(IntermediateNodeFactory.HtmlToken("Hi", CreateSource(1)));

        builder.Pop();

        // Act
        ProjectEngine.ExecutePass<InstrumentationPass>(codeDocument, documentNode);

        // Assert
        Children(
            documentNode,
            n => BeginInstrumentation("1, 1, true", n),
            n => IntermediateNodeAssert.Html("Hi", n),
            n => EndInstrumentation(n));
    }

    [Fact]
    public void InstrumentationPass_SkipsHtml_WithoutLocation()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateEmptyCodeDocument();
        var documentNode = new DocumentIntermediateNode() { Options = codeDocument.CodeGenerationOptions };

        var builder = IntermediateNodeBuilder.Create(documentNode);

        builder.Push(new HtmlContentIntermediateNode());

        builder.Add(IntermediateNodeFactory.HtmlToken("Hi"));

        builder.Pop();

        // Act
        ProjectEngine.ExecutePass<InstrumentationPass>(codeDocument, documentNode);

        // Assert
        Children(
            documentNode,
            n => IntermediateNodeAssert.Html("Hi", n));
    }

    [Fact]
    public void InstrumentationPass_InstrumentsCSharpExpression()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateEmptyCodeDocument();
        var documentNode = new DocumentIntermediateNode() { Options = codeDocument.CodeGenerationOptions };

        var builder = IntermediateNodeBuilder.Create(documentNode);

        builder.Push(new CSharpExpressionIntermediateNode()
        {
            Source = CreateSource(2),
        });

        builder.Add(IntermediateNodeFactory.CSharpToken("Hi"));

        // Act
        ProjectEngine.ExecutePass<InstrumentationPass>(codeDocument, documentNode);

        // Assert
        Children(
            documentNode,
            n => BeginInstrumentation("2, 2, false", n),
            n => CSharpExpression("Hi", n),
            n => EndInstrumentation(n));
    }

    [Fact]
    public void InstrumentationPass_SkipsCSharpExpression_WithoutLocation()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateEmptyCodeDocument();
        var documentNode = new DocumentIntermediateNode() { Options = codeDocument.CodeGenerationOptions };

        var builder = IntermediateNodeBuilder.Create(documentNode);

        builder.Push(new CSharpExpressionIntermediateNode());

        builder.Add(IntermediateNodeFactory.CSharpToken("Hi"));

        // Act
        ProjectEngine.ExecutePass<InstrumentationPass>(codeDocument, documentNode);

        // Assert
        Children(
            documentNode,
            n => CSharpExpression("Hi", n));
    }

    [Fact]
    public void InstrumentationPass_SkipsCSharpExpression_InsideTagHelperAttribute()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateEmptyCodeDocument();
        var documentNode = new DocumentIntermediateNode() { Options = codeDocument.CodeGenerationOptions };

        var builder = IntermediateNodeBuilder.Create(documentNode);

        builder.Push(new TagHelperIntermediateNode()
        {
            TagMode = 0,
            TagName = "Test"
        });

        builder.Push(new TagHelperHtmlAttributeIntermediateNode()
        {
            AttributeName = "Test",
            AttributeStructure = 0
        });

        builder.Push(new CSharpExpressionIntermediateNode()
        {
            Source = CreateSource(5)
        });

        builder.Add(IntermediateNodeFactory.CSharpToken("Hi"));

        // Act
        ProjectEngine.ExecutePass<InstrumentationPass>(codeDocument, documentNode);

        // Assert
        Children(
            documentNode,
            n =>
            {
                Assert.IsType<TagHelperIntermediateNode>(n);
                Children(
                    n,
                    c =>
                    {
                        Assert.IsType<TagHelperHtmlAttributeIntermediateNode>(c);
                        Children(
                            c,
                            s => CSharpExpression("Hi", s));
                    });
            });
    }

    [Fact]
    public void InstrumentationPass_SkipsCSharpExpression_InsideTagHelperProperty()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateEmptyCodeDocument();
        var documentNode = new DocumentIntermediateNode() { Options = codeDocument.CodeGenerationOptions };

        var builder = IntermediateNodeBuilder.Create(documentNode);

        builder.Push(new TagHelperIntermediateNode()
        {
            TagMode = 0,
            TagName = "Test"
        });

        builder.Push(new TagHelperPropertyIntermediateNode(match: default)
        {
            AttributeName = "Test",
            AttributeStructure = 0
        });

        builder.Push(new CSharpExpressionIntermediateNode()
        {
            Source = CreateSource(5)
        });

        builder.Add(IntermediateNodeFactory.CSharpToken("Hi"));

        // Act
        ProjectEngine.ExecutePass<InstrumentationPass>(codeDocument, documentNode);

        // Assert
        Children(
            documentNode,
            n =>
            {
                Assert.IsType<TagHelperIntermediateNode>(n);
                Children(
                    n,
                    c =>
                    {
                        Assert.IsType<TagHelperPropertyIntermediateNode>(c);
                        Children(
                            c,
                            s => CSharpExpression("Hi", s));
                    });
            });
    }

    [Fact]
    public void InstrumentationPass_InstrumentsTagHelper()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateEmptyCodeDocument();
        var documentNode = new DocumentIntermediateNode() { Options = codeDocument.CodeGenerationOptions };

        var builder = IntermediateNodeBuilder.Create(documentNode);

        builder.Add(new TagHelperIntermediateNode()
        {
            TagMode = 0,
            TagName = "Test",
            Source = CreateSource(3)
        });

        // Act
        ProjectEngine.ExecutePass<InstrumentationPass>(codeDocument, documentNode);

        // Assert
        Children(
            documentNode,
            n => BeginInstrumentation("3, 3, false", n),
            n => Assert.IsType<TagHelperIntermediateNode>(n),
            n => EndInstrumentation(n));
    }

    [Fact]
    public void InstrumentationPass_SkipsTagHelper_WithoutLocation()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateEmptyCodeDocument();
        var documentNode = new DocumentIntermediateNode() { Options = codeDocument.CodeGenerationOptions };

        var builder = IntermediateNodeBuilder.Create(documentNode);

        builder.Push(new TagHelperIntermediateNode()
        {
            TagMode = 0,
            TagName = "Test"
        });

        // Act
        ProjectEngine.ExecutePass<InstrumentationPass>(codeDocument, documentNode);

        // Assert
        Children(
            documentNode,
            n => Assert.IsType<TagHelperIntermediateNode>(n));
    }

    private static SourceSpan CreateSource(int number)
    {
        // The actual source span doesn't really matter, we just want to see the values used.
        return new SourceSpan(new SourceLocation(number, number, number), number);
    }
}
