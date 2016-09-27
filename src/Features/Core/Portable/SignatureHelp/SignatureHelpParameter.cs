// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SignatureHelp
{
    internal sealed class SignatureHelpParameter : IEquatable<SignatureHelpParameter>
    {
        /// <summary>
        /// The name of this parameter.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Display parts to show before the normal display parts for the parameter.
        /// </summary>
        public ImmutableArray<TaggedText> PrefixDisplayParts { get; }

        /// <summary>
        /// Display parts for this parameter.  This should normally be presented to the user as part
        /// of the entire signature display.
        /// </summary>
        public ImmutableArray<TaggedText> DisplayParts { get; }

        /// <summary>
        /// Display parts to show after the normal display parts for the parameter.
        /// </summary>
        public ImmutableArray<TaggedText> SuffixDisplayParts { get; }

        /// <summary>
        /// True if this parameter is optional or not.  Optional parameters may be presented in a
        /// different manner to users.
        /// </summary>
        public bool IsOptional { get; }

        /// <summary>
        /// Display parts for this parameter that should be presented to the user when this
        /// parameter is selected.
        /// </summary>
        public ImmutableArray<TaggedText> SelectedDisplayParts { get; }

        /// <summary>
        /// One or more properties defined by the <see cref="SignatureHelpService"/>.
        /// </summary>
        public ImmutableDictionary<string, string> Properties { get; }

        private SignatureHelpParameter(
            string name,
            bool isOptional,
            ImmutableArray<TaggedText> prefixDisplayParts,
            ImmutableArray<TaggedText> displayParts,
            ImmutableArray<TaggedText> suffixDisplayParts,
            ImmutableArray<TaggedText> selectedDisplayParts,
            ImmutableDictionary<string, string> properties)
        {
            this.Name = name ?? string.Empty;
            this.IsOptional = isOptional;
            this.PrefixDisplayParts = prefixDisplayParts.IsDefault ? ImmutableArray<TaggedText>.Empty : prefixDisplayParts;
            this.DisplayParts = displayParts.IsDefault ? ImmutableArray<TaggedText>.Empty : displayParts;
            this.SuffixDisplayParts = suffixDisplayParts.IsDefault ? ImmutableArray<TaggedText>.Empty : suffixDisplayParts;
            this.SelectedDisplayParts = selectedDisplayParts.IsDefault ? ImmutableArray<TaggedText>.Empty : selectedDisplayParts;
            this.Properties = properties ?? ImmutableDictionary<string, string>.Empty;
        }

        public static SignatureHelpParameter Create(
            string name,
            bool isOptional,
            ImmutableArray<TaggedText> prefixDisplayParts,
            ImmutableArray<TaggedText> displayParts,
            ImmutableArray<TaggedText> suffixDisplayParts,
            ImmutableArray<TaggedText> selectedDisplayParts = default(ImmutableArray<TaggedText>),
            ImmutableDictionary<string, string> properties = null)
        {
            return new SignatureHelpParameter(name, isOptional, prefixDisplayParts, displayParts, suffixDisplayParts, selectedDisplayParts, properties);
        }

        public static SignatureHelpParameter Create(
            string name,
            bool isOptional,
            ImmutableArray<TaggedText> displayParts,
            ImmutableDictionary<string, string> properties = null)
        {
            return new SignatureHelpParameter(name, isOptional, ImmutableArray<TaggedText>.Empty, displayParts, ImmutableArray<TaggedText>.Empty, ImmutableArray<TaggedText>.Empty, properties);
        }

        private SignatureHelpParameter With(
            Optional<string> name = default(Optional<string>),
            Optional<bool> isOptional = default(Optional<bool>),
            Optional<ImmutableArray<TaggedText>> prefixParts = default(Optional<ImmutableArray<TaggedText>>),
            Optional<ImmutableArray<TaggedText>> parts = default(Optional<ImmutableArray<TaggedText>>),
            Optional<ImmutableArray<TaggedText>> suffixParts = default(Optional<ImmutableArray<TaggedText>>),
            Optional<ImmutableArray<TaggedText>> selectedParts = default(Optional<ImmutableArray<TaggedText>>),
            Optional<ImmutableDictionary<string, string>> properties = default(Optional<ImmutableDictionary<string, string>>))
        {
            var newName = name.HasValue ? name.Value : this.Name;
            var newIsOptional = isOptional.HasValue ? isOptional.Value : this.IsOptional;
            var newPrefixParts = prefixParts.HasValue ? prefixParts.Value : this.PrefixDisplayParts;
            var newParts = parts.HasValue ? parts.Value : this.DisplayParts;
            var newSuffixParts = suffixParts.HasValue ? suffixParts.Value : this.SuffixDisplayParts;
            var newSelectedParts = selectedParts.HasValue ? selectedParts.Value : this.SelectedDisplayParts;
            var newProperties = properties.HasValue ? properties.Value : this.Properties;

            if (newName != this.Name
                || newIsOptional != this.IsOptional
                || newPrefixParts != this.PrefixDisplayParts
                || newParts != this.DisplayParts
                || newSuffixParts != this.SuffixDisplayParts
                || newSelectedParts != this.SelectedDisplayParts
                || newProperties != this.Properties)
            {
                return Create(newName, newIsOptional, newPrefixParts, newParts, newSuffixParts, newSelectedParts, newProperties);
            }
            else
            {
                return this;
            }
        }

        public SignatureHelpParameter WithName(string name)
        {
            return With(name: name);
        }

        public SignatureHelpParameter WithIsOptional(bool isOptional)
        {
            return With(isOptional: isOptional);
        }

        public SignatureHelpParameter WithPrefixParts(ImmutableArray<TaggedText> prefixParts)
        {
            return With(prefixParts: prefixParts);
        }

        public SignatureHelpParameter WithDisplayParts(ImmutableArray<TaggedText> displayParts)
        {
            return With(parts: displayParts);
        }

        public SignatureHelpParameter WithSuffixParts(ImmutableArray<TaggedText> suffixParts)
        {
            return With(suffixParts: suffixParts);
        }

        public SignatureHelpParameter WithSelectedParts(ImmutableArray<TaggedText> selectedParts)
        {
            return With(selectedParts: selectedParts);
        }

        public SignatureHelpParameter WithProperties(ImmutableDictionary<string, string> properties)
        {
            return With(properties: properties);
        }

        internal IEnumerable<TaggedText> GetAllParts()
        {
            return this.PrefixDisplayParts.Concat(this.DisplayParts)
                                          .Concat(this.SuffixDisplayParts)
                                          .Concat(this.SelectedDisplayParts);
        }

        public bool Equals(SignatureHelpParameter other)
        {
            return DeepEqualityComparer.Equals(this, other);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SignatureHelpProvider);
        }

        public override int GetHashCode()
        {
            return DeepEqualityComparer.GetHashCode(this);
        }

        internal static readonly IEqualityComparer<SignatureHelpParameter> DeepEqualityComparer = new SignatureParameterEqualityComparer();

        private class SignatureParameterEqualityComparer : IEqualityComparer<SignatureHelpParameter>
        {
            public bool Equals(SignatureHelpParameter x, SignatureHelpParameter y)
            {
                if (object.ReferenceEquals(x, y))
                {
                    return true;
                }

                return x != null && y != null
                    && x.Name == y.Name
                    && x.PrefixDisplayParts.DeepEquals(y.PrefixDisplayParts)
                    && x.DisplayParts.DeepEquals(y.DisplayParts)
                    && x.SuffixDisplayParts.DeepEquals(y.SuffixDisplayParts)
                    && x.IsOptional == y.IsOptional
                    && x.SelectedDisplayParts.DeepEquals(y.SelectedDisplayParts)
                    && x.Properties.DeepEquals(y.Properties);
            }

            public int GetHashCode(SignatureHelpParameter p)
            {
                return unchecked(
                    p.Name.GetHashCode()
                    + p.PrefixDisplayParts.GetDeepHashCode()
                    + p.DisplayParts.GetDeepHashCode()
                    + p.SuffixDisplayParts.GetDeepHashCode()
                    + (p.IsOptional ? 1 : 0)
                    + p.SelectedDisplayParts.GetDeepHashCode()
                    + p.Properties.GetDeepHashCode());
            }
        }
    }
}