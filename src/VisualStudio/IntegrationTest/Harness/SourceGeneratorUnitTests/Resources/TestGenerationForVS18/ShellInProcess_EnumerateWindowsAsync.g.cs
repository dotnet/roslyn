// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Threading;

    internal partial class ShellInProcess
    {
        public async IAsyncEnumerable<IVsWindowFrame> EnumerateWindowsAsync(__WindowFrameTypeFlags windowFrameTypeFlags, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var uiShell = await GetRequiredGlobalServiceAsync<SVsUIShell, IVsUIShell4>(cancellationToken);
            ErrorHandler.ThrowOnFailure(uiShell.GetWindowEnum((uint)windowFrameTypeFlags, out var enumWindowFrames));
            var frameBuffer = new IVsWindowFrame[1];
            while (true)
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                ErrorHandler.ThrowOnFailure(enumWindowFrames.Next((uint)frameBuffer.Length, frameBuffer, out var fetched));
                if (fetched == 0)
                {
                    yield break;
                }

                for (var i = 0; i < fetched; i++)
                {
                    yield return frameBuffer[i];
                }
            }
        }
    }
}
