// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.AspNetCore.Razor.Language.Intermediate.IntermediateNodeAssert;

namespace Microsoft.AspNetCore.Razor.Language;

public class DefaultRazorIntermediateNodeLoweringPhaseIntegrationTest : RazorProjectEngineTestBase
{
    protected override RazorLanguageVersion Version => RazorLanguageVersion.Latest;

    protected override void ConfigureProjectEngine(RazorProjectEngineBuilder builder)
    {
        SectionDirective.Register(builder);
    }

    protected override void ConfigureCodeDocumentProcessor(RazorCodeDocumentProcessor processor)
    {
        // Inspect the lowered + resolved IR without the classification wrapping or decl lowering, which
        // would reshape the tree the baselines assert on.
        processor.ExecutePhasesThroughExcept<DefaultTagHelperResolutionPhase>(
            typeof(DefaultRazorDocumentClassifierPhase),
            typeof(DefaultRazorDirectiveClassifierPhase),
            typeof(DefaultRazorDeclCSharpLoweringPhase));
    }

    [Fact]
    public void Lower_SetsOptions_Defaults()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateEmptyCodeDocument();
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        var documentNode = processor.GetDocumentNode();

        // Assert
        Assert.NotNull(documentNode.Options);
        Assert.Equal(4, documentNode.Options.IndentSize);
        Assert.False(documentNode.Options.IndentWithTabs);
    }

    [Fact]
    public void Lower_SetsOptions_RunsConfigureCallbacks()
    {
        // Arrange
        var projectEngine = CreateProjectEngine(builder =>
        {
            builder.ConfigureCodeGenerationOptions(builder =>
            {
                builder.IndentSize = 17;
                builder.IndentWithTabs = true;
                builder.SuppressChecksum = true;
            });
        });

        var codeDocument = projectEngine.CreateEmptyCodeDocument();
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        var documentNode = processor.GetDocumentNode();

        // Assert
        Assert.NotNull(documentNode.Options);
        Assert.Equal(17, documentNode.Options.IndentSize);
        Assert.True(documentNode.Options.IndentWithTabs);
        Assert.True(documentNode.Options.SuppressChecksum);
    }

    [Fact]
    public void Lower_HelloWorld()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument("Hello, World!");
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        var documentNode = processor.GetDocumentNode();

        // Assert
        Children(documentNode,
            n => Html("Hello, World!", n));
    }

    [Fact]
    public void Lower_HtmlWithDataDashAttributes()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument(@"
<html>
    <body>
        <span data-val=""@Hello"" />
    </body>
</html>");

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        var documentNode = processor.GetDocumentNode();

        // Assert
        Children(documentNode,
            n => Html(
@"
<html>
    <body>
        <span data-val=""", n),
            n => CSharpExpression("Hello", n),
            n => Html(@""" />
    </body>
</html>", n));
    }

    [Fact]
    public void Lower_HtmlWithConditionalAttributes()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument(@"
<html>
    <body>
        <span val=""@Hello World"" />
    </body>
</html>");

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        var documentNode = processor.GetDocumentNode();

        // Assert
        Children(documentNode,
            n => Html(
@"
<html>
    <body>
        <span", n),

            n => ConditionalAttribute(
                prefix: " val=\"",
                name: "val",
                suffix: "\"",
                node: n,
                valueValidators: [
                    value => CSharpExpressionAttributeValue(string.Empty, "Hello", value),
                    value => LiteralAttributeValue(" ",  "World", value)
                ]),
            n => Html(@" />
    </body>
</html>", n));
    }

    [Fact]
    public void Lower_WithFunctions()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument(@"@functions { public int Foo { get; set; }}");
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        var documentNode = processor.GetDocumentNode();

        // Assert
        Children(documentNode,
            n => Directive(
                "functions",
                n,
                c => Assert.IsType<CSharpCodeIntermediateNode>(c)));
    }

    [Fact]
    public void Lower_WithUsing()
    {
        // Arrange
        var codeDocument = ProjectEngine.CreateCodeDocument(@"@using System");
        var processor = CreateCodeDocumentProcessor(codeDocument);
        var expectedSourceLocation = new SourceSpan(codeDocument.Source.FilePath, 1, 0, 1, 12);

        // Act
        var documentNode = processor.GetDocumentNode();

        // Assert
        Children(documentNode,
            n =>
            {
                Using("System", n);
                Assert.Equal(expectedSourceLocation, n.Source);
            });
    }

    [Fact]
    public void Lower_TagHelpers()
    {
        // Arrange
        var tagHelper = CreateTagHelperDescriptor(
            tagName: "span",
            typeName: "SpanTagHelper",
            assemblyName: "TestAssembly");

        var codeDocument = ProjectEngine.CreateCodeDocument(@"@addTagHelper *, TestAssembly
<span val=""@Hello World""></span>",
            [tagHelper]);

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        var documentNode = processor.GetDocumentNode();

        // Assert
        Children(documentNode,
            n => Directive(
                SyntaxConstants.CSharp.AddTagHelperKeyword,
                n,
                v => DirectiveToken(DirectiveTokenKind.String, "*, TestAssembly", v)),
            n => TagHelper(
                "span",
                TagMode.StartTagAndEndTag,
                [tagHelper],
                n,
                c => Assert.IsType<TagHelperBodyIntermediateNode>(c),
                c => TagHelperHtmlAttribute(
                    "val",
                    AttributeStructure.DoubleQuotes,
                    c,
                    v => CSharpExpressionAttributeValue(string.Empty, "Hello", v),
                    v => LiteralAttributeValue(" ", "World", v))));
    }

    [Fact]
    public void Lower_TagHelpers_WithPrefix()
    {
        // Arrange
        var tagHelper = CreateTagHelperDescriptor(
            tagName: "span",
            typeName: "SpanTagHelper",
            assemblyName: "TestAssembly");

        var codeDocument = ProjectEngine.CreateCodeDocument(@"@addTagHelper *, TestAssembly
@tagHelperPrefix cool:
<cool:span val=""@Hello World""></cool:span>",
            [tagHelper]);

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        var documentNode = processor.GetDocumentNode();

        // Assert
        Children(documentNode,
            n => Directive(
                SyntaxConstants.CSharp.AddTagHelperKeyword,
                n,
                v => DirectiveToken(DirectiveTokenKind.String, "*, TestAssembly", v)),
            n => Directive(
                SyntaxConstants.CSharp.TagHelperPrefixKeyword,
                n,
                v => DirectiveToken(DirectiveTokenKind.String, "cool:", v)),
            n => TagHelper(
                "span",  // Note: this is span not cool:span
                TagMode.StartTagAndEndTag,
                [tagHelper],
                n,
                c => Assert.IsType<TagHelperBodyIntermediateNode>(c),
                c => TagHelperHtmlAttribute(
                    "val",
                    AttributeStructure.DoubleQuotes,
                    c,
                    v => CSharpExpressionAttributeValue(string.Empty, "Hello", v),
                    v => LiteralAttributeValue(" ", "World", v))));
    }

    [Fact]
    public void Lower_TagHelper_InSection()
    {
        // Arrange
        var tagHelper = CreateTagHelperDescriptor(
            tagName: "span",
            typeName: "SpanTagHelper",
            assemblyName: "TestAssembly");

        var codeDocument = ProjectEngine.CreateCodeDocument(@"@addTagHelper *, TestAssembly
@section test {
<span val=""@Hello World""></span>
}",
            [tagHelper]);

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        var documentNode = processor.GetDocumentNode();

        // Assert
        Children(
            documentNode,
            n => Directive(
                SyntaxConstants.CSharp.AddTagHelperKeyword,
                n,
                v => DirectiveToken(DirectiveTokenKind.String, "*, TestAssembly", v)),
            n => Directive(
                "section",
                n,
                c1 => DirectiveToken(DirectiveTokenKind.Member, "test", c1),
                c1 => Html(Environment.NewLine, c1),
                c1 => TagHelper(
                    "span",
                    TagMode.StartTagAndEndTag,
                    [tagHelper],
                    c1,
                    c2 => Assert.IsType<TagHelperBodyIntermediateNode>(c2),
                    c2 => TagHelperHtmlAttribute(
                        "val",
                        AttributeStructure.DoubleQuotes,
                        c2,
                        v => CSharpExpressionAttributeValue(string.Empty, "Hello", v),
                        v => LiteralAttributeValue(" ", "World", v))),
                c1 => Html(Environment.NewLine, c1)));
    }

    [Fact, WorkItem("https://github.com/microsoft/vscode-dotnettools/issues/3210")]
    public void Lower_TagHelperWithAllowedChildren_AllowsChildTagHelpersInsideCSharpBlock()
    {
        // Arrange
        var contactListTagHelper = CreateTagHelperDescriptorWithAllowedChild(
            tagName: "my-contact-list",
            typeName: "ContactListTagHelper",
            assemblyName: "TestAssembly",
            allowedChildTag: "my-contact");

        var contactTagHelper = CreateTagHelperDescriptor(
            tagName: "my-contact",
            typeName: "ContactTagHelper",
            assemblyName: "TestAssembly");

        var codeDocument = ProjectEngine.CreateCodeDocument(
            """
            @addTagHelper *, TestAssembly
            <my-contact-list>
                @foreach (var contact in Model)
                {
                    <my-contact></my-contact>
                }
            </my-contact-list>
            """,
            [contactListTagHelper, contactTagHelper]);

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        var documentNode = processor.GetDocumentNode();

        // Assert
        Assert.Empty(documentNode.GetAllDiagnostics());
    }

    [Fact]
    public void Lower_TagHelperWithAllowedChildren_ReportsInvalidChildInsideCSharpBlock()
    {
        // Arrange
        var contactListTagHelper = CreateTagHelperDescriptorWithAllowedChild(
            tagName: "my-contact-list",
            typeName: "ContactListTagHelper",
            assemblyName: "TestAssembly",
            allowedChildTag: "my-contact");

        var codeDocument = ProjectEngine.CreateCodeDocument(
            """
            @addTagHelper *, TestAssembly
            <my-contact-list>
                @foreach (var contact in Model)
                {
                    <div></div>
                }
            </my-contact-list>
            """,
            [contactListTagHelper]);

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        var documentNode = processor.GetDocumentNode();

        // Assert
        var diagnostic = Assert.Single(documentNode.GetAllDiagnostics());
        Assert.Equal("RZ2010", diagnostic.Id);
    }

    [Fact]
    public void Lower_TagHelperWithAllowedChildren_AllowsPrefixedTagHelperChild()
    {
        // Arrange
        var contactListTagHelper = CreateTagHelperDescriptorWithAllowedChild(
            tagName: "my-contact-list",
            typeName: "ContactListTagHelper",
            assemblyName: "TestAssembly",
            allowedChildTag: "my-contact");

        var contactTagHelper = CreateTagHelperDescriptor(
            tagName: "my-contact",
            typeName: "ContactTagHelper",
            assemblyName: "TestAssembly");

        var codeDocument = ProjectEngine.CreateCodeDocument(
            """
            @addTagHelper *, TestAssembly
            @tagHelperPrefix th:
            <th:my-contact-list>
                <th:my-contact></th:my-contact>
            </th:my-contact-list>
            """,
            [contactListTagHelper, contactTagHelper]);

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        var documentNode = processor.GetDocumentNode();

        // Assert
        Assert.Collection(
            documentNode.FindDescendantNodes<TagHelperIntermediateNode>(),
            node => Assert.Equal("my-contact-list", node.TagName),
            node => Assert.Equal("my-contact", node.TagName));
        Assert.Empty(documentNode.GetAllDiagnostics());
    }

    [Fact]
    public void Lower_TagHelperWithAllowedChildren_ReportsDisallowedPrefixedTagHelperChild()
    {
        // Arrange
        var contactListTagHelper = CreateTagHelperDescriptorWithAllowedChild(
            tagName: "my-contact-list",
            typeName: "ContactListTagHelper",
            assemblyName: "TestAssembly",
            allowedChildTag: "my-contact");

        var otherContactTagHelper = CreateTagHelperDescriptor(
            tagName: "other-contact",
            typeName: "OtherContactTagHelper",
            assemblyName: "TestAssembly");

        var codeDocument = ProjectEngine.CreateCodeDocument(
            """
            @addTagHelper *, TestAssembly
            @tagHelperPrefix th:
            <th:my-contact-list>
                <th:other-contact></th:other-contact>
            </th:my-contact-list>
            """,
            [contactListTagHelper, otherContactTagHelper]);

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        var documentNode = processor.GetDocumentNode();

        // Assert
        var diagnostic = Assert.Single(documentNode.GetAllDiagnostics());
        Assert.Equal("RZ2010", diagnostic.Id);
        Assert.Contains("<other-contact>", diagnostic.GetMessage());
    }

    [Fact]
    public void Lower_TagHelperWithAllowedChildren_ReportsPrefixLookingPlainChild()
    {
        // Arrange
        var contactListTagHelper = CreateTagHelperDescriptorWithAllowedChild(
            tagName: "my-contact-list",
            typeName: "ContactListTagHelper",
            assemblyName: "TestAssembly",
            allowedChildTag: "div");

        var codeDocument = ProjectEngine.CreateCodeDocument(
            """
            @addTagHelper *, TestAssembly
            @tagHelperPrefix th:
            <th:my-contact-list>
                <th:div></th:div>
            </th:my-contact-list>
            """,
            [contactListTagHelper]);

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        var documentNode = processor.GetDocumentNode();

        // Assert
        var diagnostic = Assert.Single(documentNode.GetAllDiagnostics());
        Assert.Equal("RZ2010", diagnostic.Id);
        Assert.Contains("<th:div>", diagnostic.GetMessage());
    }

    [Fact]
    public void Lower_TagHelperWithAllowedChildren_AllowsEscapedAllowedChild()
    {
        // Arrange
        var contactListTagHelper = CreateTagHelperDescriptorWithAllowedChild(
            tagName: "my-contact-list",
            typeName: "ContactListTagHelper",
            assemblyName: "TestAssembly",
            allowedChildTag: "my-contact");

        var codeDocument = ProjectEngine.CreateCodeDocument(
            """
            @addTagHelper *, TestAssembly
            <my-contact-list>
                <!my-contact></!my-contact>
            </my-contact-list>
            """,
            [contactListTagHelper]);

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        var documentNode = processor.GetDocumentNode();

        // Assert
        Assert.Empty(documentNode.GetAllDiagnostics());
    }

    [Fact]
    public void Lower_TagHelperWithAllowedChildren_ReportsEscapedDisallowedChild()
    {
        // Arrange
        var contactListTagHelper = CreateTagHelperDescriptorWithAllowedChild(
            tagName: "my-contact-list",
            typeName: "ContactListTagHelper",
            assemblyName: "TestAssembly",
            allowedChildTag: "my-contact");

        var codeDocument = ProjectEngine.CreateCodeDocument(
            """
            @addTagHelper *, TestAssembly
            <my-contact-list>
                <!div></!div>
            </my-contact-list>
            """,
            [contactListTagHelper]);

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        var documentNode = processor.GetDocumentNode();

        // Assert
        var diagnostic = Assert.Single(documentNode.GetAllDiagnostics());
        Assert.Equal("RZ2010", diagnostic.Id);
        Assert.Contains("<div>", diagnostic.GetMessage());
    }

    [Fact]
    public void Lower_TagHelperWithAllowedChildren_ReportsNonTagContent()
    {
        // Arrange
        var contactListTagHelper = CreateTagHelperDescriptorWithAllowedChild(
            tagName: "my-contact-list",
            typeName: "ContactListTagHelper",
            assemblyName: "TestAssembly",
            allowedChildTag: "my-contact");

        var codeDocument = ProjectEngine.CreateCodeDocument(
            """
            @addTagHelper *, TestAssembly
            <my-contact-list>
                Text
            </my-contact-list>
            """,
            [contactListTagHelper]);

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        var documentNode = processor.GetDocumentNode();

        // Assert
        var diagnostic = Assert.Single(documentNode.GetAllDiagnostics());
        Assert.Equal("RZ2009", diagnostic.Id);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/13219")]
    public void Lower_TagHelperWithAllowedChildren_AllowsContentInsideAllowedMarkupChild()
    {
        // Arrange
        var formTableTagHelper = CreateTagHelperDescriptor(
            tagName: "form-table",
            typeName: "FormTableTagHelper",
            assemblyName: "TestAssembly");

        var formTableHeadTagHelper = CreateTagHelperDescriptorWithAllowedChild(
            tagName: "form-table-head",
            typeName: "FormTableHeadTagHelper",
            assemblyName: "TestAssembly",
            allowedChildTag: "th",
            parentTag: "form-table");

        var codeDocument = ProjectEngine.CreateCodeDocument(
            """
            @addTagHelper *, TestAssembly
            <form-table>
                <form-table-head>
                    <th>Id</th>
                </form-table-head>
            </form-table>
            """,
            [formTableTagHelper, formTableHeadTagHelper]);

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        var documentNode = processor.GetDocumentNode();

        // Assert
        Assert.Collection(
            documentNode.FindDescendantNodes<TagHelperIntermediateNode>(),
            node => Assert.Equal("form-table", node.TagName),
            node => Assert.Equal("form-table-head", node.TagName));
        Assert.Empty(documentNode.GetAllDiagnostics());
    }

    [Fact]
    public void Lower_TagHelpersWithBoundAttribute()
    {
        // Arrange
        var tagHelper = CreateTagHelperDescriptor(
            tagName: "input",
            typeName: "InputTagHelper",
            assemblyName: "TestAssembly",
            attributes: [builder => builder
                .Name("bound")
                .PropertyName("FooProp")
                .TypeName("System.String")]);

        var codeDocument = ProjectEngine.CreateCodeDocument(@"@addTagHelper *, TestAssembly
<input bound='foo' />",
            [tagHelper]);

        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        var documentNode = processor.GetDocumentNode();

        // Assert
        Children(
            documentNode,
            n => Directive(
                SyntaxConstants.CSharp.AddTagHelperKeyword,
                n,
                v => DirectiveToken(DirectiveTokenKind.String, "*, TestAssembly", v)),
            n => TagHelper(
                "input",
                TagMode.SelfClosing,
                [tagHelper],
                n,
                c => Assert.IsType<TagHelperBodyIntermediateNode>(c),
                c => SetTagHelperProperty(
                    "bound",
                    "FooProp",
                    AttributeStructure.SingleQuotes,
                    c,
                    v => Html("foo", v))));
    }

    [Fact]
    public void Lower_WithImports_Using()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create(@"@using System.Threading.Tasks
<p>Hi!</p>");
        var importSource1 = TestRazorSourceDocument.Create("@using System.Globalization");
        var importSource2 = TestRazorSourceDocument.Create("@using System.Text");
        var codeDocument = ProjectEngine.CreateCodeDocument(source, [importSource1, importSource2]);
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        var documentNode = processor.GetDocumentNode();

        // Assert
        Children(
            documentNode,
            n => Using("System.Globalization", n),
            n => Using("System.Text", n),
            n => Using("System.Threading.Tasks", n),
            n => Html("<p>Hi!</p>", n));
    }

    [Fact]
    public void Lower_WithImports_AllowsIdenticalNamespacesInPrimaryDocument()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create(@"@using System.Threading.Tasks
@using System.Threading.Tasks");
        var importSource = TestRazorSourceDocument.Create("@using System.Threading.Tasks");
        var codeDocument = ProjectEngine.CreateCodeDocument(source, [importSource]);
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        var documentNode = processor.GetDocumentNode();

        // Assert
        Children(
            documentNode,
            n => Using("System.Threading.Tasks", n),
            n => Using("System.Threading.Tasks", n));
    }

    [Fact]
    public void Lower_WithMultipleImports_SingleLineFileScopedSinglyOccurring()
    {
        // Arrange
        var projectEngine = CreateProjectEngine(builder =>
        {
            var directive = DirectiveDescriptor.CreateDirective(
                "test",
                DirectiveKind.SingleLine,
                builder =>
                {
                    builder.AddMemberToken();
                    builder.Usage = DirectiveUsage.FileScopedSinglyOccurring;
                });

            builder.AddDirective(directive);
        });

        var source = TestRazorSourceDocument.Create("<p>Hi!</p>");
        var importSource1 = TestRazorSourceDocument.Create("@test value1");
        var importSource2 = TestRazorSourceDocument.Create("@test value2");
        var codeDocument = projectEngine.CreateCodeDocument(source, [importSource1, importSource2]);
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        var documentNode = processor.GetDocumentNode();

        // Assert
        Children(
            documentNode,
            n => Directive("test", n, c => DirectiveToken(DirectiveTokenKind.Member, "value2", c)),
            n => Html("<p>Hi!</p>", n));
    }

    [Fact]
    public void Lower_WithImports_IgnoresBlockDirective()
    {
        // Arrange
        var projectEngine = CreateProjectEngine(builder =>
        {
            var directive = DirectiveDescriptor.CreateDirective("block", DirectiveKind.RazorBlock, d => d.AddMemberToken());

            builder.AddDirective(directive);
        });

        var source = TestRazorSourceDocument.Create("<p>Hi!</p>");
        var importSource = TestRazorSourceDocument.Create("@block token { }");
        var codeDocument = projectEngine.CreateCodeDocument(source, [importSource]);
        var processor = CreateCodeDocumentProcessor(codeDocument);

        // Act
        var documentNode = processor.GetDocumentNode();

        // Assert
        Children(
            documentNode,
            n => Html("<p>Hi!</p>", n));
    }

    private static TagHelperDescriptor CreateTagHelperDescriptor(
        string tagName,
        string typeName,
        string assemblyName,
        params ReadOnlySpan<Action<BoundAttributeDescriptorBuilder>> attributes)
    {
        var builder = TagHelperDescriptorBuilder.CreateTagHelper(typeName, assemblyName);
        builder.SetTypeName(typeName, typeNamespace: null, typeNameIdentifier: null);

        foreach (var attributeBuilder in attributes)
        {
            builder.BoundAttributeDescriptor(attributeBuilder);
        }

        builder.TagMatchingRuleDescriptor(ruleBuilder => ruleBuilder.RequireTagName(tagName));

        return builder.Build();
    }

    private static TagHelperDescriptor CreateTagHelperDescriptorWithAllowedChild(
        string tagName,
        string typeName,
        string assemblyName,
        string allowedChildTag,
        string? parentTag = null)
    {
        var builder = TagHelperDescriptorBuilder.CreateTagHelper(typeName, assemblyName);
        builder.SetTypeName(typeName, typeNamespace: null, typeNameIdentifier: null);
        builder.TagMatchingRuleDescriptor(ruleBuilder =>
        {
            ruleBuilder.RequireTagName(tagName);

            if (parentTag is not null)
            {
                ruleBuilder.RequireParentTag(parentTag);
            }
        });
        builder.AllowChildTag(allowedChildTag);

        return builder.Build();
    }
}
