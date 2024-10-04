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
using Microsoft.VisualStudio.LanguageServices.Implementation.ExtractInterface;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;

namespace Roslyn.VisualStudio.NewIntegrationTests.InProcess;

[TestService]
internal partial class ExtractInterfaceDialogInProcess
{
    private async Task<ExtractInterfaceDialog?> TryGetDialogAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);
        return Application.Current.Windows.OfType<ExtractInterfaceDialog>().SingleOrDefault();
    }

    private async Task ClickAsync(Func<ExtractInterfaceDialog, ButtonBase> buttonAccessor, CancellationToken cancellationToken)
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

    public async Task ClickOKAsync(CancellationToken cancellationToken)
    {
        await ClickAsync(dialog => dialog.GetTestAccessor().OKButton, cancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.LightBulb, cancellationToken);
    }

    public async Task ClickCancelAsync(CancellationToken cancellationToken)
    {
        await ClickAsync(dialog => dialog.GetTestAccessor().CancelButton, cancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.LightBulb, cancellationToken);
    }

    public async Task<string> GetTargetFileNameAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dialog = await TryGetDialogAsync(cancellationToken);
        AssertEx.NotNull(dialog);

        return dialog.DestinationControl.fileNameTextBox.Text;
    }

    public async Task<ImmutableArray<SymbolViewModel<ISymbol>>> GetSelectedItemsAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dialog = await TryGetDialogAsync(cancellationToken);
        AssertEx.NotNull(dialog);

        var memberSelectionList = dialog.GetTestAccessor().Members;
        var comListItems = memberSelectionList.Items;
        var listItems = Enumerable.Range(0, comListItems.Count).Select(comListItems.GetItemAt);

        return listItems.Cast<SymbolViewModel<ISymbol>>()
            .Where(viewModel => viewModel.IsChecked)
            .ToImmutableArray();
    }

    public async Task ClickDeselectAllAsync(CancellationToken cancellationToken)
    {
        await ClickAsync(dialog => dialog.GetTestAccessor().DeselectAllButton, cancellationToken);
    }

    public async Task ClickSelectAllAsync(CancellationToken cancellationToken)
    {
        await ClickAsync(dialog => dialog.GetTestAccessor().SelectAllButton, cancellationToken);
    }

    public async Task SelectSameFileAsync(CancellationToken cancellationToken)
    {
        await ClickAsync(dialog => dialog.GetTestAccessor().DestinationCurrentFileSelectionRadioButton, cancellationToken);
    }

    public async Task ToggleItemAsync(string item, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dialog = await TryGetDialogAsync(cancellationToken);
        AssertEx.NotNull(dialog);

        var memberSelectionList = dialog.GetTestAccessor().Members;
        var items = memberSelectionList.Items.Cast<MemberSymbolViewModel>().ToArray();
        var itemViewModel = items.Single(x => x.SymbolName == item);
        itemViewModel.IsChecked = !itemViewModel.IsChecked;

        // Wait for changes to propagate
        await Task.Yield();
    }
}
