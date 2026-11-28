// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.Tooltip;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Remote.Razor.Tooltip;

public class MarkupTagHelperTooltipFactoryTest(ITestOutputHelper testOutput) : DocCommentFormattingTestBase(testOutput)
{
    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85487")]
    public async Task TryCreateTooltip_PlainText_TypeReferencesUseNamespaceImports()
    {
        var document = CreateGeneratedDocument("""
            namespace MyApp
            {
                using System.Collections.Generic;
                public class Component { }
            }
            """);
        var formattingContext = await DocCommentFormattingContext.CreateAsync(document, DisposalToken);
        var description = new AggregateBoundElementDescription(
            [new BoundElementDescriptionInfo("MyApp.Component", "<summary>Uses <see cref=\"T:System.Collections.Generic.List`1\"/>.</summary>")]);

        var result = await MarkupTagHelperTooltipFactory.TryCreateTooltipAsync(
            documentFilePath: null, description, componentAvailabilityService: null!, MarkupKind.PlainText, formattingContext, DisposalToken);

        Assert.Equal($"MyApp.Component{Environment.NewLine}{Environment.NewLine}Uses List<T>.", result?.Value);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85487")]
    public async Task TryCreateTooltip_PlainText_MemberReferencesUseCrefFormat()
    {
        var formattingContext = await CreateFormattingContextAsync();
        var description = new AggregateBoundElementDescription(
            [new BoundElementDescriptionInfo("MyApp.Component", "<summary>Uses <see cref=\"M:System.String.IsNullOrEmpty(System.String)\"/> and <see cref=\"P:System.String.Length\"/>.</summary>")]);

        var result = await MarkupTagHelperTooltipFactory.TryCreateTooltipAsync(
            documentFilePath: null, description, componentAvailabilityService: null!, MarkupKind.PlainText, formattingContext, DisposalToken);

        Assert.Equal($"MyApp.Component{Environment.NewLine}{Environment.NewLine}Uses string.IsNullOrEmpty(string) and string.Length.", result?.Value);
    }

    [Fact]
    public async Task TryCreateTooltip_Markup_InvalidXml_ShownAsIs()
    {
        // Arrange
        var formattingContext = await CreateFormattingContextAsync();
        var description = new AggregateBoundElementDescription(
            [new BoundElementDescriptionInfo("MyApp.Component", "Fish & chips <")]);

        // Act
        var result = await MarkupTagHelperTooltipFactory.TryCreateTooltipAsync(
            documentFilePath: null,
            description,
            componentAvailabilityService: null!,
            MarkupKind.Markdown,
            formattingContext,
            DisposalToken);

        // Assert
        Assert.Equal($"MyApp.**Component**{Environment.NewLine}{Environment.NewLine}Fish & chips \\<", result?.Value);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85480")]
    public async Task TryCreateTooltip_Markup_UsesRoslynDocumentationFormatterWhenDescriptorTypeCannotBeResolved()
    {
        // Arrange
        const string typeName = "MyApp.MissingComponent";
        const string documentation = "<summary>Accepts <see cref=\"T:System.String\" />, <c>text</c>, and <em>markup</em>.</summary>";
        var formattingContext = await CreateFormattingContextAsync();
        var description = new AggregateBoundElementDescription(
            [new BoundElementDescriptionInfo(typeName, documentation)]);

        // Act
        var result = await MarkupTagHelperTooltipFactory.TryCreateTooltipAsync(
            documentFilePath: null,
            description,
            componentAvailabilityService: null!,
            MarkupKind.Markdown,
            formattingContext,
            DisposalToken);

        // Assert
        Assert.Equal($"MyApp.**MissingComponent**{Environment.NewLine}{Environment.NewLine}Accepts&nbsp;string,&nbsp;`text`, and&nbsp;_markup_\\.", result?.Value);
    }

    [Fact]
    public async Task TryCreateTooltip_Markup_NoAssociatedTagHelperDescriptions_ReturnsFalse()
    {
        // Arrange
        var componentAvailabilityService = new TestComponentAvailabilityService();
        var elementDescription = AggregateBoundElementDescription.Empty;

        // Act
        var markdown = await MarkupTagHelperTooltipFactory.TryCreateTooltipAsync("file.razor", elementDescription, componentAvailabilityService, MarkupKind.Markdown, await CreateFormattingContextAsync(), DisposalToken);

        // Assert
        Assert.Null(markdown);
    }

    [Fact]
    public async Task TryCreateTooltip_Markup_Element_SingleAssociatedTagHelper_ReturnsTrue()
    {
        // Arrange
        var componentAvailabilityService = new TestComponentAvailabilityService();

        var associatedTagHelperInfos = new[]
        {
            new BoundElementDescriptionInfo("Microsoft.AspNetCore.SomeTagHelper", "<summary>Uses <see cref=\"T:System.Collections.Generic.List`1\" />s</summary>"),
        };

        var elementDescription = new AggregateBoundElementDescription(associatedTagHelperInfos.ToImmutableArray());
        // Act
        var markdown = await MarkupTagHelperTooltipFactory.TryCreateTooltipAsync("file.razor", elementDescription, componentAvailabilityService, MarkupKind.Markdown, await CreateFormattingContextAsync(), DisposalToken);

        // Assert
        Assert.NotNull(markdown);
        Assert.Equal(@"Microsoft.AspNetCore.**SomeTagHelper**

Uses&nbsp;System\.Collections\.Generic\.List\<T\>s", markdown.Value);
        Assert.Equal(MarkupKind.Markdown, markdown.Kind);
    }

    [Fact]
    public async Task TryCreateTooltip_Markup_Element_PlainText_NoBold()
    {
        // Arrange
        var componentAvailabilityService = new TestComponentAvailabilityService();

        var associatedTagHelperInfos = new[]
        {
            new BoundElementDescriptionInfo("Microsoft.AspNetCore.SomeTagHelper", "<summary>Uses <see cref=\"T:System.Collections.Generic.List`1\" />s</summary>"),
        };

        var elementDescription = new AggregateBoundElementDescription(associatedTagHelperInfos.ToImmutableArray());

        // Act
        var markdown = await MarkupTagHelperTooltipFactory.TryCreateTooltipAsync("file.razor", elementDescription, componentAvailabilityService, MarkupKind.PlainText, await CreateFormattingContextAsync(), DisposalToken);

        // Assert
        Assert.NotNull(markdown);
        Assert.Equal(@"Microsoft.AspNetCore.SomeTagHelper

Uses System.Collections.Generic.List<T>s", markdown.Value);
        Assert.Equal(MarkupKind.PlainText, markdown.Kind);
    }

    [Fact]
    public async Task TryCreateTooltip_Markup_Attribute_PlainText_NoBold()
    {
        // Arrange
        var associatedAttributeDescriptions = new[]
        {
            new BoundAttributeDescriptionInfo(
                ReturnTypeName: "System.String",
                TypeName: "Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName",
                PropertyName: "SomeProperty",
                Documentation: "<summary>Uses <see cref=\"T:System.Collections.Generic.List`1\" />s</summary>")
        };

        var attributeDescription = new AggregateBoundAttributeDescription(associatedAttributeDescriptions.ToImmutableArray());

        // Act
        var result = MarkupTagHelperTooltipFactory.TryCreateTooltip(attributeDescription, MarkupKind.PlainText, await CreateFormattingContextAsync(), DisposalToken, out var markdown);

        // Assert
        Assert.True(result);
        Assert.NotNull(markdown);
        Assert.Equal(@"string SomeTypeName.SomeProperty

Uses System.Collections.Generic.List<T>s", markdown.Value);
        Assert.Equal(MarkupKind.PlainText, markdown.Kind);
    }

    [Fact]
    public async Task TryCreateTooltip_Markup_Element_MultipleAssociatedTagHelpers_ReturnsTrue()
    {
        // Arrange
        var componentAvailabilityService = new TestComponentAvailabilityService();

        var associatedTagHelperInfos = new[]
        {
            new BoundElementDescriptionInfo("Microsoft.AspNetCore.SomeTagHelper", "<summary>\nUses <see cref=\"T:System.Collections.Generic.List`1\" />s\n</summary>"),
            new BoundElementDescriptionInfo("Microsoft.AspNetCore.OtherTagHelper", "<summary>\nAlso uses <see cref=\"T:System.Collections.Generic.List`1\" />s\n\r\n\r\r</summary>"),
        };

        var elementDescription = new AggregateBoundElementDescription(associatedTagHelperInfos.ToImmutableArray());

        // Act
        var markdown = await MarkupTagHelperTooltipFactory.TryCreateTooltipAsync("file.razor", elementDescription, componentAvailabilityService, MarkupKind.Markdown, await CreateFormattingContextAsync(), DisposalToken);

        // Assert
        Assert.NotNull(markdown);
        Assert.Equal(@"Microsoft.AspNetCore.**SomeTagHelper**

Uses&nbsp;System\.Collections\.Generic\.List\<T\>s
---
Microsoft.AspNetCore.**OtherTagHelper**

Also uses&nbsp;System\.Collections\.Generic\.List\<T\>s", markdown.Value);
        Assert.Equal(MarkupKind.Markdown, markdown.Kind);
    }

    [Fact]
    public async Task TryCreateTooltip_Markup_Attribute_SingleAssociatedAttribute_ReturnsTrue()
    {
        // Arrange
        var associatedAttributeDescriptions = new[]
        {
            new BoundAttributeDescriptionInfo(
                ReturnTypeName: "System.String",
                TypeName: "Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName",
                PropertyName: "SomeProperty",
                Documentation: "<summary>Uses <see cref=\"T:System.Collections.Generic.List`1\" />s</summary>")
        };
        var attributeDescription = new AggregateBoundAttributeDescription(associatedAttributeDescriptions.ToImmutableArray());

        // Act
        var result = MarkupTagHelperTooltipFactory.TryCreateTooltip(attributeDescription, MarkupKind.Markdown, await CreateFormattingContextAsync(), DisposalToken, out var markdown);

        // Assert
        Assert.True(result);
        Assert.NotNull(markdown);
        Assert.Equal(@"**string** SomeTypeName.**SomeProperty**

Uses&nbsp;System\.Collections\.Generic\.List\<T\>s", markdown.Value);
        Assert.Equal(MarkupKind.Markdown, markdown.Kind);
    }

    [Fact]
    public async Task TryCreateTooltip_Markup_Attribute_MultipleAssociatedAttributes_ReturnsTrue()
    {
        // Arrange
        var associatedAttributeDescriptions = new[]
        {
            new BoundAttributeDescriptionInfo(
                ReturnTypeName: "System.String",
                TypeName: "Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName",
                PropertyName: "SomeProperty",
                Documentation: "<summary>Uses <see cref=\"T:System.Collections.Generic.List`1\" />s</summary>"),
            new BoundAttributeDescriptionInfo(
                PropertyName: "AnotherProperty",
                TypeName: "Microsoft.AspNetCore.SomeTagHelpers.AnotherTypeName",
                ReturnTypeName: "System.Boolean?",
                Documentation: "<summary>\nUses <see cref=\"T:System.Collections.Generic.List`1\" />s\n</summary>"),
        };
        var attributeDescription = new AggregateBoundAttributeDescription(associatedAttributeDescriptions.ToImmutableArray());

        // Act
        var result = MarkupTagHelperTooltipFactory.TryCreateTooltip(attributeDescription, MarkupKind.Markdown, await CreateFormattingContextAsync(), DisposalToken, out var markdown);

        // Assert
        Assert.True(result);
        Assert.NotNull(markdown);
        Assert.Equal(@"**string** SomeTypeName.**SomeProperty**

Uses&nbsp;System\.Collections\.Generic\.List\<T\>s
---
**Boolean?** AnotherTypeName.**AnotherProperty**

Uses&nbsp;System\.Collections\.Generic\.List\<T\>s", markdown.Value);
        Assert.Equal(MarkupKind.Markdown, markdown.Kind);
    }
}
