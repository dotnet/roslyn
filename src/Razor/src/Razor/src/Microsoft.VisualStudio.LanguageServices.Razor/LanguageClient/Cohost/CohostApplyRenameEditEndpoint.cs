// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.VisualStudio.Razor.ProjectSystem;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
// NOTE: This has to use RazorMethod, not CohostEndpoint, because it has to use the "default" language,
// since it has no document associated with it to get any other language.
[RazorMethod(RazorLSPConstants.ApplyRenameEditName)]
[ExportRazorStatelessLspService(typeof(CohostApplyRenameEditEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostApplyRenameEditEndpoint(ILoggerFactory loggerFactory)
    : AbstractRazorCohostRequestHandler<ApplyRenameEditParams, VoidResult>
{
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<CohostApplyRenameEditEndpoint>();
    private readonly IFileSystem _fileSystem = new FileSystem();

    protected override bool MutatesSolutionState => true;

    protected override bool RequiresLSPSolution => false;

    protected override async Task<VoidResult> HandleRequestAsync(ApplyRenameEditParams request, RazorCohostRequestContext context, CancellationToken cancellationToken)
    {
        // We're being called from VS, which means CPS has already renamed the razor file on disk. It might also have
        // renamed the .razor.css etc. files, which we will also have suggested to rename, but unfortunately whether it
        // does that depends on the users file nesting settings, it might have also renamed additional files, for which there could be edits that refer
        // to the old name. We go through the workspace edit and fix up any names, and drop any unnecessary renames, to
        // make everything work.
        // We don't need to worry about this in VS Code, becuase the workspace edit returned from willRenameFiles is applied
        // before the rename happens. If VS ever gets proper support for willRename then this endpoint can be removed entirely.

        FixUpWorkspaceEdit(request, _fileSystem);

        var razorCohostClientLanguageServerManager = context.GetRequiredService<IRazorClientLanguageServerManager>();
        var response = await razorCohostClientLanguageServerManager.SendRequestAsync<ApplyWorkspaceEditParams, ApplyWorkspaceEditResponse>(
               Methods.WorkspaceApplyEditName,
               new ApplyWorkspaceEditParams() { Edit = request.Edit },
               cancellationToken).ConfigureAwait(false);

        if (!response.Applied)
        {
            _logger.LogWarning($"Failed to apply workspace edit for rename operation: {response.FailureReason}");
        }

        return new();
    }

    private static void FixUpWorkspaceEdit(ApplyRenameEditParams request, IFileSystem fileSystem)
    {
        var documentChanges = new PooledArrayBuilder<SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>>();

        var oldFileNamePart = Path.GetFileName(request.OldFilePath);
        var newFileNamePart = Path.GetFileName(request.NewFilePath);

        foreach (var edit in request.Edit.EnumerateEdits())
        {
            if (edit.TryGetFirst(out var textDocumentEdit) &&
                textDocumentEdit.TextDocument.DocumentUri is { UriString: { } uriString } documentUri &&
                documentUri.GetRequiredParsedUri().GetDocumentFilePath() is { } documentFilePath &&
                !fileSystem.FileExists(documentFilePath))
            {
                var extension = PathUtilities.GetExtension(uriString);
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(uriString);
                var fileNamePartLength = fileNameWithoutExtension.Length + extension.Length;

                Debug.Assert(uriString.Length >= fileNamePartLength);

                string newFileName;
                if (documentFilePath == request.OldFilePath)
                {
                    // An edit to the actual Razor file that was renamed
                    newFileName = uriString[..^fileNamePartLength] + newFileNamePart;
                }
                else if (fileNameWithoutExtension == oldFileNamePart)
                {
                    // An edit to a code behind file, or similar, that got renamed as part of the operation
                    newFileName = uriString[..^fileNamePartLength] + newFileNamePart + extension;
                }
                else
                {
                    // This is an edit for a file that doesn't exist, but isn't related to Razor in any way. All we
                    // can do is drop it and hope that the user can sort it out manually (or it was irrelevant).
                    Debug.Fail("Got an edit that we don't understand during a rename operation.");
                    continue;
                }

                textDocumentEdit.TextDocument.DocumentUri = new DocumentUri(newFileName);
                documentChanges.Add(edit);
            }
            else if (edit.TryGetThird(out var renameEdit))
            {
                if (fileSystem.FileExists(renameEdit.OldDocumentUri.GetRequiredParsedUri().GetDocumentFilePath()))
                {
                    documentChanges.Add(edit);
                }
            }
            else
            {
                documentChanges.Add(edit);
            }
        }

        request.Edit.DocumentChanges = documentChanges.ToArrayAndClear();
    }

    internal static class TestAccessor
    {
        public static void FixUpWorkspaceEdit(ApplyRenameEditParams request, IFileSystem fileSystem)
            => CohostApplyRenameEditEndpoint.FixUpWorkspaceEdit(request, fileSystem);
    }
}
