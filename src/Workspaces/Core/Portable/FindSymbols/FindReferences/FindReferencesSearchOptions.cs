// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal class FindReferencesSearchOptions
    {
        public static readonly FindReferencesSearchOptions Default =
            new FindReferencesSearchOptions(associatePropertyReferencesWithSpecificAccessor: false);

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
        public bool AssociatePropertyReferencesWithSpecificAccessor { get; }

        /// <summary>
        /// Exclude references in unchangeable documents from search results.
        /// i.e. when a document's corresponding <see cref="IDocumentOperationService.CanApplyChange()"/> returns <c>false</c>.
        /// </summary>
        public bool IgnoreUnchangeableDocuments { get; }

        public FindReferencesSearchOptions(
            bool associatePropertyReferencesWithSpecificAccessor,
            bool ignoreUnchangeableDocuments = false)
        {
            AssociatePropertyReferencesWithSpecificAccessor = associatePropertyReferencesWithSpecificAccessor;
            IgnoreUnchangeableDocuments = ignoreUnchangeableDocuments;
        }

        public FindReferencesSearchOptions WithAssociatePropertyReferencesWithSpecificAccessor(
            bool associatePropertyReferencesWithSpecificAccessor)
        {
            return new FindReferencesSearchOptions(associatePropertyReferencesWithSpecificAccessor, this.IgnoreUnchangeableDocuments);
        }

        public FindReferencesSearchOptions WithIgnoreUnchangeableDocuments(
            bool ignoreUnchangeableDocuments)
        {
            return new FindReferencesSearchOptions(this.AssociatePropertyReferencesWithSpecificAccessor, ignoreUnchangeableDocuments);
        }

        /// <summary>
        /// For IDE features, if the user starts searching on an accessor, then we want to give
        /// results associated with the specific accessor.  Otherwise, if they search on a property,
        /// then associate everything with the property.
        /// </summary>
        public static FindReferencesSearchOptions GetFeatureOptionsForStartingSymbol(ISymbol symbol)
            => symbol.IsPropertyAccessor()
                ? new FindReferencesSearchOptions(associatePropertyReferencesWithSpecificAccessor: true)
                : FindReferencesSearchOptions.Default;
    }
}
