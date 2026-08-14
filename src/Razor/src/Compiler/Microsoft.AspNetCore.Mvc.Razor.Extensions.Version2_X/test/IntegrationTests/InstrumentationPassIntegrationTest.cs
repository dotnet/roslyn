// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.IntegrationTests;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X.IntegrationTests;

public class InstrumentationPassIntegrationTest : IntegrationTestBase
{
    private static readonly CSharpCompilation DefaultBaseCompilation = MvcShim.BaseCompilation.WithAssemblyName("AppCode");

    public InstrumentationPassIntegrationTest()
        : base(layer: TestProject.Layer.Compiler, projectDirectoryHint: "Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X")
    {
        Configuration = new(RazorLanguageVersion.Version_2_0, "MVC-2.1", Extensions: []);
    }

    protected override CSharpCompilation BaseCompilation => DefaultBaseCompilation;

    protected override RazorConfiguration Configuration { get; }

    [Fact]
    public void BasicTest()
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
                        .TypeName("System.String"),      // Gets preallocated
                    builder => builder
                        .Name("date")
                        .PropertyName("BarProp")
                        .TypeName("System.DateTime"),    // Doesn't get preallocated
                ])
        ];

        var engine = CreateProjectEngine(b =>
        {
            b.SetTagHelpers(tagHelpers);
            b.Features.Add(new InstrumentationPass());

                // This test includes templates
                b.AddTargetExtension(new TemplateTargetExtension());
        });

        var projectItem = CreateProjectItemFromFile();

        // Act
        var document = engine.Process(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(document.GetDocumentNode());

        var csharpDocument = document.GetCSharpDocument();
        AssertCSharpDocumentMatchesBaseline(csharpDocument);
        Assert.Empty(csharpDocument.Diagnostics);
    }

    private static TagHelperDescriptor CreateTagHelperDescriptor(
        string tagName,
        string typeName,
        string assemblyName,
        IEnumerable<Action<BoundAttributeDescriptorBuilder>> attributes = null)
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
