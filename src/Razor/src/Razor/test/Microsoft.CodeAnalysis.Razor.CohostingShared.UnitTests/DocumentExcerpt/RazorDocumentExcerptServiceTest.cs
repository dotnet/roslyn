// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Razor.CohostingShared;
using Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Razor.LanguageClient.Cohost;
using Xunit;
using Xunit.Abstractions;
using TestFileMarkupParser = Microsoft.CodeAnalysis.Testing.TestFileMarkupParser;

namespace Microsoft.VisualStudio.Razor.DynamicFiles;

public class RazorDocumentExcerptServiceTest(ITestOutputHelper testOutput) : CohostEndpointTestBase(testOutput)
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

        var (_, secondary, secondarySpan) = await InitializeWithSnapshotAsync(razorSource, DisposalToken);

        var service = new RazorSourceGeneratedDocumentExcerptService(RemoteServiceInvoker);

        // Act
        var options = ClassificationOptions.Default;
        var result = await service.TryExcerptAsync(secondary, secondarySpan, ExcerptMode.SingleLine, options, DisposalToken);

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
        var options = ClassificationOptions.Default;
        var result = await service.TryExcerptAsync(secondary, secondarySpan, ExcerptMode.SingleLine, options, DisposalToken);

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
        var options = ClassificationOptions.Default;
        var result = await service.TryExcerptAsync(secondary, secondarySpan, ExcerptMode.SingleLine, options, DisposalToken);

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
    public async Task TryExcerptAsync_SingleLine_CanClassifyCSharp_DeclarationDocumentProperty()
    {
        // Arrange
        var razorSource = """
            <h1>@Message</h1>

            @code {
                private [|string|] Message { get; set; } = "Hello, World!";
            }
            """;

        var (primary, secondary, secondarySpan) = await InitializeWithSnapshotAsync(razorSource, DisposalToken);

        var service = new RazorSourceGeneratedDocumentExcerptService(RemoteServiceInvoker);

        // Act
        var options = ClassificationOptions.Default;
        var result = await service.TryExcerptAsync(secondary, secondarySpan, ExcerptMode.SingleLine, options, DisposalToken);

        // Assert
        Assert.EndsWith(".decl.g.cs", secondary.HintName, StringComparison.Ordinal);
        Assert.NotNull(result);
        Assert.Equal(secondarySpan, result.Value.Span);
        Assert.Same(secondary, result.Value.Document);

        Assert.Equal(
            (await secondary.GetTextAsync()).GetSubText(secondarySpan).ToString(),
            result.Value.Content.GetSubText(result.Value.MappedSpan).ToString(),
            ignoreLineEndingDifferences: true);

        Assert.Equal(@"private string Message { get; set; } = ""Hello, World!"";", result.Value.Content.ToString(), ignoreLineEndingDifferences: true);
        Assert.Contains(
            result.Value.ClassifiedSpans,
            c => c.ClassificationType == ClassificationTypeNames.Keyword &&
                result.Value.Content.GetSubText(c.TextSpan).ToString() == "string");
    }

    [Fact]
    public async Task TryExcerptAsync_SingleLine_CanClassifyCSharp_DeclarationDocumentMethod()
    {
        // Arrange
        var razorSource = """
            <button @onclick="HandleClick">Click me</button>

            @code {
                private string Message { get; set; } = "Hello, World!";
                private void [|HandleClick|]() => Message = "Clicked";
            }
            """;

        var (primary, secondary, secondarySpan) = await InitializeWithSnapshotAsync(razorSource, DisposalToken);

        var service = new RazorSourceGeneratedDocumentExcerptService(RemoteServiceInvoker);

        // Act
        var options = ClassificationOptions.Default;
        var result = await service.TryExcerptAsync(secondary, secondarySpan, ExcerptMode.SingleLine, options, DisposalToken);

        // Assert
        Assert.EndsWith(".decl.g.cs", secondary.HintName, StringComparison.Ordinal);
        Assert.NotNull(result);
        Assert.Equal(secondarySpan, result.Value.Span);
        Assert.Same(secondary, result.Value.Document);

        Assert.Equal(
            (await secondary.GetTextAsync()).GetSubText(secondarySpan).ToString(),
            result.Value.Content.GetSubText(result.Value.MappedSpan).ToString(),
            ignoreLineEndingDifferences: true);

        Assert.Equal(@"private void HandleClick() => Message = ""Clicked"";", result.Value.Content.ToString(), ignoreLineEndingDifferences: true);
        Assert.Contains(
            result.Value.ClassifiedSpans,
            c => c.ClassificationType == ClassificationTypeNames.MethodName &&
                result.Value.Content.GetSubText(c.TextSpan).ToString() == "HandleClick");
    }

    [Fact]
    public async Task TryExcerptAsync_SingleLine_CanClassifyCSharp_LegacyFunctionsProperty()
    {
        // Arrange
        var razorSource = """
            <h1>@Message</h1>

            @functions {
                private [|string|] Message { get; set; } = "Hello, World!";
            }
            """;

        var (_, secondary, secondarySpan) = await InitializeWithSnapshotAsync(razorSource, DisposalToken, RazorFileKind.Legacy);

        var service = new RazorSourceGeneratedDocumentExcerptService(RemoteServiceInvoker);

        // Act
        var options = ClassificationOptions.Default;
        var result = await service.TryExcerptAsync(secondary, secondarySpan, ExcerptMode.SingleLine, options, DisposalToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(secondarySpan, result.Value.Span);
        Assert.Same(secondary, result.Value.Document);

        Assert.Equal(
            (await secondary.GetTextAsync()).GetSubText(secondarySpan).ToString(),
            result.Value.Content.GetSubText(result.Value.MappedSpan).ToString(),
            ignoreLineEndingDifferences: true);

        Assert.Equal(@"private string Message { get; set; } = ""Hello, World!"";", result.Value.Content.ToString(), ignoreLineEndingDifferences: true);
        Assert.Contains(
            result.Value.ClassifiedSpans,
            c => c.ClassificationType == ClassificationTypeNames.Keyword &&
                result.Value.Content.GetSubText(c.TextSpan).ToString() == "string");
    }

    [Fact]
    public async Task TryExcerptAsync_SingleLine_CanClassifyCSharp_LegacyFunctionsMethod()
    {
        // Arrange
        var razorSource = """
            <button>@Message</button>

            @functions {
                private string Message { get; set; } = "Hello, World!";
                private void [|HandleClick|]() => Message = "Clicked";
            }
            """;

        var (_, secondary, secondarySpan) = await InitializeWithSnapshotAsync(razorSource, DisposalToken, RazorFileKind.Legacy);

        var service = new RazorSourceGeneratedDocumentExcerptService(RemoteServiceInvoker);

        // Act
        var options = ClassificationOptions.Default;
        var result = await service.TryExcerptAsync(secondary, secondarySpan, ExcerptMode.SingleLine, options, DisposalToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(secondarySpan, result.Value.Span);
        Assert.Same(secondary, result.Value.Document);

        Assert.Equal(
            (await secondary.GetTextAsync()).GetSubText(secondarySpan).ToString(),
            result.Value.Content.GetSubText(result.Value.MappedSpan).ToString(),
            ignoreLineEndingDifferences: true);

        Assert.Equal(@"private void HandleClick() => Message = ""Clicked"";", result.Value.Content.ToString(), ignoreLineEndingDifferences: true);
        Assert.Contains(
            result.Value.ClassifiedSpans,
            c => c.ClassificationType == ClassificationTypeNames.MethodName &&
                result.Value.Content.GetSubText(c.TextSpan).ToString() == "HandleClick");
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
        var options = ClassificationOptions.Default;
        var result = await service.TryExcerptAsync(secondary, secondarySpan, ExcerptMode.Tooltip, options, DisposalToken);

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
        var options = ClassificationOptions.Default;
        var result = await service.TryExcerptAsync(secondary, secondarySpan, ExcerptMode.SingleLine, options, DisposalToken);

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
        var options = ClassificationOptions.Default;
        var result = await service.TryExcerptAsync(secondary, secondarySpan, ExcerptMode.Tooltip, options, DisposalToken);

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
        var options = ClassificationOptions.Default;
        var result = await service.TryExcerptAsync(secondary, secondarySpan, ExcerptMode.Tooltip, options, DisposalToken);

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

    public static (SourceText sourceText, TextSpan span) CreateText(string text)
    {
        // Since we're using positions, normalize to Windows style
        text = text.Replace("\r", "").Replace("\n", "\r\n");

        TestFileMarkupParser.GetSpan(text, out text, out var span);
        return (SourceText.From(text), span);
    }
    internal async Task<(RemoteDocumentSnapshot primary, SourceGeneratedDocument generatedDocument, TextSpan generatedSpan)> InitializeWithSnapshotAsync(string razorSource, CancellationToken cancellationToken, RazorFileKind? fileKind = null)
    {
        var (razorSourceText, primarySpan) = CreateText(razorSource);
        var (primary, generatedDocument, declarationDocument) = await InitializeDocumentAsync(razorSourceText, primarySpan, fileKind, cancellationToken);
        var generatedSpan = await GetSecondarySpanAsync(primary, primarySpan, generatedDocument, declarationDocument, cancellationToken);
        return (primary, generatedDocument, generatedSpan);
    }

    // Adds the text to a ProjectSnapshot, generates code, and updates the workspace.
    private async Task<(RemoteDocumentSnapshot primary, SourceGeneratedDocument secondary, bool declarationDocument)> InitializeDocumentAsync(SourceText sourceText, TextSpan primarySpan, RazorFileKind? fileKind, CancellationToken cancellationToken)
    {
        var document = CreateProjectAndRazorDocument(sourceText.ToString(), fileKind);

        var snapshotManager = OOPExportProvider.GetExportedValue<RemoteSnapshotManager>();
        var documentMappingService = OOPExportProvider.GetExportedValue<IDocumentMappingService>();
        var snapshot = snapshotManager.GetSnapshot(document);
        var codeDocument = await snapshot.GetGeneratedOutputAsync(cancellationToken);
        Assert.True(documentMappingService.TryMapToCSharpDocumentLinePosition(codeDocument, primarySpan.Start, out _, out _, out var inDeclDocument));

        var generatedDocument = await snapshot.GetGeneratedDocumentAsync(inDeclDocument, cancellationToken);
        return (snapshot, generatedDocument, inDeclDocument);
    }

    // Maps a span in the primary buffer to the secondary buffer. This is only valid for C# code
    // that appears in the primary buffer.
    private static async Task<TextSpan> GetSecondarySpanAsync(RemoteDocumentSnapshot primary, TextSpan primarySpan, Document secondary, bool declarationDocument, CancellationToken cancellationToken)
    {
        var output = await primary.GetGeneratedOutputAsync(cancellationToken);

        foreach (var mapping in output.GetRequiredCSharpDocument(declarationDocument).SourceMappingsSortedByOriginal)
        {
            if (mapping.OriginalSpan.AbsoluteIndex <= primarySpan.Start &&
                (mapping.OriginalSpan.AbsoluteIndex + mapping.OriginalSpan.Length) >= primarySpan.End)
            {
                var offset = mapping.GeneratedSpan.AbsoluteIndex - mapping.OriginalSpan.AbsoluteIndex;
                var secondarySpan = new TextSpan(primarySpan.Start + offset, primarySpan.Length);
                Assert.Equal(
                    (await primary.GetTextAsync(cancellationToken)).ToString(primarySpan),
                    (await secondary.GetTextAsync(cancellationToken)).ToString(secondarySpan));
                return secondarySpan;
            }
        }

        throw new InvalidOperationException("Could not map the primary span to the generated code.");
    }
}
