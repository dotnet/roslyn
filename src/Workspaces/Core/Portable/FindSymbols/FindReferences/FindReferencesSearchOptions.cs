// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    [DataContract]
    internal sealed class FindReferencesSearchOptions
    {
        public static readonly FindReferencesSearchOptions Default =
            new(
                associatePropertyReferencesWithSpecificAccessor: false,
                cascade: true);

        /// <summary>
        /// When searching for property, associate specific references we find to the relevant
        /// accessor symbol (if there is one).  For example, in C#, this would result in:
        /// 
        ///     P = 0;     // A reference to the P.set accessor
        ///     var v = P; // A reference to the P.get accessor
        ///     P++;       // A reference to P.get and P.set accessors
        ///     nameof(P); // A reference only to P.  Not associated with a particular accessor.
        ///     
        /// The default for this is false.  With that default, all of the above references
        /// are associated with the property P and not the accessors.
        /// </summary>
        [DataMember(Order = 0)]
        public bool AssociatePropertyReferencesWithSpecificAccessor { get; }

        /// <summary>
        /// Whether or not we should cascade from the original search symbol to new symbols as we're
        /// doing the find-references search.
        /// </summary>
        [DataMember(Order = 1)]
        public bool Cascade { get; }

        public FindReferencesSearchOptions(
            bool associatePropertyReferencesWithSpecificAccessor,
            bool cascade)
        {
            AssociatePropertyReferencesWithSpecificAccessor = associatePropertyReferencesWithSpecificAccessor;
            Cascade = cascade;
        }

        public FindReferencesSearchOptions WithAssociatePropertyReferencesWithSpecificAccessor(bool associatePropertyReferencesWithSpecificAccessor)
            => new(associatePropertyReferencesWithSpecificAccessor, Cascade);

        public FindReferencesSearchOptions WithCascade(bool cascade)
            => new(AssociatePropertyReferencesWithSpecificAccessor, cascade);

        /// <summary>
        /// For IDE features, if the user starts searching on an accessor, then we want to give
        /// results associated with the specific accessor.  Otherwise, if they search on a property,
        /// then associate everything with the property.
        /// </summary>
        public static FindReferencesSearchOptions GetFeatureOptionsForStartingSymbol(ISymbol symbol)
            => Default.WithAssociatePropertyReferencesWithSpecificAccessor(symbol.IsPropertyAccessor());
    }
}
