// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Contracts;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Internal;

[Export(typeof(IHotReloadDiagnosticManager)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class HotReloadDiagnosticManager(IDiagnosticsRefresher diagnosticsRefresher) : IHotReloadDiagnosticManager
{
    private ImmutableArray<IHotReloadDiagnosticSource> _sources = ImmutableArray<IHotReloadDiagnosticSource>.Empty;

    ImmutableArray<IHotReloadDiagnosticSource> IHotReloadDiagnosticManager.Sources => _sources;
    void IHotReloadDiagnosticManager.Refresh() => diagnosticsRefresher.RequestWorkspaceRefresh();

    void IHotReloadDiagnosticManager.Register(IHotReloadDiagnosticSource source)
    {
        // We use array instead of e.g. HashSet because we expect the number of sources to be small. Usually 1.
        if (!_sources.Contains(source))
            _sources = _sources.Add(source);
    }

    void IHotReloadDiagnosticManager.Unregister(IHotReloadDiagnosticSource source)
        => _sources = _sources.Remove(source);
}
