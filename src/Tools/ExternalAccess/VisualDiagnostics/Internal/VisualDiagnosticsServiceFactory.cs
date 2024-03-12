// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Contracts;
using Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Internal;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.ServiceHub.Framework;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics;

/// <summary>
/// LSP Service responsible for loading IVisualDiagnosticsLanguageService workspace service and delegate the broker service to the workspace service,
/// and handling MAUI XAML/C#/CSS/Razor Hot Reload support
/// </summary>
[ExportCSharpVisualBasicLspServiceFactory(typeof(OnInitializedService)), Shared]
internal sealed class VisualDiagnosticsServiceFactory : ILspServiceFactory
{
    private readonly LspWorkspaceRegistrationService _lspWorkspaceRegistrationService;
    private readonly Lazy<IVisualDiagnosticsBrokeredDebuggerServices> _brokeredDebuggerServices;
    private readonly IAsynchronousOperationListenerProvider _listenerProvider;

    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    [ImportingConstructor]
    public VisualDiagnosticsServiceFactory(
        LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
        Lazy<IVisualDiagnosticsBrokeredDebuggerServices> brokeredDebuggerServices,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;
        _brokeredDebuggerServices = brokeredDebuggerServices;
        _listenerProvider = listenerProvider;
    }

    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        var lspWorkspaceManager = lspServices.GetRequiredService<LspWorkspaceManager>();
        return new OnInitializedService(lspServices, lspWorkspaceManager, _lspWorkspaceRegistrationService, _brokeredDebuggerServices, _listenerProvider);
    }

    private class OnInitializedService : ILspService, IOnInitialized, IDisposable
    {
        private readonly LspServices _lspServices;
        private readonly LspWorkspaceManager _lspWorkspaceManager;
        private readonly LspWorkspaceRegistrationService _lspWorkspaceRegistrationService;
        private readonly Lazy<IVisualDiagnosticsBrokeredDebuggerServices> _brokeredDebuggerServices;
        private readonly System.Timers.Timer _timer;
        private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1);
        private readonly IAsynchronousOperationListener _asyncListener;
        private IVisualDiagnosticsLanguageService? _visualDiagnosticsLanguageServiceTable;
        private CancellationToken _cancellationToken;

        public OnInitializedService(LspServices lspServices,
        LspWorkspaceManager lspWorkspaceManager,
        LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
        Lazy<IVisualDiagnosticsBrokeredDebuggerServices> brokeredDebuggerServices,
        IAsynchronousOperationListenerProvider listenerProvider)
        {
            _lspServices = lspServices;
            _lspWorkspaceManager = lspWorkspaceManager;
            _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;
            _brokeredDebuggerServices = brokeredDebuggerServices;
            _timer = new System.Timers.Timer();
            _timer.Interval = 1000;
            _timer.Elapsed += Timer_Elapsed;
            _asyncListener = listenerProvider.GetListener(nameof(VisualDiagnosticsBrokeredDebuggerServices));
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _timer.Stop();
            var token = _asyncListener.BeginAsyncOperation(nameof(OnInitializedService) + ".Timer_Elapsed");
            _ = OnTimerElapsedAsync().CompletesAsyncOperation(token);
        }

        public void Dispose()
        {
            (_visualDiagnosticsLanguageServiceTable as IDisposable)?.Dispose();
            _timer?.Dispose();
        }

        public Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            // This is not ideal, OnInitializedAsync has no way to know when the service broker is ready to be queried
            // We start a timer and wait roughly a second to see if the broker gets initialized.
            // TODO dabarbe: Service broker is initialized as part of another LSP service, not sure if there's a way await on, or having a task completion source?
            _timer.Start();
            return Task.CompletedTask;
        }

        private async Task OnTimerElapsedAsync()
        {
            using (await _mutex.DisposableWaitAsync().ConfigureAwait(false))
            {
                IVisualDiagnosticsBrokeredDebuggerServices broker = _brokeredDebuggerServices.Value;

                if (broker != null)
                {
                    // initialize VisualDiagnosticsLanguageService
                    Workspace workspace = this._lspWorkspaceRegistrationService.GetAllRegistrations().Where(w => w.Kind == WorkspaceKind.Host).FirstOrDefault();
                    if (workspace != null)
                    {
                        IVisualDiagnosticsLanguageService? visualDiagnosticsLanguageService = workspace.Services.GetService<IVisualDiagnosticsLanguageService>();

                        if (visualDiagnosticsLanguageService != null)
                        {
                            IServiceBroker? serviceProvider = await _brokeredDebuggerServices.Value.GetServiceBrokerAsync().ConfigureAwait(false);
                            await visualDiagnosticsLanguageService.InitializeAsync(serviceProvider, _cancellationToken).ConfigureAwait(false);
                            _visualDiagnosticsLanguageServiceTable = visualDiagnosticsLanguageService;
                        }
                        return;
                    }
                }

                // We're not ready, re-start the timer
                _timer.Start();
            }
        }
    }
}
