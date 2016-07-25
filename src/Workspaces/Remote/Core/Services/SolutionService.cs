// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Provide solution from given checksum
    /// 
    /// TODO: change this to workspace service
    /// </summary>
    internal class SolutionService
    {
        public async Task<Solution> GetSolutionAsync(Checksum solutionChecksum, CancellationToken cancellationToken)
        {
            var solutionChecksumObject = await RoslynServices.AssetService.GetAssetAsync<SolutionChecksumObject>(solutionChecksum, cancellationToken).ConfigureAwait(false);

            // TODO: Make these to do work concurrently
            var workspace = new AdhocWorkspace(RoslynServices.HostServices);
            var solutionInfo = await RoslynServices.AssetService.GetAssetAsync<SolutionChecksumObjectInfo>(solutionChecksumObject.Info, cancellationToken).ConfigureAwait(false);

            var projects = new List<ProjectInfo>();
            foreach (var projectChecksum in solutionChecksumObject.Projects.Objects)
            {
                var projectSnapshot = await RoslynServices.AssetService.GetAssetAsync<ProjectChecksumObject>(projectChecksum, cancellationToken).ConfigureAwait(false);

                var documents = new List<DocumentInfo>();
                foreach (var documentChecksum in projectSnapshot.Documents.Objects)
                {
                    var documentSnapshot = await RoslynServices.AssetService.GetAssetAsync<DocumentChecksumObject>(documentChecksum, cancellationToken).ConfigureAwait(false);

                    var documentInfo = await RoslynServices.AssetService.GetAssetAsync<DocumentChecksumObjectInfo>(documentSnapshot.Info, cancellationToken).ConfigureAwait(false);
                    var text = await RoslynServices.AssetService.GetAssetAsync<SourceText>(documentSnapshot.Text, cancellationToken).ConfigureAwait(false);

                    // TODO: do we need version?
                    documents.Add(
                        DocumentInfo.Create(
                            documentInfo.Id,
                            documentInfo.Name,
                            documentInfo.Folders,
                            documentInfo.SourceCodeKind,
                            TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create())),
                            documentInfo.FilePath,
                            documentInfo.IsGenerated));
                }

                var p2p = new List<ProjectReference>();
                foreach (var checksum in projectSnapshot.ProjectReferences.Objects)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var reference = await RoslynServices.AssetService.GetAssetAsync<ProjectReference>(checksum, cancellationToken).ConfigureAwait(false);
                    p2p.Add(reference);
                }

                var metadata = new List<MetadataReference>();
                foreach (var checksum in projectSnapshot.MetadataReferences.Objects)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var reference = await RoslynServices.AssetService.GetAssetAsync<MetadataReference>(checksum, cancellationToken).ConfigureAwait(false);
                    metadata.Add(reference);
                }

                var analyzers = new List<AnalyzerReference>();
                foreach (var checksum in projectSnapshot.AnalyzerReferences.Objects)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var reference = await RoslynServices.AssetService.GetAssetAsync<AnalyzerReference>(checksum, cancellationToken).ConfigureAwait(false);
                    analyzers.Add(reference);
                }

                var additionals = new List<DocumentInfo>();
                foreach (var documentChecksum in projectSnapshot.AdditionalDocuments.Objects)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var documentSnapshot = await RoslynServices.AssetService.GetAssetAsync<DocumentChecksumObject>(documentChecksum, cancellationToken).ConfigureAwait(false);

                    var documentInfo = await RoslynServices.AssetService.GetAssetAsync<DocumentChecksumObjectInfo>(documentSnapshot.Info, cancellationToken).ConfigureAwait(false);
                    var text = await RoslynServices.AssetService.GetAssetAsync<SourceText>(documentSnapshot.Text, cancellationToken).ConfigureAwait(false);

                    // TODO: do we need version?
                    additionals.Add(
                        DocumentInfo.Create(
                            documentInfo.Id,
                            documentInfo.Name,
                            documentInfo.Folders,
                            documentInfo.SourceCodeKind,
                            TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create())),
                            documentInfo.FilePath,
                            documentInfo.IsGenerated));
                }

                var projectInfo = await RoslynServices.AssetService.GetAssetAsync<ProjectChecksumObjectInfo>(projectSnapshot.Info, cancellationToken).ConfigureAwait(false);
                var compilationOptions = await RoslynServices.AssetService.GetAssetAsync<CompilationOptions>(projectSnapshot.CompilationOptions, cancellationToken).ConfigureAwait(false);
                var parseOptions = await RoslynServices.AssetService.GetAssetAsync<ParseOptions>(projectSnapshot.ParseOptions, cancellationToken).ConfigureAwait(false);

                projects.Add(
                    ProjectInfo.Create(
                        projectInfo.Id, projectInfo.Version, projectInfo.Name, projectInfo.AssemblyName,
                        projectInfo.Language, projectInfo.FilePath, projectInfo.OutputFilePath,
                        compilationOptions, parseOptions,
                        documents, p2p, metadata, analyzers, additionals));
            }

            return workspace.AddSolution(SolutionInfo.Create(solutionInfo.Id, solutionInfo.Version, solutionInfo.FilePath, projects));
        }
    }
}
