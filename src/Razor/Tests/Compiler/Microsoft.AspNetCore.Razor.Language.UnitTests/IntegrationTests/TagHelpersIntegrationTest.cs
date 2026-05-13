// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

public class TagHelpersIntegrationTest() : IntegrationTestBase(layer: TestProject.Layer.Compiler)
{
    [Fact]
    public void SimpleTagHelpers()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            CreateTagHelperDescriptor(
                tagName: "input",
                typeName: "InputTagHelper",
                assemblyName: "TestAssembly")
        ];

        var projectEngine = CreateProjectEngine(builder => builder.SetTagHelpers(tagHelpers));
        var projectItem = CreateProjectItemFromFile();

        // Act
        var codeDocument = projectEngine.Process(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(codeDocument.GetRequiredDocumentNode());
    }

    [Fact]
    public void TagHelpersWithBoundAttributes()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            CreateTagHelperDescriptor(
                tagName: "input",
                typeName: "InputTagHelper",
                assemblyName: "TestAssembly",
                attributes:
                [
                    builder => builder
                        .Name("bound")
                        .PropertyName("FooProp")
                        .TypeName("System.String"),
                ])
        ];

        var projectEngine = CreateProjectEngine(builder => builder.SetTagHelpers(tagHelpers));
        var projectItem = CreateProjectItemFromFile();

        // Act
        var codeDocument = projectEngine.Process(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(codeDocument.GetRequiredDocumentNode());
    }

    [Fact]
    public void NestedTagHelpers()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            CreateTagHelperDescriptor(
                tagName: "p",
                typeName: "PTagHelper",
                assemblyName: "TestAssembly"),
            CreateTagHelperDescriptor(
                tagName: "form",
                typeName: "FormTagHelper",
                assemblyName: "TestAssembly"),
            CreateTagHelperDescriptor(
                tagName: "input",
                typeName: "InputTagHelper",
                assemblyName: "TestAssembly",
                attributes:
                [
                    builder => builder
                        .Name("value")
                        .PropertyName("FooProp")
                        .TypeName("System.String"),
                ])
        ];

        var projectEngine = CreateProjectEngine(builder => builder.SetTagHelpers(tagHelpers));
        var projectItem = CreateProjectItemFromFile();

        // Act
        var codeDocument = projectEngine.Process(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(codeDocument.GetRequiredDocumentNode());
    }

    [Fact]
    public void AddTagHelperDirective_IsUnused_WhenNoTagHelpersReferenced()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            CreateTagHelperDescriptor(
                tagName: "input",
                typeName: "InputTagHelper",
                assemblyName: "TestAssembly")
        ];

        var projectEngine = CreateProjectEngine(builder => builder.SetTagHelpers(tagHelpers));
        var projectItem = AddProjectItemFromText("""
            @addTagHelper *, TestAssembly
            <div>Hello</div>
            """, filePath: "Index.cshtml");

        // Act
        var codeDocument = projectEngine.Process(projectItem);

        // Assert
        var addTagHelperDirective = codeDocument.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<BaseRazorDirectiveSyntax>().Single();
        Assert.False(codeDocument.IsDirectiveUsed(addTagHelperDirective));
    }

    [Fact]
    public void AddTagHelperDirective_IsUsed_WhenTagHelperReferenced()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            CreateTagHelperDescriptor(
                tagName: "input",
                typeName: "InputTagHelper",
                assemblyName: "TestAssembly")
        ];

        var projectEngine = CreateProjectEngine(builder => builder.SetTagHelpers(tagHelpers));
        var projectItem = AddProjectItemFromText("""
            @addTagHelper *, TestAssembly
            <input />
            """, filePath: "Index.cshtml");

        // Act
        var codeDocument = projectEngine.Process(projectItem);

        // Assert
        var addTagHelperDirective = codeDocument.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<BaseRazorDirectiveSyntax>().Single();
        Assert.True(codeDocument.IsDirectiveUsed(addTagHelperDirective));
    }

    [Fact]
    public void AddTagHelperDirective_StoresDirectiveTagHelperContributions()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            CreateTagHelperDescriptor(
                tagName: "input",
                typeName: "InputTagHelper",
                assemblyName: "TestAssembly")
        ];

        var projectEngine = CreateProjectEngine(builder => builder.SetTagHelpers(tagHelpers));
        var projectItem = AddProjectItemFromText("""
            @addTagHelper *, TestAssembly
            <div>Hello</div>
            """, filePath: "Index.cshtml");

        // Act
        var codeDocument = projectEngine.Process(projectItem);

        // Assert
        var addTagHelperDirective = codeDocument.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<BaseRazorDirectiveSyntax>().Single();
        var contributions = codeDocument.GetDirectiveTagHelperContributions();
        var contribution = Assert.Single(contributions);
        Assert.Equal(addTagHelperDirective.SpanStart, contribution.DirectiveSpanStart);
        Assert.NotEmpty(contribution.ContributedTagHelpers);
    }

    private static TagHelperDescriptor CreateTagHelperDescriptor(
        string tagName,
        string typeName,
        string assemblyName,
        IEnumerable<Action<BoundAttributeDescriptorBuilder>>? attributes = null)
    {
        var builder = TagHelperDescriptorBuilder.CreateTagHelper(typeName, assemblyName);
        builder.SetTypeName(typeName, typeNamespace: null, typeNameIdentifier: null);

        if (attributes != null)
        {
            foreach (var attributeBuilder in attributes)
            {
                builder.BoundAttributeDescriptor(attributeBuilder);
            }
        }

        builder.TagMatchingRuleDescriptor(ruleBuilder => ruleBuilder.RequireTagName(tagName));

        var descriptor = builder.Build();

        return descriptor;
    }
}
