// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

public class LiteralRuntimeNodeWriterTest
{
    [Fact]
    public void WriteCSharpExpression_UsesWriteLiteral_WritesLinePragma_WithSource()
    {
        // Arrange
        var writer = LiteralRuntimeNodeWriter.Instance;
        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new CSharpExpressionIntermediateNode();
        var builder = IntermediateNodeBuilder.Create(node);
        builder.Add(IntermediateNodeFactory.CSharpToken("i++", new SourceSpan("test.cshtml", 0, 0, 0, 3, 0, 3)));

        // Act
        writer.WriteCSharpExpression(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"
#nullable restore
#line (1,1)-(1,4) 13 ""test.cshtml""
WriteLiteral(i++

#line default
#line hidden
#nullable disable
);
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteCSharpExpression_WithMultipleChildren()
    {
        // Arrange
        var writer = LiteralRuntimeNodeWriter.Instance;
        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new CSharpExpressionIntermediateNode();
        var builder = IntermediateNodeBuilder.Create(node);
        builder.Add(IntermediateNodeFactory.CSharpToken("i++;", new SourceSpan("test.cshtml", 0, 0, 0, 4, 0, 4)));
        builder.Add(IntermediateNodeFactory.CSharpToken("j++;", new SourceSpan("test.cshtml", 5, 0, 5, 4, 0, 9)));
        builder.Add(IntermediateNodeFactory.CSharpToken("k++;", new SourceSpan("test.cshtml", 10, 0, 10, 4, 0, 14)));

        // Act
        writer.WriteCSharpExpression(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"
#nullable restore
#line (1,1)-(1,5) 13 ""test.cshtml""
WriteLiteral(i++;

#line default
#line hidden
#nullable disable
#nullable restore
#line (1,6)-(1,10) ""test.cshtml""
j++;

#line default
#line hidden
#nullable disable
#nullable restore
#line (1,11)-(1,15) ""test.cshtml""
k++;

#line default
#line hidden
#nullable disable
);
",
            csharp,
            ignoreLineEndingDifferences: true);
    }
}
