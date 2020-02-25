﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;

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
            if (_map.TryGetValue(workspace, out var analyzer))
            {
                return analyzer.SynchronizeWithBuildAsync(diagnostics);
            }

            return Task.CompletedTask;
        }
    }
}
