// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Composition;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.DocumentHighlighting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.DocumentHighlighting
{
    internal static class FSharpHighlightSpanKindHelpers
    {
        public static HighlightSpanKind ConvertTo(FSharpHighlightSpanKind kind)
        {
            switch (kind)
            {
                case FSharpHighlightSpanKind.None:
                    {
                        return HighlightSpanKind.None;
                    }

                case FSharpHighlightSpanKind.Definition:
                    {
                        return HighlightSpanKind.Definition;
                    }

                case FSharpHighlightSpanKind.Reference:
                    {
                        return HighlightSpanKind.Reference;
                    }

                case FSharpHighlightSpanKind.WrittenReference:
                    {
                        return HighlightSpanKind.WrittenReference;
                    }

                default:
                    {
                        throw ExceptionUtilities.UnexpectedValue(kind);
                    }
            }
        }
    }

    [Shared]
    [ExportLanguageService(typeof(IDocumentHighlightsService), LanguageNames.FSharp)]
    internal class FSharpDocumentHighlightsService : IDocumentHighlightsService
    {
        private readonly IFSharpDocumentHighlightsService _service;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FSharpDocumentHighlightsService(IFSharpDocumentHighlightsService service)
        {
            _service = service;
        }

        private static ImmutableArray<HighlightSpan> MapHighlightSpans(ImmutableArray<FSharpHighlightSpan> highlightSpans)
        {
            return highlightSpans.SelectAsArray(x => new HighlightSpan(x.TextSpan, FSharpHighlightSpanKindHelpers.ConvertTo(x.Kind)));
        }

        public async Task<ImmutableArray<DocumentHighlights>> GetDocumentHighlightsAsync(Document document, int position, IImmutableSet<Document> documentsToSearch, HighlightingOptions options, CancellationToken cancellationToken)
        {
            var highlights = await _service.GetDocumentHighlightsAsync(document, position, documentsToSearch, cancellationToken).ConfigureAwait(false);
            return highlights.SelectAsArray(x => new DocumentHighlights(x.Document, MapHighlightSpans(x.HighlightSpans)));
        }
    }
}
