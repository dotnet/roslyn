// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;
using Microsoft.CodeAnalysis.Razor.AutoInsert;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Protocol.AutoInsert;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Response = Microsoft.CodeAnalysis.Razor.Remote.RemoteResponse<Microsoft.CodeAnalysis.Razor.Protocol.AutoInsert.RemoteAutoInsertTextEdit?>;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class RemoteAutoInsertService(in ServiceArgs args)
    : RazorDocumentServiceBase(in args), IRemoteAutoInsertService
{
    internal sealed class Factory : FactoryBase<IRemoteAutoInsertService>
    {
        protected override IRemoteAutoInsertService CreateService(in ServiceArgs args)
            => new RemoteAutoInsertService(in args);
    }

    private readonly IAutoInsertService _autoInsertService = args.ExportProvider.GetExportedValue<IAutoInsertService>();
    private readonly IRazorFormattingService _razorFormattingService = args.ExportProvider.GetExportedValue<IRazorFormattingService>();
    private readonly IClientSettingsManager _clientSettingsManager = args.ExportProvider.GetExportedValue<IClientSettingsManager>();

    protected override IDocumentPositionInfoStrategy DocumentPositionInfoStrategy => PreferHtmlInAttributeValuesDocumentPositionInfoStrategy.Instance;

    public ValueTask<Response> GetAutoInsertTextEditAsync(
        RazorPinnedSolutionInfoWrapper solutionInfo,
        DocumentId documentId,
        LinePosition linePosition,
        string character,
        RazorFormattingOptions options,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            documentId,
            context => TryResolveInsertionAsync(
                context,
                linePosition,
                character,
                options,
                cancellationToken),
            cancellationToken);

    private async ValueTask<Response> TryResolveInsertionAsync(
        RemoteDocumentContext remoteDocumentContext,
        LinePosition linePosition,
        string character,
        RazorFormattingOptions options,
        CancellationToken cancellationToken)
    {
        var sourceText = await remoteDocumentContext.GetSourceTextAsync(cancellationToken).ConfigureAwait(false);
        if (!sourceText.TryGetAbsoluteIndex(linePosition, out var index))
        {
            return Response.NoFurtherHandling;
        }

        var codeDocument = await remoteDocumentContext.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);

        var clientSettings = _clientSettingsManager.GetClientSettings();

        // Always try our own service first, regardless of language
        // E.g. if ">" is typed for html tag, it's actually our auto-insert provider
        // that adds closing tag instead of HTML even though we are in HTML
        if (_autoInsertService.TryResolveInsertion(
                codeDocument,
                linePosition.ToPosition(),
                character,
                clientSettings.AdvancedSettings.AutoClosingTags,
                out var insertTextEdit))
        {
            return Response.Results(RemoteAutoInsertTextEdit.FromLspInsertTextEdit(insertTextEdit));
        }

        var positionInfo = GetPositionInfo(codeDocument, index);
        var languageKind = positionInfo.LanguageKind;

        switch (languageKind)
        {
            case RazorLanguageKind.Razor:
                // If we are in Razor and got no edit from our own service, there is nothing else to do
                return Response.NoFurtherHandling;
            case RazorLanguageKind.Html:
                return AutoInsertService.HtmlAllowedAutoInsertTriggerCharacters.Contains(character)
                    ? Response.CallHtml
                    : Response.NoFurtherHandling;
            case RazorLanguageKind.CSharp:
                var mappedPosition = positionInfo.Position.ToLinePosition();
                return await TryResolveInsertionInCSharpAsync(
                        remoteDocumentContext,
                        mappedPosition,
                        character,
                        options,
                        cancellationToken)
                    .ConfigureAwait(false);
            default:
                Logger.LogError($"Unsupported language {languageKind} in {nameof(RemoteAutoInsertService)}");
                return Response.NoFurtherHandling;
        }
    }

    private async ValueTask<Response> TryResolveInsertionInCSharpAsync(
        RemoteDocumentContext remoteDocumentContext,
        LinePosition mappedPosition,
        string character,
        RazorFormattingOptions options,
        CancellationToken cancellationToken)
    {
        // Special case for C# where we use AutoInsert for two purposes:
        // 1. For XML documentation comments (filling out the template when typing "///")
        // 2. For "on type formatting" style behavior, like adjusting indentation when pressing Enter inside empty braces
        //
        // If users have turned off on-type formatting, they don't want the behavior of number 2, but its impossible to separate
        // that out from number 1. Typing "///" could just as easily adjust indentation on some unrelated code higher up in the
        // file, which is exactly the behavior users complain about.
        //
        // Therefore we are just going to no-op if the user has turned off on type formatting. Maybe one day we can make this
        // smarter, but at least the user can always turn the setting back on, type their "///", and turn it back off, without
        // having to restart VS. Not the worst compromise (hopefully!)
        if (!_clientSettingsManager.GetClientSettings().AdvancedSettings.FormatOnType)
        {
            return Response.NoFurtherHandling;
        }

        if (!AutoInsertService.CSharpAllowedAutoInsertTriggerCharacters.Contains(character))
        {
            return Response.NoFurtherHandling;
        }

        var generatedDocument = await remoteDocumentContext.Snapshot
            .GetGeneratedDocumentAsync(cancellationToken)
            .ConfigureAwait(false);

        var autoInsertResponseItem = await OnAutoInsert.GetOnAutoInsertResponseAsync(
            generatedDocument,
            mappedPosition,
            character,
            options.ToLspFormattingOptions(),
            cancellationToken
        ).ConfigureAwait(false);

        if (autoInsertResponseItem is null)
        {
            return Response.NoFurtherHandling;
        }

        var csharpSourceText = await remoteDocumentContext.GetCSharpSourceTextAsync(cancellationToken).ConfigureAwait(false);
        var csharpTextChange = new TextChange(csharpSourceText.GetTextSpan(autoInsertResponseItem.TextEdit.Range), autoInsertResponseItem.TextEdit.NewText);
        var mappedChange = autoInsertResponseItem.TextEditFormat == InsertTextFormat.Snippet
            ? await _razorFormattingService.TryGetCSharpSnippetFormattingEditAsync(
                remoteDocumentContext,
                [csharpTextChange],
                options,
                cancellationToken)
            .ConfigureAwait(false)
            : await _razorFormattingService.TryGetSingleCSharpEditAsync(
                remoteDocumentContext,
                csharpTextChange,
                options,
                cancellationToken)
            .ConfigureAwait(false);

        if (mappedChange is not { NewText: not null } change)
        {
            return Response.NoFurtherHandling;
        }

        var sourceText = await remoteDocumentContext.GetSourceTextAsync(cancellationToken).ConfigureAwait(false);
        return Response.Results(
            new RemoteAutoInsertTextEdit(
                sourceText.GetLinePositionSpan(change.Span),
                change.NewText,
                autoInsertResponseItem.TextEditFormat));
    }
}
