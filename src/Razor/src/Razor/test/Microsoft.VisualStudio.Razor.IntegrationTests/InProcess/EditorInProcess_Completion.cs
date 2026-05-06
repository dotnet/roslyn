// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;

namespace Microsoft.VisualStudio.Extensibility.Testing;

internal partial class EditorInProcess
{
    public const int DefaultCompletionWaitTimeMilliseconds = 10000;

    public async Task DismissCompletionSessionsAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var view = await GetActiveTextViewAsync(cancellationToken);

        var asyncBroker = await GetComponentModelServiceAsync<IAsyncCompletionBroker>(cancellationToken);
        var session = asyncBroker.GetSession(view);
        if (session is not null && !session.IsDismissed)
        {
            session.Dismiss();
        }
    }

    public Task<IAsyncCompletionSession?> WaitForCompletionSessionAsync(CancellationToken cancellationToken)
    {
        return WaitForCompletionSessionAsync(TimeSpan.FromSeconds(10), cancellationToken);
    }

    public async Task<IAsyncCompletionSession?> WaitForCompletionSessionAsync(TimeSpan timeOut, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var textView = await GetActiveTextViewAsync(cancellationToken);

        var stopWatch = Stopwatch.StartNew();
        var asyncCompletion = await TestServices.Shell.GetComponentModelServiceAsync<IAsyncCompletionBroker>(cancellationToken);
        var lastTriggerTime = 0L;

        var session = asyncCompletion.GetSession(textView);
        if (session is null || session.IsDismissed)
        {
            session = TriggerCompletion();
        }

        // Loop until completion comes up
        while (session is null || session.IsDismissed)
        {
            if (stopWatch.ElapsedMilliseconds >= timeOut.TotalMilliseconds)
            {
                return null;
            }

            await Task.Delay(100, cancellationToken);
            session = asyncCompletion.GetSession(textView);
            if ((session is null || session.IsDismissed) && stopWatch.ElapsedMilliseconds - lastTriggerTime >= 1000)
            {
                session = TriggerCompletion();
            }
        }

        return session;

        IAsyncCompletionSession? TriggerCompletion()
        {
            lastTriggerTime = stopWatch.ElapsedMilliseconds;
            return asyncCompletion.TriggerCompletion(textView, new CompletionTrigger(CompletionTriggerReason.Insertion, textView.TextSnapshot), textView.Caret.Position.BufferPosition, cancellationToken);
        }
    }

    public Task<IAsyncCompletionSession?> WaitForExistingCompletionSessionAsync(CancellationToken cancellationToken)
    {
        return WaitForExistingCompletionSessionAsync(TimeSpan.FromSeconds(10), cancellationToken);
    }

    public async Task<IAsyncCompletionSession?> WaitForExistingCompletionSessionAsync(TimeSpan timeOut, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var textView = await GetActiveTextViewAsync(cancellationToken);

        var stopWatch = Stopwatch.StartNew();
        var asyncCompletion = await TestServices.Shell.GetComponentModelServiceAsync<IAsyncCompletionBroker>(cancellationToken);
        var session = asyncCompletion.GetSession(textView);

        while (session is null || session.IsDismissed)
        {
            if (stopWatch.ElapsedMilliseconds >= timeOut.TotalMilliseconds)
            {
                return null;
            }

            await Task.Delay(100, cancellationToken);
            session = asyncCompletion.GetSession(textView);
        }

        return session;
    }

    /// <summary>
    /// Open completion pop-up window UI and wait for the specified item to be present selected
    /// </summary>
    /// <param name="timeOut"></param>
    /// <param name="selectedItemLabel"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>Completion session that has matching selected item, or null otherwise</returns>
    public async Task<IAsyncCompletionSession?> OpenCompletionSessionAndWaitForItemAsync(TimeSpan timeOut, string selectedItemLabel, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        // Returns completion session that might or might not be visible in the IDE
        var session = await WaitForCompletionSessionAsync(timeOut, cancellationToken);

        if (session is null)
        {
            return null;
        }

        var textView = await GetActiveTextViewAsync(cancellationToken);
        var stopWatch = Stopwatch.StartNew();
        var asyncCompletion = await TestServices.Shell.GetComponentModelServiceAsync<IAsyncCompletionBroker>(cancellationToken);
        var lastOpenOrUpdateTime = 0L;
        var lastSessionResetTime = stopWatch.ElapsedMilliseconds;

        IAsyncCompletionSession? TriggerCompletion()
        {
            lastSessionResetTime = stopWatch.ElapsedMilliseconds;
            return asyncCompletion.TriggerCompletion(textView, new CompletionTrigger(CompletionTriggerReason.Invoke, textView.TextSnapshot), textView.Caret.Position.BufferPosition, cancellationToken);
        }

        void OpenOrUpdate(IAsyncCompletionSession currentSession)
        {
            // Preserve insertion semantics for active sessions so filtering and uniqueness still
            // reflect the text the test typed before we poll for the target item.
            currentSession.OpenOrUpdate(new CompletionTrigger(CompletionTriggerReason.Insertion, textView.TextSnapshot), textView.Caret.Position.BufferPosition, cancellationToken);
            lastOpenOrUpdateTime = stopWatch.ElapsedMilliseconds;
        }

        // Actually open the completion pop-up window and force visible items to be computed or re-computed
        OpenOrUpdate(session);
        while (true)
        {
            if (stopWatch.ElapsedMilliseconds >= timeOut.TotalMilliseconds)
            {
                return null;
            }

            var currentSession = session;
            if (currentSession is not null && !currentSession.IsDismissed)
            {
                var computedItems = currentSession.GetComputedItems(cancellationToken);
                if (computedItems.SelectedItem?.DisplayText == selectedItemLabel)
                {
                    return currentSession;
                }

                if (computedItems.SelectedItem is not null &&
                    !computedItems.Items.Any(item => item.DisplayText == selectedItemLabel) &&
                    stopWatch.ElapsedMilliseconds - lastSessionResetTime >= 1000)
                {
                    currentSession.Dismiss();
                    session = TriggerCompletion();
                    if (session is not null && !session.IsDismissed)
                    {
                        OpenOrUpdate(session);
                        continue;
                    }
                }
            }

            await Task.Delay(100, cancellationToken);

            if (currentSession is null || currentSession.IsDismissed)
            {
                session = asyncCompletion.GetSession(textView);
                if (session is null || session.IsDismissed)
                {
                    session = TriggerCompletion();
                    if (session is null || session.IsDismissed)
                    {
                        continue;
                    }
                }

                OpenOrUpdate(session);
                continue;
            }

            if (stopWatch.ElapsedMilliseconds - lastOpenOrUpdateTime >= 250)
            {
                OpenOrUpdate(currentSession);
            }
        }
    }

    public async Task<string> GetCompletionSessionDebugInfoAsync(string? expectedSelectedItemLabel, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var textView = await GetActiveTextViewAsync(cancellationToken);
        var asyncCompletion = await TestServices.Shell.GetComponentModelServiceAsync<IAsyncCompletionBroker>(cancellationToken);
        var session = asyncCompletion.GetSession(textView);
        var caret = textView.Caret.Position.BufferPosition;
        var currentLine = textView.TextSnapshot.GetLineFromPosition(caret).GetText();

        var builder = new StringBuilder();
        builder.AppendLine("Timed out waiting for completion session/item.");
        builder.AppendLine($"Expected selected item: {expectedSelectedItemLabel ?? "<none>"}");
        builder.AppendLine($"Caret position: {caret.Position}");
        builder.AppendLine($"Current line text: {currentLine}");

        if (session is null)
        {
            builder.AppendLine("No completion session was active.");
            return builder.ToString();
        }

        builder.AppendLine($"Session dismissed: {session.IsDismissed}");

        if (!session.IsDismissed)
        {
            var computedItems = session.GetComputedItems(cancellationToken);
            builder.AppendLine($"Selected item: {computedItems.SelectedItem?.DisplayText ?? "<null>"}");

            if (expectedSelectedItemLabel is not null)
            {
                builder.AppendLine($"Expected item present: {computedItems.Items.Any(i => i.DisplayText == expectedSelectedItemLabel)}");
            }

            var itemList = string.Join(", ", computedItems.Items.Take(10).Select(i => i.DisplayText));
            builder.AppendLine($"First items: {itemList}");
        }

        return builder.ToString();
    }
}
