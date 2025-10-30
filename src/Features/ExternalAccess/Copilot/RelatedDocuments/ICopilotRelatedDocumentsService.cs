// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.RelatedDocuments;

internal interface ICopilotRelatedDocumentsService : ILanguageService
{
    ValueTask GetRelatedDocumentIdsAsync(
        Document document, int position, Func<ImmutableArray<DocumentId>, CancellationToken, ValueTask> callbackAsync, CancellationToken cancellationToken);
}
