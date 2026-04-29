// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[CohostEndpoint(VSInternalMethods.TextDocumentUriPresentationName)]
[Export(typeof(IDynamicRegistrationProvider))]
[ExportCohostStatelessLspService(typeof(CohostUriPresentationEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostUriPresentationEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker,
    IFilePathService filePathService,
    IHtmlRequestInvoker requestInvoker)
    : AbstractCohostDocumentEndpoint<VSInternalUriPresentationParams, WorkspaceEdit?>(incompatibleProjectService), IDynamicRegistrationProvider
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;
    private readonly IFilePathService _filePathService = filePathService;
    private readonly IHtmlRequestInvoker _requestInvoker = requestInvoker;

    protected override bool MutatesSolutionState => false;

    protected override bool RequiresLSPSolution => true;

    public ImmutableArray<Registration> GetRegistrations(VSInternalClientCapabilities clientCapabilities, RazorCohostRequestContext requestContext)
    {
        if (clientCapabilities.SupportsVisualStudioExtensions)
        {
            return [new Registration
            {
                Method = VSInternalMethods.TextDocumentUriPresentationName,
                RegisterOptions = new TextDocumentRegistrationOptions()
            }];
        }

        return [];
    }

    protected override RazorTextDocumentIdentifier? GetRazorTextDocumentIdentifier(VSInternalUriPresentationParams request)
        => request.TextDocument.ToRazorTextDocumentIdentifier();

    protected override async Task<WorkspaceEdit?> HandleRequestAsync(VSInternalUriPresentationParams request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var data = await _remoteServiceInvoker.TryInvokeAsync<IRemoteUriPresentationService, RemoteResponse<TextChange?>>(
            razorDocument.Project.Solution,
            (service, solutionInfo, cancellationToken) => service.GetPresentationAsync(solutionInfo, razorDocument.Id, request.Range.ToLinePositionSpan(), request.Uris, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        // If we got a response back, then we're good to go
        if (data.Result is { } textChange)
        {
            var sourceText = await razorDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

            return new WorkspaceEdit
            {
                DocumentChanges = new TextDocumentEdit[]
                {
                    new TextDocumentEdit
                    {
                        TextDocument = new()
                        {
                            DocumentUri = request.TextDocument.DocumentUri
                        },
                        Edits = [sourceText.GetTextEdit(textChange)]
                    }
                }
            };
        }

        // If we didn't get anything from our logic, we might need to go and ask Html, but we also might have determined not to
        if (data.StopHandling)
        {
            return null;
        }

        var workspaceEdit = await _requestInvoker.MakeHtmlLspRequestAsync<VSInternalUriPresentationParams, WorkspaceEdit>(
            razorDocument,
            VSInternalMethods.TextDocumentUriPresentationName,
            request,
            cancellationToken).ConfigureAwait(false);

        // TODO: We _really_ should go back to OOP to remap the response to razor, but the fact is, Razor and Html are 1:1 mappings, so we're
        //       just adjusting Uris, and we don't need OOP for that. If we ever do proper Html mapping then this will not be true.

        if (workspaceEdit is null)
        {
            return null;
        }

        // NOTE: We iterate over just the TextDocumentEdit objects and modify them in place.
        // We intentionally do NOT create a new WorkspaceEdit here to avoid losing any
        // CreateFile, RenameFile, or DeleteFile operations that may be in DocumentChanges.
        // TODO: We could have a helper service for this, because RazorDocumentMappingService used to do it, but we can't use that in devenv,
        //       but if we move this all to OOP, per the above TODO, then that point is moot.
        foreach (var edit in workspaceEdit.EnumerateTextDocumentEdits())
        {
            if (edit.TextDocument.DocumentUri.ParsedUri is { } uri &&
                _filePathService.IsVirtualHtmlFile(uri))
            {
                edit.TextDocument = new OptionalVersionedTextDocumentIdentifier { DocumentUri = new(_filePathService.GetRazorDocumentUri(uri)) };
            }
        }

        return workspaceEdit;
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostUriPresentationEndpoint instance)
    {
        public Task<WorkspaceEdit?> HandleRequestAsync(VSInternalUriPresentationParams request, TextDocument razorDocument, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(request, razorDocument, cancellationToken);
    }
}
