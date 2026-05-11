// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

internal interface IHtmlDocumentSynchronizer
{
    void DocumentRemoved(Uri uri, CancellationToken cancellationToken);
    Task<SynchronizationResult> TrySynchronizeAsync(TextDocument document, CancellationToken cancellationToken);
}
