// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Tooltip;

public class MarkupTagHelperTooltipFactoryTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [Fact]
    public void CleanSummaryContent_Markup_ReplacesSeeCrefs()
    {
        // Arrange
        var summary = "Accepts <see cref=\"T:System.Collections.List{System.String}\" />s";

        // Act
        var cleanedSummary = MarkupTagHelperTooltipFactory.CleanSummaryContent(summary);

        // Assert
        Assert.Equal("Accepts `List<System.String>`s", cleanedSummary);
    }

    [Fact]
    public void CleanSummaryContent_Markup_ReplacesSeeAlsoCrefs()
    {
        // Arrange
        var summary = "Accepts <seealso cref=\"T:System.Collections.List{System.String}\" />s";

        // Act
        var cleanedSummary = MarkupTagHelperTooltipFactory.CleanSummaryContent(summary);

        // Assert
        Assert.Equal("Accepts `List<System.String>`s", cleanedSummary);
    }

    [Fact]
    public void CleanSummaryContent_Markup_TrimsSurroundingWhitespace()
    {
        // Arrange
        var summary = @"
            Hello

    World

";

        // Act
        var cleanedSummary = MarkupTagHelperTooltipFactory.CleanSummaryContent(summary);

        // Assert
        Assert.Equal(@"Hello

World", cleanedSummary);
    }

    [Fact]
    public async Task TryCreateTooltip_Markup_NoAssociatedTagHelperDescriptions_ReturnsFalse()
    {
        // Arrange
        var componentAvailabilityService = new TestComponentAvailabilityService();
        var elementDescription = AggregateBoundElementDescription.Empty;

        // Act
        var markdown = await MarkupTagHelperTooltipFactory.TryCreateTooltipAsync("file.razor", elementDescription, componentAvailabilityService, MarkupKind.Markdown, DisposalToken);

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
            new BoundElementDescriptionInfo("Microsoft.AspNetCore.SomeTagHelper", "<summary>Uses <see cref=\"T:System.Collections.List{System.String}\" />s</summary>"),
        };

        var elementDescription = new AggregateBoundElementDescription(associatedTagHelperInfos.ToImmutableArray());
        // Act
        var markdown = await MarkupTagHelperTooltipFactory.TryCreateTooltipAsync("file.razor", elementDescription, componentAvailabilityService, MarkupKind.Markdown, DisposalToken);

        // Assert
        Assert.NotNull(markdown);
        Assert.Equal(@"Microsoft.AspNetCore.**SomeTagHelper**

Uses `List<System.String>`s", markdown.Value);
        Assert.Equal(MarkupKind.Markdown, markdown.Kind);
    }

    [Fact]
    public async Task TryCreateTooltip_Markup_Element_PlainText_NoBold()
    {
        // Arrange
        var componentAvailabilityService = new TestComponentAvailabilityService();

        var associatedTagHelperInfos = new[]
        {
            new BoundElementDescriptionInfo("Microsoft.AspNetCore.SomeTagHelper", "<summary>Uses <see cref=\"T:System.Collections.List{System.String}\" />s</summary>"),
        };

        var elementDescription = new AggregateBoundElementDescription(associatedTagHelperInfos.ToImmutableArray());

        // Act
        var markdown = await MarkupTagHelperTooltipFactory.TryCreateTooltipAsync("file.razor", elementDescription, componentAvailabilityService, MarkupKind.PlainText, DisposalToken);

        // Assert
        Assert.NotNull(markdown);
        Assert.Equal(@"Microsoft.AspNetCore.SomeTagHelper

Uses `List<System.String>`s", markdown.Value);
        Assert.Equal(MarkupKind.PlainText, markdown.Kind);
    }

    [Fact]
    public void TryCreateTooltip_Markup_Attribute_PlainText_NoBold()
    {
        // Arrange
        var associatedAttributeDescriptions = new[]
        {
            new BoundAttributeDescriptionInfo(
                ReturnTypeName: "System.String",
                TypeName: "Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName",
                PropertyName: "SomeProperty",
                Documentation: "<summary>Uses <see cref=\"T:System.Collections.List{System.String}\" />s</summary>")
        };

        var attributeDescription = new AggregateBoundAttributeDescription(associatedAttributeDescriptions.ToImmutableArray());

        // Act
        var result = MarkupTagHelperTooltipFactory.TryCreateTooltip(attributeDescription, MarkupKind.PlainText, out var markdown);

        // Assert
        Assert.True(result);
        Assert.Equal(@"string SomeTypeName.SomeProperty

Uses `List<System.String>`s", markdown.Value);
        Assert.Equal(MarkupKind.PlainText, markdown.Kind);
    }

    [Fact]
    public async Task TryCreateTooltip_Markup_Element_MultipleAssociatedTagHelpers_ReturnsTrue()
    {
        // Arrange
        var componentAvailabilityService = new TestComponentAvailabilityService();

        var associatedTagHelperInfos = new[]
        {
            new BoundElementDescriptionInfo("Microsoft.AspNetCore.SomeTagHelper", "<summary>\nUses <see cref=\"T:System.Collections.List{System.String}\" />s\n</summary>"),
            new BoundElementDescriptionInfo("Microsoft.AspNetCore.OtherTagHelper", "<summary>\nAlso uses <see cref=\"T:System.Collections.List{System.String}\" />s\n\r\n\r\r</summary>"),
        };

        var elementDescription = new AggregateBoundElementDescription(associatedTagHelperInfos.ToImmutableArray());

        // Act
        var markdown = await MarkupTagHelperTooltipFactory.TryCreateTooltipAsync("file.razor", elementDescription, componentAvailabilityService, MarkupKind.Markdown, DisposalToken);

        // Assert
        Assert.NotNull(markdown);
        Assert.Equal(@"Microsoft.AspNetCore.**SomeTagHelper**

Uses `List<System.String>`s
---
Microsoft.AspNetCore.**OtherTagHelper**

Also uses `List<System.String>`s", markdown.Value);
        Assert.Equal(MarkupKind.Markdown, markdown.Kind);
    }

    [Fact]
    public void TryCreateTooltip_Markup_Attribute_SingleAssociatedAttribute_ReturnsTrue()
    {
        // Arrange
        var associatedAttributeDescriptions = new[]
        {
            new BoundAttributeDescriptionInfo(
                ReturnTypeName: "System.String",
                TypeName: "Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName",
                PropertyName: "SomeProperty",
                Documentation: "<summary>Uses <see cref=\"T:System.Collections.List{System.String}\" />s</summary>")
        };
        var attributeDescription = new AggregateBoundAttributeDescription(associatedAttributeDescriptions.ToImmutableArray());

        // Act
        var result = MarkupTagHelperTooltipFactory.TryCreateTooltip(attributeDescription, MarkupKind.Markdown, out var markdown);

        // Assert
        Assert.True(result);
        Assert.Equal(@"**string** SomeTypeName.**SomeProperty**

Uses `List<System.String>`s", markdown.Value);
        Assert.Equal(MarkupKind.Markdown, markdown.Kind);
    }

    [Fact]
    public void TryCreateTooltip_Markup_Attribute_MultipleAssociatedAttributes_ReturnsTrue()
    {
        // Arrange
        var associatedAttributeDescriptions = new[]
        {
            new BoundAttributeDescriptionInfo(
                ReturnTypeName: "System.String",
                TypeName: "Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName",
                PropertyName: "SomeProperty",
                Documentation: "<summary>Uses <see cref=\"T:System.Collections.List{System.String}\" />s</summary>"),
            new BoundAttributeDescriptionInfo(
                PropertyName: "AnotherProperty",
                TypeName: "Microsoft.AspNetCore.SomeTagHelpers.AnotherTypeName",
                ReturnTypeName: "System.Boolean?",
                Documentation: "<summary>\nUses <see cref=\"T:System.Collections.List{System.String}\" />s\n</summary>"),
        };
        var attributeDescription = new AggregateBoundAttributeDescription(associatedAttributeDescriptions.ToImmutableArray());

        // Act
        var result = MarkupTagHelperTooltipFactory.TryCreateTooltip(attributeDescription, MarkupKind.Markdown, out var markdown);

        // Assert
        Assert.True(result);
        Assert.Equal(@"**string** SomeTypeName.**SomeProperty**

Uses `List<System.String>`s
---
**Boolean?** AnotherTypeName.**AnotherProperty**

Uses `List<System.String>`s", markdown.Value);
        Assert.Equal(MarkupKind.Markdown, markdown.Kind);
    }
}
