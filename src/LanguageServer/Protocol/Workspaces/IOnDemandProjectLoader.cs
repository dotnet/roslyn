// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Loads projects on demand for file-backed document requests when the file is not currently in a loaded project.
/// </summary>
internal interface IOnDemandProjectLoader : ILspService
{
    /// <summary>
    /// Attempts to load one or more projects that may contain <paramref name="uri"/>.
    /// </summary>
    /// <returns><see langword="true"/> when at least one project load was initiated; otherwise, <see langword="false"/>.</returns>
    ValueTask<bool> TryLoadProjectsForDocumentAsync(DocumentUri uri, CancellationToken cancellationToken);
}
