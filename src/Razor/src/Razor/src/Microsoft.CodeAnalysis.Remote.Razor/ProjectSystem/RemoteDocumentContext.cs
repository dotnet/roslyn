// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

internal sealed class RemoteDocumentContext(RemoteDocumentSnapshot snapshot)
{
    private RazorCodeDocument? _codeDocument;

    public RemoteDocumentSnapshot Snapshot { get; } = snapshot;

    public TextDocument TextDocument => Snapshot.TextDocument;

    public RemoteSolutionSnapshot GetSolutionSnapshot()
        => Snapshot.ProjectSnapshot.SolutionSnapshot;

    private bool TryGetCodeDocument([NotNullWhen(true)] out RazorCodeDocument? codeDocument)
    {
        codeDocument = _codeDocument;
        return codeDocument is not null;
    }

    public ValueTask<RazorCodeDocument> GetCodeDocumentAsync(CancellationToken cancellationToken)
    {
        return TryGetCodeDocument(out var codeDocument)
            ? new(codeDocument)
            : GetCodeDocumentCoreAsync(cancellationToken);

        async ValueTask<RazorCodeDocument> GetCodeDocumentCoreAsync(CancellationToken cancellationToken)
        {
            var codeDocument = await Snapshot
                .GetGeneratedOutputAsync(cancellationToken)
                .ConfigureAwait(false);

            // Interlock to ensure that we only ever return one instance of RazorCodeDocument.
            // In race scenarios, when more than one RazorCodeDocument is produced, we want to
            // return whichever RazorCodeDocument is cached.
            return InterlockedOperations.Initialize(ref _codeDocument, codeDocument);
        }
    }
}
