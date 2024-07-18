// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

internal abstract partial class AbstractEditorInlineRenameService
{
    /// <summary>
    /// Represents information about the ability to rename a particular location.
    /// </summary>
    private partial class SymbolInlineRenameInfo : IInlineRenameInfo
    {
        private const string AttributeSuffix = "Attribute";

        private readonly SymbolicRenameInfo _info;

        private Document Document => _info.Document!;
        private readonly IEnumerable<IRefactorNotifyService> _refactorNotifyServices;

        /// <summary>
        /// Whether or not we shortened the trigger span (say because we were renaming an attribute,
        /// and we didn't select the 'Attribute' portion of the name).
        /// </summary>
        private bool IsRenamingAttributePrefix => _info.IsRenamingAttributePrefix;

        public bool CanRename { get; }
        public string? LocalizedErrorMessage => null;
        public TextSpan TriggerSpan { get; }
        public bool HasOverloads { get; }
        public bool MustRenameOverloads => _info.ForceRenameOverloads;

        /// <summary>
        /// The locations of the potential rename candidates for the symbol.
        /// </summary>
        public ImmutableArray<DocumentSpan> DefinitionLocations => _info.DocumentSpans;

        public ISymbol RenameSymbol => _info.Symbol!;

        public SymbolInlineRenameInfo(
            IEnumerable<IRefactorNotifyService> refactorNotifyServices,
            SymbolicRenameInfo info,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfTrue(info.IsError);
            this.CanRename = true;

            _info = info;
            _refactorNotifyServices = refactorNotifyServices;

            this.HasOverloads = RenameUtilities.GetOverloadedSymbols(this.RenameSymbol).Any();

            this.TriggerSpan = GetReferenceEditSpan(new InlineRenameLocation(this.Document, info.TriggerToken.Span), info.TriggerText, cancellationToken);
        }

        /// <summary>
        /// Given a span of text, we need to return the subspan that is editable and
        /// contains the current replacementText.
        ///
        /// These cases are currently handled:
        ///     - Escaped identifiers                          [goo] => goo
        ///     - Type suffixes in VB                          goo$ => goo
        ///     - Qualified names from complexification        A.goo => goo
        ///     - Optional Attribute suffixes                  XAttribute => X
        ///         Careful here:                              XAttribute => XAttribute if renamesymbol is XAttributeAttribute
        ///     - Compiler-generated EventHandler suffix       XEventHandler => X
        ///     - Compiler-generated get_ and set_ prefixes    get_X => X
        /// </summary>
        public TextSpan GetReferenceEditSpan(InlineRenameLocation location, string triggerText, CancellationToken cancellationToken)
        {
            var searchName = this.RenameSymbol.Name;
            if (this.IsRenamingAttributePrefix)
            {
                // We're only renaming the attribute prefix part.  We want to adjust the span of
                // the reference we've found to only update the prefix portion.
                searchName = _info.GetWithoutAttributeSuffix(this.RenameSymbol.Name);
            }

            var index = triggerText.LastIndexOf(searchName, StringComparison.Ordinal);
            if (index < 0)
            {
                // Couldn't even find the search text at this reference location.  This might happen
                // if the user used things like unicode escapes.  IN that case, we'll have to rename
                // the entire identifier.
                return location.TextSpan;
            }

            return new TextSpan(location.TextSpan.Start + index, searchName.Length);
        }

        public TextSpan? GetConflictEditSpan(InlineRenameLocation location, string triggerText, string replacementText, CancellationToken cancellationToken)
        {
            var position = triggerText.LastIndexOf(replacementText, StringComparison.Ordinal);

            if (this.IsRenamingAttributePrefix)
            {
                // We're only renaming the attribute prefix part.  We want to adjust the span of
                // the reference we've found to only update the prefix portion.
                var index = triggerText.LastIndexOf(replacementText + AttributeSuffix, StringComparison.Ordinal);
                position = index >= 0 ? index : position;
            }

            if (position < 0)
            {
                return null;
            }

            return new TextSpan(location.TextSpan.Start + position, replacementText.Length);
        }

        public string DisplayName => RenameSymbol.Name;
        public string FullDisplayName => RenameSymbol.ToDisplayString();
        public Glyph Glyph => RenameSymbol.GetGlyph();

        public string GetFinalSymbolName(string replacementText)
            => _info.GetFinalSymbolName(replacementText);

        public async Task<IInlineRenameLocationSet> FindRenameLocationsAsync(SymbolRenameOptions options, CancellationToken cancellationToken)
        {
            var solution = this.Document.Project.Solution;
            var locations = await Renamer.FindRenameLocationsAsync(
                solution, this.RenameSymbol, options, cancellationToken).ConfigureAwait(false);

            return new InlineRenameLocationSet(this, locations);
        }

        public bool TryOnBeforeGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, string replacementText)
        {
            return _refactorNotifyServices.TryOnBeforeGlobalSymbolRenamed(workspace, changedDocumentIDs, RenameSymbol,
                this.GetFinalSymbolName(replacementText), throwOnFailure: false);
        }

        public bool TryOnAfterGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, string replacementText)
        {
            return _refactorNotifyServices.TryOnAfterGlobalSymbolRenamed(workspace, changedDocumentIDs, RenameSymbol,
                this.GetFinalSymbolName(replacementText), throwOnFailure: false);
        }

        public InlineRenameFileRenameInfo GetFileRenameInfo()
        {
            if (RenameSymbol.Kind == SymbolKind.NamedType &&
                this.Document.Project.Solution.CanApplyChange(ApplyChangesKind.ChangeDocumentInfo))
            {
                if (RenameSymbol.Locations.Length > 1)
                {
                    return InlineRenameFileRenameInfo.TypeWithMultipleLocations;
                }

                // Get the document that the symbol is defined in to compare
                // the name with the symbol name. If they match allow
                // rename file rename as part of the symbol rename
                var symbolSourceDocument = this.Document.Project.Solution.GetDocument(RenameSymbol.Locations.Single().SourceTree);
                if (symbolSourceDocument != null && WorkspacePathUtilities.TypeNameMatchesDocumentName(symbolSourceDocument, RenameSymbol.Name))
                {
                    return InlineRenameFileRenameInfo.Allowed;
                }

                return InlineRenameFileRenameInfo.TypeDoesNotMatchFileName;
            }

            return InlineRenameFileRenameInfo.NotAllowed;
        }
    }
}
