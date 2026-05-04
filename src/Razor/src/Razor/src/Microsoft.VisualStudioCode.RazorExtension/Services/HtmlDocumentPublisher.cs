// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.LanguageServer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

namespace Microsoft.VisualStudioCode.RazorExtension.Services;

[Export(typeof(IHtmlDocumentPublisher))]
[method: ImportingConstructor]
internal sealed class HtmlDocumentPublisher(
    RazorClientServerManagerProvider razorClientServerManagerProvider) : IHtmlDocumentPublisher
{
    private readonly RazorClientServerManagerProvider _razorClientServerManagerProvider = razorClientServerManagerProvider;

    public async Task<bool> TryPublishAsync(TextDocument document, ChecksumWrapper checksum, string htmlText, CancellationToken cancellationToken)
    {
        var request = new HtmlUpdateParameters(new TextDocumentIdentifier { DocumentUri = document.CreateDocumentUri() }, checksum.ToString(), htmlText);

        var clientConnection = _razorClientServerManagerProvider.ClientLanguageServerManager.AssumeNotNull();
        await clientConnection.SendRequestAsync("razor/updateHtml", request, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private record HtmlUpdateParameters(
        [property: JsonPropertyName("textDocument")]
        TextDocumentIdentifier TextDocument,
        [property: JsonPropertyName("checksum")]
        string Checksum,
        [property: JsonPropertyName("text")]
        string Text);
}
