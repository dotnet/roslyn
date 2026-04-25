// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

public class DesignTimeNodeWriterTest : RazorProjectEngineTestBase
{
    protected override RazorLanguageVersion Version => RazorLanguageVersion.Latest;

    protected override void ConfigureCodeDocumentProcessor(RazorCodeDocumentProcessor processor)
    {
        processor.ExecutePhasesThrough<DefaultTagHelperResolutionPhase>();
    }

    [Fact]
    public void WriteUsingDirective_NoSource_WritesContent()
    {
        // Arrange
        var writer = DesignTimeNodeWriter.Instance;
        using var context = TestCodeRenderingContext.CreateDesignTime();

        var node = new UsingDirectiveIntermediateNode()
        {
            Content = "System"
        };

        // Act
        writer.WriteUsingDirective(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"using System;
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteUsingDirective_WithSource_WritesContentWithLinePragmaAndMapping()
    {
        // Arrange
        var writer = DesignTimeNodeWriter.Instance;
        using var context = TestCodeRenderingContext.CreateDesignTime();

        var originalSpan = new SourceSpan("test.cshtml", 0, 0, 0, 6);
        var generatedSpan = new SourceSpan(null, 38 + Environment.NewLine.Length * 3, 3, 0, 6);
        var expectedSourceMapping = new SourceMapping(originalSpan, generatedSpan);
        var node = new UsingDirectiveIntermediateNode()
        {
            Content = "System",
            Source = originalSpan
        };

        // Act
        writer.WriteUsingDirective(context, node);

        // Assert
        var mapping = Assert.Single(context.GetSourceMappings());
        Assert.Equal(expectedSourceMapping, mapping);

        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"
#nullable restore
#line 1 ""test.cshtml""
using System;

#nullable disable
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteUsingDirective_WithSourceAndLineDirectives_WritesContentWithLinePragmaAndMapping()
    {
        // Arrange
        var writer = DesignTimeNodeWriter.Instance;
        using var context = TestCodeRenderingContext.CreateDesignTime();

        var originalSpan = new SourceSpan("test.cshtml", 0, 0, 0, 6);
        var generatedSpan = new SourceSpan(null, 38 + Environment.NewLine.Length * 3, 3, 0, 6);
        var expectedSourceMapping = new SourceMapping(originalSpan, generatedSpan);
        var node = new UsingDirectiveIntermediateNode()
        {
            Content = "System",
            Source = originalSpan,
            AppendLineDefaultAndHidden = true
        };

        // Act
        writer.WriteUsingDirective(context, node);

        // Assert
        var mapping = Assert.Single(context.GetSourceMappings());
        Assert.Equal(expectedSourceMapping, mapping);

        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"
#nullable restore
#line 1 ""test.cshtml""
using System;

#line default
#line hidden
#nullable disable
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteCSharpExpression_SkipsLinePragma_WithoutSource()
    {
        // Arrange
        var writer = DesignTimeNodeWriter.Instance;
        using var context = TestCodeRenderingContext.CreateDesignTime();

        var node = new CSharpExpressionIntermediateNode();
        var builder = IntermediateNodeBuilder.Create(node);
        builder.Add(IntermediateNodeFactory.CSharpToken("i++"));

        // Act
        writer.WriteCSharpExpression(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"__o = i++;
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteCSharpExpression_WritesLinePragma_WithSource()
    {
        // Arrange
        var writer = DesignTimeNodeWriter.Instance;
        using var context = TestCodeRenderingContext.CreateDesignTime();

        var node = new CSharpExpressionIntermediateNode()
        {
            Source = new SourceSpan("test.cshtml", 0, 0, 0, 3),
        };

        var builder = IntermediateNodeBuilder.Create(node);

        builder.Add(IntermediateNodeFactory.CSharpToken("i++"));

        // Act
        writer.WriteCSharpExpression(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"
#nullable restore
#line 1 ""test.cshtml""
__o = i++;

#line default
#line hidden
#nullable disable
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteCSharpExpression_WithExtensionNode_WritesPadding()
    {
        // Arrange
        var writer = DesignTimeNodeWriter.Instance;
        using var context = TestCodeRenderingContext.CreateDesignTime();

        var node = new CSharpExpressionIntermediateNode();
        var builder = IntermediateNodeBuilder.Create(node);

        builder.Add(IntermediateNodeFactory.CSharpToken("i"));

        builder.Add(new MyExtensionIntermediateNode());

        builder.Add(IntermediateNodeFactory.CSharpToken("++"));

        // Act
        writer.WriteCSharpExpression(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"__o = iRender Children
++;
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteCSharpExpression_WithSource_WritesPadding()
    {
        // Arrange
        var writer = DesignTimeNodeWriter.Instance;
        using var context = TestCodeRenderingContext.CreateDesignTime();

        var node = new CSharpExpressionIntermediateNode()
        {
            Source = new SourceSpan("test.cshtml", 8, 0, 8, 3),
        };

        var builder = IntermediateNodeBuilder.Create(node);
        builder.Add(IntermediateNodeFactory.CSharpToken("i"));

        builder.Add(new MyExtensionIntermediateNode());

        builder.Add(IntermediateNodeFactory.CSharpToken("++"));

        // Act
        writer.WriteCSharpExpression(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"
#nullable restore
#line 1 ""test.cshtml""
  __o = iRender Children
++;

#line default
#line hidden
#nullable disable
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteCSharpCode_WhitespaceContentWithSource_WritesContent()
    {
        // Arrange
        var writer = DesignTimeNodeWriter.Instance;
        using var context = TestCodeRenderingContext.CreateDesignTime();

        var node = new CSharpCodeIntermediateNode()
        {
            Source = new SourceSpan("test.cshtml", 0, 0, 0, 3)
        };

        IntermediateNodeBuilder.Create(node)
            .Add(IntermediateNodeFactory.CSharpToken("    "));

        // Act
        writer.WriteCSharpCode(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"
#nullable restore
#line 1 ""test.cshtml""
    

#line default
#line hidden
#nullable disable
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteCSharpCode_SkipsLinePragma_WithoutSource()
    {
        // Arrange
        var writer = DesignTimeNodeWriter.Instance;
        using var context = TestCodeRenderingContext.CreateDesignTime();

        var node = new CSharpCodeIntermediateNode();
        IntermediateNodeBuilder.Create(node)
            .Add(IntermediateNodeFactory.CSharpToken("if (true) { }"));

        // Act
        writer.WriteCSharpCode(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"if (true) { }
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteCSharpCode_WritesLinePragma_WithSource()
    {
        // Arrange
        var writer = DesignTimeNodeWriter.Instance;
        using var context = TestCodeRenderingContext.CreateDesignTime();

        var node = new CSharpCodeIntermediateNode()
        {
            Source = new SourceSpan("test.cshtml", 0, 0, 0, 13)
        };

        IntermediateNodeBuilder.Create(node)
            .Add(IntermediateNodeFactory.CSharpToken("if (true) { }"));

        // Act
        writer.WriteCSharpCode(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"
#nullable restore
#line 1 ""test.cshtml""
if (true) { }

#line default
#line hidden
#nullable disable
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteCSharpCode_WritesPadding_WithSource()
    {
        // Arrange
        var writer = DesignTimeNodeWriter.Instance;
        using var context = TestCodeRenderingContext.CreateDesignTime();

        var node = new CSharpCodeIntermediateNode()
        {
            Source = new SourceSpan("test.cshtml", 0, 0, 0, 17)
        };

        IntermediateNodeBuilder.Create(node)
            .Add(IntermediateNodeFactory.CSharpToken("    if (true) { }"));

        // Act
        writer.WriteCSharpCode(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"
#nullable restore
#line 1 ""test.cshtml""
    if (true) { }

#line default
#line hidden
#nullable disable
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteCSharpExpressionAttributeValue_RendersCorrectly()
    {
        var writer = DesignTimeNodeWriter.Instance;

        var content = "<input checked=\"hello-world @false\" />";
        var source = TestRazorSourceDocument.Create(content);
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var processor = CreateCodeDocumentProcessor(codeDocument);
        var documentNode = processor.GetDocumentNode();
        var node = (CSharpExpressionAttributeValueIntermediateNode)FindDescendant<HtmlAttributeIntermediateNode>(documentNode).Children[1];

        using var context = TestCodeRenderingContext.CreateDesignTime(source: source);

        // Act
        writer.WriteCSharpExpressionAttributeValue(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"
#nullable restore
#line 1 ""test.cshtml""
                       __o = false;

#line default
#line hidden
#nullable disable
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteCSharpCodeAttributeValue_RendersCorrectly()
    {
        var writer = DesignTimeNodeWriter.Instance;
        var content = "<input checked=\"hello-world @if(@true){ }\" />";
        var sourceDocument = TestRazorSourceDocument.Create(content);
        var codeDocument = ProjectEngine.CreateCodeDocument(sourceDocument);
        var processor = CreateCodeDocumentProcessor(codeDocument);
        var documentNode = processor.GetDocumentNode();
        var node = (CSharpCodeAttributeValueIntermediateNode)FindDescendant<HtmlAttributeIntermediateNode>(documentNode).Children[1];

        using var context = TestCodeRenderingContext.CreateDesignTime(source: sourceDocument);

        // Act
        writer.WriteCSharpCodeAttributeValue(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"
#nullable restore
#line 1 ""test.cshtml""
                             if(@true){ }

#line default
#line hidden
#nullable disable
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteCSharpCodeAttributeValue_WithExpression_RendersCorrectly()
    {
        var writer = DesignTimeNodeWriter.Instance;
        var content = "<input checked=\"hello-world @if(@true){ @false }\" />";
        var source = TestRazorSourceDocument.Create(content);
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var processor = CreateCodeDocumentProcessor(codeDocument);
        var documentNode = processor.GetDocumentNode();
        var node = (CSharpCodeAttributeValueIntermediateNode)FindDescendant<HtmlAttributeIntermediateNode>(documentNode).Children[1];

        using var context = TestCodeRenderingContext.CreateDesignTime(source: source);

        // Act
        writer.WriteCSharpCodeAttributeValue(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"
#nullable restore
#line 1 ""test.cshtml""
                             if(@true){ 

#line default
#line hidden
#nullable disable
Render Children
#nullable restore
#line 1 ""test.cshtml""
                                               }

#line default
#line hidden
#nullable disable
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [ConditionalTheory(Is.Windows)]
    [InlineData(@"test.cshtml",                     @"test.cshtml")]
    [InlineData(@"pages/test.cshtml",               @"pages\test.cshtml")]
    [InlineData(@"pages\test.cshtml",               @"pages\test.cshtml")]
    [InlineData(@"c:/pages/test.cshtml",            @"c:\pages\test.cshtml")]
    [InlineData(@"c:\pages\test.cshtml",            @"c:\pages\test.cshtml")]
    [InlineData(@"c:/pages with space/test.cshtml", @"c:\pages with space\test.cshtml")]
    [InlineData(@"c:\pages with space\test.cshtml", @"c:\pages with space\test.cshtml")]
    [InlineData(@"//SERVER/pages/test.cshtml",      @"\\SERVER\pages\test.cshtml")]
    [InlineData(@"\\SERVER/pages\test.cshtml",      @"\\SERVER\pages\test.cshtml")]
    public void LinePragma_Is_Adjusted_On_Windows(string fileName, string expectedFileName)
    {
        var writer = DesignTimeNodeWriter.Instance;
        using var context = TestCodeRenderingContext.CreateDesignTime();

        Assert.True(context.Options.RemapLinePragmaPathsOnWindows);

        var node = new CSharpExpressionIntermediateNode()
        {
            Source = new SourceSpan(fileName, 0, 0, 0, 3)
        };

        var builder = IntermediateNodeBuilder.Create(node);
        builder.Add(IntermediateNodeFactory.CSharpToken("i++"));

        writer.WriteCSharpExpression(context, node);

        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
            $"""

            #nullable restore
            #line 1 "{expectedFileName}"
            __o = i++;

            #line default
            #line hidden
            #nullable disable

            """,
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [ConditionalTheory(Is.Windows)]
    [InlineData(@"test.cshtml",                     @"test.cshtml")]
    [InlineData(@"pages/test.cshtml",               @"pages\test.cshtml")]
    [InlineData(@"pages\test.cshtml",               @"pages\test.cshtml")]
    [InlineData(@"c:/pages/test.cshtml",            @"c:\pages\test.cshtml")]
    [InlineData(@"c:\pages\test.cshtml",            @"c:\pages\test.cshtml")]
    [InlineData(@"c:/pages with space/test.cshtml", @"c:\pages with space\test.cshtml")]
    [InlineData(@"c:\pages with space\test.cshtml", @"c:\pages with space\test.cshtml")]
    [InlineData(@"//SERVER/pages/test.cshtml",      @"\\SERVER\pages\test.cshtml")]
    [InlineData(@"\\SERVER/pages\test.cshtml",      @"\\SERVER\pages\test.cshtml")]
    public void LinePragma_Enhanced_Is_Adjusted_On_Windows(string fileName, string expectedFileName)
    {
        var writer = RuntimeNodeWriter.Instance;
        using var context = TestCodeRenderingContext.CreateDesignTime(source: RazorSourceDocument.Create("", fileName));

        Assert.True(context.Options.RemapLinePragmaPathsOnWindows);
        Assert.True(context.Options.UseEnhancedLinePragma);

        var node = new CSharpExpressionIntermediateNode();
        var builder = IntermediateNodeBuilder.Create(node);

        // Create a fake source span, so we can check it correctly maps in the #line below
        builder.Add(IntermediateNodeFactory.CSharpToken("i++", new SourceSpan(fileName, 0, 2, 3, 6, 1, 2)));

        writer.WriteCSharpExpression(context, node);

        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
            $"""

            #nullable restore
            #line (3,4)-(4,3) 6 "{expectedFileName}"
            Write(i++

            #line default
            #line hidden
            #nullable disable
            );

            """,
            csharp,
            ignoreLineEndingDifferences: true);

        Assert.Single(context.GetSourceMappings());
    }

    private class MyExtensionIntermediateNode : ExtensionIntermediateNode
    {
        public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

        public override void Accept(IntermediateNodeVisitor visitor)
        {
            visitor.VisitDefault(this);
        }

        public override void WriteNode(CodeTarget target, CodeRenderingContext context)
        {
            context.CodeWriter.WriteLine("MyExtensionNode");
        }
    }
}
