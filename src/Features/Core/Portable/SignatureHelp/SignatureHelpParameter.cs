// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SignatureHelp
{
    /// <summary>
    /// Used for C#/VB sig help providers so they can build up information using SymbolDisplayParts.
    /// These parts will then by used to properly replace anonymous type information in the parts.
    /// Once that it done, this will be converted to normal SignatureHelpParameters which only 
    /// point to TaggedText parts.
    /// </summary>
    internal class SignatureHelpSymbolParameter
    {
        /// <summary>
        /// The name of this parameter.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Documentation for this parameter.  This should normally be presented to the user when
        /// this parameter is selected.
        /// </summary>
        public Func<CancellationToken, IEnumerable<TaggedText>> DocumentationFactory { get; }

        /// <summary>
        /// Display parts to show before the normal display parts for the parameter.
        /// </summary>
        public IList<SymbolDisplayPart> PrefixDisplayParts { get; }

        /// <summary>
        /// Display parts to show after the normal display parts for the parameter.
        /// </summary>
        public IList<SymbolDisplayPart> SuffixDisplayParts { get; }

        /// <summary>
        /// Display parts for this parameter.  This should normally be presented to the user as part
        /// of the entire signature display.
        /// </summary>
        public IList<SymbolDisplayPart> DisplayParts { get; }

        /// <summary>
        /// True if this parameter is optional or not.  Optional parameters may be presented in a
        /// different manner to users.
        /// </summary>
        public bool IsOptional { get; }

        /// <summary>
        /// Display parts for this parameter that should be presented to the user when this
        /// parameter is selected.
        /// </summary>
        public IList<SymbolDisplayPart> SelectedDisplayParts { get; }

        private static readonly Func<CancellationToken, IEnumerable<TaggedText>> s_emptyDocumentationFactory =
            _ => SpecializedCollections.EmptyEnumerable<TaggedText>();

        public SignatureHelpSymbolParameter(
            string name,
            bool isOptional,
            Func<CancellationToken, IEnumerable<TaggedText>> documentationFactory,
            IEnumerable<SymbolDisplayPart> displayParts,
            IEnumerable<SymbolDisplayPart> prefixDisplayParts = null,
            IEnumerable<SymbolDisplayPart> suffixDisplayParts = null,
            IEnumerable<SymbolDisplayPart> selectedDisplayParts = null)
        {
            Name = name ?? string.Empty;
            IsOptional = isOptional;
            DocumentationFactory = documentationFactory ?? s_emptyDocumentationFactory;
            DisplayParts = displayParts.ToImmutableArrayOrEmpty();
            PrefixDisplayParts = prefixDisplayParts.ToImmutableArrayOrEmpty();
            SuffixDisplayParts = suffixDisplayParts.ToImmutableArrayOrEmpty();
            SelectedDisplayParts = selectedDisplayParts.ToImmutableArrayOrEmpty();
        }

        internal IEnumerable<SymbolDisplayPart> GetAllParts()
        {
            return PrefixDisplayParts.Concat(DisplayParts)
                                          .Concat(SuffixDisplayParts)
                                          .Concat(SelectedDisplayParts);
        }

        public static explicit operator SignatureHelpParameter(SignatureHelpSymbolParameter parameter)
        {
            return new SignatureHelp.SignatureHelpParameter(
                parameter.Name, parameter.IsOptional, parameter.DocumentationFactory,
                parameter.DisplayParts.ToTaggedText(),
                parameter.PrefixDisplayParts.ToTaggedText(),
                parameter.SuffixDisplayParts.ToTaggedText(),
                parameter.SelectedDisplayParts.ToTaggedText());
        }
    }

    internal class SignatureHelpParameter
    {
        /// <summary>
        /// The name of this parameter.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Documentation for this parameter.  This should normally be presented to the user when
        /// this parameter is selected.
        /// </summary>
        public Func<CancellationToken, IEnumerable<TaggedText>> DocumentationFactory { get; }

        /// <summary>
        /// Display parts to show before the normal display parts for the parameter.
        /// </summary>
        public IList<TaggedText> PrefixDisplayParts { get; }

        /// <summary>
        /// Display parts to show after the normal display parts for the parameter.
        /// </summary>
        public IList<TaggedText> SuffixDisplayParts { get; }

        /// <summary>
        /// Display parts for this parameter.  This should normally be presented to the user as part
        /// of the entire signature display.
        /// </summary>
        public IList<TaggedText> DisplayParts { get; }

        /// <summary>
        /// True if this parameter is optional or not.  Optional parameters may be presented in a
        /// different manner to users.
        /// </summary>
        public bool IsOptional { get; }

        /// <summary>
        /// Display parts for this parameter that should be presented to the user when this
        /// parameter is selected.
        /// </summary>
        public IList<TaggedText> SelectedDisplayParts { get; }

        private static readonly Func<CancellationToken, IEnumerable<TaggedText>> s_emptyDocumentationFactory =
            _ => SpecializedCollections.EmptyEnumerable<TaggedText>();

        // Constructor kept for binary compat with TS.  Remove when they move to the new API.
        public SignatureHelpParameter(
            string name,
            bool isOptional,
            Func<CancellationToken, IEnumerable<SymbolDisplayPart>> documentationFactory,
            IEnumerable<SymbolDisplayPart> displayParts,
            IEnumerable<SymbolDisplayPart> prefixDisplayParts = null,
            IEnumerable<SymbolDisplayPart> suffixDisplayParts = null,
            IEnumerable<SymbolDisplayPart> selectedDisplayParts = null)
            : this(name, isOptional,
                  c => documentationFactory(c).ToTaggedText(),
                  displayParts.ToTaggedText(),
                  prefixDisplayParts.ToTaggedText(),
                  suffixDisplayParts.ToTaggedText(),
                  selectedDisplayParts.ToTaggedText())
        {
        }

        public SignatureHelpParameter(
            string name,
            bool isOptional,
            Func<CancellationToken, IEnumerable<TaggedText>> documentationFactory,
            IEnumerable<TaggedText> displayParts,
            IEnumerable<TaggedText> prefixDisplayParts = null,
            IEnumerable<TaggedText> suffixDisplayParts = null,
            IEnumerable<TaggedText> selectedDisplayParts = null)
        {
            Name = name ?? string.Empty;
            IsOptional = isOptional;
            DocumentationFactory = documentationFactory ?? s_emptyDocumentationFactory;
            DisplayParts = displayParts.ToImmutableArrayOrEmpty();
            PrefixDisplayParts = prefixDisplayParts.ToImmutableArrayOrEmpty();
            SuffixDisplayParts = suffixDisplayParts.ToImmutableArrayOrEmpty();
            SelectedDisplayParts = selectedDisplayParts.ToImmutableArrayOrEmpty();
        }

        internal IEnumerable<TaggedText> GetAllParts()
        {
            return PrefixDisplayParts.Concat(DisplayParts)
                                          .Concat(SuffixDisplayParts)
                                          .Concat(SelectedDisplayParts);
        }

        public override string ToString()
        {
            var prefix = string.Concat(PrefixDisplayParts);
            var display = string.Concat(DisplayParts);
            var suffix = string.Concat(SuffixDisplayParts);
            return string.Concat(prefix, display, suffix);
        }
    }
}
