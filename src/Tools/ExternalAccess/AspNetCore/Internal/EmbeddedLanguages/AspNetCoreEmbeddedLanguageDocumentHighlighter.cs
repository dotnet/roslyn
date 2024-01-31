// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.Internal.EmbeddedLanguages
{
    [ExportEmbeddedLanguageDocumentHighlighter(
        nameof(AspNetCoreEmbeddedLanguageDocumentHighlighter),
        [LanguageNames.CSharp],
        supportsUnannotatedAPIs: false,
        // Add more syntax names here in the future if there are additional cases ASP.Net would like to light up on.
        identifiers: ["Route"]), Shared]
    internal class AspNetCoreEmbeddedLanguageDocumentHighlighter : IEmbeddedLanguageDocumentHighlighter
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AspNetCoreEmbeddedLanguageDocumentHighlighter()
        {
        }

        public ImmutableArray<DocumentHighlights> GetDocumentHighlights(
            Document document,
            SemanticModel semanticModel,
            SyntaxToken token,
            int position,
            HighlightingOptions options,
            CancellationToken cancellationToken)
        {
            var highlighters = AspNetCoreDocumentHighlighterExtensionProvider.GetExtensions(document.Project);
            foreach (var highlighter in highlighters)
            {
                var highlights = highlighter.GetDocumentHighlights(semanticModel, token, position, cancellationToken);
                if (!highlights.IsDefaultOrEmpty)
                {
                    return highlights.SelectAsArray(h => new DocumentHighlights(document,
                        h.HighlightSpans.SelectAsArray(hs => new HighlightSpan(hs.TextSpan, ConvertKind(hs.Kind)))));
                }
            }

            return ImmutableArray<DocumentHighlights>.Empty;

            static HighlightSpanKind ConvertKind(AspNetCoreHighlightSpanKind kind)
            {
                return kind switch
                {
                    AspNetCoreHighlightSpanKind.None => HighlightSpanKind.None,
                    AspNetCoreHighlightSpanKind.Definition => HighlightSpanKind.Definition,
                    AspNetCoreHighlightSpanKind.Reference => HighlightSpanKind.Reference,
                    AspNetCoreHighlightSpanKind.WrittenReference => HighlightSpanKind.WrittenReference,
                    _ => throw new NotImplementedException(),
                };
            }
        }
    }
}
