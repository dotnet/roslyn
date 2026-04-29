// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.VisualStudio.Editor.Razor;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test;

public class TagHelperFactsServiceTest
{
    private static TagHelperCollection DefaultTagHelpers => SimpleTagHelpers.Default;

    [Fact]
    public void StringifyAttributes_DirectiveAttribute()
    {
        // Arrange
        var codeDocument = CreateCodeDocument($"<TestElement @test='abc' />", RazorFileKind.Component, TagHelperCollection.Create(DefaultTagHelpers));
        var root = codeDocument.GetRequiredSyntaxRoot();
        var startTag = (MarkupTagHelperStartTagSyntax)root.FindInnermostNode(3);

        // Act
        var attributes = TagHelperFacts.StringifyAttributes(startTag.Attributes);

        // Assert
        Assert.Collection(
            attributes,
            attribute =>
            {
                Assert.Equal("@test", attribute.Key);
                Assert.Equal("abc", attribute.Value);
            });
    }

    [Fact]
    public void StringifyAttributes_DirectiveAttributeWithParameter()
    {
        // Arrange
        var codeDocument = CreateCodeDocument($"<TestElement @test:something='abc' />", RazorFileKind.Component, TagHelperCollection.Create(DefaultTagHelpers));
        var root = codeDocument.GetRequiredSyntaxRoot();
        var startTag = (MarkupTagHelperStartTagSyntax)root.FindInnermostNode(3);

        // Act
        var attributes = TagHelperFacts.StringifyAttributes(startTag.Attributes);

        // Assert
        Assert.Collection(
            attributes,
            attribute =>
            {
                Assert.Equal("@test:something", attribute.Key);
                Assert.Equal("abc", attribute.Value);
            });
    }

    [Fact]
    public void StringifyAttributes_MinimizedDirectiveAttribute()
    {
        // Arrange
        var codeDocument = CreateCodeDocument($"<TestElement @minimized />", RazorFileKind.Component, [.. DefaultTagHelpers]);
        var root = codeDocument.GetRequiredSyntaxRoot();
        var startTag = (MarkupTagHelperStartTagSyntax)root.FindInnermostNode(3);

        // Act
        var attributes = TagHelperFacts.StringifyAttributes(startTag.Attributes);

        // Assert
        Assert.Collection(
            attributes,
            attribute =>
            {
                Assert.Equal("@minimized", attribute.Key);
                Assert.Equal(string.Empty, attribute.Value);
            });
    }

    [Fact]
    public void StringifyAttributes_MinimizedDirectiveAttributeWithParameter()
    {
        // Arrange
        var codeDocument = CreateCodeDocument($"<TestElement @minimized:something />", RazorFileKind.Component, [.. DefaultTagHelpers]);
        var root = codeDocument.GetRequiredSyntaxRoot();
        var startTag = (MarkupTagHelperStartTagSyntax)root.FindInnermostNode(3);

        // Act
        var attributes = TagHelperFacts.StringifyAttributes(startTag.Attributes);

        // Assert
        Assert.Collection(
            attributes,
            attribute =>
            {
                Assert.Equal("@minimized:something", attribute.Key);
                Assert.Equal(string.Empty, attribute.Value);
            });
    }

    [Fact]
    public void StringifyAttributes_TagHelperAttribute()
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper("WithBoundAttribute", "TestAssembly");
        tagHelper.SetTypeName("WithBoundAttribute", typeNamespace: null, typeNameIdentifier: null);
        tagHelper.TagMatchingRule(rule => rule.TagName = "test");
        tagHelper.BindAttribute(attribute =>
        {
            attribute.Name = "bound";
            attribute.PropertyName = "Bound";
            attribute.TypeName = typeof(bool).FullName;
        });
        var codeDocument = CreateCodeDocument("""
            @addTagHelper *, TestAssembly
            <test bound='true' />
            """, RazorFileKind.Legacy, TagHelperCollection.Create(tagHelper.Build()));
        var root = codeDocument.GetRequiredSyntaxRoot();
        var startTag = (MarkupTagHelperStartTagSyntax)root.FindInnermostNode(30 + Environment.NewLine.Length);

        // Act
        var attributes = TagHelperFacts.StringifyAttributes(startTag.Attributes);

        // Assert
        Assert.Collection(
            attributes,
            attribute =>
            {
                Assert.Equal("bound", attribute.Key);
                Assert.Equal("true", attribute.Value);
            });
    }

    [Fact]
    public void StringifyAttributes_MinimizedTagHelperAttribute()
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper("WithBoundAttribute", "TestAssembly");
        tagHelper.SetTypeName("WithBoundAttribute", typeNamespace: null, typeNameIdentifier: null);
        tagHelper.TagMatchingRule(rule => rule.TagName = "test");
        tagHelper.BindAttribute(attribute =>
        {
            attribute.Name = "bound";
            attribute.PropertyName = "Bound";
            attribute.TypeName = typeof(bool).FullName;
        });
        var codeDocument = CreateCodeDocument("""
            @addTagHelper *, TestAssembly
            <test bound />
            """, RazorFileKind.Legacy, TagHelperCollection.Create(tagHelper.Build()));
        var root = codeDocument.GetRequiredSyntaxRoot();
        var startTag = (MarkupTagHelperStartTagSyntax)root.FindInnermostNode(30 + Environment.NewLine.Length);

        // Act
        var attributes = TagHelperFacts.StringifyAttributes(startTag.Attributes);

        // Assert
        Assert.Collection(
            attributes,
            attribute =>
            {
                Assert.Equal("bound", attribute.Key);
                Assert.Equal(string.Empty, attribute.Value);
            });
    }

    [Fact]
    public void StringifyAttributes_UnboundAttribute()
    {
        // Arrange
        var codeDocument = CreateCodeDocument("""
            @addTagHelper *, TestAssembly
            <input unbound='hello world' />
            """, RazorFileKind.Legacy, DefaultTagHelpers);
        var root = codeDocument.GetRequiredSyntaxRoot();
        var startTag = (MarkupStartTagSyntax)root.FindInnermostNode(30 + Environment.NewLine.Length);

        // Act
        var attributes = TagHelperFacts.StringifyAttributes(startTag.Attributes);

        // Assert
        Assert.Collection(
            attributes,
            attribute =>
            {
                Assert.Equal("unbound", attribute.Key);
                Assert.Equal("hello world", attribute.Value);
            });
    }

    [Fact]
    public void StringifyAttributes_UnboundMinimizedAttribute()
    {
        // Arrange
        var codeDocument = CreateCodeDocument("""
            @addTagHelper *, TestAssembly
            <input unbound />
            """, RazorFileKind.Legacy, DefaultTagHelpers);
        var root = codeDocument.GetRequiredSyntaxRoot();
        var startTag = (MarkupStartTagSyntax)root.FindInnermostNode(30 + Environment.NewLine.Length);

        // Act
        var attributes = TagHelperFacts.StringifyAttributes(startTag.Attributes);

        // Assert
        Assert.Collection(
            attributes,
            attribute =>
            {
                Assert.Equal("unbound", attribute.Key);
                Assert.Equal(string.Empty, attribute.Value);
            });
    }

    [Fact]
    public void StringifyAttributes_IgnoresMiscContent()
    {
        // Arrange
        var codeDocument = CreateCodeDocument("""
            @addTagHelper *, TestAssembly
            <input unbound @DateTime.Now />
            """, RazorFileKind.Legacy, DefaultTagHelpers);
        var root = codeDocument.GetRequiredSyntaxRoot();
        var startTag = (MarkupStartTagSyntax)root.FindInnermostNode(30 + Environment.NewLine.Length);

        // Act
        var attributes = TagHelperFacts.StringifyAttributes(startTag.Attributes);

        // Assert
        Assert.Collection(
            attributes,
            attribute =>
            {
                Assert.Equal("unbound", attribute.Key);
                Assert.Equal(string.Empty, attribute.Value);
            });
    }

    private static RazorCodeDocument CreateCodeDocument(string text, RazorFileKind fileKind, TagHelperCollection tagHelpers)
    {
        tagHelpers ??= [];
        var sourceDocument = TestRazorSourceDocument.Create(text);
        var projectEngine = RazorProjectEngine.Create(builder =>
        {
            builder.ConfigureParserOptions(builder =>
            {
                builder.UseRoslynTokenizer = true;
            });
        });

        return projectEngine.Process(sourceDocument, fileKind, importSources: default, tagHelpers);
    }
}
