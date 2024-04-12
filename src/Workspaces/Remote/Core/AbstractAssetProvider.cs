// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

/// <summary>
/// Provides corresponding data of the given checksum
/// </summary>
internal abstract class AbstractAssetProvider
{
    /// <summary>
    /// return data of type T whose checksum is the given checksum
    /// </summary>
    public abstract ValueTask<T> GetAssetAsync<T>(AssetPath assetPath, Checksum checksum, CancellationToken cancellationToken);
    public abstract Task GetAssetsAsync<T, TArg>(AssetPath assetPath, HashSet<Checksum> checksums, Action<Checksum, T, TArg>? callback, TArg? arg, CancellationToken cancellationToken);

    public async Task<SolutionInfo> CreateSolutionInfoAsync(Checksum solutionChecksum, CancellationToken cancellationToken)
    {
        var solutionCompilationChecksums = await GetAssetAsync<SolutionCompilationStateChecksums>(AssetPathKind.SolutionCompilationStateChecksums, solutionChecksum, cancellationToken).ConfigureAwait(false);
        var solutionChecksums = await GetAssetAsync<SolutionStateChecksums>(AssetPathKind.SolutionStateChecksums, solutionCompilationChecksums.SolutionState, cancellationToken).ConfigureAwait(false);

        var solutionAttributes = await GetAssetAsync<SolutionInfo.SolutionAttributes>(AssetPathKind.SolutionAttributes, solutionChecksums.Attributes, cancellationToken).ConfigureAwait(false);
        await GetAssetAsync<SourceGeneratorExecutionVersionMap>(AssetPathKind.SolutionSourceGeneratorExecutionVersionMap, solutionCompilationChecksums.SourceGeneratorExecutionVersionMap, cancellationToken).ConfigureAwait(false);

        using var _ = ArrayBuilder<ProjectInfo>.GetInstance(solutionChecksums.Projects.Length, out var projects);
        foreach (var (projectChecksum, projectId) in solutionChecksums.Projects)
            projects.Add(await CreateProjectInfoAsync(projectId, projectChecksum, cancellationToken).ConfigureAwait(false));

        var analyzerReferences = await this.GetAssetsArrayAsync<AnalyzerReference>(AssetPathKind.SolutionAnalyzerReferences, solutionChecksums.AnalyzerReferences, cancellationToken).ConfigureAwait(false);

        return SolutionInfo.Create(
            solutionAttributes.Id, solutionAttributes.Version, solutionAttributes.FilePath, projects.ToImmutableAndClear(), analyzerReferences).WithTelemetryId(solutionAttributes.TelemetryId);
    }

    public async Task<ProjectInfo> CreateProjectInfoAsync(ProjectId projectId, Checksum projectChecksum, CancellationToken cancellationToken)
    {
        var projectChecksums = await GetAssetAsync<ProjectStateChecksums>(new(AssetPathKind.ProjectStateChecksums, projectId), projectChecksum, cancellationToken).ConfigureAwait(false);
        Contract.ThrowIfFalse(projectId == projectChecksums.ProjectId);

        var attributes = await GetAssetAsync<ProjectInfo.ProjectAttributes>(new(AssetPathKind.ProjectAttributes, projectId), projectChecksums.Info, cancellationToken).ConfigureAwait(false);
        Contract.ThrowIfFalse(RemoteSupportedLanguages.IsSupported(attributes.Language));

        var compilationOptions = attributes.FixUpCompilationOptions(
            await GetAssetAsync<CompilationOptions>(new(AssetPathKind.ProjectCompilationOptions, projectId), projectChecksums.CompilationOptions, cancellationToken).ConfigureAwait(false));
        var parseOptions = await GetAssetAsync<ParseOptions>(new(AssetPathKind.ProjectParseOptions, projectId), projectChecksums.ParseOptions, cancellationToken).ConfigureAwait(false);

        var projectReferences = await this.GetAssetsArrayAsync<ProjectReference>(new(AssetPathKind.ProjectProjectReferences, projectId), projectChecksums.ProjectReferences, cancellationToken).ConfigureAwait(false);
        var metadataReferences = await this.GetAssetsArrayAsync<MetadataReference>(new(AssetPathKind.ProjectMetadataReferences, projectId), projectChecksums.MetadataReferences, cancellationToken).ConfigureAwait(false);
        var analyzerReferences = await this.GetAssetsArrayAsync<AnalyzerReference>(new(AssetPathKind.ProjectAnalyzerReferences, projectId), projectChecksums.AnalyzerReferences, cancellationToken).ConfigureAwait(false);

        // Attempt to fetch all the documents for this project in bulk.  This will allow for all the data to be fetched
        // efficiently.  We can then go and create the DocumentInfos for each document in the project.
        await SynchronizeProjectDocumentsAsync(projectChecksums, cancellationToken).ConfigureAwait(false);

        var documentInfos = await CreateDocumentInfosAsync(projectChecksums.Documents).ConfigureAwait(false);
        var additionalDocumentInfos = await CreateDocumentInfosAsync(projectChecksums.AdditionalDocuments).ConfigureAwait(false);
        var analyzerConfigDocumentInfos = await CreateDocumentInfosAsync(projectChecksums.AnalyzerConfigDocuments).ConfigureAwait(false);

        return ProjectInfo.Create(
            attributes,
            compilationOptions,
            parseOptions,
            documentInfos,
            projectReferences,
            metadataReferences,
            analyzerReferences,
            additionalDocumentInfos,
            analyzerConfigDocumentInfos,
            hostObjectType: null); // TODO: https://github.com/dotnet/roslyn/issues/62804

        async Task<ImmutableArray<DocumentInfo>> CreateDocumentInfosAsync(ChecksumsAndIds<DocumentId> checksumsAndIds)
        {
            using var _ = ArrayBuilder<DocumentInfo>.GetInstance(checksumsAndIds.Length, out var documentInfos);

            await this.GetAssetsAsync<DocumentStateChecksums>(
                new(AssetPathKind.DocumentStateChecksums, projectId),
                checksumsAndIds.Checksums,
                cancellationToken).ConfigureAwait(false);

            foreach (var (documentChecksum, documentId) in checksumsAndIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                documentInfos.Add(await CreateDocumentInfoAsync(documentId, documentChecksum, cancellationToken).ConfigureAwait(false));
            }

            return documentInfos.ToImmutableAndClear();
        }
    }

    protected async Task SynchronizeProjectDocumentsAsync(
        ProjectStateChecksums projectChecksums, CancellationToken cancellationToken)
    {
        await Task.Yield();

        // First, fetch all the DocumentStateChecksums for all the documents in the project.
        using var _1 = ArrayBuilder<DocumentStateChecksums>.GetInstance(out var allDocumentStateChecksums);
        {
            using var _2 = PooledHashSet<Checksum>.GetInstance(out var checksums);

            projectChecksums.Documents.Checksums.AddAllTo(checksums);
            projectChecksums.AdditionalDocuments.Checksums.AddAllTo(checksums);
            projectChecksums.AnalyzerConfigDocuments.Checksums.AddAllTo(checksums);

            await this.GetAssetsAsync<DocumentStateChecksums, ArrayBuilder<DocumentStateChecksums>>(
                assetPath: new(AssetPathKind.DocumentStateChecksums, projectChecksums.ProjectId), checksums,
                static (_, documentStateChecksums, allDocumentStateChecksums) => allDocumentStateChecksums.Add(documentStateChecksums),
                allDocumentStateChecksums,
                cancellationToken).ConfigureAwait(false);
        }

        // Now go and fetch the info and text for all of those documents.
        {
            using var _2 = ArrayBuilder<Task>.GetInstance(out var tasks);
            tasks.Add(GetDocumentItemsAsync<DocumentInfo.DocumentAttributes>(AssetPathKind.DocumentAttributes, static d => d.Info));
            tasks.Add(GetDocumentItemsAsync<SerializableSourceText>(AssetPathKind.DocumentText, static d => d.Text));
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        return;

        async Task GetDocumentItemsAsync<TAsset>(
            AssetPathKind assetPathKind, Func<DocumentStateChecksums, Checksum> getItemChecksum)
        {
            await Task.Yield();
            using var _ = PooledHashSet<Checksum>.GetInstance(out var checksums);

            foreach (var documentStateChecksums in allDocumentStateChecksums)
                checksums.Add(getItemChecksum(documentStateChecksums));

            // We know we only need to search the documents in this particular project for those info/text values.  So
            // pass in the right path hint to limit the search on the host side to just the document in this project.
            await this.GetAssetsAsync<TAsset>(
                assetPath: new(assetPathKind, projectChecksums.ProjectId),
                checksums, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<DocumentInfo> CreateDocumentInfoAsync(
        DocumentId documentId, Checksum documentChecksum, CancellationToken cancellationToken)
    {
        var documentSnapshot = await GetAssetAsync<DocumentStateChecksums>(new(AssetPathKind.DocumentStateChecksums, documentId), documentChecksum, cancellationToken).ConfigureAwait(false);
        Contract.ThrowIfTrue(documentId != documentSnapshot.DocumentId);

        var attributes = await GetAssetAsync<DocumentInfo.DocumentAttributes>(new(AssetPathKind.DocumentAttributes, documentId), documentSnapshot.Info, cancellationToken).ConfigureAwait(false);
        var serializableSourceText = await GetAssetAsync<SerializableSourceText>(new(AssetPathKind.DocumentText, documentId), documentSnapshot.Text, cancellationToken).ConfigureAwait(false);

        var text = await serializableSourceText.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var textLoader = TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create(), attributes.FilePath));

        // TODO: do we need version?
        return new DocumentInfo(attributes, textLoader, documentServiceProvider: null);
    }
}

internal static class AbstractAssetProviderExtensions
{
    public static Task GetAssetsAsync<TAsset>(
        this AbstractAssetProvider assetProvider, AssetPath assetPath, HashSet<Checksum> checksums, CancellationToken cancellationToken)
    {
        return assetProvider.GetAssetsAsync<TAsset, VoidResult>(
            assetPath, checksums, callback: null, arg: default, cancellationToken);
    }

    public static Task GetAssetsAsync<T>(
        this AbstractAssetProvider assetProvider, AssetPath assetPath, ChecksumCollection checksums, CancellationToken cancellationToken)
    {
        return assetProvider.GetAssetsAsync<T, VoidResult>(
            assetPath, checksums, callback: null, arg: default, cancellationToken);
    }

    public static async Task GetAssetsAsync<T, TArg>(
        this AbstractAssetProvider assetProvider, AssetPath assetPath, ChecksumCollection checksums, Action<Checksum, T, TArg>? callback, TArg? arg, CancellationToken cancellationToken)
    {
        using var _1 = PooledHashSet<Checksum>.GetInstance(out var checksumSet);
#if NET
        checksumSet.EnsureCapacity(checksums.Children.Length);
#endif
        checksumSet.AddAll(checksums.Children);

        await assetProvider.GetAssetsAsync(assetPath, checksumSet, callback, arg, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<ImmutableArray<T>> GetAssetsArrayAsync<T>(
        this AbstractAssetProvider assetProvider, AssetPath assetPath, ChecksumCollection checksums, CancellationToken cancellationToken) where T : class
    {
        using var _1 = PooledHashSet<Checksum>.GetInstance(out var checksumSet);
        checksumSet.AddAll(checksums.Children);

        using var _2 = ArrayBuilder<T>.GetInstance(checksumSet.Count, out var builder);

        await assetProvider.GetAssetsAsync<T, ArrayBuilder<T>>(
            assetPath, checksumSet,
            static (checksum, asset, builder) => builder.Add(asset),
            builder,
            cancellationToken).ConfigureAwait(false);

        return builder.ToImmutableAndClear();
    }
}
