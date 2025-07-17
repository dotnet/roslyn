// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal abstract class AbstractLspServiceProvider(
    IEnumerable<Lazy<ILspService, LspServiceMetadataView>> specificLspServices,
    IEnumerable<Lazy<ILspServiceFactory, LspServiceMetadataView>> specificLspServiceFactories)
{
    private readonly ImmutableArray<Lazy<ILspService, LspServiceMetadataView>> _lspServices = [.. specificLspServices];
    private readonly ImmutableArray<Lazy<ILspServiceFactory, LspServiceMetadataView>> _lspServiceFactories = [.. specificLspServiceFactories];

    public LspServices CreateServices(WellKnownLspServerKinds serverKind, FrozenDictionary<string, ImmutableArray<BaseService>> baseServices)
    {
        var lspServices = new LspServices(_lspServices, _lspServiceFactories, serverKind, baseServices);

        return lspServices;
    }
}
