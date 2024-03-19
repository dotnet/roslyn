// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis;

internal partial class SolutionCompilationState
{
    private partial class CompilationTracker
    {
        /// <param name="Documents">
        /// The best generated documents we have for the current state. 
        /// </param>
        /// <param name="Driver">
        /// The <see cref="GeneratorDriver"/> that was used for the last run, to allow for incremental reuse. May be
        /// null if we don't have generators in the first place, haven't ran generators yet for this project, or had to
        /// get rid of our driver for some reason.
        /// </param>
        private readonly record struct CompilationTrackerGeneratorInfo(
            TextDocumentStates<SourceGeneratedDocumentState> Documents,
            GeneratorDriver? Driver)
        {
            public static readonly CompilationTrackerGeneratorInfo Empty =
                new(TextDocumentStates<SourceGeneratedDocumentState>.Empty, Driver: null);
        }
    }
}
