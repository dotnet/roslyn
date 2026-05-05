// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.Composition;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.LanguageServer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

namespace Microsoft.VisualStudioCode.RazorExtension.Services;

[Export(typeof(IHtmlRequestInvoker))]
[method: ImportingConstructor]
internal sealed class HtmlRequestInvoker(
    RazorClientServerManagerProvider razorClientServerManagerProvider,
    IHtmlDocumentSynchronizer htmlDocumentSynchronizer,
    ILoggerFactory loggerFactory) : IHtmlRequestInvoker
{
    private readonly RazorClientServerManagerProvider _razorClientServerManagerProvider = razorClientServerManagerProvider;
    private readonly IHtmlDocumentSynchronizer _htmlDocumentSynchronizer = htmlDocumentSynchronizer;
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<HtmlRequestInvoker>();

    public async Task<TResponse?> MakeHtmlLspRequestAsync<TRequest, TResponse>(TextDocument razorDocument, string method, TRequest request, TimeSpan threshold, Guid correlationId, CancellationToken cancellationToken) where TRequest : notnull
    {
        var syncResult = await _htmlDocumentSynchronizer.TrySynchronizeAsync(razorDocument, cancellationToken).ConfigureAwait(false);
        if (!syncResult.Synchronized)
        {
            return default;
        }

        var checksumString = syncResult.Checksum.ToString();
        _logger.LogDebug($"Making Html request for {method} on {razorDocument.FilePath}, checksum {checksumString}");

        var forwardedRequest = new HtmlForwardedRequest<TRequest>(
            new TextDocumentIdentifier
            {
                DocumentUri = razorDocument.CreateDocumentUri()
            },
            checksumString,
            request);

        var clientConnection = _razorClientServerManagerProvider.ClientLanguageServerManager.AssumeNotNull();
        return await clientConnection.SendRequestAsync<HtmlForwardedRequest<TRequest>, TResponse>(method, forwardedRequest, cancellationToken).ConfigureAwait(false);
    }

    private record HtmlForwardedRequest<TRequest>(
        [property: JsonPropertyName("textDocument")]
        TextDocumentIdentifier TextDocument,
        [property: JsonPropertyName("checksum")]
        string Checksum,
        [property: JsonPropertyName("request")]
        TRequest Request);
}
