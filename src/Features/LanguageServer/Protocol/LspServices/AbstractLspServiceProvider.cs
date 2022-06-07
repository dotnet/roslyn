// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer;
internal class AbstractLspServiceProvider
{
    private readonly ImmutableArray<Lazy<ILspService, LspServiceMetadataView>> _lspServices;
    private readonly ImmutableArray<Lazy<ILspServiceFactory, LspServiceMetadataView>> _lspServiceFactories;

    public AbstractLspServiceProvider(
        IEnumerable<Lazy<ILspService, LspServiceMetadataView>> lspServices,
        IEnumerable<Lazy<ILspServiceFactory, LspServiceMetadataView>> lspServiceFactories)
    {
        _lspServices = lspServices.ToImmutableArray();
        _lspServiceFactories = lspServiceFactories.ToImmutableArray();
    }

    public LspServices CreateServices(WellKnownLspServerKinds serverKind, ImmutableArray<Lazy<ILspService, LspServiceMetadataView>> baseServices)
    {
        return new LspServices(_lspServices, _lspServiceFactories, serverKind, baseServices);
    }
}
