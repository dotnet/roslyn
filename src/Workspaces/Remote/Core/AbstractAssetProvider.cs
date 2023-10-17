﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    public abstract ValueTask<T> GetAssetAsync<T>(ProjectId? hintProject, Checksum checksum, CancellationToken cancellationToken);

    public async Task<SolutionInfo> CreateSolutionInfoAsync(Checksum solutionChecksum, CancellationToken cancellationToken)
    {
        var solutionChecksums = await GetAssetAsync<SolutionStateChecksums>(hintProject: null, solutionChecksum, cancellationToken).ConfigureAwait(false);
        var solutionAttributes = await GetAssetAsync<SolutionInfo.SolutionAttributes>(hintProject: null, solutionChecksums.Attributes, cancellationToken).ConfigureAwait(false);

        using var _ = ArrayBuilder<ProjectInfo>.GetInstance(solutionChecksums.Projects.Count, out var projects);
        for (int i = 0, n = solutionChecksums.ProjectIds.Length; i < n; i++)
            projects.AddIfNotNull(await CreateProjectInfoAsync(solutionChecksums.ProjectIds[i], solutionChecksums.Projects[i], cancellationToken).ConfigureAwait(false));

        var analyzerReferences = await CreateCollectionAsync<AnalyzerReference>(hintProject: null, solutionChecksums.AnalyzerReferences, cancellationToken).ConfigureAwait(false);

        return SolutionInfo.Create(
            solutionAttributes.Id, solutionAttributes.Version, solutionAttributes.FilePath, projects.ToImmutableAndClear(), analyzerReferences).WithTelemetryId(solutionAttributes.TelemetryId);
    }

    public async Task<ProjectInfo?> CreateProjectInfoAsync(ProjectId projectId, Checksum projectChecksum, CancellationToken cancellationToken)
    {
        var projectChecksums = await GetAssetAsync<ProjectStateChecksums>(hintProject: projectId, projectChecksum, cancellationToken).ConfigureAwait(false);
        Contract.ThrowIfFalse(projectId == projectChecksums.ProjectId);

        var attributes = await GetAssetAsync<ProjectInfo.ProjectAttributes>(hintProject: projectId, projectChecksums.Info, cancellationToken).ConfigureAwait(false);
        if (!RemoteSupportedLanguages.IsSupported(attributes.Language))
        {
            // only add project our workspace supports. 
            // workspace doesn't allow creating project with unknown languages
            return null;
        }

        var compilationOptions = attributes.FixUpCompilationOptions(
            await GetAssetAsync<CompilationOptions>(hintProject: projectId, projectChecksums.CompilationOptions, cancellationToken).ConfigureAwait(false));
        var parseOptions = await GetAssetAsync<ParseOptions>(hintProject: projectId, projectChecksums.ParseOptions, cancellationToken).ConfigureAwait(false);

        var projectReferences = await CreateCollectionAsync<ProjectReference>(hintProject: projectId, projectChecksums.ProjectReferences, cancellationToken).ConfigureAwait(false);
        var metadataReferences = await CreateCollectionAsync<MetadataReference>(hintProject: projectId, projectChecksums.MetadataReferences, cancellationToken).ConfigureAwait(false);
        var analyzerReferences = await CreateCollectionAsync<AnalyzerReference>(hintProject: projectId, projectChecksums.AnalyzerReferences, cancellationToken).ConfigureAwait(false);

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

        async Task<ImmutableArray<DocumentInfo>> CreateDocumentInfosAsync(ChecksumCollection documentChecksums)
        {
            using var _ = ArrayBuilder<DocumentInfo>.GetInstance(documentChecksums.Count, out var documentInfos);

            foreach (var documentChecksum in documentChecksums)
            {
                cancellationToken.ThrowIfCancellationRequested();
                documentInfos.Add(await CreateDocumentInfoAsync(projectId, documentChecksum, cancellationToken).ConfigureAwait(false));
            }

            return documentInfos.ToImmutableAndClear();
        }
    }

    public async Task<DocumentInfo> CreateDocumentInfoAsync(
        ProjectId projectId, Checksum documentChecksum, CancellationToken cancellationToken)
    {
        var documentSnapshot = await GetAssetAsync<DocumentStateChecksums>(hintProject: projectId, documentChecksum, cancellationToken).ConfigureAwait(false);
        var attributes = await GetAssetAsync<DocumentInfo.DocumentAttributes>(hintProject: projectId, documentSnapshot.Info, cancellationToken).ConfigureAwait(false);
        var serializableSourceText = await GetAssetAsync<SerializableSourceText>(hintProject: projectId, documentSnapshot.Text, cancellationToken).ConfigureAwait(false);

        var text = await serializableSourceText.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var textLoader = TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create(), attributes.FilePath));

        // TODO: do we need version?
        return new DocumentInfo(attributes, textLoader, documentServiceProvider: null);
    }

    public async Task<ImmutableArray<T>> CreateCollectionAsync<T>(
        ProjectId? hintProject, ChecksumCollection checksums, CancellationToken cancellationToken) where T : class
    {
        using var _ = ArrayBuilder<T>.GetInstance(checksums.Count, out var assets);

        foreach (var checksum in checksums)
        {
            cancellationToken.ThrowIfCancellationRequested();
            assets.Add(await GetAssetAsync<T>(hintProject, checksum, cancellationToken).ConfigureAwait(false));
        }

        return assets.ToImmutableAndClear();
    }
}
