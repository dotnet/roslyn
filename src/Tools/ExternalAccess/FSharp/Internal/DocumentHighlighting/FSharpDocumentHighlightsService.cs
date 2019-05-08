// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.DocumentHighlighting
{
    [Shared]
    [ExportLanguageService(typeof(IDocumentHighlightsService), LanguageNames.FSharp)]
    internal class FSharpDocumentHighlightsService : IDocumentHighlightsService
    {
        public Task<ImmutableArray<DocumentHighlights>> GetDocumentHighlightsAsync(Document document, int position, IImmutableSet<Document> documentsToSearch, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
