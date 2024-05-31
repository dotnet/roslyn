// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;
using Microsoft.CodeAnalysis.Editor.InlineRename;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess;

[TestService]
internal partial class InlineRenameInProcess
{
    public async Task InvokeAsync(CancellationToken cancellationToken)
    {
        await TestServices.Shell.ExecuteCommandAsync(VSConstants.VSStd2KCmdID.RENAME, cancellationToken);
        await TestServices.Workspace.WaitForRenameAsync(cancellationToken);
    }

    public async Task ToggleIncludeCommentsAsync(CancellationToken cancellationToken)
    {
        await TestServices.Input.SendWithoutActivateAsync([(VirtualKeyCode.VK_C, VirtualKeyCode.MENU)], cancellationToken);
        await TestServices.Workspace.WaitForRenameAsync(cancellationToken);
    }

    public async Task ToggleIncludeStringsAsync(CancellationToken cancellationToken)
    {
        await TestServices.Input.SendWithoutActivateAsync([(VirtualKeyCode.VK_S, VirtualKeyCode.MENU)], cancellationToken);
        await TestServices.Workspace.WaitForRenameAsync(cancellationToken);
    }

    public async Task ToggleIncludeOverloadsAsync(CancellationToken cancellationToken)
    {
        await TestServices.Input.SendWithoutActivateAsync([(VirtualKeyCode.VK_O, VirtualKeyCode.MENU)], cancellationToken);
        await TestServices.Workspace.WaitForRenameAsync(cancellationToken);
    }

    public async Task VerifyStringInFlyout(string expected, CancellationToken cancellationToken)
    {
        var optionService = await GetComponentModelServiceAsync<IGlobalOptionService>(cancellationToken);
        var isRenameFlyoutEnabled = optionService.GetOption(InlineRenameUIOptionsStorage.UseInlineAdornment);
        if (!isRenameFlyoutEnabled)
        {
            Contract.Fail("Inline rename flyout is disabled");
            return;
        }

        var vsTextManager = await GetRequiredGlobalServiceAsync<SVsTextManager, IVsTextManager>(cancellationToken);
        var vsTextView = await vsTextManager.GetActiveViewAsync(JoinableTaskFactory, cancellationToken);
        var testViewHost = await vsTextView.GetTextViewHostAsync(JoinableTaskFactory, cancellationToken);
        var renameAdornmentLayer = testViewHost.TextView.GetAdornmentLayer(InlineRenameAdornmentProvider.AdornmentLayerName);
        var inlineRenameFlyout = (RenameFlyout)renameAdornmentLayer.Elements.Single().Adornment;
        var actualStringInTextBox = inlineRenameFlyout.RenameUserInput.Text;
        Assert.Equal(expected, actualStringInTextBox);
    }
}
