// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.LanguageServices.CSharp.Interactive;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.InProcess;

[TestService]
internal partial class InteractiveWindowInProcess : ITextViewWindowInProcess
{
    private static readonly Func<string, string, bool> s_equals = (expected, actual) => actual.Equals(expected);
    private static readonly Func<string, string, bool> s_contains = (expected, actual) => actual.Contains(expected);
    private static readonly Func<string, string, bool> s_endsWith = (expected, actual) => actual.EndsWith(expected);

    private const string NewLineFollowedByReplSubmissionText = "\n. ";
    private const string ReplSubmissionText = ". ";
    private const string ReplPromptText = "> ";
    private const string NewLineFollowedByReplPromptText = "\n> ";

    TestServices ITextViewWindowInProcess.TestServices => TestServices;

    Task<IWpfTextView> ITextViewWindowInProcess.GetActiveTextViewAsync(CancellationToken cancellationToken)
        => GetActiveTextViewAsync(cancellationToken);

    async Task<ITextBuffer?> ITextViewWindowInProcess.GetBufferContainingCaretAsync(IWpfTextView view, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        return view.TextBuffer;
    }

    private async Task<IInteractiveWindow> GetInteractiveWindowAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var provider = await GetComponentModelServiceAsync<CSharpVsInteractiveWindowProvider>(cancellationToken);
        return provider.Open(instanceId: 0, focus: true).InteractiveWindow;
    }

    private async Task<IWpfTextView> GetActiveTextViewAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var window = await GetInteractiveWindowAsync(cancellationToken);
        return window.TextView;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        await ShowWindowAsync(waitForPrompt: false, cancellationToken);
        await CloseWindowAsync(cancellationToken);

        Assert.NotNull(await GetInteractiveWindowAsync(cancellationToken));
    }

    /// <summary>
    /// Gets the contents of the REPL window without the prompt text.
    /// </summary>
    public async Task<string> GetReplTextWithoutPromptAsync(CancellationToken cancellationToken)
    {
        var replText = await GetReplTextAsync(cancellationToken);

        // find last prompt and remove
        var lastPromptIndex = GetLastPromptIndex(replText);

        if (lastPromptIndex > 0)
        {
            replText = replText[..lastPromptIndex];
        }

        // it's possible for the editor text to contain a trailing newline, remove it
        return replText.EndsWith(Environment.NewLine)
            ? replText[..^Environment.NewLine.Length]
            : replText;
    }

    /// <summary>
    /// Gets the last output from the REPL.
    /// </summary>
    public async Task<string> GetLastReplOutputAsync(CancellationToken cancellationToken)
    {
        // TODO: This may be flaky if the last submission contains ReplPromptText
        var replText = await GetReplTextWithoutPromptAsync(cancellationToken);
        var lastPromptIndex = GetLastPromptIndex(replText);
        if (lastPromptIndex > 0)
            replText = replText[lastPromptIndex..];

        var lastSubmissionIndex = replText.LastIndexOf(NewLineFollowedByReplSubmissionText);

        if (lastSubmissionIndex > 0)
        {
            replText = replText[lastSubmissionIndex..];
        }
        else if (!replText.StartsWith(ReplPromptText))
        {
            return replText;
        }

        var firstNewLineIndex = replText.IndexOf(Environment.NewLine);

        if (firstNewLineIndex <= 0)
        {
            return replText;
        }

        firstNewLineIndex += Environment.NewLine.Length;
        return replText[firstNewLineIndex..];
    }

    /// <summary>
    /// Gets the last input from the REPL.
    /// </summary>
    public async Task<string> GetLastReplInputAsync(CancellationToken cancellationToken)
    {
        // TODO: This may be flaky if the last submission contains ReplPromptText or ReplSubmissionText

        var replText = await GetReplTextAsync(cancellationToken);
        var lastPromptIndex = GetLastPromptIndex(replText);
        replText = replText[(lastPromptIndex + ReplPromptText.Length)..];

        var lastSubmissionTextIndex = replText.LastIndexOf(NewLineFollowedByReplSubmissionText);

        int firstNewLineIndex;
        if (lastSubmissionTextIndex < 0)
        {
            firstNewLineIndex = replText.IndexOf(Environment.NewLine);
        }
        else
        {
            firstNewLineIndex = replText.IndexOf(Environment.NewLine, lastSubmissionTextIndex);
        }

        var lastReplInputWithReplSubmissionText = (firstNewLineIndex <= 0) ? replText : replText[..firstNewLineIndex];

        return lastReplInputWithReplSubmissionText.Replace(ReplSubmissionText, string.Empty);
    }

    public async Task<string> GetReplTextAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var view = await GetActiveTextViewAsync(cancellationToken);
        return view.TextBuffer.CurrentSnapshot.GetText();
    }

    public async Task ClearReplTextAsync(CancellationToken cancellationToken)
    {
        // Dismiss the pop-up (if any)
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.SelectionCancel, cancellationToken);

        // Clear the line
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.SelectionCancel, cancellationToken);
    }

    public async Task ShowWindowAsync(CancellationToken cancellationToken)
        => await ShowWindowAsync(waitForPrompt: true, cancellationToken);

    public async Task ShowWindowAsync(bool waitForPrompt, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        await TestServices.Shell.ExecuteCommandAsync<OpenInteractiveWindowCommand>(cancellationToken);

        if (waitForPrompt)
        {
            await WaitForReplPromptAsync(cancellationToken);
        }
    }

    public async Task WaitForReplPromptAsync(CancellationToken cancellationToken)
        => await WaitForPredicateAsync(GetReplTextAsync, ReplPromptText, s_endsWith, "end with", cancellationToken);

    public async Task ResetAsync(CancellationToken cancellationToken)
        => await ResetAsync(waitForPrompt: true, cancellationToken);

    public async Task ResetAsync(bool waitForPrompt, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var interactiveWindow = await GetInteractiveWindowAsync(cancellationToken);
        var operations = (IInteractiveWindowOperations)interactiveWindow;
        var result = await operations.ResetAsync().WithCancellation(cancellationToken);
        Contract.ThrowIfFalse(result.IsSuccessful);

        if (waitForPrompt)
        {
            await WaitForReplPromptAsync(cancellationToken);
        }
    }

    public async Task SubmitTextAsync(string text, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var interactiveWindow = await GetInteractiveWindowAsync(cancellationToken);
        await interactiveWindow.SubmitAsync([text]).WithCancellation(cancellationToken);
    }

    public async Task CloseWindowAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var shell = await GetRequiredGlobalServiceAsync<SVsUIShell, IVsUIShell>(cancellationToken);
        var windowId = CSharpVsInteractiveWindowPackage.Id;
        if (ErrorHandler.Succeeded(shell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fFrameOnly, windowId, out var windowFrame)))
        {
            ErrorHandler.ThrowOnFailure(windowFrame.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave));
        }
    }

    public async Task WaitForReplOutputAsync(string outputText, CancellationToken cancellationToken)
        => await WaitForPredicateAsync(GetReplTextAsync, outputText + Environment.NewLine + ReplPromptText, s_endsWith, "end with", cancellationToken);

    public async Task WaitForLastReplOutputAsync(string outputText, CancellationToken cancellationToken)
        => await WaitForPredicateAsync(GetLastReplOutputAsync, outputText, s_equals, "is", cancellationToken);

    public async Task WaitForLastReplOutputContainsAsync(string outputText, CancellationToken cancellationToken)
        => await WaitForPredicateAsync(GetLastReplOutputAsync, outputText, s_contains, "contain", cancellationToken);

    public async Task WaitForLastReplInputAsync(string outputText, CancellationToken cancellationToken)
        => await WaitForPredicateAsync(GetLastReplInputAsync, outputText, s_equals, "is", cancellationToken);

    public async Task WaitForLastReplInputContainsAsync(string outputText, CancellationToken cancellationToken)
        => await WaitForPredicateAsync(GetLastReplInputAsync, outputText, s_contains, "contain", cancellationToken);

    public async Task ClearScreenAsync(CancellationToken cancellationToken)
        => await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.InteractiveConsole.ClearScreen, cancellationToken);

    public async Task InsertCodeAsync(string text, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var interactiveWindow = await GetInteractiveWindowAsync(cancellationToken);
        interactiveWindow.InsertCode(text);
    }

    public async Task VerifyTagsAsync<TTag>(int expectedCount, CancellationToken cancellationToken)
        where TTag : ITag
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var view = await GetActiveTextViewAsync(cancellationToken);

        static bool filterTag(IMappingTagSpan<ITag> tag)
        {
            return tag.Tag.GetType().Equals(typeof(TTag));
        }

        var service = await GetComponentModelServiceAsync<IViewTagAggregatorFactoryService>(cancellationToken);
        var aggregator = service.CreateTagAggregator<ITag>(view);
        var allTags = aggregator.GetTags(new SnapshotSpan(view.TextSnapshot, 0, view.TextSnapshot.Length));
        var tags = allTags.Where(filterTag).Cast<IMappingTagSpan<ITag>>();
        var actualCount = tags.Count();

        if (expectedCount != actualCount)
        {
            var tagsTypesString = string.Join(",", allTags.Select(tag => tag.Tag.ToString()));
            throw new Exception($"Failed to verify '{typeof(TTag)}' tags. Expected count: {expectedCount}, Actual count: {actualCount}. All tags: {tagsTypesString}");
        }
    }

    private static async Task WaitForPredicateAsync(Func<CancellationToken, Task<string>> getValue, string expectedValue, Func<string, string, bool> valueComparer, string verb, CancellationToken cancellationToken)
    {
        var timer = SharedStopwatch.StartNew();

        while (true)
        {
            var actualValue = await getValue(cancellationToken);

            if (valueComparer(expectedValue, actualValue))
            {
                return;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                throw new Exception(
                    $"Unable to find expected content in REPL within {timer.Elapsed} milliseconds and no exceptions were thrown.{Environment.NewLine}" +
                    $"Buffer content is expected to {verb}: {Environment.NewLine}" +
                    $"[[{expectedValue}]]" +
                    $"Actual content:{Environment.NewLine}" +
                    $"[[{actualValue}]]");
            }

            await Task.Delay(50, cancellationToken);
        }
    }

    private static int GetLastPromptIndex(string replText)
    {
        return replText.LastIndexOf(NewLineFollowedByReplPromptText) + "\n".Length;
    }
}
