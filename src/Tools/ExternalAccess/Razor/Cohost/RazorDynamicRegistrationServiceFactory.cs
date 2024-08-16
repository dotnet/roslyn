// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;

[ExportCSharpVisualBasicLspServiceFactory(typeof(RazorDynamicRegistrationService), WellKnownLspServerKinds.AlwaysActiveVSLspServer), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RazorDynamicRegistrationServiceFactory(
    [Import(AllowDefault = true)] IUIContextActivationService? uIContextActivationService,
    [Import(AllowDefault = true)] Lazy<IRazorCohostDynamicRegistrationService>? dynamicRegistrationService) : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        var clientLanguageServerManager = lspServices.GetRequiredService<IClientLanguageServerManager>();

        return new RazorDynamicRegistrationService(uIContextActivationService, dynamicRegistrationService, clientLanguageServerManager);
    }

    private class RazorDynamicRegistrationService(
        IUIContextActivationService? uIContextActivationService,
        Lazy<IRazorCohostDynamicRegistrationService>? dynamicRegistrationService,
        IClientLanguageServerManager? clientLanguageServerManager) : ILspService, IOnInitialized, IDisposable
    {
        private readonly CancellationTokenSource _disposalTokenSource = new();
        private IDisposable? _activation;

        public void Dispose()
        {
            _activation?.Dispose();
            _activation = null;
            _disposalTokenSource.Cancel();
        }

        public Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
        {
            if (dynamicRegistrationService is null || clientLanguageServerManager is null)
            {
                return Task.CompletedTask;
            }

            if (uIContextActivationService is null)
            {
                // Outside of VS, we want to initialize immediately.. I think?
                InitializeRazor();
            }
            else
            {
                _activation = uIContextActivationService.ExecuteWhenActivated(Constants.RazorCohostingUIContext, InitializeRazor);
            }

            return Task.CompletedTask;

            void InitializeRazor()
            {
                this.InitializeRazor(clientCapabilities, context, _disposalTokenSource.Token);
            }
        }

        private void InitializeRazor(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
        {
            // The LSP server will dispose us when the server exits, but VS could decide to activate us later.
            // If a new instance of the server is created, a new instance of this class will be created and the
            // UIContext will already be active, so this method will be immediately called on the new instance.
            if (cancellationToken.IsCancellationRequested) return;

            // We use a string to pass capabilities to/from Razor to avoid version issues with the Protocol DLL
            var serializedClientCapabilities = JsonSerializer.Serialize(clientCapabilities, ProtocolConversions.LspJsonSerializerOptions);
            var razorCohostClientLanguageServerManager = new RazorCohostClientLanguageServerManager(clientLanguageServerManager!);

            var requestContext = new RazorCohostRequestContext(context);
            dynamicRegistrationService!.Value.RegisterAsync(serializedClientCapabilities, requestContext, cancellationToken).ReportNonFatalErrorAsync();
        }
    }
}
