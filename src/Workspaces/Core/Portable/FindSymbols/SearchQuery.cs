// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal class SearchQuery
    {
        /// <summary>The name being searched for.  Is null in the case of custom predicate searching..  But 
        /// can be used for faster index based searching when it is available.</summary> 
        public readonly string Name;

        ///<summary>The kind of search this is.  Faster index-based searching can be used if the 
        /// SearchKind is not <see cref="SearchKind.Custom"/>.</summary>
        public readonly SearchKind Kind;

        ///<summary>The predicate to fall back on if faster index searching is not possible.</summary>
        private readonly Func<string, bool> _predicate;

        private SearchQuery(string name, SearchKind kind)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Kind = kind;

            switch (kind)
            {
                case SearchKind.Exact:
                    _predicate = s => StringComparer.Ordinal.Equals(name, s);
                    break;
                case SearchKind.ExactIgnoreCase:
                    _predicate = s => CaseInsensitiveComparison.Comparer.Equals(name, s);
                    break;
                case SearchKind.Fuzzy:
                    // Create a single WordSimilarityChecker and capture a delegate reference to 
                    // its 'AreSimilar' method. That way we only create the WordSimilarityChecker
                    // once and it can cache all the information it needs while it does the AreSimilar
                    // check against all the possible candidates.
                    var editDistance = new WordSimilarityChecker(name, substringsAreSimilar: false);
                    _predicate = editDistance.AreSimilar;
                    break;
            }
        }

        private SearchQuery(Func<string, bool> predicate)
        {
            Kind = SearchKind.Custom;
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }

        public static SearchQuery Create(string name, bool ignoreCase)
        {
            return new SearchQuery(name, ignoreCase ? SearchKind.ExactIgnoreCase : SearchKind.Exact);
        }

        public static SearchQuery CreateFuzzy(string name)
        {
            return new SearchQuery(name, SearchKind.Fuzzy);
        }

        public static SearchQuery CreateCustom(Func<string, bool> predicate)
        {
            return new SearchQuery(predicate);
        }

        public Func<string, bool> GetPredicate()
        {
            return _predicate;
        }
    }
}