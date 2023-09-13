// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SourceGeneration;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed partial class RemoteSourceGenerationService(in BrokeredServiceBase.ServiceConstructionArguments arguments)
    : BrokeredServiceBase(arguments), IRemoteSourceGenerationService
{
    internal sealed class Factory : FactoryBase<IRemoteSourceGenerationService>
    {
        protected override IRemoteSourceGenerationService CreateService(in ServiceConstructionArguments arguments)
            => new RemoteSourceGenerationService(arguments);
    }

    public ValueTask<ImmutableArray<(SourceGeneratedDocumentIdentity identity, Checksum checksum)>> GetSourceGenerationInfoAsync(
        Checksum solutionChecksum, ProjectId projectId, CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var project = solution.GetRequiredProject(projectId);
            var documentStates = await solution.State.GetSourceGeneratedDocumentStatesAsync(project.State, cancellationToken).ConfigureAwait(false);

            using var _ = ArrayBuilder<(SourceGeneratedDocumentIdentity identity, Checksum checksum)>.GetInstance(out var result);

            foreach (var id in documentStates.Ids)
            {
                var state = documentStates.GetRequiredState(id);
                result.Add((state.Identity, state.GetTextChecksum()));
            }

            return result.ToImmutableAndClear();
        }, cancellationToken);
    }

    public ValueTask<ImmutableArray<(string contents, string? encodingName, SourceHashAlgorithm checksumAlgorithm)>> GetContentsAsync(
        Checksum solutionChecksum, ProjectId projectId, ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var project = solution.GetRequiredProject(projectId);
            var documentStates = await solution.State.GetSourceGeneratedDocumentStatesAsync(project.State, cancellationToken).ConfigureAwait(false);

            using var _ = ArrayBuilder<(string contents, string? encodingName, SourceHashAlgorithm checksumAlgorithm)>.GetInstance(out var result);

            foreach (var id in documentIds)
            {
                var state = documentStates.GetRequiredState(id);
                var text = await state.GetTextAsync(cancellationToken).ConfigureAwait(false);
                result.Add((text.ToString(), text.Encoding?.WebName, text.ChecksumAlgorithm));
            }

            return result.ToImmutableAndClear();
        }, cancellationToken);
    }
}
