// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Razor.Workspaces.Extensions;
using Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.Razor.Remote.RemoteResponse<
    Roslyn.LanguageServer.Protocol.SumType<Roslyn.LanguageServer.Protocol.VSInternalReferenceItem, Roslyn.LanguageServer.Protocol.Location>[]?>;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class RemoteFindAllReferencesService(in ServiceArgs args) : RazorDocumentServiceBase(in args), IRemoteFindAllReferencesService
{
    internal sealed class Factory : FactoryBase<IRemoteFindAllReferencesService>
    {
        protected override IRemoteFindAllReferencesService CreateService(in ServiceArgs args)
            => new RemoteFindAllReferencesService(in args);
    }

    private readonly IClientCapabilitiesService _clientCapabilitiesService = args.ExportProvider.GetExportedValue<IClientCapabilitiesService>();
    private readonly IWorkspaceProvider _workspaceProvider = args.WorkspaceProvider;

    protected override IDocumentPositionInfoStrategy DocumentPositionInfoStrategy => PreferAttributeNameDocumentPositionInfoStrategy.Instance;

    public ValueTask<RemoteResponse<SumType<VSInternalReferenceItem, LspLocation>[]?>> FindAllReferencesAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId documentId,
        Position position,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            documentId,
            snapshot => FindAllReferencesAsync(snapshot, position, cancellationToken),
            cancellationToken);

    private async ValueTask<RemoteResponse<SumType<VSInternalReferenceItem, LspLocation>[]?>> FindAllReferencesAsync(
        RemoteDocumentSnapshot snapshot,
        Position position,
        CancellationToken cancellationToken)
    {
        var codeDocument = await snapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

        if (!codeDocument.Source.Text.TryGetAbsoluteIndex(position, out var hostDocumentIndex))
        {
            return NoFurtherHandling;
        }

        // Adjust position if on a component end tag to use the start tag position
        hostDocumentIndex = codeDocument.AdjustPositionForComponentEndTag(hostDocumentIndex);

        var positionInfo = GetPositionInfo(codeDocument, hostDocumentIndex, preferCSharpOverHtml: true);

        if (positionInfo.LanguageKind is not RazorLanguageKind.CSharp)
        {
            return NoFurtherHandling;
        }

        // Finally, call into C#.
        var generatedDocument = await snapshot
            .GetGeneratedDocumentAsync(positionInfo.InDeclDocument, cancellationToken)
            .ConfigureAwait(false);
        var globalOptions = generatedDocument.Project.Solution.Services.ExportProvider.GetService<IGlobalOptionService>();
        var metadataAsSourceFileService = generatedDocument.Project.Solution.Services.ExportProvider.GetService<IMetadataAsSourceFileService>();
        var progress = BufferedProgress.Create<SumType<VSInternalReferenceItem, LspLocation>[]>(progress: null);

        await FindAllReferencesHandler.FindReferencesAsync(
            progress,
            _workspaceProvider.GetWorkspace(),
            generatedDocument,
            positionInfo.Position.ToLinePosition(),
            _clientCapabilitiesService.ClientCapabilities.SupportsVisualStudioExtensions,
            includeDeclaration: true,
            globalOptions,
            metadataAsSourceFileService,
            AsynchronousOperationListenerProvider.NullListener,
            cancellationToken).ConfigureAwait(false);

        var results = progress.GetFlattenedValues();

        if (results is null and not [])
        {
            // C# didn't return anything, so we're done.
            return NoFurtherHandling;
        }

        using var mappedResults = new PooledArrayBuilder<SumType<VSInternalReferenceItem, LspLocation>>(results.Length);

        // Map the C# locations back to the Razor file.
        foreach (var result in results)
        {
            var location = result.TryGetFirst(out var referenceItem)
                ? referenceItem.Location
                : result.Second;

            if (location is null)
            {
                continue;
            }

            var generatedDocumentUri = location.DocumentUri;
            var (mappedUri, mappedRange) = await DocumentMappingService
                .MapToHostDocumentUriAndRangeAsync(
                    snapshot,
                    generatedDocumentUri,
                    location.Range.ToLinePositionSpan(),
                    cancellationToken)
                .ConfigureAwait(false);

            if (mappedUri.IsRazorCSharpDocumentUri(snapshot.TextDocument.Project.Solution))
            {
                // Couldn't map, so probably a hidden part of the code-gen, let's skip it.
                continue;
            }

            if (referenceItem is not null)
            {
                // Indicates the reference item is directly available in the code
                referenceItem.Origin = VSInternalItemOrigin.Exact;

                // If we're going to change the Uri, then also override the file paths
                if (mappedUri != location.DocumentUri)
                {
                    var path = mappedUri.GetDocumentFilePathFromUri();
                    referenceItem.DisplayPath = path;
                    referenceItem.DocumentName = path;

                    var fixedResultText = await GetResultTextAsync(
                        snapshot,
                        generatedDocumentUri,
                        mappedRange.Start.Line,
                        path,
                        cancellationToken)
                        .ConfigureAwait(false);
                    referenceItem.Text = fixedResultText ?? referenceItem.Text;
                }
            }

            location.DocumentUri = mappedUri;
            location.Range = mappedRange.ToRange();

            mappedResults.Add(result);
        }

        return Results(mappedResults.ToArrayAndClear());
    }

    private async Task<string?> GetResultTextAsync(
        RemoteDocumentSnapshot snapshot,
        DocumentUri generatedDocumentUri,
        int lineNumber,
        string filePath,
        CancellationToken cancellationToken)
    {
        // Roslyn will have sent us back text that comes from the .g.cs file, but that is often not helpful. For example give:
        //
        // <SurveyPrompt Title="Blah" />
        //
        // A FAR for the Title property will return just the word "Title" in the Text of the reference item, which does not
        // help the user reason about the result. For such cases, its better to return the text from the Razor file, even
        // though it will be unclassified, as it will help the user.
        //
        // However, for cases where the result comes from a C# block, for example:
        //
        // @code {
        //    public string Title { get; set; }
        // }
        //
        // A FAR for the Title property here will return the full line of code, classified by Roslyn, so we don't want to
        // do anything for those.
        //
        // To identify which situation we're in, we check whether both ends of the line fall within the same source
        // mapping. If they do, the entire line is within a single C# block and the C# text is appropriate. If either
        // end is outside any mapping, or the two ends belong to different mappings (which happens for lines that mix
        // Razor transitions with C# — e.g. "@if (condition)" where "@" is outside any mapping and "if (condition)"
        // is in its own mapping), we return the Razor source text instead.

        if (snapshot.ProjectSnapshot.SolutionSnapshot.GetProjectsContainingDocument(filePath).FirstOrDefault() is { } project &&
            project.TryGetDocument(filePath, out var document))
        {
            var codeDoc = await document.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);
            var line = codeDoc.Source.Text.Lines[lineNumber];
            if (!snapshot.TextDocument.Project.Solution.TryGetSourceGeneratedDocumentIdentity(generatedDocumentUri, out var identity))
            {
                return null;
            }

            var csharpDocument = codeDoc.GetCSharpDocumentForHintName(identity.HintName);
            var startMapping = FindContainingSourceMapping(csharpDocument, line.Start);
            // Use line.End - 1 (last content character) rather than the exclusive line.End boundary
            // to avoid false positives when line.End lands exactly on a mapping edge.
            var endMapping = line.End > line.Start
                ? FindContainingSourceMapping(csharpDocument, line.End - 1)
                : startMapping;

            // Only skip overriding the text (i.e. only use C# text) if both ends of the line are
            // within the same source mapping, meaning the entire line is pure C# code.
            if (startMapping is not null && endMapping is not null && ReferenceEquals(startMapping, endMapping))
            {
                return null;
            }

            var start = line.GetFirstNonWhitespacePosition() ?? line.Start;
            return codeDoc.Source.Text.ToString(TextSpan.FromBounds(start, line.End));
        }

        return null;
    }

    /// <summary>
    /// Finds the <see cref="SourceMapping"/> in <paramref name="csharpDocument"/> that contains
    /// <paramref name="razorIndex"/>, or <see langword="null"/> if no mapping covers that position.
    /// Mirrors the containment logic used by <c>TryMapToCSharpDocumentPositionInternal</c>.
    /// </summary>
    private static SourceMapping? FindContainingSourceMapping(RazorCSharpDocument csharpDocument, int razorIndex)
    {
        foreach (var mapping in csharpDocument.SourceMappingsSortedByOriginal)
        {
            var originalSpan = mapping.OriginalSpan;
            var originalAbsoluteIndex = originalSpan.AbsoluteIndex;
            if (originalAbsoluteIndex <= razorIndex)
            {
                // Treat the mapping as owning the edge at its end (hence <=), mirroring
                // TryMapToCSharpDocumentPositionInternal's inclusive-end semantics.
                var distanceIntoOriginalSpan = razorIndex - originalAbsoluteIndex;
                if (distanceIntoOriginalSpan <= originalSpan.Length)
                {
                    return mapping;
                }
            }
            else
            {
                // Mappings are sorted by original position; once we've passed razorIndex we can stop.
                break;
            }
        }

        return null;
    }
}
