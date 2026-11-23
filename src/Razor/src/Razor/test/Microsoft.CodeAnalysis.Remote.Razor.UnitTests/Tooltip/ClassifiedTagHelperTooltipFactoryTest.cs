// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Razor.Tooltip;
using Roslyn.Text.Adornments;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Remote.Razor.Tooltip;

public class ClassifiedTagHelperTooltipFactoryTest(ITestOutputHelper testOutput) : DocCommentFormattingTestBase(testOutput)
{
    [Fact]
    public async Task TryCreateTooltip_ClassifiedTextElement_NoAssociatedTagHelperDescriptions_ReturnsFalse()
    {
        // Arrange
        var componentAvailabilityService = new TestComponentAvailabilityService();
        var elementDescription = AggregateBoundElementDescription.Empty;

        // Act
        var classifiedTextElement = await ClassifiedTagHelperTooltipFactory.TryCreateTooltipAsync("file.razor", elementDescription, componentAvailabilityService, await CreateFormattingContextAsync(), DisposalToken);

        // Assert
        Assert.Null(classifiedTextElement);
    }

    [Fact]
    public async Task TryCreateTooltip_ClassifiedTextElement_Element_SingleAssociatedTagHelper_ReturnsTrue()
    {
        // Arrange
        var componentAvailabilityService = new TestComponentAvailabilityService();

        var associatedTagHelperInfos = new[]
        {
            new BoundElementDescriptionInfo(
                "Microsoft.AspNetCore.SomeTagHelper",
                "<summary>Uses <see cref=\"T:System.Collections.Generic.List`1\" />s</summary>"),
        };

        var elementDescription = new AggregateBoundElementDescription(associatedTagHelperInfos.ToImmutableArray());

        // Act
        var classifiedTextElement = await ClassifiedTagHelperTooltipFactory.TryCreateTooltipAsync("file.razor", elementDescription, componentAvailabilityService, await CreateFormattingContextAsync(), DisposalToken);

        // Assert
        Assert.NotNull(classifiedTextElement);

        // Expected output:
        //     Microsoft.AspNetCore.SomeTagHelper
        //     Uses System.Collections.Generic.List<T>s
        Assert.Collection(classifiedTextElement.Runs,
            run => run.AssertExpectedClassification("Microsoft", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("AspNetCore", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("SomeTagHelper", ClassifiedTagHelperTooltipFactory.TypeClassificationName),
            run => run.AssertExpectedClassification(Environment.NewLine, ClassificationTypeNames.WhiteSpace),
            run => run.AssertExpectedClassification("Uses", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(" ", ClassificationTypeNames.WhiteSpace),
            run => run.AssertExpectedClassification("System", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("Collections", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("Generic", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("List", ClassificationTypeNames.ClassName),
            run => run.AssertExpectedClassification("<", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("T", ClassificationTypeNames.TypeParameterName),
            run => run.AssertExpectedClassification(">", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("s", ClassificationTypeNames.Text));
    }

    [Fact]
    public async Task TryCreateTooltip_ClassifiedTextElement_Element_NamespaceContainsTypeName_ReturnsTrue()
    {
        // Arrange
        var componentAvailabilityService = new TestComponentAvailabilityService();

        var associatedTagHelperInfos = new[]
        {
            new BoundElementDescriptionInfo(
                "Microsoft.AspNetCore.SomeTagHelper.SomeTagHelper",
                "<summary>Uses <see cref=\"T:System.Collections.Generic.List`1\" />s</summary>"),
        };

        var elementDescription = new AggregateBoundElementDescription(associatedTagHelperInfos.ToImmutableArray());

        // Act
        var classifiedTextElement = await ClassifiedTagHelperTooltipFactory.TryCreateTooltipAsync("file.razor", elementDescription, componentAvailabilityService, await CreateFormattingContextAsync(), DisposalToken);

        // Assert
        Assert.NotNull(classifiedTextElement);

        // Expected output:
        //     Microsoft.AspNetCore.SomeTagHelper.SomeTagHelper
        //     Uses System.Collections.Generic.List<T>s
        Assert.Collection(classifiedTextElement.Runs,
            run => run.AssertExpectedClassification("Microsoft", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("AspNetCore", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("SomeTagHelper", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("SomeTagHelper", ClassifiedTagHelperTooltipFactory.TypeClassificationName),
            run => run.AssertExpectedClassification(Environment.NewLine, ClassificationTypeNames.WhiteSpace),
            run => run.AssertExpectedClassification("Uses", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(" ", ClassificationTypeNames.WhiteSpace),
            run => run.AssertExpectedClassification("System", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("Collections", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("Generic", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("List", ClassificationTypeNames.ClassName),
            run => run.AssertExpectedClassification("<", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("T", ClassificationTypeNames.TypeParameterName),
            run => run.AssertExpectedClassification(">", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("s", ClassificationTypeNames.Text));
    }

    [Fact]
    public async Task TryCreateTooltip_ClassifiedTextElement_Element_MultipleAssociatedTagHelpers_ReturnsTrue()
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
        var classifiedTextElement = await ClassifiedTagHelperTooltipFactory.TryCreateTooltipAsync("file.razor", elementDescription, componentAvailabilityService, await CreateFormattingContextAsync(), DisposalToken);

        // Assert
        Assert.NotNull(classifiedTextElement);

        // Expected output:
        //     Microsoft.AspNetCore.SomeTagHelper
        //     Uses System.Collections.Generic.List<T>s
        //
        //     Microsoft.AspNetCore.OtherTagHelper
        //     Also uses System.Collections.Generic.List<T>s
        Assert.Collection(classifiedTextElement.Runs,
            run => run.AssertExpectedClassification("Microsoft", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("AspNetCore", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("SomeTagHelper", ClassifiedTagHelperTooltipFactory.TypeClassificationName),
            run => run.AssertExpectedClassification(Environment.NewLine, ClassificationTypeNames.WhiteSpace),
            run => run.AssertExpectedClassification("Uses", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(" ", ClassificationTypeNames.WhiteSpace),
            run => run.AssertExpectedClassification("System", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("Collections", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("Generic", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("List", ClassificationTypeNames.ClassName),
            run => run.AssertExpectedClassification("<", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("T", ClassificationTypeNames.TypeParameterName),
            run => run.AssertExpectedClassification(">", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("s", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(Environment.NewLine, ClassificationTypeNames.WhiteSpace),
            run => run.AssertExpectedClassification(Environment.NewLine, ClassificationTypeNames.WhiteSpace),
            run => run.AssertExpectedClassification("Microsoft", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("AspNetCore", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("OtherTagHelper", ClassifiedTagHelperTooltipFactory.TypeClassificationName),
            run => run.AssertExpectedClassification(Environment.NewLine, ClassificationTypeNames.WhiteSpace),
            run => run.AssertExpectedClassification("Also uses", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(" ", ClassificationTypeNames.WhiteSpace),
            run => run.AssertExpectedClassification("System", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("Collections", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("Generic", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("List", ClassificationTypeNames.ClassName),
            run => run.AssertExpectedClassification("<", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("T", ClassificationTypeNames.TypeParameterName),
            run => run.AssertExpectedClassification(">", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("s", ClassificationTypeNames.Text));
    }

    [Fact]
    public async Task TryCreateTooltip_ClassifiedTextElement_NoAssociatedAttributeDescriptions_ReturnsFalse()
    {
        // Arrange
        var elementDescription = AggregateBoundAttributeDescription.Empty;

        // Act
        Assert.False(ClassifiedTagHelperTooltipFactory.TryCreateTooltip(elementDescription, await CreateFormattingContextAsync(), DisposalToken, out ClassifiedTextElement? _));
    }

    [Fact]
    public async Task TryCreateTooltip_ClassifiedTextElement_Attribute_SingleAssociatedAttribute_ReturnsTrue()
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
        Assert.True(ClassifiedTagHelperTooltipFactory.TryCreateTooltip(attributeDescription, await CreateFormattingContextAsync(), DisposalToken, out ClassifiedTextElement? classifiedTextElement));

        // Assert
        Assert.NotNull(classifiedTextElement);

        // Expected output:
        //     string Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName.SomeProperty
        //     Uses System.Collections.Generic.List<T>s
        Assert.Collection(classifiedTextElement.Runs,
            run => run.AssertExpectedClassification("string", ClassificationTypeNames.Keyword),
            run => run.AssertExpectedClassification(" ", ClassificationTypeNames.WhiteSpace),
            run => run.AssertExpectedClassification("Microsoft", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("AspNetCore", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("SomeTagHelpers", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("SomeTypeName", ClassifiedTagHelperTooltipFactory.TypeClassificationName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("SomeProperty", ClassificationTypeNames.Identifier),
            run => run.AssertExpectedClassification(Environment.NewLine, ClassificationTypeNames.WhiteSpace),
            run => run.AssertExpectedClassification("Uses", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(" ", ClassificationTypeNames.WhiteSpace),
            run => run.AssertExpectedClassification("System", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("Collections", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("Generic", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("List", ClassificationTypeNames.ClassName),
            run => run.AssertExpectedClassification("<", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("T", ClassificationTypeNames.TypeParameterName),
            run => run.AssertExpectedClassification(">", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("s", ClassificationTypeNames.Text));
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85487")]
    public async Task TryCreateTooltip_ClassifiedTextElement_UsesRoslynDocumentationFormatterWhenContextIsAvailable()
    {
        // Arrange
        var formattingContext = await CreateFormattingContextAsync();
        var attributeDescription = new AggregateBoundAttributeDescription(
        [
            new BoundAttributeDescriptionInfo(
                ReturnTypeName: "System.String",
                TypeName: "MyApp.Component",
                PropertyName: "Value",
                Documentation: "<summary>Accepts <see cref=\"T:System.String\" />.</summary>")
        ]);

        // Act
        Assert.True(ClassifiedTagHelperTooltipFactory.TryCreateTooltip(
            attributeDescription, formattingContext, DisposalToken, out ClassifiedTextElement? classifiedTextElement));

        // Assert
        Assert.NotNull(classifiedTextElement);
        Assert.Contains(classifiedTextElement.Runs, run =>
            run.Text == "string" && run.ClassificationTypeName == ClassificationTypeNames.Keyword);
    }

    [Fact]
    public async Task TryCreateTooltip_ClassifiedTextElement_Attribute_MultipleAssociatedAttributes_ReturnsTrue()
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
        Assert.True(ClassifiedTagHelperTooltipFactory.TryCreateTooltip(attributeDescription, await CreateFormattingContextAsync(), DisposalToken, out ClassifiedTextElement? classifiedTextElement));

        // Assert
        Assert.NotNull(classifiedTextElement);

        // Expected output:
        //     string Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName.SomeProperty
        //     Uses System.Collections.Generic.List<T>s
        //
        //     bool? Microsoft.AspNetCore.SomeTagHelpers.AnotherTypeName.AnotherProperty
        //     Uses System.Collections.Generic.List<T>s
        Assert.Collection(classifiedTextElement.Runs,
            run => run.AssertExpectedClassification("string", ClassificationTypeNames.Keyword),
            run => run.AssertExpectedClassification(" ", ClassificationTypeNames.WhiteSpace),
            run => run.AssertExpectedClassification("Microsoft", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("AspNetCore", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("SomeTagHelpers", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("SomeTypeName", ClassifiedTagHelperTooltipFactory.TypeClassificationName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("SomeProperty", ClassificationTypeNames.Identifier),
            run => run.AssertExpectedClassification(Environment.NewLine, ClassificationTypeNames.WhiteSpace),
            run => run.AssertExpectedClassification("Uses", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(" ", ClassificationTypeNames.WhiteSpace),
            run => run.AssertExpectedClassification("System", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("Collections", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("Generic", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("List", ClassificationTypeNames.ClassName),
            run => run.AssertExpectedClassification("<", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("T", ClassificationTypeNames.TypeParameterName),
            run => run.AssertExpectedClassification(">", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("s", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(Environment.NewLine, ClassificationTypeNames.WhiteSpace),
            run => run.AssertExpectedClassification(Environment.NewLine, ClassificationTypeNames.WhiteSpace),
            run => run.AssertExpectedClassification("bool", ClassificationTypeNames.Keyword),
            run => run.AssertExpectedClassification("?", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification(" ", ClassificationTypeNames.WhiteSpace),
            run => run.AssertExpectedClassification("Microsoft", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("AspNetCore", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("SomeTagHelpers", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("AnotherTypeName", ClassifiedTagHelperTooltipFactory.TypeClassificationName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("AnotherProperty", ClassificationTypeNames.Identifier),
            run => run.AssertExpectedClassification(Environment.NewLine, ClassificationTypeNames.WhiteSpace),
            run => run.AssertExpectedClassification("Uses", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(" ", ClassificationTypeNames.WhiteSpace),
            run => run.AssertExpectedClassification("System", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("Collections", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("Generic", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("List", ClassificationTypeNames.ClassName),
            run => run.AssertExpectedClassification("<", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("T", ClassificationTypeNames.TypeParameterName),
            run => run.AssertExpectedClassification(">", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("s", ClassificationTypeNames.Text));
    }

    [Fact]
    public async Task TryCreateTooltip_ContainerElement_NoAssociatedTagHelperDescriptions_ReturnsFalse()
    {
        // Arrange
        var componentAvailabilityService = new TestComponentAvailabilityService();

        var elementDescription = AggregateBoundElementDescription.Empty;

        // Act
        var containerElement = await ClassifiedTagHelperTooltipFactory.TryCreateTooltipContainerAsync("file.razor", elementDescription, componentAvailabilityService, await CreateFormattingContextAsync(), DisposalToken);

        // Assert
        Assert.Null(containerElement);
    }

    [Fact]
    public async Task TryCreateTooltip_ContainerElement_Attribute_MultipleAssociatedTagHelpers_ReturnsTrue()
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
        var container = await ClassifiedTagHelperTooltipFactory.TryCreateTooltipContainerAsync("file.razor", elementDescription, componentAvailabilityService, await CreateFormattingContextAsync(), DisposalToken);

        // Assert
        Assert.NotNull(container);
        var containerElements = container.Elements.ToList();

        // Expected output:
        //     [Class Glyph] Microsoft.AspNetCore.SomeTagHelper
        //     Uses System.Collections.Generic.List<T>s
        //
        //     [Class Glyph] Microsoft.AspNetCore.OtherTagHelper
        //     Also uses System.Collections.Generic.List<T>s
        Assert.Equal(ContainerElementStyle.Stacked, container.Style);
        Assert.Equal(5, containerElements.Count);

        // [Class Glyph] Microsoft.AspNetCore.SomeTagHelper
        var innerContainer = ((ContainerElement)containerElements[0]).Elements.ToList();
        var classifiedTextElement = (ClassifiedTextElement)innerContainer[1];
        Assert.Equal(2, innerContainer.Count);
        Assert.Equal(ClassifiedTagHelperTooltipFactory.ClassGlyph, innerContainer[0]);
        Assert.Collection(classifiedTextElement.Runs,
            run => run.AssertExpectedClassification("Microsoft", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("AspNetCore", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("SomeTagHelper", ClassifiedTagHelperTooltipFactory.TypeClassificationName));

        // Uses System.Collections.Generic.List<T>s
        innerContainer = ((ContainerElement)containerElements[1]).Elements.ToList();
        classifiedTextElement = (ClassifiedTextElement)innerContainer[0];
        Assert.Single(innerContainer);
        Assert.Collection(classifiedTextElement.Runs,
            run => run.AssertExpectedClassification("Uses", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(" ", ClassificationTypeNames.WhiteSpace),
            run => run.AssertExpectedClassification("System", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("Collections", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("Generic", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("List", ClassificationTypeNames.ClassName),
            run => run.AssertExpectedClassification("<", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("T", ClassificationTypeNames.TypeParameterName),
            run => run.AssertExpectedClassification(">", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("s", ClassificationTypeNames.Text));

        // new line
        innerContainer = ((ContainerElement)containerElements[2]).Elements.ToList();
        classifiedTextElement = (ClassifiedTextElement)innerContainer[0];
        Assert.Single(innerContainer);
        Assert.Empty(classifiedTextElement.Runs);

        // [Class Glyph] Microsoft.AspNetCore.OtherTagHelper
        innerContainer = ((ContainerElement)containerElements[3]).Elements.ToList();
        classifiedTextElement = (ClassifiedTextElement)innerContainer[1];
        Assert.Equal(2, innerContainer.Count);
        Assert.Equal(ClassifiedTagHelperTooltipFactory.ClassGlyph, innerContainer[0]);
        Assert.Collection(classifiedTextElement.Runs,
            run => run.AssertExpectedClassification("Microsoft", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("AspNetCore", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("OtherTagHelper", ClassifiedTagHelperTooltipFactory.TypeClassificationName));

        // Also uses System.Collections.Generic.List<T>s
        innerContainer = ((ContainerElement)containerElements[4]).Elements.ToList();
        classifiedTextElement = (ClassifiedTextElement)innerContainer[0];
        Assert.Single(innerContainer);
        Assert.Collection(classifiedTextElement.Runs,
            run => run.AssertExpectedClassification("Also uses", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(" ", ClassificationTypeNames.WhiteSpace),
            run => run.AssertExpectedClassification("System", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("Collections", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("Generic", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("List", ClassificationTypeNames.ClassName),
            run => run.AssertExpectedClassification("<", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("T", ClassificationTypeNames.TypeParameterName),
            run => run.AssertExpectedClassification(">", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("s", ClassificationTypeNames.Text));
    }

    [Fact]
    public async Task TryCreateTooltip_ContainerElement_NoAssociatedAttributeDescriptions_ReturnsFalse()
    {
        // Arrange
        var elementDescription = AggregateBoundAttributeDescription.Empty;

        // Act
        Assert.False(ClassifiedTagHelperTooltipFactory.TryCreateTooltip(elementDescription, await CreateFormattingContextAsync(), DisposalToken, out ContainerElement? _));
    }

    [Fact]
    public async Task TryCreateTooltip_ContainerElement_Attribute_MultipleAssociatedAttributes_ReturnsTrue()
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
        Assert.True(ClassifiedTagHelperTooltipFactory.TryCreateTooltip(attributeDescription, await CreateFormattingContextAsync(), DisposalToken, out ContainerElement? container));

        // Assert
        Assert.NotNull(container);

        var containerElements = container.Elements.ToList();

        // Expected output:
        //     [Property Glyph] string Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName.SomeProperty
        //     Uses System.Collections.Generic.List<T>s
        //
        //     [Property Glyph] bool? Microsoft.AspNetCore.SomeTagHelpers.AnotherTypeName.AnotherProperty
        //     Uses System.Collections.Generic.List<T>s
        Assert.Equal(ContainerElementStyle.Stacked, container.Style);
        Assert.Equal(5, containerElements.Count);

        // [TagHelper Glyph] string Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName.SomeProperty
        var innerContainer = ((ContainerElement)containerElements[0]).Elements.ToList();
        var classifiedTextElement = (ClassifiedTextElement)innerContainer[1];
        Assert.Equal(2, innerContainer.Count);
        Assert.Equal(ClassifiedTagHelperTooltipFactory.PropertyGlyph, innerContainer[0]);
        Assert.Collection(classifiedTextElement.Runs,
            run => run.AssertExpectedClassification("string", ClassificationTypeNames.Keyword),
            run => run.AssertExpectedClassification(" ", ClassificationTypeNames.WhiteSpace),
            run => run.AssertExpectedClassification("Microsoft", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("AspNetCore", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("SomeTagHelpers", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("SomeTypeName", ClassifiedTagHelperTooltipFactory.TypeClassificationName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("SomeProperty", ClassificationTypeNames.Identifier));

        // Uses System.Collections.Generic.List<T>s
        innerContainer = ((ContainerElement)containerElements[1]).Elements.ToList();
        classifiedTextElement = (ClassifiedTextElement)innerContainer[0];
        Assert.Single(innerContainer);
        Assert.Collection(classifiedTextElement.Runs,
            run => run.AssertExpectedClassification("Uses", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(" ", ClassificationTypeNames.WhiteSpace),
            run => run.AssertExpectedClassification("System", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("Collections", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("Generic", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("List", ClassificationTypeNames.ClassName),
            run => run.AssertExpectedClassification("<", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("T", ClassificationTypeNames.TypeParameterName),
            run => run.AssertExpectedClassification(">", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("s", ClassificationTypeNames.Text));

        // new line
        innerContainer = ((ContainerElement)containerElements[2]).Elements.ToList();
        classifiedTextElement = (ClassifiedTextElement)innerContainer[0];
        Assert.Single(innerContainer);
        Assert.Empty(classifiedTextElement.Runs);

        // [TagHelper Glyph] bool? Microsoft.AspNetCore.SomeTagHelpers.AnotherTypeName.AnotherProperty
        innerContainer = ((ContainerElement)containerElements[3]).Elements.ToList();
        classifiedTextElement = (ClassifiedTextElement)innerContainer[1];
        Assert.Equal(2, innerContainer.Count);
        Assert.Equal(ClassifiedTagHelperTooltipFactory.PropertyGlyph, innerContainer[0]);
        Assert.Collection(classifiedTextElement.Runs,
            run => run.AssertExpectedClassification("bool", ClassificationTypeNames.Keyword),
            run => run.AssertExpectedClassification("?", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification(" ", ClassificationTypeNames.WhiteSpace),
            run => run.AssertExpectedClassification("Microsoft", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("AspNetCore", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("SomeTagHelpers", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("AnotherTypeName", ClassifiedTagHelperTooltipFactory.TypeClassificationName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("AnotherProperty", ClassificationTypeNames.Identifier));

        // Uses System.Collections.Generic.List<T>s
        innerContainer = ((ContainerElement)containerElements[4]).Elements.ToList();
        classifiedTextElement = (ClassifiedTextElement)innerContainer[0];
        Assert.Single(innerContainer);
        Assert.Collection(classifiedTextElement.Runs,
            run => run.AssertExpectedClassification("Uses", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(" ", ClassificationTypeNames.WhiteSpace),
            run => run.AssertExpectedClassification("System", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("Collections", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("Generic", ClassificationTypeNames.NamespaceName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("List", ClassificationTypeNames.ClassName),
            run => run.AssertExpectedClassification("<", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("T", ClassificationTypeNames.TypeParameterName),
            run => run.AssertExpectedClassification(">", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("s", ClassificationTypeNames.Text));
    }
}
