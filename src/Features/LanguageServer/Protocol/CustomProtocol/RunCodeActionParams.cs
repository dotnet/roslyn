// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.CustomProtocol
{
    /// <summary>
    /// Params of a RunCodeAction command that is returned by the GetCodeActionsHandler.
    /// This is an implementation detail of the server that is passed to the clients
    /// and returned back without the clients parsing it, so no need to make it public.
    /// </summary>
    internal class RunCodeActionParams
    {
        /// <summary>
        /// The original text document.
        /// </summary>
        public TextDocumentIdentifier TextDocument { get; set; }

        /// <summary>
        /// The range of the actions.
        /// </summary>
        public Range Range { get; set; }

        /// <summary>
        /// Title of the action to execute.
        /// </summary>
        public string Title { get; set; }
    }
}
