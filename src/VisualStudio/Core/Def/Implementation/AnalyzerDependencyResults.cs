// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal sealed class AnalyzerDependencyResults
    {
        public static readonly AnalyzerDependencyResults Empty = new AnalyzerDependencyResults(ImmutableArray<AnalyzerDependencyConflict>.Empty, ImmutableArray<MissingAnalyzerDependency>.Empty);

        public AnalyzerDependencyResults(ImmutableArray<AnalyzerDependencyConflict> conflicts, ImmutableArray<MissingAnalyzerDependency> missingDependencies)
        {
            Debug.Assert(conflicts != default(ImmutableArray<AnalyzerDependencyConflict>));
            Debug.Assert(missingDependencies != default(ImmutableArray<MissingAnalyzerDependency>));

            Conflicts = conflicts;
            MissingDependencies = missingDependencies;
        }

        public ImmutableArray<AnalyzerDependencyConflict> Conflicts { get; }
        public ImmutableArray<MissingAnalyzerDependency> MissingDependencies { get; }
    }
}
