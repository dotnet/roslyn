// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.CodeAnalysis.Razor.DocumentMapping;

internal interface IRazorEditService
{
    /// <summary>
    /// Maps the given text edits for a razor file based on changes in csharp. It special
    /// cases usings directives to insure they are added correctly. All other edits
    /// are applied if they map to the razor document.
    /// </summary>
    /// <remarks>
    /// Note that the changes coming in are in the generated C# file. This method will map them appropriately.
    /// </remarks>
    /// <param name="textChanges">The text changes to map.</param>
    /// <param name="snapshot">The document snapshot to use for mapping.</param>
    /// <param name="includeCSharpLanguageFeatureEdits">Whether to include edits that might be present in unmapped areas of the generated C# document. These requires extra processing, so pass <see langword="false"/> if the scenario makes them unnecessary/impossible</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<ImmutableArray<RazorTextChange>> MapCSharpEditsAsync(
        ImmutableArray<RazorTextChange> textChanges,
        IDocumentSnapshot snapshot,
        bool includeCSharpLanguageFeatureEdits,
        CancellationToken cancellationToken);

    /// <summary>
    /// Maps C# changes in a workspace edit, to their equivalent Razor changes, modifying them in place
    /// </summary>
    Task MapWorkspaceEditAsync(IDocumentSnapshot contextDocumentSnapshot, WorkspaceEdit workspaceEdit, CancellationToken cancellationToken);
}
