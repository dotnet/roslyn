// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Copilot.RelatedDocuments;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.RelatedDocuments;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Internal.RelatedDocuments;

[ExportLanguageService(typeof(ICopilotRelatedDocumentsService), language: LanguageNames.CSharp), Shared]
internal sealed class CSharpCopilotRelatedDocumentsService : ICopilotRelatedDocumentsService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpCopilotRelatedDocumentsService()
    {
    }

    public ValueTask GetRelatedDocumentIdsAsync(Document document, int position, Func<ImmutableArray<DocumentId>, CancellationToken, ValueTask> callbackAsync, CancellationToken cancellationToken)
    {
        var service = document.GetRequiredLanguageService<IRelatedDocumentsService>();
        return service.GetRelatedDocumentIdsAsync(document, position, callbackAsync, cancellationToken);
    }
}
