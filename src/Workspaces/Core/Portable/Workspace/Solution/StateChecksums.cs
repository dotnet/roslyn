// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization;

internal sealed class SolutionStateChecksums(
    Checksum attributesChecksum,
    ChecksumCollection projectChecksums,
    ChecksumCollection analyzerReferenceChecksums,
    Checksum frozenSourceGeneratedDocumentIdentity,
    Checksum frozenSourceGeneratedDocumentText) : IChecksummedObject
{
    public Checksum Checksum { get; } = Checksum.Create(stackalloc[]
    {
        attributesChecksum.Hash,
        projectChecksums.Checksum.Hash,
        analyzerReferenceChecksums.Checksum.Hash,
        frozenSourceGeneratedDocumentIdentity.Hash,
        frozenSourceGeneratedDocumentText.Hash,
    });

    public Checksum Attributes => attributesChecksum;
    public ChecksumCollection Projects => projectChecksums;
    public ChecksumCollection AnalyzerReferences => analyzerReferenceChecksums;
    public Checksum FrozenSourceGeneratedDocumentIdentity => frozenSourceGeneratedDocumentIdentity;
    public Checksum FrozenSourceGeneratedDocumentText => frozenSourceGeneratedDocumentText;

    public void AddAllTo(HashSet<Checksum> checksums)
    {
        checksums.AddIfNotNullChecksum(this.Checksum);
        checksums.AddIfNotNullChecksum(this.Attributes);
        this.Projects.AddAllTo(checksums);
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
            attributesChecksum: Checksum.ReadFrom(reader),
            projectChecksums: ChecksumCollection.ReadFrom(reader),
            analyzerReferenceChecksums: ChecksumCollection.ReadFrom(reader),
            frozenSourceGeneratedDocumentIdentity: Checksum.ReadFrom(reader),
            frozenSourceGeneratedDocumentText: Checksum.ReadFrom(reader));
        Contract.ThrowIfFalse(result.Checksum == checksum);
        return result;
    }

    public async Task FindAsync(
        SolutionState state,
        HashSet<Checksum> searchingChecksumsLeft,
        Dictionary<Checksum, object?> result,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (searchingChecksumsLeft.Count == 0)
            return;

        // verify input
        if (searchingChecksumsLeft.Remove(Checksum))
            result[Checksum] = this;

        if (searchingChecksumsLeft.Remove(Attributes))
            result[Attributes] = state.SolutionAttributes;

        if (searchingChecksumsLeft.Remove(FrozenSourceGeneratedDocumentIdentity))
        {
            Contract.ThrowIfNull(state.FrozenSourceGeneratedDocumentState, "We should not have had a FrozenSourceGeneratedDocumentIdentity checksum if we didn't have a text in the first place.");
            result[FrozenSourceGeneratedDocumentIdentity] = state.FrozenSourceGeneratedDocumentState.Identity;
        }

        if (searchingChecksumsLeft.Remove(FrozenSourceGeneratedDocumentText))
        {
            Contract.ThrowIfNull(state.FrozenSourceGeneratedDocumentState, "We should not have had a FrozenSourceGeneratedDocumentState checksum if we didn't have a text in the first place.");
            result[FrozenSourceGeneratedDocumentText] = await SerializableSourceText.FromTextDocumentStateAsync(state.FrozenSourceGeneratedDocumentState, cancellationToken).ConfigureAwait(false);
        }

        if (searchingChecksumsLeft.Remove(Projects.Checksum))
        {
            result[Projects.Checksum] = Projects;
        }

        if (searchingChecksumsLeft.Remove(AnalyzerReferences.Checksum))
        {
            result[AnalyzerReferences.Checksum] = AnalyzerReferences;
        }

        foreach (var (_, projectState) in state.ProjectStates)
        {
            if (searchingChecksumsLeft.Count == 0)
                break;

            // It's possible not all all our projects have checksums.  Specifically, we may have only been
            // asked to compute the checksum tree for a subset of projects that were all that a feature needed.
            if (projectState.TryGetStateChecksums(out var projectStateChecksums))
                await projectStateChecksums.FindAsync(projectState, searchingChecksumsLeft, result, cancellationToken).ConfigureAwait(false);
        }

        ChecksumCollection.Find(state.AnalyzerReferences, AnalyzerReferences, searchingChecksumsLeft, result, cancellationToken);
    }
}

internal class ProjectStateChecksums(
    Checksum infoChecksum,
    Checksum compilationOptionsChecksum,
    Checksum parseOptionsChecksum,
    ChecksumCollection documentChecksums,
    ChecksumCollection projectReferenceChecksums,
    ChecksumCollection metadataReferenceChecksums,
    ChecksumCollection analyzerReferenceChecksums,
    ChecksumCollection additionalDocumentChecksums,
    ChecksumCollection analyzerConfigDocumentChecksums) : IChecksummedObject
{
    public Checksum Checksum { get; } = Checksum.Create(stackalloc[]
    {
        infoChecksum.Hash,
        compilationOptionsChecksum.Hash,
        parseOptionsChecksum.Hash,
        documentChecksums.Checksum.Hash,
        projectReferenceChecksums.Checksum.Hash,
        metadataReferenceChecksums.Checksum.Hash,
        analyzerReferenceChecksums.Checksum.Hash,
        additionalDocumentChecksums.Checksum.Hash,
        analyzerConfigDocumentChecksums.Checksum.Hash,
    });

    public Checksum Info => infoChecksum;
    public Checksum CompilationOptions => compilationOptionsChecksum;
    public Checksum ParseOptions => parseOptionsChecksum;

    public ChecksumCollection Documents => documentChecksums;

    public ChecksumCollection ProjectReferences => projectReferenceChecksums;
    public ChecksumCollection MetadataReferences => metadataReferenceChecksums;
    public ChecksumCollection AnalyzerReferences => analyzerReferenceChecksums;

    public ChecksumCollection AdditionalDocuments => additionalDocumentChecksums;
    public ChecksumCollection AnalyzerConfigDocuments => analyzerConfigDocumentChecksums;

    public void AddAllTo(HashSet<Checksum> checksums)
    {
        checksums.AddIfNotNullChecksum(this.Checksum);
        checksums.AddIfNotNullChecksum(this.Info);
        checksums.AddIfNotNullChecksum(this.CompilationOptions);
        checksums.AddIfNotNullChecksum(this.ParseOptions);
        this.Documents.AddAllTo(checksums);
        this.ProjectReferences.AddAllTo(checksums);
        this.MetadataReferences.AddAllTo(checksums);
        this.AnalyzerReferences.AddAllTo(checksums);
        this.AdditionalDocuments.AddAllTo(checksums);
        this.AnalyzerConfigDocuments.AddAllTo(checksums);
    }

    public void Serialize(ObjectWriter writer)
    {
        // Writing this is optional, but helps ensure checksums are being computed properly on both the host and oop side.
        this.Checksum.WriteTo(writer);
        this.Info.WriteTo(writer);
        this.CompilationOptions.WriteTo(writer);
        this.ParseOptions.WriteTo(writer);
        this.Documents.WriteTo(writer);
        this.ProjectReferences.WriteTo(writer);
        this.MetadataReferences.WriteTo(writer);
        this.AnalyzerReferences.WriteTo(writer);
        this.AdditionalDocuments.WriteTo(writer);
        this.AnalyzerConfigDocuments.WriteTo(writer);
    }

    public static ProjectStateChecksums Deserialize(ObjectReader reader)
    {
        var checksum = Checksum.ReadFrom(reader);
        var result = new ProjectStateChecksums(
            infoChecksum: Checksum.ReadFrom(reader),
            compilationOptionsChecksum: Checksum.ReadFrom(reader),
            parseOptionsChecksum: Checksum.ReadFrom(reader),
            documentChecksums: ChecksumCollection.ReadFrom(reader),
            projectReferenceChecksums: ChecksumCollection.ReadFrom(reader),
            metadataReferenceChecksums: ChecksumCollection.ReadFrom(reader),
            analyzerReferenceChecksums: ChecksumCollection.ReadFrom(reader),
            additionalDocumentChecksums: ChecksumCollection.ReadFrom(reader),
            analyzerConfigDocumentChecksums: ChecksumCollection.ReadFrom(reader));
        Contract.ThrowIfFalse(result.Checksum == checksum);
        return result;
    }

    public async Task FindAsync(
        ProjectState state,
        HashSet<Checksum> searchingChecksumsLeft,
        Dictionary<Checksum, object?> result,
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

        if (searchingChecksumsLeft.Remove(Documents.Checksum))
        {
            result[Documents.Checksum] = Documents;
        }

        if (searchingChecksumsLeft.Remove(ProjectReferences.Checksum))
        {
            result[ProjectReferences.Checksum] = ProjectReferences;
        }

        if (searchingChecksumsLeft.Remove(MetadataReferences.Checksum))
        {
            result[MetadataReferences.Checksum] = MetadataReferences;
        }

        if (searchingChecksumsLeft.Remove(AnalyzerReferences.Checksum))
        {
            result[AnalyzerReferences.Checksum] = AnalyzerReferences;
        }

        if (searchingChecksumsLeft.Remove(AdditionalDocuments.Checksum))
        {
            result[AdditionalDocuments.Checksum] = AdditionalDocuments;
        }

        if (searchingChecksumsLeft.Remove(AnalyzerConfigDocuments.Checksum))
        {
            result[AnalyzerConfigDocuments.Checksum] = AnalyzerConfigDocuments;
        }

        ChecksumCollection.Find(state.ProjectReferences, ProjectReferences, searchingChecksumsLeft, result, cancellationToken);
        ChecksumCollection.Find(state.MetadataReferences, MetadataReferences, searchingChecksumsLeft, result, cancellationToken);
        ChecksumCollection.Find(state.AnalyzerReferences, AnalyzerReferences, searchingChecksumsLeft, result, cancellationToken);

        await ChecksumCollection.FindAsync(state.DocumentStates, searchingChecksumsLeft, result, cancellationToken).ConfigureAwait(false);
        await ChecksumCollection.FindAsync(state.AdditionalDocumentStates, searchingChecksumsLeft, result, cancellationToken).ConfigureAwait(false);
        await ChecksumCollection.FindAsync(state.AnalyzerConfigDocumentStates, searchingChecksumsLeft, result, cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class DocumentStateChecksums(Checksum infoChecksum, Checksum textChecksum) : IChecksummedObject
{
    public Checksum Checksum { get; } = Checksum.Create(infoChecksum, textChecksum);
    public Checksum Info => infoChecksum;
    public Checksum Text => textChecksum;

    public void AddAllTo(HashSet<Checksum> checksums)
    {
        checksums.AddIfNotNullChecksum(this.Checksum);
        checksums.AddIfNotNullChecksum(this.Info);
        checksums.AddIfNotNullChecksum(this.Text);
    }

    public void Serialize(ObjectWriter writer)
    {
        // Writing this is optional, but helps ensure checksums are being computed properly on both the host and oop side.
        this.Checksum.WriteTo(writer);
        this.Info.WriteTo(writer);
        this.Text.WriteTo(writer);
    }

    public static DocumentStateChecksums Deserialize(ObjectReader reader)
    {
        var checksum = Checksum.ReadFrom(reader);
        var result = new DocumentStateChecksums(
            infoChecksum: Checksum.ReadFrom(reader),
            textChecksum: Checksum.ReadFrom(reader));
        Contract.ThrowIfFalse(result.Checksum == checksum);
        return result;
    }

    public async Task FindAsync(
        TextDocumentState state,
        HashSet<Checksum> searchingChecksumsLeft,
        Dictionary<Checksum, object?> result,
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
    private static readonly ConditionalWeakTable<object, object> s_cache = new();

    public static IReadOnlyList<T> GetOrCreate<T>(IReadOnlyList<T> unorderedList, ConditionalWeakTable<object, object>.CreateValueCallback orderedListGetter)
        => (IReadOnlyList<T>)s_cache.GetValue(unorderedList, orderedListGetter);

    public static bool TryGetValue(object value, [NotNullWhen(true)] out Checksum? checksum)
    {
        // same key should always return same checksum
        if (!s_cache.TryGetValue(value, out var result))
        {
            checksum = null;
            return false;
        }

        checksum = (Checksum)result;
        return true;
    }

    public static Checksum GetOrCreate(object value, ConditionalWeakTable<object, object>.CreateValueCallback checksumCreator)
    {
        // same key should always return same checksum
        return (Checksum)s_cache.GetValue(value, checksumCreator);
    }

    public static T GetOrCreate<T>(object value, ConditionalWeakTable<object, object>.CreateValueCallback checksumCreator) where T : IChecksummedObject
    {
        // same key should always return same checksum
        return (T)s_cache.GetValue(value, checksumCreator);
    }
}
