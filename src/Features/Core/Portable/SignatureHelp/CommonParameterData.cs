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
    /// <summary>
    /// Used for C#/VB sig help providers so they can build up information using SymbolDisplayParts.
    /// These parts will then by used to properly replace anonymous type information in the parts.
    /// Once that it done, this will be converted to normal SignatureHelpParameters which only 
    /// point to TaggedText parts.
    /// </summary>
    internal class CommonParameterData
    {
        /// <summary>
        /// The name of this parameter.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Display parts to show before the normal display parts for the parameter.
        /// </summary>
        public ImmutableArray<SymbolDisplayPart> PrefixDisplayParts { get; }

        /// <summary>
        /// Display parts to show after the normal display parts for the parameter.
        /// </summary>
        public ImmutableArray<SymbolDisplayPart> SuffixDisplayParts { get; }

        /// <summary>
        /// Display parts for this parameter.  This should normally be presented to the user as part
        /// of the entire signature display.
        /// </summary>
        public ImmutableArray<SymbolDisplayPart> DisplayParts { get; }

        /// <summary>
        /// True if this parameter is optional or not.  Optional parameters may be presented in a
        /// different manner to users.
        /// </summary>
        public bool IsOptional { get; }

        public ISymbol Symbol { get; }
        public int Position { get; }

        /// <summary>
        /// Display parts for this parameter that should be presented to the user when this
        /// parameter is selected.
        /// </summary>
        public ImmutableArray<SymbolDisplayPart> SelectedDisplayParts { get; }

        public ImmutableDictionary<string, string> Properties { get; }

        public CommonParameterData(
            string name,
            bool isOptional,
            ISymbol symbol,
            int position,
            ImmutableArray<SymbolDisplayPart> displayParts,
            ImmutableArray<SymbolDisplayPart> prefixDisplayParts = default(ImmutableArray<SymbolDisplayPart>),
            ImmutableArray<SymbolDisplayPart> suffixDisplayParts = default(ImmutableArray<SymbolDisplayPart>),
            ImmutableArray<SymbolDisplayPart> selectedDisplayParts = default(ImmutableArray<SymbolDisplayPart>),
            ImmutableDictionary<string, string> properties = default(ImmutableDictionary<string, string>))
        {
            this.Name = name ?? string.Empty;
            this.IsOptional = isOptional;
            this.Symbol = symbol;
            this.Position = position;
            this.DisplayParts = displayParts.IsDefault ? ImmutableArray<SymbolDisplayPart>.Empty : displayParts;
            this.PrefixDisplayParts = prefixDisplayParts.IsDefault ? ImmutableArray<SymbolDisplayPart>.Empty : prefixDisplayParts;
            this.SuffixDisplayParts = suffixDisplayParts.IsDefault ? ImmutableArray<SymbolDisplayPart>.Empty : suffixDisplayParts;
            this.SelectedDisplayParts = selectedDisplayParts.IsDefault ? ImmutableArray<SymbolDisplayPart>.Empty : selectedDisplayParts;
            this.Properties = properties ?? ImmutableDictionary<string, string>.Empty;
        }

        internal IEnumerable<SymbolDisplayPart> GetAllParts()
        {
            return this.PrefixDisplayParts.Concat(this.DisplayParts)
                                          .Concat(this.SuffixDisplayParts)
                                          .Concat(this.SelectedDisplayParts);
        }

        public static explicit operator SignatureHelpParameter(CommonParameterData parameter)
        {
            return SignatureHelpParameter.Create(
                name: parameter.Name,
                isOptional: parameter.IsOptional,
                prefixDisplayParts: parameter.PrefixDisplayParts.ToTaggedText(),
                displayParts: parameter.DisplayParts.ToTaggedText(),
                suffixDisplayParts: parameter.SuffixDisplayParts.ToTaggedText(),
                selectedDisplayParts: parameter.SelectedDisplayParts.ToTaggedText(),
                properties: parameter.Properties)
                .WithSymbol(parameter.Symbol)
                .WithPosition(parameter.Position);
        }
    }
}
