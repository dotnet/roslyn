// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature;
using Roslyn.Test.Utilities;

namespace Roslyn.VisualStudio.NewIntegrationTests.InProcess;

[TestService]
internal sealed partial class AddParameterDialogInProcess
{
    private async Task<AddParameterDialog?> TryGetDialogAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);
        return Application.Current.Windows.OfType<AddParameterDialog>().SingleOrDefault();
    }

    private async Task ClickAsync(Func<AddParameterDialog, ButtonBase> buttonAccessor, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dialog = await TryGetDialogAsync(cancellationToken);
        AssertEx.NotNull(dialog);

        Contract.ThrowIfFalse(await buttonAccessor(dialog).SimulateClickAsync(JoinableTaskFactory));
    }

    public async Task VerifyOpenAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await TryGetDialogAsync(cancellationToken) is null)
            {
                await Task.Delay(50, cancellationToken);
                continue;
            }

            await Task.Yield();
            return;
        }
    }

    public async Task VerifyClosedAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        while (await TryGetDialogAsync(cancellationToken) is not null)
        {
            await Task.Delay(50, cancellationToken);
        }
    }

    public async Task<bool> CloseWindowAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        if (await TryGetDialogAsync(cancellationToken) is not { })
            return false;

        await ClickCancelAsync(cancellationToken);
        return true;
    }

    public Task ClickOKAsync(CancellationToken cancellationToken)
        => ClickAsync(dialog => dialog.GetTestAccessor().OKButton, cancellationToken);

    public Task ClickCancelAsync(CancellationToken cancellationToken)
        => ClickAsync(dialog => dialog.GetTestAccessor().CancelButton, cancellationToken);

    public async Task FillCallSiteFieldAsync(string callsiteValue, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dialog = await TryGetDialogAsync(cancellationToken);
        AssertEx.NotNull(dialog);

        dialog.CallsiteValueTextBox.Focus();
        dialog.CallsiteValueTextBox.Text = callsiteValue;
    }

    public async Task FillNameFieldAsync(string parameterName, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dialog = await TryGetDialogAsync(cancellationToken);
        AssertEx.NotNull(dialog);

        dialog.NameContentControl.Focus();
        dialog.NameContentControl.Text = parameterName;
    }

    public async Task FillTypeFieldAsync(string typeName, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dialog = await TryGetDialogAsync(cancellationToken);
        AssertEx.NotNull(dialog);

        dialog.TypeContentControl.Focus();
        dialog.TypeContentControl.Text = typeName;
    }

    public Task SetCallSiteTodoAsync(CancellationToken cancellationToken)
        => ClickAsync(dialog => dialog.IntroduceErrorRadioButton, cancellationToken);
}
