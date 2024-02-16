// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{
    internal partial class SolutionCompilationState
    {
        private partial class CompilationTracker
        {
            /// <summary>
            /// When we're working with compilations, we often have two: a compilation that does not contain generated files
            /// (which we might need later to run generators again), and one that has the stale generated files that we might
            /// be able to reuse as well. In those cases we have to do the same transformations to both, and this gives us
            /// a handy way to do precisely that while not forking compilations twice if there are no generated files anywhere.
            /// </summary>
            internal readonly struct CompilationPair
            {
                public CompilationPair(Compilation withoutGeneratedDocuments, Compilation withGeneratedDocuments) : this()
                {
                    CompilationWithoutGeneratedDocuments = withoutGeneratedDocuments;
                    CompilationWithGeneratedDocuments = withGeneratedDocuments;
                }

                public Compilation CompilationWithoutGeneratedDocuments { get; }
                public Compilation CompilationWithGeneratedDocuments { get; }
            }
        }
    }
}
