// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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

            public bool CanRename { get { return false; } }

            public bool HasOverloads { get { return false; } }

            public bool ForceRenameOverloads { get { return false; } }

            public string LocalizedErrorMessage { get; }

            public TextSpan TriggerSpan { get { return default(TextSpan); } }

            public string DisplayName { get { return null; } }

            public string FullDisplayName { get { return null; } }

            public Glyph Glyph { get { return default(Glyph); } }

            public string GetFinalSymbolName(string replacementText) { return null; }

            public TextSpan GetReferenceEditSpan(InlineRenameLocation location, CancellationToken cancellationToken) { return default(TextSpan); }

            public TextSpan? GetConflictEditSpan(InlineRenameLocation location, string replacementText, CancellationToken cancellationToken) { return null; }

            public Task<IInlineRenameLocationSet> FindRenameLocationsAsync(OptionSet optionSet, CancellationToken cancellationToken) { return null; }

            public bool TryOnAfterGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, string replacementText) { return false; }

            public bool TryOnBeforeGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, string replacementText) { return false; }
        }
    }
}
