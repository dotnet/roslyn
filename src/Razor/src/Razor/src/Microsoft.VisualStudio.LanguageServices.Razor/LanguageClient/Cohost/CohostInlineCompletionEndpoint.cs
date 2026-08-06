// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.CohostingShared;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.InlineCompletions;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[CohostEndpoint(VSInternalMethods.TextDocumentInlineCompletionName)]
[Export(typeof(IDynamicRegistrationProvider))]
[ExportRazorStatelessLspService(typeof(CohostInlineCompletionEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostInlineCompletionEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker,
    IClientSettingsManager clientSettingsManager)
    : AbstractCohostDocumentEndpoint<VSInternalInlineCompletionRequest, VSInternalInlineCompletionList?>(incompatibleProjectService), IDynamicRegistrationProvider
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;
    private readonly IClientSettingsManager _clientSettingsManager = clientSettingsManager;

    protected override bool MutatesSolutionState => false;

    protected override bool RequiresLSPSolution => true;

    public ImmutableArray<Registration> GetRegistrations(VSInternalClientCapabilities clientCapabilities, RequestContext requestContext)
    {
        if (clientCapabilities.TextDocument?.CodeAction?.DynamicRegistration == true)
        {
            return [new Registration
            {
                Method = VSInternalMethods.TextDocumentInlineCompletionName,
                RegisterOptions = new VSInternalInlineCompletionRegistrationOptions()
                {
                    Pattern = new Regex(string.Join("|",
                        GetBuiltInCSharpSnippetTriggers()))
                }
            }];
        }

        return [];
    }

    internal static string[] GetBuiltInCSharpSnippetTriggers()
        => ["~", "Attribute", "checked", "class", "ctor", "cw", "do", "else", "enum", "equals", "Exception", "for", "foreach", "forr",
            "if", "indexer", "interface", "invoke", "iterator", "iterindex", "lock", "mbox", "namespace", "#if", "#region", "prop",
            "propfull", "propg", "sim", "struct", "svm", "switch", "try", "tryf", "unchecked", "unsafe", "using", "while"];

    protected override TextDocumentIdentifier? GetRazorTextDocumentIdentifier(VSInternalInlineCompletionRequest request)
        => request.TextDocument;

    protected override Task<VSInternalInlineCompletionList?> HandleRequestAsync(VSInternalInlineCompletionRequest request, TextDocument razorDocument, CancellationToken cancellationToken)
        => Assumed.Unreachable<Task<VSInternalInlineCompletionList?>>("This method has to exist because its base is abstract, but it should never be called.");

    protected override Task<VSInternalInlineCompletionList?> HandleRequestAsync(VSInternalInlineCompletionRequest request, RequestContext context, TextDocument razorDocument, CancellationToken cancellationToken)
        => HandleRequestAsync(context, razorDocument, request.Position.ToLinePosition(), request.Options, cancellationToken);

    private async Task<VSInternalInlineCompletionList?> HandleRequestAsync(RequestContext? context, TextDocument razorDocument, LinePosition linePosition, FormattingOptions formattingOptions, CancellationToken cancellationToken)
    {
        var requestInfo = await _remoteServiceInvoker.TryInvokeAsync<IRemoteInlineCompletionService, InlineCompletionRequestInfo?>(
            razorDocument.Project.Solution,
            (service, solutionInfo, cancellationToken) => service.GetInlineCompletionInfoAsync(solutionInfo, razorDocument.Id, linePosition, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        if (requestInfo is not InlineCompletionRequestInfo(var generatedDocumentUri, var position))
        {
            return null;
        }

        var generatedDocument = await razorDocument.Project.Solution.TryGetSourceGeneratedDocumentAsync(generatedDocumentUri, cancellationToken).ConfigureAwait(false);
        if (generatedDocument is null)
        {
            return null;
        }

        var result = await GetInlineCompletionItemsAsync(context, generatedDocument, position, formattingOptions, cancellationToken).ConfigureAwait(false);
        if (result is null)
        {
            return null;
        }

        if (result.Range is not null)
        {
            var options = RazorFormattingOptions.From(formattingOptions, _clientSettingsManager.GetClientSettings().AdvancedSettings.CodeBlockBraceOnNextLine, _clientSettingsManager.GetClientSettings().AdvancedSettings.AttributeIndentStyle);
            var span = result.Range.ToLinePositionSpan();
            var formattedInfo = await _remoteServiceInvoker.TryInvokeAsync<IRemoteInlineCompletionService, FormattedInlineCompletionInfo?>(
                razorDocument.Project.Solution,
                (service, solutionInfo, cancellationToken) => service.FormatInlineCompletionAsync(solutionInfo, razorDocument.Id, options, span, result.Text, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            if (formattedInfo is { } formatted)
            {
                result.Range = formatted.Span.ToRange();
                result.Text = formatted.FormattedText;
            }
            else
            {
                return null;
            }
        }

        return new VSInternalInlineCompletionList { Items = [result] };
    }

    private static Task<VSInternalInlineCompletionItem?> GetInlineCompletionItemsAsync(
        RequestContext? context,
        Document document,
        LinePosition position,
        FormattingOptions options,
        CancellationToken cancellationToken)
    {
        // Razor tests don't construct a RequestContext, so we need to handle the null case.
        var logger = context?.Logger ?? NoOpLspLogger.Instance;
        var xmlSnippetParser = document.Project.Solution.Services.ExportProvider.GetService<XmlSnippetParser>();

        return InlineCompletionsHandler.GetInlineCompletionItemsAsync(logger, document, position, options, xmlSnippetParser, cancellationToken);
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostInlineCompletionEndpoint instance)
    {
        public Task<VSInternalInlineCompletionList?> HandleRequestAsync(TextDocument razorDocument, LinePosition position, FormattingOptions formattingOptions, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(context: null, razorDocument, position, formattingOptions, cancellationToken);
    }
}
