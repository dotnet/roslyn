// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Contracts;

/// <summary>
/// Provides diagnostic sources.
/// </summary>
internal interface IHotReloadDiagnosticSourceProvider
{
    /// <summary>
    /// True if this provider is for documents. False if it is for a workspace, i.e. for unopened documents.
    /// </summary>
    bool IsDocument { get; }

    /// <summary>
    /// Creates the diagnostic sources.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask<ImmutableArray<IHotReloadDiagnosticSource>> CreateDiagnosticSourcesAsync(HotReloadRequestContext context, CancellationToken cancellationToken);
}
