// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.LanguageServices.Xaml.Features.Structure;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer.Handler;

[ExportStatelessXamlLspService(typeof(FoldingRangesHandler)), Shared]
[Method(Methods.TextDocumentFoldingRangeName)]
internal sealed class FoldingRangesHandler : ILspServiceRequestHandler<FoldingRangeParams, FoldingRange[]>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public FoldingRangesHandler()
    {
    }

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(FoldingRangeParams request) => request.TextDocument;

    public async Task<FoldingRange[]> HandleRequestAsync(FoldingRangeParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var foldingRanges = ArrayBuilder<FoldingRange>.GetInstance();

        var document = context.Document;
        if (document == null)
        {
            return foldingRanges.ToArrayAndFree();
        }

        var xamlStructureService = document.Project.Services.GetService<IXamlStructureService>();
        if (xamlStructureService == null)
        {
            return foldingRanges.ToArrayAndFree();
        }

        var structureTags = await xamlStructureService.GetStructureTagsAsync(document, cancellationToken).ConfigureAwait(false);
        if (structureTags == null)
        {
            return foldingRanges.ToArrayAndFree();
        }

        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

        foreach (var structureTag in structureTags)
        {
            var linePositionSpan = text.Lines.GetLinePositionSpan(structureTag.TextSpan);

            var foldingRangeKind = structureTag.Type switch
            {
                XamlStructureTypes.Comment => (FoldingRangeKind?)FoldingRangeKind.Comment,
                XamlStructureTypes.Namespaces => FoldingRangeKind.Imports,
                XamlStructureTypes.Region => FoldingRangeKind.Region,
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
