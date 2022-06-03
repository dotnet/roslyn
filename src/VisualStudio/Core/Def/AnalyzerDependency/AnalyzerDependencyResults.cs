// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal sealed class AnalyzerDependencyResults
    {
        public static readonly AnalyzerDependencyResults Empty = new(ImmutableArray<AnalyzerDependencyConflict>.Empty, ImmutableArray<MissingAnalyzerDependency>.Empty);

        public AnalyzerDependencyResults(ImmutableArray<AnalyzerDependencyConflict> conflicts, ImmutableArray<MissingAnalyzerDependency> missingDependencies)
        {
            Debug.Assert(conflicts != default);
            Debug.Assert(missingDependencies != default);

            Conflicts = conflicts;
            MissingDependencies = missingDependencies;
        }

        public ImmutableArray<AnalyzerDependencyConflict> Conflicts { get; }
        public ImmutableArray<MissingAnalyzerDependency> MissingDependencies { get; }
    }
}
