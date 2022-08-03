// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
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
        serviceCollection
            .AddSingleton<IRequestHandler, InitializeHandler>()
            .AddSingleton<ILspLogger>(_logger)
            .AddSingleton<IRequestContextFactory<ExampleRequestContext>, ExampleRequestContextFactory>()
            .AddSingleton<ClientCapabilitiesProvider>();

        var lspServices = new ExampleLspServices(serviceCollection);

        return lspServices;
    }
}
