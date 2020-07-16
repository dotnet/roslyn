// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal abstract partial class AbstractEditorInlineRenameService
    {
        private class FailureInlineRenameInfo : IInlineRenameInfo
        {
            public FailureInlineRenameInfo(string localizedErrorMessage)
                => this.LocalizedErrorMessage = localizedErrorMessage;

            public bool CanRename => false;

            public bool HasOverloads => false;

            public bool ForceRenameOverloads => false;

            public string LocalizedErrorMessage { get; }

            public TextSpan TriggerSpan => default;

            public string DisplayName => null;

            public string FullDisplayName => null;

            public Glyph Glyph => Glyph.None;

            public ImmutableArray<DocumentSpan> DefinitionLocations => default;

            public string GetFinalSymbolName(string replacementText) => null;

            public TextSpan GetReferenceEditSpan(InlineRenameLocation location, string triggerText, CancellationToken cancellationToken) => default;

            public TextSpan? GetConflictEditSpan(InlineRenameLocation location, string triggerText, string replacementText, CancellationToken cancellationToken) => null;

            public Task<IInlineRenameLocationSet> FindRenameLocationsAsync(OptionSet optionSet, CancellationToken cancellationToken) => Task.FromResult<IInlineRenameLocationSet>(null);

            public bool TryOnAfterGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, string replacementText) => false;

            public bool TryOnBeforeGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, string replacementText) => false;
        }
    }
}
