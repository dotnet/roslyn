// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Contracts;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics;

[ExportCSharpVisualBasicLspServiceFactory(typeof(OnInitializedService)), Shared]
internal sealed class VisualDiagnosticsServiceFactory : ILspServiceFactory
{
    private readonly LspWorkspaceRegistrationService _lspWorkspaceRegistrationService;
    private readonly Lazy<IBrokeredDebuggerServices> _brokeredDebuggerServices;

    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    [ImportingConstructor]
    public VisualDiagnosticsServiceFactory(
        LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
        Lazy<IBrokeredDebuggerServices> brokeredDebuggerServices)
    {
        _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;
        _brokeredDebuggerServices = brokeredDebuggerServices;
    }

    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        var lspWorkspaceManager = lspServices.GetRequiredService<LspWorkspaceManager>();
        return new OnInitializedService(lspServices, lspWorkspaceManager, _lspWorkspaceRegistrationService, _brokeredDebuggerServices);
    }

    private class OnInitializedService : ILspService, IOnInitialized, IObserver<HotReloadNotificationType>, IDisposable
    {
        private readonly LspServices _lspServices;
        private readonly LspWorkspaceManager _lspWorkspaceManager;
        private readonly LspWorkspaceRegistrationService _lspWorkspaceRegistrationService;
        private readonly Lazy<IBrokeredDebuggerServices> _brokeredDebuggerServices;
        private readonly Dictionary<Workspace, IVisualDiagnosticsLanguageService> _visualDiagnosticsLanguageServiceRegistar;
        private readonly System.Timers.Timer _timer;
        private List<ProcessInfo> _debugProcesses;
        private CancellationToken _cancellationToken;
        private IDisposable? _adviseHotReloadSessionNotificationService;

        public OnInitializedService(LspServices lspServices, LspWorkspaceManager lspWorkspaceManager, LspWorkspaceRegistrationService lspWorkspaceRegistrationService, Lazy<IBrokeredDebuggerServices> brokeredDebuggerServices)
        {
            _lspServices = lspServices;
            _lspWorkspaceManager = lspWorkspaceManager;
            _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;
            _brokeredDebuggerServices = brokeredDebuggerServices;
            _timer = new System.Timers.Timer();
            _timer.Interval = 1000;
            _timer.Elapsed += Timer_Elapsed;
            _visualDiagnosticsLanguageServiceRegistar = new();
            _debugProcesses = new List<ProcessInfo>();
        }

        private async void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Let
            IHotReloadSessionNotificationService? hotReloadSessionNotificationService = await _brokeredDebuggerServices.Value.HotReloadSessionNotificationServiceAsync().ConfigureAwait(false);
            if (hotReloadSessionNotificationService != null)
            {
                this._timer.Stop();
                _ = InitializeHotReloadSessionNotificationServiceAsync(hotReloadSessionNotificationService);
            }
        }

        public void Dispose()
        {
            _adviseHotReloadSessionNotificationService?.Dispose();
            (_brokeredDebuggerServices as IDisposable)?.Dispose();
        }

        public Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            // Start the timer because the broker service may not be initialize immediately, wait a couple millisec to get the debugger service
            _timer.Start();
            return Task.CompletedTask;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(HotReloadNotificationType value)
        {
            _ = HandleNotificationAsync(value);
        }

        private async Task HandleNotificationAsync(HotReloadNotificationType value)
        {
            IHotReloadSessionNotificationService? notificationService = await _brokeredDebuggerServices.Value.HotReloadSessionNotificationServiceAsync().ConfigureAwait(false);
            if (notificationService != null)
            {
                HotReloadSessionInfo info = await notificationService.FetchHotReloadSessionInfoAsync(_cancellationToken).ConfigureAwait(false);
                switch (value)
                {
                    case HotReloadNotificationType.Started:
                        await StartVisualDiagnosticsAsync(info).ConfigureAwait(false);
                        break;
                    case HotReloadNotificationType.Ended:
                        await StopVisualDiagnosticsAsync(info).ConfigureAwait(false);
                        break;
                }
            }
        }

        private async Task InitializeHotReloadSessionNotificationServiceAsync(IHotReloadSessionNotificationService hotReloadSessionNotificationService)
        {
            // Subscribe to the hotReload SessionNotification Service  
            _adviseHotReloadSessionNotificationService = await hotReloadSessionNotificationService.SubscribeAsync(this, _cancellationToken).ConfigureAwait(false);
        }

        private async Task StartVisualDiagnosticsAsync(HotReloadSessionInfo info)
        {
            foreach (ManagedEditAndContinueProcessInfo processInfo in info.Processes)
            {
                ProcessInfo diagnosticsProcessInfo = new(processInfo.ProcessId, processInfo.LocalProcessId, processInfo.PathToTargetAssembly);
                IVisualDiagnosticsLanguageService? visualDiagnosticsLanguageService = await EnsureVisualDiagnosticsLanguageServiceAsync(diagnosticsProcessInfo).ConfigureAwait(false);
                if (visualDiagnosticsLanguageService != null)
                {
                    visualDiagnosticsLanguageService?.StartDebuggingSessionAsync(diagnosticsProcessInfo, _cancellationToken);
                    _debugProcesses.Add(diagnosticsProcessInfo);
                }
            }
        }

        private async Task StopVisualDiagnosticsAsync(HotReloadSessionInfo info)
        {
            // Info is the new list, so if process is in new list, no need to call StopDebugSessionAsync
            foreach (ProcessInfo trackedDebuggedProcess in _debugProcesses)
            {
                if (!info.Processes.Any(item => item.ProcessId == trackedDebuggedProcess.ProcessId))
                {
                    IVisualDiagnosticsLanguageService? visualDiagnosticsLanguageService = await EnsureVisualDiagnosticsLanguageServiceAsync(trackedDebuggedProcess).ConfigureAwait(false);
                    visualDiagnosticsLanguageService?.StopDebuggingSessionAsync(trackedDebuggedProcess, _cancellationToken);
                }
            }
            // Save the new list. 
            _debugProcesses = info.Processes.Select(_debugProcesses => new ProcessInfo(_debugProcesses.ProcessId, _debugProcesses.LocalProcessId, _debugProcesses.PathToTargetAssembly)).ToList();
        }

        private async Task<IVisualDiagnosticsLanguageService?> EnsureVisualDiagnosticsLanguageServiceAsync(ProcessInfo processInfo)
        {
            // TODO: Get the right workspace given the processInfo
            Workspace workspace = ProcessInfoToWorkspace(processInfo);
            if (workspace != null)
            {
                IVisualDiagnosticsLanguageService? visualDiagnosticsLanguageService = null;
                if (!_visualDiagnosticsLanguageServiceRegistar.TryGetValue(workspace, out visualDiagnosticsLanguageService))
                {
                    visualDiagnosticsLanguageService = workspace.Services.GetService<IVisualDiagnosticsLanguageService>();
                    IServiceBroker? serviceProvider = await _brokeredDebuggerServices.Value.ServiceBrokerAsync().ConfigureAwait(false);
                    visualDiagnosticsLanguageService?.InitializeAsync(serviceProvider, _cancellationToken).ConfigureAwait(false);
                    if (visualDiagnosticsLanguageService != null)
                    {
                        _visualDiagnosticsLanguageServiceRegistar.Add(workspace, visualDiagnosticsLanguageService);
                    }
                }
                // Could be null of we can't get the service
                return visualDiagnosticsLanguageService;
            }

            return null;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Need to determine if multiple workspace exists in LSP server")]
        private Workspace ProcessInfoToWorkspace(ProcessInfo processInfo)
        {
            // Review: Does LSP supports more than one host workspace for a given process? For a given process name with path, should we retrieve the proper Workspace or it's always the host Workspace below?
            return this._lspWorkspaceRegistrationService.GetAllRegistrations().Where(w => w.Kind == WorkspaceKind.Host).FirstOrDefault();
        }
    }
}
