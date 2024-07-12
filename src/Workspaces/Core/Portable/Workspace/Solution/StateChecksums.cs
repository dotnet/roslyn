// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization;

internal sealed class SolutionCompilationStateChecksums
{
    public SolutionCompilationStateChecksums(
        Checksum solutionState,
        Checksum sourceGeneratorExecutionVersionMap,
        // These arrays are all the same length if present, and reference the same documents in the same order.
        DocumentChecksumsAndIds? frozenSourceGeneratedDocuments,
        ChecksumCollection? frozenSourceGeneratedDocumentIdentities,
        ImmutableArray<DateTime> frozenSourceGeneratedDocumentGenerationDateTimes)
    {
        // For the frozen source generated document info, we expect two either have both checksum collections or neither, and they
        // should both be the same length as there is a 1:1 correspondence between them.
        Contract.ThrowIfFalse(frozenSourceGeneratedDocumentIdentities.HasValue == frozenSourceGeneratedDocuments.HasValue);
        Contract.ThrowIfFalse(frozenSourceGeneratedDocumentIdentities?.Count == frozenSourceGeneratedDocuments?.Length);

        SolutionState = solutionState;
        SourceGeneratorExecutionVersionMap = sourceGeneratorExecutionVersionMap;
        FrozenSourceGeneratedDocuments = frozenSourceGeneratedDocuments;
        FrozenSourceGeneratedDocumentIdentities = frozenSourceGeneratedDocumentIdentities;
        FrozenSourceGeneratedDocumentGenerationDateTimes = frozenSourceGeneratedDocumentGenerationDateTimes;

        // note: intentionally not mixing in FrozenSourceGeneratedDocumentGenerationDateTimes as that is not part of the
        // identity contract of this type.
        Checksum = Checksum.Create(
            SolutionState,
            SourceGeneratorExecutionVersionMap,
            FrozenSourceGeneratedDocumentIdentities?.Checksum ?? Checksum.Null,
            frozenSourceGeneratedDocuments?.Checksum ?? Checksum.Null);
    }

    public Checksum Checksum { get; }
    public Checksum SolutionState { get; }
    public Checksum SourceGeneratorExecutionVersionMap { get; }

    /// <summary>
    /// Checksums of the SourceTexts of the frozen documents directly.  Not checksums of their DocumentStates.
    /// </summary>
    public DocumentChecksumsAndIds? FrozenSourceGeneratedDocuments { get; }
    public ChecksumCollection? FrozenSourceGeneratedDocumentIdentities { get; }

    // note: intentionally not part of the identity contract of this type.
    public ImmutableArray<DateTime> FrozenSourceGeneratedDocumentGenerationDateTimes { get; }

    public void AddAllTo(HashSet<Checksum> checksums)
    {
        checksums.AddIfNotNullChecksum(this.Checksum);
        checksums.AddIfNotNullChecksum(this.SolutionState);
        checksums.AddIfNotNullChecksum(this.SourceGeneratorExecutionVersionMap);
        this.FrozenSourceGeneratedDocumentIdentities?.AddAllTo(checksums);
        this.FrozenSourceGeneratedDocuments?.AddAllTo(checksums);
    }

    public void Serialize(ObjectWriter writer)
    {
        // Writing this is optional, but helps ensure checksums are being computed properly on both the host and oop side.
        this.Checksum.WriteTo(writer);
        this.SolutionState.WriteTo(writer);
        this.SourceGeneratorExecutionVersionMap.WriteTo(writer);

        // Write out a boolean to know whether we'll have this extra information
        writer.WriteBoolean(this.FrozenSourceGeneratedDocumentIdentities.HasValue);
        if (FrozenSourceGeneratedDocumentIdentities.HasValue)
        {
            this.FrozenSourceGeneratedDocuments!.Value.WriteTo(writer);
            this.FrozenSourceGeneratedDocumentIdentities.Value.WriteTo(writer);
            writer.WriteArray(this.FrozenSourceGeneratedDocumentGenerationDateTimes, static (w, d) => w.WriteInt64(d.Ticks));
        }
    }

    public static SolutionCompilationStateChecksums Deserialize(ObjectReader reader)
    {
        var checksum = Checksum.ReadFrom(reader);
        var solutionState = Checksum.ReadFrom(reader);
        var sourceGeneratorExecutionVersionMap = Checksum.ReadFrom(reader);

        var hasFrozenSourceGeneratedDocuments = reader.ReadBoolean();
        DocumentChecksumsAndIds? frozenSourceGeneratedDocumentTexts = null;
        ChecksumCollection? frozenSourceGeneratedDocumentIdentities = null;
        ImmutableArray<DateTime> frozenSourceGeneratedDocumentGenerationDateTimes = default;

        if (hasFrozenSourceGeneratedDocuments)
        {
            frozenSourceGeneratedDocumentTexts = DocumentChecksumsAndIds.ReadFrom(reader);
            frozenSourceGeneratedDocumentIdentities = ChecksumCollection.ReadFrom(reader);
            frozenSourceGeneratedDocumentGenerationDateTimes = reader.ReadArray(r => new DateTime(r.ReadInt64()));
        }

        var result = new SolutionCompilationStateChecksums(
            solutionState: solutionState,
            sourceGeneratorExecutionVersionMap: sourceGeneratorExecutionVersionMap,
            frozenSourceGeneratedDocumentTexts,
            frozenSourceGeneratedDocumentIdentities,
            frozenSourceGeneratedDocumentGenerationDateTimes);
        Contract.ThrowIfFalse(result.Checksum == checksum);
        return result;
    }

    public async Task FindAsync<TArg>(
        SolutionCompilationState compilationState,
        ProjectCone? projectCone,
        AssetPath assetPath,
        HashSet<Checksum> searchingChecksumsLeft,
        Action<Checksum, object, TArg> onAssetFound,
        TArg arg,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (searchingChecksumsLeft.Count == 0)
            return;

        if (assetPath.IncludeSolutionCompilationState)
        {
            if (assetPath.IncludeSolutionCompilationStateChecksums && searchingChecksumsLeft.Remove(this.Checksum))
                onAssetFound(this.Checksum, this, arg);

            if (assetPath.IncludeSolutionSourceGeneratorExecutionVersionMap && searchingChecksumsLeft.Remove(this.SourceGeneratorExecutionVersionMap))
            {
                // Only send over the part of the execution map corresponding to the project cone.
                var filteredExecutionMap = compilationState.GetFilteredSourceGenerationExecutionMap(projectCone);
                onAssetFound(this.SourceGeneratorExecutionVersionMap, filteredExecutionMap, arg);
            }

            if (compilationState.FrozenSourceGeneratedDocumentStates != null)
            {
                Contract.ThrowIfFalse(FrozenSourceGeneratedDocumentIdentities.HasValue);
                Contract.ThrowIfFalse(FrozenSourceGeneratedDocuments.HasValue);

                // This could either be the checksum for the text (which we'll use our regular helper for first)...
                if (assetPath.IncludeSolutionFrozenSourceGeneratedDocumentText)
                {
                    await ChecksumCollection.FindAsync(
                        new AssetPath(AssetPathKind.DocumentText, assetPath.ProjectId, assetPath.DocumentId),
                        compilationState.FrozenSourceGeneratedDocumentStates, searchingChecksumsLeft, onAssetFound: onAssetFound, arg: arg, cancellationToken: cancellationToken).ConfigureAwait(false);
                }

                // ... or one of the identities. In this case, we'll use the fact that there's a 1:1 correspondence between the
                // two collections we hold onto.
                if (assetPath.IncludeSolutionFrozenSourceGeneratedDocumentIdentities)
                {
                    var documentId = assetPath.DocumentId;
                    if (documentId != null)
                    {
                        // If the caller is asking for a specific document, we can just look it up directly.
                        var index = FrozenSourceGeneratedDocuments.Value.Ids.IndexOf(documentId);
                        if (index >= 0)
                        {
                            var identityChecksum = FrozenSourceGeneratedDocumentIdentities.Value.Children[index];
                            if (searchingChecksumsLeft.Remove(identityChecksum))
                            {
                                Contract.ThrowIfFalse(compilationState.FrozenSourceGeneratedDocumentStates.TryGetState(documentId, out var state));
                                onAssetFound(identityChecksum, state.Identity, arg);
                            }
                        }
                    }
                    else
                    {
                        // Otherwise, we'll have to search through all of them.
                        for (var i = 0; i < FrozenSourceGeneratedDocumentIdentities.Value.Count; i++)
                        {
                            var identityChecksum = FrozenSourceGeneratedDocumentIdentities.Value[0];
                            if (searchingChecksumsLeft.Remove(identityChecksum))
                            {
                                var id = FrozenSourceGeneratedDocuments.Value.Ids[i];
                                Contract.ThrowIfFalse(compilationState.FrozenSourceGeneratedDocumentStates.TryGetState(id, out var state));
                                onAssetFound(identityChecksum, state.Identity, arg);
                            }
                        }
                    }
                }
            }
        }

        var solutionState = compilationState.SolutionState;
        if (projectCone is null)
        {
            // If we're not in a project cone, start the search at the top most state-checksum corresponding to the
            // entire solution.
            Contract.ThrowIfFalse(solutionState.TryGetStateChecksums(out var solutionChecksums));
            await solutionChecksums.FindAsync(solutionState, projectCone, assetPath, searchingChecksumsLeft, onAssetFound: onAssetFound, arg: arg, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // Otherwise, grab the top-most state checksum for this cone and search within that.
            Contract.ThrowIfFalse(solutionState.TryGetStateChecksums(projectCone.RootProjectId, out var solutionChecksums));
            await solutionChecksums.FindAsync(solutionState, projectCone, assetPath, searchingChecksumsLeft, onAssetFound: onAssetFound, arg: arg, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}

/// <param name="projectConeId">The particular <see cref="ProjectId"/> if this was a checksum tree made for a particular
/// project cone.</param>
internal sealed class SolutionStateChecksums(
    ProjectId? projectConeId,
    Checksum attributes,
    ProjectChecksumsAndIds projects,
    ChecksumCollection analyzerReferences,
    Checksum fallbackAnalyzerOptionsChecksum)
{
    private ProjectCone? _projectCone;

    public Checksum Checksum { get; } = Checksum.Create(stackalloc[]
    {
        projectConeId == null ? Checksum.Null : projectConeId.Checksum,
        attributes,
        projects.Checksum,
        analyzerReferences.Checksum,
        fallbackAnalyzerOptionsChecksum,
    });

    public ProjectId? ProjectConeId { get; } = projectConeId;
    public Checksum Attributes { get; } = attributes;
    public ProjectChecksumsAndIds Projects { get; } = projects;
    public ChecksumCollection AnalyzerReferences { get; } = analyzerReferences;
    public Checksum FallbackAnalyzerOptions => fallbackAnalyzerOptionsChecksum;

    // Acceptably not threadsafe.  ProjectCone is a class, and the runtime guarantees anyone will see this field fully
    // initialized.  It's acceptable to have multiple instances of this in a race condition as the data will be same
    // (and our asserts don't check for reference equality, only value equality).
    public ProjectCone? ProjectCone => _projectCone ??= ComputeProjectCone();

    private ProjectCone? ComputeProjectCone()
        => ProjectConeId == null ? null : new ProjectCone(ProjectConeId, Projects.Ids.ToFrozenSet());

    public void AddAllTo(HashSet<Checksum> checksums)
    {
        checksums.AddIfNotNullChecksum(this.Checksum);
        checksums.AddIfNotNullChecksum(this.Attributes);
        this.Projects.Checksums.AddAllTo(checksums);
        this.AnalyzerReferences.AddAllTo(checksums);
        checksums.AddIfNotNullChecksum(this.FallbackAnalyzerOptions);
    }

    public void Serialize(ObjectWriter writer)
    {
        // Writing this is optional, but helps ensure checksums are being computed properly on both the host and oop side.
        this.Checksum.WriteTo(writer);
        writer.WriteBoolean(this.ProjectConeId != null);
        this.ProjectConeId?.WriteTo(writer);

        this.Attributes.WriteTo(writer);
        this.Projects.WriteTo(writer);
        this.AnalyzerReferences.WriteTo(writer);
        this.FallbackAnalyzerOptions.WriteTo(writer);
    }

    public static SolutionStateChecksums Deserialize(ObjectReader reader)
    {
        var checksum = Checksum.ReadFrom(reader);

        var result = new SolutionStateChecksums(
            projectConeId: reader.ReadBoolean() ? ProjectId.ReadFrom(reader) : null,
            attributes: Checksum.ReadFrom(reader),
            projects: ProjectChecksumsAndIds.ReadFrom(reader),
            analyzerReferences: ChecksumCollection.ReadFrom(reader),
            fallbackAnalyzerOptionsChecksum: Checksum.ReadFrom(reader));
        Contract.ThrowIfFalse(result.Checksum == checksum);
        return result;
    }

    public async Task FindAsync<TArg>(
        SolutionState solution,
        ProjectCone? projectCone,
        AssetPath assetPath,
        HashSet<Checksum> searchingChecksumsLeft,
        Action<Checksum, object, TArg> onAssetFound,
        TArg arg,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (searchingChecksumsLeft.Count == 0)
            return;

        if (assetPath.IncludeSolutionState)
        {
            if (assetPath.IncludeSolutionStateChecksums && searchingChecksumsLeft.Remove(Checksum))
                onAssetFound(Checksum, this, arg);

            if (assetPath.IncludeSolutionAttributes && searchingChecksumsLeft.Remove(Attributes))
                onAssetFound(Attributes, solution.SolutionAttributes, arg);

            if (assetPath.IncludeSolutionAnalyzerReferences)
                ChecksumCollection.Find(solution.AnalyzerReferences, AnalyzerReferences, searchingChecksumsLeft, onAssetFound: onAssetFound, arg: arg, cancellationToken: cancellationToken);

            if (assetPath.IncludeSolutionFallbackAnalyzerOptions && searchingChecksumsLeft.Remove(FallbackAnalyzerOptions))
                onAssetFound(FallbackAnalyzerOptions, solution.FallbackAnalyzerOptions, arg);
        }

        if (searchingChecksumsLeft.Count == 0)
            return;

        if (assetPath.IncludeProjects || assetPath.IncludeDocuments)
        {
            if (assetPath.ProjectId is not null)
            {
                // Dive into this project to search for the remaining checksums.
                Contract.ThrowIfTrue(
                    projectCone != null && !projectCone.Contains(assetPath.ProjectId),
                    "Requesting an asset outside of the cone explicitly being asked for!");

                var projectState = solution.GetProjectState(assetPath.ProjectId);
                if (projectState != null &&
                    projectState.TryGetStateChecksums(out var projectStateChecksums))
                {
                    await projectStateChecksums.FindAsync(projectState, assetPath, searchingChecksumsLeft, onAssetFound: onAssetFound, arg: arg, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                // Check all projects for the remaining checksums.

                foreach (var (projectId, projectState) in solution.ProjectStates)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // If we have no more checksums, can immediately bail out.
                    if (searchingChecksumsLeft.Count == 0)
                        break;

                    if (projectCone != null && !projectCone.Contains(projectId))
                        continue;

                    // It's possible not all all our projects have checksums.  Specifically, we may have only been asked to
                    // compute the checksum tree for a subset of projects that were all that a feature needed.
                    if (!projectState.TryGetStateChecksums(out var projectStateChecksums))
                        continue;

                    await projectStateChecksums.FindAsync(projectState, assetPath, searchingChecksumsLeft, onAssetFound: onAssetFound, arg: arg, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}

internal sealed class ProjectStateChecksums(
    ProjectId projectId,
    Checksum infoChecksum,
    Checksum compilationOptionsChecksum,
    Checksum parseOptionsChecksum,
    ChecksumCollection projectReferenceChecksums,
    ChecksumCollection metadataReferenceChecksums,
    ChecksumCollection analyzerReferenceChecksums,
    DocumentChecksumsAndIds documentChecksums,
    DocumentChecksumsAndIds additionalDocumentChecksums,
    DocumentChecksumsAndIds analyzerConfigDocumentChecksums) : IEquatable<ProjectStateChecksums>
{
    public Checksum Checksum { get; } = Checksum.Create(stackalloc[]
    {
        infoChecksum,
        compilationOptionsChecksum,
        parseOptionsChecksum,
        documentChecksums.Checksum,
        projectReferenceChecksums.Checksum,
        metadataReferenceChecksums.Checksum,
        analyzerReferenceChecksums.Checksum,
        additionalDocumentChecksums.Checksum,
        analyzerConfigDocumentChecksums.Checksum,
    });

    public ProjectId ProjectId => projectId;

    public Checksum Info => infoChecksum;
    public Checksum CompilationOptions => compilationOptionsChecksum;
    public Checksum ParseOptions => parseOptionsChecksum;

    public ChecksumCollection ProjectReferences => projectReferenceChecksums;
    public ChecksumCollection MetadataReferences => metadataReferenceChecksums;
    public ChecksumCollection AnalyzerReferences => analyzerReferenceChecksums;

    public DocumentChecksumsAndIds Documents => documentChecksums;
    public DocumentChecksumsAndIds AdditionalDocuments => additionalDocumentChecksums;
    public DocumentChecksumsAndIds AnalyzerConfigDocuments => analyzerConfigDocumentChecksums;

    public override bool Equals(object? obj)
        => Equals(obj as ProjectStateChecksums);

    public bool Equals(ProjectStateChecksums? obj)
        => this.Checksum == obj?.Checksum;

    public override int GetHashCode()
        => this.Checksum.GetHashCode();

    public void AddAllTo(HashSet<Checksum> checksums)
    {
        checksums.AddIfNotNullChecksum(this.Checksum);
        checksums.AddIfNotNullChecksum(this.Info);
        checksums.AddIfNotNullChecksum(this.CompilationOptions);
        checksums.AddIfNotNullChecksum(this.ParseOptions);
        this.ProjectReferences.AddAllTo(checksums);
        this.MetadataReferences.AddAllTo(checksums);
        this.AnalyzerReferences.AddAllTo(checksums);
        this.Documents.AddAllTo(checksums);
        this.AdditionalDocuments.AddAllTo(checksums);
        this.AnalyzerConfigDocuments.AddAllTo(checksums);
    }

    public void Serialize(ObjectWriter writer)
    {
        // Writing this is optional, but helps ensure checksums are being computed properly on both the host and oop side.
        this.Checksum.WriteTo(writer);

        this.ProjectId.WriteTo(writer);
        this.Info.WriteTo(writer);
        this.CompilationOptions.WriteTo(writer);
        this.ParseOptions.WriteTo(writer);
        this.ProjectReferences.WriteTo(writer);
        this.MetadataReferences.WriteTo(writer);
        this.AnalyzerReferences.WriteTo(writer);
        this.Documents.WriteTo(writer);
        this.AdditionalDocuments.WriteTo(writer);
        this.AnalyzerConfigDocuments.WriteTo(writer);
    }

    public static ProjectStateChecksums Deserialize(ObjectReader reader)
    {
        var checksum = Checksum.ReadFrom(reader);
        var result = new ProjectStateChecksums(
            projectId: ProjectId.ReadFrom(reader),
            infoChecksum: Checksum.ReadFrom(reader),
            compilationOptionsChecksum: Checksum.ReadFrom(reader),
            parseOptionsChecksum: Checksum.ReadFrom(reader),
            projectReferenceChecksums: ChecksumCollection.ReadFrom(reader),
            metadataReferenceChecksums: ChecksumCollection.ReadFrom(reader),
            analyzerReferenceChecksums: ChecksumCollection.ReadFrom(reader),
            documentChecksums: DocumentChecksumsAndIds.ReadFrom(reader),
            additionalDocumentChecksums: DocumentChecksumsAndIds.ReadFrom(reader),
            analyzerConfigDocumentChecksums: DocumentChecksumsAndIds.ReadFrom(reader));
        Contract.ThrowIfFalse(result.Checksum == checksum);
        return result;
    }

    public async Task FindAsync<TArg>(
        ProjectState state,
        AssetPath assetPath,
        HashSet<Checksum> searchingChecksumsLeft,
        Action<Checksum, object, TArg> onAssetFound,
        TArg arg,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // verify input
        Contract.ThrowIfFalse(state.TryGetStateChecksums(out var stateChecksum));
        Contract.ThrowIfFalse(this == stateChecksum);

        if (searchingChecksumsLeft.Count == 0)
            return;

        if (assetPath.IncludeProjects)
        {
            if (assetPath.IncludeProjectStateChecksums && searchingChecksumsLeft.Remove(Checksum))
                onAssetFound(Checksum, this, arg);

            if (assetPath.IncludeProjectAttributes && searchingChecksumsLeft.Remove(Info))
                onAssetFound(Info, state.ProjectInfo.Attributes, arg);

            if (assetPath.IncludeProjectCompilationOptions && searchingChecksumsLeft.Remove(CompilationOptions))
            {
                var compilationOptions = state.CompilationOptions ?? throw new InvalidOperationException("We should not be trying to serialize a project with no compilation options; RemoteSupportedLanguages.IsSupported should have filtered it out.");
                onAssetFound(CompilationOptions, compilationOptions, arg);
            }

            if (assetPath.IncludeProjectParseOptions && searchingChecksumsLeft.Remove(ParseOptions))
            {
                var parseOptions = state.ParseOptions ?? throw new InvalidOperationException("We should not be trying to serialize a project with no parse options; RemoteSupportedLanguages.IsSupported should have filtered it out.");
                onAssetFound(ParseOptions, parseOptions, arg);
            }

            if (assetPath.IncludeProjectProjectReferences)
                ChecksumCollection.Find(state.ProjectReferences, ProjectReferences, searchingChecksumsLeft, onAssetFound: onAssetFound, arg: arg, cancellationToken: cancellationToken);

            if (assetPath.IncludeProjectMetadataReferences)
                ChecksumCollection.Find(state.MetadataReferences, MetadataReferences, searchingChecksumsLeft, onAssetFound: onAssetFound, arg: arg, cancellationToken: cancellationToken);

            if (assetPath.IncludeProjectAnalyzerReferences)
                ChecksumCollection.Find(state.AnalyzerReferences, AnalyzerReferences, searchingChecksumsLeft, onAssetFound: onAssetFound, arg: arg, cancellationToken: cancellationToken);
        }

        if (assetPath.IncludeDocuments)
        {
            await ChecksumCollection.FindAsync(assetPath, state.DocumentStates, searchingChecksumsLeft, onAssetFound: onAssetFound, arg: arg, cancellationToken: cancellationToken).ConfigureAwait(false);
            await ChecksumCollection.FindAsync(assetPath, state.AdditionalDocumentStates, searchingChecksumsLeft, onAssetFound: onAssetFound, arg: arg, cancellationToken: cancellationToken).ConfigureAwait(false);
            await ChecksumCollection.FindAsync(assetPath, state.AnalyzerConfigDocumentStates, searchingChecksumsLeft, onAssetFound: onAssetFound, arg: arg, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    public override string ToString()
        => $"""
            ProjectStateChecksums({ProjectId})
                Info={Info}
                CompilationOptions={CompilationOptions}
                ParseOptions={ParseOptions}
                ProjectReferences={ProjectReferences.Checksum}
                MetadataReferences={MetadataReferences.Checksum}
                AnalyzerReferences={AnalyzerReferences.Checksum}
                Documents={Documents.Checksum}
                AdditionalDocuments={AdditionalDocuments.Checksum}
                AnalyzerConfigDocuments={AnalyzerConfigDocuments.Checksum}
            """;
}

internal sealed class DocumentStateChecksums(
    DocumentId documentId,
    Checksum infoChecksum,
    Checksum textChecksum)
{
    public Checksum Checksum { get; } = Checksum.Create(infoChecksum, textChecksum);

    public DocumentId DocumentId => documentId;
    public Checksum Info => infoChecksum;
    public Checksum Text => textChecksum;

    public void AddAllTo(HashSet<Checksum> checksums)
    {
        checksums.AddIfNotNullChecksum(this.Info);
        checksums.AddIfNotNullChecksum(this.Text);
    }

    public async Task FindAsync<TArg>(
        AssetPath assetPath,
        TextDocumentState state,
        HashSet<Checksum> searchingChecksumsLeft,
        Action<Checksum, object, TArg> onAssetFound,
        TArg arg,
        CancellationToken cancellationToken)
    {
        Debug.Assert(state.TryGetStateChecksums(out var stateChecksum) && this == stateChecksum);

        cancellationToken.ThrowIfCancellationRequested();

        if (assetPath.IncludeDocumentAttributes && searchingChecksumsLeft.Remove(Info))
            onAssetFound(Info, state.Attributes, arg);

        if (assetPath.IncludeDocumentText && searchingChecksumsLeft.Remove(Text))
        {
            var text = await SerializableSourceText.FromTextDocumentStateAsync(state, cancellationToken).ConfigureAwait(false);
            onAssetFound(Text, text, arg);
        }
    }

    public override string ToString()
        => $"DocumentStateChecksums({DocumentId})";
}

/// <summary>
/// hold onto object checksum that currently doesn't have a place to hold onto checksum
/// </summary>
internal static class ChecksumCache
{
    public static Checksum GetOrCreate<TValue, TArg>(TValue value, Func<TValue, TArg, Checksum> checksumCreator, TArg arg)
        where TValue : class
    {
        return StronglyTypedChecksumCache<TValue, Checksum>.GetOrCreate(value, checksumCreator: checksumCreator, arg: arg);
    }

    public static ChecksumCollection GetOrCreateChecksumCollection<TReference>(
        IReadOnlyList<TReference> references, ISerializerService serializer, CancellationToken cancellationToken) where TReference : class
    {
        return StronglyTypedChecksumCache<IReadOnlyList<TReference>, ChecksumCollection>.GetOrCreate(
            references,
            checksumCreator: static (references, tuple) =>
            {
                var checksums = new FixedSizeArrayBuilder<Checksum>(references.Count);
                foreach (var reference in references)
                    checksums.Add(tuple.serializer.CreateChecksum(reference, tuple.cancellationToken));

                return new ChecksumCollection(checksums.MoveToImmutable());
            },
            arg: (serializer, cancellationToken));
    }

    private static class StronglyTypedChecksumCache<TValue, TResult>
        where TValue : class
        where TResult : struct
    {
        private static readonly ConditionalWeakTable<TValue, StrongBox<TResult>> s_objectToChecksumCollectionCache = new();

        public static TResult GetOrCreate<TArg>(TValue value, Func<TValue, TArg, TResult> checksumCreator, TArg arg)
        {
            if (s_objectToChecksumCollectionCache.TryGetValue(value, out var checksumCollection))
                return checksumCollection.Value;

            return GetOrCreateSlow(value, checksumCreator, arg);

            static TResult GetOrCreateSlow(TValue value, Func<TValue, TArg, TResult> checksumCreator, TArg arg)
                => s_objectToChecksumCollectionCache.GetValue(value, _ => new StrongBox<TResult>(checksumCreator(value, arg))).Value;
        }
    }
}
