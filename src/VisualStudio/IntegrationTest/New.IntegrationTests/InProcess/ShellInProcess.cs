// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess
{
    internal class ShellInProcess : InProcComponent
    {
        public ShellInProcess(TestServices testServices)
            : base(testServices)
        {
        }

        public async Task<string> GetActiveWindowCaptionAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var monitorSelection = await GetRequiredGlobalServiceAsync<SVsShellMonitorSelection, IVsMonitorSelection>(cancellationToken);
            ErrorHandler.ThrowOnFailure(monitorSelection.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_WindowFrame, out var windowFrameObj));
            var windowFrame = (IVsWindowFrame)windowFrameObj;

            ErrorHandler.ThrowOnFailure(windowFrame.GetProperty((int)__VSFPROPID.VSFPROPID_Caption, out var captionObj));
            return $"{captionObj}";
        }

        public async Task<Version> GetVersionAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var shell = await GetRequiredGlobalServiceAsync<SVsShell, IVsShell>(cancellationToken);
            shell.GetProperty((int)__VSSPROPID5.VSSPROPID_ReleaseVersion, out var versionProperty);

            var fullVersion = versionProperty?.ToString() ?? "";
            var firstSpace = fullVersion.IndexOf(' ');
            if (firstSpace >= 0)
            {
                // e.g. "17.1.31907.60 MAIN"
                fullVersion = fullVersion[..firstSpace];
            }

            if (Version.TryParse(fullVersion, out var version))
                return version;

            throw new NotSupportedException($"Unexpected version format: {versionProperty}");
        }
    }
}
