// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Microsoft.Win32;
using Roslyn.Utilities;
using RunTests;
using Xunit;
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
        private VisualStudioInstance? _currentlyRunningInstance;

        /// <summary>
        /// Identifies the first time a Visual Studio instance is launched during an integration test run.
        /// </summary>
        private static bool _firstLaunch = true;

        public VisualStudioInstanceFactory()
        {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolveHandler;
            AppDomain.CurrentDomain.FirstChanceException += FirstChanceExceptionHandler;

            var majorVsProductVersion = VsProductVersion.Split('.')[0];
            if (int.Parse(majorVsProductVersion) < 17)
            {
                throw new PlatformNotSupportedException("The Visual Studio Integration Test Framework is only supported on Visual Studio 17.0 and later.");
            }
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
                var logDir = Path.Combine(assemblyDirectory, "TestResults", "Screenshots");
                var baseFileName = $"{DateTime.UtcNow:HH.mm.ss}-{testName}-{eventArgs.Exception.GetType().Name}";

                var maxLength = logDir.Length + 1 + baseFileName.Length + ".Watson.log".Length + 1;
                const int MaxPath = 260;
                if (maxLength > MaxPath)
                {
                    testName = testName.Substring(0, testName.Length - (maxLength - MaxPath));
                    baseFileName = $"{DateTime.UtcNow:HH.mm.ss}-{testName}-{eventArgs.Exception.GetType().Name}";
                }

                Directory.CreateDirectory(logDir);

                File.WriteAllText(Path.Combine(logDir, $"{baseFileName}.log"), eventArgs.Exception.ToString());

                ActivityLogCollector.TryWriteActivityLogToFile(Path.Combine(logDir, $"{baseFileName}.Actvty.log"));
                EventLogCollector.TryWriteDotNetEntriesToFile(Path.Combine(logDir, $"{baseFileName}.DotNet.log"));
                EventLogCollector.TryWriteWatsonEntriesToFile(Path.Combine(logDir, $"{baseFileName}.Watson.log"));

                ScreenshotService.TakeScreenshot(Path.Combine(logDir, $"{baseFileName}.png"));
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
        private static Assembly? AssemblyResolveHandler(object sender, ResolveEventArgs eventArgs)
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
            try
            {
                var shouldStartNewInstance = ShouldStartNewInstance(requiredPackageIds);
                await UpdateCurrentlyRunningInstanceAsync(requiredPackageIds, shouldStartNewInstance).ConfigureAwait(true);
                Contract.ThrowIfNull(_currentlyRunningInstance);

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

        /// <summary>
        /// Starts up a new <see cref="VisualStudioInstance"/>, shutting down any instances that are already running.
        /// </summary>
        private async Task UpdateCurrentlyRunningInstanceAsync(ImmutableHashSet<string> requiredPackageIds, bool shouldStartNewInstance)
        {
            Process hostProcess;
            DTE dte;
            ImmutableHashSet<string> supportedPackageIds;
            string installationPath;

            var isUsingLspEditor = IsUsingLspEditor();

            if (shouldStartNewInstance)
            {
                // We are starting a new instance, so ensure we close the currently running instance, if it exists
                _currentlyRunningInstance?.Close();

                var instance = (ISetupInstance2)LocateVisualStudioInstance(requiredPackageIds);
                supportedPackageIds = ImmutableHashSet.CreateRange(instance.GetPackages().Select((supportedPackage) => supportedPackage.GetId()));
                installationPath = instance.GetInstallationPath();

                var instanceVersion = instance.GetInstallationVersion();
                var majorVersion = int.Parse(instanceVersion.Substring(0, instanceVersion.IndexOf('.')));
                hostProcess = StartNewVisualStudioProcess(installationPath, majorVersion, isUsingLspEditor);

                // We wait until the DTE instance is up before we're good
                dte = await IntegrationHelper.WaitForNotNullAsync(() => IntegrationHelper.TryLocateDteForProcess(hostProcess)).ConfigureAwait(true);
            }
            else
            {
                // We are going to reuse the currently running instance, so ensure that we grab the host Process and DTE
                // before cleaning up any hooks or remoting services created by the previous instance. We will then
                // create a new VisualStudioInstance from the previous to ensure that everything is in a 'clean' state.
                //
                // We create a new DTE instance in the current context since the COM object could have been separated
                // from its RCW during the previous test.

                Contract.ThrowIfNull(_currentlyRunningInstance);

                hostProcess = _currentlyRunningInstance.HostProcess;
                dte = await IntegrationHelper.WaitForNotNullAsync(() => IntegrationHelper.TryLocateDteForProcess(hostProcess)).ConfigureAwait(true);
                supportedPackageIds = _currentlyRunningInstance.SupportedPackageIds;
                installationPath = _currentlyRunningInstance.InstallationPath;

                _currentlyRunningInstance.Close(exitHostProcess: false);
            }

            _currentlyRunningInstance = new VisualStudioInstance(hostProcess, dte, supportedPackageIds, installationPath, isUsingLspEditor);
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
                    if (haveVsInstallDir)
                    {
                        var installationPath = instance.GetInstallationPath();
                        installationPath = Path.GetFullPath(installationPath);
                        installationPath = installationPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        isMatch &= installationPath.Equals(vsInstallDir, StringComparison.OrdinalIgnoreCase);
                    }
                    else
                    {
                        isMatch &= instance.GetInstallationVersion().StartsWith(VsProductVersion);
                    }
                }

                return isMatch;
            });

            var messages = new List<string>();

            foreach (ISetupInstance2 instance in instances)
            {
                var instancePackagesIds = instance.GetPackages().Select(p => p.GetId()).ToHashSet();
                var missingPackageIds = requiredPackageIds.Where(p => !instancePackagesIds.Contains(p)).ToList();

                if (missingPackageIds.Count > 0)
                {
                    messages.Add($"An instance of {instance.GetDisplayName()} at {instance.GetInstallationPath()} was found but was missing these packages: " +
                        string.Join(", ", missingPackageIds));
                    continue;
                }

                const InstanceState minimumRequiredState = InstanceState.Local | InstanceState.Registered;

                var state = instance.GetState();

                if ((state & minimumRequiredState) != minimumRequiredState)
                {
                    messages.Add($"An instance of {instance.GetDisplayName()} at {instance.GetInstallationPath()} matched the specified requirements but had an invalid state. (State: {state})");
                    continue;
                }

                return instance;
            }

            throw new Exception(string.Join(Environment.NewLine, messages));
        }

        private static Process StartNewVisualStudioProcess(string installationPath, int majorVersion, bool isUsingLspEditor)
        {
            var vsExeFile = Path.Combine(installationPath, @"Common7\IDE\devenv.exe");
            var vsRegEditExeFile = Path.Combine(installationPath, @"Common7\IDE\VsRegEdit.exe");

            if (_firstLaunch)
            {
                if (majorVersion >= 16)
                {
                    // Make sure the start window doesn't show on launch
                    Process.Start(CreateSilentStartInfo(vsRegEditExeFile, $"set \"{installationPath}\" {Settings.Default.VsRootSuffix} HKCU General OnEnvironmentStartup dword 10")).WaitForExit();
                }

                // BUG: Currently building with /p:DeployExtension=true does not always cause the MEF cache to recompose...
                //      So, run clearcache and updateconfiguration to workaround https://devdiv.visualstudio.com/DevDiv/_workitems?id=385351.
                Process.Start(CreateSilentStartInfo(vsExeFile, $"/clearcache {VsLaunchArgs}")).WaitForExit();
                Process.Start(CreateSilentStartInfo(vsExeFile, $"/updateconfiguration {VsLaunchArgs}")).WaitForExit();
                Process.Start(CreateSilentStartInfo(vsExeFile, $"/resetsettings General.vssettings /command \"File.Exit\" {VsLaunchArgs}")).WaitForExit();

                // Disable roaming settings to avoid interference from the online user profile
                Process.Start(CreateSilentStartInfo(vsRegEditExeFile, $"set \"{installationPath}\" {Settings.Default.VsRootSuffix} HKCU \"ApplicationPrivateSettings\\Microsoft\\VisualStudio\" RoamingEnabled string \"1*System.Boolean*False\"")).WaitForExit();

                // Disable IntelliCode line completions to avoid interference with argument completion testing
                Process.Start(CreateSilentStartInfo(vsRegEditExeFile, $"set \"{installationPath}\" {Settings.Default.VsRootSuffix} HKCU \"ApplicationPrivateSettings\\Microsoft\\VisualStudio\\IntelliCode\" wholeLineCompletions string \"0*System.Int32*2\"")).WaitForExit();

                // Disable IntelliCode RepositoryAttachedModels since it requires authentication which can fail in CI
                Process.Start(CreateSilentStartInfo(vsRegEditExeFile, $"set \"{installationPath}\" {Settings.Default.VsRootSuffix} HKCU \"ApplicationPrivateSettings\\Microsoft\\VisualStudio\\IntelliCode\" repositoryAttachedModels string \"0*System.Int32*2\"")).WaitForExit();

                // Disable background download UI to avoid toasts
                Process.Start(CreateSilentStartInfo(vsRegEditExeFile, $"set \"{installationPath}\" {Settings.Default.VsRootSuffix} HKCU \"FeatureFlags\\Setup\\BackgroundDownload\" Value dword 0")).WaitForExit();

                var lspRegistryValue = isUsingLspEditor ? "1" : "0";
                Process.Start(CreateSilentStartInfo(vsRegEditExeFile, $"set \"{installationPath}\" {Settings.Default.VsRootSuffix} HKCU \"FeatureFlags\\Roslyn\\LSP\\Editor\" Value dword {lspRegistryValue}")).WaitForExit();
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\Telemetry\Channels", "fileLogger", 1, RegistryValueKind.DWord);

                // Remove legacy experiment setting for controlling async completion to ensure it does not interfere.
                // We no longer set this value, but it could be in place from an earlier test run on the same machine.
                var disabledFlights = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\ABExp\LocalTest", "DisabledFlights", Array.Empty<string>()) as string[] ?? Array.Empty<string>();
                if (disabledFlights.Any(flight => string.Equals(flight, "completionapi", StringComparison.OrdinalIgnoreCase)))
                {
                    disabledFlights = disabledFlights.Where(flight => !string.Equals(flight, "completionapi", StringComparison.OrdinalIgnoreCase)).ToArray();
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\ABExp\LocalTest", "DisabledFlights", disabledFlights, RegistryValueKind.MultiString);
                }

                // Disable text editor error reporting because it pops up a dialog. We want to either fail fast in our
                // custom handler or fail silently and continue testing.
                Process.Start(CreateSilentStartInfo(vsRegEditExeFile, $"set \"{installationPath}\" {Settings.Default.VsRootSuffix} HKCU \"Text Editor\" \"Report Exceptions\" dword 0")).WaitForExit();

                // Configure RemoteHostOptions.OOP64Bit for testing
                if (string.Equals(Environment.GetEnvironmentVariable("ROSLYN_OOP64BIT"), "false", StringComparison.OrdinalIgnoreCase))
                {
                    Process.Start(CreateSilentStartInfo(vsRegEditExeFile, $"set \"{installationPath}\" {Settings.Default.VsRootSuffix} HKCU \"Roslyn\\Internal\\OnOff\\Features\" OOP64Bit dword 0")).WaitForExit();
                }
                else
                {
                    Process.Start(CreateSilentStartInfo(vsRegEditExeFile, $"set \"{installationPath}\" {Settings.Default.VsRootSuffix} HKCU \"Roslyn\\Internal\\OnOff\\Features\" OOP64Bit dword 1")).WaitForExit();
                }

                // Configure RemoteHostOptions.OOPCoreClrFeatureFlag for testing
                if (string.Equals(Environment.GetEnvironmentVariable("ROSLYN_OOPCORECLR"), "true", StringComparison.OrdinalIgnoreCase))
                {
                    Process.Start(CreateSilentStartInfo(vsRegEditExeFile, $"set \"{installationPath}\" {Settings.Default.VsRootSuffix} HKCU \"FeatureFlags\\Roslyn\\ServiceHubCore\" Value dword 1")).WaitForExit();
                }
                else
                {
                    Process.Start(CreateSilentStartInfo(vsRegEditExeFile, $"set \"{installationPath}\" {Settings.Default.VsRootSuffix} HKCU \"FeatureFlags\\Roslyn\\ServiceHubCore\" Value dword 0")).WaitForExit();
                }

                _firstLaunch = false;
            }

            // Make sure we kill any leftover processes spawned by the host
            IntegrationHelper.KillProcess("DbgCLR");
            IntegrationHelper.KillProcess("VsJITDebugger");
            IntegrationHelper.KillProcess("dexplore");

            var processStartInfo = new ProcessStartInfo(vsExeFile, VsLaunchArgs) { UseShellExecute = false };

            // Clear variables set by CI builds which are known to affect IDE behavior. Integration tests should show
            // correct behavior for default IDE installations, without Roslyn-, Arcade-, or Azure Pipelines-specific
            // influences.
            processStartInfo.Environment.Remove("DOTNET_MULTILEVEL_LOOKUP");
            processStartInfo.Environment.Remove("DOTNET_INSTALL_DIR");
            processStartInfo.Environment.Remove("DotNetRoot");
            processStartInfo.Environment.Remove("DotNetTool");

            if (isUsingLspEditor)
            {
                // When running under the LSP editor set logging to verbose to ensure LSP client logs are captured.
                processStartInfo.Environment.Add("LogLevel", "Verbose");
            }

            // The first element of the path in CI is a .dotnet used for the Roslyn build. Make sure to remove that.
            if (processStartInfo.Environment.TryGetValue("BUILD_SOURCESDIRECTORY", out var sourcesDirectory))
            {
                var environmentPath = processStartInfo.Environment["PATH"];

                // Assert that the PATH still has the form we are expecting since we're about to modify it
                var firstPath = environmentPath.Substring(0, environmentPath.IndexOf(';'));
                Assert.Equal(Path.Combine(sourcesDirectory, ".dotnet") + '\\', firstPath);

                // Drop the first path element
                processStartInfo.Environment["PATH"] = environmentPath.Substring(environmentPath.IndexOf(';') + 1);
            }

            var process = Process.Start(processStartInfo);
            Debug.WriteLine($"Launched a new instance of Visual Studio. (ID: {process.Id})");

            return process;

            static ProcessStartInfo CreateSilentStartInfo(string fileName, string arguments)
            {
                return new ProcessStartInfo(fileName, arguments) { CreateNoWindow = true, UseShellExecute = false };
            }
        }

        private static string GetAssemblyDirectory()
        {
            var assemblyPath = typeof(VisualStudioInstanceFactory).Assembly.Location;
            return Path.GetDirectoryName(assemblyPath);
        }

        private static bool IsUsingLspEditor()
        {
            return string.Equals(Environment.GetEnvironmentVariable("ROSLYN_LSPEDITOR"), "true", StringComparison.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            _currentlyRunningInstance?.Close();
            _currentlyRunningInstance = null;

            AppDomain.CurrentDomain.FirstChanceException -= FirstChanceExceptionHandler;
            AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolveHandler;
        }
    }
}
