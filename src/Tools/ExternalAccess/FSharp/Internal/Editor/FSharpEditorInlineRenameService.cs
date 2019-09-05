// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Editor
{
    internal static class FSharpInlineRenameReplacementKindHelpers
    {
        public static InlineRenameReplacementKind ConvertTo(FSharpInlineRenameReplacementKind kind)
        {
            switch (kind)
            {
                case FSharpInlineRenameReplacementKind.NoConflict:
                    {
                        return InlineRenameReplacementKind.NoConflict;
                    }

                case FSharpInlineRenameReplacementKind.ResolvedReferenceConflict:
                    {
                        return InlineRenameReplacementKind.ResolvedReferenceConflict;
                    }

                case FSharpInlineRenameReplacementKind.ResolvedNonReferenceConflict:
                    {
                        return InlineRenameReplacementKind.ResolvedNonReferenceConflict;
                    }

                case FSharpInlineRenameReplacementKind.UnresolvedConflict:
                    {
                        return InlineRenameReplacementKind.UnresolvedConflict;
                    }

                case FSharpInlineRenameReplacementKind.Complexified:
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
    internal class FSharpInlineRenameReplacementInfo : IInlineRenameReplacementInfo
    {
        private readonly IFSharpInlineRenameReplacementInfo _info;

        public FSharpInlineRenameReplacementInfo(IFSharpInlineRenameReplacementInfo info)
        {
            _info = info;
        }

        public Solution NewSolution => _info.NewSolution;

        public bool ReplacementTextValid => _info.ReplacementTextValid;

        public IEnumerable<DocumentId> DocumentIds => _info.DocumentIds;

        public IEnumerable<InlineRenameReplacement> GetReplacements(DocumentId documentId)
        {
            return _info.GetReplacements(documentId)?.Select(x =>
                new InlineRenameReplacement(FSharpInlineRenameReplacementKindHelpers.ConvertTo(x.Kind), x.OriginalSpan, x.NewSpan));
        }
    }
    internal class FSharpInlineRenameLocationSet : IInlineRenameLocationSet
    {
        private readonly IFSharpInlineRenameLocationSet _set;
        private readonly IList<InlineRenameLocation> _locations;

        public FSharpInlineRenameLocationSet(IFSharpInlineRenameLocationSet set)
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
                return new FSharpInlineRenameReplacementInfo(info);
            }
            else
            {
                return null;
            }
        }
    }

    internal class FSharpInlineRenameInfo : IInlineRenameInfo
    {
        private readonly IFSharpInlineRenameInfo _info;

        public FSharpInlineRenameInfo(IFSharpInlineRenameInfo info)
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

        public Glyph Glyph => FSharpGlyphHelpers.ConvertTo(_info.Glyph);

        public async Task<IInlineRenameLocationSet> FindRenameLocationsAsync(OptionSet optionSet, CancellationToken cancellationToken)
        {
            var set = await _info.FindRenameLocationsAsync(optionSet, cancellationToken).ConfigureAwait(false);
            if (set != null)
            {
                return new FSharpInlineRenameLocationSet(set);
            }
            else
            {
                return null;
            }
        }

        public TextSpan? GetConflictEditSpan(InlineRenameLocation location, string replacementText, CancellationToken cancellationToken)
        {
            return _info.GetConflictEditSpan(new FSharpInlineRenameLocation(location.Document, location.TextSpan), replacementText, cancellationToken);
        }

        public string GetFinalSymbolName(string replacementText)
        {
            return _info.GetFinalSymbolName(replacementText);
        }

        public TextSpan GetReferenceEditSpan(InlineRenameLocation location, CancellationToken cancellationToken)
        {
            return _info.GetReferenceEditSpan(new FSharpInlineRenameLocation(location.Document, location.TextSpan), cancellationToken);
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

    [Shared]
    [ExportLanguageService(typeof(IEditorInlineRenameService), LanguageNames.FSharp)]
    internal class FSharpEditorInlineRenameService : IEditorInlineRenameService
    {
        private readonly IFSharpEditorInlineRenameService _service;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FSharpEditorInlineRenameService(IFSharpEditorInlineRenameService service)
        {
            _service = service;
        }

        public async Task<IInlineRenameInfo> GetRenameInfoAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var info = await _service.GetRenameInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
            if (info != null)
            {
                return new FSharpInlineRenameInfo(info);
            }
            else
            {
                return null;
            }
        }
    }
}
