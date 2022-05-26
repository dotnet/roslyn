// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.AddParameter
{
    internal readonly struct CodeFixData
    {
        public CodeFixData(
            IMethodSymbol method,
            Func<CancellationToken, Task<Solution>> createChangedSolutionNonCascading,
            Func<CancellationToken, Task<Solution>>? createChangedSolutionCascading)
        {
            Method = method ?? throw new ArgumentNullException(nameof(method));
            CreateChangedSolutionNonCascading = createChangedSolutionNonCascading ?? throw new ArgumentNullException(nameof(createChangedSolutionNonCascading));
            CreateChangedSolutionCascading = createChangedSolutionCascading;
        }

        /// <summary>
        /// The overload to fix.
        /// </summary>
        public IMethodSymbol Method { get; }

        /// <summary>
        /// A mandatory fix for the overload without cascading.
        /// </summary>
        public Func<CancellationToken, Task<Solution>> CreateChangedSolutionNonCascading { get; }

        /// <summary>
        /// An optional fix for the overload with cascading.
        /// </summary>
        public Func<CancellationToken, Task<Solution>>? CreateChangedSolutionCascading { get; }
    }
}
