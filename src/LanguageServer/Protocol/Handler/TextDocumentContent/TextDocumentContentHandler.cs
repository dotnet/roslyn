// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.TextDocumentContent;

[ExportCSharpVisualBasicStatelessLspService(typeof(TextDocumentContentHandler)), Shared]
[Method(Methods.WorkspaceTextDocumentContentName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class TextDocumentContentHandler() : ILspServiceDocumentRequestHandler<TextDocumentContentParams, TextDocumentContentResult>
{
    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(TextDocumentContentParams request) => new() { DocumentUri = request.Uri };

    public async Task<TextDocumentContentResult> HandleRequestAsync(TextDocumentContentParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var textDocument = await context.GetTextDocumentAsync(cancellationToken).ConfigureAwait(false);
        Contract.ThrowIfNull(textDocument, $"{request.Uri} was not found in any workspace, cannot provide content");

        var scheme = request.Uri.ParsedDocumentUri?.Scheme;
        if (scheme is not null)
        {
            // URI scheme names are case-insensitive, so providers must match regardless of the casing used in the request.
            var provider = context.GetRequiredServices<ITextDocumentContentProvider>().Single(p => p.Scheme.Equals(scheme, StringComparison.OrdinalIgnoreCase));

            var contentText = await provider.GetTextAsync(textDocument, cancellationToken).ConfigureAwait(false);
            return new TextDocumentContentResult { Text = contentText };
        }

        var text = await textDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
        return new TextDocumentContentResult
        {
            Text = text.ToString()
        };
    }
}
