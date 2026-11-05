// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis;

internal sealed partial class SolutionCompilationState
{
    private sealed partial class RegularCompilationTracker
    {
        /// <param name="Documents">
        /// The best generated documents we have for the current state. 
        /// </param>
        /// <param name="Driver">
        /// The <see cref="GeneratorDriver"/> that was used for the last run, to allow for incremental reuse. May be
        /// null if we don't have generators in the first place, haven't ran generators yet for this project, or had to
        /// get rid of our driver for some reason.
        /// </param>
        /// <param name="GeneratorsWithCompletedRegularOutputs">
        /// Generators whose last selected regular execution completed successfully. A filtered shared-phase
        /// failure does not invalidate the retained regular result.
        /// </param>
        private readonly record struct CompilationTrackerGeneratorInfo(
            TextDocumentStates<SourceGeneratedDocumentState> Documents,
            GeneratorDriver? Driver,
            ImmutableHashSet<ISourceGenerator> GeneratorsWithCompletedRegularOutputs)
        {
            public static readonly CompilationTrackerGeneratorInfo Empty =
                new(TextDocumentStates<SourceGeneratedDocumentState>.Empty, Driver: null,
                    ImmutableHashSet.Create<ISourceGenerator>(ReferenceEqualityComparer.Instance));
        }
    }
}
