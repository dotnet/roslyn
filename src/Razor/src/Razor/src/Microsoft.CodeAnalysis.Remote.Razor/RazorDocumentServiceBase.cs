// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal abstract class RazorDocumentServiceBase(in ServiceArgs args) : RazorBrokeredServiceBase(in args)
{
    protected IDocumentMappingService DocumentMappingService { get; } = args.ExportProvider.GetExportedValue<IDocumentMappingService>();

    protected virtual IDocumentPositionInfoStrategy DocumentPositionInfoStrategy { get; } = DefaultDocumentPositionInfoStrategy.Instance;

    protected DocumentPositionInfo GetPositionInfo(RazorCodeDocument codeDocument, int hostDocumentIndex)
        => GetPositionInfo(codeDocument, hostDocumentIndex, preferCSharpOverHtml: false);

    protected DocumentPositionInfo GetPositionInfo(RazorCodeDocument codeDocument, int hostDocumentIndex, bool preferCSharpOverHtml)
    {
        var positionInfo = DocumentPositionInfoStrategy.GetPositionInfo(DocumentMappingService, codeDocument, hostDocumentIndex);

        if (preferCSharpOverHtml && positionInfo.LanguageKind == RazorLanguageKind.Html)
        {
            // Sometimes Html can actually be mapped to C#, like for example component attributes, which map to
            // C# properties, even though they appear entirely in a Html context. Since remapping is pretty cheap
            // it's easier to just try mapping, and see what happens, rather than checking for specific syntax nodes.
            if (DocumentMappingService.TryMapToCSharpDocumentPosition(codeDocument.GetRequiredCSharpDocument(), positionInfo.HostDocumentIndex, out Position? csharpPosition, out _))
            {
                // We're just gonna pretend this mapped perfectly normally onto C#. Moving this logic to the actual position info
                // calculating code is possible, but could have untold effects, so opt-in is better (for now?)

                // TODO: Not using a with operator here because it doesn't work in OOP for some reason.
                positionInfo = new DocumentPositionInfo(RazorLanguageKind.CSharp, csharpPosition, positionInfo.HostDocumentIndex);
            }
        }

        return positionInfo;
    }

    protected bool TryGetDocumentPositionInfo(RazorCodeDocument codeDocument, Position position, out DocumentPositionInfo positionInfo)
        => TryGetDocumentPositionInfo(codeDocument, position, preferCSharpOverHtml: false, out positionInfo);

    protected bool TryGetDocumentPositionInfo(RazorCodeDocument codeDocument, Position position, bool preferCSharpOverHtml, out DocumentPositionInfo positionInfo)
    {
        if (!codeDocument.Source.Text.TryGetAbsoluteIndex(position, out var hostDocumentIndex))
        {
            positionInfo = default;
            return false;
        }

        positionInfo = GetPositionInfo(codeDocument, hostDocumentIndex, preferCSharpOverHtml);
        return true;
    }

    protected ValueTask<T> RunServiceAsync<T>(
        RazorPinnedSolutionInfoWrapper solutionInfo,
        DocumentId razorDocumentId,
        Func<RemoteDocumentContext, ValueTask<T>> implementation,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(
            solutionInfo,
            solution =>
            {
                var documentContext = CreateRazorDocumentContext(solution, razorDocumentId);
                if (documentContext is null)
                {
                    return default;
                }

                return implementation(documentContext);
            },
            cancellationToken);
    }

    protected RemoteDocumentContext? CreateRazorDocumentContext(Solution solution, DocumentId razorDocumentId)
    {
        var razorDocument = solution.GetAdditionalDocument(razorDocumentId);
        if (razorDocument is null)
        {
            return null;
        }

        var documentSnapshot = SnapshotManager.GetSnapshot(razorDocument);

        return new RemoteDocumentContext(razorDocument.CreateUri(), documentSnapshot);
    }
}
