// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SourceGeneration;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

// Can use AnalyzerReference as a key here as we will will always get back the same instance back for the same checksum.
using AnalyzerReferenceMap = ConditionalWeakTable<AnalyzerReference, StrongBox<bool>>;

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

            var result = new FixedSizeArrayBuilder<(SourceGeneratedDocumentIdentity documentIdentity, SourceGeneratedDocumentContentIdentity contentIdentity, DateTime generationDateTime)>(documentStates.States.Count);
            foreach (var (id, state) in documentStates.States)
            {
                Contract.ThrowIfFalse(id.IsSourceGenerated);
                result.Add((state.Identity, state.GetContentIdentity(), state.GenerationDateTime));
            }

            return result.MoveToImmutable();
        }, cancellationToken);
    }

    public ValueTask<ImmutableArray<string>> GetContentsAsync(
        Checksum solutionChecksum, ProjectId projectId, ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var project = solution.GetRequiredProject(projectId);
            var documentStates = await solution.CompilationState.GetSourceGeneratedDocumentStatesAsync(project.State, cancellationToken).ConfigureAwait(false);

            var result = new FixedSizeArrayBuilder<string>(documentIds.Length);
            foreach (var id in documentIds)
            {
                Contract.ThrowIfFalse(id.IsSourceGenerated);
                var state = documentStates.GetRequiredState(id);
                var text = await state.GetTextAsync(cancellationToken).ConfigureAwait(false);
                result.Add(text.ToString());
            }

            return result.MoveToImmutable();
        }, cancellationToken);
    }

    private static readonly Dictionary<string, (AnalyzerReferenceMap analyzerReferenceMap, AnalyzerReferenceMap.CreateValueCallback callback)> s_languageToAnalyzerReferenceMap = new()
    {
        { LanguageNames.CSharp, (new(), static analyzerReference => HasSourceGenerators(analyzerReference, LanguageNames.CSharp)) },
        { LanguageNames.VisualBasic, (new(), static analyzerReference => HasSourceGenerators(analyzerReference, LanguageNames.VisualBasic)) },
    };

    private static StrongBox<bool> HasSourceGenerators(
        AnalyzerReference analyzerReference, string language)
    {
        var generators = analyzerReference.GetGenerators(language);
        return new(generators.Any());
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

        // Do not use RunServiceAsync here.  We don't want to actually synchronize a solution instance on this remote
        // side to service this request.  Specifically, solution syncing is expensive, and will pull over a lot of data
        // that we don't need (like document contents).  All we need to do is synchronize over the analyzer-references
        // (which are actually quite small as they are represented as file-paths), and then answer the question based on
        // them directly.  We can then cache that result for future requests.
        var workspace = GetWorkspace();
        var assetProvider = workspace.CreateAssetProvider(solutionChecksum, WorkspaceManager.SolutionAssetCache, SolutionAssetSource);

        using var _1 = PooledHashSet<Checksum>.GetInstance(out var checksums);
        checksums.AddRange(analyzerReferenceChecksums);

        // Fetch the analyzer references specified by the host.  Note: this will only serialize this information over
        // the first time needed. After that, it will be cached in the WorkspaceManager.SolutionAssetCache on the remote
        // side, so it will be a no-op to fetch them in the future.
        //
        // From this point on, the host won't call into us for the same project-state (as it caches the data itself). If
        // the project state changes, it will just call into us with the checksums for its analyzer references.  As
        // those will almost always be the same, we'll just fetch the precomputed values on our end, return them, and
        // the host will cache it.  We'll only actually fetch something new and compute something new when an actual new
        // analyzer reference is added.
        using var _2 = ArrayBuilder<AnalyzerReference>.GetInstance(checksums.Count, out var analyzerReferences);
        await assetProvider.GetAssetHelper<AnalyzerReference>().GetAssetsAsync(
            projectId,
            checksums,
            static (_, analyzerReference, analyzerReferences) => analyzerReferences.Add(analyzerReference),
            analyzerReferences,
            cancellationToken).ConfigureAwait(false);

        var (analyzerReferenceMap, callback) = s_languageToAnalyzerReferenceMap[language];
        foreach (var analyzerReference in analyzerReferences)
        {
            var hasGenerators = analyzerReferenceMap.GetValue(analyzerReference, callback);
            if (hasGenerators.Value)
                return true;
        }

        return false;
    }
}
