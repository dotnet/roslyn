// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal static class FoldingRangesHelper
    {
        public static async Task<FoldingRange[]> GetFoldingRangesAsync(
            Document document,
            CancellationToken cancellationToken)
        {
            var blockStructureService = document.Project.LanguageServices.GetService<BlockStructureService>();
            if (blockStructureService == null)
            {
                return Array.Empty<FoldingRange>();
            }

            var blockStructure = await blockStructureService.GetBlockStructureAsync(document, cancellationToken).ConfigureAwait(false);
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
            OptionSet options,
            bool isMetadataAsSource,
            CancellationToken cancellationToken)
        {
            var blockStructureService = (BlockStructureServiceWithProviders)languageServices.GetRequiredService<BlockStructureService>();
            var blockStructure = blockStructureService.GetBlockStructure(syntaxTree, options, isMetadataAsSource, cancellationToken);
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

                // TODO - Figure out which blocks should be returned as a folding range (and what kind).
                // https://github.com/dotnet/roslyn/projects/45#card-20049168
                var foldingRangeKind = span.Type switch
                {
                    BlockTypes.Comment => (FoldingRangeKind?)FoldingRangeKind.Comment,
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
