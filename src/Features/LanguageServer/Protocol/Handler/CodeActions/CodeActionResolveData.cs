// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Newtonsoft.Json;
using LSP = Roslyn.LanguageServer.Protocol;

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

        public ImmutableArray<string> CustomTags { get; }

        public LSP.Range Range { get; }

        public LSP.TextDocumentIdentifier TextDocument { get; }

        public string[] CodeActionPath { get; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string[]? FixAllFlavors { get; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ImmutableArray<LSP.CodeAction>? NestedCodeActions { get; }

        public CodeActionResolveData(
            string uniqueIdentifier,
            ImmutableArray<string> customTags,
            LSP.Range range,
            LSP.TextDocumentIdentifier textDocument,
            string[] codeActionPath,
            string[]? fixAllFlavors,
            ImmutableArray<LSP.CodeAction>? nestedCodeActions)
        {
            UniqueIdentifier = uniqueIdentifier;
            CustomTags = customTags;
            Range = range;
            TextDocument = textDocument;
            CodeActionPath = codeActionPath;
            FixAllFlavors = fixAllFlavors;
            NestedCodeActions = nestedCodeActions;
        }
    }
}
