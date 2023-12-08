// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Completion
{
    /// <summary>
    /// Provides the intermediate data passed from CompletionHandler to CompletionResolveHandler.
    /// Passed along via <see cref="LSP.CompletionItem.Data"/>.
    /// </summary>
    internal sealed class CompletionResolveData
    {
        /// <summary>
        /// ID associated with the item's completion list.
        /// </summary>
        /// <remarks>
        /// Used to retrieve the correct completion list from <see cref="CompletionListCache"/>.
        /// </remarks>
        public long? ResultId { get; set; }
    }
}
