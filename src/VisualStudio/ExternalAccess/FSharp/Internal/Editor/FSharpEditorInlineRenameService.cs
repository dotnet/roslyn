// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Rename;
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

    [Obsolete]
    internal class FSharpInlineRenameReplacementInfoLegacyWrapper : IInlineRenameReplacementInfo
    {
        private readonly IFSharpInlineRenameReplacementInfo _info;

        public FSharpInlineRenameReplacementInfoLegacyWrapper(IFSharpInlineRenameReplacementInfo info)
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

    [Obsolete]
    internal class FSharpInlineRenameLocationSetLegacyWrapper : IInlineRenameLocationSet
    {
        private readonly IFSharpInlineRenameLocationSet _set;

        public FSharpInlineRenameLocationSetLegacyWrapper(IFSharpInlineRenameLocationSet set)
        {
            _set = set;
            Locations = set.Locations?.Select(x => new InlineRenameLocation(x.Document, x.TextSpan)).ToList();
        }

        public IList<InlineRenameLocation> Locations { get; }

        public async Task<IInlineRenameReplacementInfo> GetReplacementsAsync(string replacementText, SymbolRenameOptions options, CancellationToken cancellationToken)
        {
            var info = await _set.GetReplacementsAsync(replacementText, optionSet: null, cancellationToken).ConfigureAwait(false);
            if (info != null)
            {
                return new FSharpInlineRenameReplacementInfoLegacyWrapper(info);
            }
            else
            {
                return null;
            }
        }
    }

    [Obsolete]
    internal class FSharpInlineRenameInfoLegacyWrapper : IInlineRenameInfo
    {
        private readonly IFSharpInlineRenameInfo _info;

        public FSharpInlineRenameInfoLegacyWrapper(IFSharpInlineRenameInfo info)
        {
            _info = info;
        }

        public bool CanRename => _info.CanRename;

        public string LocalizedErrorMessage => _info.LocalizedErrorMessage;

        public TextSpan TriggerSpan => _info.TriggerSpan;

        public bool HasOverloads => _info.HasOverloads;

        public bool MustRenameOverloads => _info.ForceRenameOverloads;

        public string DisplayName => _info.DisplayName;

        public string FullDisplayName => _info.FullDisplayName;

        public Glyph Glyph => FSharpGlyphHelpers.ConvertTo(_info.Glyph);

        // This property isn't currently supported in F# since it would involve modifying the IFSharpInlineRenameInfo interface.
        public ImmutableArray<DocumentSpan> DefinitionLocations => default;

        public async Task<IInlineRenameLocationSet> FindRenameLocationsAsync(SymbolRenameOptions options, CancellationToken cancellationToken)
        {
            var set = await _info.FindRenameLocationsAsync(optionSet: null, cancellationToken).ConfigureAwait(false);
            if (set != null)
            {
                return new FSharpInlineRenameLocationSetLegacyWrapper(set);
            }
            else
            {
                return null;
            }
        }

        public TextSpan? GetConflictEditSpan(InlineRenameLocation location, string triggerText, string replacementText, CancellationToken cancellationToken)
        {
            return _info.GetConflictEditSpan(new FSharpInlineRenameLocation(location.Document, location.TextSpan), replacementText, cancellationToken);
        }

        public string GetFinalSymbolName(string replacementText)
        {
            return _info.GetFinalSymbolName(replacementText);
        }

        public TextSpan GetReferenceEditSpan(InlineRenameLocation location, string triggerText, CancellationToken cancellationToken)
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

        public InlineRenameFileRenameInfo GetFileRenameInfo()
            => InlineRenameFileRenameInfo.NotAllowed;
    }

#nullable enable
    [Shared]
    [ExportLanguageService(typeof(IEditorInlineRenameService), LanguageNames.FSharp)]
    internal class FSharpEditorInlineRenameService : IEditorInlineRenameService
    {
        [Obsolete]
        private readonly IFSharpEditorInlineRenameService? _legacyService;

        private readonly FSharpInlineRenameServiceImplementation? _service;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FSharpEditorInlineRenameService(
            [Import(AllowDefault = true)] IFSharpEditorInlineRenameService? legacyService,
            [Import(AllowDefault = true)] FSharpInlineRenameServiceImplementation? service)
        {
            _legacyService = legacyService;
            _service = service;
        }

        public bool IsEnabled => true;

        public Task<ImmutableDictionary<string, ImmutableArray<string>>> GetRenameContextAsync(IInlineRenameInfo inlineRenameInfo, IInlineRenameLocationSet inlineRenameLocationSet, CancellationToken cancellationToken)
        {
            return Task.FromResult(ImmutableDictionary<string, ImmutableArray<string>>.Empty);
        }

        public async Task<IInlineRenameInfo> GetRenameInfoAsync(Document document, int position, CancellationToken cancellationToken)
        {
#pragma warning disable CS0612 // Type or member is obsolete
            if (_legacyService != null)
            {
                var info = await _legacyService.GetRenameInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
                return (info != null) ? new FSharpInlineRenameInfoLegacyWrapper(info) : AbstractEditorInlineRenameService.DefaultFailureInfo;
            }
#pragma warning restore CS0612 // Type or member is obsolete

            if (_service != null)
            {
                return await _service.GetRenameInfoAsync(document, position, cancellationToken).ConfigureAwait(false) ?? AbstractEditorInlineRenameService.DefaultFailureInfo;
            }

            return AbstractEditorInlineRenameService.DefaultFailureInfo;
        }
    }
}
