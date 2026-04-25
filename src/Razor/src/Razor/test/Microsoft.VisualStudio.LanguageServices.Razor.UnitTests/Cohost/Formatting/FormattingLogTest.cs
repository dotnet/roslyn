// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Razor.LanguageClient.Cohost.Formatting;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Test.Cohost.Formatting;

/// <summary>
/// Not tests of the formatting log, but tests that use formatting logs sent in
/// by users reporting issues.
/// </summary>
public class FormattingLogTest(ITestOutputHelper testOutput) : DocumentFormattingTestBase(testOutput)
{
    [Fact]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7264")]
    public async Task UnexpectedFalseInIndentBlockOperation()
        => Assert.NotNull(await GetFormattingEditsAsync());

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12416")]
    public async Task MixedIndentation()
        => Assert.NotNull(await GetFormattingEditsAsync());

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12416")]
    public async Task RealWorldMixedIndentation()
        => Assert.NotNull(await GetFormattingEditsAsync());

    [Fact]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/8333")]
    public async Task CSharpStringLiteral()
        => Assert.Null(await GetFormattingEditsAsync()); // All edits should have been filtered out

    [Fact]
    [WorkItem("https://github.com/microsoft/vscode-dotnettools/issues/2766")]
    public async Task RanOutOfOriginalLines()
        => Assert.NotNull(await GetFormattingEditsAsync());

    [Fact]
    [WorkItem("https://developercommunity.visualstudio.com/t/Whilst-using-format-document-on-a-razo/11041051#T-N11042031-N11049221")]
    public async Task CSSWrappedToMultipleLines()
        => Assert.NotNull(await GetFormattingEditsAsync());

    [Fact]
    [WorkItem("https://developercommunity.visualstudio.com/t/Razor-Formatting-Feature-internal-error/11041869#T-ND11043454")]
    public async Task MultiLineLambda()
        => Assert.NotNull(await GetFormattingEditsAsync());

    [Fact]
    [WorkItem("https://developercommunity.visualstudio.com/t/Razor-Formatting-Feature---Internal-Erro/11068847")]
    public async Task GameTracAdmin()
        => Assert.NotNull(await GetFormattingEditsAsync());

    private async Task<TextEdit[]?> GetFormattingEditsAsync([CallerMemberName] string? testName = null)
    {
        var contents = GetResource(testName.AssumeNotNull(), "InitialDocument.txt").AssumeNotNull();
        var document = CreateProjectAndRazorDocument(contents, fileKind: GetFileKind(testName));
        var sourceText = await document.GetTextAsync();

        var options = new RazorFormattingOptions() with
        {
            CSharpSyntaxFormattingOptions = CodeAnalysis.ExternalAccess.Razor.Features.RazorCSharpSyntaxFormattingOptions.Default
        };
        if (GetResource(testName, "Options.json") is { } optionsFile)
        {
            options = (RazorFormattingOptions)JsonSerializer.Deserialize(optionsFile, typeof(RazorFormattingOptions), JsonHelpers.JsonSerializerOptions).AssumeNotNull();
        }

        TextEdit[] htmlEdits = [];
        if (GetResource(testName, "HtmlChanges.json") is { } htmlChangesFile)
        {
            var htmlChanges = JsonSerializer.Deserialize<RazorTextChange[]>(htmlChangesFile, JsonHelpers.JsonSerializerOptions);
            htmlEdits = htmlChanges.Select(c => sourceText.GetTextEdit(c.ToTextChange())).ToArray();
        }

        TextSpan span = default;
        if (GetResource(testName, "Range.json") is { } rangeFile && rangeFile != "null")
        {
            var linePositionSpan = (LinePositionSpan)JsonSerializer.Deserialize(rangeFile, typeof(LinePositionSpan), JsonHelpers.JsonSerializerOptions).AssumeNotNull();
            span = sourceText.GetTextSpan(linePositionSpan);
        }

        var formattingService = (RazorFormattingService)OOPExportProvider.GetExportedValue<IRazorFormattingService>();
        formattingService.GetTestAccessor().SetFormattingLoggerFactory(new TestFormattingLoggerFactory(TestOutputHelper));

        var edits = await GetFormattingEditsAsync(document, htmlEdits, span, options.CodeBlockBraceOnNextLine, options.AttributeIndentStyle, options.InsertSpaces, options.TabSize, options.CSharpSyntaxFormattingOptions.AssumeNotNull());

        // If we have a FinalFormattedDocument from the user, then we want this test to fail until the bug is fixed, and the output changes
        if (edits is not null && GetResource(testName, "FinalFormattedDocument.txt") is { } finalFormattedDocumentFile)
        {
            var finalFormattedDocument = finalFormattedDocumentFile.AssumeNotNull();
            var formattedText = sourceText.WithChanges(edits.Select(sourceText.GetTextChange));

            Assert.False(formattedText.ToString().Equals(finalFormattedDocument), "Formatted document should not match the expected final document, otherwise the bug has not been fixed. If this isn't true for this scenario, delete the FinalFormattedDocument test file.");
        }

        return edits;
    }

    private RazorFileKind? GetFileKind(string testName)
    {
        if (GetResource(testName, "FileKind.json") is { } fileKindFile)
        {
            return (RazorFileKind)JsonSerializer.Deserialize(fileKindFile, typeof(RazorFileKind), JsonHelpers.JsonSerializerOptions).AssumeNotNull();
        }

        // If we didn't get a file kind, see if we can get it from source mappings
        if (GetResource(testName, "SourceMappings.json") is { } sourceMappings)
        {
            using var document = JsonDocument.Parse(sourceMappings);
            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var mapping in document.RootElement.EnumerateArray())
                {
                    if (mapping.TryGetProperty("OriginalSpan", out var originalSpan) &&
                        originalSpan.TryGetProperty("FilePath", out var filePathProperty) &&
                        filePathProperty.GetString() is { Length: > 0 } filePath)
                    {
                        return FileKinds.GetFileKindFromPath(filePath);
                    }
                }
            }
        }

        // Last resort fallback, try getting the filetype out of the log messages
        if (GetResource(testName, "Messages.txt") is { } messages)
        {
            const string marker = " formatting for ";
            var index = messages.IndexOf(marker, StringComparison.Ordinal);
            if (index >= 0)
            {
                var start = index + marker.Length;
                var end = messages.IndexOfAny(['\r', '\n'], start);
                var filePath = end >= 0 ? messages[start..end] : messages[start..];
                return FileKinds.GetFileKindFromPath(filePath);
            }
        }

        return null;
    }

    private string? GetResource(string testName, string name)
    {
        var baselineFileName = $@"TestFiles\FormattingLog\{testName}\{name}";

        var testFile = TestFile.Create(baselineFileName, GetType().Assembly);
        if (!testFile.Exists())
        {
            return null;
        }

        // Formatting logs capture absolute spans against the original file contents, so we must not normalize line endings.
        return testFile.ReadAllText(normalizeLineEndings: false);
    }
}
