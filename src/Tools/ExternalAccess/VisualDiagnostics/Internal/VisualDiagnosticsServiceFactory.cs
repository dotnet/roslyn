// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Contracts;
using Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Internal;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
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

    private class OnInitializedService : ILspService, IOnInitialized, IDisposable
    {
        private readonly LspServices _lspServices;
        private readonly LspWorkspaceManager _lspWorkspaceManager;
        private readonly LspWorkspaceRegistrationService _lspWorkspaceRegistrationService;
        private readonly Lazy<IBrokeredDebuggerServices> _brokeredDebuggerServices;
        private System.Timers.Timer _timer;


        public OnInitializedService(LspServices lspServices, LspWorkspaceManager lspWorkspaceManager, LspWorkspaceRegistrationService lspWorkspaceRegistrationService, Lazy<IBrokeredDebuggerServices> brokeredDebuggerServices)
        {
            _lspServices = lspServices;
            _lspWorkspaceManager = lspWorkspaceManager;
            _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;
            _brokeredDebuggerServices = brokeredDebuggerServices;
            _timer = new System.Timers.Timer();
            _timer.Interval = 1000;
            _timer.Elapsed += _timer_Elapsed;
        }

        private async void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            IManagedHotReloadService? manageHotReloadService = await _brokeredDebuggerServices.Value.ManagedHotReloadServiceAsync().ConfigureAwait(false);
            if (manageHotReloadService != null)
            {
                Debug.WriteLine("IManagedHotReloadService Broker Proxy Available");
            }
            else
            {
                Debug.WriteLine("IManagedHotReloadService Broker Proxy Not Yet Available");
            }

            IHotReloadSessionNotificationService? hotReloadSessionNotificationService = await _brokeredDebuggerServices.Value.HotReloadSessionNotificationServiceAsync().ConfigureAwait(false);
            if (hotReloadSessionNotificationService != null)
            {
                Debug.WriteLine("hotReloadSessionNotificationService Broker Proxy Available");
            }
            else
            {
                Debug.WriteLine("hotReloadSessionNotificationService Broker Proxy Not Yet Available");
            }
        }

        public void Dispose()
        {
            if (_lspWorkspaceManager != null)
            {
                _lspWorkspaceRegistrationService.LspSolutionChanged -= OnLspSolutionChanged;
            }
        }

        public Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
        {
            // _lspWorkspaceRegistrationService.LspSolutionChanged += OnLspSolutionChanged;
            _timer.Start();
            return Task.CompletedTask;
        }

        private async void OnLspSolutionChanged(object? sender, WorkspaceChangeEventArgs e)
        {
            if (e.DocumentId is not null && e.Kind is WorkspaceChangeKind.DocumentChanged)
            {
                IHotReloadSessionNotificationService? notificationService = await _brokeredDebuggerServices.Value.HotReloadSessionNotificationServiceAsync().ConfigureAwait(false);
                if (notificationService != null)
                {
                    CancellationToken token = new CancellationToken();
                    HotReloadSessionInfo info = await notificationService.FetchHotReloadSessionInfoAsync(token).ConfigureAwait(false);
                }

                IManagedHotReloadAgentManagerService? managedHotReloadAgentManagerService = await _brokeredDebuggerServices.Value.ManagedHotReloadAgentManagerServiceAsync().ConfigureAwait(false);
                if(managedHotReloadAgentManagerService != null)
                {
                    CancellationToken token = new CancellationToken();
                }

                IManagedHotReloadService? manageHotReloadService = await _brokeredDebuggerServices.Value.ManagedHotReloadServiceAsync().ConfigureAwait(false);
                if (manageHotReloadService != null)
                {
                    CancellationToken token = new CancellationToken();
                }


                Workspace workspace = this._lspWorkspaceRegistrationService.GetAllRegistrations().Where(w => w.Kind == WorkspaceKind.Host).FirstOrDefault();
                if (workspace != null)
                {
                    IVisualDiagnosticsLanguageService? visualDiagnosticsLanguageService = workspace.Services.GetService<IVisualDiagnosticsLanguageService>();
                    visualDiagnosticsLanguageService?.InitializeAsync();
                    visualDiagnosticsLanguageService?.CreateDiagnosticsSessionAsync(Guid.NewGuid());
                }
            }
        }

        private Task InitializeHotReloadSessionNotificationService()
        {
            return Task.CompletedTask;
        }
    }
}
