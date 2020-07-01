﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(Methods.TextDocumentFoldingRangeName)]
    internal class FoldingRangesHandler : AbstractRequestHandler<FoldingRangeParams, FoldingRange[]>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FoldingRangesHandler(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }

        public override async Task<FoldingRange[]> HandleRequestAsync(FoldingRangeParams request, ClientCapabilities clientCapabilities,
            string? clientName, CancellationToken cancellationToken)
        {
            var foldingRanges = ArrayBuilder<FoldingRange>.GetInstance();

            var document = SolutionProvider.GetDocument(request.TextDocument, clientName);
            if (document == null)
            {
                return foldingRanges.ToArrayAndFree();
            }

            var blockStructureService = document.Project.LanguageServices.GetService<BlockStructureService>();
            if (blockStructureService == null)
            {
                return foldingRanges.ToArrayAndFree();
            }

            var blockStructure = await blockStructureService.GetBlockStructureAsync(document, cancellationToken).ConfigureAwait(false);
            if (blockStructure == null)
            {
                return foldingRanges.ToArrayAndFree();
            }

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

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

            return foldingRanges.ToArrayAndFree();
        }
    }
}
