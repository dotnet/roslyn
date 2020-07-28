// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.VisualStudio.LanguageServer.Protocol;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    /// <summary>
    /// Result type for <see cref="SemanticTokensEditsHandler"/>.
    /// </summary>
    /// <remarks>
    /// This result type is used since <see cref="SemanticTokensEditsHandler"/> usually needs to
    /// return both SemanticTokens (for caching purposes) and SemanticTokensEdits (to return to LSP).
    /// </remarks>
    internal class SemanticTokensEditsResult

    {
        public LSP.SemanticTokens SemanticTokens { get; }

        public SemanticTokensEdits? SemanticTokensEdits { get; }

        public SemanticTokensEditsResult(LSP.SemanticTokens tokens)
        {
            SemanticTokens = tokens;
        }

        public SemanticTokensEditsResult(LSP.SemanticTokens tokens, SemanticTokensEdits edits)
        {
            SemanticTokens = tokens;
            SemanticTokensEdits = edits;
        }
    }
}
