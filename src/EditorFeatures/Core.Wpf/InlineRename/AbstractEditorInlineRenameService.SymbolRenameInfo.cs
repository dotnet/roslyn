// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell.Interop;
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

            private readonly object _gate = new object();

            private readonly Document _document;
            private readonly IEnumerable<IRefactorNotifyService> _refactorNotifyServices;

            private Task<RenameLocations> _underlyingFindRenameLocationsTask;

            /// <summary>
            /// Whether or not we shortened the trigger span (say because we were renaming an attribute,
            /// and we didn't select the 'Attribute' portion of the name.
            /// </summary>
            private readonly bool _shortenedTriggerSpan;
            private readonly bool _isRenamingAttributePrefix;

            public bool CanRename { get; }
            public string LocalizedErrorMessage { get; }
            public TextSpan TriggerSpan { get; }
            public SymbolAndProjectId RenameSymbolAndProjectId { get; }
            public bool HasOverloads { get; }
            public bool ForceRenameOverloads { get; }

            public ISymbol RenameSymbol => RenameSymbolAndProjectId.Symbol;

            public SymbolInlineRenameInfo(
                IEnumerable<IRefactorNotifyService> refactorNotifyServices,
                Document document,
                TextSpan triggerSpan,
                SymbolAndProjectId renameSymbolAndProjectId,
                bool forceRenameOverloads,
                CancellationToken cancellationToken)
            {
                this.CanRename = true;

                _refactorNotifyServices = refactorNotifyServices;
                _document = document;
                this.RenameSymbolAndProjectId = renameSymbolAndProjectId;

                this.HasOverloads = RenameLocations.GetOverloadedSymbols(this.RenameSymbolAndProjectId).Any();
                this.ForceRenameOverloads = forceRenameOverloads;

                _isRenamingAttributePrefix = CanRenameAttributePrefix(document, triggerSpan, cancellationToken);
                this.TriggerSpan = GetReferenceEditSpan(new InlineRenameLocation(document, triggerSpan), cancellationToken);

                _shortenedTriggerSpan = this.TriggerSpan != triggerSpan;
            }

            private bool CanRenameAttributePrefix(Document document, TextSpan triggerSpan, CancellationToken cancellationToken)
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
                var nameWithoutAttribute = GetWithoutAttributeSuffix(this.RenameSymbol.Name);
                var triggerText = GetSpanText(document, triggerSpan, cancellationToken);

                return triggerText.StartsWith(triggerText); // TODO: Always true? What was it supposed to do?
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
            public TextSpan GetReferenceEditSpan(InlineRenameLocation location, CancellationToken cancellationToken)
            {
                var searchName = this.RenameSymbol.Name;
                if (_isRenamingAttributePrefix)
                {
                    // We're only renaming the attribute prefix part.  We want to adjust the span of 
                    // the reference we've found to only update the prefix portion.
                    searchName = GetWithoutAttributeSuffix(this.RenameSymbol.Name);
                }

                var spanText = GetSpanText(location.Document, location.TextSpan, cancellationToken);
                var index = spanText.LastIndexOf(searchName, StringComparison.Ordinal);

                if (index < 0)
                {
                    // Couldn't even find the search text at this reference location.  This might happen
                    // if the user used things like unicode escapes.  IN that case, we'll have to rename
                    // the entire identifier.
                    return location.TextSpan;
                }

                return new TextSpan(location.TextSpan.Start + index, searchName.Length);
            }

            public TextSpan? GetConflictEditSpan(InlineRenameLocation location, string replacementText, CancellationToken cancellationToken)
            {
                var spanText = GetSpanText(location.Document, location.TextSpan, cancellationToken);
                var position = spanText.LastIndexOf(replacementText, StringComparison.Ordinal);

                if (_isRenamingAttributePrefix)
                {
                    // We're only renaming the attribute prefix part.  We want to adjust the span of 
                    // the reference we've found to only update the prefix portion.
                    var index = spanText.LastIndexOf(replacementText + AttributeSuffix, StringComparison.Ordinal);
                    position = index >= 0 ? index : position;
                }

                if (position < 0)
                {
                    return null;
                }

                return new TextSpan(location.TextSpan.Start + position, replacementText.Length);
            }

            private string GetWithoutAttributeSuffix(string value)
                => value.GetWithoutAttributeSuffix(isCaseSensitive: _document.GetLanguageService<ISyntaxFactsService>().IsCaseSensitive);

            private bool HasAttributeSuffix(string value)
                => value.TryGetWithoutAttributeSuffix(isCaseSensitive: _document.GetLanguageService<ISyntaxFactsService>().IsCaseSensitive, result: out var _);

            private static string GetSpanText(Document document, TextSpan triggerSpan, CancellationToken cancellationToken)
            {
                var sourceText = document.GetTextAsync(cancellationToken).WaitAndGetResult(cancellationToken);
                var triggerText = sourceText.ToString(triggerSpan);
                return triggerText;
            }

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

            public Task<IInlineRenameLocationSet> FindRenameLocationsAsync(OptionSet optionSet, CancellationToken cancellationToken)
            {
                Task<RenameLocations> renameTask;
                lock (_gate)
                {
                    if (_underlyingFindRenameLocationsTask == null)
                    {
                        // If this is the first call, then just start finding the initial set of rename
                        // locations.
                        _underlyingFindRenameLocationsTask = RenameLocations.FindAsync(
                            this.RenameSymbolAndProjectId, _document.Project.Solution, optionSet, cancellationToken);
                        renameTask = _underlyingFindRenameLocationsTask;

                        // null out the option set.  We don't need it anymore, and this will ensure
                        // we don't call FindWithUpdatedOptionsAsync below.
                        optionSet = null;
                    }
                    else
                    {
                        // We already have a task to figure out the set of rename locations.  Let it
                        // finish, then ask it to get the rename locations with the updated options.
                        renameTask = _underlyingFindRenameLocationsTask;
                    }
                }

                return GetLocationSet(renameTask, optionSet, cancellationToken);
            }

            private async Task<IInlineRenameLocationSet> GetLocationSet(Task<RenameLocations> renameTask, OptionSet optionSet, CancellationToken cancellationToken)
            {
                var locationSet = await renameTask.ConfigureAwait(false);
                if (optionSet != null)
                {
                    locationSet = await locationSet.FindWithUpdatedOptionsAsync(optionSet, cancellationToken).ConfigureAwait(false);
                }

                return new InlineRenameLocationSet(this, locationSet);
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
                if (RenameSymbol.Kind == SymbolKind.NamedType)
                {
                    if (RenameSymbol.Locations.Length > 1)
                    {
                        return InlineRenameFileRenameInfo.TypeWithMultipleLocations;
                    }

                    if (OriginalNameMatches(_document, RenameSymbol.Name))
                    {
                        return InlineRenameFileRenameInfo.Allowed;
                    }

                    return InlineRenameFileRenameInfo.TypeDoesNotMatchFileName;
                }

                return InlineRenameFileRenameInfo.NotAllowed;

                // Local Functions

                static bool OriginalNameMatches(Document document, string name)
                    => Path.GetFileNameWithoutExtension(document.Name)
                        .Equals(name, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
