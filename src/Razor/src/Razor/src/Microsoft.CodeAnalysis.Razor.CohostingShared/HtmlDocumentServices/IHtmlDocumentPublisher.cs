// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

internal interface IHtmlDocumentPublisher
{
    Task<bool> TryPublishAsync(TextDocument document, ChecksumWrapper checksum, string htmlText, CancellationToken cancellationToken);
}
