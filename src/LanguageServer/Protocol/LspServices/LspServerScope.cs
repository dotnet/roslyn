// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Root part of a <see cref="LspServiceComposition.SharingBoundary"/>.  One instance is created for each LSP server
/// by <see cref="AbstractLspServiceProvider"/>; every per-server LSP service is resolved relative to it.
/// </summary>
[Export(typeof(LspServerScope)), Shared(LspServiceComposition.SharingBoundary)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class LspServerScope(
    [ImportMany(LspServiceComposition.ContractName)] IEnumerable<Lazy<object, IDictionary<string, object>>> services)
{
    private LspServices? _lspServices;

    public LspServices LspServices => _lspServices ?? throw new InvalidOperationException($"{nameof(LspServerScope)} has not been initialized.");

    public void Initialize(LspServices lspServices)
    {
        Contract.ThrowIfFalse(_lspServices is null);
        _lspServices = lspServices;
    }

    /// <summary>
    /// Returns the per-server services that apply to <paramref name="lspContract"/>.  Values are shared with any part
    /// in this scope that imports the same service with <see cref="LspService{T}"/>.
    /// </summary>
    public ImmutableArray<Lazy<object, LspServiceMetadataView>> GetServices(string lspContract)
    {
        Contract.ThrowIfTrue(lspContract == ProtocolConstants.AllLspContracts, $"{ProtocolConstants.AllLspContracts} is not a valid server contract.");

        var builder = ImmutableArray.CreateBuilder<Lazy<object, LspServiceMetadataView>>();
        foreach (var service in services)
        {
            var metadata = new LspServiceMetadataView(service.Metadata);
            if (metadata.LspContract != ProtocolConstants.AllLspContracts && metadata.LspContract != lspContract)
                continue;

            builder.Add(new Lazy<object, LspServiceMetadataView>(() => service.Value, metadata));
        }

        return builder.ToImmutable();
    }
}
