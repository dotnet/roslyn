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
    public abstract ValueTask GetAssetsAsync<T, TArg>(AssetPath assetPath, HashSet<Checksum> checksums, Action<Checksum, T, TArg>? callback, TArg? arg, CancellationToken cancellationToken);

    public async Task<SolutionInfo> CreateSolutionInfoAsync(Checksum solutionChecksum, CancellationToken cancellationToken)
    {
        var solutionCompilationChecksums = await GetAssetAsync<SolutionCompilationStateChecksums>(AssetPathKind.SolutionCompilationStateChecksums, solutionChecksum, cancellationToken).ConfigureAwait(false);
        var solutionChecksums = await GetAssetAsync<SolutionStateChecksums>(AssetPathKind.SolutionStateChecksums, solutionCompilationChecksums.SolutionState, cancellationToken).ConfigureAwait(false);

        var solutionAttributes = await GetAssetAsync<SolutionInfo.SolutionAttributes>(AssetPathKind.SolutionAttributes, solutionChecksums.Attributes, cancellationToken).ConfigureAwait(false);
        await GetAssetAsync<SourceGeneratorExecutionVersionMap>(AssetPathKind.SolutionSourceGeneratorExecutionVersionMap, solutionCompilationChecksums.SourceGeneratorExecutionVersionMap, cancellationToken).ConfigureAwait(false);

        using var _ = ArrayBuilder<ProjectInfo>.GetInstance(solutionChecksums.Projects.Length, out var projects);
        foreach (var (projectChecksum, projectId) in solutionChecksums.Projects)
            projects.Add(await CreateProjectInfoAsync(projectId, projectChecksum, cancellationToken).ConfigureAwait(false));

        var analyzerReferences = await GetAssetsAsync<AnalyzerReference>(AssetPathKind.SolutionAnalyzerReferences, solutionChecksums.AnalyzerReferences, cancellationToken).ConfigureAwait(false);

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

        var projectReferences = await GetAssetsAsync<ProjectReference>(new(AssetPathKind.ProjectProjectReferences, projectId), projectChecksums.ProjectReferences, cancellationToken).ConfigureAwait(false);
        var metadataReferences = await GetAssetsAsync<MetadataReference>(new(AssetPathKind.ProjectMetadataReferences, projectId), projectChecksums.MetadataReferences, cancellationToken).ConfigureAwait(false);
        var analyzerReferences = await GetAssetsAsync<AnalyzerReference>(new(AssetPathKind.ProjectAnalyzerReferences, projectId), projectChecksums.AnalyzerReferences, cancellationToken).ConfigureAwait(false);

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

            foreach (var (documentChecksum, documentId) in checksumsAndIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                documentInfos.Add(await CreateDocumentInfoAsync(documentId, documentChecksum, cancellationToken).ConfigureAwait(false));
            }

            return documentInfos.ToImmutableAndClear();
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

    public async Task<ImmutableArray<T>> GetAssetsAsync<T>(
        AssetPath assetPath, ChecksumCollection checksums, CancellationToken cancellationToken) where T : class
    {
        using var _1 = PooledHashSet<Checksum>.GetInstance(out var checksumSet);
        checksumSet.AddAll(checksums.Children);

        using var _2 = ArrayBuilder<T>.GetInstance(checksumSet.Count, out var builder);

        await this.GetAssetsAsync<T, ArrayBuilder<T>>(
            assetPath, checksumSet,
            static (checksum, asset, builder) => builder.Add(asset),
            builder,
            cancellationToken).ConfigureAwait(false);

        return builder.ToImmutableAndClear();
    }
}
