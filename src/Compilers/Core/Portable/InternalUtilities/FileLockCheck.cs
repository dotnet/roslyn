// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Based on https://github.com/dotnet/msbuild/blob/6cd445d84e59a36c7fbb6f50b7a5a62767a6da51/src/Utilities/LockCheck.cs

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Roslyn.Utilities;

/// <summary>
/// This class implements checking what processes are locking a file on Windows.
/// It uses the Restart Manager API to do this. Other platforms are skipped.
/// </summary>
internal static class FileLockCheck
{
    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RM_UNIQUE_PROCESS
    {
        public uint dwProcessId;
        public FILETIME ProcessStartTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct RM_PROCESS_INFO
    {
        private const int CCH_RM_MAX_APP_NAME = 255;
        private const int CCH_RM_MAX_SVC_NAME = 63;

        internal RM_UNIQUE_PROCESS Process;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_APP_NAME + 1)]
        public string strAppName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_SVC_NAME + 1)]
        public string strServiceShortName;
        internal int ApplicationType;
        public uint AppStatus;
        public uint TSSessionId;
        [MarshalAs(UnmanagedType.Bool)]
        public bool bRestartable;
    }

    private const string RestartManagerDll = "rstrtmgr.dll";

    [DllImport(RestartManagerDll, CharSet = CharSet.Unicode)]
    private static extern int RmRegisterResources(uint pSessionHandle,
        uint nFiles,
        string[] rgsFilenames,
        uint nApplications,
        [In] RM_UNIQUE_PROCESS[]? rgApplications,
        uint nServices,
        string[]? rgsServiceNames);

    /// <summary>
    /// Starts a new Restart Manager session.
    /// A maximum of 64 Restart Manager sessions per user session
    /// can be open on the system at the same time. When this
    /// function starts a session, it returns a session handle
    /// and session key that can be used in subsequent calls to
    /// the Restart Manager API.
    /// </summary>
    /// <param name="pSessionHandle">
    /// A pointer to the handle of a Restart Manager session.
    /// The session handle can be passed in subsequent calls
    /// to the Restart Manager API.
    /// </param>
    /// <param name="dwSessionFlags">
    /// Reserved. This parameter should be 0.
    /// </param>
    /// <param name="strSessionKey">
    /// A null-terminated string that contains the session key
    /// to the new session. The string must be allocated before
    /// calling the RmStartSession function.
    /// </param>
    /// <returns>System error codes that are defined in Winerror.h.</returns>
    /// <remarks>
    /// The Rm­­StartSession function doesn’t properly null-terminate
    /// the session key, even though the function is documented as
    /// returning a null-terminated string. To work around this bug,
    /// we pre-fill the buffer with null characters so that whatever
    /// ends gets written will have a null terminator (namely, one of
    /// the null characters we placed ahead of time).
    /// <para>
    /// see <see href="http://blogs.msdn.com/b/oldnewthing/archive/2012/02/17/10268840.aspx"/>.
    /// </para>
    /// </remarks>
    [DllImport(RestartManagerDll, CharSet = CharSet.Unicode)]
    private static extern unsafe int RmStartSession(
        out uint pSessionHandle,
        int dwSessionFlags,
        char* strSessionKey);

    /// <summary>
    /// Ends the Restart Manager session.
    /// This function should be called by the primary installer that
    /// has previously started the session by calling the <see cref="RmStartSession"/>
    /// function. The RmEndSession function can be called by a secondary installer
    /// that is joined to the session once no more resources need to be registered
    /// by the secondary installer.
    /// </summary>
    /// <param name="pSessionHandle">A handle to an existing Restart Manager session.</param>
    /// <returns>
    /// The function can return one of the system error codes that are defined in Winerror.h.
    /// </returns>
    [DllImport(RestartManagerDll)]
    private static extern int RmEndSession(uint pSessionHandle);

    [DllImport(RestartManagerDll, CharSet = CharSet.Unicode)]
    private static extern int RmGetList(uint dwSessionHandle,
        out uint pnProcInfoNeeded,
        ref uint pnProcInfo,
        [In, Out] RM_PROCESS_INFO[]? rgAffectedApps,
        ref uint lpdwRebootReasons);

    public static ImmutableArray<(int processId, string applicationName)> TryGetLockingProcessInfos(string path)
    {
        if (!PlatformInformation.IsWindows)
        {
            return [];
        }

        try
        {
            return GetLockingProcessInfosImpl([path]);
        }
        catch
        {
            return [];
        }
    }

    private static ImmutableArray<(int processId, string applicationName)> GetLockingProcessInfosImpl(string[] paths)
    {
        const int MaxRetries = 6;
        const int ERROR_MORE_DATA = 234;
        const uint RM_REBOOT_REASON_NONE = 0;

        uint handle;
        int res;

        unsafe
        {
            char* key = stackalloc char[sizeof(Guid) * 2 + 1];
            res = RmStartSession(out handle, 0, key);
        }

        if (res != 0)
        {
            return [];
        }

        try
        {
            res = RmRegisterResources(handle, (uint)paths.Length, paths, 0, null, 0, null);
            if (res != 0)
            {
                return [];
            }

            //
            // Obtain the list of affected applications/services.
            //
            // NOTE: Restart Manager returns the results into the buffer allocated by the caller. The first call to
            // RmGetList() will return the size of the buffer (i.e. nProcInfoNeeded) the caller needs to allocate.
            // The caller then needs to allocate the buffer (i.e. rgAffectedApps) and make another RmGetList()
            // call to ask Restart Manager to write the results into the buffer. However, since Restart Manager
            // refreshes the list every time RmGetList()is called, it is possible that the size returned by the first
            // RmGetList()call is not sufficient to hold the results discovered by the second RmGetList() call. Therefore,
            // it is recommended that the caller follows the following practice to handle this race condition:
            //
            //    Use a loop to call RmGetList() in case the buffer allocated according to the size returned in previous
            //    call is not enough.
            //
            uint pnProcInfo = 0;
            RM_PROCESS_INFO[]? rgAffectedApps = null;
            int retry = 0;
            do
            {
                uint lpdwRebootReasons = RM_REBOOT_REASON_NONE;
                res = RmGetList(handle, out uint pnProcInfoNeeded, ref pnProcInfo, rgAffectedApps, ref lpdwRebootReasons);
                if (res == 0)
                {
                    // If pnProcInfo == 0, then there is simply no locking process (found), in this case rgAffectedApps is "null".
                    if (pnProcInfo == 0)
                    {
                        return [];
                    }

                    Debug.Assert(rgAffectedApps != null);

                    var lockInfos = ArrayBuilder<(int, string)>.GetInstance((int)pnProcInfo);
                    for (int i = 0; i < pnProcInfo; i++)
                    {
                        lockInfos.Add(((int)rgAffectedApps[i].Process.dwProcessId, rgAffectedApps[i].strAppName));
                    }

                    return lockInfos.ToImmutableAndFree();
                }

                if (res != ERROR_MORE_DATA)
                {
                    return [];
                }

                pnProcInfo = pnProcInfoNeeded;
                rgAffectedApps = new RM_PROCESS_INFO[pnProcInfo];
            }
            while (res == ERROR_MORE_DATA && retry++ < MaxRetries);
        }
        finally
        {
            _ = RmEndSession(handle);
        }

        return [];
    }
}
