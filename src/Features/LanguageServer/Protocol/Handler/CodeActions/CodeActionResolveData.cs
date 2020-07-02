// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions
{
    /// <summary>
    /// Provides data needed to resolve code actions.
    /// </summary>
    internal class CodeActionResolveData
    {
        /// <summary>
        /// The unique title of a code action. No two code actions should
        /// have the same unique title.
        /// </summary>
        /// <remarks>
        /// The distinct tiel is currently set as:
        /// name of top level code action + name of nested code action + nested nested code action etc.
        /// </remarks>
        public string DistinctTitle { get; set; }

        public LSP.Range Range { get; set; }

        public LSP.TextDocumentIdentifier TextDocument { get; set; }
    }
}
