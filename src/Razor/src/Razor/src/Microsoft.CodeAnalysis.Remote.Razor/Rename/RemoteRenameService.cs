// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Rename;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Microsoft.NET.Sdk.Razor.SourceGenerators;
using static Microsoft.CodeAnalysis.Razor.Remote.RemoteResponse<Roslyn.LanguageServer.Protocol.WorkspaceEdit?>;
using ExternalHandlers = Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class RemoteRenameService(in ServiceArgs args) : RazorDocumentServiceBase(in args), IRemoteRenameService
{
    internal sealed class Factory : FactoryBase<IRemoteRenameService>
    {
        protected override IRemoteRenameService CreateService(in ServiceArgs args)
            => new RemoteRenameService(in args);
    }

    private readonly IRenameService _renameService = args.ExportProvider.GetExportedValue<IRenameService>();
    private readonly IRazorEditService _razorEditService = args.ExportProvider.GetExportedValue<IRazorEditService>();

    public ValueTask<RemoteResponse<WorkspaceEdit?>> GetRenameEditAsync(
        JsonSerializableRazorPinnedSolutionInfoWrapper solutionInfo,
        JsonSerializableDocumentId documentId,
        Position position,
        string newName,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            documentId,
            context => GetRenameEditAsync(context, position, newName, cancellationToken),
            cancellationToken);

    private async ValueTask<RemoteResponse<WorkspaceEdit?>> GetRenameEditAsync(
        RemoteDocumentContext context,
        Position position,
        string newName,
        CancellationToken cancellationToken)
    {
        var codeDocument = await context.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);

        var hostDocumentIndex = codeDocument.Source.Text.GetRequiredAbsoluteIndex(position);
        hostDocumentIndex = codeDocument.AdjustPositionForComponentEndTag(hostDocumentIndex);

        var positionInfo = GetPositionInfo(codeDocument, hostDocumentIndex, preferCSharpOverHtml: true);

        var generatedDocument = await context.Snapshot
            .GetGeneratedDocumentAsync(cancellationToken)
            .ConfigureAwait(false);

        var razorEdit = await _renameService
            .TryGetRazorRenameEditsAsync(context, positionInfo, newName, context.GetSolutionQueryOperations(), cancellationToken)
            .ConfigureAwait(false);

        if (razorEdit.Edit is null && positionInfo.LanguageKind != CodeAnalysis.Razor.Protocol.RazorLanguageKind.CSharp)
        {
            return CallHtml;
        }

        if (razorEdit.Edit is null && !razorEdit.FallbackToCSharp)
        {
            return NoFurtherHandling;
        }

        var csharpEdit = await ExternalHandlers.Rename
            .GetRenameEditAsync(generatedDocument, positionInfo.Position.ToLinePosition(), newName, cancellationToken)
            .ConfigureAwait(false);

        if (csharpEdit is null)
        {
            return NoFurtherHandling;
        }

        await _razorEditService.MapWorkspaceEditAsync(context.Snapshot, csharpEdit, cancellationToken).ConfigureAwait(false);

        return Results(csharpEdit.Concat(razorEdit.Edit));
    }

    public ValueTask<WorkspaceEdit?> GetFileRenameEditAsync(
        JsonSerializableRazorPinnedSolutionInfoWrapper solutionInfo,
        RenameFilesParams fileRenameRequest,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            context => GetFileRenameEditAsync(context, fileRenameRequest, cancellationToken),
            cancellationToken);

    private async ValueTask<WorkspaceEdit?> GetFileRenameEditAsync(Solution solution, RenameFilesParams fileRenameRequest, CancellationToken cancellationToken)
    {
        var response = new WorkspaceEdit();

        foreach (var file in fileRenameRequest.Files)
        {
            var newFilePath = file.NewUri.GetAbsoluteOrUNCPath();

            // We know we won't get called unless the OldUri is a .razor document, but we are responsible to confirm that
            // the new Uri is still a .razor document.
            if (!FileUtilities.IsRazorComponentFilePath(newFilePath, PathUtilities.OSSpecificPathComparison))
            {
                continue;
            }

            var newFileName = Path.GetFileNameWithoutExtension(newFilePath);

            // We also need to make sure OldUri is a document we know about
            if (!solution.TryGetRazorDocument(file.OldUri.GetRequiredParsedUri(), out var oldDoc))
            {
                continue;
            }

            // Make sure that the filename itself is actually changing (ie, we don't care about file moves)
            if (newFileName == Path.GetFileNameWithoutExtension(oldDoc.FilePath))
            {
                continue;
            }

            Logger.LogDebug($"Rename for Razor document from {oldDoc.FilePath} to {newFileName}.");

            var documentContext = CreateRazorDocumentContext(solution, oldDoc.Id);
            if (documentContext is null)
            {
                continue;
            }

            var documentEdit = await GetEditsAsync(documentContext, newFileName, cancellationToken).ConfigureAwait(false);
            response = response.Concat(documentEdit);
        }

        if (response.DocumentChanges is null)
        {
            return null;
        }

        return response;
    }

    private async Task<WorkspaceEdit?> GetEditsAsync(RemoteDocumentContext context, string newFileName, CancellationToken cancellationToken)
    {
        if (!context.Snapshot.FileKind.IsComponent())
        {
            return null;
        }

        var generatedDocument = await context.Snapshot.GetGeneratedDocumentAsync(cancellationToken).ConfigureAwait(false);
        var text = await generatedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var tree = await generatedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var declaration = tree.AssumeNotNull().DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (declaration is null)
        {
            return null;
        }

        var position = text.GetLinePosition(declaration.Identifier.SpanStart);

        string newComponentName;
        using (var _ = StringBuilderPool.GetPooledObject(out var builder))
        {
            RazorSourceGenerator.BuildIdentifierFromPath(builder, newFileName);
            newComponentName = builder.ToString();
        }

        var csharpEdit = await ExternalHandlers.Rename
            .GetRenameEditAsync(generatedDocument, position, newComponentName, cancellationToken)
            .ConfigureAwait(false);

        if (csharpEdit is null)
        {
            return null;
        }

        await _razorEditService.MapWorkspaceEditAsync(context.Snapshot, csharpEdit, cancellationToken).ConfigureAwait(false);

        _renameService.TryGetRazorFileRenameEdit(context, newFileName, out var razorEdit);

        return csharpEdit.Concat(razorEdit);
    }
}
