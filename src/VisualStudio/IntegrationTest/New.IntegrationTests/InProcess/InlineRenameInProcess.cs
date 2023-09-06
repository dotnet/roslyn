// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Extensibility.Testing;
using WindowsInput.Native;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess
{
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
            await TestServices.Input.SendWithoutActivateAsync(new InputKey[] { (VirtualKeyCode.VK_C, VirtualKeyCode.MENU) }, cancellationToken);
            await TestServices.Workspace.WaitForRenameAsync(cancellationToken);
        }

        public async Task ToggleIncludeStringsAsync(CancellationToken cancellationToken)
        {
            await TestServices.Input.SendWithoutActivateAsync(new InputKey[] { (VirtualKeyCode.VK_S, VirtualKeyCode.MENU) }, cancellationToken);
            await TestServices.Workspace.WaitForRenameAsync(cancellationToken);
        }

        public async Task ToggleIncludeOverloadsAsync(CancellationToken cancellationToken)
        {
            await TestServices.Input.SendWithoutActivateAsync(new InputKey[] { (VirtualKeyCode.VK_O, VirtualKeyCode.MENU) }, cancellationToken);
            await TestServices.Workspace.WaitForRenameAsync(cancellationToken);
        }
    }
}
