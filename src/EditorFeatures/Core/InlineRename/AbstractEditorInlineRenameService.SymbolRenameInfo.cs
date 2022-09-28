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
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal abstract partial class AbstractEditorInlineRenameService
    {
        /// <summary>
        /// Represents information about the ability to rename a particular location.
        /// </summary>
        private partial class SymbolInlineRenameInfo : IInlineRenameInfoWithFileRename
        {
            private const string AttributeSuffix = "Attribute";

            private readonly Document _document;
            private readonly CodeCleanupOptionsProvider _fallbackOptions;
            private readonly IEnumerable<IRefactorNotifyService> _refactorNotifyServices;

            /// <summary>
            /// Whether or not we shortened the trigger span (say because we were renaming an attribute,
            /// and we didn't select the 'Attribute' portion of the name).
            /// </summary>
            private readonly bool _isRenamingAttributePrefix;

            public bool CanRename { get; }
            public string? LocalizedErrorMessage { get; }
            public TextSpan TriggerSpan { get; }
            public bool HasOverloads { get; }
            public bool MustRenameOverloads { get; }

            /// <summary>
            /// The locations of the potential rename candidates for the symbol.
            /// </summary>
            public ImmutableArray<DocumentSpan> DefinitionLocations { get; }

            public ISymbol RenameSymbol { get; }

            public SymbolInlineRenameInfo(
                IEnumerable<IRefactorNotifyService> refactorNotifyServices,
                Document document,
                TextSpan triggerSpan,
                string triggerText,
                ISymbol renameSymbol,
                bool forceRenameOverloads,
                ImmutableArray<DocumentSpan> definitionLocations,
                CodeCleanupOptionsProvider fallbackOptions,
                CancellationToken cancellationToken)
            {
                this.CanRename = true;

                _refactorNotifyServices = refactorNotifyServices;
                _document = document;
                _fallbackOptions = fallbackOptions;
                this.RenameSymbol = renameSymbol;

                this.HasOverloads = RenameLocations.GetOverloadedSymbols(this.RenameSymbol).Any();
                this.MustRenameOverloads = forceRenameOverloads;

                _isRenamingAttributePrefix = CanRenameAttributePrefix(triggerText);
                this.TriggerSpan = GetReferenceEditSpan(new InlineRenameLocation(document, triggerSpan), triggerText, cancellationToken);

                this.DefinitionLocations = definitionLocations;
            }

            private bool CanRenameAttributePrefix(string triggerText)
            {
                // if this isn't an attribute, or it doesn't have the 'Attribute' suffix, then clearly
                // we can't rename just the attribute prefix.
                if (!this.IsRenamingAttributeTypeWithAttributeSuffix())
                {
                    return false;
                }

                // Ok, the symbol is good.  Now, make sure that the trigger text starts with the prefix
                // of the attribute.  If it does, then we can rename just the attribute prefix (otherwise
                // we need to rename the entire attribute).
#pragma warning disable IDE0059 // Unnecessary assignment of a value - https://github.com/dotnet/roslyn/issues/45895
                var nameWithoutAttribute = GetWithoutAttributeSuffix(this.RenameSymbol.Name);

                return triggerText.StartsWith(triggerText); // TODO: Always true? What was it supposed to do?
#pragma warning restore IDE0059 // Unnecessary assignment of a value
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
                if (_isRenamingAttributePrefix)
                {
                    // We're only renaming the attribute prefix part.  We want to adjust the span of
                    // the reference we've found to only update the prefix portion.
                    searchName = GetWithoutAttributeSuffix(this.RenameSymbol.Name);
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

                if (_isRenamingAttributePrefix)
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

            private string GetWithoutAttributeSuffix(string value)
                => value.GetWithoutAttributeSuffix(isCaseSensitive: _document.GetRequiredLanguageService<ISyntaxFactsService>().IsCaseSensitive)!;

            private bool HasAttributeSuffix(string value)
                => value.TryGetWithoutAttributeSuffix(isCaseSensitive: _document.GetRequiredLanguageService<ISyntaxFactsService>().IsCaseSensitive, result: out var _);

            internal bool IsRenamingAttributeTypeWithAttributeSuffix()
            {
                if (this.RenameSymbol.IsAttribute() || (this.RenameSymbol.Kind == SymbolKind.Alias && ((IAliasSymbol)this.RenameSymbol).Target.IsAttribute()))
                {
                    if (HasAttributeSuffix(this.RenameSymbol.Name))
                    {
                        return true;
                    }
                }

                return false;
            }

            public string DisplayName => RenameSymbol.Name;
            public string FullDisplayName => RenameSymbol.ToDisplayString();
            public Glyph Glyph => RenameSymbol.GetGlyph();

            public string GetFinalSymbolName(string replacementText)
            {
                if (_isRenamingAttributePrefix && !HasAttributeSuffix(replacementText))
                {
                    return replacementText + AttributeSuffix;
                }

                return replacementText;
            }

            public async Task<IInlineRenameLocationSet> FindRenameLocationsAsync(SymbolRenameOptions options, CancellationToken cancellationToken)
            {
                var solution = _document.Project.Solution;
                var locations = await Renamer.FindRenameLocationsAsync(
                    solution, this.RenameSymbol, options, _fallbackOptions, cancellationToken).ConfigureAwait(false);

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
                    _document.Project.Solution.Workspace.CanApplyChange(ApplyChangesKind.ChangeDocumentInfo))
                {
                    if (RenameSymbol.Locations.Length > 1)
                    {
                        return InlineRenameFileRenameInfo.TypeWithMultipleLocations;
                    }

                    // Get the document that the symbol is defined in to compare
                    // the name with the symbol name. If they match allow
                    // rename file rename as part of the symbol rename
                    var symbolSourceDocument = _document.Project.Solution.GetDocument(RenameSymbol.Locations.Single().SourceTree);
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
}
