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
[CohostEndpoint(Methods.TextDocumentFormattingName)]
[Export(typeof(IDynamicRegistrationProvider))]
[ExportRazorStatelessLspService(typeof(CohostDocumentFormattingEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostDocumentFormattingEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker,
    IHtmlRequestInvoker requestInvoker,
    IClientSettingsManager clientSettingsManager,
    ILoggerFactory loggerFactory)
    : AbstractCohostDocumentEndpoint<DocumentFormattingParams, TextEdit[]?>(incompatibleProjectService), IDynamicRegistrationProvider
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;
    private readonly IHtmlRequestInvoker _requestInvoker = requestInvoker;
    private readonly IClientSettingsManager _clientSettingsManager = clientSettingsManager;
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<CohostDocumentFormattingEndpoint>();

    protected override bool MutatesSolutionState => false;

    protected override bool RequiresLSPSolution => true;

    public ImmutableArray<Registration> GetRegistrations(VSInternalClientCapabilities clientCapabilities, RazorCohostRequestContext requestContext)
    {
        if (clientCapabilities.TextDocument?.Formatting?.DynamicRegistration is true)
        {
            return [new Registration()
            {
                Method = Methods.TextDocumentFormattingName,
                RegisterOptions = new DocumentFormattingRegistrationOptions()
            }];
        }

        return [];
    }

    protected override RazorTextDocumentIdentifier? GetRazorTextDocumentIdentifier(DocumentFormattingParams request)
        => request.TextDocument.ToRazorTextDocumentIdentifier();

    protected override Task<TextEdit[]?> HandleRequestAsync(DocumentFormattingParams request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var csharpSyntaxFormattingOptions = RazorCSharpFormattingInteractionService.GetRazorCSharpSyntaxFormattingOptions(razorDocument.Project.Solution.Services);
        return HandleRequestAsync(request, razorDocument, csharpSyntaxFormattingOptions, cancellationToken);
    }

    private async Task<TextEdit[]?> HandleRequestAsync(DocumentFormattingParams request, TextDocument razorDocument, RazorCSharpSyntaxFormattingOptions csharpSyntaxFormattingOptions, CancellationToken cancellationToken)
    {
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

        var options = RazorFormattingOptions.From(
            request.Options,
            _clientSettingsManager.GetClientSettings().AdvancedSettings.CodeBlockBraceOnNextLine,
            _clientSettingsManager.GetClientSettings().AdvancedSettings.AttributeIndentStyle,
            csharpSyntaxFormattingOptions);

        _logger.LogDebug($"Calling OOP with the {htmlChanges.Length} html edits, so it can fill in the rest");
        var remoteResult = await _remoteServiceInvoker.TryInvokeAsync<IRemoteFormattingService, ImmutableArray<TextChange>>(
            razorDocument.Project.Solution,
            (service, solutionInfo, cancellationToken) => service.GetDocumentFormattingEditsAsync(solutionInfo, razorDocument.Id, htmlChanges, options, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        if (remoteResult.IsDefaultOrEmpty)
        {
            return null;
        }

        _logger.LogDebug($"Got a total of {remoteResult.Length} ranges back from OOP");

        return remoteResult.SelectAsPlainArray(sourceText.GetTextEdit);
    }

    private async Task<TextEdit[]?> TryGetHtmlFormattingEditsAsync(DocumentFormattingParams request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var result = await _requestInvoker.MakeHtmlLspRequestAsync<DocumentFormattingParams, TextEdit[]>(
            razorDocument,
            Methods.TextDocumentFormattingName,
            request,
            cancellationToken).ConfigureAwait(false);

        if (result is null)
        {
            _logger.LogDebug($"Didn't get any ranges back from Html. Returning null so we can abandon the whole thing");
            return null;
        }

        return result;
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostDocumentFormattingEndpoint instance)
    {
        public Task<TextEdit[]?> HandleRequestAsync(DocumentFormattingParams request, TextDocument razorDocument, RazorCSharpSyntaxFormattingOptions csharpSyntaxFormattingOptions, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(request, razorDocument, csharpSyntaxFormattingOptions, cancellationToken);
    }
}
