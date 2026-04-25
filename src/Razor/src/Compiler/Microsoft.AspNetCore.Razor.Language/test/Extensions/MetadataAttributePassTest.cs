// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;
using static Microsoft.AspNetCore.Razor.Language.Intermediate.IntermediateNodeAssert;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

public class MetadataAttributePassTest : RazorProjectEngineTestBase
{
    protected override RazorLanguageVersion Version => RazorLanguageVersion.Latest;

    protected override void ConfigureProjectEngine(RazorProjectEngineBuilder builder)
    {
        builder.Features.Add(new DefaultMetadataIdentifierFeature());
    }

    [Fact]
    public void Execute_NullCodeGenerationOptions_Noops()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create();
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var documentNode = new DocumentIntermediateNode();

        // Act
        ProjectEngine.ExecutePass<MetadataAttributePass>(codeDocument, documentNode);

        // Assert
        NoChildren(documentNode);
    }

    [Fact]
    public void Execute_SuppressMetadataAttributes_Noops()
    {
        // Arrange
        var projectEngine = CreateProjectEngine(builder =>
        {
            builder.ConfigureCodeGenerationOptions(builder =>
            {
                builder.SuppressMetadataAttributes = true;
            });
        });

        var source = TestRazorSourceDocument.Create();
        var codeDocument = projectEngine.CreateCodeDocument(source);

        var documentNode = new DocumentIntermediateNode()
        {
            Options = codeDocument.CodeGenerationOptions
        };

        // Act
        projectEngine.ExecutePass<MetadataAttributePass>(codeDocument, documentNode);

        // Assert
        NoChildren(documentNode);
    }

    [Fact]
    public void Execute_ComponentDocumentKind_Noops()
    {
        // Arrange
        var projectEngine = CreateProjectEngine(builder =>
        {
            builder.ConfigureCodeGenerationOptions(builder =>
            {
                builder.SuppressMetadataAttributes = true;
            });
        });

        var source = TestRazorSourceDocument.Create();
        var codeDocument = projectEngine.CreateCodeDocument(source);

        var documentNode = new DocumentIntermediateNode()
        {
            DocumentKind = ComponentDocumentClassifierPass.ComponentDocumentKind,
            Options = codeDocument.CodeGenerationOptions
        };

        // Act
        projectEngine.ExecutePass<MetadataAttributePass>(codeDocument, documentNode);

        // Assert
        NoChildren(documentNode);
    }

    [Fact]
    public void Execute_NoNamespaceSet_Noops()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create();
        var codeDocument = ProjectEngine.CreateCodeDocument(source);

        var documentNode = new DocumentIntermediateNode()
        {
            DocumentKind = "test",
            Options = codeDocument.CodeGenerationOptions
        };

        var builder = IntermediateNodeBuilder.Create(documentNode);
        var @namespace = new NamespaceDeclarationIntermediateNode
        {
            IsPrimaryNamespace = true,
        };

        builder.Push(@namespace);

        var @class = new ClassDeclarationIntermediateNode
        {
            IsPrimaryClass = true,
            Name = "Test"
        };

        builder.Add(@class);

        // Act
        ProjectEngine.ExecutePass<MetadataAttributePass>(codeDocument, documentNode);

        // Assert
        Assert.Equal(2, documentNode.Children.Count);

        var item = Assert.IsType<RazorCompiledItemAttributeIntermediateNode>(documentNode.Children[0]);
        Assert.Equal("/test.cshtml", item.Identifier);
        Assert.Equal("test", item.Kind);
        Assert.Equal("Test", item.TypeName);

        Assert.Equal(2, @namespace.Children.Count);
        var checksum = Assert.IsType<RazorSourceChecksumAttributeIntermediateNode>(@namespace.Children[0]);
        Assert.Equal(CodeAnalysis.Text.SourceHashAlgorithm.Sha256, checksum.ChecksumAlgorithm);
        Assert.Equal("/test.cshtml", checksum.Identifier);

        var foundClass = Assert.IsType<ClassDeclarationIntermediateNode>(@namespace.Children[1]);
        Assert.Equal("Test", foundClass.Name);
    }

    [Fact]
    public void Execute_NoClassNameSet_Noops()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create();
        var codeDocument = ProjectEngine.CreateCodeDocument(source);

        var documentNode = new DocumentIntermediateNode()
        {
            DocumentKind = "test",
            Options = codeDocument.CodeGenerationOptions
        };

        var builder = IntermediateNodeBuilder.Create(documentNode);
        var @namespace = new NamespaceDeclarationIntermediateNode
        {
            IsPrimaryNamespace = true,
            Name = "Some.Namespace"
        };

        builder.Push(@namespace);

        var @class = new ClassDeclarationIntermediateNode
        {
            IsPrimaryClass = true,
        };

        builder.Add(@class);

        // Act
        ProjectEngine.ExecutePass<MetadataAttributePass>(codeDocument, documentNode);

        // Assert
        SingleChild<NamespaceDeclarationIntermediateNode>(documentNode);
    }

    [Fact]
    public void Execute_NoDocumentKind_Noops()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create();
        var codeDocument = ProjectEngine.CreateCodeDocument(source);

        var documentNode = new DocumentIntermediateNode();

        var builder = IntermediateNodeBuilder.Create(documentNode);
        var @namespace = new NamespaceDeclarationIntermediateNode
        {
            IsPrimaryNamespace = true,
            Name = "Some.Namespace"
        };

        builder.Push(@namespace);

        var @class = new ClassDeclarationIntermediateNode
        {
            IsPrimaryClass = true,
            Name = "Test"
        };

        builder.Add(@class);

        // Act
        ProjectEngine.ExecutePass<MetadataAttributePass>(codeDocument, documentNode);

        // Assert
        SingleChild<NamespaceDeclarationIntermediateNode>(documentNode);
    }

    [Fact]
    public void Execute_NoIdentifier_Noops()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("", RazorSourceDocumentProperties.Default);
        var codeDocument = ProjectEngine.CreateCodeDocument(source);

        var documentNode = new DocumentIntermediateNode()
        {
            DocumentKind = "test",
            Options = codeDocument.CodeGenerationOptions
        };

        var builder = IntermediateNodeBuilder.Create(documentNode);
        var @namespace = new NamespaceDeclarationIntermediateNode
        {
            IsPrimaryNamespace = true,
            Name = "Some.Namespace"
        };

        builder.Push(@namespace);

        var @class = new ClassDeclarationIntermediateNode
        {
            IsPrimaryClass = true,
            Name = "Test"
        };

        builder.Add(@class);

        // Act
        ProjectEngine.ExecutePass<MetadataAttributePass>(codeDocument, documentNode);

        // Assert
        SingleChild<NamespaceDeclarationIntermediateNode>(documentNode);
    }

    [Fact]
    public void Execute_HasRequiredInfo_AddsItemAndSourceChecksum()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("", RazorSourceDocumentProperties.Create(null, "Foo\\Bar.cshtml"));
        var codeDocument = ProjectEngine.CreateCodeDocument(source);

        var documentNode = new DocumentIntermediateNode()
        {
            DocumentKind = "test",
            Options = codeDocument.CodeGenerationOptions
        };

        var builder = IntermediateNodeBuilder.Create(documentNode);
        var @namespace = new NamespaceDeclarationIntermediateNode
        {
            IsPrimaryNamespace = true,
            Name = "Some.Namespace"
        };

        builder.Push(@namespace);

        var @class = new ClassDeclarationIntermediateNode
        {
            IsPrimaryClass = true,
            Name = "Test",
        };

        builder.Add(@class);

        // Act
        ProjectEngine.ExecutePass<MetadataAttributePass>(codeDocument, documentNode);

        // Assert
        Assert.Equal(2, documentNode.Children.Count);

        var item = Assert.IsType<RazorCompiledItemAttributeIntermediateNode>(documentNode.Children[0]);
        Assert.Equal("/Foo/Bar.cshtml", item.Identifier);
        Assert.Equal("test", item.Kind);
        Assert.Equal("Some.Namespace.Test", item.TypeName);

        Assert.Equal(2, @namespace.Children.Count);
        var checksum = Assert.IsType<RazorSourceChecksumAttributeIntermediateNode>(@namespace.Children[0]);
        Assert.Equal(CodeAnalysis.Text.SourceHashAlgorithm.Sha256, checksum.ChecksumAlgorithm);
        Assert.Equal("/Foo/Bar.cshtml", checksum.Identifier);
    }

    [Fact]
    public void Execute_HasRequiredInfo_AndImport_AddsItemAndSourceChecksum()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("", RazorSourceDocumentProperties.Create(null, "Foo\\Bar.cshtml"));
        var importSource = TestRazorSourceDocument.Create("@using System", RazorSourceDocumentProperties.Create(null, "Foo\\Import.cshtml"));
        var codeDocument = ProjectEngine.CreateCodeDocument(source, [importSource]);

        var documentNode = new DocumentIntermediateNode()
        {
            DocumentKind = "test",
            Options = codeDocument.CodeGenerationOptions
        };

        var builder = IntermediateNodeBuilder.Create(documentNode);
        var @namespace = new NamespaceDeclarationIntermediateNode
        {
            IsPrimaryNamespace = true,
            Name = "Some.Namespace"
        };

        builder.Push(@namespace);

        var @class = new ClassDeclarationIntermediateNode
        {
            IsPrimaryClass = true,
            Name = "Test",
        };

        builder.Add(@class);

        // Act
        ProjectEngine.ExecutePass<MetadataAttributePass>(codeDocument, documentNode);

        // Assert
        Assert.Equal(2, documentNode.Children.Count);

        var item = Assert.IsType<RazorCompiledItemAttributeIntermediateNode>(documentNode.Children[0]);
        Assert.Equal("/Foo/Bar.cshtml", item.Identifier);
        Assert.Equal("test", item.Kind);
        Assert.Equal("Some.Namespace.Test", item.TypeName);

        Assert.Equal(3, @namespace.Children.Count);
        var checksum = Assert.IsType<RazorSourceChecksumAttributeIntermediateNode>(@namespace.Children[0]);
        Assert.Equal(CodeAnalysis.Text.SourceHashAlgorithm.Sha256, checksum.ChecksumAlgorithm);
        Assert.Equal("/Foo/Bar.cshtml", checksum.Identifier);

        checksum = Assert.IsType<RazorSourceChecksumAttributeIntermediateNode>(@namespace.Children[1]);
        Assert.Equal(CodeAnalysis.Text.SourceHashAlgorithm.Sha256, checksum.ChecksumAlgorithm);
        Assert.Equal("/Foo/Import.cshtml", checksum.Identifier);
    }

    [Fact]
    public void Execute_SuppressMetadataSourceChecksumAttributes_DoesNotGenerateSourceChecksumAttributes()
    {
        // Arrange
        var projectEngine = CreateProjectEngine(builder =>
        {
            builder.ConfigureCodeGenerationOptions(builder =>
            {
                builder.SuppressMetadataSourceChecksumAttributes = true;
            });
        });

        var source = TestRazorSourceDocument.Create("", RazorSourceDocumentProperties.Create(null, "Foo\\Bar.cshtml"));
        var importSource = TestRazorSourceDocument.Create("@using System", RazorSourceDocumentProperties.Create(null, "Foo\\Import.cshtml"));
        var codeDocument = projectEngine.CreateCodeDocument(source, [importSource]);

        var documentNode = new DocumentIntermediateNode()
        {
            DocumentKind = "test",
            Options = codeDocument.CodeGenerationOptions
        };

        var builder = IntermediateNodeBuilder.Create(documentNode);
        var @namespace = new NamespaceDeclarationIntermediateNode
        {
            IsPrimaryNamespace = true,
            Name = "Some.Namespace"
        };

        builder.Push(@namespace);

        var @class = new ClassDeclarationIntermediateNode
        {
            IsPrimaryClass = true,
            Name = "Test"
        };

        builder.Add(@class);

        // Act
        projectEngine.ExecutePass<MetadataAttributePass>(codeDocument, documentNode);

        // Assert
        Assert.Equal(2, documentNode.Children.Count);

        var item = Assert.IsType<RazorCompiledItemAttributeIntermediateNode>(documentNode.Children[0]);
        Assert.Equal("/Foo/Bar.cshtml", item.Identifier);
        Assert.Equal("test", item.Kind);
        Assert.Equal("Some.Namespace.Test", item.TypeName);

        var child = Assert.Single(@namespace.Children);
        Assert.IsType<ClassDeclarationIntermediateNode>(child);
    }
}
