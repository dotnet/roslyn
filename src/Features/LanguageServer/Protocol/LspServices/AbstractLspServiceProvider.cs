// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.LanguageServer;

public class AbstractLspServiceProvider : ILspServiceProvider
{
    private readonly ImmutableArray<Lazy<ILspService, LspServiceMetadataView>> _lspServices;
    private readonly ImmutableArray<Lazy<ILspServiceFactory, LspServiceMetadataView>> _lspServiceFactories;

    private ImmutableArray<Lazy<ILspService, LspServiceMetadataView>>? _baseServices;

    private ImmutableArray<Lazy<ILspService, LspServiceMetadataView>> BaseServices => _baseServices ?? throw new InvalidOperationException($"{nameof(BaseServices)} called before {nameof(SetBaseServices)}");

    public AbstractLspServiceProvider(
        IEnumerable<Lazy<ILspService, LspServiceMetadataView>> lspServices,
        IEnumerable<Lazy<ILspServiceFactory, LspServiceMetadataView>> lspServiceFactories)
    {
        _lspServices = lspServices.ToImmutableArray();
        _lspServiceFactories = lspServiceFactories.ToImmutableArray();
    }

    public void SetBaseServices(ImmutableArray<Lazy<ILspService, LspServiceMetadataView>> baseServices)
    {
        _baseServices = baseServices;
    }

    public ILspServices CreateServices(string serverKind)
    {
        var serverEnum = WellKnownLspServerExtensions.WellKnownLspServerKindsFromString(serverKind);
        return new LspServices(_lspServices, _lspServiceFactories, serverEnum, BaseServices);
    }
}
