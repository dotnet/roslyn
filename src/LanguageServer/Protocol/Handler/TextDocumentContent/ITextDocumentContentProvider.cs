// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.TextDocumentContent;

/// <summary>
/// LSP service that provides text content for documents identified by a specific URI scheme.
/// Implementations are collected by the <see cref="TextDocumentContentHandler"/> to delegate
/// content retrieval, and by <see cref="DefaultCapabilitiesProvider"/> to register supported schemes.
/// </summary>
internal interface ITextDocumentContentProvider : ILspService
{
    /// <summary>
    /// The URI scheme this provider handles (e.g. <c>"roslyn-source-generated"</c>).
    /// </summary>
    string Scheme { get; }

    /// <summary>
    /// Returns the text content for the given <paramref name="document"/>.
    /// </summary>
    Task<string> GetTextAsync(TextDocument document, CancellationToken cancellationToken);
}
