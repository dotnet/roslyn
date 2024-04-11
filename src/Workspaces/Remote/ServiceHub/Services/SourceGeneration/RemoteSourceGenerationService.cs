// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SourceGeneration;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

using AnalyzerReferenceMap = ConditionalWeakTable<AnalyzerReference, AsyncLazy<bool>>;

internal sealed partial class RemoteSourceGenerationService(in BrokeredServiceBase.ServiceConstructionArguments arguments)
    : BrokeredServiceBase(arguments), IRemoteSourceGenerationService
{
    internal sealed class Factory : FactoryBase<IRemoteSourceGenerationService>
    {
        protected override IRemoteSourceGenerationService CreateService(in ServiceConstructionArguments arguments)
            => new RemoteSourceGenerationService(arguments);
    }

    public ValueTask<ImmutableArray<(SourceGeneratedDocumentIdentity documentIdentity, SourceGeneratedDocumentContentIdentity contentIdentity, DateTime generationDateTime)>> GetSourceGenerationInfoAsync(
        Checksum solutionChecksum, ProjectId projectId, CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var project = solution.GetRequiredProject(projectId);
            var documentStates = await solution.CompilationState.GetSourceGeneratedDocumentStatesAsync(project.State, cancellationToken).ConfigureAwait(false);

            using var _ = ArrayBuilder<(SourceGeneratedDocumentIdentity documentIdentity, SourceGeneratedDocumentContentIdentity contentIdentity, DateTime generationDateTime)>.GetInstance(documentStates.Ids.Count, out var result);

            foreach (var (id, state) in documentStates.States)
            {
                Contract.ThrowIfFalse(id.IsSourceGenerated);
                result.Add((state.Identity, state.GetContentIdentity(), state.GenerationDateTime));
            }

            return result.ToImmutableAndClear();
        }, cancellationToken);
    }

    public ValueTask<ImmutableArray<string>> GetContentsAsync(
        Checksum solutionChecksum, ProjectId projectId, ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var project = solution.GetRequiredProject(projectId);
            var documentStates = await solution.CompilationState.GetSourceGeneratedDocumentStatesAsync(project.State, cancellationToken).ConfigureAwait(false);

            using var _ = ArrayBuilder<string>.GetInstance(documentIds.Length, out var result);

            foreach (var id in documentIds)
            {
                Contract.ThrowIfFalse(id.IsSourceGenerated);
                var state = documentStates.GetRequiredState(id);
                var text = await state.GetTextAsync(cancellationToken).ConfigureAwait(false);
                result.Add(text.ToString());
            }

            return result.ToImmutableAndClear();
        }, cancellationToken);
    }

    private static readonly ImmutableArray<(string language, AnalyzerReferenceMap analyzerReferenceMap, AnalyzerReferenceMap.CreateValueCallback callback)> s_languageToAnalyzerReferenceMap =
    [
        (LanguageNames.CSharp, new(), static analyzerReference => AsyncLazy.Create(cancellationToken => HasSourceGeneratorsAsync(analyzerReference, LanguageNames.CSharp, cancellationToken))),
        (LanguageNames.VisualBasic, new(), static analyzerReference => AsyncLazy.Create(cancellationToken => HasSourceGeneratorsAsync(analyzerReference, LanguageNames.VisualBasic, cancellationToken)))
    ];

    private static async Task<object> HasSourceGeneratorsAsync(
        AnalyzerReference analyzerReference, string language, CancellationToken cancellationToken)
    {
        var generators = analyzerReference.GetGenerators(langauge);
        return generators.Any();
    }

    public async ValueTask<bool> HasGeneratorsAsync(
        Checksum solutionChecksum,
        ProjectId projectId,
        ImmutableArray<Checksum> analyzerReferenceChecksums,
        string language,
        CancellationToken cancellationToken)
    {
        if (analyzerReferenceChecksums.Length == 0)
            return false;

        var workspace = GetWorkspace();
        var assetProvider = workspace.CreateAssetProvider(solutionChecksum, WorkspaceManager.SolutionAssetCache, SolutionAssetSource);

        using var _1 = PooledHashSet<Checksum>.GetInstance(out var checksums);
        checksums.AddRange(analyzerReferenceChecksums);

        // Fetch the analyzer references specified by the host.  Note: this will only serialize this information over
        // the first time needed. After that, it will be cached in the WorkspaceManager.SolutionAssetCache on the remote
        // side, so it will be a no-op to fetch them in the future.
        using var _2 = ArrayBuilder<AnalyzerReference>.GetInstance(checksums.Count, out var analyzerReferences);
        await assetProvider.GetAssetsAsync<AnalyzerReference, ArrayBuilder<AnalyzerReference>>(
            projectId,
            checksums,
            static (_, analyzerReference, analyzerReferences) => analyzerReferences.Add(analyzerReference),
            analyzerReferences,
            cancellationToken).ConfigureAwait(false);

        var tuple = s_languageToAnalyzerReferenceMap.Single(static (val, language) => val.language == language, language);
        var analyzerReferenceMap = tuple.analyzerReferenceMap;
        var callback = tuple.callback;

        foreach (var analyzerReference in analyzerReferences)
        {
            var hasGeneratorsLazy = analyzerReferenceMap.GetValue(analyzerReference, callback);
            if (await hasGeneratorsLazy.GetValueAsync(cancellationToken).ConfigureAwait(false))
                return true;
        }

        return false;
    }
}
