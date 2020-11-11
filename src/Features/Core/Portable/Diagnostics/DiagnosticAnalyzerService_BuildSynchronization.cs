// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal partial class DiagnosticAnalyzerService
    {
        /// <summary>
        /// Synchronize build errors with live error.
        /// </summary>
        public Task SynchronizeWithBuildAsync(
            Workspace workspace,
            ImmutableDictionary<ProjectId,
            ImmutableArray<DiagnosticData>> diagnostics,
            TaskQueue postBuildAndErrorListRefreshTaskQueue,
            bool onBuildCompleted,
            CancellationToken cancellationToken)
        {
            if (_map.TryGetValue(workspace, out var analyzer))
            {
                return analyzer.SynchronizeWithBuildAsync(diagnostics, postBuildAndErrorListRefreshTaskQueue, onBuildCompleted, cancellationToken);
            }

            return Task.CompletedTask;
        }
    }
}
