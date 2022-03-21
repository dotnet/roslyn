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
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportRoslynLanguagesLspRequestHandlerProvider(typeof(FoldingRangesHandler)), Shared]
    [Method(Methods.TextDocumentFoldingRangeName)]
    internal sealed class FoldingRangesHandler : AbstractStatelessRequestHandler<FoldingRangeParams, FoldingRange[]?>
    {
        private readonly IGlobalOptionService _globalOptions;

        public override bool MutatesSolutionState => false;
        public override bool RequiresLSPSolution => true;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FoldingRangesHandler(IGlobalOptionService globalOptions)
        {
            _globalOptions = globalOptions;
        }

        public override TextDocumentIdentifier? GetTextDocumentIdentifier(FoldingRangeParams request) => request.TextDocument;

        public override async Task<FoldingRange[]?> HandleRequestAsync(FoldingRangeParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            if (document == null)
                return null;

            var blockStructureService = document.Project.LanguageServices.GetService<BlockStructureService>();
            if (blockStructureService == null)
            {
                return Array.Empty<FoldingRange>();
            }

            var options = _globalOptions.GetBlockStructureOptions(document.Project);
            var blockStructure = await blockStructureService.GetBlockStructureAsync(document, options, cancellationToken).ConfigureAwait(false);
            if (blockStructure == null)
            {
                return Array.Empty<FoldingRange>();
            }

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return GetFoldingRanges(blockStructure, text);
        }

        public static FoldingRange[] GetFoldingRanges(
            SyntaxTree syntaxTree,
            HostLanguageServices languageServices,
            in BlockStructureOptions options,
            CancellationToken cancellationToken)
        {
            var blockStructureService = (BlockStructureServiceWithProviders)languageServices.GetRequiredService<BlockStructureService>();
            var blockStructure = blockStructureService.GetBlockStructure(syntaxTree, options, cancellationToken);
            if (blockStructure == null)
            {
                return Array.Empty<FoldingRange>();
            }

            var text = syntaxTree.GetText(cancellationToken);
            return GetFoldingRanges(blockStructure, text);
        }

        private static FoldingRange[] GetFoldingRanges(BlockStructure blockStructure, SourceText text)
        {
            if (blockStructure.Spans.IsEmpty)
            {
                return Array.Empty<FoldingRange>();
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
                    Kind = foldingRangeKind
                });
            }

            return foldingRanges.ToArray();
        }
    }
}
