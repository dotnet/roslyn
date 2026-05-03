// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
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
using Microsoft.VisualStudio.Utilities.ServiceBroker;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics;

/// <summary>
/// LSP Service responsible for loading IVisualDiagnosticsLanguageService workspace service and delegate the broker service to the workspace service,
/// and handling MAUI XAML/C#/CSS/Razor Hot Reload support
/// </summary>
[ExportCSharpVisualBasicLspServiceFactory(typeof(OnInitializedService)), Shared]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
[method: ImportingConstructor]
internal sealed class VisualDiagnosticsServiceFactory(
    LspWorkspaceRegistrationService lspWorkspaceRegistrationService) : ILspServiceFactory
{
    private readonly Lazy<OnInitializedService> _onInitializedService = new(() => new OnInitializedService(lspWorkspaceRegistrationService));

    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        return _onInitializedService.Value;
    }

    private class OnInitializedService(LspWorkspaceRegistrationService lspWorkspaceRegistrationService) : ILspService, IServiceBrokerInitializer, IDisposable
    {
        private readonly LspWorkspaceRegistrationService _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;
        private IVisualDiagnosticsLanguageService? _visualDiagnosticsLanguageService;

        public ImmutableDictionary<ServiceMoniker, ServiceRegistration> ServicesToRegister => [];

        public void Dispose()
        {
            (_visualDiagnosticsLanguageService as IDisposable)?.Dispose();
        }

        public void OnServiceBrokerInitialized(IServiceBroker serviceBroker, CancellationToken cancellationToken)
        {
            _ = OnInitializeVisualDiagnosticsLanguageServiceAsync(serviceBroker, cancellationToken);
        }

        public void Proffer(GlobalBrokeredServiceContainer container)
        {
        }

        private async Task OnInitializeVisualDiagnosticsLanguageServiceAsync(IServiceBroker serviceBroker, CancellationToken cancellationToken)
        {
            // initialize VisualDiagnosticsLanguageService
            Workspace workspace = _lspWorkspaceRegistrationService.GetAllRegistrations().First(w => w.Kind == WorkspaceKind.Host);
            Contract.ThrowIfFalse(workspace != null, "We should always have a host workspace.");

            IVisualDiagnosticsLanguageService? visualDiagnosticsLanguageService = workspace.Services.GetService<IVisualDiagnosticsLanguageService>();

            if (visualDiagnosticsLanguageService != null)
            {
                await visualDiagnosticsLanguageService.InitializeAsync(serviceBroker, cancellationToken).ConfigureAwait(false);
                _visualDiagnosticsLanguageService = visualDiagnosticsLanguageService;
            }
        }
    }
}
