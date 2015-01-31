// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor
{
    internal class SignatureHelpItem
    {
        /// <summary>
        /// True if this signature help item can have an unbounded number of arguments passed to it.
        /// If it is variadic then the last parameter will be considered selected, even if the
        /// selected parameter index strictly goes past the number of defined parameters for this
        /// item.
        /// </summary>
        public bool IsVariadic { get; private set; }

        public IList<SymbolDisplayPart> PrefixDisplayParts { get; private set; }
        public IList<SymbolDisplayPart> SuffixDisplayParts { get; private set; }

        // TODO(cyrusn): This probably won't be sufficient for VB query signature help.  It has
        // arbitrary separators between parameters.
        public IList<SymbolDisplayPart> SeparatorDisplayParts { get; private set; }

        public IList<SignatureHelpParameter> Parameters { get; private set; }

        public IList<SymbolDisplayPart> DescriptionParts { get; internal set; }

        // TODO(cyrusn): This may be unnecessary.  How would a user ever see this.
        public IList<SymbolDisplayPart> Documentation { get; set; }

        public SignatureHelpItem(
            bool isVariadic,
            IEnumerable<SymbolDisplayPart> documentation,
            IEnumerable<SymbolDisplayPart> prefixParts,
            IEnumerable<SymbolDisplayPart> separatorParts,
            IEnumerable<SymbolDisplayPart> suffixParts,
            IEnumerable<SignatureHelpParameter> parameters,
            IEnumerable<SymbolDisplayPart> descriptionParts)
        {
            if (isVariadic && !parameters.Any())
            {
                throw new ArgumentException(EditorFeaturesResources.VariadicSignaturehelpitemMustHaveOneParam);
            }

            this.IsVariadic = isVariadic;
            this.Documentation = documentation.ToImmutableArrayOrEmpty();
            this.PrefixDisplayParts = prefixParts.ToImmutableArrayOrEmpty();
            this.SeparatorDisplayParts = separatorParts.ToImmutableArrayOrEmpty();
            this.SuffixDisplayParts = suffixParts.ToImmutableArrayOrEmpty();
            this.Parameters = parameters.ToImmutableArrayOrEmpty();
            this.DescriptionParts = descriptionParts.ToImmutableArrayOrEmpty();
        }

        internal IEnumerable<SymbolDisplayPart> GetAllParts()
        {
            return
                PrefixDisplayParts.Concat(
                SeparatorDisplayParts.Concat(
                SuffixDisplayParts.Concat(
                Parameters.SelectMany(p => p.GetAllParts())).Concat(
                DescriptionParts)));
        }
    }
}
