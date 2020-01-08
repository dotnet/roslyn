// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// Provides corresponding data of the given checksum
    /// </summary>
    internal abstract class AbstractAssetProvider
    {
        /// <summary>
        /// return data of type T whose checksum is the given checksum
        /// </summary>
        public abstract Task<T> GetAssetAsync<T>(Checksum checksum, CancellationToken cancellationToken);

        public async Task<(SolutionInfo, SerializableOptionSet)> CreateSolutionInfoAndOptionsAsync(Checksum solutionChecksum, CancellationToken cancellationToken)
        {
            var solutionChecksumObject = await GetAssetAsync<SolutionStateChecksums>(solutionChecksum, cancellationToken).ConfigureAwait(false);
            var solutionInfo = await GetAssetAsync<SolutionInfo.SolutionAttributes>(solutionChecksumObject.Info, cancellationToken).ConfigureAwait(false);

            var projects = new List<ProjectInfo>();
            foreach (var projectChecksum in solutionChecksumObject.Projects)
            {
                var projectInfo = await CreateProjectInfoAsync(projectChecksum, cancellationToken).ConfigureAwait(false);
                if (projectInfo != null)
                {
                    projects.Add(projectInfo);
                }
            }

            var info = SolutionInfo.Create(solutionInfo.Id, solutionInfo.Version, solutionInfo.FilePath, projects);
            var options = await GetAssetAsync<SerializableOptionSet>(solutionChecksumObject.Options, cancellationToken).ConfigureAwait(false);
            return (info, options);
        }

        public async Task<ProjectInfo> CreateProjectInfoAsync(Checksum projectChecksum, CancellationToken cancellationToken)
        {
            var projectSnapshot = await GetAssetAsync<ProjectStateChecksums>(projectChecksum, cancellationToken).ConfigureAwait(false);

            var projectInfo = await GetAssetAsync<ProjectInfo.ProjectAttributes>(projectSnapshot.Info, cancellationToken).ConfigureAwait(false);
            if (!RemoteSupportedLanguages.IsSupported(projectInfo.Language))
            {
                // only add project our workspace supports. 
                // workspace doesn't allow creating project with unknown languages
                return null;
            }

            var compilationOptions = projectInfo.FixUpCompilationOptions(
                await GetAssetAsync<CompilationOptions>(projectSnapshot.CompilationOptions, cancellationToken).ConfigureAwait(false));

            var parseOptions = await GetAssetAsync<ParseOptions>(projectSnapshot.ParseOptions, cancellationToken).ConfigureAwait(false);

            var p2p = await CreateCollectionAsync<ProjectReference>(projectSnapshot.ProjectReferences, cancellationToken).ConfigureAwait(false);
            var metadata = await CreateCollectionAsync<MetadataReference>(projectSnapshot.MetadataReferences, cancellationToken).ConfigureAwait(false);
            var analyzers = await CreateCollectionAsync<AnalyzerReference>(projectSnapshot.AnalyzerReferences, cancellationToken).ConfigureAwait(false);

            var documentInfos = await CreateDocumentInfosAsync(projectSnapshot.Documents, cancellationToken).ConfigureAwait(false);
            var additionalDocumentInfos = await CreateDocumentInfosAsync(projectSnapshot.AdditionalDocuments, cancellationToken).ConfigureAwait(false);
            var analyzerConfigDocumentInfos = await CreateDocumentInfosAsync(projectSnapshot.AnalyzerConfigDocuments, cancellationToken).ConfigureAwait(false);

            return ProjectInfo.Create(
                projectInfo.Id, projectInfo.Version, projectInfo.Name, projectInfo.AssemblyName,
                projectInfo.Language, projectInfo.FilePath, projectInfo.OutputFilePath,
                compilationOptions, parseOptions,
                documentInfos, p2p, metadata, analyzers, additionalDocumentInfos, projectInfo.IsSubmission)
                .WithOutputRefFilePath(projectInfo.OutputRefFilePath)
                .WithHasAllInformation(projectInfo.HasAllInformation)
                .WithRunAnalyzers(projectInfo.RunAnalyzers)
                .WithDefaultNamespace(projectInfo.DefaultNamespace)
                .WithAnalyzerConfigDocuments(analyzerConfigDocumentInfos);
        }

        public async Task<DocumentInfo> CreateDocumentInfoAsync(Checksum documentChecksum, CancellationToken cancellationToken)
        {
            var documentSnapshot = await GetAssetAsync<DocumentStateChecksums>(documentChecksum, cancellationToken).ConfigureAwait(false);
            var documentInfo = await GetAssetAsync<DocumentInfo.DocumentAttributes>(documentSnapshot.Info, cancellationToken).ConfigureAwait(false);

            var textLoader = TextLoader.From(
                TextAndVersion.Create(
                    await GetAssetAsync<SourceText>(documentSnapshot.Text, cancellationToken).ConfigureAwait(false),
                    VersionStamp.Create(),
                    documentInfo.FilePath));

            // TODO: do we need version?
            return DocumentInfo.Create(
                documentInfo.Id,
                documentInfo.Name,
                documentInfo.Folders,
                documentInfo.SourceCodeKind,
                textLoader,
                documentInfo.FilePath,
                documentInfo.IsGenerated);
        }

        private async Task<IEnumerable<DocumentInfo>> CreateDocumentInfosAsync(ChecksumCollection documentChecksums, CancellationToken cancellationToken)
        {
            var documentInfos = new List<DocumentInfo>();

            foreach (var documentChecksum in documentChecksums)
            {
                cancellationToken.ThrowIfCancellationRequested();
                documentInfos.Add(await CreateDocumentInfoAsync(documentChecksum, cancellationToken).ConfigureAwait(false));
            }

            return documentInfos;
        }

        public async Task<List<T>> CreateCollectionAsync<T>(ChecksumCollection checksums, CancellationToken cancellationToken)
        {
            var assets = new List<T>();

            foreach (var checksum in checksums)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var asset = await GetAssetAsync<T>(checksum, cancellationToken).ConfigureAwait(false);
                assets.Add(asset);
            }

            return assets;
        }
    }
}
