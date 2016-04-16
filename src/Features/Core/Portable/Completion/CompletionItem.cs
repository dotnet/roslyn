// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion
{
    [DebuggerDisplay("{DisplayText}")]
    internal class CompletionItem : IComparable<CompletionItem>
    {
        internal AsyncLazy<ImmutableArray<SymbolDisplayPart>> LazyDescription;

        internal bool HasAsyncDescription { get; }

        /// <summary>
        /// An appropriate icon to present to the user for this completion item.
        /// </summary>
        public Glyph? Glyph { get; }

        /// <summary>
        /// The ICompletionProvider that this CompletionItem was created from.
        /// </summary>
        public virtual CompletionListProvider CompletionProvider { get; }

        /// <summary>
        /// The text for the completion item should be presented to the user (for example, in a
        /// completion list in an IDE).
        /// </summary>
        public string DisplayText { get; }

        /// <summary>
        /// Text to compare against when filtering completion items against user entered text.
        /// </summary>
        public string FilterText { get; }

        /// <summary>
        /// A string that is used for comparing completion items so that they can be ordered.  This
        /// is often the same as the DisplayText but may be different in certain circumstances.  For
        /// example, in C# a completion item with the display text "@int" might have the sort text
        /// "int" so that it would appear next to other items with similar names instead of
        /// appearing before, or after all the items due to the leading @ character.
        /// </summary>
        public string SortText { get; }

        /// <summary>
        /// Whether or not this item should be preselected when presented to the user.  It is up to
        /// the ICompletionRules to determine how this flag should be handled.  However, the default
        /// behavior is that, if there has been no filter text then a preselected item is preferred
        /// over any other item. If there has been filter text supplied, then a preselected item is
        /// preferred over another item if the ICompletionRules currently in effect deem them
        /// otherwise identical.
        /// </summary>
        public bool Preselect { get; }

        /// <summary>
        /// The span(respective to the original document text when this completion item was created)
        /// to use for determining what text should be used to filter this completion item against.
        /// Most commonly this is the same text span that is in TextChange, however in specialized
        /// cases it can be different.  For example, in C#, if a user types "foo." the item "operator
        /// int" may be placed in the list.  It's filter span will be created a after the dot
        /// position (so that typing "oper" will help filter down to the list of operators).
        /// However, the text change may extend further backward so that if that item is committed
        /// the resultant text becomes "((int)foo).
        /// </summary>
        public TextSpan FilterSpan { get; }

        /// <summary>
        /// A CompletionItem marked as a builder will be presented as an Intellisense Builder,
        /// initially with its display text, which will be replaced as the user types.
        /// </summary>
        public bool IsBuilder { get; }

        /// <summary>
        /// When this property is true, the completion list will display a warning icon to the
        /// right of the item's text, indicating that the corresponding symbol may not be
        /// available in every project a linked file is linked into.
        /// </summary>
        /// <returns></returns>
        public bool ShowsWarningIcon { get; }

        /// <summary>
        /// When this property is true, after performing the action associated with the item, 
        /// formatting is performed on the change
        /// </summary>
        public bool ShouldFormatOnCommit { get; internal set; }

        public CompletionItemRules Rules { get; }

        public ImmutableArray<CompletionItemFilter> Filters { get; }

        // Constructor kept for back compat.  When we move to our new completion API we can remove this.
        public CompletionItem(
            CompletionListProvider completionProvider,
            string displayText,
            TextSpan filterSpan,
            ImmutableArray<SymbolDisplayPart> description,
            Glyph? glyph,
            string sortText,
            string filterText,
            bool preselect,
            bool isBuilder,
            bool showsWarningIcon,
            bool shouldFormatOnCommit,
            CompletionItemRules rules)
            : this(completionProvider, displayText, filterSpan, description, glyph, sortText, 
                   filterText, preselect, isBuilder, showsWarningIcon, shouldFormatOnCommit, rules,
                   ImmutableArray<CompletionItemFilter>.Empty)
        {
        }

        public CompletionItem(
            CompletionListProvider completionProvider,
            string displayText,
            TextSpan filterSpan,
            ImmutableArray<SymbolDisplayPart> description = default(ImmutableArray<SymbolDisplayPart>),
            Glyph? glyph = null,
            string sortText = null,
            string filterText = null,
            bool preselect = false,
            bool isBuilder = false,
            bool showsWarningIcon = false,
            bool shouldFormatOnCommit = false,
            CompletionItemRules rules = null,
            ImmutableArray<CompletionItemFilter>? filters = null)
            : this(completionProvider, displayText, filterSpan,
                   description.IsDefault ? (Func<CancellationToken, Task<ImmutableArray<SymbolDisplayPart>>>)null : c => Task.FromResult(description),
                   glyph, /*hasAsyncDescription*/ false, sortText, filterText, preselect, isBuilder, showsWarningIcon, shouldFormatOnCommit, rules, filters)
        {
        }

        // Constructor kept for back compat.  When we move to our new completion API we can remove this.
        public CompletionItem(
            CompletionListProvider completionProvider,
            string displayText,
            TextSpan filterSpan,
            Func<CancellationToken, Task<ImmutableArray<SymbolDisplayPart>>> descriptionFactory,
            Glyph? glyph,
            string sortText,
            string filterText,
            bool preselect,
            bool isBuilder,
            bool showsWarningIcon,
            bool shouldFormatOnCommit,
            CompletionItemRules rules)
            : this(completionProvider, displayText, filterSpan, descriptionFactory, glyph, sortText,
                  filterText, preselect, isBuilder, showsWarningIcon, shouldFormatOnCommit, rules,
                  ImmutableArray<CompletionItemFilter>.Empty)
        {

        }

        public CompletionItem(
            CompletionListProvider completionProvider,
            string displayText,
            TextSpan filterSpan,
            Func<CancellationToken, Task<ImmutableArray<SymbolDisplayPart>>> descriptionFactory,
            Glyph? glyph,
            string sortText = null,
            string filterText = null,
            bool preselect = false,
            bool isBuilder = false,
            bool showsWarningIcon = false,
            bool shouldFormatOnCommit = false,
            CompletionItemRules rules = null,
            ImmutableArray<CompletionItemFilter>? filters = null)
                : this(completionProvider, displayText, filterSpan, descriptionFactory, glyph, /*hasAsyncDescription*/ true, sortText,
                     filterText, preselect, isBuilder, showsWarningIcon, shouldFormatOnCommit, rules, filters)
        {
        }

        private CompletionItem(
            CompletionListProvider completionProvider,
            string displayText,
            TextSpan filterSpan,
            Func<CancellationToken, Task<ImmutableArray<SymbolDisplayPart>>> descriptionFactory,
            Glyph? glyph,
            bool hasAsyncDescription,
            string sortText,
            string filterText,
            bool preselect,
            bool isBuilder,
            bool showsWarningIcon,
            bool shouldFormatOnCommit,
            CompletionItemRules rules,
            ImmutableArray<CompletionItemFilter>? filters)
        {
            this.CompletionProvider = completionProvider;
            this.DisplayText = displayText;
            this.Glyph = glyph;
            this.SortText = sortText ?? displayText;
            this.FilterText = filterText ?? displayText;
            this.Preselect = preselect;
            this.FilterSpan = filterSpan;
            this.IsBuilder = isBuilder;
            this.ShowsWarningIcon = showsWarningIcon;
            this.ShouldFormatOnCommit = shouldFormatOnCommit;
            this.HasAsyncDescription = hasAsyncDescription;
            this.Rules = rules ?? CompletionItemRules.DefaultRules;
            this.Filters = filters ?? ImmutableArray<CompletionItemFilter>.Empty;

            if (descriptionFactory != null)
            {
                this.LazyDescription = new AsyncLazy<ImmutableArray<SymbolDisplayPart>>(descriptionFactory, cacheResult: true);
            }
        }

        /// <summary>
        /// A description to present to the user for this completion item.
        /// </summary>
        public virtual Task<ImmutableArray<SymbolDisplayPart>> GetDescriptionAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.LazyDescription == null
                ? SpecializedTasks.EmptyImmutableArray<SymbolDisplayPart>()
                : this.LazyDescription.GetValueAsync(cancellationToken);
        }

        int IComparable<CompletionItem>.CompareTo(CompletionItem other)
        {
            var result = StringComparer.OrdinalIgnoreCase.Compare(this.SortText, other.SortText);
            if (result == 0)
            {
                result = StringComparer.OrdinalIgnoreCase.Compare(this.DisplayText, other.DisplayText);
            }

            return result;
        }

        public override string ToString()
        {
            return DisplayText;
        }
    }
}
