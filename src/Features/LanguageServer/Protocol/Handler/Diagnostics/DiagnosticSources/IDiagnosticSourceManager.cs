// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.DiagnosticSources
{
    /// <summary>
    /// Manages the diagnostic sources that provide diagnostics for the language server.
    /// </summary>
    internal interface IDiagnosticSourceManager
    {
        /// <summary>
        /// Returns the names of all the sources that provide diagnostics for the given <paramref name="isDocument"/>.
        /// </summary>
        /// <param name="isDocument">True for document sources and false for workspace sources.</param>
        IEnumerable<string> GetSourceNames(bool isDocument);

        /// <summary>
        /// Creates the diagnostic sources for the given <paramref name="sourceName"/> and <paramref name="isDocument"/>.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="sourceName">Source name.</param>
        /// <param name="isDocument">True for document sources and false for workspace sources.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        ValueTask<ImmutableArray<IDiagnosticSource>> CreateDiagnosticSourcesAsync(RequestContext context, string? sourceName, bool isDocument, CancellationToken cancellationToken);
    }
}
