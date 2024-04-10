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

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
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

            var options = _globalOptions.GetBlockStructureOptions(document.Project) with
            {
                // Need to set the block structure guide options to true since the concept does not exist in vscode
                // but we still want to categorize them as the correct BlockType.
                ShowBlockStructureGuidesForCommentsAndPreprocessorRegions = true,
                ShowBlockStructureGuidesForDeclarationLevelConstructs = true,
                ShowBlockStructureGuidesForCodeLevelConstructs = true
            };

            return await GetFoldingRangesAsync(document, options, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Used here and by lsif generator.
        /// </summary>
        public static async Task<FoldingRange[]> GetFoldingRangesAsync(
            Document document,
            BlockStructureOptions options,
            CancellationToken cancellationToken)
        {
            var blockStructureService = document.GetRequiredLanguageService<BlockStructureService>();
            var blockStructure = await blockStructureService.GetBlockStructureAsync(document, options, cancellationToken).ConfigureAwait(false);
            if (blockStructure == null)
                return [];

            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            return GetFoldingRanges(blockStructure, text);
        }

        private static FoldingRange[] GetFoldingRanges(BlockStructure blockStructure, SourceText text)
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

                // TODO - Figure out which blocks should be returned as a folding range (and what kind).
                // https://github.com/dotnet/roslyn/projects/45#card-20049168
                FoldingRangeKind? foldingRangeKind = span.Type switch
                {
                    BlockTypes.Comment => FoldingRangeKind.Comment,
                    BlockTypes.Imports => FoldingRangeKind.Imports,
                    BlockTypes.PreprocessorRegion => FoldingRangeKind.Region,
                    _ => null,
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
            }

            return foldingRanges.ToArray();
        }
    }
}
