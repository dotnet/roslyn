// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.TagHelpers.Producers;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor;

public class DefaultTagHelperProducerTest : TagHelperDescriptorProviderTestBase
{
    protected override void ConfigureEngine(RazorProjectEngineBuilder builder)
    {
        builder.Features.Add(new DefaultTagHelperProducer.Factory());
    }

    [Fact]
    public void Execute_DoesNotAddEditorBrowsableNeverDescriptorsAtDesignTime()
    {
        // Arrange
        var editorBrowsableTypeName = "TestNamespace.EditorBrowsableTagHelper";
        var compilation = BaseCompilation;

        // Act
        var result = GetTagHelpers(compilation, TagHelperDiscoveryOptions.ExcludeHidden);

        // Assert
        Assert.NotNull(compilation.GetTypeByMetadataName(editorBrowsableTypeName));
        var editorBrowsableDescriptor = result.Where(descriptor => descriptor.TypeName == editorBrowsableTypeName);
        Assert.Empty(editorBrowsableDescriptor);
    }

    [Fact]
    public void Execute_WithDefaultDiscoversTagHelpersFromAssemblyAndReference()
    {
        // Arrange
        var testTagHelper = "TestAssembly.TestTagHelper";
        var enumTagHelper = "TestNamespace.EnumTagHelper";
        var csharp = @"
using Microsoft.AspNetCore.Razor.TagHelpers;
namespace TestAssembly
{
    public class TestTagHelper : TagHelper
    {
        public override void Process(TagHelperContext context, TagHelperOutput output) {}
    }
}";
        var compilation = BaseCompilation.AddSyntaxTrees(Parse(csharp));

        // Act
        var result = GetTagHelpers(compilation);

        // Assert
        Assert.NotNull(compilation.GetTypeByMetadataName(testTagHelper));
        Assert.NotEmpty(result);
        Assert.NotEmpty(result.Where(f => f.TypeName == testTagHelper));
        Assert.NotEmpty(result.Where(f => f.TypeName == enumTagHelper));
    }

    [Fact]
    public void Execute_WithTargetAssembly_Works()
    {
        // Arrange
        var testTagHelper = "TestAssembly.TestTagHelper";
        var enumTagHelper = "TestNamespace.EnumTagHelper";
        var csharp = @"
using Microsoft.AspNetCore.Razor.TagHelpers;
namespace TestAssembly
{
    public class TestTagHelper : TagHelper
    {
        public override void Process(TagHelperContext context, TagHelperOutput output) {}
    }
}";
        var compilation = BaseCompilation.AddSyntaxTrees(Parse(csharp));

        var targetAssembly = (IAssemblySymbol?)compilation.GetAssemblyOrModuleSymbol(
            compilation.References.First(static r => r.Display?.Contains("Microsoft.CodeAnalysis.Razor.Test") == true));

        Assert.NotNull(targetAssembly);

        Assert.True(TryGetDiscoverer(compilation, out var discoverer));

        // Act
        var result = discoverer.GetTagHelpers(targetAssembly);

        // Assert
        Assert.NotNull(compilation.GetTypeByMetadataName(testTagHelper));
        Assert.NotEmpty(result);
        Assert.Empty(result.Where(f => f.TypeName == testTagHelper));
        Assert.NotEmpty(result.Where(f => f.TypeName == enumTagHelper));
    }
}
