// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote
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
            var solutionChecksums = await GetAssetAsync<SolutionStateChecksums>(solutionChecksum, cancellationToken).ConfigureAwait(false);
            var solutionAttributes = await GetAssetAsync<SolutionInfo.SolutionAttributes>(solutionChecksums.Attributes, cancellationToken).ConfigureAwait(false);

            var projects = new List<ProjectInfo>();
            foreach (var projectChecksum in solutionChecksums.Projects)
            {
                var projectInfo = await CreateProjectInfoAsync(projectChecksum, cancellationToken).ConfigureAwait(false);
                if (projectInfo != null)
                {
                    projects.Add(projectInfo);
                }
            }

            var analyzerReferences = await CreateCollectionAsync<AnalyzerReference>(solutionChecksums.AnalyzerReferences, cancellationToken).ConfigureAwait(false);

            var info = SolutionInfo.Create(solutionAttributes.Id, solutionAttributes.Version, solutionAttributes.FilePath, projects, analyzerReferences).WithTelemetryId(solutionAttributes.TelemetryId);
            var options = await GetAssetAsync<SerializableOptionSet>(solutionChecksums.Options, cancellationToken).ConfigureAwait(false);
            return (info, options);
        }

        public async Task<ProjectInfo> CreateProjectInfoAsync(Checksum projectChecksum, CancellationToken cancellationToken)
        {
            var projectChecksums = await GetAssetAsync<ProjectStateChecksums>(projectChecksum, cancellationToken).ConfigureAwait(false);

            var projectInfo = await GetAssetAsync<ProjectInfo.ProjectAttributes>(projectChecksums.Info, cancellationToken).ConfigureAwait(false);
            if (!RemoteSupportedLanguages.IsSupported(projectInfo.Language))
            {
                // only add project our workspace supports. 
                // workspace doesn't allow creating project with unknown languages
                return null;
            }

            var compilationOptions = projectInfo.FixUpCompilationOptions(
                await GetAssetAsync<CompilationOptions>(projectChecksums.CompilationOptions, cancellationToken).ConfigureAwait(false));

            var parseOptions = await GetAssetAsync<ParseOptions>(projectChecksums.ParseOptions, cancellationToken).ConfigureAwait(false);

            var projectReferences = await CreateCollectionAsync<ProjectReference>(projectChecksums.ProjectReferences, cancellationToken).ConfigureAwait(false);
            var metadataReferences = await CreateCollectionAsync<MetadataReference>(projectChecksums.MetadataReferences, cancellationToken).ConfigureAwait(false);
            var analyzerReferences = await CreateCollectionAsync<AnalyzerReference>(projectChecksums.AnalyzerReferences, cancellationToken).ConfigureAwait(false);

            var documentInfos = await CreateDocumentInfosAsync(projectChecksums.Documents, cancellationToken).ConfigureAwait(false);
            var additionalDocumentInfos = await CreateDocumentInfosAsync(projectChecksums.AdditionalDocuments, cancellationToken).ConfigureAwait(false);
            var analyzerConfigDocumentInfos = await CreateDocumentInfosAsync(projectChecksums.AnalyzerConfigDocuments, cancellationToken).ConfigureAwait(false);

            return ProjectInfo.Create(
                projectInfo.Id,
                projectInfo.Version,
                projectInfo.Name,
                projectInfo.AssemblyName,
                projectInfo.Language,
                projectInfo.FilePath,
                projectInfo.OutputFilePath,
                compilationOptions,
                parseOptions,
                documentInfos,
                projectReferences,
                metadataReferences,
                analyzerReferences,
                additionalDocumentInfos,
                projectInfo.IsSubmission)
                .WithOutputRefFilePath(projectInfo.OutputRefFilePath)
                .WithCompilationOutputInfo(projectInfo.CompilationOutputInfo)
                .WithHasAllInformation(projectInfo.HasAllInformation)
                .WithRunAnalyzers(projectInfo.RunAnalyzers)
                .WithDefaultNamespace(projectInfo.DefaultNamespace)
                .WithAnalyzerConfigDocuments(analyzerConfigDocumentInfos)
                .WithTelemetryId(projectInfo.TelemetryId);
        }

        public async Task<DocumentInfo> CreateDocumentInfoAsync(Checksum documentChecksum, CancellationToken cancellationToken)
        {
            var documentSnapshot = await GetAssetAsync<DocumentStateChecksums>(documentChecksum, cancellationToken).ConfigureAwait(false);
            var documentInfo = await GetAssetAsync<DocumentInfo.DocumentAttributes>(documentSnapshot.Info, cancellationToken).ConfigureAwait(false);
            var serializableSourceText = await GetAssetAsync<SerializableSourceText>(documentSnapshot.Text, cancellationToken).ConfigureAwait(false);

            var textLoader = TextLoader.From(
                TextAndVersion.Create(
                    await serializableSourceText.GetTextAsync(cancellationToken).ConfigureAwait(false),
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
                documentInfo.IsGenerated,
                documentInfo.DesignTimeOnly,
                documentServiceProvider: null);
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
