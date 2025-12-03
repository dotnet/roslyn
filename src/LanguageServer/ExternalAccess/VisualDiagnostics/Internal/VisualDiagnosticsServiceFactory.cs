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
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.ServiceHub.Framework;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics;

/// <summary>
/// LSP Service responsible for loading IVisualDiagnosticsLanguageService workspace service and delegate the broker service to the workspace service,
/// and handling MAUI XAML/C#/CSS/Razor Hot Reload support
/// </summary>
[Export(typeof(IOnServiceBrokerInitialized))]
[ExportCSharpVisualBasicLspServiceFactory(typeof(OnInitializedService)), Shared]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
[method: ImportingConstructor]
internal sealed class VisualDiagnosticsServiceFactory(
    LspWorkspaceRegistrationService lspWorkspaceRegistrationService) : ILspServiceFactory, IOnServiceBrokerInitialized
{
    private readonly LspWorkspaceRegistrationService _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;
    private readonly Lazy<OnInitializedService> _OnInitializedService = new(() => new OnInitializedService(lspWorkspaceRegistrationService));

    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        return _OnInitializedService.Value;
    }

    public void OnServiceBrokerInitialized(IServiceBroker serviceBroker)
    {
        _OnInitializedService.Value.OnServiceBrokerInitialized(serviceBroker);
    }

    private class OnInitializedService : ILspService, IOnInitialized, IOnServiceBrokerInitialized, IDisposable
    {
        private readonly LspWorkspaceRegistrationService _lspWorkspaceRegistrationService;
        private IVisualDiagnosticsLanguageService? _visualDiagnosticsLanguageService;
        private CancellationToken _cancellationToken;
        private static readonly TaskCompletionSource<bool> _taskCompletionSource = new();

        public OnInitializedService(LspWorkspaceRegistrationService lspWorkspaceRegistrationService)
        {
            _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;
        }

        public void Dispose()
        {
            (_visualDiagnosticsLanguageService as IDisposable)?.Dispose();
        }

        public Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _taskCompletionSource.TrySetResult(true);
            return Task.CompletedTask;
        }

        public void OnServiceBrokerInitialized(IServiceBroker serviceBroker)
        {
            _taskCompletionSource.Task.ContinueWith((initialized) => OnInitializeVisualDiagnosticsLanguageServiceAsync(serviceBroker), TaskScheduler.Default);
        }

        private async Task OnInitializeVisualDiagnosticsLanguageServiceAsync(IServiceBroker serviceBroker)
        {
            // initialize VisualDiagnosticsLanguageService
            Workspace workspace = _lspWorkspaceRegistrationService.GetAllRegistrations().First(w => w.Kind == WorkspaceKind.Host);
            Contract.ThrowIfFalse(workspace != null, "We should always have a host workspace.");

            IVisualDiagnosticsLanguageService? visualDiagnosticsLanguageService = workspace.Services.GetService<IVisualDiagnosticsLanguageService>();

            if (visualDiagnosticsLanguageService != null)
            {
                await visualDiagnosticsLanguageService.InitializeAsync(serviceBroker, _cancellationToken).ConfigureAwait(false);
                _visualDiagnosticsLanguageService = visualDiagnosticsLanguageService;
            }
        }
    }
}
