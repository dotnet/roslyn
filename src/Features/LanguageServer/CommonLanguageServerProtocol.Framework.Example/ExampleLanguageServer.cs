// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using StreamJsonRpc;

namespace CommonLanguageServerProtocol.Framework.Example;

internal class ExampleLanguageServer : LanguageServer<ExampleRequestContext>
{
    private const string ExampleServerKindName = "ExampleLanguageServer";

    public ExampleLanguageServer(JsonRpc jsonRpc, ILspLogger logger) : base(jsonRpc, logger, serverKind: ExampleServerKindName)
    {
    }

    protected override ILspServices GetLspServices()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<InitializeHandler>();

        var serviceProvider = serviceCollection.BuildServiceProvider();

        var lspServices = new ExampleLspServices(serviceProvider);

        return lspServices;
    }
}
