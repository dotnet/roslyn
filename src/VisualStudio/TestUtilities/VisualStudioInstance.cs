// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Threading.Tasks;
using System.Windows.Automation;
using EnvDTE;
using Roslyn.VisualStudio.Test.Utilities.Remoting;

using Process = System.Diagnostics.Process;

namespace Roslyn.VisualStudio.Test.Utilities
{
    public class VisualStudioInstance
    {
        public DTE DTE { get; }

        private readonly Process _hostProcess;
        private readonly IntegrationService _integrationService;
        private readonly IpcClientChannel _integrationServiceChannel;

        // TODO: We could probably expose all the windows/services/features of the host process in a better manner
        private readonly Lazy<CSharpInteractiveWindow> _csharpInteractiveWindow;
        private readonly Lazy<EditorWindow> _editorWindow;
        private readonly Lazy<SolutionExplorer> _solutionExplorer;
        private readonly Lazy<Workspace> _workspace;

        public VisualStudioInstance(Process hostProcess, DTE dte)
        {
            _hostProcess = hostProcess;
            this.DTE = dte;

            this.DTE.ExecuteCommandAsync(VisualStudioCommandNames.VsStartServiceCommand).GetAwaiter().GetResult();

            _integrationServiceChannel = new IpcClientChannel($"IPC channel client for {_hostProcess.Id}", sinkProvider: null);
            ChannelServices.RegisterChannel(_integrationServiceChannel, ensureSecurity: true);

            // Connect to a 'well defined, shouldn't conflict' IPC channel
            _integrationService = IntegrationService.GetInstanceFromHostProcess(hostProcess);

            // There is a lot of VS initialization code that goes on, so we want to wait for that to 'settle' before
            // we start executing any actual code.
            ExecuteInHostProcess(
                type: typeof(RemotingHelper),
                methodName: nameof(RemotingHelper.WaitForSystemIdle));

            _csharpInteractiveWindow = new Lazy<CSharpInteractiveWindow>(() => new CSharpInteractiveWindow(this));
            _editorWindow = new Lazy<EditorWindow>(() => new EditorWindow(this));
            _solutionExplorer = new Lazy<SolutionExplorer>(() => new SolutionExplorer(this));
            _workspace = new Lazy<Workspace>(() => new Workspace(this));

            // Ensure we are in a known 'good' state by cleaning up anything changed by the previous instance
            Cleanup();
        }

        public void ExecuteInHostProcess(Type type, string methodName)
        {
            var result = _integrationService.Execute(type.Assembly.Location, type.FullName, methodName);

            if (result != null)
            {
                throw new InvalidOperationException("The specified call was not expected to return a value.");
            }
        }

        public T ExecuteInHostProcess<T>(Type type, string methodName)
        {
            var objectUri = _integrationService.Execute(type.Assembly.Location, type.FullName, methodName);

            if (objectUri == null)
            {
                throw new InvalidOperationException("The specified call was expected to return a value.");
            }

            return (T)Activator.GetObject(typeof(T), $"{_integrationService.BaseUri}/{objectUri}");
        }

        public bool IsRunning => !_hostProcess.HasExited;

        public CSharpInteractiveWindow CSharpInteractiveWindow => _csharpInteractiveWindow.Value;

        public EditorWindow EditorWindow => _editorWindow.Value;

        public SolutionExplorer SolutionExplorer => _solutionExplorer.Value;

        public Workspace Workspace => _workspace.Value;

        public async Task ClickAutomationElementAsync(string elementName, bool recursive = false)
        {
            var element = await FindAutomationElementAsync(elementName, recursive).ConfigureAwait(false);

            if (element != null)
            {
                var tcs = new TaskCompletionSource<object>();
                Automation.AddAutomationEventHandler(InvokePattern.InvokedEvent, element, TreeScope.Element, (src, e) =>
                {
                    tcs.SetResult(null);
                });

                object invokePatternObj = null;
                if (element.TryGetCurrentPattern(InvokePattern.Pattern, out invokePatternObj))
                {
                    var invokePattern = (InvokePattern)invokePatternObj;
                    invokePattern.Invoke();
                }

                await tcs.Task;
            }
        }

        private async Task<AutomationElement> FindAutomationElementAsync(string elementName, bool recursive = false)
        {
            AutomationElement element = null;
            var scope = recursive ? TreeScope.Descendants : TreeScope.Children;
            var condition = new PropertyCondition(AutomationElement.NameProperty, elementName);

            // TODO(Dustin): This is code is a bit terrifying. If anything goes wrong and the automation
            // element can't be found, it'll continue to spin until the heat death of the universe.
            await IntegrationHelper.WaitForResultAsync(
                () => (element = AutomationElement.RootElement.FindFirst(scope, condition)) != null,
                expectedResult: true)
                .ConfigureAwait(false);

            return element;
        }

        public void Cleanup()
        {
            CleanupOpenSolution();
            CleanupInteractiveWindow();
            CleanupWaitingService();
            CleanupWorkspace();
        }

        private void CleanupInteractiveWindow()
        {
            var csharpInteractiveWindow = this.DTE.LocateWindow(CSharpInteractiveWindow.DteWindowTitle);
            IntegrationHelper.RetryRpcCall(() => csharpInteractiveWindow?.Close());
        }

        private void CleanupOpenSolution()
        {
            IntegrationHelper.RetryRpcCall(() => this.DTE.Documents.CloseAll(vsSaveChanges.vsSaveChangesNo));

            var dteSolution = IntegrationHelper.RetryRpcCall(() => this.DTE.Solution);

            if (dteSolution != null)
            {
                var directoriesToDelete = IntegrationHelper.RetryRpcCall(() =>
                {
                    var directoryList = new List<string>();

                    var dteSolutionProjects = IntegrationHelper.RetryRpcCall(() => dteSolution.Projects);

                    // Save the full path to each project in the solution. This is so we can cleanup any folders after the solution is closed.
                    foreach (EnvDTE.Project project in dteSolutionProjects)
                    {
                        var projectFullName = IntegrationHelper.RetryRpcCall(() => project.FullName);
                        directoryList.Add(Path.GetDirectoryName(projectFullName));
                    }

                    // Save the full path to the solution. This is so we can cleanup any folders after the solution is closed.
                    // The solution might be zero-impact and thus has no name, so deal with that
                    var dteSolutionFullName = IntegrationHelper.RetryRpcCall(() => dteSolution.FullName);

                    if (!string.IsNullOrEmpty(dteSolutionFullName))
                    {
                        directoryList.Add(Path.GetDirectoryName(dteSolutionFullName));

                    }

                    return directoryList;
                });

                IntegrationHelper.RetryRpcCall(() => dteSolution.Close(SaveFirst: false));

                foreach (var directoryToDelete in directoriesToDelete)
                {
                    IntegrationHelper.TryDeleteDirectoryRecursively(directoryToDelete);
                }
            }
        }

        private void CleanupWaitingService()
        {
            ExecuteInHostProcess(
                type: typeof(RemotingHelper),
                methodName: nameof(RemotingHelper.CleanupWaitingService));
        }

        private void CleanupWorkspace()
        {
            ExecuteInHostProcess(
                type: typeof(RemotingHelper),
                methodName: nameof(RemotingHelper.CleanupWorkspace));
        }

        public void Close()
        {
            if (!IsRunning)
            {
                return;
            }

            Cleanup();

            CloseRemotingService();
            CloseHostProcess();
        }

        private void CloseHostProcess()
        {
            IntegrationHelper.RetryRpcCall(() => this.DTE.Quit());

            IntegrationHelper.KillProcess(_hostProcess);
        }

        private void CloseRemotingService()
        {
            try
            {
                if ((IntegrationHelper.RetryRpcCall(() => this.DTE?.Commands.Item(VisualStudioCommandNames.VsStopServiceCommand).IsAvailable).GetValueOrDefault()))
                {
                    this.DTE.ExecuteCommandAsync(VisualStudioCommandNames.VsStopServiceCommand).GetAwaiter().GetResult();
                }
            }
            finally
            {
                if (_integrationServiceChannel != null)
                {
                    ChannelServices.UnregisterChannel(_integrationServiceChannel);
                }
            }
        }
    }
}
