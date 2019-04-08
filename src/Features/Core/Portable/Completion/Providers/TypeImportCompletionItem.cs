// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal class TypeImportCompletionItem : CompletionItem
    {
        public int TypeArity { get; }

        public int OverloadCount { get; }

        public string ContainingNamespace { get; }

        private string _metadataName = null;
        public string MetadataName
        {
            get
            {
                if (_metadataName == null)
                {
                    _metadataName = ComposeAritySuffixedMetadataName(
                       GetFullyQualifiedName(this.ContainingNamespace, this.DisplayText),
                       this.TypeArity);
                }

                return _metadataName;
            }
        }

        internal override bool UseEditorCompletionItemCache => true;

        public static TypeImportCompletionItem Create(INamedTypeSymbol typeSymbol, string containingNamespace, int overloadCount)
        {
            // TODO: Suffix should be language specific, i.e. `(Of ...)` if triggered from VB.
            return new TypeImportCompletionItem(
                 displayText: typeSymbol.Name,
                 filterText: typeSymbol.Name,
                 sortText: typeSymbol.Name,
                 span: default,
                 properties: null,
                 tags: GlyphTags.GetTags(typeSymbol.GetGlyph()),
                 rules: CompletionItemRules.Default,
                 displayTextPrefix: null,
                 displayTextSuffix: typeSymbol.Arity == 0 ? null : "<>",
                 inlineDescription: containingNamespace,
                 typeArity: typeSymbol.Arity,
                 containingNamespace: containingNamespace,
                 overloadCount: overloadCount);
        }

        protected override CompletionItem With(
            Optional<TextSpan> span = default,
            Optional<string> displayText = default,
            Optional<string> filterText = default,
            Optional<string> sortText = default,
            Optional<ImmutableDictionary<string, string>> properties = default,
            Optional<ImmutableArray<string>> tags = default,
            Optional<CompletionItemRules> rules = default,
            Optional<string> displayTextPrefix = default,
            Optional<string> displayTextSuffix = default,
            Optional<string> inlineDescription = default)
        {
            var newSpan = span.HasValue ? span.Value : this.Span;
            var newDisplayText = displayText.HasValue ? displayText.Value : this.DisplayText;
            var newFilterText = filterText.HasValue ? filterText.Value : this.FilterText;
            var newSortText = sortText.HasValue ? sortText.Value : this.SortText;
            var newInlineDescription = inlineDescription.HasValue ? inlineDescription.Value : this.InlineDescription;
            var newProperties = properties.HasValue ? properties.Value : this.Properties;
            var newTags = tags.HasValue ? tags.Value : this.Tags;
            var newRules = rules.HasValue ? rules.Value : this.Rules;
            var newDisplayTextPrefix = displayTextPrefix.HasValue ? displayTextPrefix.Value : this.DisplayTextPrefix;
            var newDisplayTextSuffix = displayTextSuffix.HasValue ? displayTextSuffix.Value : this.DisplayTextSuffix;

            if (newSpan == this.Span &&
                newDisplayText == this.DisplayText &&
                newFilterText == this.FilterText &&
                newSortText == this.SortText &&
                newProperties == this.Properties &&
                newTags == this.Tags &&
                newRules == this.Rules &&
                newDisplayTextPrefix == this.DisplayTextPrefix &&
                newDisplayTextSuffix == this.DisplayTextSuffix &&
                newInlineDescription == this.InlineDescription)
            {
                return this;
            }

            return new TypeImportCompletionItem(
                displayText: newDisplayText,
                filterText: newFilterText,
                span: newSpan,
                sortText: newSortText,
                properties: newProperties,
                tags: newTags,
                rules: newRules,
                displayTextPrefix: newDisplayTextPrefix,
                displayTextSuffix: newDisplayTextSuffix,
                inlineDescription: newInlineDescription,
                typeArity: this.TypeArity,
                containingNamespace: this.ContainingNamespace,
                overloadCount: this.OverloadCount);
        }

        private TypeImportCompletionItem(
            string displayText,
            string filterText,
            string sortText,
            TextSpan span,
            ImmutableDictionary<string, string> properties,
            ImmutableArray<string> tags,
            CompletionItemRules rules,
            string displayTextPrefix,
            string displayTextSuffix,
            string inlineDescription,
            int typeArity,
            string containingNamespace,
            int overloadCount)
            : base(displayText: displayText, filterText: filterText, sortText: sortText, span: span,
                   properties: properties, tags: tags, rules: rules, displayTextPrefix: displayTextPrefix,
                   displayTextSuffix: displayTextSuffix, inlineDescription: inlineDescription)
        {
            TypeArity = typeArity;
            ContainingNamespace = containingNamespace;
            OverloadCount = overloadCount;
        }

        private const string GenericTypeNameManglingString = "`";
        private static readonly string[] s_aritySuffixesOneToNine = { "`1", "`2", "`3", "`4", "`5", "`6", "`7", "`8", "`9" };

        private static string GetAritySuffix(int arity)
        {
            Debug.Assert(arity > 0);
            return (arity <= s_aritySuffixesOneToNine.Length)
                ? s_aritySuffixesOneToNine[arity - 1]
                : string.Concat(GenericTypeNameManglingString, arity.ToString(CultureInfo.InvariantCulture));
        }

        private static string GetFullyQualifiedName(string namespaceName, string typeName)
            => namespaceName.Length == 0 ? typeName : namespaceName + "." + typeName;

        private static string ComposeAritySuffixedMetadataName(string name, int arity)
            => arity == 0 ? name : name + GetAritySuffix(arity);
    }
}
