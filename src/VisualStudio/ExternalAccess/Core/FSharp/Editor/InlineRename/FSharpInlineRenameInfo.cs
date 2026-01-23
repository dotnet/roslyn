// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp;
using Microsoft.VisualStudio.ExternalAccess.FSharp.Internal;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Editor;
#else
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;
#endif

internal abstract class FSharpInlineRenameInfo : IInlineRenameInfo
{
    public abstract bool CanRename { get; }
    public abstract string DisplayName { get; }
    public abstract string FullDisplayName { get; }
    public abstract FSharpGlyph Glyph { get; }
    public abstract bool HasOverloads { get; }
    public abstract bool ForceRenameOverloads { get; }
    public abstract string LocalizedErrorMessage { get; }
    public abstract TextSpan TriggerSpan { get; }
    public abstract ImmutableArray<FSharpInlineRenameLocation> DefinitionLocations { get; }
    public abstract Task<FSharpInlineRenameLocationSet> FindRenameLocationsAsync(bool renameInStrings, bool renameInComments, CancellationToken cancellationToken);
    public abstract TextSpan? GetConflictEditSpan(FSharpInlineRenameLocation location, string replacementText, CancellationToken cancellationToken);
    public abstract string GetFinalSymbolName(string replacementText);
    public abstract TextSpan GetReferenceEditSpan(FSharpInlineRenameLocation location, CancellationToken cancellationToken);

    Glyph IInlineRenameInfo.Glyph
        => FSharpGlyphHelpers.ConvertTo(Glyph);

    bool IInlineRenameInfo.MustRenameOverloads
        => ForceRenameOverloads;

    ImmutableArray<DocumentSpan> IInlineRenameInfo.DefinitionLocations
        => DefinitionLocations.SelectAsArray(l => new DocumentSpan(l.Document, l.TextSpan));

    async Task<IInlineRenameLocationSet> IInlineRenameInfo.FindRenameLocationsAsync(SymbolRenameOptions options, CancellationToken cancellationToken)
        => await FindRenameLocationsAsync(
            options.RenameInStrings,
            options.RenameInComments,
            cancellationToken).ConfigureAwait(false);

    TextSpan? IInlineRenameInfo.GetConflictEditSpan(InlineRenameLocation location, string triggerText, string replacementText, CancellationToken cancellationToken)
        => GetConflictEditSpan(new FSharpInlineRenameLocation(location.Document, location.TextSpan), replacementText, cancellationToken);

    TextSpan IInlineRenameInfo.GetReferenceEditSpan(InlineRenameLocation location, string triggerText, CancellationToken cancellationToken)
        => GetReferenceEditSpan(new FSharpInlineRenameLocation(location.Document, location.TextSpan), cancellationToken);

    bool IInlineRenameInfo.TryOnAfterGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, string replacementText)
        => true;

    bool IInlineRenameInfo.TryOnBeforeGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, string replacementText)
        => true;

    public InlineRenameFileRenameInfo GetFileRenameInfo()
        => InlineRenameFileRenameInfo.NotAllowed;
}
