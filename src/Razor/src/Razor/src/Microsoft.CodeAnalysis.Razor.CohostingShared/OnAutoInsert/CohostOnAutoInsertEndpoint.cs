// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Hosting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.Razor.AutoInsert;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Protocol.AutoInsert;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;
using Microsoft.VisualStudio.Razor.LanguageClient.Cohost;
using Response = Microsoft.CodeAnalysis.Razor.Remote.RemoteResponse<Microsoft.CodeAnalysis.Razor.Protocol.AutoInsert.RemoteAutoInsertTextEdit?>;

namespace Microsoft.VisualStudio.LanguageServices.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[CohostEndpoint(VSInternalMethods.OnAutoInsertName)]
[Export(typeof(IDynamicRegistrationProvider))]
[ExportRazorStatelessLspService(typeof(CohostOnAutoInsertEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostOnAutoInsertEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker,
    IClientSettingsManager clientSettingsManager,
#pragma warning disable RS0030 // Do not use banned APIs
    [ImportMany] IEnumerable<IOnAutoInsertTriggerCharacterProvider> onAutoInsertTriggerCharacterProviders,
#pragma warning restore RS0030 // Do not use banned APIs
    IHtmlRequestInvoker requestInvoker,
    ILoggerFactory loggerFactory)
    : AbstractCohostDocumentEndpoint<VSInternalDocumentOnAutoInsertParams, VSInternalDocumentOnAutoInsertResponseItem?>(incompatibleProjectService), IDynamicRegistrationProvider
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;
    private readonly IClientSettingsManager _clientSettingsManager = clientSettingsManager;
    private readonly IHtmlRequestInvoker _requestInvoker = requestInvoker;
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<CohostOnAutoInsertEndpoint>();

    private readonly ImmutableArray<string> _triggerCharacters = CalculateTriggerChars(onAutoInsertTriggerCharacterProviders);

    private static ImmutableArray<string> CalculateTriggerChars(IEnumerable<IOnAutoInsertTriggerCharacterProvider> onAutoInsertTriggerCharacterProviders)
    {
        var providerTriggerCharacters = onAutoInsertTriggerCharacterProviders.Select((provider) => provider.TriggerCharacter).Distinct();

        ImmutableArray<string> triggerCharacters = [
            .. providerTriggerCharacters,
#if !VSCODE
            // VS Code's auto insert functionality is poly-filled by Roslyn. The Html server has no support for it.
            .. AutoInsertService.HtmlAllowedAutoInsertTriggerCharacters,
#endif
            .. AutoInsertService.CSharpAllowedAutoInsertTriggerCharacters ];

        return triggerCharacters;
    }

    protected override bool MutatesSolutionState => false;

    protected override bool RequiresLSPSolution => true;

    public ImmutableArray<Registration> GetRegistrations(VSInternalClientCapabilities clientCapabilities, RazorCohostRequestContext requestContext)
    {
        if ((clientCapabilities.TextDocument as VSInternalTextDocumentClientCapabilities)?.OnAutoInsert?.DynamicRegistration == true)
        {
            return [new Registration
            {
                Method = VSInternalMethods.OnAutoInsertName,
                RegisterOptions = new VSInternalDocumentOnAutoInsertRegistrationOptions()
                    .EnableOnAutoInsert(_triggerCharacters)
            }];
        }

        return [];
    }

    protected override RazorTextDocumentIdentifier? GetRazorTextDocumentIdentifier(VSInternalDocumentOnAutoInsertParams request)
        => request.TextDocument.ToRazorTextDocumentIdentifier();

    protected override async Task<VSInternalDocumentOnAutoInsertResponseItem?> HandleRequestAsync(VSInternalDocumentOnAutoInsertParams request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        _logger.LogDebug($"Resolving auto-insertion for {razorDocument.FilePath}");

        var clientSettings = _clientSettingsManager.GetClientSettings();
        var razorFormattingOptions = RazorFormattingOptions.From(request.Options, codeBlockBraceOnNextLine: clientSettings.AdvancedSettings.CodeBlockBraceOnNextLine, attributeIndentStyle: clientSettings.AdvancedSettings.AttributeIndentStyle);

        _logger.LogDebug($"Calling OOP to resolve insertion at {request.Position} invoked by typing '{request.Character}'");
        var data = await _remoteServiceInvoker.TryInvokeAsync<IRemoteAutoInsertService, Response>(
            razorDocument.Project.Solution,
            (service, solutionInfo, cancellationToken)
                => service.GetAutoInsertTextEditAsync(
                        solutionInfo,
                        razorDocument.Id,
                        request.Position.ToLinePosition(),
                        request.Character,
                        razorFormattingOptions,
                        cancellationToken),
            cancellationToken).ConfigureAwait(false);

        if (data.Result is { } remoteInsertTextEdit)
        {
            _logger.LogDebug($"Got insert text edit from OOP {remoteInsertTextEdit}");
            return RemoteAutoInsertTextEdit.ToLspInsertTextEdit(remoteInsertTextEdit);
        }

        if (data.StopHandling)
        {
            return null;
        }

        // Got no data but no signal to stop handling

        return await TryResolveHtmlInsertionAsync(
            razorDocument,
            request,
            clientSettings.AdvancedSettings.AutoInsertAttributeQuotes,
            cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<VSInternalDocumentOnAutoInsertResponseItem?> TryResolveHtmlInsertionAsync(
        TextDocument razorDocument,
        VSInternalDocumentOnAutoInsertParams request,
        bool autoInsertAttributeQuotes,
        CancellationToken cancellationToken)
    {
        if (!autoInsertAttributeQuotes && request.Character == "=")
        {
            // Use Razor setting for auto insert attribute quotes. HTML Server doesn't have a way to pass that
            // information along so instead we just don't delegate the request.
            _logger.LogTrace($"Not delegating to HTML completion because AutoInsertAttributeQuotes is disabled");
            return null;
        }

        var result = await _requestInvoker.MakeHtmlLspRequestAsync<VSInternalDocumentOnAutoInsertParams, VSInternalDocumentOnAutoInsertResponseItem>(
            razorDocument,
            VSInternalMethods.OnAutoInsertName,
            request,
            cancellationToken).ConfigureAwait(false);

        if (result is null)
        {
            _logger.LogDebug($"Didn't get insert edit back from Html.");
            return null;
        }

        return result;
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostOnAutoInsertEndpoint instance)
    {
        public Task<VSInternalDocumentOnAutoInsertResponseItem?> HandleRequestAsync(
            VSInternalDocumentOnAutoInsertParams request,
            TextDocument razorDocument,
            CancellationToken cancellationToken)
                => instance.HandleRequestAsync(request, razorDocument, cancellationToken);
    }
}
