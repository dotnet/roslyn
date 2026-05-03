// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Razor.Tooltip;
using Roslyn.Text.Adornments;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Hover;

public class HoverFactoryTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    private static HoverDisplayOptions UseMarkdown => new(MarkupKind.Markdown, SupportsVisualStudioExtensions: false);
    private static HoverDisplayOptions UsePlainText => new(MarkupKind.PlainText, SupportsVisualStudioExtensions: false);

    private static HoverDisplayOptions UseVisualStudio => new(MarkupKind.Markdown, SupportsVisualStudioExtensions: true);

    private static IComponentAvailabilityService CreateComponentAvailabilityService()
    {
        var mock = new StrictMock<IComponentAvailabilityService>();
        mock.Setup(x => x.GetComponentAvailabilityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        return mock.Object;
    }

    [Fact]
    public async Task GetHoverAsync_TagHelper_Element()
    {
        // Arrange
        TestCode code = """
            @addTagHelper *, TestAssembly
            <$$test1></test1>
            """;

        var codeDocument = RazorCodeDocumentFactory.CreateCodeDocument(code.Text, isRazorFile: false, SimpleTagHelpers.Default);

        // Act
        var hover = await HoverFactory.GetHoverAsync(codeDocument, code.Position, UseMarkdown, CreateComponentAvailabilityService(), DisposalToken);

        // Assert
        Assert.NotNull(hover);
        Assert.Contains("**Test1TagHelper**", ((MarkupContent)hover.Contents).Value, StringComparison.Ordinal);
        var expectedRange = LspFactory.CreateSingleLineRange(line: 1, character: 1, length: 5);
        Assert.Equal(expectedRange, hover.Range);
    }

    [Fact]
    public async Task GetHoverAsync_TagHelper_Element_WithParent()
    {
        // Arrange
        TestCode code = """
            @addTagHelper *, TestAssembly
            <test1>
                <Som$$eChild></SomeChild>
            </test1>
            """;

        var codeDocument = RazorCodeDocumentFactory.CreateCodeDocument(code.Text, isRazorFile: false, SimpleTagHelpers.Default);

        // Act
        var hover = await HoverFactory.GetHoverAsync(codeDocument, code.Position, UseMarkdown, CreateComponentAvailabilityService(), DisposalToken);

        // Assert
        Assert.NotNull(hover);
        Assert.Contains("**SomeChild**", ((MarkupContent)hover.Contents).Value, StringComparison.Ordinal);
        var expectedRange = LspFactory.CreateSingleLineRange(line: 2, character: 5, length: 9);
        Assert.Equal(expectedRange, hover.Range);
    }

    [Fact]
    public async Task GetHoverAsync_TagHelper_Attribute_WithParent()
    {
        // Arrange
        TestCode code = """
            @addTagHelper *, TestAssembly
            <test1>
                <SomeChild [|att$$ribute|]="test"></SomeChild>
            </test1>
            """;

        var codeDocument = RazorCodeDocumentFactory.CreateCodeDocument(code.Text, isRazorFile: false, SimpleTagHelpers.Default);

        // Act
        var hover = await HoverFactory.GetHoverAsync(codeDocument, code.Position, UseMarkdown, CreateComponentAvailabilityService(), DisposalToken);

        // Assert
        Assert.NotNull(hover);
        Assert.Contains("**Attribute**", ((MarkupContent)hover.Contents).Value, StringComparison.Ordinal);
        var expectedRange = codeDocument.Source.Text.GetRange(code.Span);
        Assert.Equal(expectedRange, hover.Range);
    }

    [Fact]
    public async Task GetHoverAsync_TagHelper_Element_EndTag()
    {
        // Arrange
        TestCode code = """
            @addTagHelper *, TestAssembly
            <test1></$$test1>
            """;

        var codeDocument = RazorCodeDocumentFactory.CreateCodeDocument(code.Text, isRazorFile: false, SimpleTagHelpers.Default);

        // Act
        var hover = await HoverFactory.GetHoverAsync(codeDocument, code.Position, UseMarkdown, CreateComponentAvailabilityService(), DisposalToken);

        // Assert
        Assert.NotNull(hover);
        Assert.Contains("**Test1TagHelper**", ((MarkupContent)hover.Contents).Value, StringComparison.Ordinal);
        var expectedRange = LspFactory.CreateSingleLineRange(line: 1, character: 9, length: 5);
        Assert.Equal(expectedRange, hover.Range);
    }

    [Fact]
    public async Task GetHoverAsync_TagHelper_Attribute()
    {
        // Arrange
        TestCode code = """
            @addTagHelper *, TestAssembly
            <test1 $$bool-val='true'></test1>
            """;

        var codeDocument = RazorCodeDocumentFactory.CreateCodeDocument(code.Text, isRazorFile: false, SimpleTagHelpers.Default);

        // Act
        var hover = await HoverFactory.GetHoverAsync(codeDocument, code.Position, UseMarkdown, CreateComponentAvailabilityService(), DisposalToken);

        // Assert
        Assert.NotNull(hover);
        Assert.Contains("**BoolVal**", ((MarkupContent)hover.Contents).Value, StringComparison.Ordinal);
        Assert.DoesNotContain("**IntVal**", ((MarkupContent)hover.Contents).Value, StringComparison.Ordinal);
        var expectedRange = LspFactory.CreateSingleLineRange(line: 1, character: 7, length: 8);
        Assert.Equal(expectedRange, hover.Range);
    }

    [Fact]
    public async Task GetHoverAsync_TagHelper_AttributeTrailingEdge()
    {
        // Arrange
        TestCode code = """
            @addTagHelper *, TestAssembly
            <test1 bool-val$$ minimized></test1>
            """;

        var codeDocument = RazorCodeDocumentFactory.CreateCodeDocument(code.Text, isRazorFile: false, SimpleTagHelpers.Default);

        // Act
        var hover = await HoverFactory.GetHoverAsync(codeDocument, code.Position, UseMarkdown, CreateComponentAvailabilityService(), DisposalToken);

        // Assert
        Assert.NotNull(hover);
        Assert.Contains("**BoolVal**", ((MarkupContent)hover.Contents).Value, StringComparison.Ordinal);
        Assert.DoesNotContain("**IntVal**", ((MarkupContent)hover.Contents).Value, StringComparison.Ordinal);
        var expectedRange = LspFactory.CreateSingleLineRange(line: 1, character: 7, length: 8);
        Assert.Equal(expectedRange, hover.Range);
    }

    [Fact]
    public async Task GetHoverAsync_TagHelper_AttributeValue_ReturnsNull()
    {
        // Arrange
        TestCode code = """
            @addTagHelper *, TestAssembly
            <test1 bool-val='$$true'></test1>
            """;

        var codeDocument = RazorCodeDocumentFactory.CreateCodeDocument(code.Text, isRazorFile: false, SimpleTagHelpers.Default);

        // Act
        var hover = await HoverFactory.GetHoverAsync(codeDocument, code.Position, UseMarkdown, CreateComponentAvailabilityService(), DisposalToken);

        // Assert
        Assert.Null(hover);
    }

    [Fact]
    public async Task GetHoverAsync_TagHelper_AfterAttributeEquals_ReturnsNull()
    {
        // Arrange
        TestCode code = """
            @addTagHelper *, TestAssembly
            <test1 bool-val=$$'true'></test1>
            """;

        var codeDocument = RazorCodeDocumentFactory.CreateCodeDocument(code.Text, isRazorFile: false, SimpleTagHelpers.Default);

        // Act
        var hover = await HoverFactory.GetHoverAsync(codeDocument, code.Position, UseMarkdown, CreateComponentAvailabilityService(), DisposalToken);

        // Assert
        Assert.Null(hover);
    }

    [Fact]
    public async Task GetHoverAsync_TagHelper_AttributeEnd_ReturnsNull()
    {
        // Arrange
        TestCode code = """
            @addTagHelper *, TestAssembly
            <test1 bool-val='true'$$></test1>
            """;

        var codeDocument = RazorCodeDocumentFactory.CreateCodeDocument(code.Text, isRazorFile: false, SimpleTagHelpers.Default);

        // Act
        var hover = await HoverFactory.GetHoverAsync(codeDocument, code.Position, UseMarkdown, CreateComponentAvailabilityService(), DisposalToken);

        // Assert
        Assert.Null(hover);
    }

    [Fact]
    public async Task GetHoverAsync_TagHelper_MinimizedAttribute()
    {
        // Arrange
        TestCode code = """
            @addTagHelper *, TestAssembly
            <test1 $$bool-val></test1>
            """;

        var codeDocument = RazorCodeDocumentFactory.CreateCodeDocument(code.Text, isRazorFile: false, SimpleTagHelpers.Default);

        // Act
        var hover = await HoverFactory.GetHoverAsync(codeDocument, code.Position, UseMarkdown, CreateComponentAvailabilityService(), DisposalToken);

        // Assert
        Assert.NotNull(hover);
        Assert.Contains("**BoolVal**", ((MarkupContent)hover.Contents).Value, StringComparison.Ordinal);
        Assert.DoesNotContain("**IntVal**", ((MarkupContent)hover.Contents).Value, StringComparison.Ordinal);
        var expectedRange = LspFactory.CreateSingleLineRange(line: 1, character: 7, length: 8);
        Assert.Equal(expectedRange, hover.Range);
    }

    [Fact]
    public async Task GetHoverAsync_DirectiveAttribute_HasResult()
    {
        // Arrange
        TestCode code = """
            @addTagHelper *, TestAssembly
            <any @t$$est="Increment" />
            @code{
                public void Increment(){
                }
            }
            """;

        var codeDocument = RazorCodeDocumentFactory.CreateCodeDocument(code.Text, "text.razor", SimpleTagHelpers.Default);

        // Act
        var hover = await HoverFactory.GetHoverAsync(codeDocument, code.Position, UseMarkdown, CreateComponentAvailabilityService(), DisposalToken);

        // Assert
        Assert.NotNull(hover);
        Assert.Contains("**Test**", ((MarkupContent)hover.Contents).Value, StringComparison.Ordinal);
        var expectedRange = LspFactory.CreateSingleLineRange(line: 1, character: 5, length: 5);
        Assert.Equal(expectedRange, hover.Range);
    }

    [Fact]
    public async Task GetHoverAsync_TagHelper_MalformedElement()
    {
        // Arrange
        TestCode code = """
            @addTagHelper *, TestAssembly
            <$$test1<hello
            """;

        var codeDocument = RazorCodeDocumentFactory.CreateCodeDocument(code.Text, isRazorFile: false, SimpleTagHelpers.Default);

        // Act
        var hover = await HoverFactory.GetHoverAsync(codeDocument, code.Position, UseMarkdown, CreateComponentAvailabilityService(), DisposalToken);

        // Assert
        Assert.NotNull(hover);
        Assert.Contains("**Test1TagHelper**", ((MarkupContent)hover.Contents).Value, StringComparison.Ordinal);
        var expectedRange = LspFactory.CreateSingleLineRange(line: 1, character: 1, length: 5);
        Assert.Equal(expectedRange, hover.Range);
    }

    [Fact]
    public async Task GetHoverAsync_TagHelper_MalformedAttribute()
    {
        // Arrange
        TestCode code = """
            @addTagHelper *, TestAssembly
            <test1 $$bool-val=\"aslj alsk<strong>
            """;

        var codeDocument = RazorCodeDocumentFactory.CreateCodeDocument(code.Text, isRazorFile: false, SimpleTagHelpers.Default);

        // Act
        var hover = await HoverFactory.GetHoverAsync(codeDocument, code.Position, UseMarkdown, CreateComponentAvailabilityService(), DisposalToken);

        // Assert
        Assert.NotNull(hover);
        Assert.Contains("**BoolVal**", ((MarkupContent)hover.Contents).Value, StringComparison.Ordinal);
        Assert.DoesNotContain("**IntVal**", ((MarkupContent)hover.Contents).Value, StringComparison.Ordinal);
        var expectedRange = LspFactory.CreateSingleLineRange(line: 1, character: 7, length: 8);
        Assert.Equal(expectedRange, hover.Range);
    }

    [Fact]
    public async Task GetHoverAsync_HTML_MarkupElement()
    {
        // Arrange
        TestCode code = """
            @addTagHelper *, TestAssembly
            <p><$$strong></strong></p>
            """;

        var codeDocument = RazorCodeDocumentFactory.CreateCodeDocument(code.Text, isRazorFile: false);

        // Act
        var hover = await HoverFactory.GetHoverAsync(codeDocument, code.Position, UseMarkdown, CreateComponentAvailabilityService(), DisposalToken);

        // Assert
        Assert.Null(hover);
    }

    [Fact]
    public async Task GetHoverAsync_TagHelper_PlainTextElement()
    {
        // Arrange
        TestCode code = """
            @addTagHelper *, TestAssembly
            <$$test1></test1>
            """;

        var codeDocument = RazorCodeDocumentFactory.CreateCodeDocument(code.Text, isRazorFile: false, SimpleTagHelpers.Default);

        // Act
        var hover = await HoverFactory.GetHoverAsync(codeDocument, code.Position, UsePlainText, CreateComponentAvailabilityService(), DisposalToken);

        // Assert
        Assert.NotNull(hover);
        Assert.Contains("Test1TagHelper", ((MarkupContent)hover.Contents).Value, StringComparison.Ordinal);
        Assert.Equal(MarkupKind.PlainText, ((MarkupContent)hover.Contents).Kind);
        var expectedRange = LspFactory.CreateSingleLineRange(line: 1, character: 1, length: 5);
        Assert.Equal(expectedRange, hover.Range);
    }

    [Fact]
    public async Task GetHoverAsync_TagHelper_PlainTextElement_EndTag()
    {
        // Arrange
        TestCode code = """
            @addTagHelper *, TestAssembly
            <test1></$$test1>
            """;

        var codeDocument = RazorCodeDocumentFactory.CreateCodeDocument(code.Text, isRazorFile: false, SimpleTagHelpers.Default);

        // Act
        var hover = await HoverFactory.GetHoverAsync(codeDocument, code.Position, UsePlainText, CreateComponentAvailabilityService(), DisposalToken);

        // Assert
        Assert.NotNull(hover);
        Assert.Contains("Test1TagHelper", ((MarkupContent)hover.Contents).Value, StringComparison.Ordinal);
        Assert.Equal(MarkupKind.PlainText, ((MarkupContent)hover.Contents).Kind);
        var expectedRange = LspFactory.CreateSingleLineRange(line: 1, character: 9, length: 5);
        Assert.Equal(expectedRange, hover.Range);
    }

    [Fact]
    public async Task GetHoverAsync_TagHelper_TextComponent()
    {
        // Arrange
        TestCode code = """
            <$$Text></Text>
            """;

        var codeDocument = RazorCodeDocumentFactory.CreateCodeDocument(code.Text, isRazorFile: true, SimpleTagHelpers.Default);

        // Act
        var hover = await HoverFactory.GetHoverAsync(codeDocument, code.Position, UsePlainText, CreateComponentAvailabilityService(), DisposalToken);

        // Assert
        Assert.NotNull(hover);
        Assert.Contains("Text", ((MarkupContent)hover.Contents).Value, StringComparison.Ordinal);
        Assert.Equal(MarkupKind.PlainText, ((MarkupContent)hover.Contents).Kind);
        var expectedRange = LspFactory.CreateSingleLineRange(line: 0, character: 1, length: 4);
        Assert.Equal(expectedRange, hover.Range);
    }

    [Fact]
    public async Task GetHoverAsync_TagHelper_TextComponent_NestedInHtml()
    {
        // Arrange
        TestCode code = """
            <div>
                <$$Text></Text>
            </div>
            """;

        var codeDocument = RazorCodeDocumentFactory.CreateCodeDocument(code.Text, isRazorFile: true, SimpleTagHelpers.Default);

        // Act
        var hover = await HoverFactory.GetHoverAsync(codeDocument, code.Position, UsePlainText, CreateComponentAvailabilityService(), DisposalToken);

        // Assert
        Assert.NotNull(hover);
        Assert.Contains("Text", ((MarkupContent)hover.Contents).Value, StringComparison.Ordinal);
        Assert.Equal(MarkupKind.PlainText, ((MarkupContent)hover.Contents).Kind);
        var expectedRange = LspFactory.CreateSingleLineRange(line: 1, character: 5, length: 4);
        Assert.Equal(expectedRange, hover.Range);
    }

    [Fact]
    public async Task GetHoverAsync_TagHelper_TextComponent_NestedInCSharp()
    {
        // Arrange
        TestCode code = """
            @if (true)
            {
                <$$Text></Text>
            }
            """;

        var codeDocument = RazorCodeDocumentFactory.CreateCodeDocument(code.Text, isRazorFile: true, SimpleTagHelpers.Default);

        // Act
        var hover = await HoverFactory.GetHoverAsync(codeDocument, code.Position, UsePlainText, CreateComponentAvailabilityService(), DisposalToken);

        // Assert
        Assert.Null(hover);
    }

    [Fact]
    public async Task GetHoverAsync_TagHelper_TextComponent_NestedInCSharpAndText()
    {
        // Arrange
        TestCode code = """
            @if (true)
            {
                <text>
                    <$$Text></Text>
                </text>
            }
            """;

        var codeDocument = RazorCodeDocumentFactory.CreateCodeDocument(code.Text, isRazorFile: true, SimpleTagHelpers.Default);

        // Act
        var hover = await HoverFactory.GetHoverAsync(codeDocument, code.Position, UsePlainText, CreateComponentAvailabilityService(), DisposalToken);

        // Assert
        Assert.NotNull(hover);
        Assert.Contains("Text", ((MarkupContent)hover.Contents).Value, StringComparison.Ordinal);
        Assert.Equal(MarkupKind.PlainText, ((MarkupContent)hover.Contents).Kind);
        var expectedRange = LspFactory.CreateSingleLineRange(line: 3, character: 9, length: 4);
        Assert.Equal(expectedRange, hover.Range);
    }

    [Fact]
    public async Task GetHoverAsync_TagHelper_PlainTextAttribute()
    {
        // Arrange
        TestCode code = """
            @addTagHelper *, TestAssembly
            <test1 $$bool-val></test1>
            """;

        var codeDocument = RazorCodeDocumentFactory.CreateCodeDocument(code.Text, isRazorFile: false, SimpleTagHelpers.Default);

        // Act
        var hover = await HoverFactory.GetHoverAsync(codeDocument, code.Position, UsePlainText, CreateComponentAvailabilityService(), DisposalToken);

        // Assert
        Assert.NotNull(hover);
        Assert.Contains("BoolVal", ((MarkupContent)hover.Contents).Value, StringComparison.Ordinal);
        Assert.DoesNotContain("IntVal", ((MarkupContent)hover.Contents).Value, StringComparison.Ordinal);
        Assert.Equal(MarkupKind.PlainText, ((MarkupContent)hover.Contents).Kind);
        var expectedRange = LspFactory.CreateSingleLineRange(line: 1, character: 7, length: 8);
        Assert.Equal(expectedRange, hover.Range);
    }

    [Fact]
    public async Task GetHoverAsync_HTML_PlainTextElement()
    {
        // Arrange
        TestCode code = """
            @addTagHelper *, TestAssembly
            <p><$$strong></strong></p>
            """;

        var codeDocument = RazorCodeDocumentFactory.CreateCodeDocument(code.Text, isRazorFile: false);

        // Act
        var hover = await HoverFactory.GetHoverAsync(codeDocument, code.Position, UsePlainText, CreateComponentAvailabilityService(), DisposalToken);

        // Assert
        Assert.Null(hover);
    }

    [Fact]
    public async Task GetHoverAsync_HTML_PlainTextAttribute()
    {
        // Arrange
        TestCode code = """
            @addTagHelper *, TestAssembly
            <p><strong class="$$weak"></strong></p>
            """;

        var codeDocument = RazorCodeDocumentFactory.CreateCodeDocument(code.Text, isRazorFile: false);

        // Act
        var hover = await HoverFactory.GetHoverAsync(codeDocument, code.Position, UsePlainText, CreateComponentAvailabilityService(), DisposalToken);

        // Assert
        Assert.Null(hover);
    }

    [Fact]
    public async Task GetHoverAsync_TagHelper_Element_VSClient_ReturnVSHover()
    {
        // Arrange
        TestCode code = """
            @addTagHelper *, TestAssembly
            <$$test1></test1>
            """;

        var codeDocument = RazorCodeDocumentFactory.CreateCodeDocument(code.Text, isRazorFile: false, SimpleTagHelpers.Default);

        // Act
        var hover = await HoverFactory.GetHoverAsync(codeDocument, code.Position, UseVisualStudio, CreateComponentAvailabilityService(), DisposalToken);

        // Assert
        Assert.NotNull(hover);
        Assert.False(hover.Contents.TryGetFourth(out _));
        Assert.True(hover.Contents.TryGetThird(out _) && !hover.Contents.Third.Any());
        var expectedRange = LspFactory.CreateSingleLineRange(line: 1, character: 1, length: 5);
        Assert.Equal(expectedRange, hover.Range);

        var vsHover = Assert.IsType<VSInternalHover>(hover);
        Assert.NotNull(vsHover.RawContent);
        var container = (ContainerElement)vsHover.RawContent;
        var containerElements = container.Elements.ToList();
        Assert.Equal(ContainerElementStyle.Stacked, container.Style);
        Assert.Single(containerElements);

        // [TagHelper Glyph] Test1TagHelper
        var innerContainer = ((ContainerElement)containerElements[0]).Elements.ToList();
        var classifiedTextElement = (ClassifiedTextElement)innerContainer[1];
        Assert.Equal(2, innerContainer.Count);
        Assert.Equal(ClassifiedTagHelperTooltipFactory.ClassGlyph, innerContainer[0]);
        Assert.Collection(classifiedTextElement.Runs,
            run => run.AssertExpectedClassification("Test1TagHelper", ClassifiedTagHelperTooltipFactory.TypeClassificationName));
    }

    [Fact]
    public async Task GetHoverAsync_TagHelper_Attribute_VSClient_ReturnVSHover()
    {
        // Arrange
        TestCode code = """
            @addTagHelper *, TestAssembly
            <test1 $$bool-val='true'></test1>
            """;

        var codeDocument = RazorCodeDocumentFactory.CreateCodeDocument(code.Text, isRazorFile: false, SimpleTagHelpers.Default);

        // Act
        var hover = await HoverFactory.GetHoverAsync(codeDocument, code.Position, UseVisualStudio, CreateComponentAvailabilityService(), DisposalToken);

        // Assert
        Assert.NotNull(hover);
        Assert.False(hover.Contents.TryGetFourth(out _));
        Assert.True(hover.Contents.TryGetThird(out var markedStrings) && !markedStrings.Any());
        var expectedRange = LspFactory.CreateSingleLineRange(line: 1, character: 7, length: 8);
        Assert.Equal(expectedRange, hover.Range);

        var vsHover = Assert.IsType<VSInternalHover>(hover);
        Assert.NotNull(vsHover.RawContent);
        var container = (ContainerElement)vsHover.RawContent;
        var containerElements = container.Elements.ToList();
        Assert.Equal(ContainerElementStyle.Stacked, container.Style);
        Assert.Single(containerElements);

        // [TagHelper Glyph] bool Test1TagHelper.BoolVal
        var innerContainer = ((ContainerElement)containerElements[0]).Elements.ToList();
        var classifiedTextElement = (ClassifiedTextElement)innerContainer[1];
        Assert.Equal(2, innerContainer.Count);
        Assert.Equal(ClassifiedTagHelperTooltipFactory.PropertyGlyph, innerContainer[0]);
        Assert.Collection(classifiedTextElement.Runs,
            run => run.AssertExpectedClassification("bool", ClassificationTypeNames.Keyword),
            run => run.AssertExpectedClassification(" ", ClassificationTypeNames.WhiteSpace),
            run => run.AssertExpectedClassification("Test1TagHelper", ClassifiedTagHelperTooltipFactory.TypeClassificationName),
            run => run.AssertExpectedClassification(".", ClassificationTypeNames.Punctuation),
            run => run.AssertExpectedClassification("BoolVal", ClassificationTypeNames.Identifier));
    }
}
