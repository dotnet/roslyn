// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace Roslyn.VisualStudio.Test.Utilities
{
    public sealed class VisualStudioInstanceFactory : IDisposable
    {
        internal static readonly string VsProductVersion = Settings.Default.VsProductVersion;


        internal static readonly string VsProgId = $"VisualStudio.DTE.{VsProductVersion}";

        internal static readonly string Wow6432Registry = Environment.Is64BitProcess ? "WOW6432Node" : string.Empty;
        internal static readonly string VsRegistryRoot = Path.Combine("SOFTWARE", Wow6432Registry, "Microsoft", "VisualStudio", VsProductVersion);

        internal static readonly string VsCommon7Folder = Path.GetFullPath(IntegrationHelper.GetRegistryKeyValue(Registry.LocalMachine, VsRegistryRoot, "InstallDir").ToString());

        internal static readonly string VsExeFile = Path.Combine(VsCommon7Folder, "devenv.exe");
        internal static readonly string VsLaunchArgs = $"{(string.IsNullOrWhiteSpace(Settings.Default.VsRootSuffix) ? "/log" : $"/rootsuffix {Settings.Default.VsRootSuffix}")} /log";

        /// <summary>
        /// The instance that has already been launched by this factory and can be reused.
        /// </summary>
        private VisualStudioInstance _currentlyRunningInstance;
        private bool _hasCurrentlyActiveContext;

        /// <summary>
        /// Returns a <see cref="VisualStudioInstanceContext"/>, starting a new instance of Visual Studio if necessary.
        /// </summary>
        public VisualStudioInstanceContext GetNewOrUsedInstance()
        {
            ThrowExceptionIfAlreadyHasActiveContext();

            if (_currentlyRunningInstance == null || !_currentlyRunningInstance.IsRunning)
            {
                StartNewInstance();
            }

            return new VisualStudioInstanceContext(_currentlyRunningInstance, this);
        }

        internal void NotifyCurrentInstanceContextDisposed(bool canReuse)
        {
            ThrowExceptionIfAlreadyHasActiveContext();

            _hasCurrentlyActiveContext = false;

            if (!canReuse)
            {
                _currentlyRunningInstance = null;
            }
        }

        private void ThrowExceptionIfAlreadyHasActiveContext()
        {
            if (_hasCurrentlyActiveContext)
            {
                throw new Exception($"The previous integration test failed to call {nameof(VisualStudioInstanceContext)}.{nameof(Dispose)}. Ensure that test does that to ensure the Visual Studio instance is correctly cleaned up.");
            }
        }

        /// <summary>
        /// Starts up a new <see cref="VisualStudioInstance"/>, shutting down any instances that are already running.
        /// </summary>
        private void StartNewInstance()
        {
            var process = StartNewVisualStudioProcess();

            // We wait until the DTE instance is up before we're good
            var dte = IntegrationHelper.WaitForNotNullAsync(() => IntegrationHelper.TryLocateDteForProcess(process)).Result;

            _currentlyRunningInstance = new VisualStudioInstance(process, dte);
        }

        private static Process StartNewVisualStudioProcess()
        {
            // TODO: This might not be needed anymore as I don't believe we do things which risk corrupting the MEF cache. However,
            // it is still useful to do in case some other action corruped the MEF cache as we don't have to restart the host
            Process.Start(VsExeFile, $"/clearcache {VsLaunchArgs}").WaitForExit();
            Process.Start(VsExeFile, $"/updateconfiguration {VsLaunchArgs}").WaitForExit();

            // Make sure we kill any leftover processes spawned by the host
            IntegrationHelper.KillProcess("DbgCLR");
            IntegrationHelper.KillProcess("VsJITDebugger");
            IntegrationHelper.KillProcess("dexplore");

            var process = Process.Start(VsExeFile, VsLaunchArgs);

            Debug.WriteLine($"Launched a new instance of Visual Studio. (ID: {process.Id})");

            return process;
        }

        public void Dispose()
        {
            _currentlyRunningInstance?.Close();

            // We want to make sure everybody cleaned up their contexts by the end of everything
            ThrowExceptionIfAlreadyHasActiveContext();
        }
    }
}
