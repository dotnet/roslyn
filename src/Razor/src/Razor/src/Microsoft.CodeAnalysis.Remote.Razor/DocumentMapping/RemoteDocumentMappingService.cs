// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;

[Export(typeof(IDocumentMappingService)), Shared]
[method: ImportingConstructor]
internal sealed class RemoteDocumentMappingService(
    IFilePathService filePathService,
    RemoteSnapshotManager snapshotManager,
    ILoggerFactory loggerFactory)
    : AbstractDocumentMappingService(loggerFactory.GetOrCreateLogger<RemoteDocumentMappingService>())
{
    private readonly IFilePathService _filePathService = filePathService;
    private readonly RemoteSnapshotManager _snapshotManager = snapshotManager;

    public async Task<(Uri MappedDocumentUri, LinePositionSpan MappedRange)> MapToHostDocumentUriAndRangeAsync(
        RemoteDocumentSnapshot originSnapshot,
        Uri generatedDocumentUri,
        LinePositionSpan generatedDocumentRange,
        CancellationToken cancellationToken)
    {
        // For Html we just map the Uri, the range will be the same
        if (_filePathService.IsVirtualHtmlFile(generatedDocumentUri))
        {
            return (_filePathService.GetRazorDocumentUri(generatedDocumentUri), generatedDocumentRange);
        }

        // We only map from C# files
        if (!_filePathService.IsVirtualCSharpFile(generatedDocumentUri))
        {
            return (generatedDocumentUri, generatedDocumentRange);
        }

        var solution = originSnapshot.TextDocument.Project.Solution;
        if (!solution.TryGetSourceGeneratedDocumentIdentity(generatedDocumentUri, out var identity) ||
            !solution.TryGetProject(identity.DocumentId.ProjectId, out var project))
        {
            return (generatedDocumentUri, generatedDocumentRange);
        }

        var razorCodeDocument = await _snapshotManager.GetSnapshot(project).TryGetCodeDocumentForGeneratedDocumentAsync(identity, cancellationToken).ConfigureAwait(false);
        if (razorCodeDocument is null)
        {
            return (generatedDocumentUri, generatedDocumentRange);
        }

        var razorDocumentUri = project.Solution.GetRazorDocumentUri(razorCodeDocument);
        if (TryMapToRazorDocumentRange(razorCodeDocument.GetRequiredCSharpDocument(), generatedDocumentRange, MappingBehavior.Strict, out var mappedRange))
        {
            return (razorDocumentUri, mappedRange);
        }

        // If the position is unmappable, but was in a generated Razor, we have one last check to see if Roslyn wants to navigate
        // to the class declaration, in which case we'll map to (0,0) in the Razor document itself.
        if (await TryGetCSharpClassDeclarationSpanAsync(identity, project, cancellationToken).ConfigureAwait(false) is { } classDeclSpan &&
            generatedDocumentRange.Start == classDeclSpan.Start &&
                (generatedDocumentRange.End == generatedDocumentRange.Start ||
                generatedDocumentRange.End == classDeclSpan.End))
        {
            return (razorDocumentUri, new(LinePosition.Zero, LinePosition.Zero));
        }

        return (generatedDocumentUri, generatedDocumentRange);
    }

    private static async Task<LinePositionSpan?> TryGetCSharpClassDeclarationSpanAsync(RazorGeneratedDocumentIdentity identity, Project project, CancellationToken cancellationToken)
    {
        var generatedDocument = await project.TryGetCSharpDocumentForGeneratedDocumentAsync(identity, cancellationToken).ConfigureAwait(false);
        if (generatedDocument is null)
        {
            return null;
        }

        var csharpSyntaxTree = await generatedDocument.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        if (csharpSyntaxTree is null)
        {
            return null;
        }

        var csharpSyntaxRoot = await csharpSyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
        if (!csharpSyntaxRoot.TryGetClassDeclaration(out var classDecl))
        {
            return null;
        }

        var sourceText = await generatedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var classDeclSpan = sourceText.GetLinePositionSpan(classDecl.Identifier.Span);

        return classDeclSpan;
    }
}
