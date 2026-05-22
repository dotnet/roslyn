// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class RemoteDebugInfoService(in ServiceArgs args) : RazorDocumentServiceBase(in args), IRemoteDebugInfoService
{
    internal sealed class Factory : FactoryBase<IRemoteDebugInfoService>
    {
        protected override IRemoteDebugInfoService CreateService(in ServiceArgs args)
            => new RemoteDebugInfoService(in args);
    }

    private readonly IDocumentMappingService _documentMappingService = args.ExportProvider.GetExportedValue<IDocumentMappingService>();

    public ValueTask<LinePositionSpan?> ValidateBreakableRangeAsync(RazorPinnedSolutionInfoWrapper solutionInfo, DocumentId documentId, LinePositionSpan span, CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            documentId,
            context => ValidateBreakableRangeAsync(context, span, cancellationToken),
            cancellationToken);

    public async ValueTask<LinePositionSpan?> ValidateBreakableRangeAsync(RemoteDocumentContext context, LinePositionSpan span, CancellationToken cancellationToken)
    {
        var codeDocument = await context.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);
        var csharpDocument = codeDocument.GetRequiredCSharpDocument();

        if (!_documentMappingService.TryMapToCSharpDocumentRange(csharpDocument, span, out var mappedSpan))
        {
            return null;
        }

        var generatedDocument = await context.Snapshot.GetGeneratedDocumentAsync(cancellationToken).ConfigureAwait(false);

        var result = await ExternalAccess.Razor.Cohost.Handlers.ValidateBreakableRange.GetBreakableRangeAsync(generatedDocument, mappedSpan, cancellationToken).ConfigureAwait(false);
        if (result is { } csharpSpan &&
            _documentMappingService.TryMapToRazorDocumentRange(codeDocument.GetRequiredCSharpDocument(), csharpSpan, MappingBehavior.Inclusive, out var hostSpan))
        {
            return hostSpan;
        }

        return null;
    }

    public ValueTask<LinePositionSpan?> ResolveBreakpointRangeAsync(RazorPinnedSolutionInfoWrapper solutionInfo, DocumentId documentId, LinePosition position, CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            documentId,
            context => ResolveBreakpointRangeAsync(context, position, cancellationToken),
            cancellationToken);

    private async ValueTask<LinePositionSpan?> ResolveBreakpointRangeAsync(RemoteDocumentContext context, LinePosition position, CancellationToken cancellationToken)
    {
        var codeDocument = await context.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);
        if (!TryGetUsableProjectedIndex(codeDocument, position, out var projectedIndex))
        {
            return null;
        }

        // Now ask Roslyn to adjust the breakpoint to a valid location in the code
        var syntaxTree = await context.Snapshot.GetCSharpSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        if (!RazorBreakpointSpans.TryGetBreakpointSpan(syntaxTree, projectedIndex, cancellationToken, out var csharpBreakpointSpan))
        {
            return null;
        }

        var csharpText = codeDocument.GetCSharpSourceText();
        var projectedRange = csharpText.GetLinePositionSpan(csharpBreakpointSpan);

        // Inclusive mapping means we are lenient to portions of the breakpoint that might be outside of use code in the Razor file
        if (!_documentMappingService.TryMapToRazorDocumentRange(codeDocument.GetRequiredCSharpDocument(), projectedRange, MappingBehavior.Inclusive, out var hostDocumentRange))
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();

        return hostDocumentRange;
    }

    public ValueTask<string[]?> ResolveProximityExpressionsAsync(RazorPinnedSolutionInfoWrapper solutionInfo, DocumentId documentId, LinePosition position, CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            documentId,
            context => ResolveProximityExpressionsAsync(context, position, cancellationToken),
            cancellationToken);

    private async ValueTask<string[]?> ResolveProximityExpressionsAsync(RemoteDocumentContext context, LinePosition position, CancellationToken cancellationToken)
    {
        var codeDocument = await context.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);
        if (!TryGetUsableProjectedIndex(codeDocument, position, out var projectedIndex))
        {
            return null;
        }

        // Now ask Roslyn to adjust the breakpoint to a valid location in the code
        var syntaxTree = await context.Snapshot.GetCSharpSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var result = RazorCSharpProximityExpressionResolverService.GetProximityExpressions(syntaxTree, projectedIndex, cancellationToken);

        return result?.ToArray();
    }

    private bool TryGetUsableProjectedIndex(RazorCodeDocument codeDocument, LinePosition hostDocumentPosition, out int projectedIndex)
    {
        projectedIndex = 0;

        var sourceText = codeDocument.Source.Text;
        var hostDocumentIndex = sourceText.GetPosition(hostDocumentPosition);
        var syntaxRoot = codeDocument.GetRequiredSyntaxRoot();
        var csharpDocument = codeDocument.GetRequiredCSharpDocument();

        // We want to find a position that maps to C# on the same line as the original request, but we might have to skip over
        // some Razor/HTML nodes to find valid C#.
        while (sourceText.GetLinePosition(hostDocumentIndex).Line == hostDocumentPosition.Line)
        {
            if (_documentMappingService.TryMapToCSharpPositionOrNext(csharpDocument, hostDocumentIndex, out _, out projectedIndex))
            {
                if (syntaxRoot.FindInnermostNode(hostDocumentIndex) is not { } node)
                {
                    return false;
                }

                // We want to avoid component tags and component attributes, where we map to C#, but they're not valid breakpoint locations
                if (!node.IsAnyAttributeSyntax() && node is not (MarkupTagHelperStartTagSyntax or MarkupEndTagSyntax))
                {
                    // Found something valid!
                    return true;
                }

                // It's C#, but not valid, so skip past it so we can try to find more C#
                hostDocumentIndex = node.Span.End + 1;
            }

            // See if there is more C# on the line to map to, for example "$$<p>@DateTime.Now</p>"
            if (!_documentMappingService.TryMapToCSharpPositionOrNext(csharpDocument, hostDocumentIndex, out _, out projectedIndex))
            {
                return false;
            }

            // We found some C# later on the line, so map that back to Razor so we can loop around and check the node type
            if (!_documentMappingService.TryMapToRazorDocumentPosition(csharpDocument, projectedIndex, out _, out hostDocumentIndex))
            {
                return false;
            }
        }

        return false;
    }
}
