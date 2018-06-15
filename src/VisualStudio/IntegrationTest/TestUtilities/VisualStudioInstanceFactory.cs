// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Setup.Configuration;
using RunTests;
using Process = System.Diagnostics.Process;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    public sealed class VisualStudioInstanceFactory : IDisposable
    {
        [ThreadStatic]
        private static bool s_inHandler;

        public static readonly string VsProductVersion = Settings.Default.VsProductVersion;

        public static readonly string VsLaunchArgs = $"{(string.IsNullOrWhiteSpace(Settings.Default.VsRootSuffix) ? "/log" : $"/rootsuffix {Settings.Default.VsRootSuffix}")} /log";

        /// <summary>
        /// The instance that has already been launched by this factory and can be reused.
        /// </summary>
        private VisualStudioInstance _currentlyRunningInstance;

        private bool _hasCurrentlyActiveContext;

        static VisualStudioInstanceFactory()
        {
            var majorVsProductVersion = VsProductVersion.Split('.')[0];

            if (int.Parse(majorVsProductVersion) < 15)
            {
                throw new PlatformNotSupportedException("The Visual Studio Integration Test Framework is only supported on Visual Studio 15.0 and later.");
            }
        }

        public VisualStudioInstanceFactory()
        {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolveHandler;
            AppDomain.CurrentDomain.FirstChanceException += FirstChanceExceptionHandler;
        }

        private static void FirstChanceExceptionHandler(object sender, FirstChanceExceptionEventArgs eventArgs)
        {
            if (s_inHandler)
            {
                // An exception was thrown from within the handler, resulting in a recursive call to the handler.
                // Bail out now we so don't recursively throw another exception and overflow the stack.
                return;
            }

            try
            {
                s_inHandler = true;

                var assemblyDirectory = GetAssemblyDirectory();
                var testName = CaptureTestNameAttribute.CurrentName ?? "Unknown";
                var logDir = Path.Combine(assemblyDirectory, "xUnitResults", "Screenshots");
                var baseFileName = $"{testName}-{eventArgs.Exception.GetType().Name}-{DateTime.Now:HH.mm.ss}";
                ScreenshotService.TakeScreenshot(Path.Combine(logDir, $"{baseFileName}.png"));

                var exception = eventArgs.Exception;
                File.WriteAllText(
                    Path.Combine(logDir, $"{baseFileName}.log"),
                    $"{exception}.GetType().Name{Environment.NewLine}{exception.StackTrace}");

            }
            finally
            {
                s_inHandler = false;
            }
        }

        // This looks like it is pointless (since we are returning an assembly that is already loaded) but it is actually required.
        // The BinaryFormatter, when invoking 'HandleReturnMessage', will end up attempting to call 'BinaryAssemblyInfo.GetAssembly()',
        // which will itself attempt to call 'Assembly.Load()' using the full name of the assembly for the type that is being deserialized.
        // Depending on the manner in which the assembly was originally loaded, this may end up actually trying to load the assembly a second
        // time and it can fail if the standard assembly resolution logic fails. This ensures that we 'succeed' this secondary load by returning
        // the assembly that is already loaded.
        private static Assembly AssemblyResolveHandler(object sender, ResolveEventArgs eventArgs)
        {
            Debug.WriteLine($"'{eventArgs.RequestingAssembly}' is attempting to resolve '{eventArgs.Name}'");
            var resolvedAssembly = AppDomain.CurrentDomain.GetAssemblies().Where((assembly) => assembly.FullName.Equals(eventArgs.Name)).SingleOrDefault();

            if (resolvedAssembly != null)
            {
                Debug.WriteLine("The assembly was already loaded!");
            }

            return resolvedAssembly;
        }

        /// <summary>
        /// Returns a <see cref="VisualStudioInstanceContext"/>, starting a new instance of Visual Studio if necessary.
        /// </summary>
        public async Task<VisualStudioInstanceContext> GetNewOrUsedInstanceAsync(ImmutableHashSet<string> requiredPackageIds)
        {
            ThrowExceptionIfAlreadyHasActiveContext();

            try
            {
                bool shouldStartNewInstance = ShouldStartNewInstance(requiredPackageIds);
                await UpdateCurrentlyRunningInstanceAsync(requiredPackageIds, shouldStartNewInstance).ConfigureAwait(false);

                return new VisualStudioInstanceContext(_currentlyRunningInstance, this);
            }
            catch
            {
                // Make sure the next test doesn't try to reuse the same instance
                NotifyCurrentInstanceContextDisposed(canReuse: false);
                throw;
            }
        }

        internal void NotifyCurrentInstanceContextDisposed(bool canReuse)
        {
            ThrowExceptionIfAlreadyHasActiveContext();

            _hasCurrentlyActiveContext = false;

            if (!canReuse)
            {
                _currentlyRunningInstance?.Close();
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
                || (!requiredPackageIds.All(id => _currentlyRunningInstance.SupportedPackageIds.Contains(id)))
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
        private async Task UpdateCurrentlyRunningInstanceAsync(ImmutableHashSet<string> requiredPackageIds, bool shouldStartNewInstance)
        {
            Process hostProcess;
            DTE dte;
            ImmutableHashSet<string> supportedPackageIds;
            string installationPath;

            if (shouldStartNewInstance)
            {
                // We are starting a new instance, so ensure we close the currently running instance, if it exists
                _currentlyRunningInstance?.Close();

                var instance = LocateVisualStudioInstance(requiredPackageIds) as ISetupInstance2;
                supportedPackageIds = ImmutableHashSet.CreateRange(instance.GetPackages().Select((supportedPackage) => supportedPackage.GetId()));
                installationPath = instance.GetInstallationPath();

                hostProcess = StartNewVisualStudioProcess(installationPath);

                var procDumpInfo = ProcDumpInfo.ReadFromEnvironment();
                if (procDumpInfo != null)
                {
                    ProcDumpUtil.AttachProcDump(procDumpInfo.Value, hostProcess.Id);
                }

                // We wait until the DTE instance is up before we're good
                dte = await IntegrationHelper.WaitForNotNullAsync(() => IntegrationHelper.TryLocateDteForProcess(hostProcess)).ConfigureAwait(false);
            }
            else
            {
                // We are going to reuse the currently running instance, so ensure that we grab the host Process and DTE
                // before cleaning up any hooks or remoting services created by the previous instance. We will then
                // create a new VisualStudioInstance from the previous to ensure that everything is in a 'clean' state.
                //
                // We create a new DTE instance in the current context since the COM object could have been separated
                // from its RCW during the previous test.

                Debug.Assert(_currentlyRunningInstance != null);

                hostProcess = _currentlyRunningInstance.HostProcess;
                dte = await IntegrationHelper.WaitForNotNullAsync(() => IntegrationHelper.TryLocateDteForProcess(hostProcess)).ConfigureAwait(false);
                supportedPackageIds = _currentlyRunningInstance.SupportedPackageIds;
                installationPath = _currentlyRunningInstance.InstallationPath;

                _currentlyRunningInstance.Close(exitHostProcess: false);
            }

            _currentlyRunningInstance = new VisualStudioInstance(hostProcess, dte, supportedPackageIds, installationPath);
        }

        private static IEnumerable<ISetupInstance> EnumerateVisualStudioInstances()
        {
            var setupConfiguration = new SetupConfiguration();

            var instanceEnumerator = setupConfiguration.EnumAllInstances();
            var instances = new ISetupInstance[3];

            instanceEnumerator.Next(instances.Length, instances, out var instancesFetched);

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
            var vsInstallDir = Environment.GetEnvironmentVariable("VSInstallDir");
            var haveVsInstallDir = !string.IsNullOrEmpty(vsInstallDir);

            if (haveVsInstallDir)
            {
                vsInstallDir = Path.GetFullPath(vsInstallDir);
                vsInstallDir = vsInstallDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                Debug.WriteLine($"An environment variable named 'VSInstallDir' was found, adding this to the specified requirements. (VSInstallDir: {vsInstallDir})");
            }

            var instances = EnumerateVisualStudioInstances().Where((instance) => {
                var isMatch = true;
                {
                    isMatch &= instance.GetInstallationVersion().StartsWith(VsProductVersion);

                    if (haveVsInstallDir)
                    {
                        var installationPath = instance.GetInstallationPath();
                        installationPath = Path.GetFullPath(installationPath);
                        installationPath = installationPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        isMatch &= installationPath.Equals(vsInstallDir, StringComparison.OrdinalIgnoreCase);
                    }
                }
                return isMatch;
            });

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
                                "There were no instances of Visual Studio found that match the specified requirements.");
        }

        private static Process StartNewVisualStudioProcess(string installationPath)
        {
            var vsExeFile = Path.Combine(installationPath, @"Common7\IDE\devenv.exe");

            // BUG: Currently building with /p:DeployExtension=true does not always cause the MEF cache to recompose...
            //      So, run clearcache and updateconfiguration to workaround https://devdiv.visualstudio.com/DevDiv/_workitems?id=385351.
            Process.Start(vsExeFile, $"/clearcache {VsLaunchArgs}").WaitForExit();
            Process.Start(vsExeFile, $"/updateconfiguration {VsLaunchArgs}").WaitForExit();
            Process.Start(vsExeFile, $"/resetsettings General.vssettings /command \"File.Exit\" {VsLaunchArgs}").WaitForExit();

            // Make sure we kill any leftover processes spawned by the host
            IntegrationHelper.KillProcess("DbgCLR");
            IntegrationHelper.KillProcess("VsJITDebugger");
            IntegrationHelper.KillProcess("dexplore");

            var process = Process.Start(vsExeFile, VsLaunchArgs);
            Debug.WriteLine($"Launched a new instance of Visual Studio. (ID: {process.Id})");

            return process;
        }

        private static string GetAssemblyDirectory()
        {
            var assemblyPath = typeof(VisualStudioInstanceFactory).Assembly.Location;
            return Path.GetDirectoryName(assemblyPath);
        }

        public void Dispose()
        {
            _currentlyRunningInstance?.Close();
            _currentlyRunningInstance = null;

            // We want to make sure everybody cleaned up their contexts by the end of everything
            ThrowExceptionIfAlreadyHasActiveContext();

            AppDomain.CurrentDomain.FirstChanceException -= FirstChanceExceptionHandler;
            AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolveHandler;
        }
    }
}
