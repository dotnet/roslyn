// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Extensibility.Testing;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.IntegrationTests;

public class CompletionIntegrationTests(ITestOutputHelper testOutputHelper) : AbstractRazorEditorTest(testOutputHelper)
{
    private static readonly TimeSpan s_snippetTimeout = TimeSpan.FromSeconds(10);

    [IdeFact(Skip = "We're returning the right completion item, but the editor isn't applying it?")]
    public async Task SnippetCompletion_Html()
    {
        await VerifyTypeAndCommitCompletionAsync(
            input: """
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
            output: """
                @page "/Test"

                <PageTitle>Test</PageTitle>

                <h1>Test</h1>
                <dl>
                    <dt></dt>
                    <dd></dd>
                </dl>

                @code {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            search: "<h1>Test</h1>",
            expectedSelectedItemLabel: "dd",
            stringsToType: ["{ENTER}", "d", "d"]);
    }

    [IdeFact, WorkItem("https://github.com/dotnet/razor/issues/10787")]
    public async Task CompletionCommit_HtmlAttributeWithoutValue()
    {
        await VerifyTypeAndCommitCompletionAsync(
            input: """
                @page "/Test"

                <PageTitle>Test</PageTitle>

                <button></button>

                @code {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            output: """
                @page "/Test"

                <PageTitle>Test</PageTitle>

                <button disabled></button>

                @code {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            search: "<button",
            stringsToType: [" ", "d", "i", "s"],
            expectedSelectedItemLabel: "disabled");
    }

    [IdeFact]
    public async Task CompletionCommit_HtmlAttributeWithValue()
    {
        await VerifyTypeAndCommitCompletionAsync(
            input: """
                @page "/Test"

                <PageTitle>Test</PageTitle>

                <button></button>

                @code {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            output: """
                @page "/Test"

                <PageTitle>Test</PageTitle>

                <button style=""></button>

                @code {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            search: "<button",
            stringsToType: [" ", "s", "t", "y"],
            expectedSelectedItemLabel: "style");
    }

    [IdeFact]
    public async Task CompletionCommit_BlazorDirectiveAttribute()
    {
        await VerifyTypeAndCommitCompletionAsync(
            input: """
                @page "/test"

                <PageTitle>Test</PageTitle>

                <select @=""></select>
                """,
            output: """
                @page "/test"
                
                <PageTitle>Test</PageTitle>

                <select @onactivate =""></select>
                """,
            search: "<select @",
            stringsToType: ["o", "n", "a", "c"],
            commitChar: '\t',
            expectedSelectedItemLabel: "@onactivate");
    }

    // Regression test for https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2505611
    [IdeFact]
    public async Task CompletionCommit_NoCommitOnTypingInDocComments()
    {
        await TestServices.SolutionExplorer.AddFileAsync(
            RazorProjectConstants.BlazorProjectName,
            "Test.razor",
            """
            @page "/test"
            
            <PageTitle>Test</PageTitle>
            
            @{
                /// 
            }
            """,
            open: true,
            ControlledHangMitigatingCancellationToken);

        var textView = await TestServices.Editor.GetActiveTextViewAsync(HangMitigatingCancellationToken);
        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.DismissCompletionSessionsAsync(ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("/// ", charsOffset: 1, ControlledHangMitigatingCancellationToken);
        TestServices.Input.Send("add a function");

        // Make sure extra text didn't get commited from an unexpected completion list
        var currentLineText = await TestServices.Editor.GetCurrentLineTextAsync(HangMitigatingCancellationToken);
        Assert.Contains("/// add a function", currentLineText);

        // Make sure completion doesn't come up for 15 seconds
        var completionSession = await TestServices.Editor.WaitForExistingCompletionSessionAsync(s_snippetTimeout, HangMitigatingCancellationToken);
        var items = completionSession?.GetComputedItems(HangMitigatingCancellationToken);

        if (items is null)
        {
            // No items to check, we're good
            return;
        }

        // If completion did pop up with something like "Processing", make sure no doccomment items are present
        Assert.DoesNotContain("summary", items.Items.Select(i => i.DisplayText));

    }

    [IdeFact]
    public async Task CompletionCommit_HtmlTag()
    {
        await VerifyTypeAndCommitCompletionAsync(
            input: """
                @page "/Test"

                <PageTitle>Test</PageTitle>

                @code {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            output: """
                @page "/Test"

                <PageTitle>Test</PageTitle>

                <span

                @code {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            search: "</PageTitle>",
            stringsToType: ["{ENTER}", "{ENTER}", "<", "s", "p", "a"],
            expectedSelectedItemLabel: "span");
    }

    [IdeFact]
    public async Task CompletionCommit_WithAngleBracket_HtmlTag()
    {
        await VerifyTypeAndCommitCompletionAsync(
            input: """
                @page "/Test"

                <PageTitle>Test</PageTitle>

                @code {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            output: """
                @page "/Test"

                <PageTitle>Test</PageTitle>

                <span></span>

                @code {
                    private int currentCount = 0;

                    private void IncrementCount()
                    {
                        currentCount++;
                    }
                }
                """,
            search: "</PageTitle>",
            stringsToType: ["{ENTER}", "{ENTER}", "<", "s", "p", "a"],
            commitChar: '>',
            "span");
    }

    [IdeFact]
    public async Task CompletionCommit_CSharp()
    {
        await VerifyTypeAndCommitCompletionAsync(
            input: """
                @page "/Test"

                <PageTitle>Test</PageTitle>

                @code {
                    private int myCurrentCount = 0;

                    private void IncrementCount()
                    {
                        myCurrentCount++;
                    }
                }
                """,
            output: """
                @page "/Test"

                <PageTitle>Test</PageTitle>

                @code {
                    private int myCurrentCount = 0;

                    private void IncrementCount()
                    {
                        myCurrentCount++;

                        myCurrentCount
                    }
                }
                """,
            search: "myCurrentCount++;",
            stringsToType: ["{ENTER}", "{ENTER}", "m", "y", "C", "u", "r"],
            expectedSelectedItemLabel: "myCurrentCount");
    }

    [IdeFact]
    public async Task CompletionCommit_CSharp_Override()
    {
        await VerifyTypeAndCommitCompletionAsync(
            input: """
                @page "/Test"

                <PageTitle>Test</PageTitle>

                @code {
                    private int myCurrentCount = 0;

                    override
                }
                """,
            output: """
                @page "/Test"

                <PageTitle>Test</PageTitle>

                @code {
                    private int myCurrentCount = 0;

                    protected override void OnAfterRender(bool firstRender)
                    {
                        base.OnAfterRender(firstRender);
                    }
                }
                """,
            search: "override",
            stringsToType: [" ", "O", "n", "A"],
            commitChar: '\t');
    }

    [IdeFact]
    public async Task SnippetCompletion_DoesntCommitOnSpace()
    {
        await TestServices.SolutionExplorer.AddFileAsync(
            RazorProjectConstants.BlazorProjectName,
            "Test.razor",
            """
            @page "/Test"

            <PageTitle>Test</PageTitle>

            <div></div>
            """,
            open: true,
            ControlledHangMitigatingCancellationToken);

        var textView = await TestServices.Editor.GetActiveTextViewAsync(HangMitigatingCancellationToken);
        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("<div></div>", charsOffset: 1, ControlledHangMitigatingCancellationToken);
        TestServices.Input.Send("{ENTER}");
        TestServices.Input.Send("i");
        TestServices.Input.Send("f");
        TestServices.Input.Send("r");

        // Wait until completion comes up before validating
        // that space does not commit
        await TestServices.Editor.WaitForCompletionSessionAsync(HangMitigatingCancellationToken);

        TestServices.Input.Send(" ");

        var text = textView.TextBuffer.CurrentSnapshot.GetText();

        var expected = """
            @page "/Test"
            
            <PageTitle>Test</PageTitle>
            
            <div></div>
            ifr
            """;

        AssertEx.EqualOrDiff(expected, text);
    }

    [IdeFact]
    [WorkItem("https://github.com/dotnet/razor/issues/9427")]
    public async Task Snippets_DoNotTrigger_OnDelete()
    {
        await TestServices.SolutionExplorer.AddFileAsync(
            RazorProjectConstants.BlazorProjectName,
            "Test.razor",
            """
            @page "/Test"

            <PageTitle>Test</PageTitle>

            <div>Hello</div>
            """,
            open: true,
            ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.DismissCompletionSessionsAsync(ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("Hel", charsOffset: 1, ControlledHangMitigatingCancellationToken);
        TestServices.Input.Send("{DELETE}");

        // Make sure completion doesn't come up for 15 seconds
        var completionSession = await TestServices.Editor.WaitForExistingCompletionSessionAsync(s_snippetTimeout, HangMitigatingCancellationToken);
        Assert.Null(completionSession);
    }

    [IdeTheory]
    [InlineData("<PageTitle")]
    [InlineData("</PageTitle")]
    [InlineData("<div")]
    [InlineData("</div")]
    [InlineData("// script block ")]
    [InlineData("/* style block ")]
    [InlineData("<!-- comment block ")]
    [WorkItem("https://github.com/dotnet/razor/issues/9427")]
    // Do not trigger snippets in start tags, end tags, script blocks, style blocks, or comments
    public async Task Snippets_DoNotTrigger_InDisallowedContext(string tag)
    {
        await TestServices.SolutionExplorer.AddFileAsync(
            RazorProjectConstants.BlazorProjectName,
            "Test.razor",
            """
            @page "/Test"

            <script>
                // script block 
            </script>

            <style>
                /* style block  */
            </style>

            <!-- comment block  -->

            <PageTitle>Test</PageTitle>

            <div></div>
            """,
            open: true,
            ControlledHangMitigatingCancellationToken);

        var textView = await TestServices.Editor.GetActiveTextViewAsync(HangMitigatingCancellationToken);
        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.DismissCompletionSessionsAsync(ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync(tag, charsOffset: 1, ControlledHangMitigatingCancellationToken);
        TestServices.Input.Send(" ");
        TestServices.Input.Send("dd");

        // Make sure completion doesn't come up for 15 seconds
        var completionSession = await TestServices.Editor.WaitForExistingCompletionSessionAsync(s_snippetTimeout, HangMitigatingCancellationToken);
        var items = completionSession?.GetComputedItems(HangMitigatingCancellationToken);

        if (items is null)
        {
            // No items to check, we're good
            return;
        }

        Assert.DoesNotContain("dd", items.Items.Select(i => i.DisplayText));
    }

    [IdeFact, WorkItem("https://github.com/dotnet/razor/issues/9346")]
    public async Task Completion_EnumDot()
    {
        await TestServices.SolutionExplorer.AddFileAsync(
            RazorProjectConstants.BlazorProjectName,
            "Test.razor",
            """
            <Test Param="@MyEnum." />

            @code {
                [Parameter] public string Param { get; set; }

                public enum MyEnum
                {
                    One
                }
            }
            """,
            open: true,
            ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("@MyEnum.", charsOffset: 1, ControlledHangMitigatingCancellationToken);

        await Task.Delay(500, HangMitigatingCancellationToken);

        TestServices.Input.Send("O");

        await CommitCompletionAndVerifyAsync("""
            <Test Param="@MyEnum.One" />
            
            @code {
                [Parameter] public string Param { get; set; }
            
                public enum MyEnum
                {
                    One
                }
            }
            """);
    }

    [IdeFact, WorkItem("https://github.com/dotnet/razor/issues/11385")]
    public async Task ProvisionalCompletion_DoesntBreakSemanticTokens()
    {
        await TestServices.SolutionExplorer.AddFileAsync(
            RazorProjectConstants.BlazorProjectName,
            "Test.razor",
            """
            @page "/counter"

            <PageTitle>Counter</PageTitle>

            <h1>Counter</h1>

            <p role="status">Current count: @currentCount</p>

            @DateTime

            <button class="btn btn-primary" @onclick="IncrementCount">Click me</button>

            @code {
                private int currentCount = 0;

                public void IncrementCount()
                {
                    currentCount++;
                }
            }
            """,
            open: true,
            ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("@DateTime", charsOffset: 1, ControlledHangMitigatingCancellationToken);

        await Task.Delay(500, HangMitigatingCancellationToken);

        TestServices.Input.Send(".");
        TestServices.Input.Send("n");

        await Task.Delay(500, HangMitigatingCancellationToken);

        await TestServices.Editor.ValidateNoDiscoColorsAsync(HangMitigatingCancellationToken);
    }

    [IdeFact]
    [WorkItem("https://github.com/dotnet/razor/issues/11565")]
    public async Task TagHelpers_Present_OnBackspace()
    {
        await TestServices.SolutionExplorer.AddFileAsync(
            RazorProjectConstants.BlazorProjectName,
            "Test.razor",
            """
            <PageTitle>Test</PageTitle>
            """,
            open: true,
            ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("<PageTitle", charsOffset: 1, ControlledHangMitigatingCancellationToken);
        TestServices.Input.Send("{BACKSPACE}");

        var completionSession = await TestServices.Editor.WaitForCompletionSessionAsync(s_snippetTimeout, HangMitigatingCancellationToken);
        var items = completionSession?.GetComputedItems(HangMitigatingCancellationToken);

        Assert.NotNull(items);
        Assert.Contains("PageTitle", items.Items.Select(i => i.DisplayText));
    }

    private async Task VerifyTypeAndCommitCompletionAsync(string input, string output, string search, string[] stringsToType, char? commitChar = null, string? expectedSelectedItemLabel = null)
    {
        const string CompletionTestFileName = "Completion.razor";

        await TestServices.SolutionExplorer.AddFileAsync(
            RazorProjectConstants.BlazorProjectName,
            CompletionTestFileName,
            input,
            open: true,
            ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);

        var filePath = await TestServices.SolutionExplorer.GetAbsolutePathForProjectRelativeFilePathAsync(RazorProjectConstants.BlazorProjectName, CompletionTestFileName, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync(search, charsOffset: 1, ControlledHangMitigatingCancellationToken);
        foreach (var stringToType in stringsToType)
        {
            await TestServices.RazorProjectSystem.WaitForHtmlVirtualDocumentUpdateAsync(RazorProjectConstants.BlazorProjectName, filePath, () =>
            {
                TestServices.Input.Send(stringToType);

                return Task.CompletedTask;
            }, ControlledHangMitigatingCancellationToken);
        }

        if (expectedSelectedItemLabel is not null)
        {
            await CommitCompletionAndVerifyAsync(output, expectedSelectedItemLabel, commitChar);
        }
        else
        {
            await CommitCompletionAndVerifyAsync(output, commitChar);
        }
    }

    private async Task CommitCompletionAndVerifyAsync(string expected, char? commitChar = null)
    {
        var session = await TestServices.Editor.WaitForCompletionSessionAsync(HangMitigatingCancellationToken);

        if (session is null)
        {
            Assert.Fail(await TestServices.Editor.GetCompletionSessionDebugInfoAsync(expectedSelectedItemLabel: null, HangMitigatingCancellationToken));
        }

        var completionSession = session ?? throw new InvalidOperationException("Completion session should have been available.");
        if (commitChar.HasValue)
        {
            // Commit using the specified commit character
            completionSession.Commit(commitChar.Value, HangMitigatingCancellationToken);

            // session.Commit call above commits as if the commit character was typed,
            // but doesn't actually insert the character into the buffer.
            // So we still need to insert the character into the buffer ourselves.
            TestServices.Input.Send(commitChar.Value.ToString());
        }
        else
        {
            Assert.True(completionSession.CommitIfUnique(HangMitigatingCancellationToken));
        }

        var textView = await TestServices.Editor.GetActiveTextViewAsync(HangMitigatingCancellationToken);
        var text = textView.TextBuffer.CurrentSnapshot.GetText();

        // Snippets may have slight whitespace differences due to line endings. These
        // tests allow for it as long as the content is correct
        AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, text);
    }

    private async Task CommitCompletionAndVerifyAsync(string expected, string expectedSelectedItemLabel, char? commitChar = null)
    {
        // Let outstanding async Intellisense work settle before we interrogate the active completion session.
        await TestServices.Shell.WaitForOperationProgressAsync(HangMitigatingCancellationToken);

        // Actually open completion UI and wait for it have selected item we are interested in
        var session = await TestServices.Editor.OpenCompletionSessionAndWaitForItemAsync(TimeSpan.FromSeconds(10), expectedSelectedItemLabel, HangMitigatingCancellationToken);

        if (session is null)
        {
            Assert.Fail(await TestServices.Editor.GetCompletionSessionDebugInfoAsync(expectedSelectedItemLabel, HangMitigatingCancellationToken));
        }

        var completionSession = session ?? throw new InvalidOperationException("Completion session should have been available.");
        if (commitChar is char commitCharValue)
        {
            // Commit using the specified commit character
            completionSession.Commit(commitCharValue, HangMitigatingCancellationToken);

            // session.Commit call above commits as if the commit character was typed,
            // but doesn't actually insert the character into the buffer.
            // So we still need to insert the character into the buffer ourselves.
            TestServices.Input.Send(commitCharValue.ToString());
        }
        else
        {
            Assert.True(completionSession.CommitIfUnique(HangMitigatingCancellationToken));
        }

        var textView = await TestServices.Editor.GetActiveTextViewAsync(HangMitigatingCancellationToken);

        var stopwatch = Stopwatch.StartNew();
        string text;
        while ((text = textView.TextBuffer.CurrentSnapshot.GetText()) != expected && stopwatch.ElapsedMilliseconds < EditorInProcess.DefaultCompletionWaitTimeMilliseconds)
        {
            // Text might get updated *after* completion by something like auto-insert, so wait for the desired text
            await Task.Delay(100, HangMitigatingCancellationToken);
        }

        // Snippets may have slight whitespace differences due to line endings. These
        // tests allow for it as long as the content is correct
        Assert.Equal(expected, text);
    }
}
