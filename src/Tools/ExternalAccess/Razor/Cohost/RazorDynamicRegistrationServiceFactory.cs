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
using Newtonsoft.Json;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;

[ExportCSharpVisualBasicLspServiceFactory(typeof(RazorDynamicRegistrationService), WellKnownLspServerKinds.AlwaysActiveVSLspServer), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RazorDynamicRegistrationServiceFactory([Import(AllowDefault = true)] IRazorCohostDynamicRegistrationService? dynamicRegistrationService) : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        var clientLanguageServerManager = lspServices.GetRequiredService<IClientLanguageServerManager>();

        return new RazorDynamicRegistrationService(dynamicRegistrationService, clientLanguageServerManager);
    }

    private class RazorDynamicRegistrationService : ILspService, IOnInitialized
    {
        private readonly IRazorCohostDynamicRegistrationService? _dynamicRegistrationService;
        private readonly IClientLanguageServerManager _clientLanguageServerManager;
        private readonly JsonSerializerSettings _serializerSettings;

        public RazorDynamicRegistrationService(IRazorCohostDynamicRegistrationService? dynamicRegistrationService, IClientLanguageServerManager clientLanguageServerManager)
        {
            _dynamicRegistrationService = dynamicRegistrationService;
            _clientLanguageServerManager = clientLanguageServerManager;

            var serializer = new JsonSerializer();
            serializer.AddVSInternalExtensionConverters();
            _serializerSettings = new JsonSerializerSettings { Converters = serializer.Converters };
        }

        public Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
        {
            if (_dynamicRegistrationService is null)
            {
                return Task.CompletedTask;
            }

            // We use a string to pass capabilities to/from Razor to avoid version issues with the Protocol DLL
            var serializedClientCapabilities = JsonConvert.SerializeObject(clientCapabilities, _serializerSettings);
            var razorCohostClientLanguageServerManager = new RazorCohostClientLanguageServerManager(_clientLanguageServerManager);

            var requestContext = new RazorCohostRequestContext(context);
            return _dynamicRegistrationService.RegisterAsync(serializedClientCapabilities, requestContext, cancellationToken);
        }
    }
}
