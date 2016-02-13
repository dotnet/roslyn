// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal abstract partial class AbstractEditorInlineRenameService
    {
        /// <summary>
        /// Represents information about the ability to rename a particular location.
        /// </summary>
        private partial class SymbolInlineRenameInfo : IInlineRenameInfo
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
            public ISymbol RenameSymbol { get; }
            public bool HasOverloads { get; }
            public bool ForceRenameOverloads { get; }

            public SymbolInlineRenameInfo(
                IEnumerable<IRefactorNotifyService> refactorNotifyServices,
                Document document,
                TextSpan triggerSpan,
                ISymbol renameSymbol,
                bool forceRenameOverloads,
                CancellationToken cancellationToken)
            {
                this.CanRename = true;

                _refactorNotifyServices = refactorNotifyServices;
                _document = document;
                this.RenameSymbol = renameSymbol;

                this.HasOverloads = RenameLocations.GetOverloadedSymbols(this.RenameSymbol).Any();
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
                var nameWithoutAttribute = this.RenameSymbol.Name.GetWithoutAttributeSuffix(isCaseSensitive: true);
                var triggerText = GetSpanText(document, triggerSpan, cancellationToken);

                return triggerText.StartsWith(triggerText); // TODO: Always true? What was it supposed to do?
            }

            /// <summary>
            /// Given a span of text, we need to return the subspan that is editable and
            /// contains the current replacementText.
            /// 
            /// These cases are currently handled:
            ///     - Escaped identifiers                          [foo] => foo
            ///     - Type suffixes in VB                          foo$ => foo
            ///     - Qualified names from complexification        A.foo => foo
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

            private static string GetSpanText(Document document, TextSpan triggerSpan, CancellationToken cancellationToken)
            {
                var sourceText = document.GetTextAsync(cancellationToken).WaitAndGetResult(cancellationToken);
                var triggerText = sourceText.ToString(triggerSpan);
                return triggerText;
            }

            private static string GetWithoutAttributeSuffix(string value)
            {
                return value.GetWithoutAttributeSuffix(isCaseSensitive: true);
            }

            internal bool IsRenamingAttributeTypeWithAttributeSuffix()
            {
                if (this.RenameSymbol.IsAttribute() || (this.RenameSymbol.Kind == SymbolKind.Alias && ((IAliasSymbol)this.RenameSymbol).Target.IsAttribute()))
                {
                    var name = this.RenameSymbol.Name;
                    if (name.TryGetWithoutAttributeSuffix(isCaseSensitive: true, result: out name))
                    {
                        return true;
                    }
                }

                return false;
            }

            public string DisplayName
            {
                get
                {
                    return this.RenameSymbol.Name;
                }
            }

            public string FullDisplayName
            {
                get
                {
                    return this.RenameSymbol.ToDisplayString();
                }
            }

            public Glyph Glyph
            {
                get
                {
                    return this.RenameSymbol.GetGlyph();
                }
            }

            public string GetFinalSymbolName(string replacementText)
            {
                if (_isRenamingAttributePrefix)
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
                            this.RenameSymbol, _document.Project.Solution, optionSet, cancellationToken);
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
        }
    }
}
