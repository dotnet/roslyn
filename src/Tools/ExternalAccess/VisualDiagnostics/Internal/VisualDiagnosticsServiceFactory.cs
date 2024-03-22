// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BrokeredServices;
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
    private readonly VisualDiagnosticsServiceBroker _brokeredDebuggerServices;

    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    [ImportingConstructor]
    public VisualDiagnosticsServiceFactory(
        LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
        VisualDiagnosticsServiceBroker brokeredDebuggerServices)
    {
        _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;
        _brokeredDebuggerServices = brokeredDebuggerServices;
    }

    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        return new OnInitializedService(_lspWorkspaceRegistrationService, _brokeredDebuggerServices);
    }

    private class OnInitializedService : ILspService, IOnInitialized, IOnServiceBrokerInitialized, IDisposable
    {
        private readonly LspWorkspaceRegistrationService _lspWorkspaceRegistrationService;
        private readonly VisualDiagnosticsServiceBroker _brokeredDebuggerServices;
        private IVisualDiagnosticsLanguageService? _visualDiagnosticsLanguageService;
        private CancellationToken _cancellationToken;
        private static readonly TaskCompletionSource<bool> _taskCompletionSource = new TaskCompletionSource<bool>();

        public OnInitializedService(LspWorkspaceRegistrationService lspWorkspaceRegistrationService, VisualDiagnosticsServiceBroker brokeredDebuggerServices)
        {
            _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;
            _brokeredDebuggerServices = brokeredDebuggerServices;

            _brokeredDebuggerServices.NotifyServiceBrokerInitialized = this;
        }

        public void Dispose()
        {
            (_visualDiagnosticsLanguageService as IDisposable)?.Dispose();
        }

        public Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _taskCompletionSource.SetResult(true);
            return Task.CompletedTask;
        }

        public void OnServiceBrokerInitialized(IServiceBroker serviceBroker)
        {
            Task.Run(async () => await OnInitializeVisualDiagnosticsLanguageServiceAsync(serviceBroker).ConfigureAwait(false));
        }

        private async Task OnInitializeVisualDiagnosticsLanguageServiceAsync(IServiceBroker serviceBroker)
        {
            // Make sure we're initialized. 
            bool initialized = await _taskCompletionSource.Task.ConfigureAwait(false);

            if (initialized && serviceBroker != null)
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
