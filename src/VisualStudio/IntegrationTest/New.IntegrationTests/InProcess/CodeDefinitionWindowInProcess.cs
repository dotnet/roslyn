// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;

namespace Roslyn.VisualStudio.NewIntegrationTests.InProcess;

[TestService]
internal sealed partial class CodeDefinitionWindowInProcess
{
    public async Task ShowAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        var codeDefinitionWindow = await GetRequiredGlobalServiceAsync<SVsCodeDefView, IVsCodeDefView>(cancellationToken);
        ErrorHandler.ThrowOnFailure(codeDefinitionWindow.ShowWindow());
    }

    public async Task HideAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        var codeDefinitionWindow = await GetRequiredGlobalServiceAsync<SVsCodeDefView, IVsCodeDefView>(cancellationToken);
        ErrorHandler.ThrowOnFailure(codeDefinitionWindow.HideWindow());
    }

    public async Task<string> GetCurrentLineTextAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        await WaitUntilProcessingCompleteAsync(cancellationToken);
        var view = await GetCodeDefinitionWpfTextViewAsync(cancellationToken);

        var bufferPosition = view.Caret.Position.BufferPosition;
        var line = bufferPosition.GetContainingLine();

        return line.GetText();
    }

    public async Task<string> GetTextAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        await WaitUntilProcessingCompleteAsync(cancellationToken);
        var view = await GetCodeDefinitionWpfTextViewAsync(cancellationToken);
        return view.TextSnapshot.GetText();
    }

    /// <summary>
    /// Waits for all async processing to complete, including the async processing in the
    /// code definition window itself.
    /// </summary>
    private async Task WaitUntilProcessingCompleteAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.CodeDefinitionWindow, cancellationToken);

        var codeDefinitionWindow = await GetRequiredGlobalServiceAsync<SVsCodeDefView, IVsCodeDefView>(cancellationToken);

        // The code definition window does some processing on idle, which we can force after we've completed our
        // processing.
        ErrorHandler.ThrowOnFailure(codeDefinitionWindow.ForceIdleProcessing());
    }

    private async Task<IWpfTextView> GetCodeDefinitionWpfTextViewAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var shell = await GetRequiredGlobalServiceAsync<SVsUIShell, IVsUIShell>(cancellationToken);
        var windowGuid = Guid.Parse(ToolWindowGuids80.CodedefinitionWindow);

        Marshal.ThrowExceptionForHR(shell.FindToolWindow(0, ref windowGuid, out var windowFrame));

        var view = VsShellUtilities.GetTextView(windowFrame);
        var editorAdaptersService = await GetComponentModelServiceAsync<IVsEditorAdaptersFactoryService>(cancellationToken);

        var wpfView = editorAdaptersService.GetWpfTextView(view);

        Contract.ThrowIfNull(wpfView, "We were unable to get the Code Definition Window view.");

        return wpfView;
    }
}
