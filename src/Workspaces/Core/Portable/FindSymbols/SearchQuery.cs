// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal struct SearchQuery : IDisposable
    {
        /// <summary>The name being searched for.  Is null in the case of custom predicate searching..  But 
        /// can be used for faster index based searching when it is available.</summary> 
        public readonly string? Name;

        ///<summary>The kind of search this is.  Faster index-based searching can be used if the 
        /// SearchKind is not <see cref="SearchKind.Custom"/>.</summary>
        public readonly SearchKind Kind;

        ///<summary>The predicate to fall back on if faster index searching is not possible.</summary>
        private readonly Func<string, bool> _predicate;

        /// <summary>
        /// Not readonly as this is mutable struct.
        /// </summary>
        private WordSimilarityChecker _wordSimilarityChecker;

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
                    _wordSimilarityChecker = new WordSimilarityChecker(name, substringsAreSimilar: false);
                    _predicate = _wordSimilarityChecker.AreSimilar;
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        private SearchQuery(Func<string, bool> predicate)
        {
            Kind = SearchKind.Custom;
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }

        public readonly void Dispose()
            => _wordSimilarityChecker.Dispose();

        public static SearchQuery Create(string name, SearchKind kind)
            => new(name, kind);

        public static SearchQuery Create(string name, bool ignoreCase)
            => new(name, ignoreCase ? SearchKind.ExactIgnoreCase : SearchKind.Exact);

        public static SearchQuery CreateFuzzy(string name)
            => new(name, SearchKind.Fuzzy);

        public static SearchQuery CreateCustom(Func<string, bool> predicate)
            => new(predicate);

        public readonly Func<string, bool> GetPredicate()
            => _predicate;
    }
}
