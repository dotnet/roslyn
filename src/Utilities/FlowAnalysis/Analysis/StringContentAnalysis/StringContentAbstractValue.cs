// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Analyzer.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.StringContentAnalysis
{
    /// <summary>
    /// Abstract string content data value for <see cref="AnalysisEntity"/>/<see cref="IOperation"/> tracked by <see cref="StringContentAnalysis"/>.
    /// </summary>
    internal partial class StringContentAbstractValue : CacheBasedEquatable<StringContentAbstractValue>
    {
        public static readonly StringContentAbstractValue UndefinedState = new StringContentAbstractValue(ImmutableHashSet<string>.Empty, StringContainsNonLiteralState.Undefined);
        public static readonly StringContentAbstractValue InvalidState = new StringContentAbstractValue(ImmutableHashSet<string>.Empty, StringContainsNonLiteralState.Invalid);
        public static readonly StringContentAbstractValue MayBeContainsNonLiteralState = new StringContentAbstractValue(ImmutableHashSet<string>.Empty, StringContainsNonLiteralState.Maybe);
        public static readonly StringContentAbstractValue DoesNotContainLiteralOrNonLiteralState = new StringContentAbstractValue(ImmutableHashSet<string>.Empty, StringContainsNonLiteralState.No);
        private static readonly StringContentAbstractValue ContainsEmpyStringLiteralState = new StringContentAbstractValue(ImmutableHashSet.Create(string.Empty), StringContainsNonLiteralState.No);

        public static StringContentAbstractValue Create(string literal)
        {
            if (literal.Length > 0)
            {
                return new StringContentAbstractValue(ImmutableHashSet.Create(literal), StringContainsNonLiteralState.No);
            }
            else
            {
                return ContainsEmpyStringLiteralState;
            }
        }

        private StringContentAbstractValue(ImmutableHashSet<string> literalValues, StringContainsNonLiteralState nonLiteralState)
        {
            LiteralValues = literalValues;
            NonLiteralState = nonLiteralState;
        }

        private static StringContentAbstractValue Create(ImmutableHashSet<string> literalValues, StringContainsNonLiteralState nonLiteralState)
        {
            if (literalValues.IsEmpty)
            {
                switch (nonLiteralState)
                {
                    case StringContainsNonLiteralState.Undefined:
                        return UndefinedState;
                    case StringContainsNonLiteralState.Invalid:
                        return InvalidState;
                    case StringContainsNonLiteralState.No:
                        return DoesNotContainLiteralOrNonLiteralState;
                    default:
                        return MayBeContainsNonLiteralState;
                }
            }

            return new StringContentAbstractValue(literalValues, nonLiteralState);
        }

        /// <summary>
        /// Indicates if this string variable contains non literal string operands or not.
        /// </summary>
        public StringContainsNonLiteralState NonLiteralState { get; }

        /// <summary>
        /// Gets a collection of the string literals that could possibly make up the contents of this string <see cref="Operand"/>.
        /// </summary>
        public ImmutableHashSet<string> LiteralValues { get; }

        protected override int ComputeHashCode()
        {
            var hashCode = HashUtilities.Combine(NonLiteralState.GetHashCode(), LiteralValues.Count.GetHashCode());
            foreach (var literal in LiteralValues.OrderBy(s => s))
            {
                hashCode = HashUtilities.Combine(hashCode, literal.GetHashCode());
            }

            return hashCode;
        }

        /// <summary>
        /// Performs the union of this state and the other state 
        /// and returns a new <see cref="StringContentAbstractValue"/> with the result.
        /// </summary>
        public StringContentAbstractValue Merge(StringContentAbstractValue otherState)
        {
            if (otherState == null)
            {
                throw new ArgumentNullException(nameof(otherState));
            }

            ImmutableHashSet<string> mergedLiteralValues = LiteralValues.Union(otherState.LiteralValues);
            StringContainsNonLiteralState mergedNonLiteralState = Merge(NonLiteralState, otherState.NonLiteralState);
            return Create(mergedLiteralValues, mergedNonLiteralState);
        }

        private static StringContainsNonLiteralState Merge(StringContainsNonLiteralState value1, StringContainsNonLiteralState value2)
        {
            // + U I M N
            // U U U M N
            // I U I M N
            // M M M M M
            // N N N M N
            if (value1 == StringContainsNonLiteralState.Maybe || value2 == StringContainsNonLiteralState.Maybe)
            {
                return StringContainsNonLiteralState.Maybe;
            }
            else if (value1 == StringContainsNonLiteralState.Invalid || value1 == StringContainsNonLiteralState.Undefined)
            {
                return value2;
            }
            else if (value2 == StringContainsNonLiteralState.Invalid || value2 == StringContainsNonLiteralState.Undefined)
            {
                return value1;
            }

            Debug.Assert(value1 == StringContainsNonLiteralState.No);
            Debug.Assert(value2 == StringContainsNonLiteralState.No);
            return StringContainsNonLiteralState.No;
        }

        public bool IsLiteralState => !LiteralValues.IsEmpty && NonLiteralState == StringContainsNonLiteralState.No;

        public StringContentAbstractValue IntersectLiteralValues(StringContentAbstractValue value2)
        {
            Debug.Assert(IsLiteralState);
            Debug.Assert(value2.IsLiteralState);

            // Merge Literals
            var mergedLiteralValues = this.LiteralValues.Intersect(value2.LiteralValues);
            return mergedLiteralValues.IsEmpty ? InvalidState : new StringContentAbstractValue(mergedLiteralValues, StringContainsNonLiteralState.No);
        }

        /// <summary>
        /// Performs the union of this state and the other state for a Binary add operation
        /// and returns a new <see cref="StringContentAbstractValue"/> with the result.
        /// </summary>
        public StringContentAbstractValue MergeBinaryAdd(StringContentAbstractValue otherState)
        {
            if (otherState == null)
            {
                throw new ArgumentNullException(nameof(otherState));
            }

            // Merge Literals
            var builder = ImmutableHashSet.CreateBuilder<string>();
            foreach (var leftLiteral in LiteralValues)
            {
                foreach (var rightLiteral in otherState.LiteralValues)
                {
                    builder.Add(leftLiteral + rightLiteral);
                }
            }

            ImmutableHashSet<string> mergedLiteralValues = builder.ToImmutable();
            StringContainsNonLiteralState mergedNonLiteralState = Merge(NonLiteralState, otherState.NonLiteralState);

            return new StringContentAbstractValue(mergedLiteralValues, mergedNonLiteralState);
        }

        /// <summary>
        /// Returns a string representation of <see cref="StringContentsState"/>.
        /// </summary>
        public override string ToString() =>
            string.Format(CultureInfo.InvariantCulture, "L({0}) NL:{1}", LiteralValues.Count, NonLiteralState.ToString()[0]);
    }
}
