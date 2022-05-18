// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace Microsoft.CodeAnalysis.Testing
{
    internal static class WeightedMatch
    {
        /// <summary>
        /// Gets a collection of <see cref="Result{TExpected, TActual}"/>.
        /// </summary>
        /// <typeparam name="TExpected">The type of expected items to match.</typeparam>
        /// <typeparam name="TActual">The type of actual items to match.</typeparam>
        /// <param name="expected">The expected items to match.</param>
        /// <param name="actual">The actual items to match.</param>
        /// <param name="matchers">A collection of matching functions. Each matching function returns 0 to indicate a
        /// match, or a value greater than 0 for mismatch. Greater values are further from matching.</param>
        /// <param name="matchTimeout">The maximum time to spend searching for the best match in the event no exact
        /// match exists.</param>
        /// <returns>
        /// <para>A collection of matched elements with the following characteristics:</para>
        ///
        /// <list type="bullet">
        /// <item><description>Every element of <paramref name="expected"/> will appear exactly once as the expected element of an item in the result.</description></item>
        /// <item><description>Every element of <paramref name="actual"/> will appear exactly once as the actual element of an item in the result.</description></item>
        /// <item><description>An item in the result which specifies both an expected and an actual value indicates a matched pair, i.e. the actual and expected results are believed to refer to the same item.</description></item>
        /// <item><description>An item in the result which specifies only an actual value indicates an item for which no matching expected item was found.</description></item>
        /// <item><description>An item in the result which specifies only an expected value indicates an item for which no matching actual item was found.</description></item>
        /// </list>
        ///
        /// <para>If no exact match is found (all actual diagnostics are matched to an expected diagnostic without
        /// errors), this method is <em>allowed</em> to attempt fall-back matching using a strategy intended to minimize
        /// the total number of mismatched pairs.</para>
        /// </returns>
        public static ImmutableArray<Result<TExpected, TActual>> Match<TExpected, TActual>(
            ImmutableArray<TExpected> expected,
            ImmutableArray<TActual> actual,
            ImmutableArray<Func<TExpected, TActual, double>> matchers,
            TimeSpan matchTimeout)
        {
            var resultBuilder = ImmutableArray.CreateBuilder<Result<TExpected, TActual>>(Math.Max(expected.Length, actual.Length));
            var commonCount = Math.Min(expected.Length, actual.Length);
            for (var i = 0; i < commonCount; i++)
            {
                var distance = Evaluate(expected[i], actual[i], matchers);
                resultBuilder.Add(new Result<TExpected, TActual>(hasExpected: true, expected[i], hasActual: true, actual[i], distance));
            }

            for (var i = commonCount; i < expected.Length; i++)
            {
                resultBuilder.Add(new Result<TExpected, TActual>(hasExpected: true, expected[i], hasActual: false, actual: default, distance: 0));
            }

            for (var i = commonCount; i < actual.Length; i++)
            {
                resultBuilder.Add(new Result<TExpected, TActual>(hasExpected: false, expected: default, hasActual: true, actual[i], distance: 0));
            }

            // Storage for results
            var bestResult = resultBuilder.MoveToImmutable();
            var bestDistance = bestResult.Sum(result => result.Distance);

            // Storage for temporary data
            var usedActual = new bool[actual.Length];

            // Attempt to find an exact match
            MatchRecursive(
                expected,
                actual,
                matchers,
                resultBuilder,
                ref bestResult,
                ref bestDistance,
                currentDistance: 0.0,
                remainingUnmatched: 0,
                nextExpected: 0,
                usedActual,
                exactOnly: true,
                CancellationToken.None);
            if (bestDistance == 0)
            {
                return bestResult;
            }

            // Attempt to find a better but non-exact match within a time limit
            using var cancellationTokenSource = new CancellationTokenSource(matchTimeout);
            MatchRecursive(
                expected,
                actual,
                matchers,
                resultBuilder,
                ref bestResult,
                ref bestDistance,
                currentDistance: 0.0,
                remainingUnmatched: 0,
                nextExpected: 0,
                usedActual,
                exactOnly: true,
                cancellationTokenSource.Token);

            return bestResult;
        }

        private static void MatchRecursive<TExpected, TActual>(
            ImmutableArray<TExpected> expected,
            ImmutableArray<TActual> actual,
            ImmutableArray<Func<TExpected, TActual, double>> matchers,
            ImmutableArray<Result<TExpected, TActual>>.Builder resultBuilder,
            ref ImmutableArray<Result<TExpected, TActual>> bestResult,
            ref double bestDistance,
            double currentDistance,
            int remainingUnmatched,
            int nextExpected,
            bool[] usedActual,
            bool exactOnly,
            CancellationToken cancellationToken)
        {
            if (currentDistance >= bestDistance)
            {
                return;
            }

            if (nextExpected == expected.Length)
            {
                // Nothing left to match, but we have a better overall result
                for (var i = 0; i < usedActual.Length; i++)
                {
                    if (!usedActual[i])
                    {
                        resultBuilder.Add(new Result<TExpected, TActual>(hasExpected: false, expected: default, hasActual: true, actual[i], distance: 0));
                    }
                }

                bestResult = resultBuilder.ToImmutable();
                bestDistance = currentDistance;

                // Remove any unmatched items before returning
                for (var i = 0; i < remainingUnmatched; i++)
                {
                    resultBuilder.RemoveAt(resultBuilder.Count - 1);
                }

                return;
            }

            for (var i = 0; i < usedActual.Length; i++)
            {
                if (usedActual[i])
                {
                    continue;
                }

                var distance = Evaluate(expected[nextExpected], actual[i], matchers);
                if (exactOnly && distance != 0)
                {
                    continue;
                }

                try
                {
                    usedActual[i] = true;
                    resultBuilder.Add(new Result<TExpected, TActual>(hasExpected: true, expected[nextExpected], hasActual: true, actual[i], distance));
                    MatchRecursive(
                        expected,
                        actual,
                        matchers,
                        resultBuilder,
                        ref bestResult,
                        ref bestDistance,
                        currentDistance: currentDistance + distance,
                        remainingUnmatched,
                        nextExpected: nextExpected + 1,
                        usedActual,
                        exactOnly,
                        cancellationToken);
                }
                finally
                {
                    resultBuilder.RemoveAt(resultBuilder.Count - 1);
                    usedActual[i] = false;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (currentDistance >= bestDistance)
                {
                    // We will not be able to further improve on this result without returning
                    return;
                }
            }

            if (remainingUnmatched > 0 && expected.Length > actual.Length)
            {
                try
                {
                    resultBuilder.Add(new Result<TExpected, TActual>(hasExpected: true, expected[nextExpected], hasActual: false, actual: default, distance: 0));
                    MatchRecursive(
                        expected,
                        actual,
                        matchers,
                        resultBuilder,
                        ref bestResult,
                        ref bestDistance,
                        currentDistance,
                        remainingUnmatched: remainingUnmatched - 1,
                        nextExpected: nextExpected + 1,
                        usedActual,
                        exactOnly,
                        cancellationToken);
                }
                finally
                {
                    resultBuilder.RemoveAt(resultBuilder.Count - 1);
                }
            }
        }

        private static double Evaluate<TExpected, TActual>(
            TExpected expected,
            TActual actual,
            ImmutableArray<Func<TExpected, TActual, double>> matchers)
        {
            var totalResult = 0.0;
            foreach (var matcher in matchers)
            {
                var result = matcher(expected, actual);
                totalResult += result * result;
            }

            return totalResult / matchers.Length;
        }

        public readonly struct Result<TExpected, TActual>
        {
            private readonly bool _hasExpected;
            private readonly TExpected? _expected;
            private readonly bool _hasActual;
            private readonly TActual? _actual;

            public Result(bool hasExpected, TExpected? expected, bool hasActual, TActual? actual, double distance)
            {
                _hasExpected = hasExpected;
                _expected = expected;
                _hasActual = hasActual;
                _actual = actual;
                Distance = distance;
            }

            /// <summary>
            /// Gets the match distance. A distance of <c>0</c> means the expected and actual values are considered a
            /// full match. Results with higher distance are considered to be less similar.
            /// </summary>
            public double Distance { get; }

            public bool TryGetExpected([MaybeNullWhen(false)] out TExpected expected)
            {
                expected = _expected;
                return _hasExpected;
            }

            public bool TryGetActual([MaybeNullWhen(false)] out TActual actual)
            {
                actual = _actual;
                return _hasActual;
            }
        }
    }
}
