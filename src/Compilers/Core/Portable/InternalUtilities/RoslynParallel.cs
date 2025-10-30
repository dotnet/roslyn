// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Roslyn.Utilities
{
    internal static class RoslynParallel
    {
        internal static readonly ParallelOptions DefaultParallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };

        /// <inheritdoc cref="Parallel.For(int, int, ParallelOptions, Action{int})"/>
        public static ParallelLoopResult For(int fromInclusive, int toExclusive, Action<int> body, CancellationToken cancellationToken)
        {
            var parallelOptions = cancellationToken.CanBeCanceled
                ? new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = Environment.ProcessorCount }
                : DefaultParallelOptions;

            return Parallel.For(fromInclusive, toExclusive, parallelOptions, errorHandlingBody);

            // Local function
            void errorHandlingBody(int i)
            {
                try
                {
                    body(i);
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
                {
                    throw ExceptionUtilities.Unreachable();
                }
                catch (OperationCanceledException e) when (cancellationToken.IsCancellationRequested && e.CancellationToken != cancellationToken)
                {
                    // Parallel.For checks for a specific cancellation token, so make sure we throw with the
                    // correct one.
                    cancellationToken.ThrowIfCancellationRequested();
                    throw ExceptionUtilities.Unreachable();
                }
            }
        }
    }
}
