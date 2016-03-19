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

using Process = System.Diagnostics.Process;

namespace Roslyn.VisualStudio.Test.Utilities
{
    public class VisualStudioInstance
    {
        private readonly Process _hostProcess;
        private readonly DTE _dte;
        private readonly IntegrationService _service;
        private readonly string _serviceUri;
        private readonly IpcClientChannel _serviceChannel;

        // TODO: We could probably expose all the windows/services/features of the host process in a better manner
        private InteractiveWindow _csharpInteractiveWindow;
        private EditorWindow _editorWindow;
        private SolutionExplorer _solutionExplorer;

        public VisualStudioInstance(Process process, DTE dte)
        {
            _hostProcess = process;
            _dte = dte;

            ExecuteDteCommandAsync("Tools.StartIntegrationTestService").GetAwaiter().GetResult();

            _serviceChannel = new IpcClientChannel();
            ChannelServices.RegisterChannel(_serviceChannel, ensureSecurity: true);

            // Connect to a 'well defined, shouldn't conflict' IPC channel
            _serviceUri = string.Format($"ipc://{IntegrationService.PortNameFormatString}", _hostProcess.Id);
            _service = (IntegrationService)(Activator.GetObject(typeof(IntegrationService), $"{_serviceUri}/{typeof(IntegrationService).FullName}"));
        }

        public DTE Dte => _dte;

        public bool IsRunning => !_hostProcess.HasExited;

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

        public void Close()
        {
            if (!IsRunning)
            {
                return;
            }

            CloseAndDeleteOpenSolution();
            CleanupRemotingService();
            CleanupHostProcess();
        }

        public void CloseAndDeleteOpenSolution()
        {
            // DTE can still cause a failure or crash during cleanup, such as if cleaning up the open projects/solutions fails
            try
            {
                _dte.Documents.CloseAll(EnvDTE.vsSaveChanges.vsSaveChangesNo);

                if (_dte.Solution != null)
                {
                    var directoriesToDelete = new List<string>();

                    // Save the full path to each project in the solution. This is so we can cleanup any folders after the solution is closed.
                    foreach (EnvDTE.Project project in _dte.Solution.Projects)
                    {
                        directoriesToDelete.Add(Path.GetDirectoryName(project.FullName));
                    }

                    // Save the full path to the solution. This is so we can cleanup any folders after the solution is closed.
                    directoriesToDelete.Add(Path.GetDirectoryName(_dte.Solution.FullName));

                    _dte.Solution.Close(SaveFirst: false);

                    foreach (var directoryToDelete in directoriesToDelete)
                    {
                        IntegrationHelper.TryDeleteDirectoryRecursively(directoryToDelete);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Warning: Failed to cleanup the DTE.");
                Debug.WriteLine($"\t{e}");
            }
        }

        private void CleanupHostProcess()
        {
            _dte.Quit();

            IntegrationHelper.KillProcess(_hostProcess);
        }

        private void CleanupRemotingService()
        {
            try
            {
                if ((_dte?.Commands.Item(VisualStudioCommandNames.VsStopServiceCommand).IsAvailable).GetValueOrDefault())
                {
                    ExecuteDteCommandAsync(VisualStudioCommandNames.VsStopServiceCommand).GetAwaiter().GetResult();
                }
            }
            finally
            {
                if (_serviceChannel != null)
                {
                    ChannelServices.UnregisterChannel(_serviceChannel);
                }
            }
        }
    }
}
