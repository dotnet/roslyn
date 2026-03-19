// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.InProcess;

[TestService]
internal sealed partial class DebuggerInProcess
{
    /// <summary>
    /// HResult for "Operation Not Supported" when raising commands. 
    /// </summary>
    private const uint OperationNotSupportedHResult = 0x8971003c;

    /// <summary>
    /// Time to wait before re-polling a delegate.
    /// </summary>
    private static readonly TimeSpan DefaultPollingInterCallSleep = TimeSpan.FromMilliseconds(250);

    private async Task<EnvDTE100.Debugger5> GetDebuggerAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
        return (EnvDTE100.Debugger5)dte.Debugger;
    }

    public Task SetBreakpointAsync(string projectName, string fileName, string text, CancellationToken cancellationToken)
        => SetBreakpointAsync(projectName, fileName, text, charsOffset: 0, cancellationToken);

    public async Task SetBreakpointAsync(string projectName, string fileName, string text, int charsOffset, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        await TestServices.Editor.ActivateAsync(cancellationToken);
        await TestServices.Editor.SelectTextInCurrentDocumentAsync(text, cancellationToken);

        var caretPosition = await TestServices.Editor.GetCaretPositionAsync(cancellationToken);
        caretPosition.BufferPosition.GetLineAndCharacter(out var lineNumber, out var characterIndex);
        await SetBreakpointAsync(projectName, fileName, lineNumber, characterIndex + charsOffset, cancellationToken);
    }

    public async Task SetBreakpointAsync(string projectName, string fileName, int lineNumber, int characterIndex, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var breakpointFile = await TestServices.SolutionExplorer.GetAbsolutePathForProjectRelativeFilePathAsync(projectName, fileName, cancellationToken);

        var debugger = await GetDebuggerAsync(cancellationToken);
        // Need to increment the line number because editor line numbers starts from 0 but the debugger ones starts from 1.
        debugger.Breakpoints.Add(File: breakpointFile, Line: lineNumber + 1, Column: characterIndex);
    }

    public async Task GoAsync(bool waitForBreakMode, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var debugger = await GetDebuggerAsync(cancellationToken);

        // Dismiss the unexpected WSL error if it appears, since it will cause test hangs otherwise
        using var _ = TestServices.MessageBox.HandleMessageBox((text, caption) =>
        {
            if (text.Contains("WSL is not installed."))
            {
                // We do not understand why WSL is being selected as the active debugger
                return DialogResult.OK;
            }

            return DialogResult.None;
        });

        debugger.Go(waitForBreakMode);
    }

    public Task StepOverAsync(bool waitForBreakOrEnd, CancellationToken cancellationToken)
        => WaitForRaiseDebuggerDteCommandAsync(
            async cancellationToken =>
            {
                var debugger = await GetDebuggerAsync(cancellationToken);
                debugger.StepOver(waitForBreakOrEnd);
            },
            cancellationToken);

    public async Task StopAsync(bool waitForDesignMode, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var debugger = await GetDebuggerAsync(cancellationToken);
        debugger.Stop(waitForDesignMode);
    }

    public async Task SetNextStatementAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var debugger = await GetDebuggerAsync(cancellationToken);
        debugger.SetNextStatement();
    }

    public async Task ExecuteStatementAsync(string statement, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var debugger = await GetDebuggerAsync(cancellationToken);
        debugger.ExecuteStatement(statement);
    }

    public async Task CheckExpressionAsync(string expressionText, string expectedType, string expectedValue, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var debugger = await GetDebuggerAsync(cancellationToken);
        var entry = debugger.GetExpression(expressionText);
        Assert.Equal((expectedType, expectedValue), (entry.Type, entry.Value));
    }

    private static async Task WaitForRaiseDebuggerDteCommandAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        Func<CancellationToken, Task<bool>> predicate =
            async (cancellationToken) =>
            {
                try
                {
                    await action(cancellationToken);
                    return true;
                }
                catch (COMException ex) when ((uint)ex.ErrorCode != OperationNotSupportedHResult && !cancellationToken.IsCancellationRequested)
                {
                    return false;
                }
            };

        // Repeat the command if "Operation Not Supported" is thrown.
        await TryWaitForAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// Polls for the specified delegate to return true for the given timeout.
    /// </summary>
    /// <param name="predicate">Delegate to invoke.</param>
    public static Task TryWaitForAsync(Func<CancellationToken, Task<bool>> predicate, CancellationToken cancellationToken)
        => TryWaitForAsync(DefaultPollingInterCallSleep, predicate, cancellationToken);

    /// <summary>
    /// Polls for the specified delegate to return true for the given timeout.
    /// </summary>
    /// <param name="interval">Time to wait between polling.</param>
    /// <param name="predicate">Delegate to invoke.</param>
    private static async Task TryWaitForAsync(TimeSpan interval, Func<CancellationToken, Task<bool>> predicate, CancellationToken cancellationToken)
    {
        while (!await predicate(cancellationToken))
        {
            await Task.Delay(interval, cancellationToken);
        }
    }
}
