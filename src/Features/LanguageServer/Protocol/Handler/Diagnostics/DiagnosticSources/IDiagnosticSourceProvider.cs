// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

/// <summary>
/// Provides diagnostic sources.
/// </summary>
internal interface IDiagnosticSourceProvider
{
    /// <summary>
    /// True if this provider is for documents, false if it is for workspace.
    /// </summary>
    bool IsDocument { get; }

    /// <summary>
    /// Source names that this provider can provide.
    /// </summary>
    ImmutableArray<string> SourceNames { get; }

    /// <summary>
    /// Creates the diagnostic sources for the given <paramref name="sourceName"/>.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="sourceName">Source name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask<ImmutableArray<IDiagnosticSource>> CreateDiagnosticSourcesAsync(RequestContext context, string sourceName, CancellationToken cancellationToken);
}

