// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.Win32;

using DteProject = EnvDTE.Project;
using Process = System.Diagnostics.Process;

namespace Roslyn.VisualStudio.Test.Utilities
{
    public class IntegrationHost : IDisposable
    {
        internal static readonly string VsProductVersion = Settings.Default.VsProductVersion;
        internal static readonly string VsProgId = $"VisualStudio.DTE.{VsProductVersion}";

        internal static readonly string VsRegistryRoot = Path.Combine("Software", "Microsoft", "VisualStudio", VsProductVersion);
        internal static readonly string VsConfigRootSubKey = $"{VsRegistryRoot}_Config";
        internal static readonly string VsInitConfigSubKey = Path.Combine(VsConfigRootSubKey, "Initialization");

        internal static readonly string VsCommon7Folder = Path.GetFullPath(IntegrationHelper.OpenRegistryKey(Registry.LocalMachine, VsRegistryRoot).GetValue("InstallDir").ToString());
        internal static readonly string VsUserFilesFolder = Path.GetFullPath(IntegrationHelper.OpenRegistryKey(Registry.CurrentUser, VsInitConfigSubKey).GetValue("UserFilesFolder").ToString());

        internal static readonly string VsExeFile = Path.Combine(VsCommon7Folder, "devenv.exe");
        internal static readonly string VsLaunchArgs = $"{(string.IsNullOrWhiteSpace(Settings.Default.VsRootSuffix) ? "/log" : $"/rootsuffix {Settings.Default.VsRootSuffix}")} /log";

        internal static readonly string VsStartServiceCommand = "Tools.StartIntegrationTestService";
        internal static readonly string VsStopServiceCommand = "Tools.StopIntegrationTestService";

        private string _serviceUri;
        private DTE _dte;
        private Process _hostProcess;
        private IntegrationService _service;
        private IpcClientChannel _serviceChannel;
        private bool _requireNewInstance;

        static IntegrationHost()
        {
            // Enable TraceListenerLogging here since we can have multiple instances of IntegrationHost created per process, but this is a one-time setup
            IntegrationLog.Current.EnableTraceListenerLogging();
        }

        public IntegrationHost() { }

        ~IntegrationHost()
        {
            Dispose(false);
        }

        public DTE Dte
            => _dte;

        public bool RequireNewInstance
            => (_hostProcess == null) || _hostProcess.HasExited || _requireNewInstance;

        public void Cleanup()
        {
            CleanupDte();

            if (RequireNewInstance)
            {
                IntegrationLog.Current.WriteLine("Closing existing Visual Studio instance.");

                CleanupRemotingService();
                CleanupHostProcess();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            // Explicitly don't call GC.SuppressFinalize() so that the finalizer is still called and the _hostProcess is killed
        }

        public void Initialize()
        {
            Cleanup();

            if (RequireNewInstance)
            {
                IntegrationLog.Current.WriteLine("Starting a new instance of Visual Studio.");

                InitializeHostProcess();
                InitializeDte();
                InitializeRemotingService();
            }
        }

        internal async Task ExecuteDteCommandAsync(string command, string args = "")
        {
            await WaitForDteCommandAvailabilityAsync(command).ConfigureAwait(continueOnCapturedContext: false);
            _dte.ExecuteCommand(command, args);
        }

        internal T ExecuteOnHostProcess<T>(Type type, string methodName, BindingFlags bindingFlags, params object[] parameters)
             => ExecuteOnHostProcess<T>(type.Assembly.Location, type.FullName, methodName, bindingFlags, parameters);

        internal T ExecuteOnHostProcess<T>(string assemblyFilePath, string typeFullName, string methodName, BindingFlags bindingFlags, params object[] parameters)
        {
            var objectUri = _service.Execute(assemblyFilePath, typeFullName, methodName, bindingFlags, parameters);

            if (string.IsNullOrWhiteSpace(objectUri))
            {
                return default(T);
            }

            return (T)(Activator.GetObject(typeof(T), $"{_serviceUri}/{objectUri}"));
        }

        internal async Task WaitForDteCommandAvailabilityAsync(string command)
            => await IntegrationHelper.WaitForResultAsync(() => Dte.Commands.Item(command).IsAvailable, expectedResult: true).ConfigureAwait(continueOnCapturedContext: false);

        protected virtual void Dispose(bool disposing)
        {
            _requireNewInstance |= (!disposing);
            Cleanup();
        }

        private void CleanupDte()
        {
            // DTE can still cause a failure or crash during cleanup
            try
            {
                if (_dte == null)
                {
                    return;
                }

                _dte.Documents.CloseAll(vsSaveChanges.vsSaveChangesNo);

                if (_dte.Solution != null)
                {
                    // Save the full path to each project in the solution. This is so we can cleanup any folders after the solution is closed.
                    var projectFiles = new List<string>();

                    foreach (DteProject project in _dte.Solution.Projects)
                    {
                        projectFiles.Add(project.FullName);
                    }

                    // Save the full path to the solution. This is so we can cleanup any folders after the solution is closed.
                    var solutionFile = _dte.Solution.FullName;

                    _dte.Solution.Close(SaveFirst: false);

                    foreach (var projectFile in projectFiles)
                    {
                        var projectFolder = Path.GetDirectoryName(projectFile);
                        _requireNewInstance |= IntegrationHelper.TryDeleteDirectoryRecursively(projectFolder);
                    }

                    if (!string.IsNullOrWhiteSpace(solutionFile))
                    {
                        var solutionFolder = Path.GetDirectoryName(solutionFile);
                        _requireNewInstance |= IntegrationHelper.TryDeleteDirectoryRecursively(solutionFolder);
                    }
                }
            }
            catch (Exception e)
            {
                IntegrationLog.Current.Warning($"Failed to cleanup the DTE.");
                IntegrationLog.Current.WriteLine($"\t{e}");
                _requireNewInstance |= true;
            }
        }

        private void CleanupHostProcess()
        {
            if (_dte != null)
            {
                _dte.Quit();
                _dte = null;
            }

            IntegrationHelper.KillProcess(_hostProcess);
            _hostProcess = null;
        }

        private void CleanupRemotingService()
        {
            try
            {
                if ((_dte != null) && (!_dte.Commands.Item(VsStartServiceCommand).IsAvailable))
                {
                    ExecuteDteCommandAsync(VsStopServiceCommand).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter().GetResult();
                }
            }
            finally
            {
                _service = null;
                _serviceUri = null;

                if (_serviceChannel != null)
                {
                    ChannelServices.UnregisterChannel(_serviceChannel);
                    _serviceChannel = null;
                }
            }
        }

        private void InitializeDte()
            => IntegrationHelper.WaitForResultAsync(() => {
                _dte = IntegrationHelper.LocateDteForProcess(_hostProcess);
                return (_dte != null);
            }, expectedResult: true).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter().GetResult();

        private void InitializeHostProcess()
        {
            Process.Start(VsExeFile, $"/clearcache {VsLaunchArgs}").WaitForExit();
            Process.Start(VsExeFile, $"/updateconfiguration {VsLaunchArgs}").WaitForExit();

            IntegrationHelper.KillProcess("DbgCLR");
            IntegrationHelper.KillProcess("VsJITDebugger");
            IntegrationHelper.KillProcess("dexplore");

            _hostProcess = Process.Start(VsExeFile, VsLaunchArgs);
            IntegrationLog.Current.WriteLine($"Launched a new instance of Visual Studio. (ID: {_hostProcess.Id})");
        }

        private void InitializeRemotingService()
        {
            ExecuteDteCommandAsync("Tools.StartIntegrationTestService").ConfigureAwait(continueOnCapturedContext: false).GetAwaiter().GetResult();

            _serviceChannel = new IpcClientChannel();
            ChannelServices.RegisterChannel(_serviceChannel, ensureSecurity: true);

            _serviceUri = string.Format($"ipc://{IntegrationService.PortNameFormatString}", _hostProcess.Id);
            _service = (IntegrationService)(Activator.GetObject(typeof(IntegrationService), $"{_serviceUri}/{typeof(IntegrationService).FullName}"));
        }
    }
}
