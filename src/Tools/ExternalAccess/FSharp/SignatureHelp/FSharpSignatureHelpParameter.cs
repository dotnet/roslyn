// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.SignatureHelp
{
    internal class FSharpSignatureHelpParameter
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

        public FSharpSignatureHelpParameter(
            string name,
            bool isOptional,
            Func<CancellationToken, IEnumerable<TaggedText>> documentationFactory,
            IEnumerable<TaggedText> displayParts,
            IEnumerable<TaggedText> prefixDisplayParts = null,
            IEnumerable<TaggedText> suffixDisplayParts = null,
            IEnumerable<TaggedText> selectedDisplayParts = null)
        {
            this.Name = name ?? string.Empty;
            this.IsOptional = isOptional;
            this.DocumentationFactory = documentationFactory ?? s_emptyDocumentationFactory;
            this.DisplayParts = displayParts.ToImmutableArrayOrEmpty();
            this.PrefixDisplayParts = prefixDisplayParts.ToImmutableArrayOrEmpty();
            this.SuffixDisplayParts = suffixDisplayParts.ToImmutableArrayOrEmpty();
            this.SelectedDisplayParts = selectedDisplayParts.ToImmutableArrayOrEmpty();
        }

        internal IEnumerable<TaggedText> GetAllParts()
        {
            return this.PrefixDisplayParts.Concat(this.DisplayParts)
                                          .Concat(this.SuffixDisplayParts)
                                          .Concat(this.SelectedDisplayParts);
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
