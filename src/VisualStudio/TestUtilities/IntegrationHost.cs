// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Threading.Tasks;
using System.Windows.Automation;
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

        internal static readonly string Wow6432Registry = Environment.Is64BitProcess ? "WOW6432Node" : string.Empty;
        internal static readonly string VsRegistryRoot = Path.Combine("SOFTWARE", Wow6432Registry, "Microsoft", "VisualStudio", VsProductVersion);
        internal static readonly string VsConfigRootSubKey = $"{VsRegistryRoot}_Config";
        internal static readonly string VsInitConfigSubKey = Path.Combine(VsConfigRootSubKey, "Initialization");

        internal static readonly string VsCommon7Folder = Path.GetFullPath(IntegrationHelper.GetRegistryKeyValue(Registry.LocalMachine, VsRegistryRoot, "InstallDir").ToString());
        internal static readonly string VsUserFilesFolder = Path.GetFullPath(IntegrationHelper.GetRegistryKeyValue(Registry.CurrentUser, VsInitConfigSubKey, "UserFilesFolder").ToString());

        internal static readonly string VsExeFile = Path.Combine(VsCommon7Folder, "devenv.exe");
        internal static readonly string VsLaunchArgs = $"{(string.IsNullOrWhiteSpace(Settings.Default.VsRootSuffix) ? "/log" : $"/rootsuffix {Settings.Default.VsRootSuffix}")} /log";

        internal static readonly string VsStartServiceCommand = "Tools.StartIntegrationTestService";
        internal static readonly string VsStopServiceCommand = "Tools.StopIntegrationTestService";

        // TODO: We could probably expose all the windows/services/features of the host process in a better manner
        private InteractiveWindow _csharpInteractiveWindow;
        private DTE _dte;
        private EditorWindow _editorWindow;
        private Process _hostProcess;
        private IntegrationService _service;
        private IpcClientChannel _serviceChannel;
        private SolutionExplorer _solutionExplorer;
        private string _serviceUri;
        private bool _requireNewInstance;

        static IntegrationHost()
        {
            // Enable TraceListenerLogging here since we can have multiple instances of IntegrationHost created per process, but this is a one-time setup
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new IntegrationTraceListener());
        }

        ~IntegrationHost()
        {
            Dispose(false);
        }

        public DTE Dte => _dte;

        public InteractiveWindow CSharpInteractiveWindow
        {
            get
            {
                if (_csharpInteractiveWindow == null)
                {
                    _csharpInteractiveWindow = InteractiveWindow.CreateCSharpInteractiveWindow(this);
                }

                return _csharpInteractiveWindow;
            }
        }

        public EditorWindow EditorWindow
        {
            get
            {
                if (_editorWindow == null)
                {
                    _editorWindow = new EditorWindow(this);
                }

                return _editorWindow;
            }
        }

        /// <summary>Gets a value that determines whether a new host process should be created.</summary>
        internal bool RequireNewInstance
            => (_hostProcess == null) || _hostProcess.HasExited || _requireNewInstance;

        public SolutionExplorer SolutionExplorer
        {
            get
            {
                if (_solutionExplorer == null)
                {
                    _solutionExplorer = new SolutionExplorer(this);
                }

                return _solutionExplorer;
            }
        }

        public async Task ClickAutomationElementAsync(string elementName, bool recursive = false)
        {
            var automationElement = await LocateAutomationElementAsync(elementName, recursive).ConfigureAwait(continueOnCapturedContext: false);

            object invokePattern = null;
            if (automationElement.TryGetCurrentPattern(InvokePattern.Pattern, out invokePattern))
            {
                ((InvokePattern)(invokePattern)).Invoke();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            // Explicitly don't call GC.SuppressFinalize() so that the finalizer is still called and the _hostProcess is killed
        }

        public void Initialize()        // TODO: We could probably improve this by moving to a factory based model, this would likely involve changes to 'RequireNewInstance' and 'Cleanup' as well
        {
            Cleanup();

            if (RequireNewInstance)
            {
                Debug.WriteLine("Starting a new instance of Visual Studio.");

                InitializeHostProcess();
                InitializeDte();
                InitializeRemotingService();
            }

            // TODO: We probably want to reset the environment to some stable/known state
        }

        internal async Task ExecuteDteCommandAsync(string command, string args = "")
        {
            // args is "" by default because thats what Dte.ExecuteCommand does by default and changing our default
            // to something more logical, like null, would change the expected behavior of Dte.ExecuteCommand

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

        internal async Task<AutomationElement> LocateAutomationElementAsync(string elementName, bool recursive = false)
        {
            AutomationElement automationElement = null;
            var scope = (recursive ? TreeScope.Descendants : TreeScope.Children);
            var condition = new PropertyCondition(AutomationElement.NameProperty, elementName);

            await IntegrationHelper.WaitForResultAsync(() => {
                automationElement = AutomationElement.RootElement.FindFirst(scope, condition);
                return (automationElement != null);
            }, expectedResult: true).ConfigureAwait(continueOnCapturedContext: false);

            return automationElement;
        }

        internal Task<Window> LocateDteWindowAsync(string windowTitle)
            => IntegrationHelper.WaitForNotNullAsync(() => {
                foreach (Window window in _dte.Windows)
                {
                    if (window.Caption.Equals(windowTitle, StringComparison.OrdinalIgnoreCase))
                    {
                        return window;
                    }
                }
                return null;
            });

        internal Task WaitForDteCommandAvailabilityAsync(string command)
            => IntegrationHelper.WaitForResultAsync(() => Dte.Commands.Item(command).IsAvailable, expectedResult: true);

        protected virtual void Dispose(bool disposing)
        {
            _requireNewInstance |= (!disposing);
            Cleanup();
        }

        private void Cleanup()
        {
            CleanupDte();

            if (RequireNewInstance)
            {
                Debug.WriteLine("Closing existing Visual Studio instance.");

                CleanupRemotingService();
                CleanupHostProcess();
            }
        }

        private void CleanupDte()
        {
            // DTE can still cause a failure or crash during cleanup, such as if cleaning up the open projects/solutions fails
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
                Debug.WriteLine($"Warning: Failed to cleanup the DTE.");
                Debug.WriteLine($"\t{e}");
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
                if ((_dte?.Commands.Item(VsStopServiceCommand).IsAvailable).GetValueOrDefault())
                {
                    ExecuteDteCommandAsync(VsStopServiceCommand).GetAwaiter().GetResult();
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
            => IntegrationHelper.WaitForNotNullAsync(() => IntegrationHelper.LocateDteForProcess(_hostProcess)).GetAwaiter().GetResult();

        private void InitializeHostProcess()
        {
            // TODO: This might not be needed anymore as I don't believe we do things which risk corrupting the MEF cache. However,
            // it is still useful to do in case some other action corruped the MEF cache as we don't have to restart the host
            Process.Start(VsExeFile, $"/clearcache {VsLaunchArgs}").WaitForExit();
            Process.Start(VsExeFile, $"/updateconfiguration {VsLaunchArgs}").WaitForExit();

            // Make sure we kill any leftover processes spawned by the host
            IntegrationHelper.KillProcess("DbgCLR");
            IntegrationHelper.KillProcess("VsJITDebugger");
            IntegrationHelper.KillProcess("dexplore");

            _hostProcess = Process.Start(VsExeFile, VsLaunchArgs);
            Debug.WriteLine($"Launched a new instance of Visual Studio. (ID: {_hostProcess.Id})");
        }

        private void InitializeRemotingService()
        {
            ExecuteDteCommandAsync("Tools.StartIntegrationTestService").GetAwaiter().GetResult();

            _serviceChannel = new IpcClientChannel();
            ChannelServices.RegisterChannel(_serviceChannel, ensureSecurity: true);

            // Connect to a 'well defined, shouldn't conflict' IPC channel
            _serviceUri = string.Format($"ipc://{IntegrationService.PortNameFormatString}", _hostProcess.Id);
            _service = (IntegrationService)(Activator.GetObject(typeof(IntegrationService), $"{_serviceUri}/{typeof(IntegrationService).FullName}"));
        }
    }
}
