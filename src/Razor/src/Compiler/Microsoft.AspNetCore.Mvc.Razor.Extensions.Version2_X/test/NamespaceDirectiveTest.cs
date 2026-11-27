// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X;

public class NamespaceDirectiveTest : RazorProjectEngineTestBase
{
    protected override RazorLanguageVersion Version => RazorLanguageVersion.Version_2_1;

    [Fact]
    public void GetNamespace_IncompleteDirective_UsesEmptyNamespace()
    {
        // Arrange
        var source = "c:\\foo\\bar\\bleh.cshtml";
        var imports = "c:\\foo\\baz\\bleh.cshtml";

        var node = new DirectiveIntermediateNode()
        {
            Directive = NamespaceDirective.Directive,
            Source = new SourceSpan(imports, 0, 0, 0, 0)
        };

        // Act
        var @namespace = NamespaceDirective.GetNamespace(source, node);

        // Assert
        Assert.Equal(string.Empty, @namespace);
    }

    [Fact]
    public void GetNamespace_EmptyDirective_UsesEmptyNamespace()
    {
        // Arrange
        var source = "c:\\foo\\bar\\bleh.cshtml";
        var imports = "c:\\foo\\baz\\bleh.cshtml";

        var node = new DirectiveIntermediateNode()
        {
            Directive = NamespaceDirective.Directive,
            Source = new SourceSpan(imports, 0, 0, 0, 0)
        };

        node.Children.Add(new DirectiveTokenIntermediateNode() { Content = string.Empty });

        // Act
        var @namespace = NamespaceDirective.GetNamespace(source, node);

        // Assert
        Assert.Equal(string.Empty, @namespace);
    }

    // When we don't have a relationship between the source file and the imports file
    // we will just use the namespace on the node directly.
    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData(null, "/foo/bar")]
    [InlineData("/foo/baz", "/foo/bar/bleh")]
    [InlineData("/foo.cshtml", "/foo/bar.cshtml")]
    [InlineData("c:\\foo.cshtml", "d:\\foo\\bar.cshtml")]
    [InlineData("c:\\foo\\bar\\bleh.cshtml", "c:\\foo\\baz\\bleh.cshtml")]
    public void GetNamespace_ForNonRelatedFiles_UsesNamespaceVerbatim(string? source, string? imports)
    {
        // Arrange
        var node = new DirectiveIntermediateNode()
        {
            Directive = NamespaceDirective.Directive,
            Source = new SourceSpan(imports, 0, 0, 0, 0)
        };

        node.Children.Add(new DirectiveTokenIntermediateNode() { Content = "Base" });

        // Act
        var @namespace = NamespaceDirective.GetNamespace(source, node);

        // Assert
        Assert.Equal("Base", @namespace);
    }

    [Theory]
    [InlineData("/foo.cshtml", "/_ViewImports.cshtml", "Base")]
    [InlineData("/foo/bar.cshtml", "/_ViewImports.cshtml", "Base.foo")]
    [InlineData("/foo/bar/baz.cshtml", "/_ViewImports.cshtml", "Base.foo.bar")]
    [InlineData("/foo/bar/baz.cshtml", "/foo/_ViewImports.cshtml", "Base.bar")]
    [InlineData("/Foo/bar/baz.cshtml", "/foo/_ViewImports.cshtml", "Base.bar")]
    [InlineData("c:\\foo.cshtml", "c:\\_ViewImports.cshtml", "Base")]
    [InlineData("c:\\foo\\bar.cshtml", "c:\\_ViewImports.cshtml", "Base.foo")]
    [InlineData("c:\\foo\\bar\\baz.cshtml", "c:\\_ViewImports.cshtml", "Base.foo.bar")]
    [InlineData("c:\\foo\\bar\\baz.cshtml", "c:\\foo\\_ViewImports.cshtml", "Base.bar")]
    [InlineData("c:\\Foo\\bar\\baz.cshtml", "c:\\foo\\_ViewImports.cshtml", "Base.bar")]
    public void GetNamespace_ForRelatedFiles_ComputesNamespaceWithSuffix(string source, string imports, string expected)
    {
        // Arrange
        var node = new DirectiveIntermediateNode()
        {
            Directive = NamespaceDirective.Directive,
            Source = new SourceSpan(imports, 0, 0, 0, 0)
        };

        node.Children.Add(new DirectiveTokenIntermediateNode() { Content = "Base" });

        // Act
        var @namespace = NamespaceDirective.GetNamespace(source, node);

        // Assert
        Assert.Equal(expected, @namespace);
    }

    // This is the case where a _ViewImports sets the namespace.
    [Fact]
    public void Pass_SetsNamespace_ComputedFromImports()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("ignored", filePath: "/Account/Manage/AddUser.cshtml");
        var codeDocument = ProjectEngine.CreateCodeDocument(source);

        var documentNode = new DocumentIntermediateNode();
        var builder = IntermediateNodeBuilder.Create(documentNode);

        builder.Push(new DirectiveIntermediateNode()
        {
            Directive = NamespaceDirective.Directive,
            Source = new SourceSpan("/Account/_ViewImports.cshtml", 0, 0, 0, 0)
        });

        builder.Add(new DirectiveTokenIntermediateNode() { Content = "WebApplication.Account" });
        builder.Pop();

        var @namespace = new NamespaceDeclarationIntermediateNode() { Name = "default" };
        builder.Push(@namespace);

        var @class = new ClassDeclarationIntermediateNode() { Name = "default" };
        builder.Add(@class);

        documentNode.DocumentKind = RazorPageDocumentClassifierPass.RazorPageDocumentKind;

        // Act
        ProjectEngine.ExecutePass<NamespaceDirective.Pass>(codeDocument, documentNode);

        // Assert
        Assert.Equal("WebApplication.Account.Manage", @namespace.Name);
        Assert.Equal("default", @class.Name);
    }

    // This is the case where the source file sets the namespace.
    [Fact]
    public void Pass_SetsNamespace_ComputedFromSource()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("ignored", filePath: "/Account/Manage/AddUser.cshtml");
        var codeDocument = ProjectEngine.CreateCodeDocument(source);

        var documentNode = new DocumentIntermediateNode();
        var builder = IntermediateNodeBuilder.Create(documentNode);

        // This will be ignored.
        builder.Push(new DirectiveIntermediateNode()
        {
            Directive = NamespaceDirective.Directive,
            Source = new SourceSpan("/Account/_ViewImports.cshtml", 0, 0, 0, 0)
        });

        builder.Add(new DirectiveTokenIntermediateNode() { Content = "ignored" });
        builder.Pop();

        // This will be used.
        builder.Push(new DirectiveIntermediateNode()
        {
            Directive = NamespaceDirective.Directive,
            Source = new SourceSpan("/Account/Manage/AddUser.cshtml", 0, 0, 0, 0)
        });

        builder.Add(new DirectiveTokenIntermediateNode() { Content = "WebApplication.Account.Manage" });
        builder.Pop();

        var @namespace = new NamespaceDeclarationIntermediateNode() { Name = "default" };
        builder.Push(@namespace);

        var @class = new ClassDeclarationIntermediateNode() { Name = "default" };
        builder.Add(@class);

        documentNode.DocumentKind = RazorPageDocumentClassifierPass.RazorPageDocumentKind;

        // Act
        ProjectEngine.ExecutePass<NamespaceDirective.Pass>(codeDocument, documentNode);

        // Assert
        Assert.Equal("WebApplication.Account.Manage", @namespace.Name);
        Assert.Equal("default", @class.Name);
    }

    // Handles cases where invalid characters appears in FileNames. Note that we don't sanitize the part of
    // the namespace that you put in an import, just the file-based-suffix. Garbage in, garbage out.
    [Fact]
    public void Pass_SetsNamespace_SanitizesClassAndNamespace()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("ignored", filePath: "/Account/Manage-Info/Add+User.cshtml");
        var codeDocument = ProjectEngine.CreateCodeDocument(source);

        var documentNode = new DocumentIntermediateNode();
        var builder = IntermediateNodeBuilder.Create(documentNode);

        builder.Push(new DirectiveIntermediateNode()
        {
            Directive = NamespaceDirective.Directive,
            Source = new SourceSpan("/Account/_ViewImports.cshtml", 0, 0, 0, 0)
        });

        builder.Add(new DirectiveTokenIntermediateNode() { Content = "WebApplication.Account" });
        builder.Pop();

        var @namespace = new NamespaceDeclarationIntermediateNode() { Name = "default" };
        builder.Push(@namespace);

        var @class = new ClassDeclarationIntermediateNode() { Name = "default" };
        builder.Add(@class);

        documentNode.DocumentKind = RazorPageDocumentClassifierPass.RazorPageDocumentKind;

        // Act
        ProjectEngine.ExecutePass<NamespaceDirective.Pass>(codeDocument, documentNode);

        // Assert
        Assert.Equal("WebApplication.Account.Manage_Info", @namespace.Name);
        Assert.Equal("default", @class.Name);
    }

    // This is the case where the source file sets the namespace.
    [Fact]
    public void Pass_SetsNamespace_ComputedFromSource_ForView()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("ignored", filePath: "/Account/Manage/AddUser.cshtml");
        var codeDocument = ProjectEngine.CreateCodeDocument(source);

        var documentNode = new DocumentIntermediateNode();
        var builder = IntermediateNodeBuilder.Create(documentNode);

        // This will be ignored.
        builder.Push(new DirectiveIntermediateNode()
        {
            Directive = NamespaceDirective.Directive,
            Source = new SourceSpan("/Account/_ViewImports.cshtml", 0, 0, 0, 0),
        });

        builder.Add(new DirectiveTokenIntermediateNode() { Content = "ignored" });
        builder.Pop();

        // This will be used.
        builder.Push(new DirectiveIntermediateNode()
        {
            Directive = NamespaceDirective.Directive,
            Source = new SourceSpan("/Account/Manage/AddUser.cshtml", 0, 0, 0, 0),
        });

        builder.Add(new DirectiveTokenIntermediateNode() { Content = "WebApplication.Account.Manage" });
        builder.Pop();

        var @namespace = new NamespaceDeclarationIntermediateNode() { Name = "default" };
        builder.Push(@namespace);

        var @class = new ClassDeclarationIntermediateNode() { Name = "default" };
        builder.Add(@class);

        documentNode.DocumentKind = MvcViewDocumentClassifierPass.MvcViewDocumentKind;

        // Act
        ProjectEngine.ExecutePass<NamespaceDirective.Pass>(codeDocument, documentNode);

        // Assert
        Assert.Equal("WebApplication.Account.Manage", @namespace.Name);
        Assert.Equal("default", @class.Name);
    }

    // This handles an error case where we can't determine the relationship between the
    // imports and the source.
    [Fact]
    public void Pass_SetsNamespace_VerbatimFromImports()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("ignored", filePath: "/Account/Manage/AddUser.cshtml");
        var codeDocument = ProjectEngine.CreateCodeDocument(source);

        var documentNode = new DocumentIntermediateNode();
        var builder = IntermediateNodeBuilder.Create(documentNode);

        builder.Push(new DirectiveIntermediateNode()
        {
            Directive = NamespaceDirective.Directive,
            Source = new SourceSpan(null, 0, 0, 0, 0),
        });

        builder.Add(new DirectiveTokenIntermediateNode() { Content = "WebApplication.Account" });
        builder.Pop();

        var @namespace = new NamespaceDeclarationIntermediateNode() { Name = "default" };
        builder.Push(@namespace);

        var @class = new ClassDeclarationIntermediateNode() { Name = "default" };
        builder.Add(@class);

        documentNode.DocumentKind = RazorPageDocumentClassifierPass.RazorPageDocumentKind;

        // Act
        ProjectEngine.ExecutePass<NamespaceDirective.Pass>(codeDocument, documentNode);

        // Assert
        Assert.Equal("WebApplication.Account", @namespace.Name);
        Assert.Equal("default", @class.Name);
    }

    [Fact]
    public void Pass_DoesNothing_ForUnknownDocumentKind()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("ignored", filePath: "/Account/Manage/AddUser.cshtml");
        var codeDocument = ProjectEngine.CreateCodeDocument(source);

        var documentNode = new DocumentIntermediateNode();
        var builder = IntermediateNodeBuilder.Create(documentNode);

        builder.Push(new DirectiveIntermediateNode()
        {
            Directive = NamespaceDirective.Directive,
            Source = new SourceSpan(null, 0, 0, 0, 0),
        });

        builder.Add(new DirectiveTokenIntermediateNode() { Content = "WebApplication.Account" });
        builder.Pop();

        var @namespace = new NamespaceDeclarationIntermediateNode() { Name = "default" };
        builder.Push(@namespace);

        var @class = new ClassDeclarationIntermediateNode() { Name = "default" };
        builder.Add(@class);

        documentNode.DocumentKind = null;

        // Act
        ProjectEngine.ExecutePass<NamespaceDirective.Pass>(codeDocument, documentNode);

        // Assert
        Assert.Equal("default", @namespace.Name);
        Assert.Equal("default", @class.Name);
    }
}
