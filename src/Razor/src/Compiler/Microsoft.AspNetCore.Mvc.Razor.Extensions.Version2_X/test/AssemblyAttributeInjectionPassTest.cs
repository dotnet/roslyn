// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X;

public class AssemblyAttributeInjectionPassTest : RazorProjectEngineTestBase
{
    protected override RazorLanguageVersion Version => RazorLanguageVersion.Version_2_1;

    [Fact]
    public void Execute_NoOps_IfNamespaceNodeIsMissing()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateEmptyCodeDocument();
        var documentNode = new DocumentIntermediateNode() { Options = codeDocument.CodeGenerationOptions };

        // Act
        ProjectEngine.ExecutePass<AssemblyAttributeInjectionPass>(codeDocument, documentNode);

        // Assert
        Assert.Empty(documentNode.Children);
    }

    [Fact]
    public void Execute_NoOps_IfNamespaceNodeHasEmptyContent()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateEmptyCodeDocument();
        var documentNode = new DocumentIntermediateNode() { Options = codeDocument.CodeGenerationOptions };

        var builder = IntermediateNodeBuilder.Create(documentNode);
        var @namespace = new NamespaceDeclarationIntermediateNode()
        {
            Name = string.Empty,
            IsPrimaryNamespace = true,
        };

        builder.Push(@namespace);

        // Act
        ProjectEngine.ExecutePass<AssemblyAttributeInjectionPass>(codeDocument, documentNode);

        // Assert
        var node = Assert.Single(documentNode.Children);
        Assert.Same(@namespace, node);
    }

    [Fact]
    public void Execute_NoOps_IfClassNameNodeIsMissing()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateEmptyCodeDocument();
        var documentNode = new DocumentIntermediateNode() { Options = codeDocument.CodeGenerationOptions };

        var builder = IntermediateNodeBuilder.Create(documentNode);
        var @namespace = new NamespaceDeclarationIntermediateNode() { Name = "SomeNamespace" };
        builder.Push(@namespace);

        // Act
        ProjectEngine.ExecutePass<AssemblyAttributeInjectionPass>(codeDocument, documentNode);

        // Assert
        var node = Assert.Single(documentNode.Children);
        Assert.Same(@namespace, node);
    }

    [Fact]
    public void Execute_NoOps_IfClassNameIsEmpty()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateEmptyCodeDocument();
        var documentNode = new DocumentIntermediateNode() { Options = codeDocument.CodeGenerationOptions };

        var builder = IntermediateNodeBuilder.Create(documentNode);

        var @namespace = new NamespaceDeclarationIntermediateNode()
        {
            Name = "SomeNamespace",
            IsPrimaryNamespace = true,
        };

        builder.Push(@namespace);

        builder.Add(new ClassDeclarationIntermediateNode
        {
            IsPrimaryClass = true,
        });

        // Act
        ProjectEngine.ExecutePass<AssemblyAttributeInjectionPass>(codeDocument, documentNode);

        // Assert
        var node = Assert.Single(documentNode.Children);
        Assert.Same(@namespace, node);
    }

    [Fact]
    public void Execute_NoOps_IfDocumentIsNotViewOrPage()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateEmptyCodeDocument();
        var documentNode = new DocumentIntermediateNode()
        {
            DocumentKind = "Default",
            Options = codeDocument.CodeGenerationOptions
        };

        var builder = IntermediateNodeBuilder.Create(documentNode);
        var @namespace = new NamespaceDeclarationIntermediateNode() { Name = "SomeNamespace" };
        builder.Push(@namespace);

        var @class = new ClassDeclarationIntermediateNode
        {
            Name = "SomeName",
            IsPrimaryClass = true,
        };

        builder.Add(@class);

        // Act
        ProjectEngine.ExecutePass<AssemblyAttributeInjectionPass>(codeDocument, documentNode);

        // Assert
        var node = Assert.Single(documentNode.Children);
        Assert.Same(@namespace, node);
    }

    [Fact]
    public void Execute_NoOps_ForDesignTime()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("test", RazorSourceDocumentProperties.Create(filePath: null, relativePath: "/Views/Index.cshtml"));
        var codeDocument = ProjectEngine.CreateDesignTimeCodeDocument(source);

        var documentNode = new DocumentIntermediateNode()
        {
            DocumentKind = MvcViewDocumentClassifierPass.MvcViewDocumentKind,
            Options = codeDocument.CodeGenerationOptions
        };

        var builder = IntermediateNodeBuilder.Create(documentNode);
        var @namespace = new NamespaceDeclarationIntermediateNode
        {
            Name = "SomeNamespace",
            IsPrimaryNamespace = true,
        };

        builder.Push(@namespace);

        var @class = new ClassDeclarationIntermediateNode
        {
            Name = "SomeName",
            IsPrimaryClass = true,
        };

        builder.Add(@class);

        // Act
        ProjectEngine.ExecutePass<AssemblyAttributeInjectionPass>(codeDocument, documentNode);

        // Assert
        var node = Assert.Single(documentNode.Children);
        Assert.Same(@namespace, node);
    }

    [Fact]
    public void Execute_AddsRazorViewAttribute_ToViews()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("test", RazorSourceDocumentProperties.Create(filePath: null, relativePath: "/Views/Index.cshtml"));
        var codeDocument = ProjectEngine.CreateCodeDocument(source);

        var expectedAttribute = "[assembly:global::Microsoft.AspNetCore.Mvc.Razor.Compilation.RazorViewAttribute(@\"/Views/Index.cshtml\", typeof(SomeNamespace.SomeName))]";

        var documentNode = new DocumentIntermediateNode()
        {
            DocumentKind = MvcViewDocumentClassifierPass.MvcViewDocumentKind,
            Options = codeDocument.CodeGenerationOptions
        };

        var builder = IntermediateNodeBuilder.Create(documentNode);

        var @namespace = new NamespaceDeclarationIntermediateNode
        {
            Name = "SomeNamespace",
            IsPrimaryNamespace = true,
        };

        builder.Push(@namespace);
        var @class = new ClassDeclarationIntermediateNode
        {
            Name = "SomeName",
            IsPrimaryClass = true,
        };

        builder.Add(@class);

        // Act
        ProjectEngine.ExecutePass<AssemblyAttributeInjectionPass>(codeDocument, documentNode);

        // Assert
        Assert.Collection(documentNode.Children,
            node =>
            {
                var csharpCode = Assert.IsType<CSharpCodeIntermediateNode>(node);
                var token = Assert.IsAssignableFrom<CSharpIntermediateToken>(Assert.Single(csharpCode.Children));
                Assert.Equal(expectedAttribute, token.Content);
            },
            node => Assert.Same(@namespace, node));
    }

    [Fact]
    public void Execute_EscapesViewPathWhenAddingAttributeToViews()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("test", RazorSourceDocumentProperties.Create(filePath: null, relativePath: "\\test\\\"Index.cshtml"));
        var codeDocument = ProjectEngine.CreateCodeDocument(source);

        var expectedAttribute = "[assembly:global::Microsoft.AspNetCore.Mvc.Razor.Compilation.RazorViewAttribute(@\"/test/\"\"Index.cshtml\", typeof(SomeNamespace.SomeName))]";

        var documentNode = new DocumentIntermediateNode()
        {
            DocumentKind = MvcViewDocumentClassifierPass.MvcViewDocumentKind,
            Options = codeDocument.CodeGenerationOptions
        };

        var builder = IntermediateNodeBuilder.Create(documentNode);

        var @namespace = new NamespaceDeclarationIntermediateNode
        {
            Name = "SomeNamespace",
            IsPrimaryNamespace = true,
        };

        builder.Push(@namespace);

        var @class = new ClassDeclarationIntermediateNode
        {
            Name = "SomeName",
            IsPrimaryClass = true,
        };

        builder.Add(@class);

        // Act
        ProjectEngine.ExecutePass<AssemblyAttributeInjectionPass>(codeDocument, documentNode);

        // Assert
        Assert.Collection(documentNode.Children,
            node =>
            {
                var csharpCode = Assert.IsType<CSharpCodeIntermediateNode>(node);
                var token = Assert.IsAssignableFrom<CSharpIntermediateToken>(Assert.Single(csharpCode.Children));
                Assert.Equal(expectedAttribute, token.Content);
            },
            node => Assert.Same(@namespace, node));
    }

    [Fact]
    public void Execute_AddsRazorPagettribute_ToPage()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("test", RazorSourceDocumentProperties.Create(filePath: null, relativePath: "/Views/Index.cshtml"));
        var codeDocument = ProjectEngine.CreateCodeDocument(source);

        var expectedAttribute = "[assembly:global::Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.RazorPageAttribute(@\"/Views/Index.cshtml\", typeof(SomeNamespace.SomeName), null)]";

        var documentNode = new DocumentIntermediateNode()
        {
            DocumentKind = RazorPageDocumentClassifierPass.RazorPageDocumentKind,
            Options = codeDocument.CodeGenerationOptions
        };

        var builder = IntermediateNodeBuilder.Create(documentNode);

        var pageDirective = new DirectiveIntermediateNode
        {
            Directive = PageDirective.Directive
        };

        builder.Add(pageDirective);

        var @namespace = new NamespaceDeclarationIntermediateNode
        {
            Name = "SomeNamespace",
            IsPrimaryNamespace = true,
        };

        builder.Push(@namespace);

        var @class = new ClassDeclarationIntermediateNode
        {
            Name = "SomeName",
            IsPrimaryClass = true,
        };

        builder.Add(@class);

        // Act
        ProjectEngine.ExecutePass<AssemblyAttributeInjectionPass>(codeDocument, documentNode);

        // Assert
        Assert.Collection(documentNode.Children,
            node => Assert.Same(pageDirective, node),
            node =>
            {
                var csharpCode = Assert.IsType<CSharpCodeIntermediateNode>(node);
                var token = Assert.IsAssignableFrom<CSharpIntermediateToken>(Assert.Single(csharpCode.Children));
                Assert.Equal(expectedAttribute, token.Content);
            },
            node => Assert.Same(@namespace, node));
    }

    [Fact]
    public void Execute_EscapesViewPathAndRouteWhenAddingAttributeToPage()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("test", RazorSourceDocumentProperties.Create(filePath: null, relativePath: "test\\\"Index.cshtml"));
        var codeDocument = ProjectEngine.CreateCodeDocument(source);

        var expectedAttribute = "[assembly:global::Microsoft.AspNetCore.Mvc.Razor.Compilation.RazorViewAttribute(@\"/test/\"\"Index.cshtml\", typeof(SomeNamespace.SomeName))]";

        var documentNode = new DocumentIntermediateNode()
        {
            DocumentKind = MvcViewDocumentClassifierPass.MvcViewDocumentKind,
            Options = codeDocument.CodeGenerationOptions
        };

        var builder = IntermediateNodeBuilder.Create(documentNode);

        var @namespace = new NamespaceDeclarationIntermediateNode
        {
            Name = "SomeNamespace",
            IsPrimaryNamespace = true,
        };
        builder.Push(@namespace);

        var @class = new ClassDeclarationIntermediateNode
        {
            Name = "SomeName",
            IsPrimaryClass = true,
        };

        builder.Add(@class);

        // Act
        ProjectEngine.ExecutePass<AssemblyAttributeInjectionPass>(codeDocument, documentNode);

        // Assert
        Assert.Collection(documentNode.Children,
            node =>
            {
                var csharpCode = Assert.IsType<CSharpCodeIntermediateNode>(node);
                var token = Assert.IsAssignableFrom<CSharpIntermediateToken>(Assert.Single(csharpCode.Children));
                Assert.Equal(expectedAttribute, token.Content);
            },
            node => Assert.Same(@namespace, node));
    }
}
