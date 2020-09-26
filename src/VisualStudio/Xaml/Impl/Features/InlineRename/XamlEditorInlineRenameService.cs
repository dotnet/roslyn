// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Xaml.Features.InlineRename
{
    [ExportLanguageService(typeof(IEditorInlineRenameService), StringConstants.XamlLanguageName), Shared]
    internal class XamlEditorInlineRenameService : IEditorInlineRenameService
    {
        private readonly IXamlRenameInfoService _renameService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public XamlEditorInlineRenameService(IXamlRenameInfoService renameService)
        {
            _renameService = renameService;
        }

        public async Task<IInlineRenameInfo> GetRenameInfoAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var renameInfo = await _renameService.GetRenameInfoAsync(document, position, cancellationToken).ConfigureAwait(false);

            return new InlineRenameInfo(document, renameInfo);
        }

        private class InlineRenameInfo : IInlineRenameInfo
        {
            private readonly Document _document;
            private readonly IXamlRenameInfo _renameInfo;

            public InlineRenameInfo(Document document, IXamlRenameInfo renameInfo)
            {
                _document = document;
                _renameInfo = renameInfo;
            }

            public bool CanRename => _renameInfo.CanRename;

            public string DisplayName => _renameInfo.DisplayName;

            public string FullDisplayName => _renameInfo.FullDisplayName;

            public Glyph Glyph => InlineRenameInfo.FromSymbolKind(_renameInfo.Kind);

            public bool HasOverloads => false;

            public bool ForceRenameOverloads => false;

            public string LocalizedErrorMessage => _renameInfo.LocalizedErrorMessage;

            public TextSpan TriggerSpan => _renameInfo.TriggerSpan;

            // This property isn't currently supported in XAML since it would involve modifying the IXamlRenameInfo interface.
            public ImmutableArray<CodeAnalysis.DocumentSpan> DefinitionLocations => default;

            public async Task<IInlineRenameLocationSet> FindRenameLocationsAsync(OptionSet optionSet, CancellationToken cancellationToken)
            {
                var references = new List<InlineRenameLocation>();

                var renameLocations = await _renameInfo.FindRenameLocationsAsync(
                    renameInStrings: optionSet.GetOption(RenameOptions.RenameInStrings),
                    renameInComments: optionSet.GetOption(RenameOptions.RenameInComments),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                references.AddRange(renameLocations.Select(
                    ds => new InlineRenameLocation(ds.Document, ds.TextSpan)));

                return new InlineRenameLocationSet(
                    _renameInfo, _document.Project.Solution,
                    references.ToImmutableArray());
            }

            public TextSpan? GetConflictEditSpan(InlineRenameLocation location, string triggerText, string replacementText, CancellationToken cancellationToken)
            {
                return location.TextSpan;
            }

            public string GetFinalSymbolName(string replacementText)
            {
                return replacementText;
            }

            public TextSpan GetReferenceEditSpan(InlineRenameLocation location, string triggerText, CancellationToken cancellationToken)
            {
                return location.TextSpan;
            }

            public bool TryOnAfterGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, string replacementText)
            {
                return true;
            }

            public bool TryOnBeforeGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, string replacementText)
            {
                return true;
            }

            private static Glyph FromSymbolKind(SymbolKind kind)
            {
                var glyph = Glyph.Error;

                switch (kind)
                {
                    case SymbolKind.Namespace:
                        glyph = Glyph.Namespace;
                        break;
                    case SymbolKind.NamedType:
                        glyph = Glyph.ClassPublic;
                        break;
                    case SymbolKind.Property:
                        glyph = Glyph.PropertyPublic;
                        break;
                    case SymbolKind.Event:
                        glyph = Glyph.EventPublic;
                        break;
                }

                return glyph;
            }

            private class InlineRenameLocationSet : IInlineRenameLocationSet
            {
                private readonly IXamlRenameInfo _renameInfo;
                private readonly Solution _oldSolution;

                public InlineRenameLocationSet(IXamlRenameInfo renameInfo, Solution solution, ImmutableArray<InlineRenameLocation> locations)
                {
                    _renameInfo = renameInfo;
                    _oldSolution = solution;
                    Locations = locations;
                }

                public IList<InlineRenameLocation> Locations { get; }

                public bool IsReplacementTextValid(string replacementText)
                {
                    return _renameInfo.IsReplacementTextValid(replacementText);
                }

                public async Task<IInlineRenameReplacementInfo> GetReplacementsAsync(string replacementText, OptionSet optionSet, CancellationToken cancellationToken)
                {
                    var newSolution = _oldSolution;
                    foreach (var group in Locations.GroupBy(l => l.Document))
                    {
                        var document = group.Key;
                        var oldSource = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                        var newSource = oldSource.WithChanges(group.Select(l => new TextChange(l.TextSpan, replacementText)));
                        newSolution = newSolution.WithDocumentText(document.Id, newSource);
                    }

                    return new InlineRenameReplacementInfo(this, newSolution, replacementText);
                }

                private class InlineRenameReplacementInfo : IInlineRenameReplacementInfo
                {
                    private readonly InlineRenameLocationSet _inlineRenameLocationSet;
                    private readonly string _replacementText;

                    public InlineRenameReplacementInfo(InlineRenameLocationSet inlineRenameLocationSet, Solution newSolution, string replacementText)
                    {
                        NewSolution = newSolution;
                        _inlineRenameLocationSet = inlineRenameLocationSet;
                        _replacementText = replacementText;
                    }

                    public Solution NewSolution { get; }

                    public IEnumerable<DocumentId> DocumentIds => _inlineRenameLocationSet.Locations.Select(l => l.Document.Id).Distinct();

                    public bool ReplacementTextValid => _inlineRenameLocationSet.IsReplacementTextValid(_replacementText);

                    public IEnumerable<InlineRenameReplacement> GetReplacements(DocumentId documentId)
                    {
                        yield break;
                    }
                }
            }
        }
    }
}
