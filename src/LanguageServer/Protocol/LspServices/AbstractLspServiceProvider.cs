// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Creates the <see cref="LspServices"/> for LSP servers of a given LSP contract.
/// </summary>
/// <param name="scopeFactory">Creates a new <see cref="LspServiceComposition.SharingBoundary"/> for each server.</param>
/// <param name="lspContract">The LSP contract (e.g. <see cref="ProtocolConstants.RoslynLspLanguagesContract"/>) of the servers.</param>
internal abstract class AbstractLspServiceProvider(
    ExportFactory<LspServerScope> scopeFactory,
    string lspContract)
{
    public LspServices CreateServices(WellKnownLspServerKinds serverKind, FrozenDictionary<string, ImmutableArray<BaseService>> baseServices)
    {
        // Each server gets a new MEF sharing boundary, so every per-server LSP service is instantiated once per server.
        var scopeExport = scopeFactory.CreateExport();
        var scope = scopeExport.Value;

        var lspServices = new LspServices(scope.GetServices(lspContract), serverKind, baseServices, scopeExport);
        scope.Initialize(lspServices);

        return lspServices;
    }
}
