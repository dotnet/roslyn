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
            ImmutableArray<Func<TExpected, TActual, bool, double>> matchers,
            TimeSpan matchTimeout)
        {
            // Initialize the algorithm with an initial "best guess" that evaluates the distance for the items in
            // expected and actual matched in the order they appear, with any remaining items from the longer array
            // added at the end. After this point, a new best guess will only be assigned if its distance is strictly
            // less than the prior best guess.
            var resultBuilder = ImmutableArray.CreateBuilder<Result<TExpected, TActual>>(Math.Max(expected.Length, actual.Length));
            var commonCount = Math.Min(expected.Length, actual.Length);
            for (var i = 0; i < commonCount; i++)
            {
                var distance = Evaluate(expected[i], actual[i], exactOnly: false, matchers);
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

            var initialResult = resultBuilder.MoveToImmutable();

            // Storage for results
            var bestResult = initialResult;
            var bestDistance = bestResult.Sum(result => result.Distance);

            /*
             * Storage for temporary data.
             */

            // Keeps track of the items from 'actual' which have been matched on the current path.
            var usedActual = new bool[actual.Length];

            // The distance cache records the distance evaluation. Values less than 0 mean the distance has not been
            // computed. Initially, all values are -1 except for the items calculated as part of the initial best guess
            // above.
            var distanceCache = new double[expected.Length * actual.Length];
            for (var i = 0; i < distanceCache.Length; i++)
            {
                distanceCache[i] = -1.0;
            }

            for (var i = 0; i < commonCount; i++)
            {
                distanceCache[(i * actual.Length) + i] = initialResult[i].Distance;
            }

            // Attempt to find an exact match. This portion is run without a timeout to ensure that an exact match will
            // be found if it exists.
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
                distanceCache,
                exactOnly: true,
                CancellationToken.None);
            if (bestDistance == 0)
            {
                return bestResult;
            }

            // Attempt to find a better but non-exact match within a time limit. Make sure to reset the distance cache
            // for any non-zero distances, since the exact match function may have only calculated a portion of the
            // distance.
            for (var i = 0; i < distanceCache.Length; i++)
            {
                if (distanceCache[i] != 0.0)
                {
                    distanceCache[i] = -1.0;
                }
            }

            for (var i = 0; i < commonCount; i++)
            {
                distanceCache[(i * actual.Length) + i] = initialResult[i].Distance;
            }

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
                distanceCache,
                exactOnly: true,
                cancellationTokenSource.Token);

            return bestResult;
        }

        private static void MatchRecursive<TExpected, TActual>(
            ImmutableArray<TExpected> expected,
            ImmutableArray<TActual> actual,
            ImmutableArray<Func<TExpected, TActual, bool, double>> matchers,
            ImmutableArray<Result<TExpected, TActual>>.Builder resultBuilder,
            ref ImmutableArray<Result<TExpected, TActual>> bestResult,
            ref double bestDistance,
            double currentDistance,
            int remainingUnmatched,
            int nextExpected,
            bool[] usedActual,
            double[] distanceCache,
            bool exactOnly,
            CancellationToken cancellationToken)
        {
            if (currentDistance >= bestDistance)
            {
                // From this point, it will not be possible to reach the end with a lower total distance, so we can
                // return immediately.
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

                ref var cachedDistance = ref distanceCache[(nextExpected * actual.Length) + i];
                var distance = cachedDistance;
                if (distance < 0)
                {
                    distance = cachedDistance = Evaluate(expected[nextExpected], actual[i], exactOnly, matchers);
                }

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
                        distanceCache,
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
                        distanceCache,
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
            bool exactOnly,
            ImmutableArray<Func<TExpected, TActual, bool, double>> matchers)
        {
            var totalResult = 0.0;
            foreach (var matcher in matchers)
            {
                var result = matcher(expected, actual, exactOnly);
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
