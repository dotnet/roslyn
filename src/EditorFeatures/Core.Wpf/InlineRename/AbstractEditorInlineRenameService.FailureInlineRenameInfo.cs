// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            {
                this.LocalizedErrorMessage = localizedErrorMessage;
            }

            public bool CanRename => false;

            public bool HasOverloads => false;

            public bool ForceRenameOverloads => false;

            public string LocalizedErrorMessage { get; }

            public TextSpan TriggerSpan => default;

            public string DisplayName => null;

            public string FullDisplayName => null;

            public Glyph Glyph => Glyph.None;

            public string GetFinalSymbolName(string replacementText) { return null; }

            public TextSpan GetReferenceEditSpan(InlineRenameLocation location, CancellationToken cancellationToken) { return default; }

            public TextSpan? GetConflictEditSpan(InlineRenameLocation location, string replacementText, CancellationToken cancellationToken) { return null; }

            public Task<IInlineRenameLocationSet> FindRenameLocationsAsync(OptionSet optionSet, CancellationToken cancellationToken) { return null; }

            public bool TryOnAfterGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, string replacementText) { return false; }

            public bool TryOnBeforeGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, string replacementText) { return false; }
        }
    }
}
