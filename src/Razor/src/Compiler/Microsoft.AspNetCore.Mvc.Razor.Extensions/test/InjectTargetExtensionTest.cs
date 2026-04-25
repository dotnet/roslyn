// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

public class InjectTargetExtensionTest
{
    [Fact]
    public void InjectDirectiveTargetExtension_WritesProperty()
    {
        // Arrange
        using var context = TestCodeRenderingContext.CreateRuntime();
        var target = new InjectTargetExtension(considerNullabilityEnforcement: true);
        var node = new InjectIntermediateNode()
        {
            TypeName = "PropertyType",
            MemberName = "PropertyName",
        };

        // Act
        target.WriteInjectProperty(context, node);

        // Assert
        Assert.Equal("""
            #nullable restore
            [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
            public PropertyType PropertyName { get; private set; } = default!;
            #nullable disable

            """,
            context.CodeWriter.GetText().ToString());
    }

    [Fact]
    public void InjectDirectiveTargetExtension_WritesPropertyWithLinePragma_WhenSourceIsSet()
    {
        // Arrange
        using var context = TestCodeRenderingContext.CreateRuntime();
        var target = new InjectTargetExtension(considerNullabilityEnforcement: true);
        var node = new InjectIntermediateNode()
        {
            TypeName = "PropertyType<ModelType>",
            MemberName = "PropertyName",
            TypeSource = new SourceSpan(
                filePath: "test-path",
                absoluteIndex: 7,
                lineIndex: 1,
                characterIndex: 7,
                length: 23),
            MemberSource = new SourceSpan(
                filePath: "test-path",
                absoluteIndex: 31,
                lineIndex: 1,
                characterIndex: 31,
                length: 12)
        };

        // Act
        target.WriteInjectProperty(context, node);

        // Assert
        Assert.Equal("""
            [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
            public 
            #nullable restore
            #line (2,8)-(2,1) "test-path"
            PropertyType<ModelType>

            #line default
            #line hidden
            #nullable disable
             
            #nullable restore
            #line (2,32)-(2,1) "test-path"
            PropertyName

            #line default
            #line hidden
            #nullable disable
             { get; private set; }
             = default!;
            
            """,
            context.CodeWriter.GetText().ToString());
    }
}
