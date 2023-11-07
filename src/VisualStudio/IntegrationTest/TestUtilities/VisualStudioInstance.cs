// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;
using Xunit;
using Process = System.Diagnostics.Process;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    public class VisualStudioInstance
    {
        /// <summary>
        /// Used for creating unique IPC channel names each time a Visual Studio instance is created during tests.
        /// </summary>
        /// <seealso cref="GetIpcClientChannelName"/>
        private static int s_connectionIndex = 0;

        private readonly IntegrationService _integrationService;
        private readonly IpcClientChannel _integrationServiceChannel;
        private readonly VisualStudio_InProc _inProc;

        public SendKeys SendKeys { get; }

        public SolutionExplorer_OutOfProc SolutionExplorer { get; }

        public VisualStudioWorkspace_OutOfProc Workspace { get; }

        public StartPage_OutOfProc StartPage { get; }

        internal DTE Dte { get; }

        internal Process HostProcess { get; }

        /// <summary>
        /// The set of Visual Studio packages that are installed into this instance.
        /// </summary>
        public ImmutableHashSet<string> SupportedPackageIds { get; }

        /// <summary>
        /// The path to the root of this installed version of Visual Studio. This is the folder that contains
        /// Common7\IDE.
        /// </summary>
        public string InstallationPath { get; }

        public bool IsUsingLspEditor { get; }

        public VisualStudioInstance(Process hostProcess, DTE dte, ImmutableHashSet<string> supportedPackageIds, string installationPath, bool isUsingLspEditor)
        {
            HostProcess = hostProcess;
            Dte = dte;
            SupportedPackageIds = supportedPackageIds;
            InstallationPath = installationPath;
            IsUsingLspEditor = isUsingLspEditor;

            if (System.Diagnostics.Debugger.IsAttached)
            {
                // If a Visual Studio debugger is attached to the test process, attach it to the instance running
                // integration tests as well.
                var debuggerHostDte = GetDebuggerHostDte();
                var targetProcessId = Process.GetCurrentProcess().Id;
                var localProcess = debuggerHostDte?.Debugger.LocalProcesses.OfType<EnvDTE80.Process2>().FirstOrDefault(p => p.ProcessID == hostProcess.Id);
                localProcess?.Attach2("Managed");
            }

            StartRemoteIntegrationService(dte);

            _integrationServiceChannel = new IpcClientChannel(GetIpcClientChannelName(HostProcess), sinkProvider: null);
            ChannelServices.RegisterChannel(_integrationServiceChannel, ensureSecurity: true);

            // Connect to a 'well defined, shouldn't conflict' IPC channel
            _integrationService = IntegrationService.GetInstanceFromHostProcess(hostProcess);

            // Create marshal-by-ref object that runs in host-process.
            _inProc = ExecuteInHostProcess<VisualStudio_InProc>(
                type: typeof(VisualStudio_InProc),
                methodName: nameof(VisualStudio_InProc.Create)
            );

            // There is a lot of VS initialization code that goes on, so we want to wait for that to 'settle' before
            // we start executing any actual code.
            _inProc.WaitForSystemIdle();

            SolutionExplorer = new SolutionExplorer_OutOfProc(this);
            Workspace = new VisualStudioWorkspace_OutOfProc(this);
            StartPage = new StartPage_OutOfProc(this);

            SendKeys = new SendKeys(this);

            // Ensure we are in a known 'good' state by cleaning up anything changed by the previous instance
            CleanUp();
        }

        private static string GetIpcClientChannelName(Process hostProcess)
        {
            var index = Interlocked.Increment(ref s_connectionIndex) - 1;
            if (index == 0)
            {
                return $"IPC channel client for {hostProcess.Id}";
            }
            else
            {
                return $"IPC channel client for {hostProcess.Id} ({index})";
            }
        }

        public T ExecuteInHostProcess<T>(Type type, string methodName)
        {
            var objectUri = _integrationService.Execute(type.Assembly.Location, type.FullName, methodName) ?? throw new InvalidOperationException("The specified call was expected to return a value.");
            return (T)Activator.GetObject(typeof(T), $"{_integrationService.BaseUri}/{objectUri}");
        }

        public void ActivateMainWindow()
            => _inProc.ActivateMainWindow();

        public void WaitForApplicationIdle(CancellationToken cancellationToken)
        {
            var task = Task.Factory.StartNew(() => _inProc.WaitForApplicationIdle(Helper.HangMitigatingTimeout), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            task.Wait(cancellationToken);
        }

        public bool IsRunning => !HostProcess.HasExited;

        public void CleanUp()
        {
            Workspace.CleanUpWaitingService();
            Workspace.CleanUpWorkspace();
            SolutionExplorer.CleanUpOpenSolution();
            Workspace.WaitForAllAsyncOperationsOrFail(Helper.HangMitigatingTimeout);

            // Close any windows leftover from previous (failed) tests
            StartPage.CloseWindow();

            // Prevent the start page from showing after each solution closes
            StartPage.SetEnabled(false);
            Workspace.ResetOptions();
        }

        public void Close(bool exitHostProcess = true)
        {
            if (!IsRunning)
            {
                CloseRemotingService(allowInProcCalls: false);
                return;
            }

            try
            {
                CleanUp();
            }
            catch
            {
                // A cleanup failure occurred, but we still need to close the communication channel from this side
                CloseRemotingService(allowInProcCalls: false);
                throw;
            }

            CloseRemotingService(allowInProcCalls: true);

            if (exitHostProcess)
            {
                CloseHostProcess();
            }
        }

        private static DTE? GetDebuggerHostDte()
        {
            var currentProcessId = Process.GetCurrentProcess().Id;
            foreach (var process in Process.GetProcessesByName("devenv"))
            {
                var dte = IntegrationHelper.TryLocateDteForProcess(process);
                if (dte?.Debugger?.DebuggedProcesses?.OfType<EnvDTE.Process>().Any(p => p.ProcessID == currentProcessId) ?? false)
                {
                    return dte;
                }
            }

            return null;
        }

        private void CloseHostProcess()
        {
            _inProc.Quit();
            if (!HostProcess.WaitForExit(milliseconds: 10000))
            {
                IntegrationHelper.KillProcess(HostProcess);
            }
        }

        private void CloseRemotingService(bool allowInProcCalls)
        {
            try
            {
                if (allowInProcCalls)
                {
                    StopRemoteIntegrationService();
                }
            }
            finally
            {
                if (_integrationServiceChannel != null
                    && ChannelServices.RegisteredChannels.Contains(_integrationServiceChannel))
                {
                    ChannelServices.UnregisterChannel(_integrationServiceChannel);
                }
            }
        }

        private static void StartRemoteIntegrationService(DTE dte)
        {
            // We use DTE over RPC to start the integration service. All other DTE calls should happen in the host process.
            if (dte.Commands.Item(WellKnownCommandNames.Test_IntegrationTestService_Start).IsAvailable)
            {
                dte.ExecuteCommand(WellKnownCommandNames.Test_IntegrationTestService_Start);
            }
        }

        private void StopRemoteIntegrationService()
        {
            if (_inProc.IsCommandAvailable(WellKnownCommandNames.Test_IntegrationTestService_Stop))
            {
                _inProc.ExecuteCommand(WellKnownCommandNames.Test_IntegrationTestService_Stop);
            }
        }
    }
}
