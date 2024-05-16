﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
        private readonly ImmutableDictionary<Type, ImmutableArray<Func<ILspServices, object>>> _baseServices;
        private readonly WellKnownLspServerKinds _serverKind;

        public RoslynLanguageServer(
            AbstractLspServiceProvider lspServiceProvider,
            JsonRpc jsonRpc,
            JsonSerializerOptions serializerOptions,
            ICapabilitiesProvider capabilitiesProvider,
            AbstractLspLogger logger,
            HostServices hostServices,
            ImmutableArray<string> supportedLanguages,
            WellKnownLspServerKinds serverKind)
            : base(jsonRpc, serializerOptions, logger)
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

        private ImmutableDictionary<Type, ImmutableArray<Func<ILspServices, object>>> GetBaseServices(
            JsonRpc jsonRpc,
            AbstractLspLogger logger,
            ICapabilitiesProvider capabilitiesProvider,
            HostServices hostServices,
            WellKnownLspServerKinds serverKind,
            ImmutableArray<string> supportedLanguages)
        {
            var baseServices = new Dictionary<Type, ImmutableArray<Func<ILspServices, object>>>();
            var clientLanguageServerManager = new ClientLanguageServerManager(jsonRpc);
            var lifeCycleManager = new LspServiceLifeCycleManager(clientLanguageServerManager);

            AddBaseService<IClientLanguageServerManager>(clientLanguageServerManager);
            AddBaseService<ILspLogger>(logger);
            AddBaseService<AbstractLspLogger>(logger);
            AddBaseService<ICapabilitiesProvider>(capabilitiesProvider);
            AddBaseService<ILifeCycleManager>(lifeCycleManager);
            AddBaseService(new ServerInfoProvider(serverKind, supportedLanguages));
            AddBaseServiceFromFunc<AbstractRequestContextFactory<RequestContext>>((lspServices) => new RequestContextFactory(lspServices));
            AddBaseServiceFromFunc<IRequestExecutionQueue<RequestContext>>((_) => GetRequestExecutionQueue());
            AddBaseServiceFromFunc<AbstractTelemetryService>((lspServices) => new TelemetryService(lspServices));
            AddBaseService<IInitializeManager>(new InitializeManager());
            AddBaseService<IMethodHandler>(new InitializeHandler());
            AddBaseService<IMethodHandler>(new InitializedHandler());
            AddBaseService<IOnInitialized>(this);
            AddBaseService<ILanguageInfoProvider>(new LanguageInfoProvider());

            // In all VS cases, we already have a misc workspace.  Specifically
            // Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.MiscellaneousFilesWorkspace.  In
            // those cases, we do not need to add an additional workspace to manage new files we hear about.  So only
            // add the LspMiscellaneousFilesWorkspace for hosts that have not already brought their own.
            if (serverKind == WellKnownLspServerKinds.CSharpVisualBasicLspServer)
                AddBaseServiceFromFunc<LspMiscellaneousFilesWorkspace>(lspServices => new LspMiscellaneousFilesWorkspace(lspServices, hostServices));

            return baseServices.ToImmutableDictionary();

            void AddBaseService<T>(T instance) where T : class
            {
                AddBaseServiceFromFunc<T>((_) => instance);
            }

            void AddBaseServiceFromFunc<T>(Func<ILspServices, object> creatorFunc)
            {
                var added = baseServices.GetValueOrDefault(typeof(T), []).Add(creatorFunc);
                baseServices[typeof(T)] = added;
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
