// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Razor.IntegrationTests.InProcess;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Extensibility.Testing;

internal partial class EditorInProcess
{
    public async Task DismissLightBulbSessionAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var view = await GetActiveTextViewAsync(cancellationToken);

        var broker = await GetComponentModelServiceAsync<ILightBulbBroker>(cancellationToken);
        broker.DismissSession(view);
    }

    public async Task InvokeCodeActionAsync(string codeActionTitle, CancellationToken cancellationToken)
    {
        var codeActions = await ShowLightBulbAsync(cancellationToken);

        var codeAction = codeActions.First(a => a.Actions.Single().DisplayText == codeActionTitle).Actions.Single();

        await InvokeCodeActionAsync(codeAction, cancellationToken);
    }

    public async Task<IEnumerable<SuggestedActionSet>> InvokeCodeActionListAsync(CancellationToken cancellationToken)
    {
        var lightbulbs = await ShowLightBulbAsync(cancellationToken);
        return lightbulbs;
    }

    public async Task InvokeCodeActionAsync(ISuggestedAction codeAction, CancellationToken cancellationToken)
    {
        var view = await GetActiveTextViewAsync(cancellationToken);

        codeAction.Invoke(cancellationToken);

        // ISuggestedAction.Invoke does not dismiss the session, so we must do it manually
        var broker = await GetComponentModelServiceAsync<ILightBulbBroker>(cancellationToken);
        broker.DismissSession(view);
    }

    public async Task<bool> IsLightBulbSessionExpandedAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var view = await GetActiveTextViewAsync(cancellationToken);

        var broker = await GetComponentModelServiceAsync<ILightBulbBroker>(cancellationToken);
        if (!broker.IsLightBulbSessionActive(view))
        {
            return false;
        }

        var session = broker.GetSession(view);
        if (session is null || !session.IsExpanded)
        {
            return false;
        }

        return true;
    }

    private async Task<IEnumerable<SuggestedActionSet>> ShowLightBulbAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var shell = await GetRequiredGlobalServiceAsync<SVsUIShell, IVsUIShell>(cancellationToken);
        var cmdGroup = typeof(VSConstants.VSStd14CmdID).GUID;
        var cmdExecOpt = OLECMDEXECOPT.OLECMDEXECOPT_DONTPROMPTUSER;

        var cmdID = VSConstants.VSStd14CmdID.ShowQuickFixes;
        object? obj = null;
        shell.PostExecCommand(cmdGroup, (uint)cmdID, (uint)cmdExecOpt, ref obj);

        var view = await GetActiveTextViewAsync(cancellationToken);
        var broker = await GetComponentModelServiceAsync<ILightBulbBroker>(cancellationToken);

        await LightBulbHelper.WaitForLightBulbSessionAsync(broker, view, cancellationToken);
        return await LightBulbHelper.WaitForItemsAsync(broker, view, cancellationToken);
    }
}
