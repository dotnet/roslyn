// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Setup.Configuration;
using Roslyn.VisualStudio.Test.Utilities.Interop;

namespace Roslyn.VisualStudio.Test.Utilities
{
    public sealed class VisualStudioInstanceFactory : IDisposable
    {
        internal static readonly string VsProductVersion = Settings.Default.VsProductVersion;

        internal static readonly string VsProgId = $"VisualStudio.DTE.{VsProductVersion}";

        internal static readonly string VsLaunchArgs = $"{(string.IsNullOrWhiteSpace(Settings.Default.VsRootSuffix) ? "/log" : $"/rootsuffix {Settings.Default.VsRootSuffix}")} /log";

        static VisualStudioInstanceFactory()
        {
            var majorVsProductVersion = VsProductVersion.Split('.')[0];

            if (int.Parse(majorVsProductVersion) < 15)
            {
                throw new PlatformNotSupportedException("The Visual Studio Integration Test Framework is only supported on Visual Studio 15.0 and later.");
            }
        }

        /// <summary>
        /// The instance that has already been launched by this factory and can be reused.
        /// </summary>
        private VisualStudioInstance _currentlyRunningInstance;
        private ImmutableHashSet<string> _supportedPackageIds;
        private string _installationPath;
        private bool _hasCurrentlyActiveContext;

        /// <summary>
        /// Returns a <see cref="VisualStudioInstanceContext"/>, starting a new instance of Visual Studio if necessary.
        /// </summary>
        public VisualStudioInstanceContext GetNewOrUsedInstance(ImmutableHashSet<string> requiredPackageIds)
        {
            ThrowExceptionIfAlreadyHasActiveContext();

            if (ShouldStartNewInstance(requiredPackageIds))
            {
                StartNewInstance(requiredPackageIds);
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

        private bool ShouldStartNewInstance(ImmutableHashSet<string> requiredPackageIds)
        {
            // We need to start a new instance if:
            //  * The current instance does not exist -or-
            //  * The current instance does not support all the required packages -or-
            //  * The current instance is no longer running

            return _currentlyRunningInstance == null
                || (_supportedPackageIds != null && !requiredPackageIds.All((requiredPackageId) => _supportedPackageIds.Contains(requiredPackageId))) // _supportedPackagesIds will be null if ISetupInstance2.GetPackages() is NYI
                || !_currentlyRunningInstance.IsRunning;
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
        private void StartNewInstance(ImmutableHashSet<string> requiredPackageIds)
        {
            var instance = LocateVisualStudioInstance(requiredPackageIds) as ISetupInstance2;

            _supportedPackageIds = ImmutableHashSet.CreateRange(instance.GetPackages().Select((supportedPackage) => supportedPackage.GetId()));
            _installationPath = instance.GetInstallationPath();

            var process = StartNewVisualStudioProcess(_installationPath);
            // We wait until the DTE instance is up before we're good
            var dte = IntegrationHelper.WaitForNotNullAsync(() => IntegrationHelper.TryLocateDteForProcess(process)).Result;

            _currentlyRunningInstance = new VisualStudioInstance(process, dte);
        }

        private static ISetupConfiguration GetSetupConfiguration()
        {
            ISetupConfiguration setupConfiguration;

            try
            {
                setupConfiguration = new SetupConfiguration();
            }
            catch (COMException comException) when (comException.HResult == NativeMethods.REGDB_E_CLASSNOTREG)
            {
                // Fallback to P/Invoke if the COM registration is missing
                NativeMethods.GetSetupConfiguration(out setupConfiguration);
            }

            return setupConfiguration;
        }

        private static IEnumerable<ISetupInstance> EnumerateVisualStudioInstances()
        {
            var setupConfiguration = GetSetupConfiguration() as ISetupConfiguration2;

            var instanceEnumerator = setupConfiguration.EnumAllInstances();
            var instances = new ISetupInstance[3];

            var instancesFetched = 0;
            instanceEnumerator.Next(instances.Length, instances, out instancesFetched);

            if (instancesFetched == 0)
            {
                throw new Exception("There were no instances of Visual Studio 15.0 or later found.");
            }

            do
            {
                for (var index = 0; index < instancesFetched; index++)
                {
                    yield return instances[index];
                }

                instanceEnumerator.Next(instances.Length, instances, out instancesFetched);
            }
            while (instancesFetched != 0);
        }

        private static ISetupInstance LocateVisualStudioInstance(ImmutableHashSet<string> requiredPackageIds)
        {
            var instances = EnumerateVisualStudioInstances().Where((instance) => instance.GetInstallationVersion().StartsWith(VsProductVersion));

            var instanceFoundWithInvalidState = false;

            foreach (ISetupInstance2 instance in instances)
            {
                var packages = instance.GetPackages()
                                        .Where((package) => requiredPackageIds.Contains(package.GetId()));

                if (packages.Count() != requiredPackageIds.Count())
                {
                    continue;
                }

                const InstanceState minimumRequiredState = InstanceState.Local | InstanceState.Registered;

                var state = instance.GetState();

                if ((state & minimumRequiredState) == minimumRequiredState)
                {
                    return instance;
                }

                Debug.WriteLine($"An instance matching the specified requirements but had an invalid state. (State: {state})");
                instanceFoundWithInvalidState = true;
            }

            throw new Exception(instanceFoundWithInvalidState ?
                                "An instance matching the specified requirements was found but it was in an invalid state." :
                                "There were no instances of Visual Studio 15.0 or later found that match the specified requirements.");
        }

        private static Process StartNewVisualStudioProcess(string installationPath)
        {
            var vsExeFile = Path.Combine(installationPath, @"Common7\IDE\devenv.exe");

            Process.Start(vsExeFile, $"/resetsettings General.vssettings /command \"File.Exit\" {VsLaunchArgs}").WaitForExit();

            // Make sure we kill any leftover processes spawned by the host
            IntegrationHelper.KillProcess("DbgCLR");
            IntegrationHelper.KillProcess("VsJITDebugger");
            IntegrationHelper.KillProcess("dexplore");

            var process = Process.Start(vsExeFile, VsLaunchArgs);

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
