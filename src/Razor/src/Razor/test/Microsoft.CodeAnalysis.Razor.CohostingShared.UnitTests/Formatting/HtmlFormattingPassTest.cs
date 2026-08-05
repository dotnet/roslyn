// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;
using AssertEx = Roslyn.Test.Utilities.AssertEx;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.Formatting;

public class HtmlFormattingPassTest(ITestOutputHelper testOutput) : DocumentFormattingTestBase(testOutput)
{
    public static TheoryData<string, string> StringLiteralSplitTestData => new()
    {
        { "", "" },
        { "$", "" },
        { "", "u8" },
        { "$", "u8" },
        { "@", "" },
        { "@$", "" },
        { @"""""""", @"""""""""" },
        { @"$""""""", @"""""""""" },
        { @"""""""\r\n", @"\r\n""""""" },
        { @"$""""""\r\n", @"\r\n""""""" },
        { @"""""""", @"""""""u8" },
        { @"$""""""", @"""""""u8" },
        { @"""""""\r\n", @"\r\n""""""u8" },
        { @"$""""""\r\n", @"\r\n""""""u8" },
    };

    [Theory]
    [WorkItem("https://github.com/dotnet/razor/issues/11846")]
    [MemberData(nameof(StringLiteralSplitTestData))]
    public async Task RemoveEditThatSplitsStringLiteral(string prefix, string suffix)
    {
        TestCode input = $"""
            @({prefix}"this is a line that i$$s 46 characters long"{suffix})
            """;
        var document = CreateProjectAndRazorDocument(input.Text);
        var change = new TextChange(new TextSpan(input.Position, 0), "\r\n");
        var edits = await GetHtmlFormattingEditsAsync(document, change);
        Assert.Empty(edits);
    }

    [Theory]
    [WorkItem("https://github.com/dotnet/razor/issues/11846")]
    [MemberData(nameof(StringLiteralSplitTestData))]
    public async Task RemoveEditThatSplitsStringLiteral_MultiLineDocument(string prefix, string suffix)
    {
        TestCode input = $"""
            <div>

                @({prefix}"this is a line that i$$s 46 characters long"{suffix})

            </div>
            """;
        var document = CreateProjectAndRazorDocument(input.Text);
        var change = new TextChange(new TextSpan(input.Position, 0), "\r\n");
        var edits = await GetHtmlFormattingEditsAsync(document, change);
        Assert.Empty(edits);
    }

    [Fact]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/3040290")]
    public async Task KeepEditWithEquivalentNonWhitespaceContent()
    {
        TestCode input = """
            <script>
                [|var x=2;|]
            </script>
            """;
        var document = CreateProjectAndRazorDocument(input.Text);
        var sourceText = SourceText.From(input.Text);
        var change = new TextChange(input.Span, "var x = 2;");

        var edits = await GetHtmlFormattingEditsAsync(document, change);

        AssertEx.EqualOrDiff(
            sourceText.WithChanges(change).ToString(),
            sourceText.WithChanges(edits).ToString());
    }

    [Fact]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/3040290")]
    public async Task KeepMultipleWhitespaceEditsWithLengthChanges()
    {
        TestCode input = """
            <script>
                var first=1;
                var second  = 2;
                var third=3;
            </script>
            """;
        var document = CreateProjectAndRazorDocument(input.Text);
        var sourceText = SourceText.From(input.Text);
        ImmutableArray<TextChange> changes =
        [
            new(sourceText.Lines[1].Span, "    var first = 1;"),
            new(sourceText.Lines[2].Span, "    var second=2;"),
            new(sourceText.Lines[3].Span, "    var third = 3;"),
        ];

        var edits = await GetHtmlFormattingEditsAsync(document, changes);

        AssertEx.EqualOrDiff(
            sourceText.WithChanges(changes).ToString(),
            sourceText.WithChanges(edits).ToString());
    }

    [Theory]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/3040290")]
    [InlineData("AAAA", "BBBBBBBB")]
    [InlineData("BBBBBBBB", "AAAA")]
    public async Task KeepWhitespaceOnlyEditsAroundFilteredLengthChangingEdit(string original, string replacement)
    {
        TestCode input = $$"""
            <script>
                var first=1;
                {{original}}
                var third  =  3;
            </script>
            """;
        var document = CreateProjectAndRazorDocument(input.Text);
        var sourceText = SourceText.From(input.Text);

        var firstLine = sourceText.Lines[1];
        var firstEquals = firstLine.Start + sourceText.ToString(firstLine.Span).IndexOf('=');
        var insertSpaceBeforeFirstEquals = new TextChange(new(firstEquals, 0), " ");
        var insertSpaceAfterFirstEquals = new TextChange(new(firstEquals + 1, 0), " ");

        var unsafeChange = new TextChange(sourceText.Lines[2].Span, $"    {replacement}");

        var thirdLine = sourceText.Lines[3];
        var thirdEquals = thirdLine.Start + sourceText.ToString(thirdLine.Span).IndexOf('=');
        var removeSpaceBeforeThirdEquals = new TextChange(new(thirdEquals - 1, 1), "");
        var removeSpaceAfterThirdEquals = new TextChange(new(thirdEquals + 1, 1), "");

        var edits = await GetHtmlFormattingEditsAsync(
            document,
            insertSpaceBeforeFirstEquals,
            insertSpaceAfterFirstEquals,
            unsafeChange,
            removeSpaceBeforeThirdEquals,
            removeSpaceAfterThirdEquals);

        AssertEx.EqualOrDiff(
            $$"""
            <script>
                var first = 1;
                {{original}}
                var third = 3;
            </script>
            """,
            sourceText.WithChanges(edits).ToString());
    }

    [Fact]
    public async Task FilterOutHtmlEdits()
    {
        TestCode input = """
            <div>
            </div>
            <div>
                <span>
                    Test
                </span>
            </div>
            <script>
            $$   script1
            </script>
            <div>
                <script>
            $$        script2
                </script>
            </div>
            <style>
            $$     style1
            </style>
            <div>
                <style>
            $$        style2
                </style>
            </div>
            <script>hello</script>
            <div><script>hello</script></div>
            <script>
            $$hello</script>
            <div><script>
            $$hello</script></div>
            <script>
            </script>
            @{
                var x = @<div>
                    <script>
            $$            function foo() { }
                    </script>
                </div>;
            }
            
            """;

        var document = CreateProjectAndRazorDocument(input.Text);
        var sourceText = SourceText.From(input.Text);
        var changes = ImmutableArray.CreateBuilder<TextChange>();

        // Create an edit to indent every line. The actual size doesn't matter for this test.
        var indent = "      ";
        foreach (var line in sourceText.Lines)
        {
            changes.Add(new TextChange(new TextSpan(line.Start, 0), indent));
        }

        var edits = await GetHtmlFormattingEditsAsync(document, changes.ToImmutable());

        var newDoc = sourceText.WithChanges(edits);
        // The only places the indent should have been kept is places that we marked with dollar signs
        AssertEx.EqualOrDiff(input.OriginalInput.Replace("$$", indent), newDoc.ToString());
    }

    private async Task<ImmutableArray<TextChange>> GetHtmlFormattingEditsAsync(CodeAnalysis.TextDocument document, params ImmutableArray<TextChange> changes)
    {
        var documentMappingService = OOPExportProvider.GetExportedValue<IDocumentMappingService>();
        var pass = new HtmlFormattingPass(documentMappingService, LoggerFactory);

        var snapshotManager = OOPExportProvider.GetExportedValue<RemoteSnapshotManager>();
        var snapshot = snapshotManager.GetSnapshot(document);

        var loggerFactory = new TestFormattingLoggerFactory(TestOutputHelper);
        var logger = loggerFactory.CreateLogger(document.FilePath.AssumeNotNull(), "Html");
        var codeDocument = await snapshot.GetGeneratedOutputAsync(DisposalToken);
        var context = FormattingContext.Create(snapshot,
            codeDocument,
            new RazorFormattingOptions(),
            logger);

        var edits = await pass.GetTestAccessor().FilterIncomingChangesAsync(context, changes, DisposalToken);
        return edits;
    }
}
