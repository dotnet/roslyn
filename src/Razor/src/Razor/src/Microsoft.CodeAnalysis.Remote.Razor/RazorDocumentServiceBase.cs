// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

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
            if (DocumentMappingService.TryMapToCSharpDocumentLinePosition(codeDocument, positionInfo.HostDocumentIndex, out var csharpPosition, out _, out var inDeclDocument))
            {
                // We're just gonna pretend this mapped perfectly normally onto C#. Moving this logic to the actual position info
                // calculating code is possible, but could have untold effects, so opt-in is better (for now?)

                // TODO: Not using a with operator here because it doesn't work in OOP for some reason.
                positionInfo = new DocumentPositionInfo(RazorLanguageKind.CSharp, csharpPosition.ToPosition(), positionInfo.HostDocumentIndex, inDeclDocument);
            }
        }

        return positionInfo;
    }

    protected ValueTask<T> RunServiceAsync<T>(
        RazorSolutionWrapper solutionInfo,
        DocumentId razorDocumentId,
        Func<RemoteDocumentSnapshot, ValueTask<T>> implementation,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(
            solutionInfo,
            solution =>
            {
                var documentSnapshot = CreateRazorDocumentSnapshot(solution, razorDocumentId);
                if (documentSnapshot is null)
                {
                    return default;
                }

                return implementation(documentSnapshot);
            },
            cancellationToken);
    }

    protected RemoteDocumentSnapshot? CreateRazorDocumentSnapshot(Solution solution, DocumentId razorDocumentId)
    {
        var razorDocument = solution.GetAdditionalDocument(razorDocumentId);
        if (razorDocument is null)
        {
            return null;
        }

        var documentSnapshot = SnapshotManager.GetSnapshot(razorDocument);

        return documentSnapshot;
    }
}
