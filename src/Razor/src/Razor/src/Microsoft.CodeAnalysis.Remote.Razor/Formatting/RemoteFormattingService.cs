// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Response = Microsoft.CodeAnalysis.Razor.Remote.IRemoteFormattingService.TriggerKind;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class RemoteFormattingService(in ServiceArgs args) : RazorDocumentServiceBase(in args), IRemoteFormattingService
{
    internal sealed class Factory : FactoryBase<IRemoteFormattingService>
    {
        protected override IRemoteFormattingService CreateService(in ServiceArgs args)
            => new RemoteFormattingService(in args);
    }

    private readonly IRazorFormattingService _formattingService = args.ExportProvider.GetExportedValue<IRazorFormattingService>();

    public ValueTask<ImmutableArray<TextChange>> GetDocumentFormattingEditsAsync(
        RazorPinnedSolutionInfoWrapper solutionInfo,
        DocumentId documentId,
        ImmutableArray<TextChange> htmlChanges,
        RazorFormattingOptions options,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            documentId,
            context => new ValueTask<ImmutableArray<TextChange>>(_formattingService.GetDocumentFormattingChangesAsync(context, htmlChanges, span: null, options, cancellationToken)),
            cancellationToken);

    public ValueTask<ImmutableArray<TextChange>> GetRangeFormattingEditsAsync(
        RazorPinnedSolutionInfoWrapper solutionInfo,
        DocumentId documentId,
        ImmutableArray<TextChange> htmlChanges,
        LinePositionSpan linePositionSpan,
        RazorFormattingOptions options,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            documentId,
            async context =>
            {
                try
                {
                    return await _formattingService.GetDocumentFormattingChangesAsync(context, htmlChanges, linePositionSpan, options, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception) when (options.FromPaste)
                {
                    // Swallow exceptions during paste, because it's likely the cause is simply invalid code that the user will fix.
                    return [];
                }
            },
            cancellationToken);

    public ValueTask<ImmutableArray<TextChange>> GetOnTypeFormattingEditsAsync(
        RazorPinnedSolutionInfoWrapper solutionInfo,
        DocumentId documentId,
        ImmutableArray<TextChange> htmlChanges,
        LinePosition linePosition,
        string character,
        RazorFormattingOptions options,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            documentId,
            context => GetOnTypeFormattingEditsAsync(context, htmlChanges, linePosition, character, options, cancellationToken),
            cancellationToken);

    private async ValueTask<ImmutableArray<TextChange>> GetOnTypeFormattingEditsAsync(RemoteDocumentContext context, ImmutableArray<TextChange> htmlChanges, LinePosition linePosition, string triggerCharacter, RazorFormattingOptions options, CancellationToken cancellationToken)
    {
        var codeDocument = await context.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);
        var sourceText = await context.GetSourceTextAsync(cancellationToken).ConfigureAwait(false);
        if (!sourceText.TryGetAbsoluteIndex(linePosition, out var hostDocumentIndex))
        {
            return [];
        }

        if (!_formattingService.TryGetOnTypeFormattingTriggerKind(codeDocument, hostDocumentIndex, triggerCharacter, out var triggerCharacterKind))
        {
            return [];
        }

        if (triggerCharacterKind is RazorLanguageKind.Html)
        {
            return await _formattingService.GetHtmlOnTypeFormattingChangesAsync(context, htmlChanges, options, hostDocumentIndex, triggerCharacter[0], cancellationToken).ConfigureAwait(false);
        }

        Debug.Assert(triggerCharacterKind is RazorLanguageKind.CSharp);
        return await _formattingService.GetCSharpOnTypeFormattingChangesAsync(context, options, hostDocumentIndex, triggerCharacter[0], cancellationToken).ConfigureAwait(false);
    }

    public ValueTask<Response> GetOnTypeFormattingTriggerKindAsync(
        RazorPinnedSolutionInfoWrapper solutionInfo,
        DocumentId documentId,
        LinePosition linePosition,
        string triggerCharacter,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            documentId,
            context => GetOnTypeFormattingTriggerKindAsync(context, linePosition, triggerCharacter, cancellationToken),
            cancellationToken);

    private async ValueTask<Response> GetOnTypeFormattingTriggerKindAsync(RemoteDocumentContext context, LinePosition linePosition, string triggerCharacter, CancellationToken cancellationToken)
    {
        var codeDocument = await context.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);
        var sourceText = codeDocument.Source.Text;
        if (!sourceText.TryGetAbsoluteIndex(linePosition, out var hostDocumentIndex))
        {
            return Response.Invalid;
        }

        if (!_formattingService.TryGetOnTypeFormattingTriggerKind(codeDocument, hostDocumentIndex, triggerCharacter, out var triggerCharacterKind))
        {
            return Response.Invalid;
        }

        if (triggerCharacterKind is RazorLanguageKind.Html)
        {
            return Response.ValidHtml;
        }

        // TryGetOnTypeFormattingTriggerKind only returns true for C# or Html
        Debug.Assert(triggerCharacterKind is RazorLanguageKind.CSharp);

        return Response.ValidCSharp;
    }
}
