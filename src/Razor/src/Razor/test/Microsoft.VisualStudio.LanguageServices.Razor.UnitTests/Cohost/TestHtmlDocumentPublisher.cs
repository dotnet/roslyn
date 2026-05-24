// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

internal sealed class TestHtmlDocumentPublisher(bool publishResult = true) : IHtmlDocumentPublisher
{
    private readonly bool _publishResult = publishResult;

    public List<(TextDocument Document, string Text, ChecksumWrapper Checksum)> Publishes { get; } = [];

    public Task<bool> TryPublishAsync(TextDocument document, ChecksumWrapper checksum, string htmlText, CancellationToken cancellationToken)
    {
        Publishes.Add((document, htmlText, checksum));
        return Task.FromResult(_publishResult);
    }
}
