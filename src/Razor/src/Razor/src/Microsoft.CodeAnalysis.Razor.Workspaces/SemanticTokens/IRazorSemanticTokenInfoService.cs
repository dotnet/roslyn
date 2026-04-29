// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.SemanticTokens;

internal interface IRazorSemanticTokensInfoService
{
    /// <summary>
    /// Gets the int array representing the semantic tokens for the given range.
    /// </summary>
    /// <remarks>See https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#textDocument_semanticTokens for details about the int array</remarks>
    Task<int[]?> GetSemanticTokensAsync(DocumentContext documentContext, LinePositionSpan range, bool colorBackground, Guid correlationId, CancellationToken cancellationToken);
}
