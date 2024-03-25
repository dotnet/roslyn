// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization;

internal sealed class SolutionCompilationStateChecksums
{
    public SolutionCompilationStateChecksums(
        Checksum solutionState,
        ChecksumCollection? frozenSourceGeneratedDocumentIdentities,
        ChecksumsAndIds<DocumentId>? frozenSourceGeneratedDocuments)
    {
        // For the frozen source generated document info, we expect two either have both checksum collections or neither, and they
        // should both be the same length as there is a 1:1 correspondence between them.
        Contract.ThrowIfFalse(frozenSourceGeneratedDocumentIdentities.HasValue == frozenSourceGeneratedDocuments.HasValue);
        Contract.ThrowIfFalse(frozenSourceGeneratedDocumentIdentities?.Count == frozenSourceGeneratedDocuments?.Length);

        SolutionState = solutionState;
        FrozenSourceGeneratedDocumentIdentities = frozenSourceGeneratedDocumentIdentities;
        FrozenSourceGeneratedDocuments = frozenSourceGeneratedDocuments;

        Checksum = Checksum.Create(
            SolutionState,
            FrozenSourceGeneratedDocumentIdentities?.Checksum ?? Checksum.Null,
            FrozenSourceGeneratedDocuments?.Checksum ?? Checksum.Null);
    }

    public Checksum Checksum { get; }
    public Checksum SolutionState { get; }
    public ChecksumCollection? FrozenSourceGeneratedDocumentIdentities { get; }
    public ChecksumsAndIds<DocumentId>? FrozenSourceGeneratedDocuments { get; }

    public void AddAllTo(HashSet<Checksum> checksums)
    {
        checksums.AddIfNotNullChecksum(this.Checksum);
        checksums.AddIfNotNullChecksum(this.SolutionState);
        this.FrozenSourceGeneratedDocumentIdentities?.AddAllTo(checksums);
        this.FrozenSourceGeneratedDocuments?.Checksums.AddAllTo(checksums);
    }

    public void Serialize(ObjectWriter writer)
    {
        // Writing this is optional, but helps ensure checksums are being computed properly on both the host and oop side.
        this.Checksum.WriteTo(writer);
        this.SolutionState.WriteTo(writer);

        // Write out a boolean to know whether we'll have this extra information
        writer.WriteBoolean(this.FrozenSourceGeneratedDocumentIdentities.HasValue);
        if (FrozenSourceGeneratedDocumentIdentities.HasValue)
        {
            this.FrozenSourceGeneratedDocumentIdentities.Value.WriteTo(writer);
            this.FrozenSourceGeneratedDocuments!.Value.WriteTo(writer);
        }
    }

    public static SolutionCompilationStateChecksums Deserialize(ObjectReader reader)
    {
        var checksum = Checksum.ReadFrom(reader);
        var solutionState = Checksum.ReadFrom(reader);

        var hasFrozenSourceGeneratedDocuments = reader.ReadBoolean();
        ChecksumCollection? frozenSourceGeneratedDocumentIdentities = null;
        ChecksumsAndIds<DocumentId>? frozenSourceGeneratedDocuments = null;

        if (hasFrozenSourceGeneratedDocuments)
        {
            frozenSourceGeneratedDocumentIdentities = ChecksumCollection.ReadFrom(reader);
            frozenSourceGeneratedDocuments = ChecksumsAndIds<DocumentId>.ReadFrom(reader);
        }

        var result = new SolutionCompilationStateChecksums(
            solutionState: solutionState,
            frozenSourceGeneratedDocumentIdentities: frozenSourceGeneratedDocumentIdentities,
            frozenSourceGeneratedDocuments: frozenSourceGeneratedDocuments);
        Contract.ThrowIfFalse(result.Checksum == checksum);
        return result;
    }

    public async Task FindAsync(
        SolutionCompilationState compilationState,
        ProjectCone? projectCone,
        AssetHint assetHint,
        HashSet<Checksum> searchingChecksumsLeft,
        Dictionary<Checksum, object> result,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (searchingChecksumsLeft.Count == 0)
            return;

        // verify input
        if (searchingChecksumsLeft.Remove(Checksum))
            result[Checksum] = this;

        if (compilationState.FrozenSourceGeneratedDocumentStates.HasValue)
        {
            Contract.ThrowIfFalse(FrozenSourceGeneratedDocumentIdentities.HasValue);

            // This could either be the checksum for the text (which we'll use our regular helper for first)...
            await ChecksumCollection.FindAsync(compilationState.FrozenSourceGeneratedDocumentStates.Value, assetHint.DocumentId, searchingChecksumsLeft, result, cancellationToken).ConfigureAwait(false);

            // ... or one of the identities. In this case, we'll use the fact that there's a 1:1 correspondence between the
            // two collections we hold onto.
            for (var i = 0; i < FrozenSourceGeneratedDocumentIdentities.Value.Count; i++)
            {
                var identityChecksum = FrozenSourceGeneratedDocumentIdentities.Value[0];
                if (searchingChecksumsLeft.Remove(identityChecksum))
                {
                    var id = FrozenSourceGeneratedDocuments!.Value.Ids[i];
                    Contract.ThrowIfFalse(compilationState.FrozenSourceGeneratedDocumentStates.Value.TryGetState(id, out var state));
                    result[identityChecksum] = state.Identity;
                }
            }
        }

        var solutionState = compilationState.SolutionState;
        if (projectCone is null)
        {
            // If we're not in a project cone, start the search at the top most state-checksum corresponding to the
            // entire solution.
            Contract.ThrowIfFalse(solutionState.TryGetStateChecksums(out var solutionChecksums));
            await solutionChecksums.FindAsync(solutionState, projectCone, assetHint, searchingChecksumsLeft, result, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // Otherwise, grab the top-most state checksum for this cone and search within that.
            Contract.ThrowIfFalse(solutionState.TryGetStateChecksums(projectCone.RootProjectId, out var solutionChecksums));
            await solutionChecksums.FindAsync(solutionState, projectCone, assetHint, searchingChecksumsLeft, result, cancellationToken).ConfigureAwait(false);
        }
    }
}

/// <param name="projectConeId">The particular <see cref="ProjectId"/> if this was a checksum tree made for a particular
/// project cone.</param>
internal sealed class SolutionStateChecksums(
    ProjectId? projectConeId,
    Checksum attributes,
    ChecksumsAndIds<ProjectId> projects,
    ChecksumCollection analyzerReferences)
{
    private ProjectCone? _projectCone;

    public Checksum Checksum { get; } = Checksum.Create(stackalloc[]
    {
        projectConeId == null ? Checksum.Null : projectConeId.Checksum,
        attributes,
        projects.Checksum,
        analyzerReferences.Checksum,
    });

    public ProjectId? ProjectConeId { get; } = projectConeId;
    public Checksum Attributes { get; } = attributes;
    public ChecksumsAndIds<ProjectId> Projects { get; } = projects;
    public ChecksumCollection AnalyzerReferences { get; } = analyzerReferences;

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
    }

    public static SolutionStateChecksums Deserialize(ObjectReader reader)
    {
        var checksum = Checksum.ReadFrom(reader);

        var result = new SolutionStateChecksums(
            projectConeId: reader.ReadBoolean() ? ProjectId.ReadFrom(reader) : null,
            attributes: Checksum.ReadFrom(reader),
            projects: ChecksumsAndIds<ProjectId>.ReadFrom(reader),
            analyzerReferences: ChecksumCollection.ReadFrom(reader));
        Contract.ThrowIfFalse(result.Checksum == checksum);
        return result;
    }

    public async Task FindAsync(
        SolutionState solution,
        ProjectCone? projectCone,
        AssetHint assetHint,
        HashSet<Checksum> searchingChecksumsLeft,
        Dictionary<Checksum, object> result,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (searchingChecksumsLeft.Count == 0)
            return;

        // verify input
        if (searchingChecksumsLeft.Remove(Checksum))
            result[Checksum] = this;

        if (searchingChecksumsLeft.Remove(Attributes))
            result[Attributes] = solution.SolutionAttributes;

        ChecksumCollection.Find(solution.AnalyzerReferences, AnalyzerReferences, searchingChecksumsLeft, result, cancellationToken);

        if (searchingChecksumsLeft.Count == 0)
            return;

        if (assetHint.ProjectId != null)
        {
            Contract.ThrowIfTrue(
                projectCone != null && !projectCone.Contains(assetHint.ProjectId),
                "Requesting an asset outside of the cone explicitly being asked for!");

            var projectState = solution.GetProjectState(assetHint.ProjectId);
            if (projectState != null &&
                projectState.TryGetStateChecksums(out var projectStateChecksums))
            {
                await projectStateChecksums.FindAsync(projectState, assetHint.DocumentId, searchingChecksumsLeft, result, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            Contract.ThrowIfTrue(assetHint.DocumentId != null);

            // Before doing a depth-first-search *into* each project, first run across all the project at their top
            // level. This ensures that when we are trying to sync the projects referenced by a SolutionStateChecksums'
            // instance that we don't unnecessarily walk all documents looking just for those.

            foreach (var (projectId, projectState) in solution.ProjectStates)
            {
                if (searchingChecksumsLeft.Count == 0)
                    break;

                // If we're syncing a project cone, no point at all at looking at child projects of the solution that
                // are not in that cone.
                if (projectCone != null && !projectCone.Contains(projectId))
                    continue;

                if (projectState.TryGetStateChecksums(out var projectStateChecksums) &&
                    searchingChecksumsLeft.Remove(projectStateChecksums.Checksum))
                {
                    result[projectStateChecksums.Checksum] = projectStateChecksums;
                }
            }

            // Now actually do the depth first search into each project.

            foreach (var (projectId, projectState) in solution.ProjectStates)
            {
                if (searchingChecksumsLeft.Count == 0)
                    break;

                // If we're syncing a project cone, no point at all at looking at child projects of the solution that
                // are not in that cone.
                if (projectCone != null && !projectCone.Contains(projectId))
                    continue;

                // It's possible not all all our projects have checksums.  Specifically, we may have only been asked to
                // compute the checksum tree for a subset of projects that were all that a feature needed.
                if (projectState.TryGetStateChecksums(out var projectStateChecksums))
                    await projectStateChecksums.FindAsync(projectState, hintDocument: null, searchingChecksumsLeft, result, cancellationToken).ConfigureAwait(false);
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
    ChecksumsAndIds<DocumentId> documentChecksums,
    ChecksumsAndIds<DocumentId> additionalDocumentChecksums,
    ChecksumsAndIds<DocumentId> analyzerConfigDocumentChecksums) : IEquatable<ProjectStateChecksums>
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

    public ChecksumsAndIds<DocumentId> Documents => documentChecksums;
    public ChecksumsAndIds<DocumentId> AdditionalDocuments => additionalDocumentChecksums;
    public ChecksumsAndIds<DocumentId> AnalyzerConfigDocuments => analyzerConfigDocumentChecksums;

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
        this.Documents.Checksums.AddAllTo(checksums);
        this.AdditionalDocuments.Checksums.AddAllTo(checksums);
        this.AnalyzerConfigDocuments.Checksums.AddAllTo(checksums);
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
            documentChecksums: ChecksumsAndIds<DocumentId>.ReadFrom(reader),
            additionalDocumentChecksums: ChecksumsAndIds<DocumentId>.ReadFrom(reader),
            analyzerConfigDocumentChecksums: ChecksumsAndIds<DocumentId>.ReadFrom(reader));
        Contract.ThrowIfFalse(result.Checksum == checksum);
        return result;
    }

    public async Task FindAsync(
        ProjectState state,
        DocumentId? hintDocument,
        HashSet<Checksum> searchingChecksumsLeft,
        Dictionary<Checksum, object> result,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // verify input
        Contract.ThrowIfFalse(state.TryGetStateChecksums(out var stateChecksum));
        Contract.ThrowIfFalse(this == stateChecksum);

        if (searchingChecksumsLeft.Count == 0)
            return;

        if (searchingChecksumsLeft.Remove(Checksum))
        {
            result[Checksum] = this;
        }

        // It's normal for callers to just want to sync a single ProjectStateChecksum.  So quickly check this, without
        // doing all the expensive linear work below if we can bail out early here.
        if (searchingChecksumsLeft.Count == 0)
            return;

        if (searchingChecksumsLeft.Remove(Info))
        {
            result[Info] = state.ProjectInfo.Attributes;
        }

        if (searchingChecksumsLeft.Remove(CompilationOptions))
        {
            Contract.ThrowIfNull(state.CompilationOptions, "We should not be trying to serialize a project with no compilation options; RemoteSupportedLanguages.IsSupported should have filtered it out.");
            result[CompilationOptions] = state.CompilationOptions;
        }

        if (searchingChecksumsLeft.Remove(ParseOptions))
        {
            Contract.ThrowIfNull(state.ParseOptions, "We should not be trying to serialize a project with no compilation options; RemoteSupportedLanguages.IsSupported should have filtered it out.");
            result[ParseOptions] = state.ParseOptions;
        }

        ChecksumCollection.Find(state.ProjectReferences, ProjectReferences, searchingChecksumsLeft, result, cancellationToken);
        ChecksumCollection.Find(state.MetadataReferences, MetadataReferences, searchingChecksumsLeft, result, cancellationToken);
        ChecksumCollection.Find(state.AnalyzerReferences, AnalyzerReferences, searchingChecksumsLeft, result, cancellationToken);

        await ChecksumCollection.FindAsync(state.DocumentStates, hintDocument, searchingChecksumsLeft, result, cancellationToken).ConfigureAwait(false);
        await ChecksumCollection.FindAsync(state.AdditionalDocumentStates, hintDocument, searchingChecksumsLeft, result, cancellationToken).ConfigureAwait(false);
        await ChecksumCollection.FindAsync(state.AnalyzerConfigDocumentStates, hintDocument, searchingChecksumsLeft, result, cancellationToken).ConfigureAwait(false);
    }
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

    public void Serialize(ObjectWriter writer)
    {
        // We don't write out the checksum itself as it would bloat the size of this message. If there is corruption
        // (which should never ever happen), it will be detected at the project level.
        this.DocumentId.WriteTo(writer);
        this.Info.WriteTo(writer);
        this.Text.WriteTo(writer);
    }

    public static DocumentStateChecksums Deserialize(ObjectReader reader)
    {
        return new DocumentStateChecksums(
            documentId: DocumentId.ReadFrom(reader),
            infoChecksum: Checksum.ReadFrom(reader),
            textChecksum: Checksum.ReadFrom(reader));
    }

    public async Task FindAsync(
        TextDocumentState state,
        HashSet<Checksum> searchingChecksumsLeft,
        Dictionary<Checksum, object> result,
        CancellationToken cancellationToken)
    {
        Debug.Assert(state.TryGetStateChecksums(out var stateChecksum) && this == stateChecksum);

        cancellationToken.ThrowIfCancellationRequested();

        if (searchingChecksumsLeft.Remove(Checksum))
        {
            result[Checksum] = this;
        }

        if (searchingChecksumsLeft.Remove(Info))
        {
            result[Info] = state.Attributes;
        }

        if (searchingChecksumsLeft.Remove(Text))
        {
            result[Text] = await SerializableSourceText.FromTextDocumentStateAsync(state, cancellationToken).ConfigureAwait(false);
        }
    }
}

/// <summary>
/// hold onto object checksum that currently doesn't have a place to hold onto checksum
/// </summary>
internal static class ChecksumCache
{
    public static Checksum GetOrCreate<TValue, TArg>(TValue value, Func<TValue, TArg, Checksum> checksumCreator, TArg arg)
        where TValue : class
    {
        return StronglyTypedChecksumCache<TValue, Checksum>.GetOrCreate(value, checksumCreator, arg);
    }

    public static ChecksumCollection GetOrCreateChecksumCollection<TReference>(
        IReadOnlyList<TReference> references, ISerializerService serializer, CancellationToken cancellationToken) where TReference : class
    {
        return StronglyTypedChecksumCache<IReadOnlyList<TReference>, ChecksumCollection>.GetOrCreate(
            references,
            static (references, tuple) =>
            {
                using var _ = ArrayBuilder<Checksum>.GetInstance(references.Count, out var checksums);
                foreach (var reference in references)
                    checksums.Add(tuple.serializer.CreateChecksum(reference, tuple.cancellationToken));

                return new ChecksumCollection(checksums.ToImmutableAndClear());
            },
            (serializer, cancellationToken));
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
