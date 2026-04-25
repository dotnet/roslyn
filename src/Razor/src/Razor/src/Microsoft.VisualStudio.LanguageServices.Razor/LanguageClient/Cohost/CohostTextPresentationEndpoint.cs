// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[CohostEndpoint(VSInternalMethods.TextDocumentTextPresentationName)]
[Export(typeof(IDynamicRegistrationProvider))]
[ExportCohostStatelessLspService(typeof(CohostTextPresentationEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostTextPresentationEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IFilePathService filePathService,
    IHtmlRequestInvoker requestInvoker)
    : AbstractCohostDocumentEndpoint<VSInternalTextPresentationParams, WorkspaceEdit?>(incompatibleProjectService), IDynamicRegistrationProvider
{
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
                Method = VSInternalMethods.TextDocumentTextPresentationName,
                RegisterOptions = new TextDocumentRegistrationOptions()
            }];
        }

        return [];
    }

    protected override RazorTextDocumentIdentifier? GetRazorTextDocumentIdentifier(VSInternalTextPresentationParams request)
        => request.TextDocument.ToRazorTextDocumentIdentifier();

    protected override async Task<WorkspaceEdit?> HandleRequestAsync(VSInternalTextPresentationParams request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var workspaceEdit = await _requestInvoker.MakeHtmlLspRequestAsync<VSInternalTextPresentationParams, WorkspaceEdit>(
            razorDocument,
            VSInternalMethods.TextDocumentTextPresentationName,
            request,
            cancellationToken).ConfigureAwait(false);

        if (workspaceEdit is null)
        {
            return null;
        }

        // NOTE: We iterate over just the TextDocumentEdit objects and modify them in place.
        // We intentionally do NOT create a new WorkspaceEdit here to avoid losing any
        // CreateFile, RenameFile, or DeleteFile operations that may be in DocumentChanges.
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

    internal readonly struct TestAccessor(CohostTextPresentationEndpoint instance)
    {
        public Task<WorkspaceEdit?> HandleRequestAsync(VSInternalTextPresentationParams request, TextDocument razorDocument, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(request, razorDocument, cancellationToken);
    }
}
