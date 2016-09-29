﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public Task SynchronizeWithBuildAsync(Workspace workspace, ImmutableDictionary<ProjectId, ImmutableArray<DiagnosticData>> diagnostics)
        {
            BaseDiagnosticIncrementalAnalyzer analyzer;
            if (_map.TryGetValue(workspace, out analyzer))
            {
                return analyzer.SynchronizeWithBuildAsync(workspace, diagnostics);
            }

            return SpecializedTasks.EmptyTask;
        }
    }
}
