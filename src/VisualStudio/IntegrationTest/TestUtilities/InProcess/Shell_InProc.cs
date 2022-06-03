// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class Shell_InProc : InProcComponent
    {
        public static Shell_InProc Create() => new Shell_InProc();

        public IntPtr GetHWnd()
            => GetDTE().MainWindow.HWnd;

        public bool IsUIContextActive(Guid context)
        {
            return UIContext.FromUIContextGuid(context).IsActive;
        }

        public Version GetVersion()
        {
            return InvokeOnUIThread(cancellationToken =>
            {
                var shell = GetGlobalService<SVsShell, IVsShell>();
                shell.GetProperty((int)__VSSPROPID5.VSSPROPID_ReleaseVersion, out var versionProperty);

                var fullVersion = versionProperty?.ToString() ?? string.Empty;
                var firstSpace = fullVersion.IndexOf(' ');
                if (firstSpace >= 0)
                {
                    // e.g. "17.1.31907.60 MAIN"
                    fullVersion = fullVersion.Substring(0, firstSpace);
                }

                if (Version.TryParse(fullVersion, out var version))
                {
                    return version;
                }

                throw new NotSupportedException($"Unexpected version format: {versionProperty}");
            });
        }
    }
}
