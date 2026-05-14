// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[CohostEndpoint(Methods.TextDocumentRangeFormattingName)]
[Export(typeof(IDynamicRegistrationProvider))]
[ExportRazorStatelessLspService(typeof(CohostRangeFormattingEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostRangeFormattingEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker,
    IHtmlRequestInvoker requestInvoker,
    IClientSettingsManager clientSettingsManager,
    ILoggerFactory loggerFactory)
    : AbstractCohostDocumentEndpoint<DocumentRangeFormattingParams, TextEdit[]?>(incompatibleProjectService), IDynamicRegistrationProvider
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;
    private readonly IHtmlRequestInvoker _requestInvoker = requestInvoker;
    private readonly IClientSettingsManager _clientSettingsManager = clientSettingsManager;
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<CohostRangeFormattingEndpoint>();

    protected override bool MutatesSolutionState => false;

    protected override bool RequiresLSPSolution => true;

    public ImmutableArray<Registration> GetRegistrations(VSInternalClientCapabilities clientCapabilities, RazorCohostRequestContext requestContext)
    {
        if (clientCapabilities.TextDocument?.Formatting?.DynamicRegistration is true)
        {
            return [new Registration()
            {
                Method = Methods.TextDocumentRangeFormattingName,
                RegisterOptions = new DocumentRangeFormattingRegistrationOptions()
            }];
        }

        return [];
    }

    protected override RazorTextDocumentIdentifier? GetRazorTextDocumentIdentifier(DocumentRangeFormattingParams request)
        => request.TextDocument.ToRazorTextDocumentIdentifier();

    protected override async Task<TextEdit[]?> HandleRequestAsync(DocumentRangeFormattingParams request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var fromPaste = false;
        if (request.Options.OtherOptions is not null && request.Options.OtherOptions.TryGetValue("fromPaste", out var fromPasteObj) && fromPasteObj.TryGetFirst(out fromPaste))
        {
            if (fromPaste && !_clientSettingsManager.GetClientSettings().AdvancedSettings.FormatOnPaste)
            {
                return null;
            }
        }

        _logger.LogDebug($"Getting Html formatting changes for {razorDocument.FilePath}");
        var htmlResult = await TryGetHtmlFormattingEditsAsync(request, razorDocument, cancellationToken).ConfigureAwait(false);

        if (htmlResult is not { } htmlEdits)
        {
            // We prefer to return null, so the client will try again
            _logger.LogDebug($"Didn't get any edits back from Html");
            return null;
        }

        var sourceText = await razorDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var htmlChanges = htmlEdits.SelectAsArray(sourceText.GetTextChange);

        var csharpSyntaxFormattingOptions = RazorCSharpFormattingInteractionService.GetRazorCSharpSyntaxFormattingOptions(razorDocument.Project.Solution.Services);
        var options = RazorFormattingOptions.From(
            request.Options,
            _clientSettingsManager.GetClientSettings().AdvancedSettings.CodeBlockBraceOnNextLine,
            _clientSettingsManager.GetClientSettings().AdvancedSettings.AttributeIndentStyle,
            csharpSyntaxFormattingOptions,
            fromPaste);

        _logger.LogDebug($"Calling OOP with the {htmlChanges.Length} html edits, so it can fill in the rest");
        var remoteResult = await _remoteServiceInvoker.TryInvokeAsync<IRemoteFormattingService, ImmutableArray<TextChange>>(
            razorDocument.Project.Solution,
            (service, solutionInfo, cancellationToken) => service.GetRangeFormattingEditsAsync(solutionInfo, razorDocument.Id, htmlChanges, request.Range.ToLinePositionSpan(), options, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        if (remoteResult.IsDefaultOrEmpty)
        {
            return null;
        }

        _logger.LogDebug($"Got a total of {remoteResult.Length} ranges back from OOP");

        return remoteResult.SelectAsPlainArray(sourceText.GetTextEdit);
    }

    private async Task<TextEdit[]?> TryGetHtmlFormattingEditsAsync(DocumentRangeFormattingParams request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        // We don't actually request range formatting results from Html, because our formatting engine can't deal with
        // relative formatting results. Instead we request full document formatting, and filter the edits inside the
        // formatting service to only the ones we care about.
        var formattingRequest = new DocumentFormattingParams
        {
            TextDocument = request.TextDocument,
            Options = request.Options
        };

        var result = await _requestInvoker.MakeHtmlLspRequestAsync<DocumentFormattingParams, TextEdit[]>(
            razorDocument,
            Methods.TextDocumentFormattingName,
            formattingRequest,
            cancellationToken).ConfigureAwait(false);

        if (result is null)
        {
            _logger.LogDebug($"Didn't get any ranges back from Html. Returning null so we can abandon the whole thing");
            return null;
        }

        return result;
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostRangeFormattingEndpoint instance)
    {
        public Task<TextEdit[]?> HandleRequestAsync(DocumentRangeFormattingParams request, TextDocument razorDocument, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(request, razorDocument, cancellationToken);
    }
}
