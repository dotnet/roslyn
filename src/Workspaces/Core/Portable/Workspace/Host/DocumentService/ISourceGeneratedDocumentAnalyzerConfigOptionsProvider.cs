// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Host;

internal interface ISourceGeneratedDocumentAnalyzerConfigOptionsProvider : IWorkspaceService
{
    /// <summary>
    /// Allows a host to supply analyzer-config options for code-action cleanup when a source-generated
    /// document represents another host document.
    /// </summary>
    /// <returns>
    /// The options to use for <paramref name="sourceGeneratedDocument"/>, or <see langword="null"/> to use normal lookup.
    /// </returns>
    ValueTask<AnalyzerConfigOptions?> GetOptionsAsync(SourceGeneratedDocument sourceGeneratedDocument, CancellationToken cancellationToken);
}
