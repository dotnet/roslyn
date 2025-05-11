// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Contracts;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Internal;

[Export(typeof(IHotReloadDiagnosticManager)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class HotReloadDiagnosticManager([Import] IDiagnosticsRefresher diagnosticsRefresher) : IHotReloadDiagnosticManager
{
    private readonly object _syncLock = new();
    public ImmutableArray<IHotReloadDiagnosticSourceProvider> Providers { get; private set; } = [];

    public void RequestRefresh()
            => diagnosticsRefresher.RequestWorkspaceRefresh();

    public void Register(IEnumerable<IHotReloadDiagnosticSourceProvider> providers)
    {
        // We use array instead of e.g. HashSet because we expect the number of sources to be small.
        // Usually 2, one workspace and one document provider.
        lock (_syncLock)
        {
            foreach (var provider in providers)
            {
                if (!Providers.Contains(provider))
                    Providers = Providers.Add(provider);
            }
        }
    }

    public void Unregister(IEnumerable<IHotReloadDiagnosticSourceProvider> providers)
    {
        lock (_syncLock)
        {
            foreach (var provider in Providers)
                Providers = Providers.Remove(provider);
        }
    }

}
