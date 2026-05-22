// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Classification;
using Roslyn.Text.Adornments;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Tooltip;

public class ClassifiedTagHelperTooltipFactoryTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [Fact]
    public void CleanAndClassifySummaryContent_ClassifiedTextElement_ReplacesSeeCrefs()
    {
        // Arrange
        var runs = new List<ClassifiedTextRun>();
        var summary = "Accepts <see cref=\"T:System.Collections.List{System.String}\" />s";

        // Act
        ClassifiedTagHelperTooltipFactory.CleanAndClassifySummaryContent(runs, summary);

        // Assert

        // Expected output:
        //     Accepts List<string>s
        Assert.Collection(runs,
            run => run.AssertExpectedClassification("Accepts ", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification("List", ClassifiedTagHelperTooltipFactory.TypeClassificationName),
            run => run.AssertExpectedClassification("<", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("string", ClassificationTypeNames.Keyword),
            run => run.AssertExpectedClassification(">", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("s", ClassificationTypeNames.Text));
    }

    [Fact]
    public void CleanSummaryContent_ClassifiedTextElement_ReplacesSeeAlsoCrefs()
    {
        // Arrange
        var runs = new List<ClassifiedTextRun>();
        var summary = "Accepts <seealso cref=\"T:System.Collections.List{System.String}\" />s";

        // Act
        ClassifiedTagHelperTooltipFactory.CleanAndClassifySummaryContent(runs, summary);

        // Assert

        // Expected output:
        //     Accepts List<string>s
        Assert.Collection(runs,
            run => run.AssertExpectedClassification("Accepts ", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification("List", ClassifiedTagHelperTooltipFactory.TypeClassificationName),
            run => run.AssertExpectedClassification("<", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("string", ClassificationTypeNames.Keyword),
            run => run.AssertExpectedClassification(">", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("s", ClassificationTypeNames.Text));
    }

    [Fact]
    public void CleanSummaryContent_ClassifiedTextElement_TrimsSurroundingWhitespace()
    {
        // Arrange
        var runs = new List<ClassifiedTextRun>();
        var summary = @"
            Hello

    World

";

        // Act
        ClassifiedTagHelperTooltipFactory.CleanAndClassifySummaryContent(runs, summary);

        // Assert

        // Expected output:
        //     Hello
        //
        //     World
        Assert.Collection(runs, run => run.AssertExpectedClassification(
            """
            Hello

            World
            """, ClassificationTypeNames.Text));
    }

    [Fact]
    public void CleanSummaryContent_ClassifiedTextElement_ClassifiesCodeBlocks()
    {
        // Arrange
        var runs = new List<ClassifiedTextRun>();
        var summary = @"code: <code>This is code</code> and <code>This is some other code</code>.";

        // Act
        ClassifiedTagHelperTooltipFactory.CleanAndClassifySummaryContent(runs, summary);

        // Assert

        // Expected output:
        //     code: This is code and This is some other code.
        Assert.Collection(runs,
            run => run.AssertExpectedClassification("code: ", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification("This is code", ClassificationTypeNames.Text, ClassifiedTextRunStyle.UseClassificationFont),
            run => run.AssertExpectedClassification(" and ", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification("This is some other code", ClassificationTypeNames.Text, ClassifiedTextRunStyle.UseClassificationFont),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Text));
    }

    [Fact]
    public void CleanSummaryContent_ClassifiedTextElement_ClassifiesCBlocks()
    {
        // Arrange
        var runs = new List<ClassifiedTextRun>();
        var summary = @"code: <c>This is code</c> and <c>This is some other code</c>.";

        // Act
        ClassifiedTagHelperTooltipFactory.CleanAndClassifySummaryContent(runs, summary);

        // Assert

        // Expected output:
        //     code: This is code and This is some other code.
        Assert.Collection(runs,
            run => run.AssertExpectedClassification("code: ", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification("This is code", ClassificationTypeNames.Text, ClassifiedTextRunStyle.UseClassificationFont),
            run => run.AssertExpectedClassification(" and ", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification("This is some other code", ClassificationTypeNames.Text, ClassifiedTextRunStyle.UseClassificationFont),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Text));
    }

    [Fact]
    public void CleanSummaryContent_ClassifiedTextElement_ParasCreateNewLines()
    {
        // Arrange
        var runs = new List<ClassifiedTextRun>();
        var summary = @"Summary description:
<para>Paragraph text.</para>
End summary description.";

        // Act
        ClassifiedTagHelperTooltipFactory.CleanAndClassifySummaryContent(runs, summary);

        // Assert

        // Expected output:
        //     code: This is code and This is some other code.
        Assert.Collection(runs, run => run.AssertExpectedClassification(
            """
            Summary description:

            Paragraph text.

            End summary description.
            """,
            ClassificationTypeNames.Text));
    }

    [Fact]
    public async Task TryCreateTooltip_ClassifiedTextElement_NoAssociatedTagHelperDescriptions_ReturnsFalse()
    {
        // Arrange
        var componentAvailabilityService = new TestComponentAvailabilityService();
        var elementDescription = AggregateBoundElementDescription.Empty;

        // Act
        var classifiedTextElement = await ClassifiedTagHelperTooltipFactory.TryCreateTooltipAsync("file.razor", elementDescription, componentAvailabilityService, DisposalToken);

        // Assert
        Assert.Null(classifiedTextElement);
    }

    [Fact]
    public async Task TryCreateTooltip_ClassifiedTextElement_Element_SingleAssociatedTagHelper_ReturnsTrue_NestedTypes()
    {
        // Arrange
        var componentAvailabilityService = new TestComponentAvailabilityService();

        var associatedTagHelperInfos = new[]
        {
            new BoundElementDescriptionInfo(
                "Microsoft.AspNetCore.SomeTagHelper",
                "<summary>Uses <see cref=\"T:System.Collections.List{System.Collections.List{System.String}}\" />s</summary>"),
        };

        var elementDescription = new AggregateBoundElementDescription(associatedTagHelperInfos.ToImmutableArray());

        // Act
        var classifiedTextElement = await ClassifiedTagHelperTooltipFactory.TryCreateTooltipAsync("file.razor", elementDescription, componentAvailabilityService, DisposalToken);

        // Assert
        Assert.NotNull(classifiedTextElement);

        // Expected output:
        //     Microsoft.AspNetCore.SomeTagHelper
        //     Uses List<List<string>>s
        Assert.Collection(classifiedTextElement.Runs,
            run => run.AssertExpectedClassification("Microsoft", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("AspNetCore", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("SomeTagHelper", ClassifiedTagHelperTooltipFactory.TypeClassificationName),
            run => run.AssertExpectedClassification(Environment.NewLine, ClassificationTypeNames.WhiteSpace),
            run => run.AssertExpectedClassification("Uses ", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification("List", ClassifiedTagHelperTooltipFactory.TypeClassificationName),
            run => run.AssertExpectedClassification("<", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("List", ClassifiedTagHelperTooltipFactory.TypeClassificationName),
            run => run.AssertExpectedClassification("<", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("string", ClassificationTypeNames.Keyword),
            run => run.AssertExpectedClassification(">", ClassificationTypeNames.Punctuation),
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
                "<summary>Uses <see cref=\"T:A.B.C{C.B}\" />s</summary>"),
        };

        var elementDescription = new AggregateBoundElementDescription(associatedTagHelperInfos.ToImmutableArray());

        // Act
        var classifiedTextElement = await ClassifiedTagHelperTooltipFactory.TryCreateTooltipAsync("file.razor", elementDescription, componentAvailabilityService, DisposalToken);

        // Assert
        Assert.NotNull(classifiedTextElement);

        // Expected output:
        //     Microsoft.AspNetCore.SomeTagHelper.SomeTagHelper
        //     Uses C<B>s
        Assert.Collection(classifiedTextElement.Runs,
            run => run.AssertExpectedClassification("Microsoft", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("AspNetCore", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("SomeTagHelper", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("SomeTagHelper", ClassifiedTagHelperTooltipFactory.TypeClassificationName),
            run => run.AssertExpectedClassification(Environment.NewLine, ClassificationTypeNames.WhiteSpace),
            run => run.AssertExpectedClassification("Uses ", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification("C", ClassifiedTagHelperTooltipFactory.TypeClassificationName),
            run => run.AssertExpectedClassification("<", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("B", ClassifiedTagHelperTooltipFactory.TypeClassificationName),
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
            new BoundElementDescriptionInfo("Microsoft.AspNetCore.SomeTagHelper", "<summary>\nUses <see cref=\"T:System.Collections.List{System.String}\" />s\n</summary>"),
            new BoundElementDescriptionInfo("Microsoft.AspNetCore.OtherTagHelper", "<summary>\nAlso uses <see cref=\"T:System.Collections.List{System.String}\" />s\n\r\n\r\r</summary>"),
        };
        var elementDescription = new AggregateBoundElementDescription(associatedTagHelperInfos.ToImmutableArray());

        // Act
        var classifiedTextElement = await ClassifiedTagHelperTooltipFactory.TryCreateTooltipAsync("file.razor", elementDescription, componentAvailabilityService, DisposalToken);

        // Assert
        Assert.NotNull(classifiedTextElement);

        // Expected output:
        //     Microsoft.AspNetCore.SomeTagHelper
        //     Uses List<string>s
        //
        //     Microsoft.AspNetCore.OtherTagHelper
        //     Also uses List<string>s
        Assert.Collection(classifiedTextElement.Runs,
            run => run.AssertExpectedClassification("Microsoft", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("AspNetCore", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("SomeTagHelper", ClassifiedTagHelperTooltipFactory.TypeClassificationName),
            run => run.AssertExpectedClassification(Environment.NewLine, ClassificationTypeNames.WhiteSpace),
            run => run.AssertExpectedClassification("Uses ", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification("List", ClassifiedTagHelperTooltipFactory.TypeClassificationName),
            run => run.AssertExpectedClassification("<", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("string", ClassificationTypeNames.Keyword),
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
            run => run.AssertExpectedClassification("Also uses ", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification("List", ClassifiedTagHelperTooltipFactory.TypeClassificationName),
            run => run.AssertExpectedClassification("<", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("string", ClassificationTypeNames.Keyword),
            run => run.AssertExpectedClassification(">", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("s", ClassificationTypeNames.Text));
    }

    [Fact]
    public void TryCreateTooltip_ClassifiedTextElement_NoAssociatedAttributeDescriptions_ReturnsFalse()
    {
        // Arrange
        var elementDescription = AggregateBoundAttributeDescription.Empty;

        // Act
        Assert.False(ClassifiedTagHelperTooltipFactory.TryCreateTooltip(elementDescription, out ClassifiedTextElement _));
    }

    [Fact]
    public void TryCreateTooltip_ClassifiedTextElement_Attribute_SingleAssociatedAttribute_ReturnsTrue_NestedTypes()
    {
        // Arrange
        var associatedAttributeDescriptions = new[]
        {
            new BoundAttributeDescriptionInfo(
                ReturnTypeName: "System.String",
                TypeName: "Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName",
                PropertyName: "SomeProperty",
                Documentation: "<summary>Uses <see cref=\"T:System.Collections.List{System.Collections.List{System.String}}\" />s</summary>")
        };
        var attributeDescription = new AggregateBoundAttributeDescription(associatedAttributeDescriptions.ToImmutableArray());

        // Act
        Assert.True(ClassifiedTagHelperTooltipFactory.TryCreateTooltip(attributeDescription, out ClassifiedTextElement classifiedTextElement));

        // Assert
        // Expected output:
        //     string Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName.SomeProperty
        //     Uses List<List<string>>s
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
            run => run.AssertExpectedClassification("Uses ", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification("List", ClassifiedTagHelperTooltipFactory.TypeClassificationName),
            run => run.AssertExpectedClassification("<", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("List", ClassifiedTagHelperTooltipFactory.TypeClassificationName),
            run => run.AssertExpectedClassification("<", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("string", ClassificationTypeNames.Keyword),
            run => run.AssertExpectedClassification(">", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification(">", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("s", ClassificationTypeNames.Text));
    }

    [Fact]
    public void TryCreateTooltip_ClassifiedTextElement_Attribute_MultipleAssociatedAttributes_ReturnsTrue()
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
        Assert.True(ClassifiedTagHelperTooltipFactory.TryCreateTooltip(attributeDescription, out ClassifiedTextElement classifiedTextElement));

        // Assert

        // Expected output:
        //     string Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName.SomeProperty
        //     Uses List<string>s
        //
        //     bool? Microsoft.AspNetCore.SomeTagHelpers.AnotherTypeName.AnotherProperty
        //     Uses List<string>s
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
            run => run.AssertExpectedClassification("Uses ", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification("List", ClassifiedTagHelperTooltipFactory.TypeClassificationName),
            run => run.AssertExpectedClassification("<", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("string", ClassificationTypeNames.Keyword),
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
            run => run.AssertExpectedClassification("Uses ", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification("List", ClassifiedTagHelperTooltipFactory.TypeClassificationName),
            run => run.AssertExpectedClassification("<", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("string", ClassificationTypeNames.Keyword),
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
        var containerElement = await ClassifiedTagHelperTooltipFactory.TryCreateTooltipContainerAsync("file.razor", elementDescription, componentAvailabilityService, DisposalToken);

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
            new BoundElementDescriptionInfo("Microsoft.AspNetCore.SomeTagHelper", "<summary>\nUses <see cref=\"T:System.Collections.List{System.String}\" />s\n</summary>"),
            new BoundElementDescriptionInfo("Microsoft.AspNetCore.OtherTagHelper", "<summary>\nAlso uses <see cref=\"T:System.Collections.List{System.String}\" />s\n\r\n\r\r</summary>"),
        };

        var elementDescription = new AggregateBoundElementDescription(associatedTagHelperInfos.ToImmutableArray());

        // Act
        var container = await ClassifiedTagHelperTooltipFactory.TryCreateTooltipContainerAsync("file.razor", elementDescription, componentAvailabilityService, DisposalToken);

        // Assert
        Assert.NotNull(container);
        var containerElements = container.Elements.ToList();

        // Expected output:
        //     [Class Glyph] Microsoft.AspNetCore.SomeTagHelper
        //     Uses List<string>s
        //
        //     [Class Glyph] Microsoft.AspNetCore.OtherTagHelper
        //     Also uses List<string>s
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

        // Uses List<string>s
        innerContainer = ((ContainerElement)containerElements[1]).Elements.ToList();
        classifiedTextElement = (ClassifiedTextElement)innerContainer[0];
        Assert.Single(innerContainer);
        Assert.Collection(classifiedTextElement.Runs,
            run => run.AssertExpectedClassification("Uses ", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification("List", ClassifiedTagHelperTooltipFactory.TypeClassificationName),
            run => run.AssertExpectedClassification("<", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("string", ClassificationTypeNames.Keyword),
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

        // Also uses List<string>s
        innerContainer = ((ContainerElement)containerElements[4]).Elements.ToList();
        classifiedTextElement = (ClassifiedTextElement)innerContainer[0];
        Assert.Single(innerContainer);
        Assert.Collection(classifiedTextElement.Runs,
            run => run.AssertExpectedClassification("Also uses ", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification("List", ClassifiedTagHelperTooltipFactory.TypeClassificationName),
            run => run.AssertExpectedClassification("<", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("string", ClassificationTypeNames.Keyword),
            run => run.AssertExpectedClassification(">", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("s", ClassificationTypeNames.Text));
    }

    [Fact]
    public void TryCreateTooltip_ContainerElement_NoAssociatedAttributeDescriptions_ReturnsFalse()
    {
        // Arrange
        var elementDescription = AggregateBoundAttributeDescription.Empty;

        // Act
        Assert.False(ClassifiedTagHelperTooltipFactory.TryCreateTooltip(elementDescription, out ContainerElement _));
    }

    [Fact]
    public void TryCreateTooltip_ContainerElement_Attribute_MultipleAssociatedAttributes_ReturnsTrue()
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
        Assert.True(ClassifiedTagHelperTooltipFactory.TryCreateTooltip(attributeDescription, out ContainerElement container));

        // Assert
        var containerElements = container.Elements.ToList();

        // Expected output:
        //     [Property Glyph] string Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName.SomeProperty
        //     Uses List<string>s
        //
        //     [Property Glyph] bool? Microsoft.AspNetCore.SomeTagHelpers.AnotherTypeName.AnotherProperty
        //     Uses List<string>s
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

        // Uses List<string>s
        innerContainer = ((ContainerElement)containerElements[1]).Elements.ToList();
        classifiedTextElement = (ClassifiedTextElement)innerContainer[0];
        Assert.Single(innerContainer);
        Assert.Collection(classifiedTextElement.Runs,
            run => run.AssertExpectedClassification("Uses ", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification("List", ClassifiedTagHelperTooltipFactory.TypeClassificationName),
            run => run.AssertExpectedClassification("<", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("string", ClassificationTypeNames.Keyword),
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

        // Uses List<string>s
        innerContainer = ((ContainerElement)containerElements[4]).Elements.ToList();
        classifiedTextElement = (ClassifiedTextElement)innerContainer[0];
        Assert.Single(innerContainer);
        Assert.Collection(classifiedTextElement.Runs,
            run => run.AssertExpectedClassification("Uses ", ClassificationTypeNames.Text),
            run => run.AssertExpectedClassification("List", ClassifiedTagHelperTooltipFactory.TypeClassificationName),
            run => run.AssertExpectedClassification("<", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("string", ClassificationTypeNames.Keyword),
            run => run.AssertExpectedClassification(">", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("s", ClassificationTypeNames.Text));
    }
}
