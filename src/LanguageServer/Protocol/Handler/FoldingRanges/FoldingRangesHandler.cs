// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[ExportCSharpVisualBasicStatelessLspService(typeof(FoldingRangesHandler)), Shared]
[Method(Methods.TextDocumentFoldingRangeName)]
internal sealed class FoldingRangesHandler : ILspServiceDocumentRequestHandler<FoldingRangeParams, FoldingRange[]?>
{
    private readonly IGlobalOptionService _globalOptions;

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public FoldingRangesHandler(IGlobalOptionService globalOptions)
    {
        _globalOptions = globalOptions;
    }

    public TextDocumentIdentifier GetTextDocumentIdentifier(FoldingRangeParams request) => request.TextDocument;

    public async Task<FoldingRange[]?> HandleRequestAsync(FoldingRangeParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var document = context.Document;
        if (document is null)
            return null;

        var lineFoldingOnly = context.GetRequiredClientCapabilities().TextDocument?.FoldingRange?.LineFoldingOnly == true;
        return await SpecializedTasks.AsNullable(GetFoldingRangesAsync(_globalOptions, document, lineFoldingOnly, cancellationToken)).ConfigureAwait(false);
    }

    internal static Task<FoldingRange[]> GetFoldingRangesAsync(
        IGlobalOptionService globalOptions,
        Document document,
        bool lineFoldingOnly,
        CancellationToken cancellationToken)
    {
        var options = globalOptions.GetBlockStructureOptions(document.Project) with
        {
            // Need to set the block structure guide options to true since the concept does not exist in vscode
            // but we still want to categorize them as the correct BlockType.
            ShowBlockStructureGuidesForCommentsAndPreprocessorRegions = true,
            ShowBlockStructureGuidesForDeclarationLevelConstructs = true,
            ShowBlockStructureGuidesForCodeLevelConstructs = true
        };

        return GetFoldingRangesAsync(document, options, lineFoldingOnly, cancellationToken);
    }

    /// <summary>
    /// Used here and by lsif generator.
    /// </summary>
    public static async Task<FoldingRange[]> GetFoldingRangesAsync(
        Document document,
        BlockStructureOptions options,
        bool lineFoldingOnly,
        CancellationToken cancellationToken)
    {
        var blockStructureService = document.GetRequiredLanguageService<BlockStructureService>();
        var blockStructure = await blockStructureService.GetBlockStructureAsync(document, options, cancellationToken).ConfigureAwait(false);
        if (blockStructure == null)
            return [];

        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        return GetFoldingRanges(blockStructure, text, lineFoldingOnly);
    }

    private static FoldingRange[] GetFoldingRanges(BlockStructure blockStructure, SourceText text, bool lineFoldingOnly)
    {
        if (blockStructure.Spans.IsEmpty)
        {
            return [];
        }

        using var _ = ArrayBuilder<FoldingRange>.GetInstance(out var foldingRanges);

        foreach (var span in blockStructure.Spans)
        {
            if (!span.IsCollapsible)
            {
                continue;
            }

            var linePositionSpan = text.Lines.GetLinePositionSpan(span.TextSpan);

            // Filter out single line spans.
            if (linePositionSpan.Start.Line == linePositionSpan.End.Line)
            {
                continue;
            }

            FoldingRangeKind? foldingRangeKind = span.Type switch
            {
                BlockTypes.Comment => FoldingRangeKind.Comment,
                BlockTypes.Imports => FoldingRangeKind.Imports,
                BlockTypes.PreprocessorRegion => FoldingRangeKind.Region,
                BlockTypes.Member => VSFoldingRangeKind.Implementation,
                _ => span.AutoCollapse ? VSFoldingRangeKind.Implementation : null,
            };

            foldingRanges.Add(new FoldingRange()
            {
                StartLine = linePositionSpan.Start.Line,
                StartCharacter = linePositionSpan.Start.Character,
                EndLine = linePositionSpan.End.Line,
                EndCharacter = linePositionSpan.End.Character,
                Kind = foldingRangeKind,
                CollapsedText = span.BannerText
            });

            if (lineFoldingOnly)
            {
                foldingRanges = AdjustToEnsureNonOverlappingLines(foldingRanges);
            }
        }

        return foldingRanges.ToArray();

        static ArrayBuilder<FoldingRange> AdjustToEnsureNonOverlappingLines(ArrayBuilder<FoldingRange> foldingRanges)
        {
            using var _ = PooledDictionary<int, FoldingRange>.GetInstance(out var startLineToFoldingRange);

            // Spans are sorted in descending order by start position (the span starting closer to the end of the file is first).
            foreach (var foldingRange in foldingRanges)
            {
                var updatedRange = foldingRange;
                // Check if another span starts on the same line.
                if (startLineToFoldingRange.ContainsKey(foldingRange.StartLine))
                {
                    // There's already a span that starts on this line.  We want to keep the innermost span, which is the one
                    // we already have in the dictionary (as it started later in the file).  Skip this one.
                    continue;
                }

                var endLine = foldingRange.EndLine;

                // Check if this span ends on the same line another span starts.
                // Since we're iterating bottom up, if there is a span that starts on this end line, it will be in the dictionary.
                if (startLineToFoldingRange.ContainsKey(endLine))
                {
                    // The end line of this span overlaps with the start line of another span - attempt to adjust this one
                    // to the prior line.
                    var adjustedEndLine = endLine - 1;

                    // If the adjusted end line is now at or before the start line, there's no folding range possible without line overlapping another span.
                    if (adjustedEndLine <= foldingRange.StartLine)
                    {
                        continue;
                    }

                    updatedRange = new FoldingRange
                    {
                        StartLine = foldingRange.StartLine,
                        StartCharacter = foldingRange.StartCharacter,
                        EndLine = adjustedEndLine,
                        EndCharacter = foldingRange.EndCharacter,
                        Kind = foldingRange.Kind,
                        CollapsedText = foldingRange.CollapsedText
                    };
                }

                // These are explicitly ignored by the client when lineFoldingOnly is true, so no need to serialize them.
                updatedRange.StartCharacter = null;
                updatedRange.EndCharacter = null;

                startLineToFoldingRange[foldingRange.StartLine] = updatedRange;
            }

            return [.. startLineToFoldingRange.Values];
        }
    }
}
