// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

internal static class CommandLineMode
{
    // tri-state: uninitialized (0), devenv is in command line mode (1), devenv is not in command line mode (-1)
    private static volatile int s_isInCommandLineMode;

    /// <summary>
    /// Returns true if devenv is invoked in command line mode for build, e.g. devenv /rebuild MySolution.sln
    /// </summary>
    public static async Task<bool> IsInCommandLineModeAsync(IAsyncServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (s_isInCommandLineMode == 0)
        {
            var appId = await serviceProvider.GetServiceAsync<IVsAppId, IVsAppId>(cancellationToken).ConfigureAwait(true);

            s_isInCommandLineMode =
                ErrorHandler.Succeeded(appId.GetProperty(VSAPROPID_IsInCommandLineMode, out var result)) &&
                (bool)result ? 1 : -1;
        }

        return s_isInCommandLineMode == 1;
    }

    // Copied from https://github.com/dotnet/project-system/blob/698c90fc016a24fd5b0b2b73df2c68299e04bd66/src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Interop/IVsAppId.cs
    [Guid("1EAA526A-0898-11d3-B868-00C04F79F802"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsAppId
    {
        [PreserveSig]
        int SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider pSP);

        [PreserveSig]
        int GetProperty(int propid, // VSAPROPID
            [MarshalAs(UnmanagedType.Struct)] out object pvar);

        [PreserveSig]
        int SetProperty(int propid, //[in] VSAPROPID
            [MarshalAs(UnmanagedType.Struct)] object var);

        [PreserveSig]
        int GetGuidProperty(int propid, // VSAPROPID
            out Guid guid);

        [PreserveSig]
        int SetGuidProperty(int propid, // [in] VSAPROPID
            ref Guid rguid);

        [PreserveSig]
        int Initialize();  // called after main initialization and before command executing and entering main loop
    }

    private const int VSAPROPID_IsInCommandLineMode = -8660;
}
