// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SignatureHelp
{
    internal class SignatureHelpItem
    {
        /// <summary>
        /// True if this signature help item can have an unbounded number of arguments passed to it.
        /// If it is variadic then the last parameter will be considered selected, even if the
        /// selected parameter index strictly goes past the number of defined parameters for this
        /// item.
        /// </summary>
        public bool IsVariadic { get; }

        public ImmutableArray<TaggedText> PrefixDisplayParts { get; }
        public ImmutableArray<TaggedText> SuffixDisplayParts { get; }

        // TODO: This probably won't be sufficient for VB query signature help.  It has
        // arbitrary separators between parameters.
        public ImmutableArray<TaggedText> SeparatorDisplayParts { get; }

        public ImmutableArray<SignatureHelpParameter> Parameters { get; }

        public ImmutableArray<TaggedText> DescriptionParts { get; internal set; }

        public Func<CancellationToken, IEnumerable<TaggedText>> DocumentationFactory { get; }

        private static readonly Func<CancellationToken, IEnumerable<TaggedText>> s_emptyDocumentationFactory =
            _ => SpecializedCollections.EmptyEnumerable<TaggedText>();

        public SignatureHelpItem(
            bool isVariadic,
            Func<CancellationToken, IEnumerable<TaggedText>> documentationFactory,
            IEnumerable<TaggedText> prefixParts,
            IEnumerable<TaggedText> separatorParts,
            IEnumerable<TaggedText> suffixParts,
            IEnumerable<SignatureHelpParameter> parameters,
            IEnumerable<TaggedText> descriptionParts)
        {
            if (isVariadic && !parameters.Any())
            {
                throw new ArgumentException(FeaturesResources.Variadic_SignatureHelpItem_must_have_at_least_one_parameter);
            }

            IsVariadic = isVariadic;
            DocumentationFactory = documentationFactory ?? s_emptyDocumentationFactory;
            PrefixDisplayParts = prefixParts.ToImmutableArrayOrEmpty();
            SeparatorDisplayParts = separatorParts.ToImmutableArrayOrEmpty();
            SuffixDisplayParts = suffixParts.ToImmutableArrayOrEmpty();
            Parameters = parameters.ToImmutableArrayOrEmpty();
            DescriptionParts = descriptionParts.ToImmutableArrayOrEmpty();
        }

        // Constructor kept for back compat
        public SignatureHelpItem(
            bool isVariadic,
            Func<CancellationToken, IEnumerable<SymbolDisplayPart>> documentationFactory,
            IEnumerable<SymbolDisplayPart> prefixParts,
            IEnumerable<SymbolDisplayPart> separatorParts,
            IEnumerable<SymbolDisplayPart> suffixParts,
            IEnumerable<SignatureHelpParameter> parameters,
            IEnumerable<SymbolDisplayPart> descriptionParts)
            : this(isVariadic,
                  documentationFactory != null
                    ? c => documentationFactory(c).ToTaggedText()
                    : s_emptyDocumentationFactory,
                  prefixParts.ToTaggedText(),
                  separatorParts.ToTaggedText(),
                  suffixParts.ToTaggedText(),
                  parameters,
                  descriptionParts.ToTaggedText())
        {
        }

        internal IEnumerable<TaggedText> GetAllParts()
        {
            return
                PrefixDisplayParts.Concat(
                SeparatorDisplayParts.Concat(
                SuffixDisplayParts.Concat(
                Parameters.SelectMany(p => p.GetAllParts())).Concat(
                DescriptionParts)));
        }

        public override string ToString()
        {
            var prefix = string.Concat(PrefixDisplayParts);
            var suffix = string.Concat(SuffixDisplayParts);
            var parameters = string.Join(string.Concat(SeparatorDisplayParts), Parameters);
            var description = string.Concat(DescriptionParts);
            return string.Concat(prefix, parameters, suffix, description);
        }
    }
}
