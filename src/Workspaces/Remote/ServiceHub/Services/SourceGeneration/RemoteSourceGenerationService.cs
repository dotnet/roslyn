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
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SourceGeneration;
using Microsoft.CodeAnalysis.SourceGeneratorTelemetry;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

// Can use AnalyzerReference as a key here as we will will always get back the same instance back for the same checksum.
using AnalyzerReferenceMap = ConditionalWeakTable<AnalyzerReference, StrongBox<SourceGeneratorPresence>>;

internal sealed partial class RemoteSourceGenerationService(in BrokeredServiceBase.ServiceConstructionArguments arguments)
    : BrokeredServiceBase(arguments), IRemoteSourceGenerationService
{
    internal sealed class Factory : FactoryBase<IRemoteSourceGenerationService>
    {
        protected override IRemoteSourceGenerationService CreateService(in ServiceConstructionArguments arguments)
            => new RemoteSourceGenerationService(arguments);
    }

    public ValueTask<ImmutableArray<SourceGeneratedDocumentInfo>> GetSourceGeneratedDocumentInfoAsync(
        Checksum solutionChecksum, ProjectId projectId, bool withFrozenSourceGeneratedDocuments, CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var project = solution.GetRequiredProject(projectId);
            var documentStates = await solution.CompilationState.GetSourceGeneratedDocumentStatesAsync(
                project.State, withFrozenSourceGeneratedDocuments, cancellationToken).ConfigureAwait(false);

            var result = new FixedSizeArrayBuilder<SourceGeneratedDocumentInfo>(documentStates.States.Count);
            foreach (var (id, state) in documentStates.States)
            {
                Contract.ThrowIfFalse(id.IsSourceGenerated);
                result.Add(new(state.Identity, state.GetContentIdentity(), state.GenerationDateTime));
            }

            return result.MoveToImmutable();
        }, cancellationToken);
    }

    public ValueTask<ImmutableArray<string>> GetContentsAsync(
        Checksum solutionChecksum, ProjectId projectId, ImmutableArray<DocumentId> documentIds, bool withFrozenSourceGeneratedDocuments, CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var project = solution.GetRequiredProject(projectId);
            var documentStates = await solution.CompilationState.GetSourceGeneratedDocumentStatesAsync(
                project.State, withFrozenSourceGeneratedDocuments, cancellationToken).ConfigureAwait(false);

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
        { LanguageNames.CSharp, (new(), static analyzerReference => GetSourceGeneratorPresence(analyzerReference, LanguageNames.CSharp)) },
        { LanguageNames.VisualBasic, (new(), static analyzerReference => GetSourceGeneratorPresence(analyzerReference, LanguageNames.VisualBasic)) },
    };

    private static StrongBox<SourceGeneratorPresence> GetSourceGeneratorPresence(
        AnalyzerReference analyzerReference, string language)
    {
        var generators = analyzerReference.GetGenerators(language);
        return new(generators.GetSourceGeneratorPresence());
    }

    public async ValueTask<SourceGeneratorPresence> GetSourceGeneratorPresenceAsync(
        Checksum solutionChecksum,
        ProjectId projectId,
        ImmutableArray<Checksum> analyzerReferenceChecksums,
        string language,
        CancellationToken cancellationToken)
    {
        if (analyzerReferenceChecksums.Length == 0)
            return SourceGeneratorPresence.NoSourceGenerators;

        // Do not use RunServiceAsync here.  We don't want to actually synchronize a solution instance on this remote
        // side to service this request.  Specifically, solution syncing is expensive, and will pull over a lot of data
        // that we don't need (like document contents).  All we need to do is synchronize over the analyzer-references
        // (which are actually quite small as they are represented as file-paths), and then answer the question based on
        // them directly.  We can then cache that result for future requests.
        var workspace = GetWorkspace();
        var assetProvider = workspace.CreateAssetProvider(solutionChecksum, WorkspaceManager.SolutionAssetCache, SolutionAssetSource);

        // Fetch the analyzer references specified by the host.  Note: this will only serialize this information over
        // the first time needed. After that, it will be cached in the WorkspaceManager.SolutionAssetCache on the remote
        // side, so it will be a no-op to fetch them in the future.
        //
        // From this point on, the host won't call into us for the same project-state (as it caches the data itself). If
        // the project state changes, it will just call into us with the checksums for its analyzer references.  As
        // those will almost always be the same, we'll just fetch the precomputed values on our end, return them, and
        // the host will cache it.  We'll only actually fetch something new and compute something new when an actual new
        // analyzer reference is added.

        var checksumCollection = new ChecksumCollection(analyzerReferenceChecksums);

        // Make sure the analyzer references are loaded into an isolated ALC so that we can properly load them if
        // they're a new version of some analyzer reference we've already loaded.
        var isolatedReferences = await IsolatedAnalyzerReferenceSet.CreateIsolatedAnalyzerReferencesAsync(
            useAsync: true,
            checksumCollection,
            workspace.Services.SolutionServices,
            () => assetProvider.GetAssetsArrayAsync<AnalyzerReference>(projectId, checksumCollection, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        // check through each reference to see if we have source generators, and if any of them are required
        var hasOptionalGenerators = false;
        var (analyzerReferenceMap, callback) = s_languageToAnalyzerReferenceMap[language];
        foreach (var analyzerReference in isolatedReferences)
        {
            var generatorPresence = analyzerReferenceMap.GetValue(analyzerReference, callback).Value;

            // we have at least one required generator, so no need to check the others
            if (generatorPresence is SourceGeneratorPresence.ContainsRequiredSourceGenerators)
                return SourceGeneratorPresence.ContainsRequiredSourceGenerators;

            // if we have optional generators, make a note of it,
            // but we still need to scan the rest to see if they have any required ones
            if (generatorPresence is SourceGeneratorPresence.OnlyOptionalSourceGenerators)
                hasOptionalGenerators = true;
        }

        // we found no required generators, did we find any optional ones?
        return hasOptionalGenerators
            ? SourceGeneratorPresence.OnlyOptionalSourceGenerators
            : SourceGeneratorPresence.NoSourceGenerators;
    }

    public ValueTask<ImmutableArray<SourceGeneratorIdentity>> GetSourceGeneratorIdentitiesAsync(
        Checksum solutionChecksum,
        ProjectId projectId,
        string analyzerReferenceFullPath,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var project = solution.GetRequiredProject(projectId);
            var analyzerReference = project.AnalyzerReferences
                .First(r => r.FullPath == analyzerReferenceFullPath);

            return SourceGeneratorIdentity.GetIdentities(analyzerReference, project.Language);
        }, cancellationToken);
    }

    public ValueTask<bool> HasAnalyzersOrSourceGeneratorsAsync(
        Checksum solutionChecksum,
        ProjectId projectId,
        string analyzerReferenceFullPath,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var project = solution.GetRequiredProject(projectId);
            var analyzerReference = project.AnalyzerReferences
                .First(r => r.FullPath == analyzerReferenceFullPath);

            return analyzerReference.HasAnalyzersOrSourceGenerators(project.Language);
        }, cancellationToken);
    }

    public async ValueTask<ImmutableArray<ImmutableDictionary<string, object?>>> FetchAndClearTelemetryKeyValuePairsAsync(CancellationToken _)
    {
        var workspaceService = GetWorkspaceServices().GetRequiredService<ISourceGeneratorTelemetryCollectorWorkspaceService>();
        return workspaceService.FetchKeysAndAndClear();
    }
}
