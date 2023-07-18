// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal abstract class VSTypeScriptInlineRenameInfo : IInlineRenameInfo
    {
        public abstract bool CanRename { get; }
        public abstract string DisplayName { get; }
        public abstract string FullDisplayName { get; }
        public abstract VSTypeScriptGlyph Glyph { get; }
        public abstract bool HasOverloads { get; }
        public abstract bool ForceRenameOverloads { get; }
        public abstract string LocalizedErrorMessage { get; }
        public abstract TextSpan TriggerSpan { get; }
        public abstract ImmutableArray<VSTypeScriptDocumentSpan> DefinitionLocations { get; }
        public abstract Task<VSTypeScriptInlineRenameLocationSet> FindRenameLocationsAsync(bool renameInStrings, bool renameInComments, CancellationToken cancellationToken);
        public abstract TextSpan? GetConflictEditSpan(VSTypeScriptInlineRenameLocationWrapper location, string replacementText, CancellationToken cancellationToken);
        public abstract string GetFinalSymbolName(string replacementText);
        public abstract TextSpan GetReferenceEditSpan(VSTypeScriptInlineRenameLocationWrapper location, CancellationToken cancellationToken);

        bool IInlineRenameInfo.MustRenameOverloads
            => ForceRenameOverloads;

        Glyph IInlineRenameInfo.Glyph
            => VSTypeScriptGlyphHelpers.ConvertTo(Glyph);

        ImmutableArray<DocumentSpan> IInlineRenameInfo.DefinitionLocations
            => DefinitionLocations.SelectAsArray(l => new DocumentSpan(l.Document, l.SourceSpan));

        async Task<IInlineRenameLocationSet> IInlineRenameInfo.FindRenameLocationsAsync(SymbolRenameOptions options, CancellationToken cancellationToken)
            => await FindRenameLocationsAsync(options.RenameInStrings, options.RenameInComments, cancellationToken).ConfigureAwait(false);

        TextSpan? IInlineRenameInfo.GetConflictEditSpan(InlineRenameLocation location, string triggerText, string replacementText, CancellationToken cancellationToken)
            => GetConflictEditSpan(new VSTypeScriptInlineRenameLocationWrapper(
                new InlineRenameLocation(location.Document, location.TextSpan)), replacementText, cancellationToken);

        TextSpan IInlineRenameInfo.GetReferenceEditSpan(InlineRenameLocation location, string triggerText, CancellationToken cancellationToken)
            => GetReferenceEditSpan(new VSTypeScriptInlineRenameLocationWrapper(
                new InlineRenameLocation(location.Document, location.TextSpan)), cancellationToken);

        bool IInlineRenameInfo.TryOnAfterGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, string replacementText)
            => true;

        bool IInlineRenameInfo.TryOnBeforeGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, string replacementText)
            => true;

        public InlineRenameFileRenameInfo GetFileRenameInfo()
            => InlineRenameFileRenameInfo.NotAllowed;
    }
}
