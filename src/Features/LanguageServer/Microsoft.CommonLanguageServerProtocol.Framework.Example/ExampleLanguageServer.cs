// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommonLanguageServerProtocol.Framework.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using StreamJsonRpc;

namespace Microsoft.CommonLanguageServerProtocol.Framework.Example;

public class ExampleLanguageServer : AbstractLanguageServer<ExampleRequestContext>
{
    public ExampleLanguageServer(JsonRpc jsonRpc, ILspLogger logger) : base(jsonRpc, logger)
    {
        // This spins up the queue and ensure the LSP is ready to start receiving requests
        Initialize();
    }

    protected override ILspServices ConstructLspServices()
    {
        var serviceCollection = new ServiceCollection();

        var _ = AddHandlers(serviceCollection)
            .AddSingleton<ILspLogger>(_logger)
            .AddSingleton<IRequestContextFactory<ExampleRequestContext>, ExampleRequestContextFactory>()
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

    private static IServiceCollection AddHandlers(IServiceCollection serviceCollection)
    {
        _ = serviceCollection
            .AddSingleton<IMethodHandler, InitializeHandler<InitializeParams, InitializeResult, ExampleRequestContext>>()
            .AddSingleton<IMethodHandler, InitializedHandler<InitializedParams, ExampleRequestContext>>();
        return serviceCollection;
    }
}
