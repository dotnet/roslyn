// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.ServerLifetime;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal sealed class RoslynLanguageServer : SystemTextJsonLanguageServer<RequestContext>, IOnInitialized
    {
        private readonly AbstractLspServiceProvider _lspServiceProvider;
        private readonly FrozenDictionary<string, ImmutableArray<BaseService>> _baseServices;
        private readonly WellKnownLspServerKinds _serverKind;

        public RoslynLanguageServer(
            AbstractLspServiceProvider lspServiceProvider,
            JsonRpc jsonRpc,
            JsonSerializerOptions serializerOptions,
            ICapabilitiesProvider capabilitiesProvider,
            AbstractLspLogger logger,
            HostServices hostServices,
            ImmutableArray<string> supportedLanguages,
            WellKnownLspServerKinds serverKind,
            AbstractTypeRefResolver? typeRefResolver = null)
            : base(jsonRpc, serializerOptions, logger, typeRefResolver)
        {
            _lspServiceProvider = lspServiceProvider;
            _serverKind = serverKind;

            // Create services that require base dependencies (jsonrpc) or are more complex to create to the set manually.
            _baseServices = GetBaseServices(jsonRpc, logger, capabilitiesProvider, hostServices, serverKind, supportedLanguages);

            // This spins up the queue and ensure the LSP is ready to start receiving requests
            Initialize();
        }

        public static SystemTextJsonFormatter CreateJsonMessageFormatter()
        {
            var messageFormatter = new SystemTextJsonFormatter();
            messageFormatter.JsonSerializerOptions.AddLspSerializerOptions();
            return messageFormatter;
        }

        protected override ILspServices ConstructLspServices()
        {
            return _lspServiceProvider.CreateServices(_serverKind, _baseServices);
        }

        protected override IRequestExecutionQueue<RequestContext> ConstructRequestExecutionQueue()
        {
            var provider = GetLspServices().GetRequiredService<IRequestExecutionQueueProvider<RequestContext>>();
            return provider.CreateRequestExecutionQueue(this, Logger, HandlerProvider);
        }

        private FrozenDictionary<string, ImmutableArray<BaseService>> GetBaseServices(
            JsonRpc jsonRpc,
            AbstractLspLogger logger,
            ICapabilitiesProvider capabilitiesProvider,
            HostServices hostServices,
            WellKnownLspServerKinds serverKind,
            ImmutableArray<string> supportedLanguages)
        {
            // This map will hold either a single BaseService instance, or an ImmutableArray<BaseService>.Builder.
            var baseServiceMap = new Dictionary<string, object>();

            var clientLanguageServerManager = new ClientLanguageServerManager(jsonRpc);
            var lifeCycleManager = new LspServiceLifeCycleManager(clientLanguageServerManager);

            AddService<IClientLanguageServerManager>(clientLanguageServerManager);
            AddService<ILspLogger>(logger);
            AddService<AbstractLspLogger>(logger);
            AddService<ICapabilitiesProvider>(capabilitiesProvider);
            AddService<ILifeCycleManager>(lifeCycleManager);
            AddService(new ServerInfoProvider(serverKind, supportedLanguages));
            AddLazyService<AbstractRequestContextFactory<RequestContext>>((lspServices) => new RequestContextFactory(lspServices));
            AddLazyService<IRequestExecutionQueue<RequestContext>>((_) => GetRequestExecutionQueue());
            AddLazyService<AbstractTelemetryService>((lspServices) => new TelemetryService(lspServices));
            AddService<IInitializeManager>(new InitializeManager());
            AddService<IMethodHandler>(new InitializeHandler());
            AddService<IMethodHandler>(new InitializedHandler());
            AddService<IOnInitialized>(this);
            AddService<ILanguageInfoProvider>(new LanguageInfoProvider());

            // In all VS cases, we already have a misc workspace.  Specifically
            // Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.MiscellaneousFilesWorkspace.  In
            // those cases, we do not need to add an additional workspace to manage new files we hear about.  So only
            // add the LspMiscellaneousFilesWorkspace for hosts that have not already brought their own.
            if (serverKind == WellKnownLspServerKinds.CSharpVisualBasicLspServer)
                AddLazyService<LspMiscellaneousFilesWorkspace>(lspServices => new LspMiscellaneousFilesWorkspace(lspServices, hostServices));

            return baseServiceMap.ToFrozenDictionary(
                keySelector: kvp => kvp.Key,
                elementSelector: kvp => kvp.Value switch
                {
                    BaseService service => [service],
                    ImmutableArray<BaseService>.Builder builder => builder.ToImmutable(),
                    _ => throw ExceptionUtilities.Unreachable()
                });

            void AddService<T>(T instance)
                where T : class
            {
                AddBaseService(BaseService.Create(instance));
            }

            void AddLazyService<T>(Func<ILspServices, T> creator)
                where T : class
            {
                AddBaseService(BaseService.CreateLazily(creator));
            }

            void AddBaseService(BaseService baseService)
            {
                var typeName = baseService.Type.FullName;
                Contract.ThrowIfNull(typeName);

                // If the service doesn't exist in the map yet, just add it.
                if (!baseServiceMap.TryGetValue(typeName, out var value))
                {
                    baseServiceMap.Add(typeName, baseService);
                    return;
                }

                // If the service exists in the map, check to see if it's a...
                switch (value)
                {
                    // ... BaseService. In this case, update the map with an ImmutableArray<BaseService>.Builder
                    // and add both the existing and new services to it.
                    case BaseService existingService:
                        var builder = ImmutableArray.CreateBuilder<BaseService>();
                        builder.Add(existingService);
                        builder.Add(baseService);

                        baseServiceMap[typeName] = builder;
                        break;

                    // ... ImmutableArray<BaseService>.Builder. In this case, just add the new service to the builder.
                    case ImmutableArray<BaseService>.Builder existingBuilder:
                        existingBuilder.Add(baseService);
                        break;

                    default:
                        throw ExceptionUtilities.Unreachable();
                }
            }
        }

        public Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
        {
            OnInitialized();
            return Task.CompletedTask;
        }

        protected override string GetLanguageForRequest(string methodName, JsonElement? parameters)
        {
            if (parameters == null)
            {
                Logger.LogInformation("No request parameters given, using default language handler");
                return LanguageServerConstants.DefaultLanguageName;
            }

            // For certain requests like text syncing we'll always use the default language handler
            // as we do not want languages to be able to override them.
            if (ShouldUseDefaultLanguage(methodName))
            {
                return LanguageServerConstants.DefaultLanguageName;
            }

            var lspWorkspaceManager = GetLspServices().GetRequiredService<LspWorkspaceManager>();

            // All general LSP spec document params have the following json structure
            // { "textDocument": { "uri": "<uri>" ... } ... }
            //
            // We can easily identify the URI for the request by looking for this structure
            if (parameters.Value.TryGetProperty("textDocument", out var textDocumentToken) ||
                parameters.Value.TryGetProperty("_vs_textDocument", out textDocumentToken))
            {
                var uriToken = textDocumentToken.GetProperty("uri");
                var uri = JsonSerializer.Deserialize<Uri>(uriToken, ProtocolConversions.LspJsonSerializerOptions);
                Contract.ThrowIfNull(uri, "Failed to deserialize uri property");
                var language = lspWorkspaceManager.GetLanguageForUri(uri);
                Logger.LogInformation($"Using {language} from request text document");
                return language;
            }

            // All the LSP resolve params have the following known json structure
            // { "data": { "TextDocument": { "uri": "<uri>" ... } ... } ... }
            //
            // We can deserialize the data object using our unified DocumentResolveData.
            //var dataToken = parameters["data"];
            if (parameters.Value.TryGetProperty("data", out var dataToken))
            {
                var data = JsonSerializer.Deserialize<DocumentResolveData>(dataToken, ProtocolConversions.LspJsonSerializerOptions);
                Contract.ThrowIfNull(data, "Failed to document resolve data object");
                var language = lspWorkspaceManager.GetLanguageForUri(data.TextDocument.Uri);
                Logger.LogInformation($"Using {language} from data text document");
                return language;
            }

            // This request is not for a textDocument and is not a resolve request.
            Logger.LogInformation("Request did not contain a textDocument, using default language handler");
            return LanguageServerConstants.DefaultLanguageName;

            static bool ShouldUseDefaultLanguage(string methodName)
            {
                return methodName switch
                {
                    Methods.InitializeName => true,
                    Methods.InitializedName => true,
                    Methods.TextDocumentDidOpenName => true,
                    Methods.TextDocumentDidChangeName => true,
                    Methods.TextDocumentDidCloseName => true,
                    Methods.TextDocumentDidSaveName => true,
                    Methods.ShutdownName => true,
                    Methods.ExitName => true,
                    _ => false,
                };
            }
        }
    }
}
