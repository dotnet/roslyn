// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;

[ExportCSharpVisualBasicLspServiceFactory(typeof(RazorDynamicRegistrationService), WellKnownLspServerKinds.AlwaysActiveVSLspServer), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RazorDynamicRegistrationServiceFactory([Import(AllowDefault = true)] Lazy<IRazorCohostDynamicRegistrationService>? dynamicRegistrationService) : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        var clientLanguageServerManager = lspServices.GetRequiredService<IClientLanguageServerManager>();

        return new RazorDynamicRegistrationService(dynamicRegistrationService, clientLanguageServerManager);
    }

    private class RazorDynamicRegistrationService : ILspService, IOnInitialized
    {
        private readonly Lazy<IRazorCohostDynamicRegistrationService>? _dynamicRegistrationService;
        private readonly IClientLanguageServerManager? _clientLanguageServerManager;
        private readonly JsonSerializerSettings _serializerSettings;

        public RazorDynamicRegistrationService(Lazy<IRazorCohostDynamicRegistrationService>? dynamicRegistrationService, IClientLanguageServerManager? clientLanguageServerManager)
        {
            _dynamicRegistrationService = dynamicRegistrationService;
            _clientLanguageServerManager = clientLanguageServerManager;

            var serializer = new JsonSerializer();
            serializer.AddVSInternalExtensionConverters();
            _serializerSettings = new JsonSerializerSettings { Converters = serializer.Converters };
        }

        public Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
        {
            if (_dynamicRegistrationService is null || _clientLanguageServerManager is null)
            {
                return Task.CompletedTask;
            }

            var uiContext = UIContext.FromUIContextGuid(Constants.RazorCohostingUIContext);
            uiContext.WhenActivated(() =>
            {
                // Not using the cancellation token passed in, as the context could be activated well after LSP server initialization
                InitializeRazor(clientCapabilities, context, CancellationToken.None);
            });

            return Task.CompletedTask;
        }

        private void InitializeRazor(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
        {
            // We use a string to pass capabilities to/from Razor to avoid version issues with the Protocol DLL
            var serializedClientCapabilities = JsonConvert.SerializeObject(clientCapabilities, _serializerSettings);
            var razorCohostClientLanguageServerManager = new RazorCohostClientLanguageServerManager(_clientLanguageServerManager!);

            var requestContext = new RazorCohostRequestContext(context);
            _dynamicRegistrationService!.Value.RegisterAsync(serializedClientCapabilities, requestContext, cancellationToken).ReportNonFatalErrorAsync();
        }
    }
}
