// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Contracts;

/// <summary>
/// Source of hot reload diagnostics.
/// </summary>
internal interface IHotReloadDiagnosticSource
{
    /// <summary>
    /// Text document for which diagnostics are provided.
    /// </summary>
    DocumentId DocumentId { get; }

    /// <summary>
    /// Provides list of diagnostics for the given document.
    /// </summary>
    ValueTask<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(HotReloadRequestContext request, CancellationToken cancellationToken);
}
