// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.LanguageServices.Implementation.GenerateType;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;

namespace Roslyn.VisualStudio.NewIntegrationTests.InProcess;

[TestService]
internal sealed partial class GenerateTypeDialogInProcess
{
    private async Task<GenerateTypeDialog?> TryGetDialogAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);
        return Application.Current.Windows.OfType<GenerateTypeDialog>().SingleOrDefault();
    }

    private async Task ClickAsync(Func<GenerateTypeDialog, ButtonBase> buttonAccessor, CancellationToken cancellationToken)
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

    public async Task SetAccessibilityAsync(string accessibility, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dialog = await TryGetDialogAsync(cancellationToken);
        AssertEx.NotNull(dialog);

        Contract.ThrowIfFalse(await dialog.GetTestAccessor().AccessListComboBox.SimulateSelectItemAsync(JoinableTaskFactory, accessibility, cancellationToken));
    }

    public async Task SetKindAsync(string kind, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dialog = await TryGetDialogAsync(cancellationToken);
        AssertEx.NotNull(dialog);

        Contract.ThrowIfFalse(await dialog.GetTestAccessor().KindListComboBox.SimulateSelectItemAsync(JoinableTaskFactory, kind, cancellationToken));
    }

    public async Task SetTargetProjectAsync(string projectName, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dialog = await TryGetDialogAsync(cancellationToken);
        AssertEx.NotNull(dialog);

        Contract.ThrowIfFalse(await dialog.GetTestAccessor().ProjectListComboBox.SimulateSelectItemAsync(JoinableTaskFactory, projectName, cancellationToken));
    }

    public async Task SetTargetFileToNewNameAsync(string newFileName, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dialog = await TryGetDialogAsync(cancellationToken);
        AssertEx.NotNull(dialog);

        Contract.ThrowIfFalse(await dialog.GetTestAccessor().CreateNewFileRadioButton.SimulateClickAsync(JoinableTaskFactory));
        Contract.ThrowIfFalse(await dialog.GetTestAccessor().CreateNewFileComboBox.SimulateSelectItemAsync(JoinableTaskFactory, newFileName, mustExist: false, cancellationToken));
    }

    /// <summary>
    /// Clicks the "OK" button and waits for the related Code Action to complete.
    /// </summary>
    public async Task ClickOKAsync(CancellationToken cancellationToken)
    {
        await ClickAsync(dialog => dialog.GetTestAccessor().OKButton, cancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.LightBulb, cancellationToken);
    }

    /// <summary>
    /// Clicks the "Cancel" button and waits for the related Code Action to complete.
    /// </summary>
    public async Task ClickCancelAsync(CancellationToken cancellationToken)
    {
        await ClickAsync(dialog => dialog.GetTestAccessor().CancelButton, cancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.LightBulb, cancellationToken);
    }

    public async Task<ImmutableArray<string>> GetNewFileComboBoxItemsAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dialog = await TryGetDialogAsync(cancellationToken);
        AssertEx.NotNull(dialog);

        return dialog.GetTestAccessor().CreateNewFileComboBox.Items.Cast<string>().ToImmutableArray();
    }
}
