// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

public class MetadataAttributeTargetExtensionTest
{
    [Fact]
    public void WriteRazorCompiledItemAttribute_RendersCorrectly()
    {
        // Arrange
        var extension = new MetadataAttributeTargetExtension()
        {
            CompiledItemAttributeName = "global::TestItem",
        };
        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new RazorCompiledItemAttributeIntermediateNode()
        {
            TypeName = "Foo.Bar",
            Kind = "test",
            Identifier = "Foo/Bar",
        };

        // Act
        extension.WriteRazorCompiledItemAttribute(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"[assembly: global::TestItem(typeof(Foo.Bar), @""test"", @""Foo/Bar"")]
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteRazorCompiledItemAttribute_EscapesKindAndIdentifier()
    {
        var extension = new MetadataAttributeTargetExtension();
        using var context = TestCodeRenderingContext.CreateRuntime();
        var node = new RazorCompiledItemAttributeIntermediateNode
        {
            TypeName = "Foo.Bar",
            Kind = """test"kind""",
            Identifier = """git:/repo/Index.cshtml?{"ref":"~"}""",
        };

        extension.WriteRazorCompiledItemAttribute(context, node);

        var tree = CSharpSyntaxTree.ParseText(context.CodeWriter.GetText());
        Assert.Empty(tree.GetDiagnostics());
        var attribute = Assert.Single(Assert.Single(((CompilationUnitSyntax)tree.GetRoot()).AttributeLists).Attributes);
        Assert.Equal(node.Kind, Assert.IsType<LiteralExpressionSyntax>(attribute.ArgumentList.Arguments[1].Expression).Token.ValueText);
        Assert.Equal(node.Identifier, Assert.IsType<LiteralExpressionSyntax>(attribute.ArgumentList.Arguments[2].Expression).Token.ValueText);
    }

    [Fact]
    public void WriteRazorSourceChecksumAttribute_RendersCorrectly()
    {
        // Arrange
        var extension = new MetadataAttributeTargetExtension()
        {
            SourceChecksumAttributeName = "global::TestChecksum",
        };
        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new RazorSourceChecksumAttributeIntermediateNode()
        {
            ChecksumAlgorithm = CodeAnalysis.Text.SourceHashAlgorithm.Sha256,
            Checksum = ImmutableArray.Create((byte)'t', (byte)'e', (byte)'s', (byte)'t'),
            Identifier = "Foo/Bar",
        };

        // Act
        extension.WriteRazorSourceChecksumAttribute(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"[global::TestChecksum(@""Sha256"", @""74657374"", @""Foo/Bar"")]
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteRazorSourceChecksumAttribute_EscapesIdentifier()
    {
        var extension = new MetadataAttributeTargetExtension();
        using var context = TestCodeRenderingContext.CreateRuntime();
        var node = new RazorSourceChecksumAttributeIntermediateNode
        {
            ChecksumAlgorithm = CodeAnalysis.Text.SourceHashAlgorithm.Sha256,
            Checksum = [.. "test"u8],
            Identifier = """git:/repo/Index.cshtml?{"ref":"~"}""",
        };

        extension.WriteRazorSourceChecksumAttribute(context, node);

        var tree = CSharpSyntaxTree.ParseText(context.CodeWriter.GetText().ToString() + "class C {}");
        Assert.Empty(tree.GetDiagnostics());
        var declaration = Assert.IsType<ClassDeclarationSyntax>(Assert.Single(((CompilationUnitSyntax)tree.GetRoot()).Members));
        var attribute = Assert.Single(Assert.Single(declaration.AttributeLists).Attributes);
        Assert.Equal(node.Identifier, Assert.IsType<LiteralExpressionSyntax>(attribute.ArgumentList.Arguments[2].Expression).Token.ValueText);
    }

    [Fact]
    public void WriteRazorCompiledItemAttributeMetadata_RendersCorrectly()
    {
        // Arrange
        var extension = new MetadataAttributeTargetExtension()
        {
            CompiledItemMetadataAttributeName = "global::TestItemMetadata",
        };
        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new RazorCompiledItemMetadataAttributeIntermediateNode
        {
            Key = "key",
            Value = "value",
        };

        // Act
        extension.WriteRazorCompiledItemMetadataAttribute(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString().Trim();
        Assert.Equal(
"[global::TestItemMetadata(\"key\", \"value\")]",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteRazorCompiledItemAttributeMetadata_EscapesKeysAndValuesCorrectly()
    {
        // Arrange
        var extension = new MetadataAttributeTargetExtension()
        {
            CompiledItemMetadataAttributeName = "global::TestItemMetadata",
        };
        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new RazorCompiledItemMetadataAttributeIntermediateNode
        {
            Key = "\"test\" key",
            Value = @"""test"" value",
        };

        // Act
        extension.WriteRazorCompiledItemMetadataAttribute(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString().Trim();
        Assert.Equal(
"[global::TestItemMetadata(\"\\\"test\\\" key\", \"\\\"test\\\" value\")]",
            csharp,
            ignoreLineEndingDifferences: true);
    }
}
