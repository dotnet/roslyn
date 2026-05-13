// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common.Workspaces;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.CohostingShared;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.DynamicFiles;

public class RazorDocumentExcerptServiceTest(ITestOutputHelper testOutput) : DocumentExcerptServiceTestBase(testOutput)
{
    [Fact]
    public async Task TryExcerptAsync_SingleLine_CanClassifyCSharp()
    {
        // Arrange
        var razorSource = @"
<html>
@{
    var [|foo|] = ""Hello, World!"";
}
  <body>@foo</body>
  <div>@(3 + 4)</div><div>@(foo + foo)</div>
</html>
";

        var (primary, secondary, secondarySpan) = await InitializeWithSnapshotAsync(razorSource, DisposalToken);

        var service = new RazorSourceGeneratedDocumentExcerptService(RemoteServiceInvoker);

        // Act
        var options = RazorClassificationOptionsWrapper.Default;
        var result = await service.TryExcerptAsync(secondary, secondarySpan, RazorExcerptMode.SingleLine, options, DisposalToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(secondarySpan, result.Value.Span);
        Assert.Same(secondary, result.Value.Document);

        // Verifies that the right part of the primary document will be highlighted.
        Assert.Equal(
            (await secondary.GetTextAsync()).GetSubText(secondarySpan).ToString(),
            result.Value.Content.GetSubText(result.Value.MappedSpan).ToString(),
            ignoreLineEndingDifferences: true);

        Assert.Equal(@"var foo = ""Hello, World!"";", result.Value.Content.ToString(), ignoreLineEndingDifferences: true);
        Assert.Collection(
            result.Value.ClassifiedSpans,
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Keyword, c.ClassificationType);
                Assert.Equal("var", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.LocalName, c.ClassificationType);
                Assert.Equal("foo", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Operator, c.ClassificationType);
                Assert.Equal("=", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.StringLiteral, c.ClassificationType);
                Assert.Equal("\"Hello, World!\"", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Punctuation, c.ClassificationType);
                Assert.Equal(";", result.Value.Content.GetSubText(c.TextSpan).ToString());
            });
    }

    [Fact]
    public async Task TryExcerptAsync_SingleLine_CanClassifyCSharp_ImplicitExpression()
    {
        // Arrange
        var razorSource = @"
<html>
@{
    var foo = ""Hello, World!"";
}
  <body>@[|foo|]</body>
  <div>@(3 + 4)</div><div>@(foo + foo)</div>
</html>
";

        var (primary, secondary, secondarySpan) = await InitializeWithSnapshotAsync(razorSource, DisposalToken);

        var service = new RazorSourceGeneratedDocumentExcerptService(RemoteServiceInvoker);

        // Act
        var options = RazorClassificationOptionsWrapper.Default;
        var result = await service.TryExcerptAsync(secondary, secondarySpan, RazorExcerptMode.SingleLine, options, DisposalToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(secondarySpan, result.Value.Span);
        Assert.Same(secondary, result.Value.Document);

        // Verifies that the right part of the primary document will be highlighted.
        Assert.Equal(
            (await secondary.GetTextAsync()).GetSubText(secondarySpan).ToString(),
            result.Value.Content.GetSubText(result.Value.MappedSpan).ToString(),
            ignoreLineEndingDifferences: true);

        Assert.Equal(@"<body>@foo</body>", result.Value.Content.ToString(), ignoreLineEndingDifferences: true);
        Assert.Collection(
            result.Value.ClassifiedSpans,
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal("<body>@", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.LocalName, c.ClassificationType);
                Assert.Equal("foo", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal("</body>", result.Value.Content.GetSubText(c.TextSpan).ToString());
            });
    }

    [Fact]
    public async Task TryExcerptAsync_SingleLine_CanClassifyCSharp_ComplexLine()
    {
        // Arrange
        var razorSource = @"
<html>
@{
    var foo = ""Hello, World!"";
}
  <body>@foo</body>
  <div>@(3 + 4)</div><div>@(foo + [|foo|])</div>
</html>
";

        var (primary, secondary, secondarySpan) = await InitializeWithSnapshotAsync(razorSource, DisposalToken);

        var service = new RazorSourceGeneratedDocumentExcerptService(RemoteServiceInvoker);

        // Act
        var options = RazorClassificationOptionsWrapper.Default;
        var result = await service.TryExcerptAsync(secondary, secondarySpan, RazorExcerptMode.SingleLine, options, DisposalToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(secondarySpan, result.Value.Span);
        Assert.Same(secondary, result.Value.Document);

        Assert.Equal(
            (await secondary.GetTextAsync()).GetSubText(secondarySpan).ToString(),
            result.Value.Content.GetSubText(result.Value.MappedSpan).ToString(),
            ignoreLineEndingDifferences: true);

        // Verifies that the right part of the primary document will be highlighted.
        Assert.Equal(@"<div>@(3 + 4)</div><div>@(foo + foo)</div>", result.Value.Content.ToString(), ignoreLineEndingDifferences: true);
        Assert.Collection(
            result.Value.ClassifiedSpans,
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal("<div>@(", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.NumericLiteral, c.ClassificationType);
                Assert.Equal("3", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Operator, c.ClassificationType);
                Assert.Equal("+", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.NumericLiteral, c.ClassificationType);
                Assert.Equal("4", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal(")</div><div>@(", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.LocalName, c.ClassificationType);
                Assert.Equal("foo", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Operator, c.ClassificationType);
                Assert.Equal("+", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.LocalName, c.ClassificationType);
                Assert.Equal("foo", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal(")</div>", result.Value.Content.GetSubText(c.TextSpan).ToString());
            });
    }

    [Fact]
    public async Task TryExcerptAsync_MultiLine_MultilineString()
    {
        // Arrange
        var razorSource = """
            <html>
            @{
                [|string|] bigString = @"
                    Razor shows 3 lines in a
                    tooltip maximum, so this
                    multi-line verbatim
                    string must be longer
                    than that.
                    ";
            }
            </html>
            """;

        var (primary, secondary, secondarySpan) = await InitializeWithSnapshotAsync(razorSource, DisposalToken);

        var service = new RazorSourceGeneratedDocumentExcerptService(RemoteServiceInvoker);

        // Act
        var options = RazorClassificationOptionsWrapper.Default;
        var result = await service.TryExcerptAsync(secondary, secondarySpan, RazorExcerptMode.Tooltip, options, DisposalToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(secondarySpan, result.Value.Span);
        Assert.Same(secondary, result.Value.Document);

        // Verifies that the right part of the primary document will be highlighted.
        Assert.Equal(
            (await secondary.GetTextAsync()).GetSubText(secondarySpan).ToString(),
            result.Value.Content.GetSubText(result.Value.MappedSpan).ToString(),
            ignoreLineEndingDifferences: true);

        Assert.Equal("""
            <html>
            @{
                string bigString = @"
                    Razor shows 3 lines in a
                    tooltip maximum, so this
                    multi-line verbatim
            """,
            result.Value.Content.ToString(), ignoreLineEndingDifferences: true);

        Assert.Collection(
            result.Value.ClassifiedSpans,
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal("""
                    <html>
                    @{
                    """,
                    result.Value.Content.GetSubText(c.TextSpan).ToString(),
                    ignoreLineEndingDifferences: true);
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal("\r\n    ", result.Value.Content.GetSubText(c.TextSpan).ToString(), ignoreLineEndingDifferences: true);
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Keyword, c.ClassificationType);
                Assert.Equal("string", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.LocalName, c.ClassificationType);
                Assert.Equal("bigString", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Operator, c.ClassificationType);
                Assert.Equal("=", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.VerbatimStringLiteral, c.ClassificationType);
                Assert.Equal("""
                    @"
                            Razor shows 3 lines in a
                            tooltip maximum, so this
                            multi-line verbatim
                    """, result.Value.Content.GetSubText(c.TextSpan).ToString(), ignoreLineEndingDifferences: true);
            });
    }

    [Fact]
    public async Task TryExcerptAsync_SingleLine_MultilineString()
    {
        // Arrange
        var razorSource = """
            <html>
            @{
                [|string|] bigString = @"
                    This is a
                    multi-line verbatim
                    string.
                    ";
            }
            </html>
            """;

        var (primary, secondary, secondarySpan) = await InitializeWithSnapshotAsync(razorSource, DisposalToken);

        var service = new RazorSourceGeneratedDocumentExcerptService(RemoteServiceInvoker);

        // Act
        var options = RazorClassificationOptionsWrapper.Default;
        var result = await service.TryExcerptAsync(secondary, secondarySpan, RazorExcerptMode.SingleLine, options, DisposalToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(secondarySpan, result.Value.Span);
        Assert.Same(secondary, result.Value.Document);

        // Verifies that the right part of the primary document will be highlighted.
        Assert.Equal(
            (await secondary.GetTextAsync()).GetSubText(secondarySpan).ToString(),
            result.Value.Content.GetSubText(result.Value.MappedSpan).ToString(),
            ignoreLineEndingDifferences: true);

        Assert.Equal("string bigString = @\"", result.Value.Content.ToString(), ignoreLineEndingDifferences: true);

        Assert.Collection(
            result.Value.ClassifiedSpans,
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Keyword, c.ClassificationType);
                Assert.Equal("string", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.LocalName, c.ClassificationType);
                Assert.Equal("bigString", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Operator, c.ClassificationType);
                Assert.Equal("=", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.VerbatimStringLiteral, c.ClassificationType);
                Assert.Equal("@\"", result.Value.Content.GetSubText(c.TextSpan).ToString());
            });
    }

    [Fact]
    public async Task TryExcerptAsync_MultiLine_CanClassifyCSharp()
    {
        // Arrange
        var razorSource = @"
<html>
@{
    var [|foo|] = ""Hello, World!"";
}
  <body></body>
  <div></div>
</html>
";

        var (primary, secondary, secondarySpan) = await InitializeWithSnapshotAsync(razorSource, DisposalToken);

        var service = new RazorSourceGeneratedDocumentExcerptService(RemoteServiceInvoker);

        // Act
        var options = RazorClassificationOptionsWrapper.Default;
        var result = await service.TryExcerptAsync(secondary, secondarySpan, RazorExcerptMode.Tooltip, options, DisposalToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(secondarySpan, result.Value.Span);
        Assert.Same(secondary, result.Value.Document);

        // Verifies that the right part of the primary document will be highlighted.
        Assert.Equal(
            (await secondary.GetTextAsync()).GetSubText(secondarySpan).ToString(),
            result.Value.Content.GetSubText(result.Value.MappedSpan).ToString(),
            ignoreLineEndingDifferences: true);

        Assert.Equal(
@"
<html>
@{
    var foo = ""Hello, World!"";
}
  <body></body>
  <div></div>",
            result.Value.Content.ToString(), ignoreLineEndingDifferences: true);

        Assert.Collection(
            result.Value.ClassifiedSpans,
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal(
@"
<html>
@{",
                        result.Value.Content.GetSubText(c.TextSpan).ToString(),
                        ignoreLineEndingDifferences: true);
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal("\r\n    ", result.Value.Content.GetSubText(c.TextSpan).ToString(), ignoreLineEndingDifferences: true);
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Keyword, c.ClassificationType);
                Assert.Equal("var", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.LocalName, c.ClassificationType);
                Assert.Equal("foo", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Operator, c.ClassificationType);
                Assert.Equal("=", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.StringLiteral, c.ClassificationType);
                Assert.Equal("\"Hello, World!\"", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Punctuation, c.ClassificationType);
                Assert.Equal(";", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal("\r\n", result.Value.Content.GetSubText(c.TextSpan).ToString(), ignoreLineEndingDifferences: true);
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal(
@"}
  <body></body>
  <div></div>",
                    result.Value.Content.GetSubText(c.TextSpan).ToString(),
                    ignoreLineEndingDifferences: true);
            });
    }

    [Fact]
    public async Task TryExcerptAsync_MultiLine_Boundaries_CanClassifyCSharp()
    {
        // Arrange
        var razorSource = @"@{ var [|foo|] = ""Hello, World!""; }";

        var (primary, secondary, secondarySpan) = await InitializeWithSnapshotAsync(razorSource, DisposalToken);

        var service = new RazorSourceGeneratedDocumentExcerptService(RemoteServiceInvoker);

        // Act
        var options = RazorClassificationOptionsWrapper.Default;
        var result = await service.TryExcerptAsync(secondary, secondarySpan, RazorExcerptMode.Tooltip, options, DisposalToken);

        // Assert
        // Verifies that the right part of the primary document will be highlighted.
        Assert.NotNull(result);
        Assert.Equal(secondarySpan, result.Value.Span);
        Assert.Same(secondary, result.Value.Document);

        Assert.Equal(
            (await secondary.GetTextAsync()).GetSubText(secondarySpan).ToString(),
            result.Value.Content.GetSubText(result.Value.MappedSpan).ToString(),
            ignoreLineEndingDifferences: true);

        Assert.Equal(
@"@{ var foo = ""Hello, World!""; }",
            result.Value.Content.ToString(), ignoreLineEndingDifferences: true);

        Assert.Collection(
            result.Value.ClassifiedSpans,
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal("@{", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Keyword, c.ClassificationType);
                Assert.Equal("var", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.LocalName, c.ClassificationType);
                Assert.Equal("foo", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Operator, c.ClassificationType);
                Assert.Equal("=", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.StringLiteral, c.ClassificationType);
                Assert.Equal("\"Hello, World!\"", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Punctuation, c.ClassificationType);
                Assert.Equal(";", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal(" ", result.Value.Content.GetSubText(c.TextSpan).ToString());
            },
            c =>
            {
                Assert.Equal(ClassificationTypeNames.Text, c.ClassificationType);
                Assert.Equal("}", result.Value.Content.GetSubText(c.TextSpan).ToString());
            });
    }
}
