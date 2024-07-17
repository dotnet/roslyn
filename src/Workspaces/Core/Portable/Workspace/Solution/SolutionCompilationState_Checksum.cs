// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal partial class SolutionCompilationState
{
    /// <summary>
    /// Checksum representing the full checksum tree for this solution compilation state.  Includes the checksum for
    /// <see cref="SolutionState"/>, as well as the checksums for <see cref="FrozenSourceGeneratedDocumentStates"/>
    /// if present.
    /// </summary>
    private readonly AsyncLazy<SolutionCompilationStateChecksums> _lazyChecksums;

    /// <summary>
    /// Mapping from project-id to the checksums needed to synchronize it over to an OOP host.  Lock this specific
    /// field before reading/writing to it.
    /// </summary>
    private readonly Dictionary<ProjectId, AsyncLazy<(SolutionCompilationStateChecksums checksums, ProjectCone projectCone)>> _lazyProjectChecksums = [];

    public bool TryGetStateChecksums([NotNullWhen(true)] out SolutionCompilationStateChecksums? stateChecksums)
        => _lazyChecksums.TryGetValue(out stateChecksums);

    public bool TryGetStateChecksums(ProjectId projectId, [NotNullWhen(true)] out SolutionCompilationStateChecksums? stateChecksums)
    {
        AsyncLazy<(SolutionCompilationStateChecksums checksums, ProjectCone projectCone)>? lazyChecksums;
        lock (_lazyProjectChecksums)
        {
            if (!_lazyProjectChecksums.TryGetValue(projectId, out lazyChecksums) ||
                lazyChecksums == null)
            {
                stateChecksums = null;
                return false;
            }
        }

        if (!lazyChecksums.TryGetValue(out var checksumsAndCone))
        {
            stateChecksums = null;
            return false;
        }

        stateChecksums = checksumsAndCone.checksums;
        return true;
    }

    public Task<SolutionCompilationStateChecksums> GetStateChecksumsAsync(CancellationToken cancellationToken)
        => _lazyChecksums.GetValueAsync(cancellationToken);

    public async Task<Checksum> GetChecksumAsync(CancellationToken cancellationToken)
    {
        var collection = await GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
        return collection.Checksum;
    }

    /// <summary>Gets the checksum for only the requested project (and any project it depends on)</summary>
    public async Task<(SolutionCompilationStateChecksums checksums, ProjectCone projectCone)> GetStateChecksumsAsync(
        ProjectId projectId,
        CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(projectId);

        AsyncLazy<(SolutionCompilationStateChecksums checksums, ProjectCone projectCone)>? checksums;
        lock (_lazyProjectChecksums)
        {
            if (!_lazyProjectChecksums.TryGetValue(projectId, out checksums))
            {
                checksums = AsyncLazy.Create(asynchronousComputeFunction: static async (arg, cancellationToken) =>
                {
                    var (checksum, projectCone) = await arg.self.ComputeChecksumsAsync(arg.projectId, cancellationToken).ConfigureAwait(false);
                    Contract.ThrowIfNull(projectCone);
                    return (checksum, projectCone);
                }, arg: (self: this, projectId));

                _lazyProjectChecksums.Add(projectId, checksums);
            }
        }

        var collection = await checksums.GetValueAsync(cancellationToken).ConfigureAwait(false);
        return collection;
    }

    /// <summary>Gets the checksum for only the requested project (and any project it depends on)</summary>
    public async Task<Checksum> GetChecksumAsync(ProjectId projectId, CancellationToken cancellationToken)
    {
        var (checksums, _) = await GetStateChecksumsAsync(projectId, cancellationToken).ConfigureAwait(false);
        return checksums.Checksum;
    }

    private async Task<(SolutionCompilationStateChecksums checksums, ProjectCone? projectCone)> ComputeChecksumsAsync(
        ProjectId? projectId,
        CancellationToken cancellationToken)
    {
        try
        {
            using (Logger.LogBlock(FunctionId.SolutionCompilationState_ComputeChecksumsAsync, this.SolutionState.FilePath, cancellationToken))
            {
                Checksum solutionStateChecksum;
                ProjectCone? projectCone;

                if (projectId is null)
                {
                    solutionStateChecksum = await this.SolutionState.GetChecksumAsync(cancellationToken).ConfigureAwait(false);
                    projectCone = null;
                }
                else
                {
                    var stateChecksums = await this.SolutionState.GetStateChecksumsAsync(projectId, cancellationToken).ConfigureAwait(false);
                    solutionStateChecksum = stateChecksums.Checksum;
                    projectCone = stateChecksums.ProjectCone;
                }

                ChecksumCollection? frozenSourceGeneratedDocumentIdentities = null;
                DocumentChecksumsAndIds? frozenSourceGeneratedDocumentTexts = null;
                ImmutableArray<DateTime> frozenSourceGeneratedDocumentGenerationDateTimes = default;

                if (FrozenSourceGeneratedDocumentStates != null)
                {
                    var serializer = this.SolutionState.Services.GetRequiredService<ISerializerService>();
                    var identityChecksums = FrozenSourceGeneratedDocumentStates.SelectAsArray(
                        selector: static (s, arg) => arg.serializer.CreateChecksum(s.Identity, cancellationToken: arg.cancellationToken), arg: (serializer, cancellationToken));

                    frozenSourceGeneratedDocumentTexts = await FrozenSourceGeneratedDocumentStates.GetDocumentChecksumsAndIdsAsync(cancellationToken).ConfigureAwait(false);
                    frozenSourceGeneratedDocumentIdentities = new ChecksumCollection(identityChecksums);
                    frozenSourceGeneratedDocumentGenerationDateTimes = FrozenSourceGeneratedDocumentStates.SelectAsArray(d => d.GenerationDateTime);
                }

                // Ensure we only send the execution map over for projects in the project cone.
                var versionMapChecksum = this.GetFilteredSourceGenerationExecutionMap(projectCone).GetChecksum();

                var compilationStateChecksums = new SolutionCompilationStateChecksums(
                    solutionStateChecksum,
                    versionMapChecksum,
                    frozenSourceGeneratedDocumentTexts,
                    frozenSourceGeneratedDocumentIdentities,
                    frozenSourceGeneratedDocumentGenerationDateTimes);
                return (compilationStateChecksums, projectCone);
            }
        }
        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    public SourceGeneratorExecutionVersionMap GetFilteredSourceGenerationExecutionMap(ProjectCone? projectCone)
    {
        var builder = this.SourceGeneratorExecutionVersionMap.Map.ToBuilder();

        foreach (var (projectId, projectState) in this.SolutionState.ProjectStates)
        {
            if (!RemoteSupportedLanguages.IsSupported(projectState.Language))
            {
                builder.Remove(projectId);
            }
            else if (projectCone != null && !projectCone.Contains(projectId))
            {
                builder.Remove(projectId);
            }
        }

        if (builder.Count == this.SourceGeneratorExecutionVersionMap.Map.Count)
            return this.SourceGeneratorExecutionVersionMap;

        return new SourceGeneratorExecutionVersionMap(builder.ToImmutable());
    }
}
