// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
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
    private readonly Dictionary<ProjectId, AsyncLazy<SolutionCompilationStateChecksums>> _lazyProjectChecksums = [];

    public bool TryGetStateChecksums([NotNullWhen(true)] out SolutionCompilationStateChecksums? stateChecksums)
        => _lazyChecksums.TryGetValue(out stateChecksums);

    public bool TryGetStateChecksums(ProjectId projectId, [NotNullWhen(true)] out SolutionCompilationStateChecksums? stateChecksums)
    {
        AsyncLazy<SolutionCompilationStateChecksums>? checksums;
        lock (_lazyProjectChecksums)
        {
            if (!_lazyProjectChecksums.TryGetValue(projectId, out checksums) ||
                checksums == null)
            {
                stateChecksums = null;
                return false;
            }
        }

        return checksums.TryGetValue(out stateChecksums);
    }

    public Task<SolutionCompilationStateChecksums> GetStateChecksumsAsync(CancellationToken cancellationToken)
        => _lazyChecksums.GetValueAsync(cancellationToken);

    public async Task<Checksum> GetChecksumAsync(CancellationToken cancellationToken)
    {
        var collection = await GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
        return collection.Checksum;
    }

    /// <summary>Gets the checksum for only the requested project (and any project it depends on)</summary>
    public async Task<SolutionCompilationStateChecksums> GetStateChecksumsAsync(
        ProjectId projectId,
        CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(projectId);

        AsyncLazy<SolutionCompilationStateChecksums>? checksums;
        lock (_lazyProjectChecksums)
        {
            if (!_lazyProjectChecksums.TryGetValue(projectId, out checksums))
            {
                checksums = AsyncLazy.Create(static (arg, c) =>
                    arg.self.ComputeChecksumsAsync(arg.projectId, c),
                    arg: (self: this, projectId));

                _lazyProjectChecksums.Add(projectId, checksums);
            }
        }

        var collection = await checksums.GetValueAsync(cancellationToken).ConfigureAwait(false);
        return collection;
    }

    /// <summary>Gets the checksum for only the requested project (and any project it depends on)</summary>
    public async Task<Checksum> GetChecksumAsync(ProjectId projectId, CancellationToken cancellationToken)
    {
        var checksums = await GetStateChecksumsAsync(projectId, cancellationToken).ConfigureAwait(false);
        return checksums.Checksum;
    }

    private async Task<SolutionCompilationStateChecksums> ComputeChecksumsAsync(
        ProjectId? projectId,
        CancellationToken cancellationToken)
    {
        try
        {
            using (Logger.LogBlock(FunctionId.SolutionCompilationState_ComputeChecksumsAsync, this.SolutionState.FilePath, cancellationToken))
            {
                var solutionStateChecksum = projectId == null
                    ? await this.SolutionState.GetChecksumAsync(cancellationToken).ConfigureAwait(false)
                    : await this.SolutionState.GetChecksumAsync(projectId, cancellationToken).ConfigureAwait(false);

                ChecksumCollection? frozenSourceGeneratedDocumentIdentities = null;
                ChecksumsAndIds<DocumentId>? frozenSourceGeneratedDocuments = null;

                if (FrozenSourceGeneratedDocumentStates.HasValue)
                {
                    var serializer = this.SolutionState.Services.GetRequiredService<ISerializerService>();
                    var identityChecksums = FrozenSourceGeneratedDocumentStates.Value
                        .SelectAsArray(static (s, arg) => arg.serializer.CreateChecksum(s.Identity, cancellationToken: arg.cancellationToken), (serializer, cancellationToken));
                    frozenSourceGeneratedDocumentIdentities = new ChecksumCollection(identityChecksums);
                    frozenSourceGeneratedDocuments = await FrozenSourceGeneratedDocumentStates.Value.GetChecksumsAndIdsAsync(cancellationToken).ConfigureAwait(false);
                }

                return new SolutionCompilationStateChecksums(
                    solutionStateChecksum,
                    frozenSourceGeneratedDocumentIdentities,
                    frozenSourceGeneratedDocuments);
            }
        }
        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }
}
