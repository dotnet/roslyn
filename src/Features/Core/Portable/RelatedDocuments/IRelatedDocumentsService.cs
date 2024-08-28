﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.RelatedDocuments;

internal interface IRelatedDocumentsService : ILanguageService
{
    /// <summary>
    /// Given an document, and an optional position in that document, streams a unique list of documents Ids that the
    /// language think are "related".  It is up to the language to define what "related" means.  However, common
    /// examples might be checking to see which symbols are used at that particular location and prioritizing documents
    /// those symbols are defined in.
    /// </summary>
    ValueTask GetRelatedDocumentIdsAsync(
        Document document, int position, Func<ImmutableArray<DocumentId>, CancellationToken, ValueTask> callbackAsync, CancellationToken cancellationToken);
}
