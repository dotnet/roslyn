// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.VisualStudio.LanguageServer.Protocol
{
    /// <summary>
    /// Class which contains the string values for CodeMapper-related LSP messages.
    /// </summary>
    internal static class MapperMethods
    {
        /// <summary>
        /// Method name for 'textDocument/mapCode'.
        /// </summary>
        public const string TextDocumentMapCodeName = "textDocument/mapCode";

        /// <summary>
        /// Strongly typed message object for 'textDocument/mapCode'
        /// </summary>
        public readonly static LspRequest<MapCodeParams, WorkspaceEdit?> TextDocumentMapCode = new(TextDocumentMapCodeName);
    }
}
