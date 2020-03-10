// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.ExternalAccess.VSTypeScript
{
    internal abstract class VSTypeScriptInlineRenameInfoBase : IInlineRenameInfo
    {
        public abstract bool CanRename { get; }
        public abstract string LocalizedErrorMessage { get; }
        public abstract TextSpan TriggerSpan { get; }
        public abstract bool HasOverloads { get; }
        public abstract bool ForceRenameOverloads { get; }
        public abstract string DisplayName { get; }
        public abstract string FullDisplayName { get; }
        public abstract Glyph Glyph { get; }
        public abstract ImmutableArray<DocumentSpan> DefinitionLocations { get; }

        public abstract Task<IInlineRenameLocationSet> FindRenameLocationsAsync(OptionSet optionSet, CancellationToken cancellationToken);
        public abstract TextSpan? GetConflictEditSpan(InlineRenameLocation location, string replacementText, CancellationToken cancellationToken);
        public abstract string GetFinalSymbolName(string replacementText);
        public abstract TextSpan GetReferenceEditSpan(InlineRenameLocation location, CancellationToken cancellationToken);
        public abstract bool TryOnAfterGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, string replacementText);
        public abstract bool TryOnBeforeGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, string replacementText);
    }
}
