// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal class AbstractLspServiceProvider
{
    private readonly ImmutableArray<Lazy<ILspService, LspServiceMetadataView>> _lspServices;
    private readonly ImmutableArray<Lazy<ILspServiceFactory, LspServiceMetadataView>> _lspServiceFactories;

    public AbstractLspServiceProvider(
        IEnumerable<Lazy<ILspService, LspServiceMetadataView>> specificLspServices,
        IEnumerable<Lazy<ILspServiceFactory, LspServiceMetadataView>> specificLspServiceFactories,
        IEnumerable<Lazy<ILspService, LspServiceMetadataView>> generalLspServices,
        IEnumerable<Lazy<ILspServiceFactory, LspServiceMetadataView>> generalLspServiceFactories)
    {
        var joinsedLspServices = specificLspServices.Concat(generalLspServices);
        _lspServices = joinsedLspServices.ToImmutableArray();

        var joinedLspServiceFactories = specificLspServiceFactories.Concat(generalLspServiceFactories);
        _lspServiceFactories = joinedLspServiceFactories.ToImmutableArray();
    }

    public LspServices CreateServices(string serverKind, ImmutableArray<Lazy<ILspService, LspServiceMetadataView>> baseServices, IServiceCollection serviceCollection)
    {
        var serverEnum = WellKnownLspServerExtensions.WellKnownLspServerKindsFromString(serverKind);
        var lspServices = new LspServices(_lspServices, _lspServiceFactories, serverEnum, baseServices, serviceCollection);

        return lspServices;
    }
}
