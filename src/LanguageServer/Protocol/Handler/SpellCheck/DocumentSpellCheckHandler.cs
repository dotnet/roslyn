// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SpellCheck
{
    [Method(VSInternalMethods.TextDocumentSpellCheckableRangesName)]
    internal class DocumentSpellCheckHandler : AbstractSpellCheckHandler<VSInternalDocumentSpellCheckableParams, VSInternalSpellCheckableRangeReport>
    {
        public override TextDocumentIdentifier GetTextDocumentIdentifier(VSInternalDocumentSpellCheckableParams requestParams)
            => requestParams.TextDocument;

        protected override VSInternalSpellCheckableRangeReport CreateReport(TextDocumentIdentifier identifier, int[]? ranges, string? resultId)
            => new()
            {
                Ranges = ranges,
                ResultId = resultId,
            };

        protected override ImmutableArray<PreviousPullResult>? GetPreviousResults(VSInternalDocumentSpellCheckableParams requestParams)
        {
            if (requestParams.PreviousResultId != null && requestParams.TextDocument != null)
            {
                return ImmutableArray.Create(new PreviousPullResult(requestParams.PreviousResultId, requestParams.TextDocument));
            }

            // The client didn't provide us with a previous result to look for, so we can't lookup anything.
            return null;
        }

        protected override ImmutableArray<Document> GetOrderedDocuments(RequestContext context, CancellationToken cancellationToken)
            => GetRequestedDocument(context);

        internal static ImmutableArray<Document> GetRequestedDocument(RequestContext context)
        {
            // For the single document case, that is the only doc we want to process.
            //
            // Note: context.Document may be null in the case where the client is asking about a document that we have
            // since removed from the workspace.  In this case, we don't really have anything to process.
            // GetPreviousResults will be used to properly realize this and notify the client that the doc is gone.
            //
            // Only consider open documents here (and only closed ones in the WorkspaceSpellCheckingHandler).  Each
            // handler treats those as separate worlds that they are responsible for.
            if (context.Document == null)
            {
                context.TraceInformation("Ignoring spell check request because no document was provided");
                return [];
            }

            if (!context.IsTracking(context.Document.GetURI()))
            {
                context.TraceInformation($"Ignoring spell check request for untracked document: {context.Document.GetURI()}");
                return [];
            }

            return [context.Document];
        }
    }
}
