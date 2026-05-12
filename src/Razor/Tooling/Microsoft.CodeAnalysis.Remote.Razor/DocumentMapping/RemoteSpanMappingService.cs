// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.DocumentExcerpt;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed partial class RemoteSpanMappingService(in ServiceArgs args) : RazorBrokeredServiceBase(in args), IRemoteSpanMappingService
{
    internal sealed class Factory : FactoryBase<IRemoteSpanMappingService>
    {
        protected override IRemoteSpanMappingService CreateService(in ServiceArgs args)
            => new RemoteSpanMappingService(in args);
    }

    private readonly RemoteSnapshotManager _snapshotManager = args.ExportProvider.GetExportedValue<RemoteSnapshotManager>();
    private readonly ITelemetryReporter _telemetryReporter = args.ExportProvider.GetExportedValue<ITelemetryReporter>();
    private readonly IRazorEditService _razorEditService = args.ExportProvider.GetExportedValue<IRazorEditService>();

    public ValueTask<RemoteExcerptResult?> TryExcerptAsync(RazorPinnedSolutionInfoWrapper solutionInfo, DocumentId generatedDocumentId, TextSpan span, RazorExcerptMode mode, RazorClassificationOptionsWrapper options, CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            solution => TryExcerptAsync(solution, generatedDocumentId, span, mode, options, cancellationToken),
            cancellationToken);

    private async ValueTask<RemoteExcerptResult?> TryExcerptAsync(Solution solution, DocumentId generatedDocumentId, TextSpan span, RazorExcerptMode mode, RazorClassificationOptionsWrapper options, CancellationToken cancellationToken)
    {
        var generatedDocument = await solution.GetSourceGeneratedDocumentAsync(generatedDocumentId, cancellationToken).ConfigureAwait(false);
        if (generatedDocument is null)
        {
            return null;
        }

        var razorDocument = await TryGetRazorDocumentForGeneratedDocumentAsync(generatedDocument, cancellationToken).ConfigureAwait(false);
        if (razorDocument is null)
        {
            return null;
        }

        var documentSnapshot = _snapshotManager.GetSnapshot(razorDocument);
        var codeDocument = await documentSnapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

        var mappedSpans = await MapSpansAsync(documentSnapshot, codeDocument, [span], cancellationToken).ConfigureAwait(false);
        if (mappedSpans is not [{ IsDefault: false } mappedSpan])
        {
            return null;
        }

        var razorDocumentText = await razorDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var razorDocumentSpan = razorDocumentText.GetTextSpan(mappedSpan.LinePositionSpan);

        // First compute the range of text we want to we to display relative to the primary document.
        var excerptSpan = DocumentExcerptHelper.ChooseExcerptSpan(razorDocumentText, razorDocumentSpan, mode);

        // Then we'll classify the spans based on the primary document, since that's the coordinate
        // space that our output mappings use.
        var mappingsSortedByOriginal = codeDocument.GetRequiredCSharpDocument().SourceMappingsSortedByOriginal;
        var classifiedSpans = await DocumentExcerptHelper.ClassifyPreviewAsync(
            excerptSpan,
            generatedDocument,
            mappingsSortedByOriginal,
            options,
            cancellationToken).ConfigureAwait(false);

        return new RemoteExcerptResult(razorDocument.Id, razorDocumentSpan, excerptSpan, classifiedSpans.ToImmutable(), span);
    }

    public ValueTask<ImmutableArray<RazorMappedSpanResult>> MapSpansAsync(RazorPinnedSolutionInfoWrapper solutionInfo, DocumentId generatedDocumentId, ImmutableArray<TextSpan> spans, CancellationToken cancellationToken)
       => RunServiceAsync(
            solutionInfo,
            solution => MapSpansAsync(solution, generatedDocumentId, spans, cancellationToken),
            cancellationToken);

    private async ValueTask<ImmutableArray<RazorMappedSpanResult>> MapSpansAsync(Solution solution, DocumentId generatedDocumentId, ImmutableArray<TextSpan> spans, CancellationToken cancellationToken)
    {
        var razorDocument = await TryGetRazorDocumentForGeneratedDocumentIdAsync(generatedDocumentId, solution, cancellationToken).ConfigureAwait(false);
        if (razorDocument is null)
        {
            return [];
        }

        var documentSnapshot = _snapshotManager.GetSnapshot(razorDocument);
        var codeDocument = await documentSnapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

        return await MapSpansAsync(documentSnapshot, codeDocument, spans, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ImmutableArray<RazorMappedSpanResult>> MapSpansAsync(RemoteDocumentSnapshot documentSnapshot, RazorCodeDocument codeDocument, ImmutableArray<TextSpan> spans, CancellationToken cancellationToken)
    {
        var csharpSyntaxTree = await documentSnapshot.GetCSharpSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var csharpSyntaxNode = await csharpSyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

        var source = codeDocument.Source.Text;

        var csharpDocument = codeDocument.GetRequiredCSharpDocument();
        var filePath = codeDocument.Source.FilePath.AssumeNotNull();

        var classDeclSpan = csharpSyntaxNode.TryGetClassDeclaration(out var classDecl)
            ? classDecl.Identifier.Span
            : default;

        using var results = new PooledArrayBuilder<RazorMappedSpanResult>();

        foreach (var span in spans)
        {
            // If Roslyn is trying to navigate to, or show a reference to the class declaration, then we remap it to
            // (0,0) in the Razor document.
            if (span.Start == classDeclSpan.Start &&
                (span.Length == 0 ||
                span.Length == classDeclSpan.Length))
            {
                results.Add(new(filePath, new(LinePosition.Zero, LinePosition.Zero), new TextSpan()));
            }
            else if (TryGetMappedSpan(span, source, csharpDocument, out var linePositionSpan, out var mappedSpan))
            {
                results.Add(new(filePath, linePositionSpan, mappedSpan));
            }
            else
            {
                results.Add(default);
            }
        }

        return results.ToImmutableAndClear();
    }

    private static bool TryGetMappedSpan(TextSpan span, SourceText source, RazorCSharpDocument csharpDocument, out LinePositionSpan linePositionSpan, out TextSpan mappedSpan)
    {
        foreach (var mapping in csharpDocument.SourceMappingsSortedByGenerated)
        {
            var generated = mapping.GeneratedSpan.AsTextSpan();

            if (!generated.Contains(span))
            {
                if (generated.Start > span.End)
                {
                    // This span (and all following) are after the area we're interested in
                    break;
                }

                // If the search span isn't contained within the generated span, it is not a match.
                // A C# identifier won't cover multiple generated spans.
                continue;
            }

            var leftOffset = span.Start - generated.Start;
            var rightOffset = span.End - generated.End;
            if (leftOffset >= 0 && rightOffset <= 0)
            {
                // This span mapping contains the span.
                var original = mapping.OriginalSpan.AsTextSpan();
                mappedSpan = new TextSpan(original.Start + leftOffset, (original.End + rightOffset) - (original.Start + leftOffset));
                linePositionSpan = source.GetLinePositionSpan(mappedSpan);
                return true;
            }
        }

        mappedSpan = default;
        linePositionSpan = default;
        return false;
    }

    public ValueTask<ImmutableArray<RazorMappedEditResult>> MapTextChangesAsync(RazorPinnedSolutionInfoWrapper solutionInfo, DocumentId generatedDocumentId, ImmutableArray<TextChange> changes, CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            solution => MapTextChangesAsync(solution, generatedDocumentId, changes, cancellationToken),
            cancellationToken);

    private async ValueTask<ImmutableArray<RazorMappedEditResult>> MapTextChangesAsync(Solution solution, DocumentId generatedDocumentId, ImmutableArray<TextChange> changes, CancellationToken cancellationToken)
    {
        try
        {
            var razorDocument = await TryGetRazorDocumentForGeneratedDocumentIdAsync(generatedDocumentId, solution, cancellationToken).ConfigureAwait(false);
            if (razorDocument is null)
            {
                return [];
            }

            var documentSnapshot = _snapshotManager.GetSnapshot(razorDocument);
            var textChanges = await _razorEditService.MapCSharpEditsAsync(
                changes,
                documentSnapshot,
                cancellationToken).ConfigureAwait(false);

            if (textChanges.IsDefaultOrEmpty)
            {
                return [];
            }

            var razorSource = await razorDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

            return [new RazorMappedEditResult() { FilePath = documentSnapshot.FilePath, TextChanges = [.. textChanges] }];
        }
        catch (Exception ex)
        {
            _telemetryReporter.ReportFault(ex, "Failed to map edits");
            return [];
        }
    }

    private async Task<TextDocument?> TryGetRazorDocumentForGeneratedDocumentIdAsync(DocumentId generatedDocumentId, Solution solution, CancellationToken cancellationToken)
    {
        var generatedDocument = await solution.GetSourceGeneratedDocumentAsync(generatedDocumentId, cancellationToken).ConfigureAwait(false);
        if (generatedDocument is null)
        {
            return null;
        }

        return await TryGetRazorDocumentForGeneratedDocumentAsync(generatedDocument, cancellationToken).ConfigureAwait(false);
    }

    private async Task<TextDocument?> TryGetRazorDocumentForGeneratedDocumentAsync(SourceGeneratedDocument generatedDocument, CancellationToken cancellationToken)
    {
        var identity = RazorGeneratedDocumentIdentity.Create(generatedDocument);
        if (!identity.IsRazorSourceGeneratedDocument())
        {
            return null;
        }

        var projectSnapshot = _snapshotManager.GetSnapshot(generatedDocument.Project);

        return await projectSnapshot.TryGetRazorDocumentForGeneratedDocumentAsync(identity, cancellationToken).ConfigureAwait(false);
    }
}
