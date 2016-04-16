// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV1
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        /// <summary>
        /// EventArgs for <see cref="StateManager.ProjectAnalyzerReferenceChanged"/>
        /// 
        /// this event args contains information such as <see cref="Project"/> the <see cref="AnalyzerReference"/> has changed
        /// and what <see cref="StateSet"/> has changed.
        /// </summary>
        private class ProjectAnalyzerReferenceChangedEventArgs : EventArgs
        {
            public readonly Project Project;
            public readonly ImmutableArray<StateSet> Added;
            public readonly ImmutableArray<StateSet> Removed;

            public ProjectAnalyzerReferenceChangedEventArgs(Project project, ImmutableArray<StateSet> added, ImmutableArray<StateSet> removed)
            {
                Project = project;
                Added = added;
                Removed = removed;
            }
        }
    }
}
