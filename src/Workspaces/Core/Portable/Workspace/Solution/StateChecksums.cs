// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization;

internal sealed class SolutionStateChecksums(
    Checksum attributes,
    ChecksumsAndIds<ProjectId> projects,
    ChecksumCollection analyzerReferences,
    Checksum frozenSourceGeneratedDocumentIdentity,
    Checksum frozenSourceGeneratedDocumentText)
{
    public Checksum Checksum { get; } = Checksum.Create(stackalloc[]
    {
        attributes,
        projects.Checksum,
        analyzerReferences.Checksum,
        frozenSourceGeneratedDocumentIdentity,
        frozenSourceGeneratedDocumentText,
    });

    public Checksum Attributes { get; } = attributes;
    public ChecksumsAndIds<ProjectId> Projects { get; } = projects;
    public ChecksumCollection AnalyzerReferences { get; } = analyzerReferences;
    public Checksum FrozenSourceGeneratedDocumentIdentity { get; } = frozenSourceGeneratedDocumentIdentity;
    public Checksum FrozenSourceGeneratedDocumentText { get; } = frozenSourceGeneratedDocumentText;

    public void AddAllTo(HashSet<Checksum> checksums)
    {
        checksums.AddIfNotNullChecksum(this.Checksum);
        checksums.AddIfNotNullChecksum(this.Attributes);
        this.Projects.Checksums.AddAllTo(checksums);
        this.AnalyzerReferences.AddAllTo(checksums);
        checksums.AddIfNotNullChecksum(this.FrozenSourceGeneratedDocumentIdentity);
        checksums.AddIfNotNullChecksum(this.FrozenSourceGeneratedDocumentText);
    }

    public void Serialize(ObjectWriter writer)
    {
        // Writing this is optional, but helps ensure checksums are being computed properly on both the host and oop side.
        this.Checksum.WriteTo(writer);
        this.Attributes.WriteTo(writer);
        this.Projects.WriteTo(writer);
        this.AnalyzerReferences.WriteTo(writer);
        this.FrozenSourceGeneratedDocumentIdentity.WriteTo(writer);
        this.FrozenSourceGeneratedDocumentText.WriteTo(writer);
    }

    public static SolutionStateChecksums Deserialize(ObjectReader reader)
    {
        var checksum = Checksum.ReadFrom(reader);
        var result = new SolutionStateChecksums(
            attributes: Checksum.ReadFrom(reader),
            projects: ChecksumsAndIds<ProjectId>.ReadFrom(reader),
            analyzerReferences: ChecksumCollection.ReadFrom(reader),
            frozenSourceGeneratedDocumentIdentity: Checksum.ReadFrom(reader),
            frozenSourceGeneratedDocumentText: Checksum.ReadFrom(reader));
        Contract.ThrowIfFalse(result.Checksum == checksum);
        return result;
    }

    public async Task FindAsync(
        SolutionCompilationState compilationState,
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
            result[Attributes] = compilationState.SolutionState.SolutionAttributes;

        if (searchingChecksumsLeft.Remove(FrozenSourceGeneratedDocumentIdentity))
        {
            Contract.ThrowIfNull(compilationState.FrozenSourceGeneratedDocumentState, "We should not have had a FrozenSourceGeneratedDocumentIdentity checksum if we didn't have a text in the first place.");
            result[FrozenSourceGeneratedDocumentIdentity] = compilationState.FrozenSourceGeneratedDocumentState.Identity;
        }

        if (searchingChecksumsLeft.Remove(FrozenSourceGeneratedDocumentText))
        {
            Contract.ThrowIfNull(compilationState.FrozenSourceGeneratedDocumentState, "We should not have had a FrozenSourceGeneratedDocumentState checksum if we didn't have a text in the first place.");
            result[FrozenSourceGeneratedDocumentText] = await SerializableSourceText.FromTextDocumentStateAsync(compilationState.FrozenSourceGeneratedDocumentState, cancellationToken).ConfigureAwait(false);
        }

        ChecksumCollection.Find(compilationState.SolutionState.AnalyzerReferences, AnalyzerReferences, searchingChecksumsLeft, result, cancellationToken);

        if (searchingChecksumsLeft.Count == 0)
            return;

        if (assetHint.ProjectId != null)
        {
            var projectState = compilationState.SolutionState.GetProjectState(assetHint.ProjectId);
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

            foreach (var (_, projectState) in compilationState.SolutionState.ProjectStates)
            {
                if (searchingChecksumsLeft.Count == 0)
                    break;

                if (projectState.TryGetStateChecksums(out var projectStateChecksums) &&
                    searchingChecksumsLeft.Remove(projectStateChecksums.Checksum))
                {
                    result[projectStateChecksums.Checksum] = projectStateChecksums;
                }
            }

            // Now actually do the depth first search into each project.

            foreach (var (_, projectState) in compilationState.SolutionState.ProjectStates)
            {
                if (searchingChecksumsLeft.Count == 0)
                    break;

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
