// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Contracts;

internal interface IHotReloadDiagnosticManager
{
    /// <summary>
    /// Refreshes hot reload diagnostics.
    /// </summary>
    void RequestRefresh();

    /// <summary>
    /// Registers providers of hot reload diagnostics. Callers are responsible for refreshing diagnostics after registration.
    /// </summary>
    void Register(IEnumerable<IHotReloadDiagnosticSourceProvider> providers);

    /// <summary>
    /// Unregisters providers of hot reload diagnostics. Callers are responsible for refreshing diagnostics after un-registration.
    /// </summary>
    void Unregister(IEnumerable<IHotReloadDiagnosticSourceProvider> providers);

    /// <summary>
    /// Providers.
    /// </summary>
    ImmutableArray<IHotReloadDiagnosticSourceProvider> Providers { get; }
}
