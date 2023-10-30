// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Completion
{
    /// <summary>
    /// Provides the intermediate data passed from CompletionHandler to CompletionResolveHandler.
    /// Passed along via <see cref="CompletionItem.Data"/>.
    /// <param name="ResultId">the resultId associated with the completion created on original request.</param>
    /// <param name="DocumentId">the text document id associated with the completion item to resolve.</param>
    /// </summary>
    internal sealed record CompletionResolveData(long ResultId, long DocumentId) : DocumentIdResolveData(DocumentId);
}
