// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

public partial class Workspace
{
    /// <summary>
    /// Used for batching up a lot of events and only combining them into a single request to update generators.  The
    /// <see cref="ProjectId"/> represents the projects that have changed, and which need their source-generators
    /// re-run.  <see langword="null"/> in the list indicates the entire solution has changed and all generators need to
    /// be rerun.  The <see cref="bool"/> represents if source generators should be fully rerun for the requested
    /// project or solution.  If <see langword="false"/>, the existing generator driver will be used, which may result
    /// in no actual changes to emitted source (as the driver may decide no inputs changed, and thus all outputs should
    /// be reused).  If <see langword="true"/>, the existing driver will be dropped, forcing all generation to be redone.
    /// </summary>
    private readonly AsyncBatchingWorkQueue<(ProjectId? projectId, bool forceRegeneration)> _updateSourceGeneratorsQueue;

    private readonly CancellationTokenSource _updateSourceGeneratorsQueueTokenSource = new();

    internal void EnqueueUpdateSourceGeneratorVersion(ProjectId? projectId, bool forceRegeneration)
        => _updateSourceGeneratorsQueue.AddWork((projectId, forceRegeneration));

    private async ValueTask ProcessUpdateSourceGeneratorRequestAsync(
        ImmutableSegmentedList<(ProjectId? projectId, bool forceRegeneration)> projectIds, CancellationToken cancellationToken)
    {
        var configuration = this.Services.GetRequiredService<IWorkspaceConfigurationService>().Options;
        if (configuration.SourceGeneratorExecution is SourceGeneratorExecutionPreference.Automatic)
        {
            // If we're in automatic mode, we don't need to do anything *unless* the host has asked us to
            // force-regenerate something.  In that case we're literally going to drop our generator drivers and
            // regenerate the code, so we can't depend on automatic running generators normally.
            if (!projectIds.Any(t => t.forceRegeneration))
                return;
        }

        await this.SetCurrentSolutionAsync(
            useAsync: true,
            oldSolution =>
            {
                var updates = SolutionCompilationState.GetUpdatedSourceGeneratorVersions(oldSolution.CompilationState, projectIds);
                return oldSolution.UpdateSpecificSourceGeneratorExecutionVersions(updates);
            },
            static (_, _) => (WorkspaceChangeKind.SolutionChanged, projectId: null, documentId: null),
            onBeforeUpdate: null,
            onAfterUpdate: null,
            cancellationToken).ConfigureAwait(false);

        return;
    }
}
