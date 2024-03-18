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
    private readonly Lazy<IVisualDiagnosticsServiceBroker> _brokeredDebuggerServices;
    private readonly IAsynchronousOperationListenerProvider _listenerProvider;

    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    [ImportingConstructor]
    public VisualDiagnosticsServiceFactory(
        LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
        Lazy<IVisualDiagnosticsServiceBroker> brokeredDebuggerServices,
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
        private readonly Lazy<IVisualDiagnosticsServiceBroker> _brokeredDebuggerServices;
        private readonly System.Timers.Timer _timer;
        private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1);
        private readonly IAsynchronousOperationListener _asyncListener;
        private IVisualDiagnosticsLanguageService? _visualDiagnosticsLanguageService;
        private CancellationToken _cancellationToken;

        public OnInitializedService(LspServices lspServices,
        LspWorkspaceManager lspWorkspaceManager,
        LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
        Lazy<IVisualDiagnosticsServiceBroker> brokeredDebuggerServices,
        IAsynchronousOperationListenerProvider listenerProvider)
        {
            _lspServices = lspServices;
            _lspWorkspaceManager = lspWorkspaceManager;
            _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;
            _brokeredDebuggerServices = brokeredDebuggerServices;
            _timer = new System.Timers.Timer();
            _timer.Interval = 500;
            _timer.Elapsed += Timer_Elapsed;
            _asyncListener = listenerProvider.GetListener(nameof(VisualDiagnosticsServiceBroker));
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _timer.Stop();
            var token = _asyncListener.BeginAsyncOperation(nameof(OnInitializedService) + ".Timer_Elapsed");
            _ = OnTimerElapsedAsync().CompletesAsyncOperation(token);
        }

        public void Dispose()
        {
            (_visualDiagnosticsLanguageService as IDisposable)?.Dispose();
            _timer?.Dispose();
        }

        public Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            // TODO dabarbe: Calling ServiceBrokerFactory.GetRequiredServiceBrokerContainerAsync.ConfigureAwait(false); here prevents
            // ServiceBrokerConnectHandler from being initialized. Not sure why? There's something in the synchronization context that
            // prevents TaskCompletionSource tasks to run properly during the LSP initialization.  Solution is to getting out of OnInitializedAsync and calling 
            // _brokeredDebuggerServices.Value.GetServiceBrokerAsync().ConfigureAwait(false) on the timer dispatcher, which is a separate synchronization context. This
            // allows GetServiceBrokerAsync() to await until ServiceBrokerFactory.CreateAsync sets the container, and allowing the task completion source to resume
            _timer.Start();
            return Task.CompletedTask;
        }

        private async Task OnTimerElapsedAsync()
        {
            await OnInitializeVisualDiagnosticsLanguageServiceAsync().ConfigureAwait(false);
        }

        private async Task OnInitializeVisualDiagnosticsLanguageServiceAsync()
        {
            using (await _mutex.DisposableWaitAsync().ConfigureAwait(false))
            {
                if (_visualDiagnosticsLanguageService != null)
                {
                    return;
                }

                IVisualDiagnosticsServiceBroker brokerService = _brokeredDebuggerServices.Value;

                if (brokerService != null)
                {
                    // The broker service may be not available right await. That's because the broker service gets initialized
                    // when the C# DevKit is getting initialized in parallel.  GetServiceBrokerAsync() will await until
                    // the container gets created. Once created, this task will resume.  In case where C# Dev kit is not
                    // enabled, the service broker container will never be created and this task will never return, thus preventing
                    // us from creating the IVisualDiagnosticsLanguageService
                    IServiceBroker? serviceBroker = await brokerService.GetServiceBrokerAsync().ConfigureAwait(false);
                    if (serviceBroker != null)
                    {
                        // initialize VisualDiagnosticsLanguageService
                        Workspace workspace = this._lspWorkspaceRegistrationService.GetAllRegistrations().Where(w => w.Kind == WorkspaceKind.Host).FirstOrDefault();
                        if (workspace != null)
                        {
                            IVisualDiagnosticsLanguageService? visualDiagnosticsLanguageService = workspace.Services.GetService<IVisualDiagnosticsLanguageService>();

                            if (visualDiagnosticsLanguageService != null)
                            {
                                await visualDiagnosticsLanguageService.InitializeAsync(serviceBroker, _cancellationToken).ConfigureAwait(false);
                                _visualDiagnosticsLanguageService = visualDiagnosticsLanguageService;
                            }
                        }
                    }
                }
            }
        }
    }
}
