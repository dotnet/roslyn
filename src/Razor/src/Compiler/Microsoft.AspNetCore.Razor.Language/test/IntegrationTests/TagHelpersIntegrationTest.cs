// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Roslyn.Test.Utilities;
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
    [WorkItem("https://github.com/dotnet/razor/issues/13206")]
    public void NestedPrefixedTagHelpers()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            CreateTagHelperDescriptor(
                tagName: "p",
                typeName: "PTagHelper",
                assemblyName: "TestAssembly"),
            CreateTagHelperDescriptor(
                tagName: "input",
                typeName: "InputTagHelper",
                assemblyName: "TestAssembly")
        ];

        var projectEngine = CreateProjectEngine(builder => builder.SetTagHelpers(tagHelpers));
        var projectItem = AddProjectItemFromText("""
            @tagHelperPrefix th:
            @addTagHelper *, TestAssembly
            <th:p><th:input /></th:p>
            """, filePath: "Index.cshtml");

        // Act
        var codeDocument = projectEngine.Process(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(codeDocument.GetRequiredDocumentNode());
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/13206")]
    public void NestedPrefixedTagHelpers_OrphanEndTag()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            CreateTagHelperDescriptor(
                tagName: "p",
                typeName: "PTagHelper",
                assemblyName: "TestAssembly"),
            CreateTagHelperDescriptor(
                tagName: "input",
                typeName: "InputTagHelper",
                assemblyName: "TestAssembly")
        ];

        var projectEngine = CreateProjectEngine(builder => builder.SetTagHelpers(tagHelpers));
        var projectItem = AddProjectItemFromText("""
            @tagHelperPrefix th:
            @addTagHelper *, TestAssembly
            <th:p>Hello</th:input></th:p>
            """, filePath: "Index.cshtml");

        // Act
        var codeDocument = projectEngine.Process(projectItem);

        // Assert
        var documentNode = codeDocument.GetRequiredDocumentNode();
        AssertDocumentNodeMatchesBaseline(documentNode);
        var diagnostic = Assert.Single(documentNode.GetAllDiagnostics());
        Assert.Equal("RZ1034", diagnostic.Id);
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
        var addTagHelperDirective = codeDocument.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<BaseRazorDirectiveSyntax>().ToImmutableArray().Single();
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
        var addTagHelperDirective = codeDocument.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<BaseRazorDirectiveSyntax>().ToImmutableArray().Single();
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
        var addTagHelperDirective = codeDocument.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<BaseRazorDirectiveSyntax>().ToImmutableArray().Single();
        var contributions = codeDocument.GetDirectiveTagHelperContributions();
        var contribution = Assert.Single(contributions);
        Assert.Equal(addTagHelperDirective.SpanStart, contribution.DirectiveSpanStart);
        Assert.NotEmpty(contribution.ContributedTagHelpers);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/68193")]
    public void ConsecutiveWithoutEndTagTagHelpers_AllBind()
    {
        // The HTML parser nests consecutive unclosed tags (`<alpha><beta><gamma>` becomes
        // alpha > beta > gamma). Each of these is a WithoutEndTag tag helper, so all three
        // must still bind as siblings rather than only the first.
        TagHelperCollection tagHelpers =
        [
            CreateTagHelperDescriptor(
                tagName: "alpha",
                typeName: "AlphaTagHelper",
                assemblyName: "TestAssembly",
                tagStructure: TagStructure.WithoutEndTag),
            CreateTagHelperDescriptor(
                tagName: "beta",
                typeName: "BetaTagHelper",
                assemblyName: "TestAssembly",
                tagStructure: TagStructure.WithoutEndTag),
            CreateTagHelperDescriptor(
                tagName: "gamma",
                typeName: "GammaTagHelper",
                assemblyName: "TestAssembly",
                tagStructure: TagStructure.WithoutEndTag),
        ];

        var projectEngine = CreateProjectEngine(builder => builder.SetTagHelpers(tagHelpers));
        var projectItem = AddProjectItemFromText("""
            @addTagHelper *, TestAssembly
            <head>
            <alpha>
            <beta>
            <gamma>
            </head>
            """, filePath: "Index.cshtml");

        // Act
        var codeDocument = projectEngine.Process(projectItem);

        // Assert
        var documentNode = codeDocument.GetRequiredDocumentNode();
        var tagHelperNodes = documentNode.FindDescendantNodes<TagHelperIntermediateNode>();
        Assert.Collection(tagHelperNodes,
            node => Assert.Equal("alpha", node.TagName),
            node => Assert.Equal("beta", node.TagName),
            node => Assert.Equal("gamma", node.TagName));

        // They are siblings, not nested: each WithoutEndTag helper promoted its body out, so
        // none of them contains another tag helper.
        Assert.All(tagHelperNodes, node => Assert.Empty(node.FindDescendantNodes<TagHelperIntermediateNode>()));
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/68193")]
    public void MixedNestedStartTagOnlyAndHtmlTagHelpers_AllResolveCorrectly()
    {
        // A deliberately tangled mix: a nestable tag helper (wrapper) containing consecutive
        // WithoutEndTag helpers (alpha, beta) and a normal one (bold), followed by a second
        // WithoutEndTag helper whose parser-nested body wraps a real HTML element (<div>) and
        // yet another WithoutEndTag helper. Every tag helper must bind, real HTML (<section>,
        // <div>) must stay markup, and document order must be preserved.
        // Note: custom (non-void) tag names are required -- the HTML parser self-terminates
        // real void elements like <input>/<br>, which wouldn't exercise the nesting-promotion path.
        TagHelperCollection tagHelpers =
        [
            CreateTagHelperDescriptor(
                tagName: "wrapper",
                typeName: "WrapperTagHelper",
                assemblyName: "TestAssembly"),
            CreateTagHelperDescriptor(
                tagName: "bold",
                typeName: "BoldTagHelper",
                assemblyName: "TestAssembly"),
            CreateTagHelperDescriptor(
                tagName: "alpha",
                typeName: "AlphaTagHelper",
                assemblyName: "TestAssembly",
                tagStructure: TagStructure.WithoutEndTag),
            CreateTagHelperDescriptor(
                tagName: "beta",
                typeName: "BetaTagHelper",
                assemblyName: "TestAssembly",
                tagStructure: TagStructure.WithoutEndTag),
        ];

        var projectEngine = CreateProjectEngine(builder => builder.SetTagHelpers(tagHelpers));
        var projectItem = AddProjectItemFromText("""
            @addTagHelper *, TestAssembly
            <section>
            <wrapper>
            <alpha>
            <beta>
            <bold>text</bold>
            </wrapper>
            <alpha>
            <div>plain html</div>
            <beta>
            </section>
            """, filePath: "Index.cshtml");

        // Act
        var codeDocument = projectEngine.Process(projectItem);

        // Assert: every tag helper binds, in document order, with the WithoutEndTag helpers
        // promoted to siblings rather than swallowing what follows them.
        var documentNode = codeDocument.GetRequiredDocumentNode();
        var tagHelperNodes = documentNode.FindDescendantNodes<TagHelperIntermediateNode>();
        Assert.Collection(tagHelperNodes,
            node => Assert.Equal("wrapper", node.TagName),
            node => Assert.Equal("alpha", node.TagName),
            node => Assert.Equal("beta", node.TagName),
            node => Assert.Equal("bold", node.TagName),
            node => Assert.Equal("alpha", node.TagName),
            node => Assert.Equal("beta", node.TagName));

        // Structure: wrapper genuinely contains alpha/beta/bold, while the WithoutEndTag helpers
        // stay flat -- their bodies were promoted out, so none nests another tag helper.
        Assert.Collection(tagHelperNodes[0].FindDescendantNodes<TagHelperIntermediateNode>(),
            node => Assert.Equal("alpha", node.TagName),
            node => Assert.Equal("beta", node.TagName),
            node => Assert.Equal("bold", node.TagName));
        Assert.Empty(tagHelperNodes[1].FindDescendantNodes<TagHelperIntermediateNode>()); // alpha inside wrapper
        Assert.Empty(tagHelperNodes[2].FindDescendantNodes<TagHelperIntermediateNode>()); // beta inside wrapper
        Assert.Empty(tagHelperNodes[4].FindDescendantNodes<TagHelperIntermediateNode>()); // alpha at top level
        Assert.Empty(tagHelperNodes[5].FindDescendantNodes<TagHelperIntermediateNode>()); // beta at top level

        // The real HTML elements must survive as literal markup, never bound as tag helpers.
        var generatedCode = codeDocument.GetRequiredCSharpDocument(declarationDocument: false).Text.ToString();
        Assert.Contains("<section>", generatedCode);
        Assert.Contains("<div>", generatedCode);
        Assert.Contains("plain html", generatedCode);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/68193")]
    public void PromotedStartTagOnlySibling_BindsUsingParentTagHelperContext()
    {
        // `child` is a WithoutEndTag helper that only matches when its parent is `wrapper`. The
        // parser nests it under the preceding WithoutEndTag helper `lead` (<lead><child>), so it
        // is reached only after `lead` promotes it to a sibling inside wrapper's body. That
        // re-resolution must carry wrapper as the parent-tag context, otherwise the
        // RequireParentTag("wrapper") rule can't match and `child` silently fails to bind.
        TagHelperCollection tagHelpers =
        [
            CreateTagHelperDescriptor(
                tagName: "wrapper",
                typeName: "WrapperTagHelper",
                assemblyName: "TestAssembly"),
            CreateTagHelperDescriptor(
                tagName: "lead",
                typeName: "LeadTagHelper",
                assemblyName: "TestAssembly",
                tagStructure: TagStructure.WithoutEndTag),
            CreateTagHelperDescriptor(
                tagName: "child",
                typeName: "ChildTagHelper",
                assemblyName: "TestAssembly",
                tagStructure: TagStructure.WithoutEndTag,
                parentTag: "wrapper"),
        ];

        var projectEngine = CreateProjectEngine(builder => builder.SetTagHelpers(tagHelpers));
        var projectItem = AddProjectItemFromText("""
            @addTagHelper *, TestAssembly
            <wrapper>
            <lead>
            <child>
            </wrapper>
            """, filePath: "Index.cshtml");

        // Act
        var codeDocument = projectEngine.Process(projectItem);

        // Assert: child binds because the promoted sibling was resolved with wrapper as its
        // parent-tag context.
        var documentNode = codeDocument.GetRequiredDocumentNode();
        var tagHelperNodes = documentNode.FindDescendantNodes<TagHelperIntermediateNode>();
        Assert.Collection(tagHelperNodes,
            node => Assert.Equal("wrapper", node.TagName),
            node => Assert.Equal("lead", node.TagName),
            node => Assert.Equal("child", node.TagName));
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/68193")]
    public void PromotedWithoutEndTagTagHelpers_Baseline()
    {
        // Full-IR structural baseline for the promotion scenario: a nestable helper containing
        // consecutive WithoutEndTag helpers plus a normal one, followed by a second WithoutEndTag
        // helper whose parser-nested body wraps real HTML and another helper. The .ir.txt baseline
        // captures the exact tree shape, proving the promoted helpers become siblings (not nested)
        // and that the real HTML survives as markup.
        TagHelperCollection tagHelpers =
        [
            CreateTagHelperDescriptor(
                tagName: "wrapper",
                typeName: "WrapperTagHelper",
                assemblyName: "TestAssembly"),
            CreateTagHelperDescriptor(
                tagName: "bold",
                typeName: "BoldTagHelper",
                assemblyName: "TestAssembly"),
            CreateTagHelperDescriptor(
                tagName: "alpha",
                typeName: "AlphaTagHelper",
                assemblyName: "TestAssembly",
                tagStructure: TagStructure.WithoutEndTag),
            CreateTagHelperDescriptor(
                tagName: "beta",
                typeName: "BetaTagHelper",
                assemblyName: "TestAssembly",
                tagStructure: TagStructure.WithoutEndTag),
        ];

        var projectEngine = CreateProjectEngine(builder => builder.SetTagHelpers(tagHelpers));
        var projectItem = CreateProjectItemFromFile();

        // Act
        var codeDocument = projectEngine.Process(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(codeDocument.GetRequiredDocumentNode());
    }

    private static TagHelperDescriptor CreateTagHelperDescriptor(
        string tagName,
        string typeName,
        string assemblyName,
        IEnumerable<Action<BoundAttributeDescriptorBuilder>>? attributes = null,
        TagStructure tagStructure = TagStructure.Unspecified,
        string? parentTag = null)
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

        builder.TagMatchingRuleDescriptor(ruleBuilder =>
        {
            ruleBuilder.RequireTagName(tagName).RequireTagStructure(tagStructure);

            if (parentTag != null)
            {
                ruleBuilder.RequireParentTag(parentTag);
            }
        });

        var descriptor = builder.Build();

        return descriptor;
    }
}
