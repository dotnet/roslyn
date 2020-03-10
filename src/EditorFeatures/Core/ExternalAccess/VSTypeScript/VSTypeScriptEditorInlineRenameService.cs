// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript
{
    internal static class VSTypeScriptInlineRenameReplacementKindHelpers
    {
        public static InlineRenameReplacementKind ConvertTo(VSTypeScriptInlineRenameReplacementKind kind)
        {
            switch (kind)
            {
                case VSTypeScriptInlineRenameReplacementKind.NoConflict:
                    {
                        return InlineRenameReplacementKind.NoConflict;
                    }

                case VSTypeScriptInlineRenameReplacementKind.ResolvedReferenceConflict:
                    {
                        return InlineRenameReplacementKind.ResolvedReferenceConflict;
                    }

                case VSTypeScriptInlineRenameReplacementKind.ResolvedNonReferenceConflict:
                    {
                        return InlineRenameReplacementKind.ResolvedNonReferenceConflict;
                    }

                case VSTypeScriptInlineRenameReplacementKind.UnresolvedConflict:
                    {
                        return InlineRenameReplacementKind.UnresolvedConflict;
                    }

                case VSTypeScriptInlineRenameReplacementKind.Complexified:
                    {
                        return InlineRenameReplacementKind.Complexified;
                    }

                default:
                    {
                        throw ExceptionUtilities.UnexpectedValue(kind);
                    }
            }
        }
    }

    internal class VSTypeScriptInlineRenameReplacementInfo : IInlineRenameReplacementInfo
    {
        private readonly IVSTypeScriptInlineRenameReplacementInfo _info;

        public VSTypeScriptInlineRenameReplacementInfo(IVSTypeScriptInlineRenameReplacementInfo info)
        {
            _info = info;
        }

        public Solution NewSolution => _info.NewSolution;

        public bool ReplacementTextValid => _info.ReplacementTextValid;

        public IEnumerable<DocumentId> DocumentIds => _info.DocumentIds;

        public IEnumerable<InlineRenameReplacement> GetReplacements(DocumentId documentId)
        {
            return _info.GetReplacements(documentId)?.Select(x =>
                new InlineRenameReplacement(VSTypeScriptInlineRenameReplacementKindHelpers.ConvertTo(x.Kind), x.OriginalSpan, x.NewSpan));
        }
    }

    internal class VSTypeScriptInlineRenameLocationSet : IInlineRenameLocationSet
    {
        private readonly IVSTypeScriptInlineRenameLocationSet _set;
        private readonly IList<InlineRenameLocation> _locations;

        public VSTypeScriptInlineRenameLocationSet(IVSTypeScriptInlineRenameLocationSet set)
        {
            _set = set;
            _locations = set.Locations?.Select(x => new InlineRenameLocation(x.Document, x.TextSpan)).ToList();
        }

        public IList<InlineRenameLocation> Locations => _locations;

        public async Task<IInlineRenameReplacementInfo> GetReplacementsAsync(string replacementText, OptionSet optionSet, CancellationToken cancellationToken)
        {
            var info = await _set.GetReplacementsAsync(replacementText, optionSet, cancellationToken).ConfigureAwait(false);
            if (info != null)
            {
                return new VSTypeScriptInlineRenameReplacementInfo(info);
            }
            else
            {
                return null;
            }
        }
    }

    internal class VSTypeScriptInlineRenameInfo : IInlineRenameInfo
    {
        private readonly IVSTypeScriptInlineRenameInfo _info;

        public VSTypeScriptInlineRenameInfo(IVSTypeScriptInlineRenameInfo info)
        {
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

        public TextSpan? GetConflictEditSpan(InlineRenameLocation location, string replacementText, CancellationToken cancellationToken)
        {
            return _info.GetConflictEditSpan(new VSTypeScriptInlineRenameLocation(location.Document, location.TextSpan), replacementText, cancellationToken);
        }

        public string GetFinalSymbolName(string replacementText)
        {
            return _info.GetFinalSymbolName(replacementText);
        }

        public TextSpan GetReferenceEditSpan(InlineRenameLocation location, CancellationToken cancellationToken)
        {
            return _info.GetReferenceEditSpan(new VSTypeScriptInlineRenameLocation(location.Document, location.TextSpan), cancellationToken);
        }

        public bool TryOnAfterGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, string replacementText)
        {
            return _info.TryOnAfterGlobalSymbolRenamed(workspace, changedDocumentIDs, replacementText);
        }

        public bool TryOnBeforeGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, string replacementText)
        {
            return _info.TryOnBeforeGlobalSymbolRenamed(workspace, changedDocumentIDs, replacementText);
        }
    }
}
