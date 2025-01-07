// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[ExportCSharpVisualBasicStatelessLspService(typeof(DataTipRangeHandler)), Shared]
[Method(VSInternalMethods.TextDocumentDataTipRangeName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DataTipRangeHandler()
    : ILspServiceDocumentRequestHandler<TextDocumentPositionParams, VSInternalDataTip?>
{
    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(TextDocumentPositionParams request)
        => request.TextDocument;

    public async Task<VSInternalDataTip?> HandleRequestAsync(TextDocumentPositionParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var document = context.GetRequiredDocument();

        var service = document.GetRequiredLanguageService<ILanguageDebugInfoService>();

        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var linePosition = ProtocolConversions.PositionToLinePosition(request.Position);
        var position = text.Lines.GetPosition(linePosition);
        var info = await service.GetDataTipInfoAsync(document, position, includeKind: true, cancellationToken).ConfigureAwait(false);
        if (info.IsDefault)
            return null;

        return new VSInternalDataTip
        {
            DataTipTags = info.Kind == DebugDataTipInfoKind.LinqExpression ? VSInternalDataTipTags.LinqExpression : 0,
            HoverRange = ProtocolConversions.TextSpanToRange(info.Span, text),
            ExpressionRange = ProtocolConversions.TextSpanToRange(info.ExpressionSpan, text),
        };
    }
}
