// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.EmbeddedLanguages;

namespace Microsoft.CodeAnalysis.DocumentHighlighting
{
    /// <inheritdoc cref="IDocumentHighlightsService"/>
    internal interface IEmbeddedLanguageDocumentHighlighter : IEmbeddedLanguageFeatureService
    {
        /// <inheritdoc cref="IDocumentHighlightsService.GetDocumentHighlightsAsync"/>
        ImmutableArray<DocumentHighlights> GetDocumentHighlights(
            Document document,
            SemanticModel semanticModel,
            SyntaxToken token,
            int position,
            HighlightingOptions options,
            CancellationToken cancellationToken);
    }
}
