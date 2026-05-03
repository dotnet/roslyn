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
using Microsoft.VisualStudio.Razor.LanguageClient.Cohost.Formatting;
using Xunit;
using Xunit.Abstractions;
using AssertEx = Roslyn.Test.Utilities.AssertEx;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Test.Cohost.Formatting;

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
