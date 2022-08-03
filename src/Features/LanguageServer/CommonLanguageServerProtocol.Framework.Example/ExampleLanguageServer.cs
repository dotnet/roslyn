// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommonLanguageServerProtocol.Framework.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using StreamJsonRpc;

namespace CommonLanguageServerProtocol.Framework.Example;

public class ExampleLanguageServer : LanguageServer<ExampleRequestContext>
{
    private const string ExampleServerKindName = "ExampleLanguageServer";

    public ExampleLanguageServer(JsonRpc jsonRpc, ILspLogger logger) : base(jsonRpc, logger, serverKind: ExampleServerKindName)
    {
        Initialize();
    }

    protected override ILspServices ConstructLspServices()
    {
        var serviceCollection = new ServiceCollection();

        AddHandlers(serviceCollection)
            .AddSingleton<ILspLogger>(_logger)
            .AddSingleton<IRequestContextFactory<ExampleRequestContext>, ExampleRequestContextFactory>()
            .AddSingleton<ICapabilitiesManager<InitializeParams, InitializeResult>, CapabilitiesManager>()
            .AddSingleton<ILifeCycleManager>((s) => new LifeCycleManager<ExampleRequestContext>(this));

        var lspServices = new ExampleLspServices(serviceCollection);

        return lspServices;
    }

    private static IServiceCollection AddHandlers(IServiceCollection serviceCollection)
    {
        serviceCollection
            .AddSingleton<IRequestHandler, InitializeHandler<InitializeParams, InitializeResult, ExampleRequestContext>>()
            .AddSingleton<IRequestHandler, InitializedHandler<InitializedParams, ExampleRequestContext>>()
            .AddSingleton<IRequestHandler, ShutdownHandler<ExampleRequestContext>>()
            .AddSingleton<IRequestHandler, ExitHandler<ExampleRequestContext>>();

        return serviceCollection;
    }
}
