// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Rename
{
    internal partial class RoslynRenameService
    {
        /// <summary>
        /// Used for a failed rename session. CanRename and LocalizedErrorMessage are the only
        /// relevant properties.
        /// </summary>
        private class FailureInlineRenameInfo : IInlineRenameInfo
        {
            public FailureInlineRenameInfo(string localizedErrorMessage)
            {
                LocalizedErrorMessage = localizedErrorMessage;
            }

            public bool CanRename => false;
            public bool HasOverloads => false;
            public bool ForceRenameOverloads => false;
            public string LocalizedErrorMessage { get; }
            public TextSpan TriggerSpan => default;
            public string DisplayName => null;
            public string FullDisplayName => null;
            public Glyph Glyph => Glyph.None;
            public string GetFinalSymbolName(string replacementText) => null;
            public TextSpan GetReferenceEditSpan(InlineRenameLocation location, CancellationToken cancellationToken) => default;
            public TextSpan? GetConflictEditSpan(InlineRenameLocation location, string replacementText, CancellationToken cancellationToken) => null;
            public Task<IInlineRenameLocationSet> FindRenameLocationsAsync(OptionSet optionSet, CancellationToken cancellationToken) => null;
            public bool TryOnAfterGlobalSymbolRenamed(CodeAnalysis.Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, string replacementText) => false;
            public bool TryOnBeforeGlobalSymbolRenamed(CodeAnalysis.Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, string replacementText) => false;
        }
    }
}
