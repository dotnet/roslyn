// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        private sealed class ProjectAndCompilationWithAnalyzers
        {
            public Project Project { get; }
            public CompilationWithAnalyzers? CompilationWithAnalyzers { get; }

            public ProjectAndCompilationWithAnalyzers(Project project, CompilationWithAnalyzers? compilationWithAnalyzers)
            {
                Project = project;
                CompilationWithAnalyzers = compilationWithAnalyzers;
            }
        }
    }
}
