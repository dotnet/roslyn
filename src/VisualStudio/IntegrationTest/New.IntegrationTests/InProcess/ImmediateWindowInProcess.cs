// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.VisualStudio.IntegrationTests;

namespace Roslyn.VisualStudio.NewIntegrationTests.InProcess;

[TestService]
internal sealed partial class ImmediateWindowInProcess
{
    public Task ShowAsync(CancellationToken cancellationToken)
        => TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Debug.Immediate, cancellationToken);

    public async Task ClearAllAsync(CancellationToken cancellationToken)
    {
        await ShowAsync(cancellationToken);
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.ClearAll, cancellationToken);
    }

    public async Task<string> GetTextAsync(CancellationToken cancellationToken)
    {
        var shell = await GetRequiredGlobalServiceAsync<SVsUIShell, IVsUIShell>(cancellationToken);
        var immediateWindowGuid = VSConstants.StandardToolWindows.Immediate;
        IVsWindowFrame immediateWindowFrame;
        ErrorHandler.ThrowOnFailure(shell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref immediateWindowGuid, out immediateWindowFrame));
        ErrorHandler.ThrowOnFailure(immediateWindowFrame.Show());
        ErrorHandler.ThrowOnFailure(immediateWindowFrame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out var docView));
        var vsTextView = (IVsTextView)docView;
        ErrorHandler.ThrowOnFailure(vsTextView.GetBuffer(out var vsTextLines));
        ErrorHandler.ThrowOnFailure(vsTextLines.GetLineCount(out var lineCount));
        ErrorHandler.ThrowOnFailure(vsTextLines.GetLengthOfLine(lineCount - 1, out var lastLineLength));
        ErrorHandler.ThrowOnFailure(vsTextLines.GetLineText(0, 0, lineCount - 1, lastLineLength, out var text));
        return text;
    }
}
