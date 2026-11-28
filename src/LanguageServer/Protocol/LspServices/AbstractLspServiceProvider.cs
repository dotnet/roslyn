// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal abstract class AbstractLspServiceProvider(
    IEnumerable<Lazy<ILspService, LspServiceMetadataView>> specificLspServices,
    IEnumerable<Lazy<ILspServiceFactory, LspServiceMetadataView>> specificLspServiceFactories,
    IEnumerable<Lazy<IJsonTypeInfoResolver>>? jsonTypeInfoResolvers)
{
    private readonly ImmutableArray<Lazy<ILspService, LspServiceMetadataView>> _lspServices = [.. specificLspServices];
    private readonly ImmutableArray<Lazy<ILspServiceFactory, LspServiceMetadataView>> _lspServiceFactories = [.. specificLspServiceFactories];
    private readonly ImmutableArray<Lazy<IJsonTypeInfoResolver>> _jsonTypeInfoResolvers = jsonTypeInfoResolvers is null ? [] : [.. jsonTypeInfoResolvers];

    public AbstractLspServiceProvider(
        IEnumerable<Lazy<ILspService, LspServiceMetadataView>> specificLspServices,
        IEnumerable<Lazy<ILspServiceFactory, LspServiceMetadataView>> specificLspServiceFactories)
        : this(specificLspServices, specificLspServiceFactories, jsonTypeInfoResolvers: null)
    {
    }

    internal JsonSerializerOptions ConfigureJsonSerializerOptions(JsonSerializerOptions options)
    {
        var insertionIndex = options.TypeInfoResolverChain.IndexOf(LspJsonSerializerContext.Resolver) + 1;
        foreach (var resolver in _jsonTypeInfoResolvers)
        {
            if (!options.TypeInfoResolverChain.Contains(resolver.Value))
                options.TypeInfoResolverChain.Insert(insertionIndex++, resolver.Value);
        }

        return options;
    }

    public LspServices CreateServices(WellKnownLspServerKinds serverKind, FrozenDictionary<string, ImmutableArray<BaseService>> baseServices)
    {
        var lspServices = new LspServices(_lspServices, _lspServiceFactories, serverKind, baseServices);

        return lspServices;
    }
}
