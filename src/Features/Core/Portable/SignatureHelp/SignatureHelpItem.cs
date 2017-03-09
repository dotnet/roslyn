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
    internal sealed class SignatureHelpItem : IEquatable<SignatureHelpItem>
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

        public ImmutableArray<TaggedText> DescriptionParts { get; }

        public ImmutableDictionary<string, string> Properties { get; }

        private SignatureHelpItem(
            bool isVariadic,
            ImmutableArray<TaggedText> prefixParts,
            ImmutableArray<TaggedText> separatorParts,
            ImmutableArray<TaggedText> suffixParts,
            ImmutableArray<SignatureHelpParameter> parameters,
            ImmutableArray<TaggedText> descriptionParts,
            ImmutableDictionary<string, string> properties)
        {
            if (isVariadic && !parameters.Any())
            {
                throw new ArgumentException(FeaturesResources.Variadic_SignatureHelpItem_must_have_at_least_one_parameter);
            }

            this.IsVariadic = isVariadic;
            this.PrefixDisplayParts = prefixParts.IsDefault ? ImmutableArray<TaggedText>.Empty : prefixParts;
            this.SeparatorDisplayParts = separatorParts.IsDefault ? ImmutableArray<TaggedText>.Empty : separatorParts;
            this.SuffixDisplayParts = suffixParts.IsDefault ? ImmutableArray<TaggedText>.Empty : suffixParts;
            this.Parameters = parameters.IsDefault ? ImmutableArray<SignatureHelpParameter>.Empty : parameters;
            this.DescriptionParts = descriptionParts.IsDefault ? ImmutableArray<TaggedText>.Empty : descriptionParts;
            this.Properties = properties ?? ImmutableDictionary<string, string>.Empty;
        }

        public static SignatureHelpItem Create(
            bool isVariadic,
            ImmutableArray<TaggedText> prefixParts,
            ImmutableArray<TaggedText> separatorParts,
            ImmutableArray<TaggedText> suffixParts,
            ImmutableArray<SignatureHelpParameter> parameters,
            ImmutableArray<TaggedText> descriptionParts,
            ImmutableDictionary<string, string> properties = null)
        {
            return new SignatureHelpItem(isVariadic, prefixParts, separatorParts, suffixParts, parameters, descriptionParts, properties);
        }

        private SignatureHelpItem With(
            Optional<bool> isVariadic = default(Optional<bool>),
            Optional<ImmutableArray<TaggedText>> prefixParts = default(Optional<ImmutableArray<TaggedText>>),
            Optional<ImmutableArray<TaggedText>> separatorParts = default(Optional<ImmutableArray<TaggedText>>),
            Optional<ImmutableArray<TaggedText>> suffixParts = default(Optional<ImmutableArray<TaggedText>>),
            Optional<ImmutableArray<SignatureHelpParameter>> parameters = default(Optional<ImmutableArray<SignatureHelpParameter>>),
            Optional<ImmutableArray<TaggedText>> descriptionParts = default(Optional<ImmutableArray<TaggedText>>),
            Optional<ImmutableDictionary<string, string>> properties = default(Optional<ImmutableDictionary<string, string>>))
        {
            var newIsVariadic = isVariadic.HasValue ? isVariadic.Value : this.IsVariadic;
            var newPrefixParts = prefixParts.HasValue ? prefixParts.Value : this.PrefixDisplayParts;
            var newSeparatorParts = separatorParts.HasValue ? separatorParts.Value : this.SeparatorDisplayParts;
            var newSuffixParts = suffixParts.HasValue ? suffixParts.Value : this.SuffixDisplayParts;
            var newParameters = parameters.HasValue ? parameters.Value : this.Parameters;
            var newDescriptionParts = descriptionParts.HasValue ? descriptionParts.Value : this.DescriptionParts;
            var newProperties = properties.HasValue ? properties.Value : this.Properties;

            if (newIsVariadic != this.IsVariadic
                || newPrefixParts != this.PrefixDisplayParts
                || newSeparatorParts != this.SeparatorDisplayParts
                || newSuffixParts != this.SuffixDisplayParts
                || newParameters != this.Parameters
                || newDescriptionParts != this.DescriptionParts
                || newProperties != this.Properties)
            {
                return Create(newIsVariadic, newPrefixParts, newSeparatorParts, newSuffixParts, newParameters, newDescriptionParts, newProperties);
            }
            else
            {
                return this;
            }
        }

        public SignatureHelpItem WithIsVariadic(bool isVariadic)
        {
            return With(isVariadic: isVariadic);
        }

        public SignatureHelpItem WithPrefixParts(ImmutableArray<TaggedText> prefixParts)
        {
            return With(prefixParts: prefixParts);
        }

        public SignatureHelpItem WithSeparatorParts(ImmutableArray<TaggedText> separatorParts)
        {
            return With(separatorParts: separatorParts);
        }

        public SignatureHelpItem WithSuffixParts(ImmutableArray<TaggedText> suffixParts)
        {
            return With(suffixParts: suffixParts);
        }

        public SignatureHelpItem WithParameters(ImmutableArray<SignatureHelpParameter> parameters)
        {
            return With(parameters: parameters);
        }

        public SignatureHelpItem WithDescriptionParts(ImmutableArray<TaggedText> descriptionParts)
        {
            return With(descriptionParts: descriptionParts);
        }

        public SignatureHelpItem WithProperties(ImmutableDictionary<string, string> properties)
        {
            return With(properties: properties);
        }

        public static readonly SignatureHelpItem Empty = Create(
            isVariadic: false,
            prefixParts: ImmutableArray<TaggedText>.Empty,
            separatorParts: ImmutableArray<TaggedText>.Empty,
            suffixParts: ImmutableArray<TaggedText>.Empty,
            parameters: ImmutableArray<SignatureHelpParameter>.Empty,
            descriptionParts: ImmutableArray<TaggedText>.Empty,
            properties: null);

        internal IEnumerable<TaggedText> GetAllParts()
        {
            return
                PrefixDisplayParts.Concat(
                SeparatorDisplayParts.Concat(
                SuffixDisplayParts.Concat(
                Parameters.SelectMany(p => p.GetAllParts())).Concat(
                DescriptionParts)));
        }

        public bool Equals(SignatureHelpItem other)
        {
            return DeepEqualityComparer.Equals(this, other);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SignatureHelpItem);
        }

        public override int GetHashCode()
        {
            return DeepEqualityComparer.GetHashCode(this);
        }

        private static readonly IEqualityComparer<SignatureHelpItem> DeepEqualityComparer = new SignatureDeepEqualityComparer();

        private class SignatureDeepEqualityComparer : IEqualityComparer<SignatureHelpItem>
        {
            public static readonly SignatureDeepEqualityComparer Instance = new SignatureDeepEqualityComparer();

            public bool Equals(SignatureHelpItem x, SignatureHelpItem y)
            {
                if (object.ReferenceEquals(x, y))
                {
                    return true;
                }

                return x != null && y != null
                    && x.IsVariadic == y.IsVariadic
                    && x.PrefixDisplayParts.DeepEquals(y.PrefixDisplayParts)
                    && x.SuffixDisplayParts.DeepEquals(y.SuffixDisplayParts)
                    && x.SeparatorDisplayParts.DeepEquals(y.SeparatorDisplayParts)
                    && x.DescriptionParts.DeepEquals(y.DescriptionParts)
                    && x.Parameters.DeepEquals(y.Parameters)
                    && x.Properties.DeepEquals(y.Properties);
            }

            public int GetHashCode(SignatureHelpItem s)
            {
                return unchecked(
                    (s.IsVariadic ? 1 : 0)
                    + s.PrefixDisplayParts.GetDeepHashCode()
                    + s.SuffixDisplayParts.GetDeepHashCode()
                    + s.SeparatorDisplayParts.GetDeepHashCode()
                    + s.DescriptionParts.GetDeepHashCode()
                    + s.Parameters.GetDeepHashCode()
                    + s.Properties.GetDeepHashCode());
            }
        }
    }
}
