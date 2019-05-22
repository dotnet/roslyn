// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// Provides corresponding data of the given checksum
    /// </summary>
    internal interface IAssetProvider
    {
        /// <summary>
        /// return data of type T whose checksum is the given checksum
        /// </summary>
        Task<T> GetAssetAsync<T>(Checksum checksum, CancellationToken cancellationToken);
    }

    internal static class SolutionInfoCreator
    {
        public static async Task<SolutionInfo> CreateSolutionInfoAsync(IAssetProvider assetProvider, Checksum solutionChecksum, CancellationToken cancellationToken)
        {
            var solutionChecksumObject = await assetProvider.GetAssetAsync<SolutionStateChecksums>(solutionChecksum, cancellationToken).ConfigureAwait(false);
            var solutionInfo = await assetProvider.GetAssetAsync<SolutionInfo.SolutionAttributes>(solutionChecksumObject.Info, cancellationToken).ConfigureAwait(false);

            var projects = new List<ProjectInfo>();
            foreach (var projectChecksum in solutionChecksumObject.Projects)
            {
                var projectInfo = await CreateProjectInfoAsync(assetProvider, projectChecksum, cancellationToken).ConfigureAwait(false);
                if (projectInfo != null)
                {
                    projects.Add(projectInfo);
                }
            }

            return SolutionInfo.Create(solutionInfo.Id, solutionInfo.Version, solutionInfo.FilePath, projects);
        }

        public static async Task<ProjectInfo> CreateProjectInfoAsync(IAssetProvider assetProvider, Checksum projectChecksum, CancellationToken cancellationToken)
        {
            var projectSnapshot = await assetProvider.GetAssetAsync<ProjectStateChecksums>(projectChecksum, cancellationToken).ConfigureAwait(false);

            var projectInfo = await assetProvider.GetAssetAsync<ProjectInfo.ProjectAttributes>(projectSnapshot.Info, cancellationToken).ConfigureAwait(false);
            if (!RemoteSupportedLanguages.IsSupported(projectInfo.Language))
            {
                // only add project our workspace supports. 
                // workspace doesn't allow creating project with unknown languages
                return null;
            }

            var compilationOptions = projectInfo.FixUpCompilationOptions(
                await assetProvider.GetAssetAsync<CompilationOptions>(projectSnapshot.CompilationOptions, cancellationToken).ConfigureAwait(false));

            var parseOptions = await assetProvider.GetAssetAsync<ParseOptions>(projectSnapshot.ParseOptions, cancellationToken).ConfigureAwait(false);

            var p2p = await assetProvider.CreateCollectionAsync<ProjectReference>(projectSnapshot.ProjectReferences, cancellationToken).ConfigureAwait(false);
            var metadata = await assetProvider.CreateCollectionAsync<MetadataReference>(projectSnapshot.MetadataReferences, cancellationToken).ConfigureAwait(false);
            var analyzers = await assetProvider.CreateCollectionAsync<AnalyzerReference>(projectSnapshot.AnalyzerReferences, cancellationToken).ConfigureAwait(false);

            var documentInfos = await CreateDocumentInfosAsync(assetProvider, projectSnapshot.Documents, cancellationToken).ConfigureAwait(false);
            var additionalDocumentInfos = await CreateDocumentInfosAsync(assetProvider, projectSnapshot.AdditionalDocuments, cancellationToken).ConfigureAwait(false);
            var analyzerConfigDocumentInfos = await CreateDocumentInfosAsync(assetProvider, projectSnapshot.AnalyzerConfigDocuments, cancellationToken).ConfigureAwait(false);

            return ProjectInfo.Create(
                projectInfo.Id, projectInfo.Version, projectInfo.Name, projectInfo.AssemblyName,
                projectInfo.Language, projectInfo.FilePath, projectInfo.OutputFilePath,
                compilationOptions, parseOptions,
                documentInfos, p2p, metadata, analyzers, additionalDocumentInfos, projectInfo.IsSubmission)
                .WithOutputRefFilePath(projectInfo.OutputRefFilePath)
                .WithHasAllInformation(projectInfo.HasAllInformation)
                .WithDefaultNamespace(projectInfo.DefaultNamespace)
                .WithAnalyzerConfigDocuments(analyzerConfigDocumentInfos);
        }

        public static async Task<DocumentInfo> CreateDocumentInfoAsync(IAssetProvider assetProvider, Checksum documentChecksum, CancellationToken cancellationToken)
        {
            var documentSnapshot = await assetProvider.GetAssetAsync<DocumentStateChecksums>(documentChecksum, cancellationToken).ConfigureAwait(false);
            var documentInfo = await assetProvider.GetAssetAsync<DocumentInfo.DocumentAttributes>(documentSnapshot.Info, cancellationToken).ConfigureAwait(false);

            var textLoader = TextLoader.From(
                TextAndVersion.Create(
                    await assetProvider.GetAssetAsync<SourceText>(documentSnapshot.Text, cancellationToken).ConfigureAwait(false),
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

        private static async Task<IEnumerable<DocumentInfo>> CreateDocumentInfosAsync(IAssetProvider assetProvider, ChecksumCollection documentChecksums, CancellationToken cancellationToken)
        {
            var documentInfos = new List<DocumentInfo>();

            foreach (var documentChecksum in documentChecksums)
            {
                cancellationToken.ThrowIfCancellationRequested();
                documentInfos.Add(await CreateDocumentInfoAsync(assetProvider, documentChecksum, cancellationToken).ConfigureAwait(false));
            }

            return documentInfos;
        }
    }
}
