// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal sealed class VSTypeScriptInlineRenameInfo : IInlineRenameInfo
    {
        private readonly IVSTypeScriptInlineRenameInfo _info;

        public VSTypeScriptInlineRenameInfo(IVSTypeScriptInlineRenameInfo info)
        {
            Contract.ThrowIfNull(info);
            _info = info;
        }

        public bool CanRename => _info.CanRename;

        public string LocalizedErrorMessage => _info.LocalizedErrorMessage;

        public TextSpan TriggerSpan => _info.TriggerSpan;

        public bool HasOverloads => _info.HasOverloads;

        public bool ForceRenameOverloads => _info.ForceRenameOverloads;

        public string DisplayName => _info.DisplayName;

        public string FullDisplayName => _info.FullDisplayName;

        public Glyph Glyph => VSTypeScriptGlyphHelpers.ConvertTo(_info.Glyph);

        public ImmutableArray<DocumentSpan> DefinitionLocations => _info.DefinitionLocations;

        public async Task<IInlineRenameLocationSet> FindRenameLocationsAsync(OptionSet optionSet, CancellationToken cancellationToken)
        {
            var set = await _info.FindRenameLocationsAsync(optionSet, cancellationToken).ConfigureAwait(false);
            if (set != null)
            {
                return new VSTypeScriptInlineRenameLocationSet(set);
            }
            else
            {
                return null;
            }
        }

        public TextSpan? GetConflictEditSpan(InlineRenameLocation location, string triggerText, string replacementText, CancellationToken cancellationToken)
        {
            return _info.GetConflictEditSpan(new VSTypeScriptInlineRenameLocationWrapper(
                new InlineRenameLocation(location.Document, location.TextSpan)), replacementText, cancellationToken);
        }

        public string GetFinalSymbolName(string replacementText)
            => _info.GetFinalSymbolName(replacementText);

        public TextSpan GetReferenceEditSpan(InlineRenameLocation location, string triggerText, CancellationToken cancellationToken)
        {
            return _info.GetReferenceEditSpan(new VSTypeScriptInlineRenameLocationWrapper(
                new InlineRenameLocation(location.Document, location.TextSpan)), cancellationToken);
        }

        public bool TryOnAfterGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, string replacementText)
            => _info.TryOnAfterGlobalSymbolRenamed(workspace, changedDocumentIDs, replacementText);

        public bool TryOnBeforeGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, string replacementText)
            => _info.TryOnBeforeGlobalSymbolRenamed(workspace, changedDocumentIDs, replacementText);
    }
}
