// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions
{
    /// <summary>
    /// This class provides the intermediate data passed between CodeActionsHandler, CodeActionResolveHandler,
    /// and RunCodeActionsHandler. The class provides enough information for each handler to identify the code
    /// action that it is dealing with. The information is passed along via the Data property in LSP.VSCodeAction. 
    /// </summary>
    internal class CodeActionResolveData
    {
        /// <summary>
        /// The unique identifier of a code action. No two code actions should have the same unique identifier.
        /// </summary>
        /// <remarks>
        /// The unique identifier is currently set as:
        /// name of top level code action + '|' + name of nested code action + '|' + name of nested nested code action + etc.
        /// e.g. 'Suppress or Configure issues|Suppress IDEXXXX|in Source'
        /// </remarks>
        public string UniqueIdentifier { get; }

        /// <summary>
        /// Identifies the Code Action Provider which produced this Code Action.
        /// </summary>
        /// <remarks>
        /// The unique identifier is currently set as:
        /// name of top level code action provider + '|' + name of code action provider implementation
        /// e.g. 'Add Await Code Action Provider | GetTitleWithConfigureAwait'
        /// </remarks>
        public string ProviderName { get; }

        public LSP.Range Range { get; }

        public LSP.TextDocumentIdentifier TextDocument { get; }

        public CodeActionResolveData(string uniqueIdentifier, string providerName, LSP.Range range, LSP.TextDocumentIdentifier textDocument)
        {
            UniqueIdentifier = uniqueIdentifier;
            ProviderName = providerName;
            Range = range;
            TextDocument = textDocument;
        }
    }
}
