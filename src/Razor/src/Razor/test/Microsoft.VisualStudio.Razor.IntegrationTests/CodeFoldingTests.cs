// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.IntegrationTests;

public class CodeFoldingTests(ITestOutputHelper testOutputHelper) : AbstractRazorEditorTest(testOutputHelper)
{
    private struct CollapsibleBlock
    {
        public int Start { get; set; }
        public int End { get; set; }
    }

    private async Task AssertFoldableBlocksAsync(params string[] blockTexts)
    {
        var textView = await TestServices.Editor.GetActiveTextViewAsync(ControlledHangMitigatingCancellationToken);
        var text = textView.TextBuffer.CurrentSnapshot.GetText();

        var foldableSpans = blockTexts.Select(blockText =>
        {
            Assert.Contains(blockText, text);
            var start = text.IndexOf(blockText);
            return new Span(start, blockText.Length);
        }).ToImmutableArray();

        var foldableLines = foldableSpans.Select(s => ConvertToLineNumbers(s, textView)).ToImmutableArray();

        //
        // Built in retry logic because getting spans can take time.
        //
        var tries = 0;
        const int MaxTries = 10;
        const int Delay = 500;
        ImmutableArray<CollapsibleBlock> missingLines;
        var outlines = new ICollapsible[0];
        while (tries++ < MaxTries)
        {
            await TestServices.Editor.WaitForOutlineRegionsAsync(ControlledHangMitigatingCancellationToken);

            textView = await TestServices.Editor.GetActiveTextViewAsync(ControlledHangMitigatingCancellationToken);
            outlines = await TestServices.Editor.GetOutlineRegionsAsync(textView, ControlledHangMitigatingCancellationToken);

            (missingLines, var extraLines) = GetOutlineDiff(outlines, foldableSpans, textView);
            if (missingLines.Length == 0)
            {
                if (extraLines.Length > 0)
                {
                    var extraLineText = PrintLines(extraLines, textView);
                    var lineText = PrintLines(foldableLines, textView);

                    Assert.Fail($"Extra Lines: {extraLineText}Expected Lines: {lineText}");
                }

                return;
            }

            await Task.Delay(Delay);
        }

        if (missingLines.Length > 0)
        {
            var missingSpanText = PrintLines(missingLines, textView);
            var spans = outlines.Select(o => o.Extent.GetSpan(textView.TextSnapshot).Span).ToImmutableArray();
            var lines = spans.Select(s => ConvertToLineNumbers(s, textView)).ToImmutableArray();
            var linesText = PrintLines(lines, textView);

            Assert.Fail($"Missing Lines: {missingSpanText}Actual Lines: {linesText}");
        }

        Assert.All(outlines, o =>
        {
            Assert.Equal("...", o.CollapsedForm);
            Assert.True(o.IsCollapsible);
        });

        Assert.Empty(missingLines);

        static (ImmutableArray<CollapsibleBlock> missingSpans, ImmutableArray<CollapsibleBlock> extraSpans) GetOutlineDiff(ICollapsible[] outlines, ImmutableArray<Span> foldableSpans, ITextView textView)
        {
            var spans = outlines.Select(o => o.Extent.GetSpan(textView.TextSnapshot).Span).ToImmutableArray();
            var lines = spans.Select(s => ConvertToLineNumbers(s, textView));

            var foldableLines = foldableSpans.Select(s => ConvertToLineNumbers(s, textView));

            var missingSpans = foldableLines.Except(lines).ToImmutableArray();
            var extraSpans = lines.Except(foldableLines).ToImmutableArray();

            return (missingSpans, extraSpans);
        }

        static string PrintLines(ImmutableArray<CollapsibleBlock> lines, ITextView textView)
        {
            using var _ = StringBuilderPool.GetPooledObject(out var sb);

            foreach (var line in lines)
            {
                sb.AppendLine();

                var startLine = textView.TextSnapshot.GetLineFromLineNumber(line.Start);
                var endLine = textView.TextSnapshot.GetLineFromLineNumber(line.End);
                var span = Span.FromBounds(startLine.Start, endLine.End);
                var text = textView.TextSnapshot.GetText(span);

                sb.AppendLine(span.ToString());
                sb.AppendLine(text);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        static CollapsibleBlock ConvertToLineNumbers(Span span, ITextView textView)
        {
            return new CollapsibleBlock()
            {
                Start = textView.TextSnapshot.GetLineNumberFromPosition(span.Start),
                End = textView.TextSnapshot.GetLineNumberFromPosition(span.End)
            };
        }
    }

    [IdeFact]
    public async Task CodeFolding_CodeBlock()
    {
        await TestServices.SolutionExplorer.AddFileAsync(
            RazorProjectConstants.BlazorProjectName,
            "Test.razor",
            """

            @page "/Test"

            <PageTitle>Test</PageTitle>

            <h1>Test</h1>

            @code {
                private int currentCount = 0;

                private void IncrementCount()
                {
                    currentCount++;
                }
            }
            """,
            open: true,
            ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);

        TestServices.Input.Send("{ENTER}");

        await AssertFoldableBlocksAsync(
            """
            @code {
                private int currentCount = 0;

                private void IncrementCount()
                {
                    currentCount++;
                }
            }
            """,
            """
            private void IncrementCount()
                {
                    currentCount++;
                }
            """);
    }

    [IdeFact(Skip = "https://github.com/dotnet/razor/issues/10860")] // FUSE changes whitespace on folding ranges
    public async Task CodeFolding_IfBlock()
    {
        await TestServices.SolutionExplorer.AddFileAsync(
            RazorProjectConstants.BlazorProjectName,
            "Test.razor",
            """

            @page "/Test"

            <PageTitle>Test</PageTitle>

            <h1>Test</h1>

            @if(true)
            {
                if (true)
                {
                    M();
                }
            }

            @code {
                string M() => "M";
            }

            """,
            open: true,
            ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);

        TestServices.Input.Send("{ENTER}");

        await AssertFoldableBlocksAsync(
            """
            @if(true)
            {
                if (true)
                {
                    M();
                }
            }
            """,
            """
            if (true)
                {
                    M();
                }
            """,
            """
            @code {
                string M() => "M";
            }
            """);
    }

    [IdeFact]
    public async Task CodeFolding_ForEach()
    {
        await TestServices.SolutionExplorer.AddFileAsync(
            RazorProjectConstants.BlazorProjectName,
            "Test.razor",
            """

            @page "/Test"

            <PageTitle>Test</PageTitle>

            <h1>Test</h1>

            @foreach (var s in GetStuff())
            {
                <h2>s</h2>
            }

            @code {
                string[] GetStuff() => new string[0];
            }

            """,
            open: true,
            ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);

        TestServices.Input.Send("{ENTER}");

        await AssertFoldableBlocksAsync(
            """
            @foreach (var s in GetStuff())
            {
                <h2>s</h2>
            }

            """,
            """
            @code {
                string[] GetStuff() => new string[0];
            }
            """);
    }

    [IdeFact]
    public async Task CodeFolding_CodeBlock_Region()
    {
        await TestServices.SolutionExplorer.AddFileAsync(
            RazorProjectConstants.BlazorProjectName,
            "Test.razor",
            """

            @page "/Test"

            <PageTitle>Test</PageTitle>

            <h1>Test</h1>

            @code {
                #region Methods
                void M1() { }
                void M2() { }
                #endregion
            }

            """,
            open: true,
            ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);

        TestServices.Input.Send("{ENTER}");

        await AssertFoldableBlocksAsync(
            """
            #region Methods
                void M1() { }
                void M2() { }
                #endregion
            """,
            """
            @code {
                #region Methods
                void M1() { }
                void M2() { }
                #endregion
            }
            """);
    }

    [IdeFact]
    public async Task CodeFolding_Div()
    {
        await TestServices.SolutionExplorer.AddFileAsync(
            RazorProjectConstants.BlazorProjectName,
            "Test.razor",
            """
            @page "/Test"

            <PageTitle>Test</PageTitle>

            <div>
                <h1>Test</h1>
            </div>

            """,
            open: true,
            ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);

        TestServices.Input.Send("{ENTER}");

        await AssertFoldableBlocksAsync(
            """
            <div>
                <h1>Test</h1>
            </div>
            """);
    }
}
