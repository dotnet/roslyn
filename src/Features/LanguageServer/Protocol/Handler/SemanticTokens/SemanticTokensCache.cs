// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    /// <summary>
    /// Caches the semantic token information that needs to preserved between multiple calls to <see cref="SemanticTokensHandler"/>
    /// and <see cref="SemanticTokensEditsHandler"/>.
    /// </summary>
    internal class SemanticTokensCache
    {
        /// <summary>
        /// The document the cached semantic tokens apply to.
        /// </summary>
        public LSP.TextDocumentIdentifier? Document { get; private set; }

        public LSP.SemanticTokens Tokens { get; private set; }

        public SemanticTokensCache()
        {
            Document = new LSP.TextDocumentIdentifier();
            Tokens = new LSP.SemanticTokens();
        }

        public void UpdateCache(LSP.TextDocumentIdentifier? document, LSP.SemanticTokens tokens)
        {
            Document = document;
            Tokens = tokens;
        }
    }
}
