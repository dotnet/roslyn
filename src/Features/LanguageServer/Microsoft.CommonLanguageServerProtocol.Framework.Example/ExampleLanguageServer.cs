// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CommonLanguageServerProtocol.Framework.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Roslyn.LanguageServer.Protocol;
using StreamJsonRpc;

namespace Microsoft.CommonLanguageServerProtocol.Framework.Example;

internal class ExampleLanguageServer : AbstractLanguageServer<ExampleRequestContext>
{
    private readonly Action<IServiceCollection>? _addExtraHandlers;

    public ExampleLanguageServer(JsonRpc jsonRpc, ILspLogger logger, Action<IServiceCollection>? addExtraHandlers) : base(jsonRpc, logger)
    {
        _addExtraHandlers = addExtraHandlers;
        // This spins up the queue and ensure the LSP is ready to start receiving requests
        Initialize();
    }

    protected override ILspServices ConstructLspServices()
    {
        var serviceCollection = new ServiceCollection();

        var _ = AddHandlers(serviceCollection)
            .AddSingleton<ILspLogger>(_logger)
            .AddSingleton<AbstractRequestContextFactory<ExampleRequestContext>, ExampleRequestContextFactory>()
            .AddSingleton<AbstractHandlerProvider>(s => HandlerProvider)
            .AddSingleton<IInitializeManager<InitializeParams, InitializeResult>, CapabilitiesManager>()
            .AddSingleton(this);

        var lifeCycleManager = GetLifeCycleManager();
        if (lifeCycleManager != null)
        {
            serviceCollection.AddSingleton(lifeCycleManager);
        }

        var lspServices = new ExampleLspServices(serviceCollection);

        return lspServices;
    }

    protected virtual ILifeCycleManager? GetLifeCycleManager()
    {
        return null;
    }

    protected virtual IServiceCollection AddHandlers(IServiceCollection serviceCollection)
    {
        _ = serviceCollection
            .AddSingleton<IMethodHandler, MultiRegisteringHandler>()
            .AddSingleton<IMethodHandler, InitializeHandler<InitializeParams, InitializeResult, ExampleRequestContext>>()
            .AddSingleton<IMethodHandler, InitializedHandler<InitializedParams, ExampleRequestContext>>();

        if (_addExtraHandlers is not null)
        {
            _addExtraHandlers(serviceCollection);
        }

        return serviceCollection;
    }
}
