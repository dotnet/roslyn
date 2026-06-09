// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class RemoteInlineCompletionService(in ServiceArgs args) : RazorDocumentServiceBase(in args), IRemoteInlineCompletionService
{
    internal sealed class Factory : FactoryBase<IRemoteInlineCompletionService>
    {
        protected override IRemoteInlineCompletionService CreateService(in ServiceArgs args)
            => new RemoteInlineCompletionService(in args);
    }

    private readonly IDocumentMappingService _documentMappingService = args.ExportProvider.GetExportedValue<IDocumentMappingService>();

    public ValueTask<InlineCompletionRequestInfo?> GetInlineCompletionInfoAsync(RazorSolutionWrapper solutionInfo, DocumentId documentId, LinePosition linePosition, CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            documentId,
            context => GetInlineCompletionInfoAsync(context, linePosition, cancellationToken),
            cancellationToken);

    public async ValueTask<InlineCompletionRequestInfo?> GetInlineCompletionInfoAsync(RemoteDocumentContext context, LinePosition linePosition, CancellationToken cancellationToken)
    {
        var codeDocument = await context.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);

        if (!codeDocument.Source.Text.TryGetAbsoluteIndex(linePosition, out var hostDocumentPosition))
        {
            return null;
        }

        // Keep track of which generated C# document the request maps into so the client asks Roslyn for completions in
        // that document, and the formatting step maps Roslyn's result back through the same document's source mappings.
        if (!_documentMappingService.TryMapToCSharpDocumentLinePosition(codeDocument, hostDocumentPosition, out var mappedPosition, out _, out var inDeclDocument))
        {
            return null;
        }

        var generatedDocument = await context.Snapshot.GetGeneratedDocumentAsync(inDeclDocument, cancellationToken).ConfigureAwait(false);
        return new InlineCompletionRequestInfo(
            GeneratedDocumentUri: generatedDocument.CreateSystemUri(),
            Position: mappedPosition,
            InDeclDocument: inDeclDocument);
    }

    public ValueTask<FormattedInlineCompletionInfo?> FormatInlineCompletionAsync(RazorSolutionWrapper solutionInfo, DocumentId documentId, bool inDeclDocument, RazorFormattingOptions options, LinePositionSpan span, string text, CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            documentId,
            context => FormatInlineCompletionAsync(context, inDeclDocument, options, span, text, cancellationToken),
            cancellationToken);

    private async ValueTask<FormattedInlineCompletionInfo?> FormatInlineCompletionAsync(RemoteDocumentContext context, bool inDeclDocument, RazorFormattingOptions options, LinePositionSpan span, string text, CancellationToken cancellationToken)
    {
        var codeDocument = await context.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);
        var csharpDocument = codeDocument.GetRequiredCSharpDocument(inDeclDocument);

        if (!_documentMappingService.TryMapToRazorDocumentRange(csharpDocument, span, out var razorRange))
        {
            return null;
        }

        var hostDocumentIndex = codeDocument.Source.Text.GetRequiredAbsoluteIndex(razorRange.End);

        var formattingContext = FormattingContext.Create(context.Snapshot, codeDocument, options, logger: null);
        if (!SnippetFormatter.TryGetSnippetWithAdjustedIndentation(formattingContext, text, hostDocumentIndex, out var newSnippetText))
        {
            return null;
        }

        return new FormattedInlineCompletionInfo(razorRange, newSnippetText);
    }
}
