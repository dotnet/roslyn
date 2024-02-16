// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
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

/// <summary>
/// LSP Service responsible for hooking up to the debugger's broker IHotReloadSessionNotificationService
/// and listening to start/end debugging session delegating to IVisualDiagnosticsLanguageService workspace service
/// </summary>
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
        private readonly ConditionalWeakTable<Workspace, IVisualDiagnosticsLanguageService> _visualDiagnosticsLanguageServiceTable;
        private readonly System.Timers.Timer _timer;
        private static readonly object _lock = new object();
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
            _visualDiagnosticsLanguageServiceTable = new();
            _debugProcesses = new List<ProcessInfo>();
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // This timer is necessary because OnInitializedService 
            lock (_lock)
            {
                _ = OnTimerElapsedAsync();
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
            // This is not ideal, OnInitializedAsync has no way to know when the service broker is ready to be queried
            // We start a timer and wait rough a second to see if the broker gets initialized.
            // TODO dabarbe: Service broker is initialized as part of another LSP service, not sure if there's a way for this service to have an api to await on?
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

        private async Task OnTimerElapsedAsync()
        {
            if (_adviseHotReloadSessionNotificationService != null)
            {
                // just double make sure the timer is stopped
                _timer.Stop();
                return;
            }

            IBrokeredDebuggerServices broker = _brokeredDebuggerServices.Value;

            if (broker != null)
            {
                IHotReloadSessionNotificationService? hotReloadSessionNotificationService = await broker.HotReloadSessionNotificationServiceAsync().ConfigureAwait(false);
                if (hotReloadSessionNotificationService != null)
                {
                    _adviseHotReloadSessionNotificationService = await InitializeHotReloadSessionNotificationServiceAsync(hotReloadSessionNotificationService).ConfigureAwait(false);
                    if (_adviseHotReloadSessionNotificationService != null)
                    {
                        _timer.Stop();
                    }
                }
            }
        }

        private async Task HandleNotificationAsync(HotReloadNotificationType value)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                return;
            }

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

        private async Task<IDisposable?> InitializeHotReloadSessionNotificationServiceAsync(IHotReloadSessionNotificationService hotReloadSessionNotificationService)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            // Subscribe to the hotReload SessionNotification Service  
            return await hotReloadSessionNotificationService.SubscribeAsync(this, _cancellationToken).ConfigureAwait(false);
        }

        private async Task StartVisualDiagnosticsAsync(HotReloadSessionInfo info)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                return;
            }

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
            if (_cancellationToken.IsCancellationRequested)
            {
                return;
            }

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
            _debugProcesses = info.Processes.Select(_debugProcess => new ProcessInfo(_debugProcess.ProcessId, _debugProcess.LocalProcessId, _debugProcess.PathToTargetAssembly)).ToList();
        }

        private async Task<IVisualDiagnosticsLanguageService?> EnsureVisualDiagnosticsLanguageServiceAsync(ProcessInfo processInfo)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            Workspace workspace = ProcessInfoToWorkspace(processInfo);
            if (workspace != null)
            {
                IVisualDiagnosticsLanguageService? visualDiagnosticsLanguageService;
                if (!_visualDiagnosticsLanguageServiceTable.TryGetValue(workspace, out visualDiagnosticsLanguageService))
                {
                    visualDiagnosticsLanguageService = workspace.Services.GetService<IVisualDiagnosticsLanguageService>();

                    if (visualDiagnosticsLanguageService != null)
                    {
                        IServiceBroker? serviceProvider = await _brokeredDebuggerServices.Value.ServiceBrokerAsync().ConfigureAwait(false);
                        await visualDiagnosticsLanguageService.InitializeAsync(serviceProvider, _cancellationToken).ConfigureAwait(false);
                        _visualDiagnosticsLanguageServiceTable.Add(workspace, visualDiagnosticsLanguageService);
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
            // If the Host workspace provides access to a active set of source code projects and documents and their associated syntax trees, compilations and semantic models, and maps to the current opened solution in C# Dev Kit.
            // Than I think we're good, are we? 
            return this._lspWorkspaceRegistrationService.GetAllRegistrations().Where(w => w.Kind == WorkspaceKind.Host).FirstOrDefault();
        }
    }
}
