// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal partial class DiagnosticAnalyzerService
    {
        /// <summary>
        /// Synchronize build errors with live error.
        /// 
        /// no cancellationToken since this can't be cancelled
        /// </summary>
        public Task SynchronizeWithBuildAsync(Project project, ImmutableArray<DiagnosticData> diagnostics)
        {
            BaseDiagnosticIncrementalAnalyzer analyzer;
            if (_map.TryGetValue(project.Solution.Workspace, out analyzer))
            {
                return analyzer.SynchronizeWithBuildAsync(project, diagnostics);
            }

            return SpecializedTasks.EmptyTask;
        }

        /// <summary>
        /// Synchronize build errors with live error
        /// 
        /// no cancellationToken since this can't be cancelled
        /// </summary>
        public Task SynchronizeWithBuildAsync(Document document, ImmutableArray<DiagnosticData> diagnostics)
        {
            BaseDiagnosticIncrementalAnalyzer analyzer;
            if (_map.TryGetValue(document.Project.Solution.Workspace, out analyzer))
            {
                return analyzer.SynchronizeWithBuildAsync(document, diagnostics);
            }

            return SpecializedTasks.EmptyTask;
        }
    }
}
