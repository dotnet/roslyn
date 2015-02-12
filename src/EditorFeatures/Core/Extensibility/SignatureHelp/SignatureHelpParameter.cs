// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor
{
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
        /// <remarks>
        /// Note that this is an IEnumerable, not an IList because we want lazy evaluation.
        /// </remarks>
        public IEnumerable<SymbolDisplayPart> Documentation { get; }

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

        public SignatureHelpParameter(
            string name,
            bool isOptional,
            IEnumerable<SymbolDisplayPart> documentation,
            IEnumerable<SymbolDisplayPart> displayParts,
            IEnumerable<SymbolDisplayPart> prefixDisplayParts = null,
            IEnumerable<SymbolDisplayPart> suffixDisplayParts = null,
            IEnumerable<SymbolDisplayPart> selectedDisplayParts = null)
        {
            this.Name = name ?? string.Empty;
            this.IsOptional = isOptional;
            this.Documentation = documentation;
            this.DisplayParts = displayParts.ToImmutableArrayOrEmpty();
            this.PrefixDisplayParts = prefixDisplayParts.ToImmutableArrayOrEmpty();
            this.SuffixDisplayParts = suffixDisplayParts.ToImmutableArrayOrEmpty();
            this.SelectedDisplayParts = selectedDisplayParts.ToImmutableArrayOrEmpty();
        }

        internal IEnumerable<SymbolDisplayPart> GetAllParts()
        {
            return this.PrefixDisplayParts.Concat(this.DisplayParts)
                                          .Concat(this.SuffixDisplayParts)
                                          .Concat(this.SelectedDisplayParts);
        }
    }
}
