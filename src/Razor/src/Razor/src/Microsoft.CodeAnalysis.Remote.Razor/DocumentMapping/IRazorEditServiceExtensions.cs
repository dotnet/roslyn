// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;

internal static class IRazorEditServiceExtensions
{
#if SONICDEV
    [System.Obsolete("PROTOTYPE(sonic): Call the overload that takes a bool to prove that you thought about which document to get")]
#endif
    public static async Task<ImmutableArray<TextChange>> MapCSharpEditsAsync(
        this IRazorEditService service,
        ImmutableArray<TextChange> textChanges,
        RemoteDocumentSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var mappedChanges = await service.MapCSharpEditsAsync(
            textChanges.SelectAsArray(static c => c.ToRazorTextChange()),
            snapshot,
            declarationDocument: false,
            includeCSharpLanguageFeatureEdits: true,
            directlyMappedEditFilter: null,
            cancellationToken).ConfigureAwait(false);

        return mappedChanges.SelectAsArray(static c => c.ToTextChange());
    }

    public static async Task<ImmutableArray<TextChange>> MapCSharpEditsAsync(
        this IRazorEditService service,
        ImmutableArray<TextChange> textChanges,
        bool declarationDocument,
        RemoteDocumentSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var mappedChanges = await service.MapCSharpEditsAsync(
            textChanges.SelectAsArray(static c => c.ToRazorTextChange()),
            snapshot,
            declarationDocument,
            includeCSharpLanguageFeatureEdits: true,
            directlyMappedEditFilter: null,
            cancellationToken).ConfigureAwait(false);

        return mappedChanges.SelectAsArray(static c => c.ToTextChange());
    }
}
