// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.VisualStudio.Razor.IntegrationTests.InProcess;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Xunit;

namespace Microsoft.VisualStudio.Extensibility.Testing;

internal partial class EditorInProcess
{
    public async Task UndoTextAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
        dte.ActiveDocument.Undo();
    }

    public async Task InsertTextAsync(string text, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var view = await GetActiveTextViewAsync(cancellationToken);

        var position = await GetCaretPositionAsync(cancellationToken);

        _ = view.TextBuffer.Insert(position, text);
    }

    public async Task<int> SetTextAsync(TestCode text, CancellationToken cancellationToken, bool placeCaretAtPosition = true)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var view = await GetActiveTextViewAsync(cancellationToken);
        var textSnapshot = view.TextSnapshot;
        var replacementSpan = new SnapshotSpan(textSnapshot, 0, textSnapshot.Length);
        _ = view.TextBuffer.Replace(replacementSpan, text.Text);

        if (text is { Positions: [var position] })
        {
            if (placeCaretAtPosition)
            {
                await TestServices.Editor.PlaceCaretAsync(text.Position, cancellationToken);
            }

            return position;
        }

        return 0;
    }

    public async Task WaitForTextChangeAsync(Action action, CancellationToken cancellationToken)
    {
        using var semaphore = new SemaphoreSlim(1);
        await semaphore.WaitAsync(cancellationToken);

        var view = await GetActiveTextViewAsync(cancellationToken);
        view.TextBuffer.PostChanged += TextBuffer_PostChanged;

        action.Invoke();

        try
        {
            await semaphore.WaitAsync(cancellationToken);
        }
        finally
        {
            view.TextBuffer.PostChanged -= TextBuffer_PostChanged;
        }

        void TextBuffer_PostChanged(object sender, EventArgs e)
        {
            semaphore.Release();
            view.TextBuffer.PostChanged -= TextBuffer_PostChanged;
        }
    }

    public async Task<string> WaitForTextChangeAsync(string text, CancellationToken cancellationToken)
    {
        var result = await Helper.RetryAsync(async ct =>
            {
                var view = await GetActiveTextViewAsync(cancellationToken);
                var content = view.TextBuffer.CurrentSnapshot.GetText();

                if (text != content)
                {
                    return content;
                }

                return null;
            },
            TimeSpan.FromMilliseconds(50),
            cancellationToken).ConfigureAwait(false);

        return result!;
    }

    public async Task<bool> WaitForTextContainsAsync(string text, CancellationToken cancellationToken)
    {
        var result = await Helper.RetryAsync(async ct =>
            {
                var view = await GetActiveTextViewAsync(ct);
                var content = view.TextBuffer.CurrentSnapshot.GetText();

                return content.Contains(text);
            },
            TimeSpan.FromMilliseconds(50),
            cancellationToken).ConfigureAwait(false);

        return result;
    }

    public async Task VerifyTextContainsAsync(string text, CancellationToken cancellationToken)
    {
        var view = await GetActiveTextViewAsync(cancellationToken);
        var content = view.TextBuffer.CurrentSnapshot.GetText();
        Assert.Contains(text, content);
    }

    public async Task VerifyTextDoesntContainAsync(string text, CancellationToken cancellationToken)
    {
        var view = await GetActiveTextViewAsync(cancellationToken);
        var content = view.TextBuffer.CurrentSnapshot.GetText();
        Assert.DoesNotContain(text, content);
    }

    public async Task WaitForCurrentLineTextAsync(string text, CancellationToken cancellationToken)
    {
        await Helper.RetryAsync(async ct =>
        {
            var line = await GetCurrentLineTextAsync(cancellationToken);

            return line.Trim() == text.Trim();
        },
            TimeSpan.FromMilliseconds(50),
            cancellationToken);
    }

    public async Task<string> GetCurrentLineTextAsync(CancellationToken cancellationToken)
    {
        var view = await GetActiveTextViewAsync(cancellationToken);
        var caret = view.Caret.Position.BufferPosition;
        var line = view.TextBuffer.CurrentSnapshot.GetLineFromPosition(caret).GetText();
        return line;
    }

    public async Task WaitForCurrentLineTextStartsWithAsync(string text, CancellationToken cancellationToken)
    {
        await Helper.RetryAsync(async ct =>
            {
                var view = await GetActiveTextViewAsync(cancellationToken);
                var caret = view.Caret.Position.BufferPosition;
                var line = view.TextBuffer.CurrentSnapshot.GetLineFromPosition(caret).GetText();

                return line.Trim().StartsWith(text.Trim());
            },
            TimeSpan.FromMilliseconds(50),
            cancellationToken);
    }

    public async Task WaitForActiveWindowAsync(string windowTitle, CancellationToken cancellationToken)
    {
        await Helper.RetryAsync(async ct =>
            {
                var activeWindowCaption = await TestServices.Shell.GetActiveWindowCaptionAsync(cancellationToken);

                return activeWindowCaption == windowTitle;
            },
            TimeSpan.FromMilliseconds(50),
            cancellationToken);
    }

    public async Task WaitForActiveWindowByFileAsync(string fileName, CancellationToken cancellationToken)
    {
        await Helper.RetryAsync(async ct =>
            {
                var activeWindowCaption = await TestServices.Shell.GetActiveDocumentFileNameAsync(cancellationToken);

                return activeWindowCaption == fileName;
            },
            TimeSpan.FromMilliseconds(50),
            cancellationToken);
    }
}
