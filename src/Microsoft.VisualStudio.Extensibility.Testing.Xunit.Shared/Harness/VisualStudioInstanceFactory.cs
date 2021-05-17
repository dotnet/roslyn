// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Harness
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Setup.Configuration;
    using Microsoft.Win32;
    using DTE = EnvDTE.DTE;
    using File = System.IO.File;
    using Path = System.IO.Path;

    internal sealed class VisualStudioInstanceFactory : MarshalByRefObject, IDisposable
    {
        public static readonly string VsLaunchArgs = $"{(string.IsNullOrWhiteSpace(Settings.Default.VsRootSuffix) ? "/log" : $"/rootsuffix {Settings.Default.VsRootSuffix}")} /log";

        private static readonly Dictionary<Version, Assembly> _installerAssemblies = new Dictionary<Version, Assembly>();

        /// <summary>
        /// The instance that has already been launched by this factory and can be reused.
        /// </summary>
        private VisualStudioInstance _currentlyRunningInstance;

        private bool _hasCurrentlyActiveContext;

        public VisualStudioInstanceFactory()
        {
            if (Process.GetCurrentProcess().ProcessName != "devenv")
            {
                AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolveHandler;
            }
        }

        // This looks like it is pointless (since we are returning an assembly that is already loaded) but it is actually required.
        // The BinaryFormatter, when invoking 'HandleReturnMessage', will end up attempting to call 'BinaryAssemblyInfo.GetAssembly()',
        // which will itself attempt to call 'Assembly.Load()' using the full name of the assembly for the type that is being deserialized.
        // Depending on the manner in which the assembly was originally loaded, this may end up actually trying to load the assembly a second
        // time and it can fail if the standard assembly resolution logic fails. This ensures that we 'succeed' this secondary load by returning
        // the assembly that is already loaded.
        internal static Assembly AssemblyResolveHandler(object sender, ResolveEventArgs eventArgs)
        {
            Debug.WriteLine($"'{eventArgs.RequestingAssembly}' is attempting to resolve '{eventArgs.Name}'");
            var resolvedAssembly = AppDomain.CurrentDomain.GetAssemblies().Where((assembly) => assembly.FullName.Equals(eventArgs.Name)).SingleOrDefault();

            if (resolvedAssembly != null)
            {
                Debug.WriteLine("The assembly was already loaded!");
            }

            // Support resolving embedded assemblies
            using (var assemblyStream = typeof(VisualStudioInstanceFactory).Assembly.GetManifestResourceStream(new AssemblyName(eventArgs.Name).Name + ".dll"))
            using (var memoryStream = new MemoryStream())
            {
                if (assemblyStream != null)
                {
                    assemblyStream.CopyTo(memoryStream);
                    return Assembly.Load(memoryStream.ToArray());
                }
            }

            return resolvedAssembly;
        }

        /// <summary>
        /// Returns a <see cref="VisualStudioInstanceContext"/>, starting a new instance of Visual Studio if necessary.
        /// </summary>
        public async Task<VisualStudioInstanceContext> GetNewOrUsedInstanceAsync(Version version, ImmutableList<string> extensionFiles, ImmutableHashSet<string> requiredPackageIds)
        {
            ThrowExceptionIfAlreadyHasActiveContext();

            bool shouldStartNewInstance = ShouldStartNewInstance(version, requiredPackageIds);
            await UpdateCurrentlyRunningInstanceAsync(version, extensionFiles, requiredPackageIds, shouldStartNewInstance).ConfigureAwait(false);

            return new VisualStudioInstanceContext(_currentlyRunningInstance, this);
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

        private bool ShouldStartNewInstance(Version version, ImmutableHashSet<string> requiredPackageIds)
        {
            // We need to start a new instance if:
            //  * The current instance does not exist -or-
            //  * The current instance is not the correct version -or-
            //  * The current instance does not support all the required packages -or-
            //  * The current instance is no longer running
            return _currentlyRunningInstance == null
                || _currentlyRunningInstance.Version.Major != version.Major
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
        private async Task UpdateCurrentlyRunningInstanceAsync(Version version, ImmutableList<string> extensionFiles, ImmutableHashSet<string> requiredPackageIds, bool shouldStartNewInstance)
        {
            Process hostProcess;
            DTE dte;
            Version actualVersion;
            ImmutableHashSet<string> supportedPackageIds;
            string installationPath;

            if (shouldStartNewInstance)
            {
                // We are starting a new instance, so ensure we close the currently running instance, if it exists
                _currentlyRunningInstance?.Close();

                var instance = LocateVisualStudioInstance(version, requiredPackageIds);
                supportedPackageIds = instance.Item3;
                installationPath = instance.Item1;
                actualVersion = instance.Item2;

                hostProcess = StartNewVisualStudioProcess(installationPath, version, extensionFiles);

                // We wait until the DTE instance is up before we're good
                dte = await IntegrationHelper.WaitForNotNullAsync(() => IntegrationHelper.TryLocateDteForProcess(hostProcess)).ConfigureAwait(true);
            }
            else
            {
                // We are going to reuse the currently running instance, so ensure that we grab the host Process and Dte
                // before cleaning up any hooks or remoting services created by the previous instance. We will then
                // create a new VisualStudioInstance from the previous to ensure that everything is in a 'clean' state.
                //
                // We create a new DTE instance in the current context since the COM object could have been separated
                // from its RCW during the previous test.
                Debug.Assert(_currentlyRunningInstance != null, "Assertion failed: _currentlyRunningInstance != null");

                hostProcess = _currentlyRunningInstance.HostProcess;
                dte = await IntegrationHelper.WaitForNotNullAsync(() => IntegrationHelper.TryLocateDteForProcess(hostProcess)).ConfigureAwait(true);
                actualVersion = _currentlyRunningInstance.Version;
                supportedPackageIds = _currentlyRunningInstance.SupportedPackageIds;
                installationPath = _currentlyRunningInstance.InstallationPath;

                _currentlyRunningInstance.Close(exitHostProcess: false);
            }

            _currentlyRunningInstance = new VisualStudioInstance(hostProcess, dte, actualVersion, supportedPackageIds, installationPath);
            if (shouldStartNewInstance)
            {
                var harnessAssemblyDirectory = Path.GetDirectoryName(typeof(VisualStudioInstanceFactory).Assembly.CodeBase);
                if (harnessAssemblyDirectory.StartsWith("file:"))
                {
                    harnessAssemblyDirectory = new Uri(harnessAssemblyDirectory).LocalPath;
                }

                _currentlyRunningInstance.AddCodeBaseDirectory(harnessAssemblyDirectory);
            }
        }

        private static IEnumerable<Tuple<string, Version, ImmutableHashSet<string>, InstanceState>> EnumerateVisualStudioInstances()
        {
            foreach (var result in EnumerateVisualStudioInstancesInRegistry())
            {
                yield return Tuple.Create(result.Item1, result.Item2, ImmutableHashSet.Create<string>(), InstanceState.Local | InstanceState.Registered | InstanceState.NoErrors | InstanceState.NoRebootRequired);
            }

            foreach (ISetupInstance2 result in EnumerateVisualStudioInstancesViaInstaller())
            {
                var productDir = Path.GetFullPath(result.GetInstallationPath());
                var version = Version.Parse(result.GetInstallationVersion());
                var supportedPackageIds = ImmutableHashSet.CreateRange(result.GetPackages().Select(package => package.GetId()));
                yield return Tuple.Create(productDir, version, supportedPackageIds, result.GetState());
            }
        }

        private static IEnumerable<Tuple<string, Version>> EnumerateVisualStudioInstancesInRegistry()
        {
            using (var software = Registry.LocalMachine.OpenSubKey("SOFTWARE"))
            using (var microsoft = software.OpenSubKey("Microsoft"))
            using (var visualStudio = microsoft.OpenSubKey("VisualStudio"))
            {
                foreach (string versionKey in visualStudio.GetSubKeyNames())
                {
                    if (!Version.TryParse(versionKey, out var version))
                    {
                        continue;
                    }

                    string path = Registry.GetValue($@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\{versionKey}\Setup\VS", "ProductDir", null) as string;
                    if (string.IsNullOrEmpty(path) || !File.Exists(Path.Combine(path, @"Common7\IDE\devenv.exe")))
                    {
                        continue;
                    }

                    yield return Tuple.Create(path, version);
                }
            }
        }

        private static IEnumerable<ISetupInstance> EnumerateVisualStudioInstancesViaInstaller()
        {
            var setupConfiguration = new SetupConfiguration();

            var instanceEnumerator = setupConfiguration.EnumAllInstances();
            var instances = new ISetupInstance[3];

            instanceEnumerator.Next(instances.Length, instances, out var instancesFetched);

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

        private static Tuple<string, Version, ImmutableHashSet<string>, InstanceState> LocateVisualStudioInstance(Version version, ImmutableHashSet<string> requiredPackageIds)
        {
            var vsInstallDir = Environment.GetEnvironmentVariable("__UNITTESTEXPLORER_VSINSTALLPATH__")
                ?? Environment.GetEnvironmentVariable("VSAPPIDDIR");
            if (vsInstallDir != null)
            {
                vsInstallDir = Path.GetFullPath(Path.Combine(vsInstallDir, @"..\.."));
            }
            else
            {
                vsInstallDir = Environment.GetEnvironmentVariable("VSInstallDir");
            }

            var haveVsInstallDir = !string.IsNullOrEmpty(vsInstallDir);

            if (haveVsInstallDir)
            {
                vsInstallDir = Path.GetFullPath(vsInstallDir);
                vsInstallDir = vsInstallDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                Debug.WriteLine($"An environment variable named 'VSInstallDir' (or equivalent) was found, adding this to the specified requirements. (VSInstallDir: {vsInstallDir})");
            }

            var instances = EnumerateVisualStudioInstances().Where((instance) =>
            {
                var isMatch = true;
                {
                    isMatch &= version.Major == instance.Item2.Major;
                    isMatch &= instance.Item2 >= version;

                    if (haveVsInstallDir && version.Major >= 15)
                    {
                        var installationPath = instance.Item1;
                        installationPath = Path.GetFullPath(installationPath);
                        installationPath = installationPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        isMatch &= installationPath.Equals(vsInstallDir, StringComparison.OrdinalIgnoreCase);
                    }
                }

                return isMatch;
            });

            var instanceFoundWithInvalidState = false;

            foreach (var instance in instances)
            {
                var packages = instance.Item3.Where(package => requiredPackageIds.Contains(package));
                if (packages.Count() != requiredPackageIds.Count())
                {
                    continue;
                }

                const InstanceState minimumRequiredState = InstanceState.Local | InstanceState.Registered;

                var state = instance.Item4;

                if ((state & minimumRequiredState) == minimumRequiredState)
                {
                    return instance;
                }

                Debug.WriteLine($"An instance matching the specified requirements but had an invalid state. (State: {state})");
                instanceFoundWithInvalidState = true;
            }

            throw new PlatformNotSupportedException(instanceFoundWithInvalidState ?
                                "An instance matching the specified requirements was found but it was in an invalid state." :
                                "There were no instances of Visual Studio found that match the specified requirements.");
        }

        private static Process StartNewVisualStudioProcess(string installationPath, Version version, ImmutableList<string> extensionFiles)
        {
            var vsExeFile = Path.Combine(installationPath, @"Common7\IDE\devenv.exe");
            var vsRegEditExeFile = Path.Combine(installationPath, @"Common7\IDE\VsRegEdit.exe");

            var temporaryFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Assert.False(Directory.Exists(temporaryFolder));
            Directory.CreateDirectory(temporaryFolder);

            var installerAssemblyPath = ExtractInstallerAssembly(version, temporaryFolder);

            var integrationTestServiceExtension = ExtractIntegrationTestServiceExtension(temporaryFolder);
            var extensions = extensionFiles.Add(integrationTestServiceExtension);
            var rootSuffix = Settings.Default.VsRootSuffix;

            try
            {
                var arguments = string.Join(
                    " ",
                    rootSuffix,
                    $"\"{installationPath}\"",
                    string.Join(" ", extensions.Select(extension => $"\"{extension}\"")));
                Process.Start(CreateSilentStartInfo(installerAssemblyPath, arguments)).WaitForExit();
            }
            finally
            {
                File.Delete(integrationTestServiceExtension);
                Directory.Delete(temporaryFolder, recursive: true);
            }

            if (version.Major >= 16)
            {
                // Make sure the start window doesn't show on launch
                Process.Start(CreateSilentStartInfo(vsRegEditExeFile, $"set \"{installationPath}\" {Settings.Default.VsRootSuffix} HKCU General OnEnvironmentStartup dword 10")).WaitForExit();
            }

            // BUG: Currently building with /p:DeployExtension=true does not always cause the MEF cache to recompose...
            //      So, run clearcache and updateconfiguration to workaround https://devdiv.visualstudio.com/DevDiv/_workitems?id=385351.
            if (version.Major >= 12)
            {
                Process.Start(CreateSilentStartInfo(vsExeFile, $"/clearcache {VsLaunchArgs}")).WaitForExit();
            }

            Process.Start(CreateSilentStartInfo(vsExeFile, $"/updateconfiguration {VsLaunchArgs}")).WaitForExit();
            Process.Start(CreateSilentStartInfo(vsExeFile, $"/resetsettings General.vssettings /command \"File.Exit\" {VsLaunchArgs}")).WaitForExit();

            // Make sure we kill any leftover processes spawned by the host
            IntegrationHelper.KillProcess("DbgCLR");
            IntegrationHelper.KillProcess("VsJITDebugger");
            IntegrationHelper.KillProcess("dexplore");

            var process = Process.Start(vsExeFile, VsLaunchArgs);
            Debug.WriteLine($"Launched a new instance of Visual Studio. (ID: {process.Id})");

            return process;

            ProcessStartInfo CreateSilentStartInfo(string fileName, string arguments)
            {
                return new ProcessStartInfo(fileName, arguments) { CreateNoWindow = true, UseShellExecute = false };
            }
        }

        private static string ExtractInstallerAssembly(Version version, string temporaryFolder)
        {
            var installerDirectory = Path.Combine(temporaryFolder, $"{version.Major}.{version.Minor}");
            Directory.CreateDirectory(installerDirectory);

            var installerFileName = $"Microsoft.VisualStudio.VsixInstaller.{version.Major}.exe";
            var path = Path.Combine(installerDirectory, installerFileName);
            using (var resourceStream = typeof(VisualStudioInstanceFactory).Assembly.GetManifestResourceStream(installerFileName))
            using (var writerStream = File.Open(path, FileMode.CreateNew, FileAccess.Write))
            {
                resourceStream.CopyTo(writerStream);
            }

            return path;
        }

        private static string ExtractIntegrationTestServiceExtension(string temporaryFolder)
        {
            var extensionFileName = "Microsoft.VisualStudio.IntegrationTestService.vsix";
            var path = Path.Combine(temporaryFolder, extensionFileName);
            using (var resourceStream = typeof(VisualStudioInstanceFactory).Assembly.GetManifestResourceStream(extensionFileName))
            using (var writerStream = File.Open(path, FileMode.CreateNew, FileAccess.Write))
            {
                resourceStream.CopyTo(writerStream);
            }

            return path;
        }

        public void Dispose()
        {
            _currentlyRunningInstance?.Close();
            _currentlyRunningInstance = null;

            // We want to make sure everybody cleaned up their contexts by the end of everything
            ThrowExceptionIfAlreadyHasActiveContext();

            AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolveHandler;
        }
    }
}
