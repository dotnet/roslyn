using System;

namespace Roslyn.Services.Shared.Utilities
{
    /// <summary>
    /// Note(cyrusn): this enum is ordered from strongest match type to weakest match type.
    /// </summary>
    internal enum PatternMatchKind
    {
        /// <summary>
        /// The candidate string matched the pattern exactly.
        /// </summary>
        Exact,

        /// <summary>
        /// The pattern was a prefix of the candidate string.
        /// </summary>
        Prefix,

        /// <summary>
        /// The pattern was a substring of the candidate string, but in a way that wasn't a CamelCase match.
        /// </summary>
        Substring,

        /// <summary>
        /// The pattern was matched the CamelCased candidate string.
        /// </summary>
        CamelCase
    }

    internal struct PatternMatch
    {
        /// <summary>
        /// The weight of a CamelCase match. A higher number indicates a more accurate match.
        /// </summary>
        public int? CamelCaseWeight { get; private set; }

        /// <summary>
        /// True if this was a case sensitive match.
        /// </summary>
        public bool IsCaseSensitive { get; private set; }

        /// <summary>
        /// The type of match that occured.
        /// </summary>
        public PatternMatchKind Kind { get; private set; }

        internal PatternMatch(PatternMatchKind resultType, bool isCaseSensitive, int? camelCaseWeight = null)
            : this()
        {
            this.Kind = resultType;
            this.IsCaseSensitive = isCaseSensitive;
            this.CamelCaseWeight = camelCaseWeight;

            if ((resultType == PatternMatchKind.CamelCase) != camelCaseWeight.HasValue)
            {
                throw new ArgumentException("A CamelCase weight must be specified if and only if the resultType is CamelCase.");
            }
        }
    }
}